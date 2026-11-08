using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestEmbroidery()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        Vector2I originalSize = Root.Size;
        string originalLocale = TranslationServer.GetLocale();
        try
        {
            var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
            double dayTime = game.DayRemainingSeconds;
            await ClickControl(game, "World/Atelier/GetItemButton");
            Check(embroidery.IsOpen && Paused && game.StoredItems.Length == 0 && game.Score == 220,
                "Atelier click must open embroidery without granting items or charging before confirmation");
            double beforeCraftFrame = game.DayRemainingSeconds;
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.DayRemainingSeconds < beforeCraftFrame && beforeCraftFrame <= dayTime,
                "The trading clock must continue on the ready embroidery screen");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            Check(!game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
                "Pause menu must not steal the embroidery pause");
            await ClickControl(game, "EmbroideryGame/Surface/Cancel");
            Check(!Paused && !embroidery.IsOpen && game.Score == 220 && game.EnergyRemaining == 2,
                "Cancel before start must restore the world without cost");

            foreach (Vector2I size in new[] { new Vector2I(1280,720), new Vector2I(960,540) })
            {
                Root.Size = size;
                game.GetItemFromAtelier();
                await ToSignal(this, SignalName.ProcessFrame);
                var surface = embroidery.GetNode<EmbroiderySurface>("Surface");
                Check(new Rect2(Vector2.Zero, Root.GetVisibleRect().Size).Encloses(surface.PanelRect),
                    "Embroidery panel must fit the viewport independently of camera zoom");
                Check(surface.PanelRect.Encloses(surface.QualityTrackRect.Grow(3)) &&
                      surface.QualityTrackRect.Size.Y == surface.FabricRect.Size.Y,
                    "The framed quality scale must span the fabric height and fit the panel");
                foreach (string stat in new[] { "Time", "Stitches", "Misses" })
                {
                    var card = surface.GetNode<Control>("Stat" + stat);
                    Check(surface.PanelRect.Encloses(card.GetRect()) && card.GetRect().End.Y < surface.FabricRect.Position.Y,
                        "Stat cards must fit above the embroidery fabric");
                }
                Check(surface.GetNode<Label>("StatTime/Value").Text == "10.0" &&
                      surface.GetNode<Label>("StatStitches/Value").Text == "0 / " + embroidery.Session.Points.Count &&
                      surface.GetNode<Label>("StatMisses/Value").Text == "0",
                    "Ready stat cards must show full time, current pattern length and zero misses");
                for (int i = 0; i < 5; i++)
                {
                    var label = surface.GetNode<Label>("Rank" + "SABCD"[i]);
                    Check(label.Text == "SABCD"[i].ToString() && surface.PanelRect.Encloses(label.GetRect()) &&
                          label.GetRect().End.X < surface.QualityTrackRect.Position.X &&
                          Mathf.IsEqualApprox(label.GetRect().GetCenter().Y,
                              surface.QualityTrackRect.Position.Y + surface.QualityTrackRect.Size.Y * (i + 0.5f) / 5),
                        "Ranks S–D must align to the five sections to the left of the scale");
                }
                Check(surface.GetNode<Label>("RankS").GetThemeColor("font_color") == new Color("ffe0a0"),
                    "The initial S rank must be highlighted in warm gold");
                foreach (string button in new[] { "Action", "Cancel" })
                    Check(surface.PanelRect.Encloses(surface.GetNode<Control>(button).GetRect()),
                        "Embroidery buttons must fit the panel");
                await ClickControl(game, "EmbroideryGame/Surface/Cancel");
            }

            game.GetItemFromAtelier();
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Escape }, true);
            Check(!embroidery.IsOpen && !Paused, "Escape must cancel the ready craft without opening pause menu");

            TranslationServer.SetLocale("en");
            game.GetItemFromAtelier();
            await ToSignal(this, SignalName.ProcessFrame);
            Check(embroidery.GetNode<Label>("Surface/Title").Text == "Embroidery" &&
                  embroidery.GetNode<Button>("Surface/Action").Text == "Start",
                "Embroidery text must follow the chosen language");
            Check(embroidery.GetNode<Label>("Surface/StatTime/Heading").Text == "TIME" &&
                  embroidery.GetNode<Label>("Surface/StatStitches/Heading").Text == "STITCHES" &&
                  embroidery.GetNode<Label>("Surface/StatMisses/Heading").Text == "MISSES",
                "Stat card headings must follow the chosen language");
            await ClickControl(game, "EmbroideryGame/Surface/Action");
            var board = embroidery.GetNode<EmbroiderySurface>("Surface");
            var camera = game.GetNode<WorldCamera>("Camera2D");
            Vector2 cameraPosition = camera.Position;
            Vector2 cameraZoom = camera.Zoom;
            Vector2 badPoint = board.FabricRect.Position + Vector2.One * 2;
            PushEmbroideryClick(badPoint);
            Check(embroidery.Session.Misses == 1, "A real click outside the target must count a miss");
            Root.PushInput(new InputEventMouseButton { Position = badPoint, ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
            Check(camera.Zoom == cameraZoom && camera.Position == cameraPosition,
                "The camera must ignore wheel and craft clicks while embroidery owns pause");
            int count = embroidery.Session.Points.Count;
            for (int i=0;i<count;i++)
            {
                Vector2 point = board.ScreenPoint(i);
                if (i == 0)
                {
                    Root.PushInput(new InputEventScreenTouch { Position = point, Index = 0, Pressed = true }, true);
                    Root.PushInput(new InputEventScreenTouch { Position = point, Index = 0, Pressed = false }, true);
                }
                else PushEmbroideryClick(point);
            }
            await ToSignal(this, SignalName.ProcessFrame);
            Check(embroidery.Session.Complete && embroidery.Session.Stitches == count &&
                  embroidery.Session.Misses == 1 && game.StoredItems.Length == 3 && game.Score == 130,
                "Mouse and touch must stitch the ordered pattern, with the purchase committed at start");
            Check(board.GetNode<Label>("StatStitches/Value").Text == $"{count} / {count}" &&
                  board.GetNode<Label>("StatMisses/Value").Text == "1" &&
                  board.GetNode<Label>("StatTime/Value").Text == FormattableString.Invariant($"{embroidery.Session.RemainingSeconds:0.0}"),
                "Stat cards must reflect actual elapsed time, completed stitches and misses");
            Check(board.GetNode<Label>("Rank" + embroidery.Session.Rank).GetThemeColor("font_color") == new Color("ffe0a0") &&
                  board.GetNode<Label>("RankS").GetThemeColor("font_color") == new Color("a99b83") &&
                  board.GetNode<Label>("Quality").Text == embroidery.Session.Quality + "%",
                "The active rank must follow quality, with only the percentage below the scale");
            await ClickControl(game, "EmbroideryGame/Surface/Action");
            embroidery.Activate();
            Check(!Paused && !embroidery.IsOpen && game.StoredItems.Length == 3 && game.Score == 130 && game.EnergyRemaining == 1,
                "Collect must keep the paid clothing batch without duplicating it and unpause");
            CraftAtelier(game);
            Check(game.StoredItems.Length == 6 && game.Score == 40 && game.EnergyRemaining == 0,
                "Timeout must settle the same paid batch with a lower assessment");
            game.GetItemFromAtelier();
            Check(!embroidery.IsOpen && !Paused, "Unavailable crafting must never open or pause");
            game.NewGame();
            game.SetProcess(false);
            embroidery.SetProcess(false);
            game.GetItemFromAtelier();
            double readyTime = game.DayRemainingSeconds;
            game._Process(1);
            Check(game.DayRemainingSeconds == readyTime, "The paused world must not double-tick the craft clock");
            embroidery._Process(1);
            Check(game.DayRemainingSeconds == readyTime - 1 && embroidery.Session.RemainingSeconds == 10,
                "Waiting to start must spend day time without spending embroidery time");
            embroidery.Activate();
            embroidery.Activate();
            Check(game.Score == 130 && game.EnergyRemaining == 1 && game.StoredItems.Length == 3,
                "Starting must commit the paid batch only once");
            embroidery._Process(2);
            Check(game.DayRemainingSeconds == readyTime - 3 && embroidery.Session.RemainingSeconds == 8,
                "Playing must advance both clocks by the same delta");
            embroidery._Process(8);
            double resultTime = game.DayRemainingSeconds;
            embroidery._Process(1);
            Check(game.DayRemainingSeconds == resultTime - 1 && embroidery.Session.RemainingSeconds == 0,
                "Viewing the finished result must continue to spend day time");
            embroidery.Activate();

            game.NewGame();
            game._Process(119.5);
            game.GetItemFromAtelier();
            embroidery._Process(0.5);
            Check(game.DayClosed && !embroidery.IsOpen && !Paused && game.StoredItems.Length == 0 && game.Score == 220,
                "Expiry on the ready screen must cancel the unstarted craft without charging");

            game.NewGame();
            game._Process(119.5);
            game.GetItemFromAtelier();
            embroidery.Activate();
            embroidery._Process(0.5);
            Check(game.DayClosed && embroidery.IsOpen && Paused && game.EnergyRemaining == 0 &&
                  game.StoredItems.Length == 3 && game.Score == 130 &&
                  embroidery.GetNode<Label>("Surface/DayTimer").Text == "00:00",
                "Expiry during a started craft must close the day and retain the paid batch");
            embroidery._Process(10);
            embroidery.Activate();
            embroidery.Activate();
            Check(!Paused && !embroidery.IsOpen && game.StoredItems.Length == 3 && game.Score == 130 && game.DayClosed,
                "Finishing after expiry must neither lose nor duplicate the paid batch");
            game.GetItemFromAtelier();
            Check(!embroidery.IsOpen, "A closed day must reject another embroidery session");
            game.EndDay();
            Check(!game.DayClosed && game.DayRemainingSeconds == 120 && game.EnergyRemaining == 2,
                "After finishing the craft, the next day must start with two minutes");
            game.NewGame();
            game.GetItemFromAtelier();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            Check(!Paused, "Removing a crafting scene must release its pause");
        }
        finally
        {
            Root.Size = originalSize;
            TranslationServer.SetLocale(originalLocale);
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Embroidery GUI, payment, timeout, localization and pause checks passed");
    }

    private async Task TestPolishing()
    {
        var scene=GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game=scene.Instantiate<TradeScene>(); game.SavePath="";
        Root.AddChild(game);
        await ToSignal(this,SignalName.ProcessFrame);
        Vector2I originalSize=Root.Size;
        string originalLocale=TranslationServer.GetLocale();
        const string path="user://polishing_progress_test.json";
        try
        {
            var polishing=game.GetNode<EmbroideryGame>("PolishingGame");
            game.GetItemFromMusicWorkshop();
            Check(!polishing.IsOpen && !Paused,"Locked music workshop must not open polishing");
            game.Score=500; game.TryUnlockMusicWorkshop();
            foreach(string kind in new[]{"violin","trumpet","sax","guitar"})
            {
                var mask=GD.Load<Texture2D>($"res://art/minigames/polishing_{kind}_mask.svg").GetImage();
                Check(mask.GetWidth()==600 && mask.GetHeight()==480 &&
                      GD.Load<Texture2D>($"res://art/minigames/polishing_{kind}.svg")!=null,
                    "All four reference illustrations and masks must import at matching dimensions");
                mask.Convert(Image.Format.Rgba8);
                byte[] rgba=mask.GetData(),alpha=new byte[600*480];
                for(int i=0;i<alpha.Length;i++) alpha[i]=rgba[i*4+3];
                var model=new ChikagoShop.Domain.Crafting.PolishingSession(alpha);
                Check(model.Cells.Count(cell=>cell.Inside)>100,
                    "Every reference instrument must supply a nonempty playable polishing region");
            }
            foreach(Vector2I size in new[]{new Vector2I(1280,720),new Vector2I(960,540)})
            {
                Root.Size=size;
                await ClickControl(game,"World/MusicWorkshop/GetItemButton");
                Check(polishing.IsOpen && Paused && game.Score==400 && game.StoredItems.Length==0,
                    "The real music create button must open the shared craft shell without payment yet");
                var surface=polishing.GetNode<EmbroiderySurface>("Surface");
                var board=surface.GetNode<PolishingSurface>("Polishing");
                Check(surface.FabricRect==board.GetRect() && surface.PanelRect.Encloses(surface.QualityTrackRect.Grow(3)) &&
                      board.GetRect().Size.X>board.GetRect().Size.Y,
                    "Polishing must occupy the existing fabric workspace with the same quality scale and layout");
                Check(surface.GetNode<Label>("StatStitches/Value").Text=="0%" &&
                      surface.GetNode<Label>("StatMisses/Value").Text=="0" &&
                      surface.GetNode<Label>("StatTime/Value").Text=="10.0",
                    "Shared cards must show polishing progress, misses and ten seconds");
                double remaining=game.DayRemainingSeconds;
                await ToSignal(this,SignalName.ProcessFrame);
                Check(game.DayRemainingSeconds<remaining,"The day clock must continue on the polishing ready screen");
                game.GetItemFromAtelier(); game.GetNode<PauseMenu>("PauseMenu").Open();
                Check(!game.GetNode<EmbroideryGame>("EmbroideryGame").IsOpen &&
                      !game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
                    "Another craft and pause menu must not steal polishing's modal state");
                await ClickControl(game,"PolishingGame/Surface/Cancel");
                Check(!Paused && !polishing.IsOpen && game.Score==400,"Cancel polishing must be free");
            }
            TranslationServer.SetLocale("en");
            game.GetItemFromMusicWorkshop();
            await ToSignal(this,SignalName.ProcessFrame);
            Check(polishing.GetNode<Label>("Surface/Title").Text=="Polishing" &&
                  polishing.GetNode<Label>("Surface/StatStitches/Heading").Text=="CLEANED",
                "Polishing-specific content must follow RU/EN while sharing the shell");
            Root.PushInput(new InputEventKey { Keycode=Key.Escape,Pressed=true },true);
            Check(!Paused && !polishing.IsOpen,"Escape must cancel ready polishing without opening pause");
            game.GetItemFromMusicWorkshop();
            await ClickControl(game,"PolishingGame/Surface/Action");
            Check(game.Score==260 && game.EnergyRemaining==1 && game.StoredItems.Length==3 &&
                  game.StoredItems.All(index=>WorkshopItemCatalog.Items[index].WorkshopId=="music"),
                "Polishing start must commit exactly three music items for 140 dollars and one energy");
            polishing.Activate();
            Check(game.Score==260 && game.StoredItems.Length==3,"Duplicate start must not buy another batch");
            var workspace=polishing.GetNode<PolishingSurface>("Surface/Polishing");
            var target=polishing.Polishing.Cells.First(cell=>cell.Inside && polishing.Polishing.Inside(cell.X+14,cell.Y));
            Vector2 left=workspace.LogicalToScreen(new Vector2((float)target.X,(float)target.Y));
            Vector2 right=workspace.LogicalToScreen(new Vector2((float)target.X+14,(float)target.Y));
            void Motion(Vector2 point,bool held=true)=>Root.PushInput(new InputEventMouseMotion
                { Position=point,GlobalPosition=point,ButtonMask=held?MouseButtonMask.Left:0 },true);
            Motion(left);
            Root.PushInput(new InputEventMouseButton { Position=left,GlobalPosition=left,ButtonIndex=MouseButton.Left,Pressed=true,ButtonMask=MouseButtonMask.Left },true);
            for(int i=0;i<30;i++) Motion(i%2==0?right:left);
            Check(polishing.Polishing.Progress>0 && polishing.Polishing.DirtProgress>0,
                "Actual held mouse strokes must remove coating from the instrument");
            Vector2 far=workspace.LogicalToScreen(Vector2.Zero);
            Motion(far);
            Check(polishing.Polishing.Misses==1,"Dragging clearly outside the silhouette must count one miss");
            Root.PushInput(new InputEventMouseButton { Position=new Vector2(2,2),GlobalPosition=new Vector2(2,2),ButtonIndex=MouseButton.Left,Pressed=false },true);
            double progress=polishing.Polishing.Progress;
            Motion(left,false); Motion(right,false);
            Check(polishing.Polishing.Progress==progress,"Releasing outside the board must stop rubbing");
            Root.PushInput(new InputEventScreenTouch { Position=left,Index=0,Pressed=true },true);
            for(int i=0;i<15;i++) Root.PushInput(new InputEventScreenDrag { Position=i%2==0?right:left,Index=0 },true);
            Root.PushInput(new InputEventScreenTouch { Position=left,Index=0,Pressed=false },true);
            Check(polishing.Polishing.Progress>progress,"Touch dragging must also polish without synthetic mouse duplication");
            polishing._Process(0);
            Check(polishing.GetNode<Label>("Surface/StatMisses/Value").Text=="1" &&
                  polishing.GetNode<Label>("Surface/StatStitches/Value").Text==$"{Math.Round(polishing.Polishing.Progress*100)}%",
                "The shared stat cards must update from the polishing session");
            if(OS.GetCmdlineUserArgs().Contains("--polishing-preview"))
            {
                await ToSignal(this,SignalName.ProcessFrame);
                await ToSignal(this,SignalName.ProcessFrame);
                Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"chikago-polishing-preview.png"));
            }
            double beforeTimeout=game.DayRemainingSeconds;
            polishing._Process(10);
            Check(polishing.ActiveSession.State==ChikagoShop.Domain.Crafting.EmbroideryState.Finished &&
                  game.DayRemainingSeconds==beforeTimeout-10,"Both clocks must advance through polishing timeout");
            await ClickControl(game,"PolishingGame/Surface/Action");
            Check(!Paused && !polishing.IsOpen && game.Score==260 && game.StoredItems.Length==3,
                "Collect after polishing must resume the world without duplicating payment or goods");

            game.SetProcess(false); polishing.SetProcess(false);
            game.NewGame(); game.Score=500; game.TryUnlockMusicWorkshop();
            game._Process(119.5); game.GetItemFromMusicWorkshop(); polishing._Process(.5);
            Check(game.DayClosed && !polishing.IsOpen && !Paused && game.Score==400,
                "Expiry before starting polishing must cancel without cost");
            game.NewGame(); game.Score=500; game.TryUnlockMusicWorkshop();
            game._Process(119.5); game.GetItemFromMusicWorkshop(); polishing.Activate(); polishing._Process(.5);
            Check(game.DayClosed && polishing.IsOpen && game.StoredItems.Length==3 && game.Score==260,
                "A started polishing craft must retain its batch when the day expires");
            polishing._Process(10); polishing.Activate(); polishing.Activate();
            Check(!Paused && game.Score==260 && game.StoredItems.Length==3,"Late collect must remain idempotent");

            game.NewGame(); game.Score=500; game.TryUnlockMusicWorkshop(); game.SavePath=path;
            game.GetItemFromMusicWorkshop(); polishing.Activate();
            int[] items=game.StoredItems;
            game.QueueFree(); await ToSignal(this,SignalName.ProcessFrame);
            Check(!Paused,"Leaving a started polishing scene must release its modal pause");
            game=scene.Instantiate<TradeScene>(); game.SavePath=path; Root.AddChild(game); game.SetProcess(false);
            await ToSignal(this,SignalName.ProcessFrame);
            Check(game.Score==260 && game.EnergyRemaining==1 && game.StoredItems.SequenceEqual(items) &&
                  !game.CraftMiniGameOpen && !Paused,"Reload must keep the paid music batch without reopening or duplicating the craft");
        }
        finally
        {
            if(GodotObject.IsInstanceValid(game)) game.QueueFree();
            if(FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            Root.Size=originalSize; TranslationServer.SetLocale(originalLocale);
        }
        await ToSignal(this,SignalName.ProcessFrame);
        GD.Print("Polishing shared shell, mouse/touch strokes, costs, clocks, expiry and reload checks passed");
    }

    private void PushEmbroideryClick(Vector2 point)
    {
        Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
        Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = false }, true);
    }

    private static void CraftMusic(TradeScene game)
    {
        game.GetItemFromMusicWorkshop();
        FinishMusic(game);
    }

    private static void FinishMusic(TradeScene game)
    {
        var polishing = game.GetNode<EmbroideryGame>("PolishingGame");
        if (!polishing.IsOpen) return;
        polishing.Activate();
        polishing._Process(10);
        polishing.Activate();
    }

    private static void CraftAtelier(TradeScene game)
    {
        game.GetItemFromAtelier();
        FinishEmbroidery(game);
    }

    private static void FinishEmbroidery(TradeScene game)
    {
        var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
        if (!embroidery.IsOpen) return;
        embroidery.Activate();
        embroidery._Process(10);
        embroidery.Activate();
    }
}
