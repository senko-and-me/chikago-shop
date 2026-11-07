using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

public partial class ProjectTest : SceneTree
{
    private const string SavePath = "user://trade_progress_test.json";

    public override void _Initialize() => Callable.From(Run).CallDeferred();

    private async void Run()
    {
        string originalLocale = TranslationServer.GetLocale();
        TranslationServer.SetLocale("ru");
        try
        {
            if (OS.GetCmdlineUserArgs().Contains("--intro-transition-checks"))
            {
                await TestIntroTransition();
                GD.Print("INTRO_TRANSITION_SMOKE_PASS: finish, skip, null fallback and continue reached TradeScene");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--balance-checks"))
            {
                await TestLiveEconomyBalance();
                await TestClockHud();
                await TestDayCardPurchases();
                GD.Print("Live economy, clock and paid upgrade regression checks passed");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--day-timer-checks"))
            {
                await TestClockHud();
                GD.Print("Clock HUD seconds, assets, layout, expiry and reload checks passed");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--upgrade-purchase-checks"))
            {
                await TestDayCardPurchases();
                GD.Print("Paid upgrade and skip checks passed");
                Quit();
                return;
            }
            TestEconomyConfig();
            await TestPlaceholderItems();
            TestPopupPlacement();
            await TestStandaloneCamera();
            await TestUiLayoutInvalidation();
            await TestPauseAndZoomUi();
            await TestCollapsibleProgress();
            await TestRarityPopupGeometry();
            await TestEmbroidery();
            await TestPolishing();
            await TestLockedRoomDrag();
            await TestMouseControls();
            await TestInventoryTransfers();
            await TestTradeScene();
            await TestShowcaseReuse();
            await TestProgress();
            await TestDayCycle();
            await TestClockHud();
            await TestDayTransitionCards();
            await TestDayCardPurchases();
            await TestCategoryPriceUpgrades();
            await TestLocalization();
            await TestLongSentenceScroll();
            await TestMainMenu();
            GD.Print("All C# project checks passed");
            Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            Quit(1);
        }
        finally
        {
            TranslationServer.SetLocale(originalLocale);
        }
    }

    private async Task TestPlaceholderItems()
    {
        const string path = "user://placeholder_items_test.json";
        TradeScene game = null;
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        async Task<TradeScene> Load()
        {
            var loaded = scene.Instantiate<TradeScene>();
            loaded.SavePath = path;
            Root.AddChild(loaded);
            loaded.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            return loaded;
        }
        try
        {
            var config = EconomyConfigProvider.Load();
            foreach (string workshop in new[] { "atelier", "music" })
            {
                int[] counts = new int[WorkshopItemCatalog.Items.Count];
                Check(WorkshopItemCatalog.TotalWeight(workshop) == config.Rarities.Sum(rarity => rarity.Weight),
                    "Crafting must use the configured rarity weight total");
                for (int ticket = 0; ticket < WorkshopItemCatalog.TotalWeight(workshop); ticket++)
                    counts[WorkshopItemCatalog.Roll(workshop, ticket)]++;
                foreach (var rarity in config.Rarities)
                {
                    int index = WorkshopItemCatalog.Items.Select((item,index)=>(item,index))
                        .Single(entry => entry.item.WorkshopId == workshop && entry.item.RarityId == rarity.Id).index;
                    Check(counts[index] == rarity.Weight && TradeScene.GetItemBasePrice(index) == rarity.BasePrices[workshop] &&
                          TradeScene.GetItemRerollLimit(index) == rarity.Rerolls,
                        "All five workshop rarities must use the configured odds, prices and rerolls");
                }
            }
            Check(TradeScene.GetItemKind(0) == "hat" && TradeScene.GetItemKind(1) == "gloves" &&
                  TradeScene.GetItemKind(2) == "guitar" && TradeScene.GetItemKind(3) == "trumpet",
                "Legacy save indices must keep their original item identity");
            var colors = new System.Collections.Generic.HashSet<Color>();
            for (int index=4;index<10;index++)
            {
                var texture = GD.Load<Texture2D>(TradeScene.GetItemArtPath(index));
                var image = texture.GetImage();
                Check(image.GetWidth() == 128 && image.GetHeight() == 128 && image.GetPixel(0,0).A == 0,
                    "Placeholder icons must be square with transparent padding");
                colors.Add(image.GetPixel(64,64));
            }
            Check(colors.Count == 6, "Every new placeholder must have a distinct color");
            using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":8,\"day_remaining_seconds\":120,\"score\":1000,\"energy_remaining\":2,\"stored_items\":[4,5,6,7,8,9]}");
            game = await Load();
            for(int index=4;index<10;index++)
            {
                var inventory = game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer");
                var slot = inventory.GetChild<SlotUI>(0);
                string art = TradeScene.GetItemArtPath(index);
                Check(game.StoredItems[0] == index && slot.GetNode<TextureRect>("Icon").Texture.ResourcePath == art &&
                      slot.ItemInfo == ExpectedItemTooltip(index),
                    "New placeholder identity, art and tooltip must survive inventory reload");
                SelectInventorySlot(game,0);
                game.PlaceItemInShowcase(0);
                Check(game.GetNode<TextureRect>("World/ShowroomLeft/Showcases/Slot1/Icon").Texture.ResourcePath == art,
                    "Showcases must display the matching colored square");
                game._Process(game.BuyerCountdown+0.001);
                var visible = game.GetNode<Node2D>("World/TradingRoom/Item").GetChildren().OfType<Sprite2D>()
                    .Where(sprite => sprite.Visible).ToArray();
                Check(game.ItemIndex == index && visible.Length == 1 && visible[0].Texture.ResourcePath == art &&
                      game.RerollsRemaining == TradeScene.GetItemRerollLimit(index),
                    "Each colored square must have its own visible trading sprite and rarity rerolls");
                game.RerollCustomer();
                int remaining = game.RerollsRemaining;
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await Load();
                Check(game.TradeActive && game.ItemIndex == index && game.RerollsRemaining == remaining,
                    "Active placeholder trades must preserve identity and spent rerolls across reload");
                int cash = game.Score, offer = game.OfferedPrice;
                Check(offer == TradeScene.PriceForPercent(TradeScene.GetItemBasePrice(index), game.Percentage),
                    "High-rarity offers must use the correct workshop base price");
                game.AcceptTrade();
                Check(game.Score == cash+offer && !game.TradeActive,
                    "Placeholder sales must credit their offer and finish the trade");
            }
        }
        finally
        {
            if(game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            if(FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("All six colored placeholder items, configured rarity odds, trading and reload checks passed");
    }

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

    private static void TestEconomyConfig()
    {
        var config = EconomyConfigProvider.Load();
        Check(config.SchemaVersion == 1 && config.IsProvisional,
            "The game must load the versioned provisional economy config");
        Check(config.Workshops.Count == 2 && config.Rarities.Count == 5 && config.OfferRanges.Count == 3,
            "The game and domain tests must share the complete economy v1 source");
        Check(config.StartingMoney == 220 && config.Workshops[0].SpinCost == 25 &&
              config.Workshops[1].SpinCost == 50 && config.Workshops[1].UnlockCost == 100 &&
		      config.Quotas[0].Amount == 250 &&
              config.Quotas[1].Amount == 450,
            "The runtime must load the provisional paid-spin and raised-quota balance");
    }

    private static string ExpectedItemTooltip(int index) =>
        $"{TradeScene.GetItemName(index)}\nРедкость: {TradeScene.GetItemRarity(index)}\nСтоимость: {TradeScene.GetItemBasePrice(index)}$";

    private static void TestPopupPlacement()
    {
        Rect2 viewport = new(0, 0, 1000, 700);
        Rect2 anchor = new(400, 300, 100, 80);
        Vector2 size = new(200, 120);
        Vector2 right = PopupPlacement.FindPosition(viewport, anchor, size, ReadOnlySpan<Rect2>.Empty);
        Check(right == new Vector2(512, 280), "Unobstructed popups must keep the existing preferred right-hand placement");
        ReadOnlySpan<Rect2> obstacles = stackalloc Rect2[] { new(right, size) };
        Vector2 left = PopupPlacement.FindPosition(viewport, anchor, size, obstacles);
        Check(left == new Vector2(188, 280), "Popups must move to the left when the preferred side is occupied");
        for (int i = 0; i < 32; i++) PopupPlacement.FindPosition(viewport, anchor, size, obstacles);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) PopupPlacement.FindPosition(viewport, anchor, size, obstacles);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Popup geometry must allocate no managed memory in steady state (actual {allocated} bytes)");
        GD.Print("Popup placement: 1000 calculations, 0 managed bytes allocated");
    }

    private async Task TestStandaloneCamera()
    {
        // Deliberately contains no TradeScene: navigation must work with just bounds and a viewport.
        var scene = new Node2D();
        var boundsSource = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://art/rooms/room_layout.jpg"),
            Centered = false, Position = new Vector2(100, 200), Scale = Vector2.One * 4
        };
        scene.AddChild(boundsSource);
        var camera = new WorldCamera
        {
            BoundsSource = boundsSource, Zoom = Vector2.One * 0.7f,
            Position = (boundsSource.Transform * boundsSource.GetRect()).GetCenter()
        };
        scene.AddChild(camera);
        var layer = new CanvasLayer();
        scene.AddChild(layer);
        var button = new Button { Position = new Vector2(12, 12), Size = new Vector2(160, 64), Text = "UI" };
        layer.AddChild(button);
        Root.AddChild(scene);
        camera.SetProcess(false);
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            void Move(Vector2 point, Vector2 relative, MouseButtonMask mask = 0) =>
                Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                    Relative = relative, ButtonMask = mask }, true);
            void Left(Vector2 point, bool pressed) => Root.PushInput(new InputEventMouseButton
            {
                Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left,
                Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0
            }, true);

            Vector2 pointer = Root.GetVisibleRect().Size * new Vector2(0.6f, 0.55f);
            Vector2 screenOffset = pointer - Root.GetVisibleRect().Size / 2;
            Vector2 anchoredWorldPoint = camera.Position + screenOffset / camera.Zoom;
            float zoomBefore = camera.Zoom.X;
            Root.PushInput(new InputEventMouseButton { Position = pointer, GlobalPosition = pointer,
                ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
            Check(camera.Zoom.X == zoomBefore, "Standalone camera wheel input must preserve smooth zoom");
            camera._Process(1d / 60d);
            Check(camera.Zoom.X > zoomBefore &&
                  (camera.Position + screenOffset / camera.Zoom).DistanceTo(anchoredWorldPoint) < 0.01f,
                "The extracted camera must zoom toward the world point under the cursor");

            Vector2 positionBefore = camera.Position;
            Vector2 uiPoint = button.GetGlobalRect().GetCenter();
            Move(uiPoint, Vector2.Zero);
            Left(uiPoint, true);
            Move(uiPoint + new Vector2(40, 0), new Vector2(40, 0), MouseButtonMask.Left);
            Left(uiPoint + new Vector2(40, 0), false);
            Check(camera.Position == positionBefore, "A GUI click/drag must not start world-camera panning");
            Move(pointer, Vector2.Zero);
            Left(pointer, true);
            Move(pointer + new Vector2(40, 0), new Vector2(40, 0), MouseButtonMask.Left);
            Check(camera.Position.X < positionBefore.X, "Dragging empty world space must still pan the standalone camera");

            Paused = true;
            Vector2 pausedPosition = camera.Position;
            Vector2 pausedZoom = camera.Zoom;
            camera._Process(1);
            camera._Input(new InputEventMouseMotion { Relative = new Vector2(100, 100) });
            Check(camera.Position == pausedPosition && camera.Zoom == pausedZoom,
                "Pause must freeze the independent camera's motion and zoom");
            Paused = false;
            Move(pointer + new Vector2(60, 0), new Vector2(20, 0), MouseButtonMask.Left);
            Check(camera.Position == pausedPosition, "Resuming must not revive a mouse drag that began before pause");
            Left(pointer + new Vector2(60, 0), false);
        }
        finally
        {
            Paused = false;
            scene.QueueFree();
        }
        await ToSignal(this, SignalName.ProcessFrame);
        Vector2I originalSize = Root.Size;
        Root.Size = new Vector2I(originalSize.X - 20, originalSize.Y);
        await ToSignal(this, SignalName.ProcessFrame);
        Root.Size = originalSize;
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Standalone camera, zoom anchor, GUI input isolation and pause lifecycle checks passed");
    }

    private async Task TestUiLayoutInvalidation()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
        var camera = game.GetNode<Camera2D>("Camera2D");
        var zoomUi = game.GetNode<ZoomResponsiveUi>("ZoomResponsiveUi");
        camera.Zoom = Vector2.One * 0.7f;
        zoomUi.UpdateForZoom(0.7f);
        var button = game.GetNode<Control>("World/Atelier/GetItemButton");
        var hint = button.GetNode<Control>("PriceHint");
        button.Size += new Vector2(100, 40);
        hint.Size += new Vector2(80, 20);
        var tradePanel = game.GetNode<Control>("World/TradingRoom/Item/TradePanel");
        tradePanel.Size += new Vector2(80, 40);
        for (int i = 0; i < 3; i++) await ToSignal(this, SignalName.ProcessFrame);
        Rect2 hintRect = hint.GetTransform() * new Rect2(Vector2.Zero, hint.Size);
        Check(Mathf.Abs(-hintRect.End.Y * 0.7f - 8) < 0.1f &&
              Mathf.Abs(hintRect.GetCenter().X - button.Size.X / 2) < 0.1f &&
              tradePanel.PivotOffset == tradePanel.Size / 2,
            "Resizing controls at constant zoom must invalidate cached pivots and price-hint placement");

        CraftAtelier(game);
        var inventory = game.GetNode<InventoryUI>("HUD/InventoryUI");
        for (int i = 0; i < 2; i++) await ToSignal(this, SignalName.ProcessFrame);
        Check(!inventory.ItemPopup.IsProcessing(), "Hidden item tooltips must not run a frame callback");
        inventory.Scroller.Size = new Vector2(110, inventory.Scroller.Size.Y);
        for (int i = 0; i < 2; i++) await ToSignal(this, SignalName.ProcessFrame);
        var slot = (SlotUI)inventory.Container.GetChild(1);
        Vector2 point = slot.GetGlobalRect().GetCenter();
        Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        Check(inventory.ItemPopup.Visible && inventory.ItemPopup.IsProcessing(),
            "Showing a tooltip must immediately activate its presentation updates");
        Vector2 oldAnchor = slot.GetGlobalRect().Position;
        inventory.Scroller.ScrollHorizontal = 10;
        for (int i = 0; i < 2; i++) await ToSignal(this, SignalName.ProcessFrame);
        Check(slot.GetGlobalRect().Position.X < oldAnchor.X && inventory.ItemPopup.Visible &&
              Mathf.Abs(inventory.ItemPopup.GetGlobalRect().GetCenter().X - slot.GetGlobalRect().GetCenter().X) < 0.1f,
            "A visible tooltip must follow horizontal scrolling without becoming detached from its item");
        slot.Size += new Vector2(20, 12);
        inventory.Container.Position += new Vector2(12, 0);
        ((ItemInfoPopup)inventory.ItemPopup).Refresh();
        Rect2 popupRect = inventory.ItemPopup.GetGlobalRect();
        Check(Mathf.Abs(slot.GetGlobalRect().Position.Y - popupRect.End.Y - 8) < 0.1f &&
              Mathf.Abs(slot.GetGlobalRect().GetCenter().X - popupRect.GetCenter().X) < 0.1f,
            "Tooltip presentation must follow moved and resized inventory slots");
        game.GetNode<PauseMenu>("PauseMenu").Open();
        Check(!inventory.ItemPopup.Visible && !inventory.ItemPopup.IsProcessing(),
            "Pause must hide the extracted tooltip and stop its processing");
        game.GetNode<PauseMenu>("PauseMenu").Close();
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Constant-zoom layout invalidation and item popup lifecycle checks passed");
    }

    private async Task TestPauseAndZoomUi()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        var ui = game.GetNode<ZoomResponsiveUi>("ZoomResponsiveUi");
        var panel = game.GetNode<Control>("World/TradingRoom/Item/TradePanel");
        var hud = game.GetNode<Control>("HUD/Root/ProgressPanel");
        var headers = new[] { "Atelier/Header", "Atelier/NameLabel", "Atelier/HintLabel",
            "MusicWorkshop/Header", "MusicWorkshop/NameLabel", "MusicWorkshop/HintLabel",
            "Showroom/ShowroomHeader", "Showroom/ShowroomLabel", "Showroom/ShowroomHint",
            "ShowroomLeft/ShowroomHeader", "ShowroomLeft/ShowroomLabel", "ShowroomLeft/ShowroomHint"
            }.Select(path => game.GetNode<CanvasItem>("World/" + path)).ToArray();
        Check(!game.HasNode("World/TradingRoom/SalesRoomLabel"), "The trading room heading must be removed");
        ui.UpdateForZoom(0.23f);
        Check(headers.All(header => !header.Visible && Mathf.IsZeroApprox(header.Modulate.A)),
            "All room titles, descriptions and backplates must disappear at zoom 0.23");
        Check(panel.Scale.X > 1 && panel.Scale.X <= 1.2f && hud.Scale == Vector2.One,
            "Small world UI must grow moderately on zoom-out while HUD stays fixed");
        ui.UpdateForZoom(0.29f);
        Check(headers.All(header => header.Visible && header.Modulate.A > 0 && header.Modulate.A < 1),
            "Room headings must fade in gradually above 0.23");
        ui.UpdateForZoom(0.35f);
        Check(headers.All(header => header.Visible && Mathf.IsEqualApprox(header.Modulate.A, 1)),
            "Room headings must reach full opacity at 0.35");
        ui.UpdateForZoom(0.1f);
        Check(Mathf.IsEqualApprox(panel.Scale.X, 1.2f), "Trade panel zoom compensation must be capped");
        foreach (float zoom in new[] { 1f, 1.8f })
        {
            ui.UpdateForZoom(zoom);
            Check(panel.Scale.X <= 1 && panel.Scale.X * zoom <= 1.08f,
                "Zooming in must keep the trade panel's screen size bounded without cumulative growth");
        }
        foreach (float zoom in new[] { 0.23f, 0.3f, 0.5f, 1f, 1.8f })
        {
            ui.UpdateForZoom(zoom);
            foreach (string room in new[] { "Atelier", "MusicWorkshop" })
            {
                var button = game.GetNode<Control>($"World/{room}/GetItemButton");
                var hint = button.GetNode<Control>("PriceHint");
                Rect2 rect = hint.GetTransform() * new Rect2(Vector2.Zero, hint.Size);
                Check(Mathf.Abs(-rect.End.Y * zoom - 8) < 0.1f &&
                      Mathf.Abs(rect.GetCenter().X - button.Size.X / 2) < 0.1f,
                    "Price hints must remain centered above both buttons with an eight-screen-pixel gap at every zoom");
            }
        }
        Check(TradeScene.GetItemBasePrice(0) == 30 && TradeScene.GetItemBasePrice(1) == 50 &&
              TradeScene.GetItemBasePrice(2) == 45 && TradeScene.GetItemBasePrice(3) == 75,
            "Inventory prices must match the current trade catalog");

        CraftAtelier(game);
        game.SelectInventoryItem(0);
        game.PlaceItemInShowcase(0);
        double countdown = game.BuyerCountdown;
        Root.PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
        Check(Paused && game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
            "Escape must open a modal pause instead of quitting");
        var camera = game.GetNode<Camera2D>("Camera2D");
        Vector2 zoomBefore = camera.Zoom;
        int score = game.Score;
        int day = game.CurrentDay;
        // This HUD position lies outside the centered menu; the modal shade must intercept it.
        await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
        Vector2 position = camera.Position;
        Root.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
        Root.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = false }, true);
        Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = true }, true);
        Root.PushInput(new InputEventMouseMotion { Relative = new Vector2(100, 100) }, true);
        game._Process(5);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(Paused && !game.TradeActive && game.BuyerCountdown == countdown && game.Score == score && game.CurrentDay == day &&
              camera.Zoom == zoomBefore && camera.Position == position,
            "Pause must freeze buyer time, purchases, camera and game hotkeys");
        const string content = "PauseMenu/Root/Overlay/Center/Panel/Content/";
        await ClickControl(game, content + "Menu/SettingsButton");
        Check(Paused && game.GetNode<Control>(content + "Settings").Visible,
            "Settings must open inside pause without resuming gameplay");
        Check(game.GetNode<CheckButton>(content + "Settings/FullscreenButton").IsVisibleInTree(),
            "Pause settings must expose the fullscreen control");
        CheckFullscreenText(game.GetNode<CheckButton>(content + "Settings/FullscreenButton"));
        Root.PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
        Check(Paused && game.GetNode<Control>(content + "Menu").Visible,
            "Escape in settings must return to the pause menu");
        Root.PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
        Check(!Paused && !game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
            "Escape in pause must resume gameplay");
        game._Process(countdown + 0.001);
        Check(game.TradeActive, "Buyer time must continue after resuming");
        int rerolls = game.RerollsRemaining;
        await ClickControl(game, "PauseMenu/Root/PauseButton");
        Check(Paused, "The visible pause button must open the menu with mouse input");
        Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = true }, true);
        Check(game.RerollsRemaining == rerolls, "Pause must block reroll hotkeys during an active trade");
        await ClickControl(game, content + "Menu/ResumeButton");
        Check(!Paused, "Continue must resume the game");
        await ClickControl(game, "PauseMenu/Root/PauseButton");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        Check(!Paused, "Removing a paused game must release its pause");
        GD.Print("Pause, zoom readability and room heading checks passed");
    }

    private async Task TestCollapsibleProgress()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        var panel = game.GetNode<PanelContainer>("HUD/Root/ProgressPanel");
        var content = panel.GetNode<Control>("Content");
        var toggle = game.GetNode<Button>("HUD/Root/ProgressToggleButton");
        var endDay = content.GetNode<Button>("EndDayButton");
        float compactHeight = panel.Size.Y;
        Check(panel.Visible && toggle.Text == "<" && compactHeight < 260 &&
              Mathf.IsEqualApprox(panel.Size.Y, panel.GetCombinedMinimumSize().Y),
            "Progress must open compactly at its content height without blank space left by old buttons");
        Check(Mathf.Abs(panel.GetGlobalRect().End.Y - endDay.GetGlobalRect().End.Y -
                  panel.GetThemeStylebox("panel").GetContentMargin(Side.Bottom)) < 1,
            "The frame must end at the normal padding immediately after End Day");
        int day = game.CurrentDay;
        int score = game.Score;
        for (int i = 0; i < 3; i++)
        {
            await ClickControl(game, "HUD/Root/ProgressToggleButton");
            Check(!panel.IsVisibleInTree() && !endDay.IsVisibleInTree() && toggle.Text == ">" &&
                  toggle.IsVisibleInTree() && Mathf.IsEqualApprox(toggle.Position.X, panel.Position.X),
                "Collapse must hide the entire panel and leave an accessible right arrow at the left edge");
            Check(game.CurrentDay == day && game.Score == score,
                "Collapsing the panel must preserve gameplay state");
            await ClickControl(game, "HUD/Root/ProgressToggleButton");
            Check(panel.IsVisibleInTree() && toggle.Text == "<" &&
                  toggle.GetGlobalRect().Position.X > panel.GetGlobalRect().End.X,
                "Expand must restore the panel with its arrow outside the frame");
        }
        var status = content.GetNode<Label>("StatusLabel");
        string shortStatus = status.Text;
        status.Text = "Не хватает 100$ на квоту. Вернитесь к контрольной точке через меню паузы, чтобы продолжить игру с сохранённого состояния.";
        for (int i = 0; i < 4; i++) await ToSignal(this, SignalName.ProcessFrame);
        Check(panel.Size.Y > compactHeight && panel.GetGlobalRect().Encloses(endDay.GetGlobalRect()) &&
              panel.GetGlobalRect().Encloses(status.GetGlobalRect()),
            "The compact panel must grow when a longer status wraps instead of clipping text or buttons");
        status.Text = shortStatus;
        for (int i = 0; i < 4; i++) await ToSignal(this, SignalName.ProcessFrame);
        Check(Mathf.IsEqualApprox(panel.Size.Y, compactHeight),
            "The panel must shrink again when the longer status clears");
        await ClickControl(game, "HUD/Root/ProgressToggleButton");
        Vector2I originalSize = Root.Size;
        Root.Size = new Vector2I(960, 540);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(game.GetViewportRect().Encloses(toggle.GetGlobalRect()),
            "The collapsed arrow must stay reachable after viewport resize");
        AdvanceDayForTest(game);
        await ClickControl(game, "HUD/Root/ProgressToggleButton");
        Check(game.CurrentDay == day + 1 && content.GetNode<Label>("DayLabel").Text.Contains($"{day + 1}"),
            "Hidden progress must still update and show fresh data after expansion");
        Check(game.GetViewportRect().Encloses(panel.GetGlobalRect()) &&
              game.GetViewportRect().Encloses(toggle.GetGlobalRect()),
            "The expanded panel and collapse arrow must fit after resize");
        Root.Size = originalSize;
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Collapsible progress panel sizing and mouse checks passed");
    }

    private async Task TestRarityPopupGeometry()
    {
        Vector2I originalSize = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
        var camera = game.GetNode<Camera2D>("Camera2D");
        game.TryUnlockMusicWorkshop();
        try
        {
            foreach (Vector2I viewportSize in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                Root.Size = viewportSize;
                await ToSignal(this, SignalName.ProcessFrame);
                foreach (string room in new[] { "Atelier", "MusicWorkshop" })
                {
                    var popup = game.GetNode<RarityPanel>($"World/{room}/RarityPanel");
                    var button = game.GetNode<Control>($"World/{room}/GetItemButton");
                    var hotspot = button.GetNode<Control>("RarityHotspot");
                    var priceHint = button.GetNode<Control>("PriceHint");
                    var layer = popup.GetNode<CanvasLayer>("PopupLayer");
                    Check(layer.Layer > game.GetNode<CanvasLayer>("HUD").Layer &&
                          layer.Layer < game.GetNode<CanvasLayer>("PauseMenu").Layer,
                        "Rarity tables must render above every room background and HUD, below pause");
                    foreach (float zoom in new[] { 0.25f, 0.7f, 1.8f })
                    foreach (Vector2 relativePoint in new[] { new Vector2(0.15f, 0.65f),
                                 new Vector2(0.85f, 0.65f), new Vector2(0.5f, 0.4f) })
                    {
                        camera.Zoom = Vector2.One * zoom;
                        Vector2 viewport = game.GetViewportRect().Size;
                        camera.Position = button.GetGlobalRect().GetCenter() -
                            (viewport * relativePoint - viewport / 2) / zoom;
                        camera.ForceUpdateScroll();
                        hotspot.EmitSignal(Control.SignalName.MouseEntered);
                        await ToSignal(this, SignalName.ProcessFrame);
                        await ToSignal(this, SignalName.ProcessFrame);
                        popup.UpdatePlacement();
                        Rect2 rect = popup.Popup.GetGlobalRect();
                        Check(layer.Visible && game.GetViewportRect().Grow(-11).Encloses(rect),
                            $"The entire {room} table must fit on screen at zoom {zoom}, near {relativePoint}");
                        Check(rect.Size.X <= 500 && rect.Size.Y <= 280 &&
                              popup.Values.GetNode<Label>("Price0").GetThemeFontSize("font_size") * popup.Popup.Scale.X >= 15,
                            "Rarity tables must stay compact with readable screen-space text at every zoom");
                        foreach (Label cell in popup.Values.GetChildren().OfType<Label>())
                            Check(rect.Grow(-18 * popup.Popup.Scale.X).Encloses(cell.GetGlobalRect()) &&
                                  cell.Size.X >= cell.GetMinimumSize().X,
                                "Every heading, rarity and value must fit inside the compact table without clipping");
                        for (int row = 0; row < 6; row++)
                        for (int col = 0; col < 3; col++)
                        {
                            var left = (Label)popup.Values.GetChild(row * 4 + col);
                            var right = (Label)popup.Values.GetChild(row * 4 + col + 1);
                            Check(right.GetGlobalRect().Position.X - left.GetGlobalRect().End.X >= 15 * popup.Popup.Scale.X,
                                "Rarity table columns must retain clear spacing in every popup placement");
                        }
                        Rect2 anchor = button.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, button.Size);
                        // The centered baseline leaves enough space to avoid both the button and every HUD panel.
                        if (zoom == 0.7f && relativePoint == new Vector2(0.5f, 0.4f))
                        {
                            Check(!rect.Intersects(anchor), "The table must sit beside the button without covering it");
                            foreach (string path in new[] { "HUD/Root/ProgressPanel", "HUD/Root/ScorePanel", "HUD/Root/EnergyPanel" })
                                Check(!rect.Intersects(game.GetNode<Control>(path).GetGlobalRect()),
                                    "The table must prefer available space away from HUD panels");
                        }
                        Check(rect.GetCenter().DistanceTo(anchor.GetCenter()) < rect.Size.Length() + anchor.Size.Length(),
                            "The table must stay near the create button rather than move to a fixed screen corner");
                        Check(popup.Values.GetNode<Label>("Price0").Text == (room == "Atelier" ? "30$" : "45$") &&
                              popup.Values.GetNode<Label>("Rarity4").Text == "Неповторимая",
                            "Both tables must preserve workshop-specific prices and all rarity names");
                        hotspot.EmitSignal(Control.SignalName.MouseExited);
                        Check(!layer.Visible, "Leaving the hotspot must hide the entire overlay layer");
                    }
                    camera.Zoom = Vector2.One * 0.7f;
                    camera.Position = button.GetGlobalRect().GetCenter() -
                        new Vector2(0, game.GetViewportRect().Size.Y * 0.1f) / 0.7f;
                    camera.ForceUpdateScroll();
                    Vector2 mainPoint = button.GetGlobalTransformWithCanvas() * (button.Size / 2);
                    Root.PushInput(new InputEventMouseMotion { Position = mainPoint, GlobalPosition = mainPoint }, true);
                    await ToSignal(this, SignalName.ProcessFrame);
                    Check(priceHint.Visible && !layer.Visible,
                        "Hovering the create button must show the price without the rarity table");
                    Vector2 point = hotspot.GetGlobalTransformWithCanvas() * (hotspot.Size / 2);
                    Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    Check(Root.GuiGetHoveredControl() == hotspot && layer.Visible && !priceHint.Visible,
                        "Real pointer hover must show the table without the overlay stealing input");
                    var scorePanel = game.GetNode<Control>("HUD/Root/ScorePanel");
                    Vector2 scorePosition = scorePanel.Position;
                    Vector2 scoreSize = scorePanel.Size;
                    Rect2 oldPopupRect = popup.Popup.GetGlobalRect();
                    scorePanel.GlobalPosition = oldPopupRect.Position;
                    scorePanel.Size = oldPopupRect.Size;
                    popup.UpdatePlacement();
                    Rect2 movedPopupRect = popup.Popup.GetGlobalRect();
                    Check(movedPopupRect.Position != oldPopupRect.Position &&
                          movedPopupRect.Intersection(scorePanel.GetGlobalRect()).Area <
                          oldPopupRect.Intersection(scorePanel.GetGlobalRect()).Area,
                        "Cached HUD references must use fresh geometry to reduce overlap when a HUD obstacle moves over the popup");
                    scorePanel.Position = scorePosition;
                    scorePanel.Size = scoreSize;
                    popup.UpdatePlacement();
                    Root.PushInput(new InputEventMouseMotion { Position = mainPoint, GlobalPosition = mainPoint }, true);
                    await ToSignal(this, SignalName.ProcessFrame);
                    Check(priceHint.Visible && !layer.Visible,
                        "Returning from the question mark to the main button must restore its price tag");
                    Root.PushInput(new InputEventMouseMotion { Position = new Vector2(3, 3), GlobalPosition = new Vector2(3, 3) }, true);
                    await ToSignal(this, SignalName.ProcessFrame);
                    Check(!layer.Visible && !priceHint.Visible, "Leaving the button must hide both hints");
                    Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
                    await ToSignal(this, SignalName.ProcessFrame);
                    Check(layer.Visible && !priceHint.Visible,
                        "Entering the question mark directly must keep the price tag hidden");
                    Root.PushInput(new InputEventMouseMotion { Position = new Vector2(3, 3), GlobalPosition = new Vector2(3, 3) }, true);
                    await ToSignal(this, SignalName.ProcessFrame);
                    Check(!layer.Visible && !priceHint.Visible,
                        "Leaving the question mark directly must not leave a stale price tag visible");
                }
            }
        }
        finally
        {
            game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Compact rarity popup placement, readability and layer checks passed");
    }

    private async Task TestLockedRoomDrag()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var camera = game.GetNode<WorldCamera>("Camera2D");
        camera.SetProcess(false);
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            var overlay = game.GetNode<Control>("World/MusicWorkshop/LockedOverlay");
            foreach (var local in new[] { new Vector2(400, 780), new Vector2(960, 385) })
            {
                camera.Zoom = Vector2.One * 0.7f;
                camera.GlobalPosition = overlay.GetGlobalTransform() * local;
                camera.ClampToBounds();
                camera.ForceUpdateScroll();
                await ToSignal(this, SignalName.ProcessFrame);
                Vector2 point = overlay.GetGlobalTransformWithCanvas() * local;
                Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
                Vector2 before = camera.Position;
                Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                    ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
                Root.PushInput(new InputEventMouseMotion { Position = point + new Vector2(60, 0),
                    GlobalPosition = point + new Vector2(60, 0), Relative = new Vector2(60, 0),
                    ButtonMask = MouseButtonMask.Left }, true);
                Check(camera.Position.X < before.X,
                    "Dragging the locked room surface or title must pan the camera");
                Root.PushInput(new InputEventMouseButton { Position = point + new Vector2(60, 0),
                    ButtonIndex = MouseButton.Left, Pressed = false }, true);
                before = camera.Position;
                Root.PushInput(new InputEventMouseMotion { Position = point, Relative = new Vector2(-60, 0) }, true);
                Check(camera.Position == before && !game.MusicWorkshopUnlocked && game.Score == 220 &&
                      game.StoredItems.Length == 0, "Releasing drag must stop panning without buying or crafting");
            }
            await ClickControl(game, "World/MusicWorkshop/LockedOverlay/UnlockButton");
            Check(game.MusicWorkshopUnlocked && game.Score == 120,
                "The unlock button must still work after dragging the closed room");
        }
        finally { game.QueueFree(); }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestMouseControls()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        var world = game.GetNode<CounterWorld>("World");
        Check(!world.ItemVisible && !game.TradeActive && game.ItemIndex == -1 &&
              !game.GetNode<CanvasItem>("World/TradingRoom/Item").Visible &&
              !game.GetNode<Control>("World/TradingRoom/Item/TradePanel").Visible,
            "A fresh scene must not show a phantom item or trade");

        await ClickControl(game, "World/Atelier/GetItemButton");
        FinishEmbroidery(game);
        Check(game.StoredItems.Length == 3 && game.Score == 130 && game.EnergyRemaining == 1,
            "A mouse click must create three paid atelier items");
        await ClickControl(game, "HUD/InventoryUI/ScrollContainer/HBoxContainer/" +
            game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChild(0).Name);
        Check(game.SelectedInventoryIndex == 0, "Inventory must accept mouse selection");
        var inventoryUi = game.GetNode<InventoryUI>("HUD/InventoryUI");
        Check(inventoryUi.ItemPopup.Visible && inventoryUi.VisibleItemInfo == ExpectedItemTooltip(game.StoredItems[0]),
            "Hovering the inventory image must expose its name, rarity and price");
        var hoveredSlot = (SlotUI)Root.GuiGetHoveredControl();
        Rect2 itemRect = hoveredSlot.GetGlobalRect();
        Rect2 infoRect = inventoryUi.ItemPopup.GetGlobalRect();
        Check(Mathf.Abs(itemRect.Position.Y - infoRect.End.Y - 8) < 1 &&
              Mathf.Abs(itemRect.GetCenter().X - infoRect.GetCenter().X) < 1,
            "Item information must be centered directly above the slot with an eight-pixel gap");
        Check(hoveredSlot.TooltipText == "", "The delayed system tooltip must stay disabled");
        await ClickControl(game, "World/ShowroomLeft/Showcases/Slot1");
        Check(!inventoryUi.ItemPopup.Visible, "Leaving the inventory must hide item information");
        await ToSignal(this, SignalName.ProcessFrame);
        Check(!game.TradeActive, "Mouse placement must wait for a buyer");
        game._Process(game.BuyerCountdown + 0.001);
        Check(game.TradeActive && game.StoredItems.Length == 2 && world.ItemVisible,
            "Placing an existing item with the mouse must start a real trade");
        int rerolls = game.RerollsRemaining;
        await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton");
        Check(game.RerollsRemaining == rerolls - 1, "The reroll button must accept mouse input");
        int score = game.Score;
        int offer = game.OfferedPrice;
        await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
        Check(!game.TradeActive && game.Score == score + offer && !world.ItemVisible,
            "The accept button must sell once and hide the trade");
        await ClickControl(game, "World/Atelier/GetItemButton");
        FinishEmbroidery(game);
        var inventory = game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer");
        await ClickControl(game, "HUD/InventoryUI/ScrollContainer/HBoxContainer/" + inventory.GetChild(0).Name);
        await ClickControl(game, "World/Showroom/Showcases/Slot4");
        await ToSignal(this, SignalName.ProcessFrame);
        game._Process(game.BuyerCountdown + 0.001);
        Check(game.TradeActive && game.StoredItems.Length == 4,
            "The second showroom must also place a selected item with the mouse");
        await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
        Check(game.StoredItems.Length == 4, "Unsold goods must remain in inventory without disposal controls");
        await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
        Check(game.DayClosed && game.CurrentDay == 1 && game.EnergyRemaining == 0,
            "End day must close the shop and discard unused energy with mouse input");
        await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
        Check(game.CurrentDay == 2 && !game.DayClosed, "The next day must start with a separate mouse click");
        await ClickControl(game, "DayTransitionCard/Root/Center/Card/Content/Choices/Cash/Content/Choose");
        await ClickControl(game, "PauseMenu/Root/PauseButton");
        await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/CheckpointButton");
        Check(game.CurrentDay == 1 && game.Score == 220, "Checkpoint handler must be connected");
        await ClickControl(game, "World/MusicWorkshop/LockedOverlay/UnlockButton");
        Check(game.MusicWorkshopUnlocked, "Unlock must accept mouse input");
        game.Score = 140;
        game.RefreshItemButtons();
        await ClickControl(game, "World/MusicWorkshop/GetItemButton");
        FinishMusic(game);
        Check(game.Score == 0 && game.StoredItems.Length == 3 && game.EnergyRemaining == 1,
            "The unlocked music workshop must accept mouse input and charge its spin price");
        await ClickControl(game, "PauseMenu/Root/PauseButton");
        await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/NewGameButton");
        Check(!game.MusicWorkshopUnlocked && game.Score == 220 && game.StoredItems.Length == 0,
            "New game handler must reset the state");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Mouse controls and phantom trade regression checks passed");
    }

    private async Task ClickControl(Node game, string path)
    {
        var control = game.GetNode<Control>(path);
        if (path.StartsWith("World/", StringComparison.Ordinal))
        {
            game.GetNode<Camera2D>("Camera2D").Position = control.GetGlobalRect().GetCenter();
            game.GetNode<WorldCamera>("Camera2D").ClampToBounds();
        }
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(control.IsVisibleInTree(), $"Mouse target must be visible: {path}");
        Vector2 point = control.GetGlobalTransformWithCanvas() * (control.Size / 2);
        Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        if (control is SlotUI slot)
        {
            var inventoryUi = game.GetNode<InventoryUI>("HUD/InventoryUI");
            Check(inventoryUi.ItemPopup.Visible && inventoryUi.VisibleItemInfo == slot.ItemInfo,
                "Item information must appear synchronously on mouse enter without waiting for a timer or another frame");
        }
        await ToSignal(this, SignalName.ProcessFrame);
        Root.PushInput(new InputEventMouseButton
        {
            Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left,
            Pressed = true, ButtonMask = MouseButtonMask.Left
        }, true);
        Root.PushInput(new InputEventMouseButton
        {
            Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false
        }, true);
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestInventoryTransfers()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
        game.Score = 10000;
        CraftAtelier(game);
        CraftAtelier(game);
        AdvanceDayForTest(game);
        CraftAtelier(game);
        await ToSignal(this, SignalName.ProcessFrame);
        var inventory = game.GetNode<InventoryUI>("HUD/InventoryUI");
        var first = game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1");
        var second = game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot2");
        int count = game.StoredItems.Length;
        int item = game.StoredItems[0];

        async Task<Variant> DragTo(Control target, bool replay = false, Variant oldData = default,
            float zoom = 0.7f, string previewArt = null)
        {
            var spriteScales = game.FindChildren("*", "Sprite2D", true, false).OfType<Sprite2D>()
                .ToDictionary(sprite => sprite, sprite => sprite.Scale);
            Vector2 showcaseIconSize = first.GetNode<Control>("Icon").Size;
            var camera = game.GetNode<Camera2D>("Camera2D");
            camera.Zoom = Vector2.One * zoom;
            camera.Position = target == null ? first.GetGlobalRect().GetCenter() : target.GetGlobalRect().GetCenter();
            camera.ForceUpdateScroll();
            await ToSignal(this, SignalName.ProcessFrame);
            var slot = (SlotUI)inventory.Container.GetChild(0);
            var originalTexture = slot.GetNode<TextureRect>("Icon").Texture;
            if (previewArt != null) slot.SetItemTexture(GD.Load<Texture2D>(previewArt));
            Vector2 start = slot.GetGlobalTransformWithCanvas() * (slot.Size / 2);
            Root.PushInput(new InputEventMouseMotion { Position = start, GlobalPosition = start }, true);
            Root.PushInput(new InputEventMouseButton { Position = start, GlobalPosition = start,
                ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
            if (replay) slot.ForceDrag(oldData, new Control());
            else
            {
                Vector2 moved = start + new Vector2(0, -80);
                Root.PushInput(new InputEventMouseMotion { Position = moved, GlobalPosition = moved,
                    Relative = new Vector2(0, -80), ButtonMask = MouseButtonMask.Left }, true);
            }
            Check(Root.GuiIsDragging(), $"Holding and moving an inventory item must start native drag-and-drop (start={start}, hovered={Root.GuiGetHoveredControl()?.GetPath()}, threshold={Root.GuiDragThreshold})");
            if (!replay)
            {
                var preview = (Control)Root.FindChild("InventoryDragPreview", true, false);
                Check(preview != null && preview.Size == new Vector2(50, 50),
                    $"Drag preview must remain 50x50 instead of expanding to source texture size (actual={preview?.Size})");
                var image = preview.GetNode<TextureRect>("Icon");
                Check(image.Texture == slot.GetNode<TextureRect>("Icon").Texture &&
                      image.GetGlobalRect().Size == new Vector2(50, 50),
                    "The dragged item's artwork must fit the fixed screen-space preview bounds");
                Check(image.GetGlobalRect().GetCenter().DistanceTo(start + new Vector2(0, -80)) < 0.1f,
                    "The cursor must sit at the center of the dragged artwork when dragging begins");
                Check(!slot.GetNode<TextureRect>("Icon").Visible && slot.SelfModulate == Colors.White,
                    "The source slot must look empty and unselected while its item is being dragged");
                await ToSignal(this, SignalName.ProcessFrame);
                Check(preview.GetGlobalRect().Size == new Vector2(50, 50) && image.GetGlobalRect().Size == new Vector2(50, 50),
                    "Preview and artwork bounds must remain fixed after layout regardless of camera zoom");
            }
            Check(!inventory.ItemPopup.Visible, "Dragging must hide the item information popup");
            Variant data = Root.GuiGetDragData();
            Vector2 end = target == null ? new Vector2(5, 400) :
                target.GetGlobalTransformWithCanvas() * (target.Size / 2);
            Root.PushInput(new InputEventMouseMotion { Position = end, GlobalPosition = end,
                Relative = end - start, ButtonMask = MouseButtonMask.Left }, true);
            if (!replay)
            {
                var preview = (Control)Root.FindChild("InventoryDragPreview", true, false);
                Check(preview.GetNode<Control>("Icon").GetGlobalRect().GetCenter().DistanceTo(end) < 0.1f,
                    "The artwork must remain centered on the cursor after moving to the drop target");
            }
            Root.PushInput(new InputEventMouseButton { Position = end, GlobalPosition = end,
                ButtonIndex = MouseButton.Left, Pressed = false }, true);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(Root.FindChild("InventoryDragPreview", true, false) == null,
                "Drop and cancellation must remove the drag preview");
            if (IsInstanceValid(slot) && !slot.IsQueuedForDeletion())
                Check(slot.GetNode<TextureRect>("Icon").Visible,
                    "Ending the drag must reveal the current source-slot image on both cancellation and successful placement");
            Check(spriteScales.All(entry => entry.Key.Scale == entry.Value) &&
                  first.GetNode<Control>("Icon").Size == showcaseIconSize,
                "Dragging and dropping must preserve all world sprite scales and showcase icon bounds");
            if (previewArt != null) slot.SetItemTexture(originalTexture);
            return data;
        }

        foreach (float zoom in new[] { 0.25f, 1.8f })
        foreach (int itemIndex in new[] { 0, 1 })
            await DragTo(null, zoom: zoom, previewArt: TradeScene.GetItemArtPath(itemIndex));
        await DragTo(null);
        Check(game.StoredItems.Length == count && game.ShowcaseItems.All(value => value == -1),
            "Releasing outside showcases must cancel without losing an item");
        Variant payload = await DragTo(first);
        Check(game.StoredItems.Length == count - 1 && game.ShowcaseItems[0] == item,
            "A real GUI drag must move exactly the dragged item to its target showcase");
        await DragTo(second, true, payload);
        Check(game.StoredItems.Length == count - 1 && game.ShowcaseItems[1] == -1,
            "Replaying an old drag must not move the next item or duplicate the transferred item");
        await DragTo(first);
        Check(game.StoredItems.Length == count - 1 && game.ShowcaseItems[0] == item,
            "Dropping on an occupied showcase must leave both items untouched");
        game._Process(game.BuyerCountdown + 0.01);
        Check(game.TradeActive, "The displayed item must become reserved for a trade");
        await DragTo(first);
        Check(game.StoredItems.Length == count - 2 && game.ShowcaseItems[0] >= 0 && game.ItemIndex == item,
            "Dropping on the freed showcase must stock it without replacing the item being sold");

        async Task ShiftClick()
        {
            var slot = (SlotUI)inventory.Container.GetChild(0);
            Vector2 point = slot.GetGlobalTransformWithCanvas() * (slot.Size / 2);
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = true, ShiftPressed = true, ButtonMask = MouseButtonMask.Left }, true);
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = false, ShiftPressed = true }, true);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        int next = game.StoredItems[0];
        await ShiftClick();
        Check(game.StoredItems.Length == count - 3 && game.ShowcaseItems[1] == next,
            "Shift-click must move to the first available showcase during a trade");
        for (int i = 0; i < 4; i++) await ShiftClick();
        Check(game.ShowcaseItems.Count(value => value >= 0) == 6 && game.StoredItems.Length == count - 7,
            "Repeated Shift-clicks must fill both showrooms without duplicate transfers");
        int[] before = game.StoredItems;
        await ShiftClick();
        Check(game.StoredItems.SequenceEqual(before), "Shift-click with no free showcases must keep inventory intact");
        game.GetNode<PauseMenu>("PauseMenu").Open();
        Check(((SlotUI)inventory.Container.GetChild(0)).CreateDragData(0).VariantType == Variant.Type.Nil,
            "Pause must reject new drag payloads");
        ((SlotUI)inventory.Container.GetChild(0)).QuickTransfer(0);
        Check(game.StoredItems.SequenceEqual(before), "Pause must also reject quick transfers");
        game.GetNode<PauseMenu>("PauseMenu").Close();
        game.NewGame();
        CraftAtelier(game);
        game.Score = 0;
        for (int i = 0; i < 4 && !game.Failed; i++) AdvanceDayForTest(game);
        before = game.StoredItems;
        Check(game.Failed && before.Length == 3, "Terminal transfer checks need a failed game with remaining items");
        var remainingSlot = (SlotUI)inventory.Container.GetChild(0);
        Check(remainingSlot.CreateDragData(0).VariantType == Variant.Type.Nil,
            "A terminal game must reject item drags");
        remainingSlot.QuickTransfer(0);
        Check(game.StoredItems.SequenceEqual(before) && game.ShowcaseItems.All(value => value == -1),
            "A terminal game must reject quick transfers without losing items");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Inventory drag/drop, cancellation, stale payload and Shift transfer checks passed");
    }

    private async Task TestTradeScene()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn")
            ?? throw new Exception("The trade scene must load");
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);

        var world = game.GetNode<CounterWorld>("World");
        var wall = game.GetNode<ColorRect>("World/Wall");
        Check(wall.Size == new Vector2(5760, 3240), "The world bounds must contain the full room-layout artwork");
        var roomLayout = game.GetNode<Sprite2D>("World/RoomLayout");
        Check(roomLayout.Texture?.GetSize() == new Vector2(2560, 1440) &&
              roomLayout.Scale == new Vector2(2.25f, 2.25f) && roomLayout.Position == Vector2.Zero &&
              wall.ZIndex < roomLayout.ZIndex,
            "The approved room scheme must form the architectural background");
        Check(game.GetNode<Node2D>("World/Atelier").Position == new Vector2(304, 535) &&
              game.GetNode<Node2D>("World/Corridor").Position == new Vector2(2224, 531) &&
              game.GetNode<Node2D>("World/MusicWorkshop").Position == new Vector2(3622, 535) &&
              game.GetNode<Node2D>("World/ShowroomLeft").Position == new Vector2(304, 1629) &&
              game.GetNode<Node2D>("World/TradingRoom").Position == new Vector2(2224, 1625) &&
              game.GetNode<Node2D>("World/Showroom").Position == new Vector2(3622, 1629),
            "Room roots must align to the six spaces in the approved scheme");
        var salesRoom = game.GetNode<Sprite2D>("World/TradingRoom/TradingRoomBackground");
        var roomTexture = salesRoom.Texture ?? throw new Exception("The room background must load");
        Check(roomTexture.GetSize() == new Vector2(1398, 1080) && salesRoom.GlobalPosition == new Vector2(2227, 1629),
            "The cashier art must occupy the lower central room");
        Check(game.GetNode<Sprite2D>("World/Atelier/Background").Texture?.GetSize() == new Vector2(1920, 1080),
            "The atelier must use the approved full-room art");
        var showroom = game.GetNode<Sprite2D>("World/Showroom/ShowroomBackground");
        Check(showroom.Texture != null && showroom.GlobalPosition == new Vector2(3622, 1629) &&
              wall.ZIndex < showroom.ZIndex && showroom.ZIndex < game.GetNode<Control>("World/Showroom/ShowroomHeader").ZIndex,
            "The showroom background must fill the lower-right room behind its controls");
        foreach (string backgroundPath in new[]
        {
            "World/Atelier/Background", "World/MusicWorkshop/Background",
            "World/TradingRoom/TradingRoomBackground", "World/Showroom/ShowroomBackground"
        })
            CheckSpriteInside(wall.GetGlobalRect(), game.GetNode<Sprite2D>(backgroundPath),
                $"Room background must stay inside world bounds: {backgroundPath}");
        var leftShowcases = game.GetNode<Node2D>("World/ShowroomLeft/Showcases");
        var rightShowcases = game.GetNode<Node2D>("World/Showroom/Showcases");
        Check(leftShowcases.GetChildCount() == 3 && rightShowcases.GetChildCount() == 3,
            "The six showcases must be split evenly between both showroom floors");
        foreach (Button slot in leftShowcases.GetChildren().Concat(rightShowcases.GetChildren()).OfType<Button>())
        {
            Check(slot.Size == new Vector2(440, 230) &&
                  slot.Position.X >= 180 && slot.Position.X + slot.Size.X <= 1700 &&
                  slot.Position.Y >= 640 && slot.Position.Y + slot.Size.Y <= 920,
                "Full-size showcase furniture must form a centered row low in the floor zone");
            Check(slot.GetNode<Label>("ItemLabel").Position.Y < 0,
                "Showcase state text must sit above the furniture");
            Check(!slot.GetNode<ColorRect>("Glass").Visible && !slot.HasNode("Glare"),
                "Redundant glass and glare decoration must stay out of the simplified showcase");
            var normalStyle = (StyleBoxFlat)slot.GetThemeStylebox("normal");
            Check(normalStyle.BgColor.A == 0 && normalStyle.BorderWidthLeft == 0 &&
                  normalStyle.BorderWidthTop == 0 && normalStyle.BorderWidthRight == 0 &&
                  normalStyle.BorderWidthBottom == 0,
                "Showcase controls must not render a dark rectangular frame");
            var hoverStyle = (StyleBoxFlat)slot.GetThemeStylebox("hover");
            Check(hoverStyle.BgColor.A == 0 && hoverStyle.BorderWidthLeft == 0 &&
                  hoverStyle.BorderWidthTop == 0 && hoverStyle.BorderWidthRight == 0 &&
                  hoverStyle.BorderWidthBottom == 0,
                "Hovering a showcase slot must not highlight its background or border");
            foreach (string state in new[] { "pressed", "hover_pressed" })
            {
                var pressedStyle = (StyleBoxFlat)slot.GetThemeStylebox(state);
                Check(pressedStyle.BgColor.A == 0 && pressedStyle.BorderWidthLeft == 0 &&
                      pressedStyle.BorderWidthTop == 0 && pressedStyle.BorderWidthRight == 0 &&
                      pressedStyle.BorderWidthBottom == 0,
                    $"Pressing a showcase slot must not highlight its background or border ({state})");
            }
        }
        var atelierButton = game.GetNode<Button>("World/Atelier/GetItemButton");
        var musicButton = game.GetNode<Button>("World/MusicWorkshop/GetItemButton");
        Check(atelierButton.GetThemeStylebox("normal") is StyleBoxTexture &&
              musicButton.GetThemeStylebox("normal") is StyleBoxTexture,
            "Both workshops must use the art team's create-item button");
        var atelierRarityPanel = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
        var musicRarityPanel = game.GetNode<RarityPanel>("World/MusicWorkshop/RarityPanel");
        Check(!atelierRarityPanel.Visible && atelierRarityPanel.Values.GetChildCount() == 24,
            "The compact rarity table must provide four headings and five complete rows");
        Check(atelierRarityPanel.Price0 == 30 && atelierRarityPanel.Price1 == 50 &&
              atelierRarityPanel.Price2 == 80 && atelierRarityPanel.Price3 == 120 &&
              atelierRarityPanel.Price4 == 200 && musicRarityPanel.Price0 == 45 &&
              musicRarityPanel.Price1 == 75 && musicRarityPanel.Price2 == 120 &&
              musicRarityPanel.Price3 == 180 && musicRarityPanel.Price4 == 300,
            "Rarity panels must use the atelier and music prices from the balance document");
		var atelierPriceHint = atelierButton.GetNode<PanelContainer>("PriceHint");
		var musicPriceHint = musicButton.GetNode<PanelContainer>("PriceHint");
		Check(!atelierPriceHint.Visible && !musicPriceHint.Visible &&
		      atelierPriceHint.GetNode<Label>("Label").Text.Contains("90$") &&
		      musicPriceHint.GetNode<Label>("Label").Text.Contains("140$") &&
		      game.GetNode<Label>("World/Atelier/HintLabel").Text.Contains("90$") &&
		      game.GetNode<Label>("World/MusicWorkshop/HintLabel").Text.Contains("140$"),
		    "Each workshop must show its spin price in the room header and prepare a custom hover price tag");
        atelierButton.EmitSignal(Control.SignalName.MouseEntered);
        Check(atelierPriceHint.Visible && !atelierRarityPanel.Visible,
		    "Hovering the main create area must show only the custom price tag");
		atelierButton.EmitSignal(Control.SignalName.MouseExited);
		Check(!atelierPriceHint.Visible, "Leaving the create button must hide its price tag");
        var rarityHotspot = atelierButton.GetNode<Control>("RarityHotspot");
        Check(rarityHotspot.Size.X <= 72 && rarityHotspot.Size.Y <= 68 && rarityHotspot.TooltipText.Length == 0,
            "The rarity hover target must cover only the drawn question-mark circle without a system tooltip");
        foreach (var panel in new[] { atelierRarityPanel, musicRarityPanel })
            foreach (Label value in panel.Values.GetChildren().OfType<Label>())
                Check(value.GetThemeColor("font_color") == Colors.Black,
                    $"Every rarity value must be black: {panel.GetParent().Name}/{value.Name}");
        rarityHotspot.EmitSignal(Control.SignalName.MouseEntered);
        Check(atelierRarityPanel.Visible, "Hovering the question-mark circle must reveal rarity information");
        rarityHotspot.EmitSignal(Control.SignalName.MouseExited);
        Check(!atelierRarityPanel.Visible, "Leaving the question-mark circle must hide rarity information");
        foreach (string room in new[] { "ShowroomLeft", "Showroom" })
            Check(!game.HasNode($"World/{room}/PlaceItemButton") && !game.HasNode($"World/{room}/TrashBin"),
                "The game must initialize without automatic placement or disposal controls");
        foreach (string kind in new[] { "Hat", "Gloves", "Guitar", "Trumpet" })
            Check(game.GetNode<Sprite2D>($"World/TradingRoom/Item/{kind}").Texture != null,
                $"The {kind} item must have an illustration");
        Check(TradeScene.GetItemArtPath(0).EndsWith("hat_cloche.png") &&
              TradeScene.GetItemArtPath(1).EndsWith("gloves.png"),
            "Atelier items must use the transparent-background PNG artwork");
        Check(TradeScene.GetItemRarity(0) == "Ширпотреб" && TradeScene.GetItemRerollLimit(0) == 3 &&
              TradeScene.GetItemRarity(1) == "Заурядная" && TradeScene.GetItemRerollLimit(1) == 2 &&
              TradeScene.GetItemRerollLimit(2) == 3 && TradeScene.GetItemRerollLimit(3) == 2 &&
              TradeScene.GetRerollsForRarity("first_class") == 2 &&
              TradeScene.GetRerollsForRarity("exclusive") == 1 &&
              TradeScene.GetRerollsForRarity("unique") == 1,
            "Every prototype item must derive its reroll limit from its configured rarity");
		var musicOverlay = game.GetNode<ColorRect>("World/MusicWorkshop/LockedOverlay");
		var musicUnlockButton = musicOverlay.GetNode<Button>("UnlockButton");
        Check(!atelierButton.Disabled && musicButton.Disabled && musicOverlay.Visible &&
		      !musicUnlockButton.Disabled && !game.MusicWorkshopUnlocked && game.Score == 220 &&
		      game.EnergyRemaining == 2,
            "A new game must start with two energy and the music workshop covered by its lock overlay");
		CraftMusic(game);
		Check(game.StoredItems.Length == 0 && game.Score == 220,
		    "The locked music workshop must not create or charge for items");
		musicUnlockButton.EmitSignal(Button.SignalName.Pressed);
		Check(game.MusicWorkshopUnlocked && !musicOverlay.Visible && game.Score == 120 &&
		      musicButton.Disabled,
		    "Buying the music workshop must cost 100 dollars and reveal the room");
		var scorePanel = game.GetNode<PanelContainer>("HUD/Root/ScorePanel");
		var energyPanel = game.GetNode<PanelContainer>("HUD/Root/EnergyPanel");
        Check(energyPanel.GetNode<Label>("EnergyLabel").Text == "Энергия: 2 / 2" &&
		      energyPanel.GetThemeStylebox("panel") is StyleBoxTexture &&
		      scorePanel.GetThemeStylebox("panel") is StyleBoxTexture &&
		      energyPanel.Position.Y > scorePanel.Position.Y + scorePanel.Size.Y,
            "Energy must use its own matching HUD panel below the score");
        Check(game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").Disabled &&
              game.GetNode<Button>("World/Showroom/Showcases/Slot4").Disabled &&
              !game.TradeActive && game.StoredItems.Length == 0,
            "A new game must start without a free item to sell");
        Check(game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChildCount() == 0,
            "An empty inventory must not render placeholder slots");

        game.Score = 5000;
        for (int day = 0; day < 4; day++)
        {
            atelierButton.EmitSignal(Button.SignalName.Pressed);
            FinishEmbroidery(game);
            atelierButton.EmitSignal(Button.SignalName.Pressed);
            FinishEmbroidery(game);
            AdvanceDayForTest(game);
        }
        var inventoryScroll = game.GetNode<ScrollContainer>("HUD/InventoryUI/ScrollContainer");
        await ToSignal(this, SignalName.ProcessFrame);
        Check(game.StoredItems.Length == 24 &&
              game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChildCount() == 24,
            "Prototype inventory and UI must not restore the old six-item limit");
        inventoryScroll.EmitSignal(Control.SignalName.GuiInput,
            new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true });
        Check(inventoryScroll.ScrollHorizontal > 0,
            "Mouse wheel over an overflowing inventory must scroll it horizontally");
        game.NewGame();
        await ToSignal(this, SignalName.ProcessFrame);
        Check(game.StoredItems.Length == 0 && game.Score == 220 && game.EnergyRemaining == 2 &&
              game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChildCount() == 0,
            "New game must restore cash and energy and remove all dynamic inventory slots");

        var customer = game.GetNode<ModularCustomer>("World/TradingRoom/Customer");
        Check(customer.GetAppearanceCount() == 18, "The customer must combine two bodies, three outfits and three hairstyles");
        foreach (string bodyName in new[] { "Male", "Female" })
        {
            Check(customer.GetNode<Sprite2D>($"{bodyName}/Body").Texture != null, "Each body needs art");
            Node2D clothes = customer.GetNode<Node2D>($"{bodyName}/Clothes");
            Check(clothes.GetChildCount() == 3 && clothes.GetChildren().All(node =>
                    node is Sprite2D { Texture: not null }),
                "Each body needs three outfit textures");
            Check(customer.GetNode($"{bodyName}/Hair").GetChildCount() == 3, "Each body needs three hairstyles");
        }
        for (int appearance = 0; appearance < customer.GetAppearanceCount(); appearance++)
        {
            customer.SetAppearance(appearance);
            int visibleBodies = customer.GetChildren().Count(node => node is Node2D { Visible: true });
            Check(visibleBodies == 1, "Every appearance must select exactly one body");
            Node2D visibleBody = customer.GetChildren().OfType<Node2D>().Single(node => node.Visible);
            var tradePanel = game.GetNode<Control>("World/TradingRoom/Item/TradePanel");
            foreach (float zoom in new[] { 0.25f, 0.7f, 1f, 1.8f })
            {
                game.GetNode<ZoomResponsiveUi>("ZoomResponsiveUi").UpdateForZoom(zoom);
                var room = game.GetNode<Node2D>("World/TradingRoom");
                Rect2 registerBounds = room.GlobalTransform * new Rect2(450, 490, 170, 170);
                Check(!tradePanel.GetGlobalRect().Intersects(registerBounds) &&
                      tradePanel.Size.X <= 320 && tradePanel.Size.Y <= 210,
                    "The compact central trade panel must leave the cash register visible");
                foreach (var itemSprite in game.GetNode("World/TradingRoom/Item").GetChildren().OfType<Sprite2D>())
                    Check(!tradePanel.GetGlobalRect().Intersects(itemSprite.GlobalTransform * itemSprite.GetRect()),
                        "The central trade panel must leave every sale item visible");
                foreach (var sprite in visibleBody.FindChildren("*", "Sprite2D", true, false).Cast<Sprite2D>().Where(s => s.Visible))
                {
                    Rect2 bounds = sprite.GlobalTransform * sprite.GetRect();
                    Check(!tradePanel.GetGlobalRect().Intersects(bounds),
                        "The trade panel must not cover any body, outfit or hairstyle at supported zoom levels");
                }
            }
            Check(visibleBody.GetNode<Node2D>("Clothes").GetChildren().Count(node =>
                      node is CanvasItem { Visible: true }) == 1 &&
                  visibleBody.GetNode<Node2D>("Hair").GetChildren().Count(node =>
                      node is CanvasItem { Visible: true }) == 1,
                "Every appearance must select exactly one outfit and hairstyle");
        }

        var rKey = new InputEventKey { Keycode = Key.R, Pressed = true };
        for (int i = 0; i < 20; i++) game._Input(rKey);
        Check(game.StoredItems.Length == 0 && !game.TradeActive, "R must never create an item or a trade");

		game.Score = 1000;
		game.RefreshItemButtons();
		game.TryUnlockMusicWorkshop();
        atelierButton.EmitSignal(Button.SignalName.Pressed);
            FinishEmbroidery(game);
        musicButton.EmitSignal(Button.SignalName.Pressed);
        FinishMusic(game);
        Check(game.StoredItems.Length == 6 && game.StoredItems.Take(3).All(index => WorkshopItemCatalog.Items[index].WorkshopId == "atelier") &&
              game.StoredItems.Skip(3).All(index => WorkshopItemCatalog.Items[index].WorkshopId == "music") && game.Score == 670 &&
              game.EnergyRemaining == 0 && atelierButton.Disabled && musicButton.Disabled,
            "Unlocking and two paid spins must consume 330 dollars and both energy, producing three category items each");
        CraftAtelier(game);
        Check(game.StoredItems.Length == 6 && game.Score == 670,
            "A third spin in one day must not spend money or create items");
        game.EnergyRemaining = 20;
        game.Score = 10000;
        game.RefreshItemButtons();
        await ToSignal(this, SignalName.ProcessFrame);
        Check(game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChildCount() == 6,
            "Inventory UI must render exactly one slot per retained real item");
        CheckInventoryTooltips(game);
        int firstItem = game.StoredItems[0];
        int[] remainingStored = game.StoredItems.Skip(1).ToArray();
        var firstSlot = game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1");
        Check(firstSlot.Disabled, "Placing requires an inventory selection");
        game.PlaceItemInShowcase(0);
        Check(game.StoredItems.Length == 6 && game.ShowcaseItems.All(item => item == -1),
            "Manual placement without an inventory selection must leave goods untouched");
        SelectInventorySlot(game, 0);
        Check(!firstSlot.Disabled, "Placing must become available after selecting an item");
        firstSlot.EmitSignal(Button.SignalName.Pressed);
        Check(!game.TradeActive && game.ShowcaseItems[0] == firstItem &&
              game.StoredItems.SequenceEqual(remainingStored),
            "Placing must move the selected item into a showcase");
        CheckInventoryTooltips(game);
        Check(game.GetNode<TextureRect>("World/ShowroomLeft/Showcases/Slot1/Icon").Texture != null,
            "The showcase must display the item's illustration");
        double firstDelay = game.BuyerCountdown;
        Check(firstDelay is >= 0.5 and <= 2, "First buyer delay must be between 0.5 and 2 seconds");
        game._Process(0.49);
        Check(!game.TradeActive, "A buyer must not arrive before the minimum delay");
        game._Process(firstDelay - 0.491);
        Check(!game.TradeActive, "A buyer must wait until the sampled delay expires");
        game._Process(0.002);
        Check(game.TradeActive && game.ItemIndex == firstItem && game.ShowcaseItems.All(item => item == -1),
            "A buyer must choose a displayed item when the delay expires");
        Check(world.ItemVisible &&
              game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").Disabled &&
              game.GetNode<Label>("World/ShowroomLeft/Showcases/Slot1/ItemLabel").Text == "СВОБОДНО",
            "The active item's showcase must be empty and require a selection for restocking");
        CraftAtelier(game);
        SelectInventorySlot(game, 1);
        Check(!firstSlot.Disabled, "The freed showcase must accept another selected item during the trade");
        var secondSlot = game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot2");
        Check(!secondSlot.Disabled, "Another showcase must remain available during a trade");
        secondSlot.EmitSignal(Button.SignalName.Pressed);
        remainingStored = game.StoredItems;
        Check(game.TradeActive && game.ShowcaseItems[1] >= 0 && remainingStored.Length == 7,
            "The player must be able to stock another showcase during a trade");
        int displayedItem = game.ShowcaseItems[1];
        SelectInventorySlot(game, 0);
        secondSlot.EmitSignal(Button.SignalName.Pressed);
        Check(game.TradeActive && game.ShowcaseItems[1] == displayedItem &&
              game.StoredItems.SequenceEqual(remainingStored) &&
              !game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").Disabled,
            "Clicking an occupied showcase must neither replace nor discard goods");
        foreach (string kind in new[] { "hat", "gloves", "guitar", "trumpet" })
            Check(game.GetNode<Sprite2D>($"World/TradingRoom/Item/{char.ToUpperInvariant(kind[0])}{kind[1..]}").Visible ==
                  (kind == TradeScene.GetItemKind(firstItem)), "Only the selected item illustration must be visible");
        Check(game.Percentage is >= -30 and <= 30, "Offer percentage is out of range");
        Check(game.OfferedPrice == TradeScene.PriceForPercent(TradeScene.GetItemBasePrice(firstItem), game.Percentage),
            "Offer price does not match the percentage");
        Check(game.GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/OfferLabel").Text == $"Получим: {game.OfferedPrice}$",
            "The panel must show the amount received");

		var redRerollButton = game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton");
		int rarityRerolls = TradeScene.GetItemRerollLimit(firstItem);
		Check(!game.HasNode("World/TradingRoom/Item/TradePanel/Content/RerollButton") &&
		      redRerollButton.Text == $"× ({rarityRerolls})" &&
		      game.RerollsRemaining == rarityRerolls,
		    "The red button must be the only reroll control and show the current rarity limit");
        game.Percentage = -30;
        game.ShowTrade();
        for (int remaining = rarityRerolls - 1; remaining > 0; remaining--)
        {
            int oldCustomer = game.CustomerIndex;
            int oldPrice = game.OfferedPrice;
            redRerollButton.EmitSignal(Button.SignalName.Pressed);
            Check(game.ItemIndex == firstItem && game.CustomerIndex != oldCustomer, "Reroll must keep the item and replace the buyer");
            Check(game.TradeActive && game.OfferedPrice != oldPrice && game.RerollsRemaining == remaining &&
		          redRerollButton.Text == $"× ({remaining})",
                "Reroll must change the price and consume one attempt");
        }
		redRerollButton.EmitSignal(Button.SignalName.Pressed);
		Check(game.TradeActive && game.RerollsRemaining == 0 && game.Declined == 0 &&
		      game.ItemIndex == firstItem && game.StoredItems.SequenceEqual(remainingStored) &&
		      game.ShowcaseItems[0] == -1 && redRerollButton.Disabled,
		    "Spending the last rarity reroll must keep the item in the active sale and disable rerolling");
		Check(game.GetNode<PanelContainer>("World/TradingRoom/Item/TradePanel").Visible &&
		      !game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton").Disabled,
		    "An item with no rerolls must keep only the active sale controls available");
        int acceptedOffer = game.OfferedPrice;
        int scoreBeforeSale = game.Score;
        game.AcceptTrade();
        Check(game.Accepted == 1 && game.Score == scoreBeforeSale + acceptedOffer, "Accepting must record the sale");
        Check(game.BuyerCountdown is >= 0.5 and <= 2,
            "A completed sale must schedule a new random delay for the next displayed item");
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(remainingStored) &&
              game.ShowcaseItems[0] == -1 && game.ShowcaseItems[1] == displayedItem,
            "Selling must preserve unrelated inventory and showcase goods");
        game.AcceptTrade();
        Check(game.Accepted == 1, "An inactive trade must not be accepted twice");
        game._Input(rKey);
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(remainingStored),
            "R must not bypass the stored-item queue");

        game._Process(0.49);
        Check(!game.TradeActive, "Consecutive buyers must be separated by at least half a second");
        game._Process(game.BuyerCountdown + 0.001);
        game.AcceptTrade();
        Check(game.ShowcaseItems.All(item => item == -1), "The remaining displayed item must leave only through sale");
        Check(game.BuyerCountdown == 0, "Empty showcases must not schedule a buyer");

        SelectInventorySlot(game, 0);
        game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot3").EmitSignal(Button.SignalName.Pressed);
        CraftAtelier(game);
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(4);
        int[] displayed = game.ShowcaseItems;
        double pendingDelay = game.BuyerCountdown;
        Check(pendingDelay is >= 0.5 and <= 2, "Restocking an empty showroom must schedule a delay");
        game._Process(game.BuyerCountdown + 0.001);
        int[] remainingShowcases = game.ShowcaseItems;
        Check(game.TradeActive && displayed[2] >= 0 && displayed[4] >= 0 &&
              remainingShowcases.Count(item => item >= 0) == 1 &&
              ((remainingShowcases[2] == -1 && game.ItemIndex == displayed[2]) ||
               (remainingShowcases[4] == -1 && game.ItemIndex == displayed[4])),
            "A buyer must choose one of the occupied showcases");
		int activeItemWithoutRerolls = game.ItemIndex;
		for (int reroll = game.RerollsRemaining; reroll > 0; reroll--) game.RerollCustomer();
		Check(game.TradeActive && game.RerollsRemaining == 0 && game.ItemIndex == activeItemWithoutRerolls &&
		      game.ShowcaseItems.Count(item => item >= 0) == 1 && redRerollButton.Disabled,
		    "Exhausting rerolls must reserve the selected item for sale and leave other showcases untouched");
		game.AcceptTrade();
		Check(!game.TradeActive && game.ShowcaseItems.Count(item => item >= 0) == 1,
		    "The no-reroll item must leave the trade only through a completed sale");

        var camera = game.GetNode<WorldCamera>("Camera2D");
        Check(camera.BoundsSource == roomLayout, "The camera component must receive the room-layout bounds from the scene");
        Rect2 cameraBounds = roomLayout.GlobalTransform * roomLayout.GetRect();
        float widthZoom = game.GetViewportRect().Size.X / cameraBounds.Size.X;
        Check(camera.Position.Y > 1625, "The camera must start in the lower-left showroom");
        float oldZoom = camera.Zoom.X;
        for (int frame = 0; frame < 120; frame++) camera._Process(1d / 60d);
        Check(Mathf.IsEqualApprox(camera.Zoom.X, oldZoom) && camera.Zoom.X >= widthZoom && camera.Zoom.X <= 1.8f,
            "Camera zoom must remain stable without wheel input");
        camera._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Position = new Vector2(640, 360), Pressed = true });
        Check(Mathf.IsEqualApprox(camera.Zoom.X, oldZoom), "Wheel input must not jump to the target zoom");
        camera._Process(1d / 60d);
        Check(camera.Zoom.X > oldZoom && camera.Zoom.X < oldZoom * 1.12f,
            "Mouse wheel zoom must advance smoothly between frames");
        float zoomAfterStep = camera.Zoom.X;
        camera._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Position = new Vector2(640, 360), Pressed = true });
        camera._Process(1d / 60d);
        Check(camera.Zoom.X < zoomAfterStep && camera.Zoom.X > oldZoom,
            "Zooming back out must also advance smoothly");
        for (int step = 0; step < 100; step++)
            camera._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Position = new Vector2(640, 360), Pressed = true });
        for (int frame = 0; frame < 180; frame++) camera._Process(1d / 60d);
        Check(Mathf.IsEqualApprox(camera.Zoom.X, widthZoom, 0.001f) && widthZoom < 0.3f &&
              Mathf.IsEqualApprox(camera.Position.X, cameraBounds.GetCenter().X),
            "Zoom-out must stop exactly at the layout width, beyond the old fixed 0.3 limit");
        camera.Zoom = Vector2.One * 1.8f;
        camera.Position = cameraBounds.GetCenter();
        camera.ClampToBounds();
        float startingX = camera.Position.X;
        camera._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
        camera._Input(new InputEventMouseMotion { Relative = new Vector2(100, 0) });
        Check(Mathf.IsEqualApprox(camera.Position.X, startingX - 100f / camera.Zoom.X), "Dragging must pan the camera");
        camera.Zoom = Vector2.One * 1.8f;
        camera.Position = new Vector2(-100, -100);
        camera.ClampToBounds();
        Vector2 halfView = game.GetViewportRect().Size / (2f * camera.Zoom);
        Check((camera.Position - halfView).IsEqualApprox(cameraBounds.Position),
            "The top-left camera boundary must match RoomLayout without padding");
        camera.Position = new Vector2(100000, 100000);
        camera.ClampToBounds();
        Check((camera.Position + halfView).IsEqualApprox(cameraBounds.End),
            "The bottom-right camera boundary must match RoomLayout without padding");
        camera.Position = new Vector2(4582, 1103);
        camera.ClampToBounds();
        Check(camera.Position.X > 3622 && camera.Position.Y < 1625, "The camera must reach the upper-right workshop");
        camera.ClampToBounds();
        camera.Position = new Vector2(4582, 2183);
        camera.ClampToBounds();
        Check(camera.Position.X > 3622 && camera.Position.Y > 1625,
            "The camera must reach the lower-right showroom");

        Vector2 layoutPosition = roomLayout.Position;
        Vector2 layoutScale = roomLayout.Scale;
        roomLayout.Position += new Vector2(200, 150);
        roomLayout.Scale *= new Vector2(0.5f, 0.2f);
        cameraBounds = roomLayout.GlobalTransform * roomLayout.GetRect();
        camera.Zoom = Vector2.One * 0.001f;
        camera.ClampToBounds();
        Check(Mathf.IsEqualApprox(camera.Zoom.X, game.GetViewportRect().Size.X / cameraBounds.Size.X) &&
              camera.Position.IsEqualApprox(cameraBounds.GetCenter()),
            "Bounds must follow the layout transform; zoom-out depends only on width even for a short layout");
        Vector2I windowSize = Root.Size;
        Root.Size = new Vector2I(960, 900);
        camera.Zoom = Vector2.One * 0.001f;
        camera.ClampToBounds();
        Check(Mathf.IsEqualApprox(camera.Zoom.X, game.GetViewportRect().Size.X / cameraBounds.Size.X),
            "Resizing the viewport must recalculate the width-based zoom boundary");
        Root.Size = windowSize;
        roomLayout.Position = layoutPosition;
        roomLayout.Scale = layoutScale;
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Trade scene checks passed");
    }

    private async Task TestShowcaseReuse()
    {
        string absolutePath = ProjectSettings.GlobalizePath(SavePath);
        if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(absolutePath);
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        var game = await CreateGame(scene);
        CraftAtelier(game);
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(0);
        game._Process(game.BuyerCountdown + 0.01);
        int activeItem = game.ItemIndex;
        int replacement = game.StoredItems[0];
        SelectInventorySlot(game, 0);
        game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").EmitSignal(Button.SignalName.Pressed);
        int[] inventory = game.StoredItems;
        Check(game.TradeActive && game.ItemIndex == activeItem && game.ShowcaseItems[0] == replacement && inventory.Length == 1,
            "A normal click must refill the freed showcase without changing the active trade");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.TradeActive && game.ItemIndex == activeItem && game.ShowcaseItems[0] == replacement &&
              game.StoredItems.SequenceEqual(inventory),
            "Reload must preserve both the active sale and its refilled original showcase");
        int score = game.Score;
        int offer = game.OfferedPrice;
        game.AcceptTrade();
        game.AcceptTrade();
        Check(game.Score == score + offer && game.ShowcaseItems[0] == replacement &&
              game.StoredItems.SequenceEqual(inventory) &&
              !game.GetNode<Button>("HUD/Root/ProgressPanel/Content/EndDayButton").Disabled,
            "Selling exactly once must preserve the replacement item and inventory");
        game._Process(game.BuyerCountdown + 0.01);
        Check(game.TradeActive && game.ItemIndex == replacement && game.ShowcaseItems[0] == -1,
            "The replacement item must be offered to the next buyer");
        var slot = (SlotUI)game.GetNode<InventoryUI>("HUD/InventoryUI").Container.GetChild(0);
        slot.QuickTransfer(0);
        Check(game.StoredItems.Length == 0 && game.ShowcaseItems[0] == inventory[0] && game.ItemIndex == replacement,
            "Quick transfer must reuse the original slot while its next item is being sold");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestProgress()
    {
        string absolutePath = ProjectSettings.GlobalizePath(SavePath);
        if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(absolutePath);
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn")
            ?? throw new Exception("The trade scene must load");
        TradeScene game = await CreateGame(scene);
		game.Score = 500;
		game.RefreshItemButtons();
		game.TryUnlockMusicWorkshop();
        CraftAtelier(game);
        CraftMusic(game);
        int[] itemsBeforePlacement = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(3);
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(1)) &&
              game.ShowcaseItems[3] == itemsBeforePlacement[0],
            "Placing must move the selected item into a showcase without starting a trade");
        double savedDelay = game.BuyerCountdown;
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(!game.TradeActive && Math.Abs(game.BuyerCountdown - savedDelay) < 0.00001 &&
              game.ShowcaseItems[3] == itemsBeforePlacement[0],
            "Reload must restore the pending buyer delay without starting an immediate trade");
        CheckInventoryTooltips(game);
        game._Process(game.BuyerCountdown + 0.001);
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0] && game.ShowcaseItems[3] == -1 &&
              game.StoredItems[0] == itemsBeforePlacement[1] && game.EnergyRemaining == 0 &&
		      game.Score == 170 && game.MusicWorkshopUnlocked,
            "The delayed trade, inventory, unlocked room, cash and spent energy must survive a reload");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0] && game.ShowcaseItems[3] == -1 &&
              game.GetNode<Button>("World/Showroom/Showcases/Slot4").Disabled &&
              game.GetNode<Label>("World/Showroom/Showcases/Slot4/ItemLabel").Text == "СВОБОДНО",
            "An active showcase trade must survive a reload");
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(5);
        Check(game.TradeActive && game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.ShowcaseItems[3] == -1,
            "Other showcases must remain available during a restored trade");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.TradeActive && game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.GetNode<Button>("World/Showroom/Showcases/Slot4").Disabled,
            "Stocking during a trade must survive another reload");
        game.DeclineTrade();
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0] &&
              game.ShowcaseItems[3] == -1 && game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(2)),
            "The red button must reroll a restored trade without returning or moving goods");

        game.CurrentDay = 4;
        game.AcceptTrade();
        game.Score = game.FirstQuota - 1;
        AdvanceDayForTest(game);
        Check(game.Failed && !game.TradeActive && game.ShowcaseItems[3] == -1 &&
              game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(2)),
            "Failure at the deadline must preserve unsold goods and keep the sold item removed");
        CraftAtelier(game);
        game._Input(new InputEventKey { Keycode = Key.R, Pressed = true });
        Check(game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(2)),
            "Failure and R must not create items");
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 1 && game.Score == 220 && game.EnergyRemaining == 2 &&
		      !game.MusicWorkshopUnlocked && game.StoredItems.Length == 0 &&
		      game.ShowcaseItems.All(item => item == -1),
            "First failure must restore the empty day-one checkpoint");

		game.Score = 500;
		game.RefreshItemButtons();
		game.TryUnlockMusicWorkshop();
        CraftAtelier(game);
        CraftMusic(game);
        int[] checkpointItems = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(2);
        game.CurrentDay = 4;
        game.Score = game.FirstQuota + 12;
        AdvanceDayForTest(game);
        Check(game.QuotaStage == 1 && game.CurrentDay == 5 && game.Score == 12 &&
		      game.EnergyRemaining == 2 && game.MusicWorkshopUnlocked,
            "Paying the first quota must open day five with the remaining cash");
        Check(game.StoredItems.SequenceEqual(checkpointItems.Skip(1)) &&
              game.ShowcaseItems[2] == checkpointItems[0] && !game.TradeActive,
            "The checkpoint must retain unsold showroom items");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(!game.TradeActive && game.BuyerCountdown is > 0 and <= 2,
            "Reloaded checkpoint goods must wait for a buyer");
        game._Process(game.BuyerCountdown + 0.001);
        Check(game.CurrentDay == 5 && game.Score == 12 && game.EnergyRemaining == 2 &&
		      game.MusicWorkshopUnlocked &&
              game.TradeActive && game.ItemIndex == checkpointItems[0] && game.ShowcaseItems[2] == -1 &&
              game.StoredItems.SequenceEqual(checkpointItems.Skip(1)),
            "The delayed showroom trade, inventory, cash and restored energy must survive a scene reload");
        game.AcceptTrade();
        Check(!game.TradeActive && game.ShowcaseItems[2] == -1,
            "The selected showroom item must leave through a completed sale");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.ShowcaseItems[2] == -1 && game.StoredItems.SequenceEqual(checkpointItems.Skip(1)),
            "A sold showroom item must stay sold after reload");
        game.ReturnToCheckpoint();
        Check(game.ShowcaseItems[2] == checkpointItems[0],
            "Returning to the checkpoint must restore its showroom contents");
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(4);
        game.CurrentDay = 6;
        game.Score = 1000;
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 5 && game.Score == 12 && game.EnergyRemaining == 2 &&
		      game.MusicWorkshopUnlocked && !game.TradeActive &&
              game.StoredItems.SequenceEqual(checkpointItems.Skip(1)) &&
              game.ShowcaseItems[2] == checkpointItems[0] && game.ShowcaseItems[4] == -1,
            "Checkpoint must restore cash, inventory and showroom items");

        game.CurrentDay = 7;
        game.Score = game.SecondQuota;
        AdvanceDayForTest(game);
        Check(game.Completed && game.QuotaStage == 2 && game.Score == 0,
            "Paying the second quota must finish the game");
        game.NewGame();
        Check(game.CurrentDay == 1 && game.Score == 220 && game.EnergyRemaining == 2 &&
		      !game.MusicWorkshopUnlocked &&
              game.StoredItems.Length == 0 && !game.TradeActive &&
              game.ShowcaseItems.All(item => item == -1),
            "New game must reset progress without giving a free item");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);

        foreach (double savedCountdown in new[] { 0d, 0.125d })
        {
            using (var waitingSave = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                waitingSave.StoreString(savedCountdown == 0
                    ? "{\"save_version\":7,\"showcase_items\":[0],\"buyer_countdown\":0}"
                    : "{\"save_version\":7,\"showcase_items\":[0],\"buyer_countdown\":0.125}");
            game = await CreateGame(scene);
            Check(!game.TradeActive && (savedCountdown == 0
                    ? game.BuyerCountdown is >= 0.5 and <= 2
                    : game.BuyerCountdown == savedCountdown),
                "Legacy zero timers must gain a delay; a positive remainder below 0.5 must survive reload");
            game._Process(game.BuyerCountdown - 0.001);
            Check(!game.TradeActive, "Reload must not bypass the remaining delay");
            game._Process(0.002);
            Check(game.TradeActive && game.ItemIndex == 0, "Reloaded waiting goods must reach a buyer after the delay");
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
        }

        using (var oldSave = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
            oldSave.StoreString("{\"trade_active\":true,\"item_index\":2,\"customer_index\":1,\"percentage\":0,\"rerolls_remaining\":1}");
        game = await CreateGame(scene);
        Check(game.TradeActive && game.StoredItems.Length == 0, "An older active trade must be restored without duplicating its item");
        Check(game.RerollsRemaining == 3 && game.CustomerIndex == 3,
            "Old reroll limits and customer appearance must migrate");
        game.RerollCustomer();
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.RerollsRemaining == 2, "A current save must preserve spent rerolls");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        DirAccess.RemoveAbsolute(absolutePath);
        GD.Print("Inventory, save and quota checks passed");
    }

    private async Task TestDayCycle()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        Vector2I originalSize = Root.Size;
        TradeScene game = null;
        try
        {
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            game = await CreateGame(scene);
            Check(!game.DayClosed && game.DayRemainingSeconds == 120 &&
                  game.GetNode<Label>("HUD/Root/DayTimer").Text == "02:00",
                "A new day must start with a full two minutes in mm:ss format");
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            foreach (float zoom in new[] { 0.25f, 1.8f })
            {
                Root.Size = size;
                game.GetNode<Camera2D>("Camera2D").Zoom = Vector2.One * zoom;
                await ToSignal(this, SignalName.ProcessFrame);
                var timer = game.GetNode<Label>("HUD/Root/DayTimer");
                Rect2 bounds = timer.GetGlobalRect();
                Check(Mathf.IsEqualApprox(bounds.GetCenter().X, Root.GetVisibleRect().Size.X / 2) &&
                      Root.GetVisibleRect().Encloses(bounds) && timer.MouseFilter == Control.MouseFilterEnum.Ignore,
                    "The timer must stay centered at the top, in bounds, and ignore mouse input regardless of zoom");
            }
            game._Process(0.25);
            Check(game.DayRemainingSeconds == 119.75 && game.GetNode<Label>("HUD/Root/DayTimer").Text == "02:00",
                "The timer must count partial seconds without showing zero early");
            game._Process(0.75);
            Check(game.GetNode<Label>("HUD/Root/DayTimer").Text == "01:59", "The timer must roll from minutes to seconds");

            foreach (bool manual in new[] { true, false })
            {
                game.NewGame();
                game.Score = 1000;
                CraftAtelier(game);
                SelectInventorySlot(game, 0);
                game.PlaceItemInShowcase(0);
                SelectInventorySlot(game, 0);
                game.PlaceItemInShowcase(1);
                game._Process(game.BuyerCountdown + 0.001);
                Check(game.TradeActive && game.EnergyRemaining == 1, "Closing test must have an active trade and unused energy");
                int item = game.ItemIndex;
                int[] inventory = game.StoredItems;
                int[] showcases = game.ShowcaseItems;
                double remaining = game.DayRemainingSeconds;
                game.GetNode<PauseMenu>("PauseMenu").Open();
                game._Process(120);
                Check(!game.DayClosed && game.DayRemainingSeconds == remaining && game.TradeActive,
                    "Pause must freeze the day even with an active trade");
                game.GetNode<PauseMenu>("PauseMenu").Close();
                game.CurrentDay = 4;
                game.Score = game.FirstQuota - 1;
                if (manual) await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
                else game._Process(game.DayRemainingSeconds);
                Check(game.DayClosed && game.DayRemainingSeconds == 0 && game.EnergyRemaining == 0 &&
                      game.CurrentDay == 4 && !game.Failed && game.TradeActive && game.ItemIndex == item &&
                      game.StoredItems.SequenceEqual(inventory) && game.ShowcaseItems.SequenceEqual(showcases) &&
                      game.GetNode<Label>("HUD/Root/DayTimer").Text == "00:00" && game.BuyerCountdown == 0,
                    "Manual close and timeout must discard energy, retain goods and keep the last trade before quota settlement");
                Check(game.GetNode<Button>("World/Atelier/GetItemButton").Disabled &&
                      game.GetNode<Button>("HUD/Root/ProgressPanel/Content/EndDayButton").Disabled &&
                      !game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton").Disabled &&
                      !game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton").Disabled,
                    "Closed-day controls must block production/advance and retain both trade actions");
                int cash = game.Score;
                CraftAtelier(game);
                game.EndDay();
                game._Process(120);
                Check(game.DayClosed && game.TradeActive && game.ItemIndex == item && game.Score == cash &&
                      game.StoredItems.SequenceEqual(inventory) && game.CurrentDay == 4,
                    "Repeated closing and ticking must not advance the day, spend money or replace the active buyer");
                int rerolls = game.RerollsRemaining;
                int customer = game.CustomerIndex;
                await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton");
                Check(game.TradeActive && game.RerollsRemaining == rerolls - 1 && game.CustomerIndex != customer,
                    "A real reroll click must still replace the price and buyer after closing");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(game.DayClosed && game.DayRemainingSeconds == 0 && game.EnergyRemaining == 0 &&
                      game.TradeActive && game.ItemIndex == item && game.RerollsRemaining == rerolls - 1 &&
                      game.ShowcaseItems.SequenceEqual(showcases),
                    "Reload must retain closed day, zero energy and the last trade with its consumed reroll");
                game._Input(new InputEventKey { Keycode = Key.F, Pressed = true });
                Check(game.RerollsRemaining == Math.Max(0, rerolls - 2), "The reroll hotkey must remain available after closing and reload");
                for (int count = game.RerollsRemaining; count > 0; count--) game.RerollCustomer();
                int offer = game.OfferedPrice;
                await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
                game.AcceptTrade();
                Check(!game.TradeActive && game.DayClosed && game.Score == cash + offer && game.EnergyRemaining == 0 &&
                      game.ShowcaseItems.SequenceEqual(showcases) && game.BuyerCountdown == 0,
                    "The final sale must credit money once and keep the shop closed with displayed goods intact");
                game._Process(120);
                Check(!game.TradeActive && game.BuyerCountdown == 0, "No further buyers may arrive after the last sale");
                await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
                Check(!game.DayClosed && game.CurrentDay == 5 && game.QuotaStage == 1 &&
                      game.Score == cash + offer - game.FirstQuota && game.EnergyRemaining == 2 && game.DayRemainingSeconds == 120 &&
                      game.BuyerCountdown is >= 0.5 and <= 2,
                    "The next day must settle the quota using the last sale, restore energy/time and schedule a fresh buyer delay");
                await ClickControl(game, "DayTransitionCard/Root/Center/Card/Content/Choices/Atelier/Content/Choose");
                game._Process(game.BuyerCountdown + 0.001);
                Check(game.TradeActive, "Buyers must return when the next day starts");
                game.ReturnToCheckpoint();
                Check(!game.TradeActive && !game.DayClosed && game.DayRemainingSeconds == 120 && game.EnergyRemaining == 2,
                    "Checkpoint restore must start a fresh open day");
            }

            game.NewGame();
            CraftAtelier(game);
            SelectInventorySlot(game, 0);
            game.PlaceItemInShowcase(0);
            int displayedItem = game.ShowcaseItems[0];
            game._Process(120);
            Check(game.DayClosed && !game.TradeActive && game.EnergyRemaining == 0 && game.ShowcaseItems[0] == displayedItem,
                "Expiry must cancel a pending buyer in the same frame instead of creating a new last trade");
            game._Process(120);
            Check(!game.TradeActive, "An expired day with no active deal must stay closed");
            game.NewGame();
            game._Process(10.25);
            game.GetNode<PauseMenu>("PauseMenu").Open();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = await CreateGame(scene);
            Check(!Paused && !game.DayClosed && game.DayRemainingSeconds == 109.75 && game.EnergyRemaining == 2,
                "Pause/menu save must restore the exact remaining fraction without charging offline time");
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            using (var legacy = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                legacy.StoreString("{\"save_version\":7,\"energy_remaining\":1}");
            game = await CreateGame(scene);
            Check(!game.DayClosed && game.DayRemainingSeconds == 120 && game.EnergyRemaining == 1,
                "Pre-timer saves must gain two full minutes while preserving their spent energy");
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Two-minute timer, manual/timeout closure, last sale, quota, pause and save migration checks passed");
    }

    private async Task TestDayTransitionCards()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        Vector2I originalSize = Root.Size;
        string language = GameLocalization.Language;
        TradeScene game = null;
        const string content = "DayTransitionCard/Root/Center/Card/Content/";
        try
        {
            game = await CreateGame(scene);
            foreach (string locale in new[] { "ru", "en" })
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            {
                TranslationServer.SetLocale(locale);
                Root.Size = size;
                game.NewGame();
                game.Score = 2000;
                for (int day = 1; day <= 6; day++)
                {
                    game.EndDay();
                    Check(game.DayClosed && !game.DayTransitionOpen, "Closing alone must not show a new-day card");
                    game.EndDay();
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                    Check(game.DayTransitionOpen && Paused && game.CurrentDay == day + 1 && game.DayRemainingSeconds == 120 &&
                          game.GetNode<Label>(content + "Route").Text.StartsWith($"{day:00}  →  {day + 1:00}"),
                        "Each of the six transitions must show its own card with the new day frozen at two minutes");
                    Check(card.Options.Count == 3 && card.Options.Distinct().Count() == 3 &&
                          game.GetNode(content + "Choices").GetChildCount() == 3, "Each transition must offer exactly three different upgrades");
                    if (day == 6 && locale == "ru" && size.X == 1280 && OS.GetCmdlineUserArgs().Contains("--day-card-preview"))
                    {
                        await ToSignal(this, SignalName.ProcessFrame);
                        await ToSignal(this, SignalName.ProcessFrame);
                        Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-day-card-preview.png"));
                    }
                    foreach (var option in card.Options)
                    {
                        foreach (string node in new[] { "Amount", "Title", "Description", "Duration", "Choose" })
                        {
                            var control = game.GetNode<Control>(content + $"Choices/{option}/Content/{node}");
                            Check(Root.GetVisibleRect().Encloses(control.GetGlobalRect()), "Upgrade card content must fit both sizes and languages");
                        }
                        var button = game.GetNode<Button>(content + $"Choices/{option}/Content/Choose");
                        Check(button.Text == GameLocalization.Format("Купить за {0}$", DayUpgradeSession.Cost(option)), "Upgrade prices must translate");
                    }
                    double remaining = game.DayRemainingSeconds;
                    int cash = game.Score;
                    var camera = game.GetNode<Camera2D>("Camera2D");
                    Vector2 position = camera.Position;
                    game.EndDay();
                    game._Process(60);
                    game.TickCraftDay(60);
                    game.GetNode<PauseMenu>("PauseMenu").Open();
                    game.GetItemFromAtelier();
                    Check(game.DayRemainingSeconds == remaining && game.Score == cash && game.CurrentDay == day + 1 &&
                          !game.CraftMiniGameOpen && camera.Position == position &&
                          !game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
                        "Repeated advance, pause and crafting must not act underneath a day card");
                    if (day == 4) Check(game.QuotaStage == 1, "The 4-to-5 choice must follow ordinary quota settlement");
                    DayUpgrade choice = card.Options[0];
                    card._Input(new InputEventKey { Pressed = true, Echo = true, Keycode = Key.Key1 });
                    Check(card.IsOpen, "Held keys must not purchase an upgrade");
                    if (day % 2 == 0) await ClickControl(game, content + $"Choices/{choice}/Content/Choose");
                    else card._Input(new InputEventKey { Pressed = true, Keycode = Key.Key1 });
                    card.Choose(card.Options[1]);
                    Check(!Paused && !game.DayTransitionOpen && game.CurrentDay == day + 1,
                        "Mouse/keyboard selection must apply only one upgrade without skipping a day");
                    Check(game.DayUpgrades.Selected == choice && game.Score == cash - DayUpgradeSession.Cost(choice) + (choice == DayUpgrade.Cash ? 50 : 0) &&
                          game.EnergyRemaining == (choice == DayUpgrade.Energy ? 3 : 2) &&
                          game.DayRemainingSeconds == (choice == DayUpgrade.Time ? 150 : 120), "The chosen upgrade must change only its intended stat");
                }
                game.EndDay();
                game.EndDay();
                Check(game.Completed && !game.DayTransitionOpen && !Paused, "Winning after day seven must not show a day-eight card");
            }
            game.NewGame();
            game.CurrentDay = 4;
            game.Score = 0;
            game.EndDay();
            game.EndDay();
            Check(game.Failed && !game.DayTransitionOpen && !Paused, "An unpaid quota must not show a successful transition");
            game.NewGame();
            game.EndDay();
            game.EndDay();
            game.ReturnToCheckpoint();
            Check(!Paused && !game.DayTransitionOpen && game.CurrentDay == 1, "Checkpoint restore must release the card pause");
            game.EndDay();
            game.EndDay();
            game.NewGame();
            Check(!Paused && !game.DayTransitionOpen && game.CurrentDay == 1, "New game must release the card pause");
            game.EndDay();
            game.EndDay();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            Check(!Paused, "Freeing an open card must release its pause");
            game = await CreateGame(scene);
            Check(game.CurrentDay == 2 && Paused && game.DayTransitionOpen && game.DayRemainingSeconds == 120,
                "Reload before selection must reopen the three choices without repeating the day transition");
            foreach (DayUpgrade choice in new[] { DayUpgrade.Cash, DayUpgrade.Energy, DayUpgrade.Time, DayUpgrade.Atelier, DayUpgrade.Music })
            {
                game.NewGame();
                game.Score = 1000;
                game.CurrentDay = choice is DayUpgrade.Atelier or DayUpgrade.Music ? 3 : 1;
                game.EndDay();
                game.EndDay();
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                card.Choose(choice);
                int cash = game.Score;
                int energy = game.EnergyRemaining;
                double time = game.DayRemainingSeconds;
                int atelierLevel = game.DayUpgrades.AtelierLevel;
                int musicLevel = game.DayUpgrades.MusicLevel;
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(!Paused && !game.DayTransitionOpen && game.Score == cash && game.EnergyRemaining == energy &&
                      game.DayRemainingSeconds == time && game.DayUpgrades.AtelierLevel == atelierLevel && game.DayUpgrades.MusicLevel == musicLevel,
                    "Every upgrade must survive reload with no repeated reward, lost energy or clamped bonus time");
                game.GetNode<DayTransitionCard>("DayTransitionCard").Choose(choice);
                Check(game.Score == cash && game.EnergyRemaining == energy && game.DayRemainingSeconds == time, "Reload must not reapply a reward");
                if (choice is DayUpgrade.Atelier or DayUpgrade.Music)
                {
                    foreach (string room in new[] { "Atelier", "MusicWorkshop" })
                    {
                        var panel = game.GetNode<RarityPanel>($"World/{room}/RarityPanel");
                        panel.RefreshChances();
                        bool boosted = (room == "Atelier") == (choice == DayUpgrade.Atelier);
                        Check(panel.Values.GetNode<Label>("Chance0").Text == (boosted ? "45%" : "60%") &&
                              panel.Values.GetNode<Label>("Chance2").Text == (boosted ? "18%" : "10%"), "Category chances must match the actual boosted table");
                        Check(panel.Values.GetNode<Label>("Chance0").GetThemeColor("font_color") == (boosted ? new Color("bf3232") : Colors.Black) &&
                              panel.Values.GetNode<Label>("Chance2").GetThemeColor("font_color") == (boosted ? new Color("208447") : Colors.Black),
                            "Chances below baseline must be red and above baseline green; the other category stays neutral");
                    }
                    int[] counts = new int[10];
                    string workshop = choice == DayUpgrade.Atelier ? "atelier" : "music";
                    for (int ticket = 0; ticket < 100; ticket++) counts[WorkshopItemCatalog.Roll(workshop, ticket, game.DayUpgrades)]++;
                    foreach (var item in WorkshopItemCatalog.Items.Select((item, index) => (item, index)).Where(x => x.item.WorkshopId == workshop))
                        Check(counts[item.index] == WorkshopItemCatalog.RarityWeight(workshop, item.item.RarityId, game.DayUpgrades), "All 100 tickets must match the displayed category chances");
                }
                game.EndDay();
                game.EndDay();
                Check(game.DayUpgrades.AtelierLevel == atelierLevel && game.DayUpgrades.MusicLevel == musicLevel &&
                      game.DayRemainingSeconds == 120 && game.EnergyRemaining == 2, "Category upgrades must persist across days while daily energy/time expire");
                game.ReturnToCheckpoint();
                Check(game.DayUpgrades.AtelierLevel == (game.CheckpointDay == 5 ? atelierLevel : 0) &&
                      game.DayUpgrades.MusicLevel == (game.CheckpointDay == 5 ? musicLevel : 0), "Checkpoint restore must retain exactly its captured category upgrades");
            }
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(language);
            Root.Size = originalSize;
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("All six day transition cards, localization, layout, input, quotas and reload checks passed");
    }

    private async Task TestIntroTransition()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmoke"),
            "Startup smoke requires isolated userdata via a temporary override.cfg; refusing to touch player saves");
        GD.Print("Startup smoke user directory: " + OS.GetUserDataDir());
        Check(FileAccess.FileExists("res://config/economy.v1.json") && FileAccess.FileExists("res://dialogue/intro.ru.json"),
            "The executable must contain both economy and dialogue JSON files");
        var menuScene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
        foreach (bool nullOverride in new[] { false, true })
        foreach (bool skip in new[] { false, true })
        {
            var menu = menuScene.Instantiate<MainMenu>();
            Check(menu.GameScene?.ResourcePath == "res://scenes/trade_scene.tscn",
                "The serialized main menu must reference the actual game scene");
            if (nullOverride) menu.GameScene = null;
            Root.AddChild(menu);
            CurrentScene = menu;
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(menu.ActiveDialogue != null, "New game must open the intro");
            if (skip) await ClickControl(menu, "IntroSkipLayer/SkipButton");
            else
            {
                int steps = 0;
                while (menu.ActiveDialogue != null && steps++ < 512) menu.ActiveDialogue.Advance();
                Check(steps < 512, "The full intro must make progress and reach its finish event");
            }
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(CurrentScene is TradeScene && !Paused,
                $"Intro must enter gameplay instead of returning to NewGame (null={nullOverride}, skip={skip})");
            var game = (TradeScene)CurrentScene;
            game.SetProcess(false);
            Check(game.CurrentDay == 1 && game.Score == game.StartingMoney && game.DayRemainingSeconds > 88 &&
                  game.GetNode<Label>("HUD/Root/DayTimer").Text == "90",
                "The exported game must start a real new day with the current economy and clock");
            game.Score = 321;
            game.PersistProgress();
            game.QueueFree();
            CurrentScene = null;
            await ToSignal(this, SignalName.ProcessFrame);
        }
        var continueMenu = menuScene.Instantiate<MainMenu>();
        continueMenu.GameScene = null;
        Root.AddChild(continueMenu);
        CurrentScene = continueMenu;
        continueMenu.OnPlayPressed();
        continueMenu.GetNode<TextureButton>("MainMenuContainer/Menu/ContinueButton").EmitSignal(BaseButton.SignalName.Pressed);
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene is TradeScene && ((TradeScene)CurrentScene).Score == 321,
            "Continue must also resolve a missing scene without resetting saved progress");
        CurrentScene.QueueFree();
        CurrentScene = null;
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestLiveEconomyBalance()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        var config = EconomyConfigProvider.Load();
        TradeScene game = null;
        string locale = GameLocalization.Language;
        try
        {
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            game = await CreateGame(scene);
            Check(game.StartingMoney == config.StartingMoney && game.FirstQuota == 250 && game.SecondQuota == 450,
                "The live game must read the same starting money and quotas as the simulator");
            foreach (string language in new[] { "ru", "en" })
            {
                TranslationServer.SetLocale(language);
                game.NewGame();
                Check(game.DayRemainingSeconds == 90 && game.GetNode<Label>("HUD/Root/DayTimer").Text == "90",
                    "The first day must start at ninety seconds before unlocking music");
                foreach (string workshop in new[] { "atelier", "music" })
                {
                    string room = workshop == "atelier" ? "Atelier" : "MusicWorkshop";
                    int cost = WorkshopItemCatalog.CraftCost(workshop);
                    if (workshop == "music")
                    {
                        game.NewGame(); game.Score = 1000; game.TryUnlockMusicWorkshop();
                        Check(game.DayRemainingSeconds == 90, "Unlocking music must not change the ninety-second baseline");
                    }
                    Check(game.GetNode<Label>($"World/{room}/HintLabel").Text.Contains(cost.ToString()) &&
                          game.GetNode<Label>($"World/{room}/GetItemButton/PriceHint/Label").Text.Contains(cost.ToString()),
                        "RU/EN crafting labels must show the configured single-item cost");
                    game.Score = cost - 1; game.RefreshItemButtons();
                    var button = game.GetNode<Button>($"World/{room}/GetItemButton");
                    Check(button.Disabled, "Crafting must be unavailable one dollar below the real cost");
                    if (workshop == "atelier") game.GetItemFromAtelier(); else game.GetItemFromMusicWorkshop();
                    var mini = game.GetNode<EmbroideryGame>(workshop == "atelier" ? "EmbroideryGame" : "PolishingGame");
                    Check(!mini.IsOpen, "The minigame must use the new affordability threshold too");
                    game.Score = cost; game.RefreshItemButtons();
                    Check(!button.Disabled, "Exact funds must enable single-item crafting");
                    button.EmitSignal(Button.SignalName.Pressed);
                    Check(mini.IsOpen, "The real craft button must open the minigame at the configured cost");
                    int items = game.StoredItems.Length, energy = game.EnergyRemaining;
                    mini.Activate(); mini.Activate();
                    Check(game.Score == 0 && game.StoredItems.Length == items + 1 && game.EnergyRemaining == energy - 1,
                        "Starting a craft must charge once for exactly one item and one energy");
                    mini._Process(10); mini.Activate();
                    Check(!Paused && !mini.IsOpen && game.DayRemainingSeconds == 80,
                        "Crafting must consume its actual ten seconds within the ninety-second day");
                }
            }
            game.NewGame(); game.CurrentDay = 4; game.Score = 250;
            game.EndDay(); game.EndDay();
            Check(game.CurrentDay == 5 && game.QuotaStage == 1 && game.Score == 0 && game.CheckpointDay == 5,
                "The first quota must be exactly250 at the end of day four");
            game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
            game.CurrentDay = 7; game.Score = 450; game.EndDay(); game.EndDay();
            Check(game.Completed && game.Score == 0 && !game.DayTransitionOpen,
                "The final quota must be exactly450 at the end of day seven");
            game.NewGame(); game.CurrentDay = 4; game.Score = 249; game.EndDay(); game.EndDay();
            Check(game.Failed && !game.DayTransitionOpen, "One dollar below the first quota must remain a real loss");
            game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); game = null;
            using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":12,\"day\":1,\"score\":220,\"energy_remaining\":3,\"day_remaining_seconds\":30}");
            game = await CreateGame(scene);
            Check(game.DayRemainingSeconds == 45 && game.GetNode<Label>("HUD/Root/DayTimer").Text == "45",
                "Half of a legacy sixty-second first day must restore as half of ninety seconds");
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(locale);
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestClockHud()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        Vector2I originalSize = Root.Size;
        try
        {
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            game = await CreateGame(scene);
            game.NewGame();
            var timer = game.GetNode<Label>("HUD/Root/DayTimer");
            var clock = timer.GetNode<TextureRect>("Clock");
            Check(timer.Text == "90" && clock.Texture.ResourcePath == "res://art/ui/day_timer_empty.png",
                "The existing one-room duration must display plain seconds on the empty clock");
            foreach (bool music in new[] { false, true })
            {
                if (music) { game.Score = 1000; game.TryUnlockMusicWorkshop(); }
                foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
                foreach (float zoom in new[] { 0.25f, 1.8f })
                {
                    Root.Size = size;
                    game.GetNode<Camera2D>("Camera2D").Zoom = Vector2.One * zoom;
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    game._Process(0);
                    Rect2 bounds = timer.GetGlobalRect();
                    Rect2 inventory = game.GetNode<Control>("HUD/InventoryUI/ScrollContainer").GetGlobalRect();
                    Check(Root.GetVisibleRect().Encloses(bounds) && bounds.Position.X == 12 &&
                          Mathf.IsEqualApprox(bounds.End.Y, Root.GetVisibleRect().Size.Y - 12) && !bounds.Intersects(inventory),
                        "The clock must remain bottom-left without overlapping three or six inventory slots at either size/zoom");
                    Check(timer.MouseFilter == Control.MouseFilterEnum.Ignore && clock.MouseFilter == Control.MouseFilterEnum.Ignore &&
                          clock.ShowBehindParent && timer.GetThemeFontSize("font_size") >= 18,
                        "Clock art must sit behind readable digits and let mouse input reach the world");
                }
            }
            Check(timer.Text == "90" && game.DayRemainingSeconds == 90, "Unlocking the music room must retain the existing ninety-second day");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            game._Process(10);
            Check(timer.Text == "90" && game.DayRemainingSeconds == 90, "Pause must freeze the seconds and clock state");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            if (OS.GetCmdlineUserArgs().Contains("--clock-preview"))
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-clock-active.png"));
            }
            game._Process(0.25);
            Check(timer.Text == "90", "Fractional seconds must round up rather than show zero early");
            game._Process(0.75);
            Check(timer.Text == "89", "Seconds must count down without a minutes separator");
            game._Process(88.99);
            Check(timer.Text == "01" && clock.Texture.ResourcePath.EndsWith("day_timer_empty.png"), "The last partial second must show 01 on the empty face");
            game._Process(0.02);
            Check(timer.Text == "00" && game.DayClosed && clock.Texture.ResourcePath.EndsWith("day_timer_expired.png"),
                "At timeout the clock must show 00 and switch to the supplied filled red face");
            if (OS.GetCmdlineUserArgs().Contains("--clock-preview"))
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-clock-expired.png"));
            }
            game.EndDay();
            game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
            Check(timer.Text == "90" && clock.Texture.ResourcePath.EndsWith("day_timer_empty.png"), "The next day must restore the empty clock");
            game._Process(2.25);
            double remaining = game.DayRemainingSeconds;
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = await CreateGame(scene);
            timer = game.GetNode<Label>("HUD/Root/DayTimer");
            clock = timer.GetNode<TextureRect>("Clock");
            Check(game.DayRemainingSeconds == remaining && timer.Text == "88" && clock.Texture.ResourcePath.EndsWith("day_timer_empty.png"),
                "Reload must restore fractional time with the matching seconds and art");
            game.EndDay();
            Check(timer.Text == "00" && clock.Texture.ResourcePath.EndsWith("day_timer_expired.png"), "Manual closure must also show the filled clock");
            game.EndDay();
            game.GetNode<DayTransitionCard>("DayTransitionCard").Choose(DayUpgrade.Time);
            Check(timer.Text == "120" && game.DayRemainingSeconds == 120, "The timer must show all remaining seconds including a purchased time bonus");
            game.GetItemFromAtelier();
            var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
            Check(embroidery.IsOpen && embroidery.GetNode<Label>("Surface/DayTimer").Text == timer.Text,
                "The minigame clock copy must keep working with the new seconds format");
            embroidery.GetNode<Button>("Surface/Cancel").EmitSignal(Button.SignalName.Pressed);
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            Root.Size = originalSize;
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestDayCardPurchases()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        string language = GameLocalization.Language;
        Vector2I originalSize = Root.Size;
        const string content = "DayTransitionCard/Root/Center/Card/Content/";
        void Seed(int day, int cash)
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            file.StoreString(System.Text.Json.JsonSerializer.Serialize(new
            {
                save_version = 10, day, score = cash, quota_stage = day >= 5 ? 1 : 0,
                energy_remaining = 2, day_remaining_seconds = 60
            }));
        }
        async Task Reload()
        {
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = await CreateGame(scene);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            foreach (string locale in new[] { "ru", "en" })
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            {
                TranslationServer.SetLocale(locale);
                Root.Size = size;
                Seed(1, 0);
                game = await CreateGame(scene);
                game.EndDay(); game.EndDay();
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.IsOpen && Paused, "A zero balance must still allow viewing and skipping the shop");
                foreach (var choice in card.Options)
                {
                    var button = game.GetNode<Button>(content + $"Choices/{choice}/Content/Choose");
                    Check(button.Disabled && button.Text == GameLocalization.Format("Купить за {0}$", DayUpgradeSession.Cost(choice)),
                        "Unaffordable cards must show their price and disable purchase in both languages");
                    foreach (string name in new[] { "Amount", "Title", "Description", "Duration", "Choose" })
                        Check(Root.GetVisibleRect().Encloses(game.GetNode<Control>(content + $"Choices/{choice}/Content/{name}").GetGlobalRect()),
                            "All paid card content must fit the viewport");
                    card.Choose(choice);
                }
                card._Input(new InputEventKey { Pressed = true, Keycode = Key.Key1 });
                Check(card.IsOpen && game.Score == 0 && game.DayUpgrades.Selected == DayUpgrade.None,
                    "Direct requests and hotkeys must not purchase cards without funds");
                var skip = game.GetNode<Button>(content + "Skip");
                Check(!skip.Disabled && Root.GetVisibleRect().Encloses(skip.GetGlobalRect()) &&
                      skip.Tr(skip.Text) == (locale == "en" ? "Skip" : "Пропустить"), "Free skip must stay visible and localized");
                if (size.X == 960) await ClickControl(game, content + "Skip");
                else card._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
                card.Skip();
                Check(!Paused && !card.IsOpen && game.Score == 0 && !game.DayUpgrades.Pending &&
                      game.DayUpgrades.Selected == DayUpgrade.None, "Skipping must resume with no payment or effect");
                await Reload();
                Check(!Paused && !game.DayTransitionOpen && !game.DayUpgrades.Pending && game.Score == 0 && game.CurrentDay == 2,
                    "Reload must not reopen or charge a skipped offer");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = null;
            }
            Root.Size = new Vector2I(1280, 720);
            foreach (var choice in Enum.GetValues<DayUpgrade>().Where(value => value != DayUpgrade.None))
            {
                int day = choice is DayUpgrade.AtelierPrice or DayUpgrade.MusicPrice ? 5 : choice is DayUpgrade.Atelier or DayUpgrade.Music ? 3 : 1;
                Seed(day, DayUpgradeSession.Cost(choice));
                game = await CreateGame(scene);
                game.EndDay(); game.EndDay();
                await Reload();
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.IsOpen && game.DayUpgrades.Pending, "An unfinished purchase must reopen after reload");
                int balance = game.Score;
                double remaining = game.DayRemainingSeconds;
                int energy = game.EnergyRemaining;
                var button = game.GetNode<Button>(content + $"Choices/{choice}/Content/Choose");
                Check(!button.Disabled, "Exact funds must allow buying the card");
                if (choice == DayUpgrade.AtelierPrice && OS.GetCmdlineUserArgs().Contains("--day-card-preview"))
                {
                    await ToSignal(this, SignalName.ProcessFrame);
                    Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-paid-cards-preview.png"));
                }
                await ClickControl(game, content + $"Choices/{choice}/Content/Choose");
                int expected = balance - DayUpgradeSession.Cost(choice) + (choice == DayUpgrade.Cash ? game.DayUpgrades.CashReward : 0);
                card.Choose(choice); card.Skip();
                Check(game.Score == expected && !Paused && !card.IsOpen && game.DayUpgrades.Selected == choice,
                    "A card must charge once, apply once and dismiss the shop");
                Check(game.EnergyRemaining == energy + (choice == DayUpgrade.Energy ? 1 : 0) &&
                      game.DayRemainingSeconds == remaining + (choice == DayUpgrade.Time ? 30 : 0), $"Paid energy/time effects must match their descriptions: {choice}, energy={game.EnergyRemaining}/{energy}, time={game.DayRemainingSeconds}/{remaining}");
                int atelier = game.DayUpgrades.AtelierLevel, music = game.DayUpgrades.MusicLevel;
                int atelierPrice = game.DayUpgrades.AtelierPriceLevel, musicPrice = game.DayUpgrades.MusicPriceLevel;
                Check(atelier == (choice == DayUpgrade.Atelier ? 1 : 0) && music == (choice == DayUpgrade.Music ? 1 : 0) &&
                      atelierPrice == (choice == DayUpgrade.AtelierPrice ? 1 : 0) && musicPrice == (choice == DayUpgrade.MusicPrice ? 1 : 0),
                    "Paid category upgrades must apply exactly one level to the correct category");
                await Reload();
                Check(game.Score == expected && !Paused && !game.DayTransitionOpen && game.DayUpgrades.Selected == choice &&
                      game.DayUpgrades.AtelierLevel == atelier && game.DayUpgrades.MusicLevel == music &&
                      game.DayUpgrades.AtelierPriceLevel == atelierPrice && game.DayUpgrades.MusicPriceLevel == musicPrice,
                    "Reload must preserve the payment and upgrade without charging or rewarding twice");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = null;
            }
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            Root.Size = originalSize;
            TranslationServer.SetLocale(language);
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestCategoryPriceUpgrades()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        string language = GameLocalization.Language;
        Vector2I size = Root.Size;
        try
        {
            TranslationServer.SetLocale("ru");
            Root.Size = new Vector2I(1280, 720);
            foreach (DayUpgrade choice in new[] { DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice })
            {
                bool clothing = choice == DayUpgrade.AtelierPrice;
                using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                    file.StoreString(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        save_version = 10, day = clothing ? 4 : 5, score = 1000, energy_remaining = 2,
                        quota_stage = clothing ? 0 : 1, checkpoint_day = clothing ? 1 : 5,
                        day_remaining_seconds = 120, music_workshop_unlocked = true,
                        stored_items = Enumerable.Range(0, 10).ToArray()
                    }));
                game = await CreateGame(scene);
                game.EndDay();
                game.EndDay();
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.Options.Contains(choice) && card.Options.Count == 3,
                    $"Price card {choice} must be reachable: day={game.CurrentDay}, failed={game.Failed}, pending={game.DayUpgrades.Pending}, options={string.Join(',', card.Options)}");
                if (clothing && OS.GetCmdlineUserArgs().Contains("--day-card-preview"))
                    Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-price-upgrade-preview.png"));
                await ClickControl(game, $"DayTransitionCard/Root/Center/Card/Content/Choices/{choice}/Content/Choose");
                Check(!Paused && !game.DayTransitionOpen, "Selecting a price card must apply it and resume the day");
                foreach (int index in Enumerable.Range(0, 10))
                {
                    bool boosted = WorkshopItemCatalog.Items[index].WorkshopId == (clothing ? "atelier" : "music");
                    int expected = boosted ? (int)Math.Round(TradeScene.GetItemBasePrice(index) * 1.2m, MidpointRounding.AwayFromZero) : TradeScene.GetItemBasePrice(index);
                    Check(game.GetItemPrice(index) == expected, "A price card must affect all rarities of only its category, including existing goods");
                    var slot = (SlotUI)game.GetNode<InventoryUI>("HUD/InventoryUI").Container.GetChild(index);
                    Check(slot.ItemInfo.EndsWith($"Стоимость: {expected}$"), "Existing inventory tooltips must refresh to the improved price");
                }
                foreach (string room in new[] { "Atelier", "MusicWorkshop" })
                {
                    var panel = game.GetNode<RarityPanel>($"World/{room}/RarityPanel");
                    panel.RefreshChances();
                    int[] baseline = room == "Atelier" ? new[] { 30, 50, 80, 120, 200 } : new[] { 45, 75, 120, 180, 300 };
                    bool boosted = (room == "Atelier") == clothing;
                    for (int rarity = 0; rarity < 5; rarity++)
                    {
                        var label = panel.Values.GetNode<Label>($"Price{rarity}");
                        int expected = boosted ? (int)(baseline[rarity] * 1.2m) : baseline[rarity];
                        Check(label.Text == $"{expected}$" && label.GetThemeColor("font_color") == (boosted ? new Color("208447") : Colors.Black),
                            "Craft tables must show boosted prices in green and the other category in neutral color");
                    }
                    if (clothing && room == "Atelier" && OS.GetCmdlineUserArgs().Contains("--day-card-preview"))
                    {
                        panel.Show();
                        await ToSignal(this, SignalName.ProcessFrame);
                        await ToSignal(this, SignalName.ProcessFrame);
                        Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-price-table-preview.png"));
                        panel.Hide();
                    }
                }
                int item = clothing ? 0 : 2;
                SelectInventorySlot(game, item);
                game.PlaceItemInShowcase(0);
                game._Process(game.BuyerCountdown + 0.001);
                Check(game.TradeActive && game.ItemIndex == item && game.OfferedPrice == TradeScene.PriceForPercent(game.GetItemPrice(item), game.Percentage),
                    "The buyer offer must use the improved category price");
                int offer = game.OfferedPrice;
                int cash = game.Score;
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(game.TradeActive && game.OfferedPrice == offer, "Reload must retain an active trade with the improved price");
                game.AcceptTrade();
                game.AcceptTrade();
                Check(game.Score == cash + offer, "Selling an improved item must credit its offer exactly once");
                int beforeCraft = game.Score;
                if (clothing) CraftAtelier(game); else CraftMusic(game);
                Check(game.Score == beforeCraft - (clothing ? 90 : 140), "Price upgrades must leave production costs unchanged");
                foreach (int index in game.StoredItems.TakeLast(3))
                    Check(game.GetItemPrice(index) == (int)(TradeScene.GetItemBasePrice(index) * 1.2m), "Newly crafted goods must receive the category price too");
                game.EndDay();
                game.EndDay();
                card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                card.Choose(choice);
                Check(game.GetItemPrice(item) == (int)(TradeScene.GetItemBasePrice(item) * 1.4m), "Repeated price cards must add twenty percentage points rather than compound");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(game.GetItemPrice(item) == (int)(TradeScene.GetItemBasePrice(item) * 1.4m), "Stacked price bonuses must survive reload");
                game.ReturnToCheckpoint();
                Check(game.GetItemPrice(item) == TradeScene.GetItemBasePrice(item), "Checkpoint rollback must remove price cards selected after that checkpoint");
                game.NewGame();
                Check(game.DayUpgrades.AtelierPriceLevel == 0 && game.DayUpgrades.MusicPriceLevel == 0, "New game must clear both category price bonuses");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = null;
            }
            using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":10,\"day\":4,\"score\":1000,\"atelier_price_level\":1,\"music_price_level\":1}");
            game = await CreateGame(scene);
            game.EndDay(); game.EndDay();
            game.GetNode<DayTransitionCard>("DayTransitionCard").Choose(DayUpgrade.AtelierPrice);
            game.ReturnToCheckpoint();
            Check(game.DayUpgrades.AtelierPriceLevel == 1 && game.DayUpgrades.MusicPriceLevel == 1,
                "The day-five checkpoint must capture both price bonus levels before the next selection");
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(language);
            Root.Size = size;
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Category price cards, tables, inventory, sale, stacking, save and checkpoint checks passed");
    }

    private async Task TestLocalization()
    {
        const string preferenceTest = "user://language_preference_test.cfg";
        bool hadSettings = FileAccess.FileExists(GameLocalization.SettingsPath);
        byte[] settingsBackup = hadSettings ? FileAccess.GetFileAsBytes(GameLocalization.SettingsPath) : null;
        Vector2I windowSize = Root.Size;
        MainMenu menu = null;
        TradeScene game = null;
        try
        {
            if (FileAccess.FileExists(preferenceTest)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(preferenceTest));
            GameLocalization.LoadPreference(preferenceTest);
            Check(GameLocalization.Language == "ru", "Missing language settings must default to Russian");
            var preservedSettings = new ConfigFile();
            preservedSettings.SetValue("audio", "volume", 0.5);
            Check(preservedSettings.Save(preferenceTest) == Error.Ok, "Test settings must save");
            Check(GameLocalization.SelectLanguage("en", preferenceTest) == Error.Ok, "English preference must save");
            preservedSettings.Load(preferenceTest);
            Check(preservedSettings.GetValue("audio", "volume").AsDouble() == 0.5,
                "Changing language must preserve other preference sections");
            TranslationServer.SetLocale("ru");
            GameLocalization.LoadPreference(preferenceTest);
            Check(GameLocalization.Language == "en", "Loading settings must restore the saved language");
            Check(GameLocalization.SelectLanguage("fr", preferenceTest) == Error.InvalidParameter && GameLocalization.Language == "en",
                "Unsupported language selection must leave the language unchanged");
            using (var invalid = FileAccess.Open(preferenceTest, FileAccess.ModeFlags.Write))
                invalid.StoreString("[interface]\nlanguage=\"unsupported\"\n");
            GameLocalization.LoadPreference(preferenceTest);
            Check(GameLocalization.Language == "ru", "Unsupported stored language must fall back to Russian");

            var catalog = GD.Load<Translation>("res://localization/en.tres");
            foreach (string source in catalog.GetMessageList())
            {
                string translated = catalog.GetMessage(source).ToString();
                Check(translated.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(translated, "[А-Яа-яЁё]"),
                    $"English catalog entry must be complete: {source}");
                var sourceArgs = System.Text.RegularExpressions.Regex.Matches(source, @"\{\d+\}").Select(match => match.Value).OrderBy(value => value);
                var translatedArgs = System.Text.RegularExpressions.Regex.Matches(translated, @"\{\d+\}").Select(match => match.Value).OrderBy(value => value);
                Check(sourceArgs.SequenceEqual(translatedArgs), $"Format arguments must match: {source}");
            }

            var menuScene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
            menu = menuScene.Instantiate<MainMenu>();
            Root.AddChild(menu);
            menu.OnSettingsPressed();
            const string settings = "SettingsMenuContainer/Settings/";
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                Root.Size = size;
                foreach (string language in new[] { "en", "ru" })
                {
                    string button = language == "en" ? "EnglishButton" : "RussianButton";
                    await ClickControl(menu, settings + "LanguageRow/" + button);
                    Check(GameLocalization.Language == language && menu.GetNode<Button>(settings + "LanguageRow/" + button).ButtonPressed,
                        "Language buttons must switch immediately and mark the selected language");
                    var title = menu.GetNode<Label>(settings + "SettingsTitle");
                    Check(title.Tr(title.Text) == (language == "en" ? "SETTINGS" : "НАСТРОЙКИ"),
                        "The visible settings title must translate without reopening the menu");
                    foreach (string path in new[] { "LanguageRow/RussianButton", "LanguageRow/EnglishButton", "FullscreenButton", "BackButton" })
                    {
                        Rect2 bounds = menu.GetNode<Control>(settings + path).GetGlobalRect();
                        Check(new Rect2(Vector2.Zero, Root.GetVisibleRect().Size).Encloses(bounds),
                            $"Settings controls must fit both window sizes and languages: {path}");
                    }
                }
            }
            await ClickControl(menu, settings + "LanguageRow/EnglishButton");
            TranslationServer.SetLocale("ru");
            GameLocalization.LoadPreference();
            Check(GameLocalization.Language == "en", "The actual GUI selection must survive preference reload");
            menu.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            menu = menuScene.Instantiate<MainMenu>();
            Root.AddChild(menu);
            Check(menu.GetNode<Button>(settings + "LanguageRow/EnglishButton").ButtonPressed,
                "A recreated menu must retain the selected language");
            menu.Hide();

            var gameScene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
            game = await CreateGame(gameScene);
            game.NewGame();
            CraftAtelier(game);
            Check(game.GetNode<Label>("HUD/Root/ScorePanel/ScoreLabel").Text == "Cash: $130" &&
                  game.GetNode<Label>("HUD/Root/ProgressPanel/Content/DayLabel").Text == "Day: 1 / 7",
                "Dynamic cash and day labels must use English with their actual values");
            Check(game.GetNode<Label>("HUD/Root/ProgressPanel/Content/StatusLabel").Text == "New game.",
                "Saved Russian status messages must display in English");
            var slot = (SlotUI)game.GetNode<InventoryUI>("HUD/InventoryUI").Container.GetChild(0);
            Check(slot.ItemInfo.Contains("Rarity:") && slot.ItemInfo.Contains("Value: $") &&
                  !System.Text.RegularExpressions.Regex.IsMatch(slot.ItemInfo, "[А-Яа-яЁё]"),
                "Inventory tooltip names, rarity and value must all translate");
            var heading = game.GetNode<RarityPanel>("World/Atelier/RarityPanel").Values.GetNode<Label>("Heading0");
            Check(heading.Tr(heading.Text) == "Rarity", "Rarity table headings must translate");
            SelectInventorySlot(game, 0);
            game.PlaceItemInShowcase(0);
            Check(!System.Text.RegularExpressions.Regex.IsMatch(game.GetNode<Label>("World/ShowroomLeft/Showcases/Slot1/ItemLabel").Text, "[А-Яа-яЁё]"),
                "An occupied showcase must display the localized item name");
            game._Process(game.BuyerCountdown + 0.01);
            Check(game.GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/OfferLabel").Text == $"You get: ${game.OfferedPrice}",
                "Offer text must translate while retaining the actual offer value");
            string saveBefore = FileAccess.GetFileAsString(SavePath);
            int score = game.Score;
            int item = game.ItemIndex;
            TranslationServer.SetLocale("ru");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.GetNode<Label>("HUD/Root/ScorePanel/ScoreLabel").Text == $"Счёт: {score}$" &&
                  game.GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/OfferLabel").Text == $"Получим: {game.OfferedPrice}$",
                "Switching back to Russian must refresh dynamic HUD and trade text");
            TranslationServer.SetLocale("en");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.Score == score && game.ItemIndex == item && game.TradeActive && FileAccess.GetFileAsString(SavePath) == saveBefore,
                "Changing language must not mutate gameplay or rewrite the save");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            var resume = game.GetNode<Button>("PauseMenu/Root/Overlay/Center/Panel/Content/Menu/ResumeButton");
            Check(resume.Tr(resume.Text) == "Resume", "Pause controls must use the selected language");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = await CreateGame(gameScene);
            Check(game.TradeActive && game.ItemIndex == item &&
                  game.GetNode<Label>("HUD/Root/ProgressPanel/Content/StatusLabel").Text == "New game.",
                "Loading a gameplay save in English must preserve the trade and translate its Russian status");
            Check(GameLocalization.Status("Не хватает 17$ на квоту. Вернитесь к контрольной точке.") ==
                  "You need $17 more for the quota. Return to the checkpoint.",
                "Legacy interpolated quota messages must retain their saved amount in English");
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (menu != null && GodotObject.IsInstanceValid(menu)) menu.QueueFree();
            if (hadSettings)
            {
                using var settings = FileAccess.Open(GameLocalization.SettingsPath, FileAccess.ModeFlags.Write);
                settings.StoreBuffer(settingsBackup);
            }
            else if (FileAccess.FileExists(GameLocalization.SettingsPath))
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GameLocalization.SettingsPath));
            if (FileAccess.FileExists(preferenceTest)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(preferenceTest));
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            Root.Size = windowSize;
            TranslationServer.SetLocale("ru");
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Russian/English UI, settings persistence, catalog, layout and save compatibility checks passed");
    }

    private async Task TestMainMenu()
    {
        var scene = GD.Load<PackedScene>("res://scenes/main_menu.tscn")
            ?? throw new Exception("Main menu must load");
        var menu = scene.Instantiate<MainMenu>();
        Root.AddChild(menu);
        CurrentScene = menu;
        await ToSignal(this, SignalName.ProcessFrame);
        Check(ProjectSettings.GetSetting("gui/theme/custom").AsString() == "res://themes/game_theme.tres",
            "The art team's font must be configured as the global UI theme");
        var gameFont = GD.Load<Theme>("res://themes/game_theme.tres").DefaultFont;
        foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя")
            Check(gameFont.HasChar(letter), $"The game font must include English and Russian letters: {letter}");
        Check(menu.GetNode<CheckButton>("SettingsMenuContainer/Settings/FullscreenButton").GetThemeFont("font") == gameFont,
            "Explicit menu fonts must use the same completed font as the global UI theme");
        Check(menu.GameScene.ResourcePath == "res://scenes/trade_scene.tscn", "Play must open the trade scene");
        foreach (string path in new[]
        {
            "MainMenuContainer/Menu/PlayButton", "MainMenuContainer/Menu/SettingsButton",
            "MainMenuContainer/Menu/QuitButton", "SettingsMenuContainer/Settings/BackButton"
        })
            Check(menu.GetNode<TextureButton>(path).TextureNormal != null, $"Menu button art must load: {path}");
        var mainView = menu.GetNode<CenterContainer>("MainMenuContainer");
        var settingsView = menu.GetNode<CenterContainer>("SettingsMenuContainer");
        Check(settingsView.GetNode<TextureRect>("SettingsFrame").Texture != null,
            "Settings must use the art team's finished panel");
        menu.OnSettingsPressed();
        Check(!mainView.Visible && settingsView.Visible, "Settings must open");
        CheckFullscreenText(menu.GetNode<CheckButton>("SettingsMenuContainer/Settings/FullscreenButton"));
        menu.OnBackPressed();
        Check(mainView.Visible && !settingsView.Visible, "Back must return to the main menu");
        menu.OnPlayPressed();
        Check(menu.ActiveDialogue != null && !mainView.Visible && CurrentScene == menu,
            "Play must start the prologue without creating a running game");
        var dialogue = menu.ActiveDialogue;
        menu.OnPlayPressed();
        Check(menu.ActiveDialogue == dialogue, "Repeated Play must not create duplicate dialogues");
        var dialogueStyle = dialogue.GetNode<PanelContainer>("Panel").GetThemeStylebox("panel") as StyleBoxTexture;
        Check(dialogueStyle?.Texture?.ResourcePath == "res://art/ui/dialog_panel.png",
            "Dialogue panel must use the supplied sprite as its background");
        Check(dialogue.Theme.DefaultFont == gameFont &&
              dialogue.GetNode<Label>("Panel/Content/Speaker").GetThemeFont("font") == gameFont &&
              dialogue.GetNode<RichTextLabel>("Panel/Content/Text").GetThemeFont("normal_font") == gameFont,
            "Dialogue labels and speech must use the project's shared font");
        Check(!dialogue.HasNode("Panel/Content/Actions") && !dialogue.HasNode("Panel/Content/Hint") &&
              dialogue.GetNode<Control>("Panel/Content").GetChildCount() == 2,
            "Only speaker and speech must remain in the cloud, with no buttons, counter or hints");
        Check(!dialogue.HasNode("Panel/Content/Rule") &&
              dialogue.GetNode<PanelContainer>("Panel").Material is ShaderMaterial,
            "The divider must be removed and the cloud edges must fade smoothly");
        Vector2I dialogueViewport = Root.Size;
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
        {
            Root.Size = size;
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            var panel = dialogue.GetNode<Control>("Panel");
            var text = dialogue.GetNode<RichTextLabel>("Panel/Content/Text");
            Check(panel.Size.Y >= 260 && panel.Size.Y <= 280 &&
                  panel.Size.X <= Root.GetVisibleRect().Size.X * 0.79f &&
                  text.GlobalPosition.X >= panel.GlobalPosition.X + 112 &&
                  text.GetGlobalRect().End.X <= panel.GetGlobalRect().End.X - 112 &&
                  text.GetGlobalRect().End.Y <= panel.GetGlobalRect().End.Y - 64,
                "The cloud must retain horizontal and bottom text padding at both viewport sizes");
            Check(new Rect2(Vector2.Zero, Root.GetVisibleRect().Size).Encloses(panel.GetGlobalRect()) &&
                  panel.GetGlobalRect().Encloses(text.GetGlobalRect()) && text.Size.Y > 40 &&
                  (text.ScrollActive || text.GetContentHeight() <= text.Size.Y) &&
                  text.GetThemeFontSize("normal_font_size") == 20,
                "Speech must retain its font size, using scrolling when needed inside the cloud");
        }
        Root.Size = dialogueViewport;
        await ToSignal(this, SignalName.ProcessFrame);
        Check(dialogue.Session.Count == 24 && dialogue.Session.Current.Speaker == "Покупательница",
            "The prologue must begin directly with the customer, without the opening storyboard");
        await ClickControl(dialogue, "Panel/Content/Text");
        Check(!dialogue.IsRevealing && dialogue.Session.Index == 0,
            "First click must reveal text without skipping the storyboard");
        string firstPage = dialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text;
        dialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
        Check(dialogue.IsRevealing && (dialogue.Session.Index > 0 ||
              dialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text != firstPage),
            "Space must advance the fully revealed page, including continuation of a long speech");
        int heldIndex = dialogue.Session.Index;
        string heldText = dialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text;
        dialogue._Input(new InputEventKey { Pressed = true, Echo = true, Keycode = Key.Space });
        Check(dialogue.Session.Index == heldIndex && dialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text == heldText,
            "Held keys must not skip dialogue");
        dialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene == menu && menu.ActiveDialogue == null && mainView.Visible,
            "Escape must return to the menu without quitting or starting gameplay");
        menu.OnPlayPressed();
        Check(menu.ActiveDialogue.Session.Index == 0, "A cancelled prologue must restart from its first frame");
        Root.Size = new Vector2I(960, 540);
        await CompletePrologue(menu);
        Root.Size = dialogueViewport;
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene is TradeScene, "Play must enter the game");
        var game = (TradeScene)CurrentScene;
        int score = game.Score;
        int[] stored = game.StoredItems;
        int[] showcases = game.ShowcaseItems;
        bool active = game.TradeActive;
        int activeItem = game.ItemIndex;
        game.SetProcess(false);
        await ClickControl(game, "PauseMenu/Root/PauseButton");
        Check(Paused, "Returning to the menu must begin from pause");
        await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/MainMenuButton");
        Check(CurrentScene is MainMenu && !Paused && !GodotObject.IsInstanceValid(game),
            "The main menu button must change scenes, free the game and release pause");
        menu = (MainMenu)CurrentScene;
        await ClickControl(menu, "MainMenuContainer/Menu/SettingsButton");
        Check(menu.GetNode<Control>("SettingsMenuContainer").Visible,
            "The returned main menu must respond to input");
        menu.OnBackPressed();
        menu.OnPlayPressed();
        await CompletePrologue(menu);
        await ToSignal(this, SignalName.ProcessFrame);
        game = (TradeScene)CurrentScene;
        game.SetProcess(false);
        Check(!Paused && game.Score == score && game.StoredItems.SequenceEqual(stored) &&
              game.ShowcaseItems.SequenceEqual(showcases) && game.TradeActive == active && game.ItemIndex == activeItem,
            "Reentering from the menu must retain saved goods and the active trade");
        GD.Print("Main menu checks passed");
    }

    private async Task CompletePrologue(MainMenu menu)
    {
        var dialogue = menu.ActiveDialogue;
        Check(dialogue != null, "Play must always open the prologue");
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        int count = dialogue.Session.Count;
        bool checkedScrolling = false;
        for (int index = 0; index < count; index++)
        {
            Check(CurrentScene == menu && dialogue.Session.Index == index,
                "Gameplay must not begin before all dialogue lines have been acknowledged");
            string original = dialogue.Session.Current.Text;
            string reconstructed = "";
            int pages = 0;
            while (!dialogue.Session.IsComplete && dialogue.Session.Index == index)
            {
                Check(++pages < 50, "Pagination must always make progress");
                if (dialogue.IsRevealing) dialogue.Advance();
                var text = dialogue.GetNode<RichTextLabel>("Panel/Content/Text");
                var speaker = dialogue.GetNode<Label>("Panel/Content/Speaker");
                Check(speaker.Visible == !string.IsNullOrWhiteSpace(dialogue.Session.Current.Speaker) &&
                      speaker.Text != "Раскадровка", "Actions must not display a narration heading");
                Check(text.GetThemeFontSize("normal_font_size") == 20 &&
                      (text.ScrollActive || text.GetContentHeight() <= text.Size.Y),
                    "Every page must retain font size, allowing scrolling for long sentences");
                Check(text.GetVScrollBar().GetThemeStylebox("grabber") is StyleBoxFlat,
                    "Long dialogue must use a styled scrollbar");
                if (!checkedScrolling && text.ScrollActive && text.GetContentHeight() > text.Size.Y)
                {
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    var scrollbar = text.GetVScrollBar();
                    Check(scrollbar.IsVisibleInTree() && scrollbar.MaxValue > scrollbar.Page,
                        "Overflowing sentences must expose a usable scroll range");
                    string page = text.Text;
                    dialogue._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left,
                        Position = scrollbar.GetGlobalRect().GetCenter() });
                    Check(dialogue.Session.Index == index && text.Text == page,
                        "Clicking the scrollbar must not advance the dialogue");
                    scrollbar.Value = scrollbar.MaxValue;
                    Check(scrollbar.Value > 0, "The end of the long sentence must be reachable by scrolling");
                    checkedScrolling = true;
                }
                reconstructed += text.Text;
                if (reconstructed.Length < original.Length)
                {
                    string ending = text.Text.TrimEnd().TrimEnd('»', '”', '"', '\'', ')', ']', '*');
                    Check(ending.Length > 0 && ".!?…".Contains(ending[^1]),
                        "A continuing speech must end on a complete sentence");
                }
                dialogue._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left });
            }
            Check(reconstructed == original, "All pages must reproduce the entire original speech without loss or duplication");
        }
        dialogue.Advance(); // Completed sessions must not start the game twice.
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestLongSentenceScroll()
    {
        Vector2I originalViewport = Root.Size;
        const string sequencePath = "user://dialogue_scroll_sequence_test.json";
        var examples = new[]
        {
            new { speaker = "Кастелло", text = "Знаешь же, мы стали работать по всему миру, последняя поставка в Великобританию на практически 11 тонн прошла все проверки и мы купаемся в золоте." },
            new { speaker = "Френки", text = "Здесь 150 лет назад шили хлопком наших плантаций, а когда всех негров распустили, мы всей семьёй кредитами выбивали новые земли, скупали шёлк, лён, кожу животных." },
            new { speaker = "Кастелло", text = "Проиграешь - я вышибу твои мозги и выкуплю всю мастерскую по половинной стоимости рынка, а твои дети извинятся перед всей моей семьёй за личное оскорбление в мою сторону." }
        };
        using (var file = FileAccess.Open(sequencePath, FileAccess.ModeFlags.Write))
            file.StoreString(System.Text.Json.JsonSerializer.Serialize(examples));
        try
        {
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1096, 616), new Vector2I(1280, 720), new Vector2I(1600, 900) })
            {
                Root.Size = size;
                var sequence = new DialoguePlayer { DialoguePath = sequencePath };
                Root.AddChild(sequence);
                try
                {
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    foreach (var example in examples)
                    {
                        if (sequence.IsRevealing) sequence.Advance();
                        var label = sequence.GetNode<RichTextLabel>("Panel/Content/Text");
                        Check(label.Text == example.text, "Screenshot examples must remain whole");
                        if (label.GetLineCount() <= 3)
                            Check(!label.ScrollActive && label.GetContentHeight() <= label.Size.Y,
                                $"Screenshot dialogue must fit three lines at {size}: {label.GetContentHeight()}/{label.Size.Y}");
                        Root.Size = size + new Vector2I(8, 8);
                        await ToSignal(this, SignalName.ProcessFrame);
                        Root.Size = size;
                        await ToSignal(this, SignalName.ProcessFrame);
                        await ToSignal(this, SignalName.ProcessFrame);
                        if (sequence.IsRevealing) sequence.Advance();
                        if (label.GetLineCount() <= 3)
                            Check(!label.ScrollActive && !label.GetVScrollBar().IsVisibleInTree(),
                                "Resizing must not leave a scrollbar on a three-line page");
                        sequence.Advance();
                    }
                }
                finally { sequence.QueueFree(); }
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        finally
        {
            Root.Size = originalViewport;
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(sequencePath));
        }
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
        {
            Root.Size = size;
            const string fittingPath = "user://fitting_sentence_scroll_test.json";
            string fittingText = size.X == 1280
                ? "Знаешь же, мы стали работать по всему миру, последняя поставка в Великобританию на практически 11 тонн прошла все проверки и мы купаемся в золоте."
                : "Первая строка\nВторая строка\nТретья строка.";
            using (var file = FileAccess.Open(fittingPath, FileAccess.ModeFlags.Write))
                file.StoreString(System.Text.Json.JsonSerializer.Serialize(new[] { new { speaker = "Кастелло", text = fittingText } }));
            var fittingDialogue = new DialoguePlayer { DialoguePath = fittingPath };
            Root.AddChild(fittingDialogue);
            try
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                fittingDialogue.Advance();
                var fittingLabel = fittingDialogue.GetNode<RichTextLabel>("Panel/Content/Text");
                Check(fittingLabel.GetLineCount() == 3 && fittingLabel.GetContentHeight() <= fittingLabel.Size.Y &&
                      !fittingLabel.ScrollActive && !fittingLabel.GetVScrollBar().IsVisibleInTree(),
                    $"Three fitting lines must not enable scrolling or reserve scrollbar width: viewport={size}, lines={fittingLabel.GetLineCount()}, height={fittingLabel.GetContentHeight()}/{fittingLabel.Size.Y}, active={fittingLabel.ScrollActive}, visible={fittingLabel.GetVScrollBar().IsVisibleInTree()}");
            }
            finally
            {
                fittingDialogue.QueueFree();
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(fittingPath));
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
        {
            Root.Size = size;
            const string path = "user://long_sentence_scroll_test.json";
            string sentence = string.Join(" ", Enumerable.Repeat("Длинное предложение для проверки прокрутки", 20)) + ".";
            using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
                file.StoreString(System.Text.Json.JsonSerializer.Serialize(new[] { new { speaker = "Френки", text = sentence } }));
            var dialogue = new DialoguePlayer { DialoguePath = path };
            Root.AddChild(dialogue);
            try
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                dialogue.Advance();
                var text = dialogue.GetNode<RichTextLabel>("Panel/Content/Text");
                await ToSignal(this, SignalName.ProcessFrame);
                var bar = text.GetVScrollBar();
                Check(text.Text == sentence && text.ScrollActive && text.GetThemeFontSize("normal_font_size") == 20 &&
                      bar.IsVisibleInTree() && bar.MaxValue > bar.Page,
                    "A very long sentence must stay whole with the normal font and a usable scrollbar");
                dialogue._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left,
                    Position = bar.GetGlobalRect().GetCenter() });
                Check(!dialogue.Session.IsComplete, "Scrollbar interaction must not finish the dialogue");
                bar.Value = bar.MaxValue;
                Check(bar.Value > 0, "The end of the sentence must be reachable");
            }
            finally
            {
                dialogue.QueueFree();
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        Root.Size = originalViewport;
    }

    private static void CheckFullscreenText(CheckButton button)
    {
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            var style = button.GetThemeStylebox(state) as StyleBoxFlat;
            Check(style != null && style.BgColor.R > 0.8f && style.BgColor.G > 0.8f && style.BgColor.B > 0.8f,
                "Fullscreen text must have a light opaque background in every state");
        }
        foreach (bool enabled in new[] { false, true })
        {
            button.SetPressedNoSignal(enabled);
            foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color",
                         "font_hover_pressed_color", "font_focus_color", "font_disabled_color" })
            {
                Color color = button.GetThemeColor(state);
                Check(color.R < 0.3f && color.G < 0.3f && color.B < 0.3f && color.A == 1,
                    "Fullscreen text must stay dark and legible in all interaction and toggle states");
            }
        }
    }

    private static void AdvanceDayForTest(TradeScene game)
    {
        if (!game.DayClosed) game.EndDay();
        game.EndDay();
        var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
        if (card.IsOpen) card.Skip();
    }

    private async Task<TradeScene> CreateGame(PackedScene scene)
    {
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = SavePath;
        Root.AddChild(game);
        game.SetProcess(false);
        game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
        await ToSignal(this, SignalName.ProcessFrame);
        return game;
    }

    private static void CheckInventoryTooltips(TradeScene game)
    {
        var container = game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer");
        int[] items = game.StoredItems;
        for (int i = 0; i < items.Length; i++)
        {
            var slot = (SlotUI)container.GetChild(i);
            Check(slot.ItemInfo == ExpectedItemTooltip(items[i]),
                "Inventory tooltips must follow each current item's name, rarity and price after creation, movement and reload");
            var inventoryUi = game.GetNode<InventoryUI>("HUD/InventoryUI");
            slot.EmitSignal(Control.SignalName.MouseEntered);
            Check(inventoryUi.ItemPopup.Visible && inventoryUi.VisibleItemInfo == ExpectedItemTooltip(items[i]),
                "The immediate popup must show fresh content after creation, movement and reload");
            slot.EmitSignal(Control.SignalName.MouseExited);
            Check(!inventoryUi.ItemPopup.Visible, "Item information must hide immediately on mouse exit");
            Check(slot.GetNode<TextureRect>("Icon").MouseFilter == Control.MouseFilterEnum.Ignore,
                "Inventory artwork must allow its slot to receive hover and click input");
        }
    }

    private static void SelectInventorySlot(TradeScene game, int index)
    {
        var container = game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer");
        ((Button)container.GetChild(index)).EmitSignal(Button.SignalName.Pressed);
    }

    private static void CheckSpriteInside(Rect2 worldBounds, Sprite2D sprite, string message)
    {
        Texture2D texture = sprite.Texture ?? throw new Exception($"{message}: texture is missing");
        Check(!sprite.Centered, $"{message}: contract expects a top-left anchored background");
        var spriteBounds = new Rect2(sprite.GlobalPosition, texture.GetSize() * sprite.GlobalScale);
        Check(worldBounds.Encloses(spriteBounds),
            $"{message}; world={worldBounds}, sprite={spriteBounds}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
