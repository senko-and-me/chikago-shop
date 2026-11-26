using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Crafting;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestPolishingStart()
    {
        Vector2I originalSize = Root.Size;
        string locale = TranslationServer.GetLocale();
        TradeScene game = null;
        try
        {
            foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540), new Vector2I(1920, 1080) })
            foreach (string language in new[] { "ru", "en" })
            {
                Root.Size = size;
                TranslationServer.SetLocale(language);
                game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
                game.SavePath = "";
                game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
                Root.AddChild(game); game.SetProcess(false);
                game.EndDay(); game.Score = 10000; game.TryUnlockMusicWorkshop();
                var craft = game.GetNode<EmbroideryGame>("PolishingGame");
                game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar");
                Check(craft.IsOpen, "Unlocked room must open polishing");
                await ToSignal(this, SignalName.ProcessFrame);
                var cloth = craft.GetNode<Sprite2D>("Surface/PolishingCursor");
                Check(Input.MouseMode == Input.MouseModeEnum.Visible && !cloth.Visible,
                    "Ready buttons need the native pointer: the centered cloth must not hide their click hotspot");
                await CancelLegacyCraft(game, "PolishingGame");
                Check(!craft.IsOpen && !Paused && Input.MouseMode == Input.MouseModeEnum.Visible,
                    "Cancel must restore the pointer and leave the preview reusable");
                game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar");
                await ToSignal(this, SignalName.ProcessFrame);
                int money = game.Score, energy = game.EnergyRemaining;
                var button = craft.GetNode<Button>("Surface/Panel/Buttons/Action");
                Vector2 point = button.GetGlobalTransformWithCanvas() * (button.Size / 2);
                Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
                await ToSignal(this, SignalName.ProcessFrame);
                Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                    ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
                for (int i = 0; i < 5; i++) await ToSignal(this, SignalName.ProcessFrame);
                Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                    ButtonIndex = MouseButton.Left, Pressed = false }, true);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(craft.ActiveSession.State == EmbroideryState.Playing,
                    $"Normal held mouse click must start music mini-game ({size}, {language})");
                Check(Input.MouseMode == Input.MouseModeEnum.Hidden && cloth.Visible,
                    "Playing must switch from the UI pointer to the polishing cloth");
                Check(game.StoredItems.Length == 1,
                    "Start must grant the paid batch exactly once");
                Check(game.Score == money - WorkshopItemCatalog.ItemCraftCost(craft.PreparedItemIndex.Value) && game.EnergyRemaining == energy,
                    "Start must charge its price once without changing the legacy energy counter");
                var board = craft.GetNode<PolishingSurface>("Surface/Polishing");
                var cell = craft.Polishing.Cells.First(value => value.Inside && craft.Polishing.Inside(value.X + 14, value.Y));
                Vector2 left = board.LogicalToScreen(new Vector2((float)cell.X, (float)cell.Y));
                Vector2 right = board.LogicalToScreen(new Vector2((float)cell.X + 14, (float)cell.Y));
                Root.PushInput(new InputEventMouseMotion { Position = left, GlobalPosition = left }, true);
                Root.PushInput(new InputEventMouseButton { Position = left, GlobalPosition = left,
                    ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
                for (int i = 0; i < 12; i++)
                {
                    await ToSignal(this, SignalName.ProcessFrame);
                    Vector2 stroke = i % 2 == 0 ? right : left;
                    Root.PushInput(new InputEventMouseMotion { Position = stroke, GlobalPosition = stroke,
                        ButtonMask = MouseButtonMask.Left }, true);
                }
                Root.PushInput(new InputEventMouseButton { Position = left, GlobalPosition = left,
                    ButtonIndex = MouseButton.Left, Pressed = false }, true);
                Check(craft.Polishing.Progress > 0, "Held mouse strokes after Start must polish the instrument");
                craft.FinishForTesting(true);
                Check(Input.MouseMode == Input.MouseModeEnum.Visible && !cloth.Visible,
                    "Collect must immediately restore the native pointer even before the next frame");
                await ClickControl(game, "PolishingGame/Surface/Panel/Buttons/Action");
                Check(!craft.IsOpen && !Paused, "Collect must close polishing and restore gameplay");
                game.QueueFree(); game = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        finally
        {
            Paused = false;
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(locale);
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }

    private async Task TestPolishingReward()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = "";
        game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        Root.AddChild(game); game.SetProcess(false);
        const string savePath = "user://polishing_reward_test.json";
        try
        {
            var craft = game.GetNode<EmbroideryGame>("PolishingGame");
            craft.SetProcess(false);
            var board = craft.GetNode<PolishingSurface>("Surface/Polishing");
            int iteration = 0;
            foreach (int level in new[] { 0, 3 })
            foreach (var entry in WorkshopItemCatalog.Items.Select((item, index) => (item, index))
                .Where(entry => entry.item.WorkshopId == "music"))
            {
                game.NewGame(); game.EndDay(); game.Score = 10000; game.TryUnlockMusicWorkshop();
                game.DayUpgrades.Restore(false, DayUpgrade.None, musicLevel: level);
                Check(!craft.PreparedItemIndex.HasValue, "New game must clear the prior selection");
                game.GetItemFromMusicWorkshop();
                Check(game.Crafting.Select(entry.item.Kind), "Each music item must be selectable regardless of its rarity");
                Check(craft.IsOpen && craft.PreparedItemIndex == entry.index && board.InstrumentKind == entry.item.Kind,
                    "The selected item must determine the preview artwork and playable mask");
                int money = game.Score, energy = game.EnergyRemaining, count = game.StoredItems.Length;
                await CancelLegacyCraft(game, "PolishingGame");
                Check(game.Score == money && game.EnergyRemaining == energy && game.StoredItems.Length == count,
                    "Cancelling an unstarted selected preview must remain free");
                GD.Seed((ulong)++iteration);
                game.GetItemFromMusicWorkshop();
                Check(game.Crafting.Select(entry.item.Kind) && craft.PreparedItemIndex == entry.index,
                    "Random generator changes must never substitute the selected instrument");
                int cost = WorkshopItemCatalog.ItemCraftCost(entry.index);
                await ClickControl(game, "PolishingGame/Surface/Panel/Buttons/Action");
                Check(game.Score == money - cost && game.EnergyRemaining == energy &&
                    game.StoredItems.Length == count + 1 && game.StoredItems[count] == entry.index,
                    "Start must debit the selected price and grant precisely one displayed instrument");
                craft.Activate();
                Check(game.Score == money - cost && game.StoredItems.Length == count + 1,
                    "Duplicate start must not pay or grant twice");
                if (iteration % 2 == 0) craft.FinishForTesting(true);
                else craft._Process(craft.ActiveSession.RemainingSeconds + .1);
                Check(craft.ActiveSession.State == EmbroideryState.Finished &&
                    craft.GetNode<Label>("Surface/Panel/Quality").Text == $"{craft.ActiveSession.Quality}%" &&
                    game.StoredItems[count] == entry.index, "Success and timeout must preserve the exact chosen item");
                await ClickControl(game, "PolishingGame/Surface/Panel/Buttons/Action");
                craft.Activate();
                Check(!craft.IsOpen && !Paused && game.StoredItems.Length == count + 1 && game.Score == money - cost,
                    "Collect and activation after close must be idempotent");
            }
            game.SavePath = savePath; game.PersistProgress();
            int[] saved = game.StoredItems;
            int savedMoney = game.Score, savedEnergy = game.EnergyRemaining;
            game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame);
            game = scene.Instantiate<TradeScene>(); game.SavePath = savePath;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game); game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.StoredItems.SequenceEqual(saved) && game.Score == savedMoney && game.EnergyRemaining == savedEnergy &&
                !game.CraftMiniGameOpen && !game.Crafting.IsCatalogOpen && !Paused,
                "Reload must preserve selected goods without reopening or duplicating an attempt");
        }
        finally
        {
            Paused = false; GD.Randomize();
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (FileAccess.FileExists(savePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(savePath));
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }

    private async Task TestCraftContrast()
    {
        Vector2I originalSize = Root.Size;
        string originalLocale = TranslationServer.GetLocale();
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        try
        {
            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720), new Vector2I(1000, 800) })
            foreach (string locale in new[] { "ru", "en" })
            foreach (string name in new[] { "EmbroideryGame", "PolishingGame" })
            foreach (string state in new[] { "ready", "playing", "timeout", "success" })
            {
                Root.Size = size;
                TranslationServer.SetLocale(locale);
                game.NewGame();
                game.EndDay();
                game.Score = 10000;
                if (name == "PolishingGame") game.TryUnlockMusicWorkshop();
                var craft = game.GetNode<EmbroideryGame>(name);
                craft.Open(); SelectLegacyCraft(game, craft.IsPolishing ? "guitar" : "hat");
                craft.SetProcess(false);
                if (state != "ready") craft.Activate();
                if (state == "timeout")
                {
                    if (!craft.IsPolishing)
                        for (int i = 0; i < craft.Session.Points.Count / 2; i++)
                        {
                            var point = craft.Session.Points[i];
                            craft.Session.Hit(point.X, point.Y);
                        }
                    craft._Process(craft.ActiveSession.RemainingSeconds + 0.1);
                }
                if (state == "success") craft.FinishForTesting(true);
                var surface = craft.GetNode<EmbroiderySurface>("Surface");
                using var frame = await CapturePresentation();
                Vector2 scale = new Vector2(frame.GetWidth(), frame.GetHeight()) / Root.GetVisibleRect().Size;
                Color Pixel(Vector2 point)
                {
                    Vector2 p = surface.GetGlobalTransformWithCanvas() * point * scale;
                    return frame.GetPixel(Mathf.RoundToInt(p.X), Mathf.RoundToInt(p.Y));
                }
                Check(Pixel(surface.PanelRect.Position + new Vector2(32, 32)).R > 0.9f,
                    "Both craft modal backgrounds must stay light, including timeout and success screens");
                foreach (var label in surface.FindChildren("*", "Label", true, false).Cast<Label>().Where(value => value.IsVisibleInTree() && value.Text.Length > 0))
                {
                    Check(label.GetThemeColor("font_color") == Colors.Black, "Every craft label must use black text");
                    Rect2 rect = ScreenBounds(label);
                    int dark = 0;
                    for (int y = (int)(rect.Position.Y * scale.Y); y < (int)(rect.End.Y * scale.Y); y++)
                    for (int x = (int)(rect.Position.X * scale.X); x < (int)(rect.End.X * scale.X); x++)
                        if (frame.GetPixel(x, y).R < 0.25f) dark++;
                    Check(dark > 2, $"Rendered {name}/{label.Name} text must remain visibly dark ({state}, {locale}, {size})");
                }
                foreach (string stat in new[] { "Time", "Stitches", "Misses" })
                    Check(Pixel(CraftBounds(surface, surface.GetNode<Control>("Panel/Stat" + stat)).Position + new Vector2(10, 10)).R > 0.9f,
                        "Statistics must have light cards behind their black headings and values");
                Rect2 panel = surface.PanelRect, fabric = surface.FabricRect;
                var title = surface.GetNode<Label>("Panel/Title");
                var firstStat = surface.GetNode<Control>("Panel/StatTime");
                var lastStat = surface.GetNode<Control>("Panel/StatMisses");
                var hint = surface.GetNode<Label>("Panel/Hint");
                Check(CraftBounds(surface, title).End.X + 8 <= CraftBounds(surface, firstStat).Position.X &&
                    CraftBounds(surface, lastStat).End.X + 8 <= panel.End.X &&
                    CraftBounds(surface, firstStat).End.Y + 20 <= fabric.Position.Y,
                    "Title and three statistics must share a separated header above the workspace");
                Check(panel.Encloses(fabric.Grow(7)) && fabric.End.Y + 7 <= CraftBounds(surface, hint).Position.Y &&
                    surface.QualityTrackRect.Position.X >= fabric.End.X + 16 &&
                    CraftBounds(surface, surface.GetNode<Label>("Panel/Quality")).End.Y <= fabric.End.Y + 1,
                    "Workspace, quality sidebar and footer must stay separated inside the modal");
                var clockLayer = game.GetNode<CanvasLayer>("HUD/DayClock");
                var clockAction = game.GetNode<Button>("HUD/DayClock/Root/ClockActionButton");
                var clock = game.GetNode<Label>("HUD/DayClock/Root/DayTimer");
                Check(!surface.HasNode("DayTimer") && !surface.HasNode("DayHeading") &&
                    clockLayer.Layer > craft.Layer && clock.IsVisibleInTree() && clockAction.Disabled &&
                    surface.GetNode<Control>("Panel/StatTime").Visible,
                    "One shared clock must render above the craft modal, with input disabled and craft time preserved");
                int visibleClock = 0;
                Rect2 clockBounds = ScreenBounds(clock);
                for (int y = (int)clockBounds.Position.Y; y < (int)clockBounds.End.Y; y += 2)
                for (int x = (int)clockBounds.Position.X; x < (int)clockBounds.End.X; x += 2)
                {
                    if (panel.HasPoint(new Vector2(x, y))) continue;
                    Vector2 pixel = new Vector2(x, y) * scale;
                    if (frame.GetPixel((int)pixel.X, (int)pixel.Y).R > 0.8f) visibleClock++;
                }
                Check(visibleClock > 20, "Shared clock must remain visibly bright above modal dimming");
                double beforeEnd = game.DayRemainingSeconds;
                game.EndDay();
                Check(game.DayRemainingSeconds == beforeEnd && !game.DayClosed,
                    "Direct end-day calls must be ignored while crafting");
                bool expectsHint = state == "playing" || (state == "ready" && craft.IsPolishing);
                Check((hint.Text.Length > 0) == expectsHint, "Only the appropriate craft states must display controls guidance");
                if (state == "playing" && locale == "ru" && size.X == 960)
                    frame.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chikago-hf183-{name}-playing.png"));
                var track = surface.QualityTrackRect;
                foreach (float depth in new[] { 0.15f, 0.35f, 0.65f, 0.85f })
                {
                    float luminance = Pixel(track.Position + track.Size * new Vector2(0.75f, depth)).R;
                    bool filled = depth > 1 - craft.ActiveSession.Quality / 100f;
                    Check(filled ? luminance < 0.15f : luminance > 0.9f,
                        $"The quality meter must distinguish black fill from its light empty part ({name}, {state}, quality={craft.ActiveSession.Quality})");
                }
                foreach (string buttonName in new[] { "Action", "Cancel" })
                foreach (string color in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
                    Check(surface.GetNode<Button>("Panel/Buttons/" + buttonName).GetThemeColor(color) ==
                        (buttonName == "Action" ? Colors.White : Colors.Black),
                        "Primary actions must have white text on dark fill and Back must retain black text");
                var action = surface.GetNode<Button>("Panel/Buttons/Action");
                var cancel = surface.GetNode<Button>("Panel/Buttons/Cancel");
                if (state == "ready")
                {
                    Check(CraftBounds(surface, action).End.X < CraftBounds(surface, cancel).Position.X && action.HasFocus() &&
                        surface.PanelRect.Encloses(CraftBounds(surface, action)) && surface.PanelRect.Encloses(CraftBounds(surface, cancel)),
                        "Start must be left of Back, initially focused, with both buttons inside the modal");
                    Check(Pixel(CraftBounds(surface, action).Position + new Vector2(10, 10)).R < 0.35f &&
                        Pixel(CraftBounds(surface, cancel).Position + new Vector2(10, 10)).R > 0.8f,
                        "The rendered Start button must remain clearly darker than Back through the grayscale shader");
                    if (size.X == 960)
                        frame.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chikago-hf179-{name}-{locale}.png"));
                }
                else if (action.Visible)
                    Check(Mathf.Abs(CraftBounds(surface, action).GetCenter().X - surface.PanelRect.GetCenter().X) < 0.1f &&
                        surface.PanelRect.End.Y - CraftBounds(surface, action).End.Y >= 20,
                        "Collect must remain centered with a clear margin from the lower frame");
                if (locale == "ru" && size.X == 960 && state == "timeout")
                    frame.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chikago-hf166-{name}.png"));
                string closeButton = state == "ready" ? "Cancel" : "Action";
                if (state == "ready" && locale == "ru" && size.X == 960)
                {
                    int money = game.Score, energy = game.EnergyRemaining, count = game.StoredItems.Length;
                    Rect2 beforeStart = surface.FabricRect;
                    await ClickControl(game, name + "/Surface/Panel/Buttons/Action");
                    Check(surface.FabricRect == beforeStart, "Starting must keep the workspace and hit targets in place");
                    Check(craft.ActiveSession.State == EmbroideryState.Playing && !action.Visible && !cancel.Visible &&
                        game.Score < money && game.EnergyRemaining == energy && game.StoredItems.Length == count + 1,
                        "The highlighted left Start must begin the real game, charge once and hide both buttons");
                    craft.FinishForTesting(false);
                    closeButton = "Action";
                }
                await ClickControl(game, "HUD/DayClock/Root/ClockActionButton");
                Check(craft.IsOpen && !game.DayClosed, "Visible shared clock must not close the day through a real click during craft");
                int items = game.StoredItems.Length;
                if (state == "playing") craft.FinishForTesting(false);
                if (closeButton == "Cancel") await CancelLegacyCraft(game, name);
                else await ClickControl(game, name + "/Surface/Panel/Buttons/" + closeButton);
                Check(clockLayer.Layer < 100 && !clockAction.Disabled, "Closing craft must restore ordinary clock layer and input");
                Check(!craft.IsOpen && !Paused && game.StoredItems.Length == items,
                    "Restyled cancel/collect controls must close the minigame without duplicating items");
            }
        }
        finally
        {
            Paused = false;
            game.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(originalLocale);
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }

    private async Task TestEmbroidery()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        Root.AddChild(game); game.SetProcess(false);
        await ToSignal(this, SignalName.ProcessFrame);
        Vector2I originalSize = Root.Size;
        string originalLocale = TranslationServer.GetLocale();
        try
        {
            var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
            embroidery.SetProcess(false);
            if (!game.DayStarted) game.EndDay();
            int initialMoney = game.Score, initialEnergy = game.EnergyRemaining;
            await ClickControl(game, "World/Atelier/GetItemButton");
            Check(game.Crafting.IsCatalogOpen && !embroidery.IsOpen && Paused,
                "The real atelier button must open the catalog before preparation");
            SelectLegacyCraft(game, "hat");
            double readyTime = game.DayRemainingSeconds;
            game._Process(1); embroidery._Process(1);
            Check(game.DayRemainingSeconds == readyTime && embroidery.Session.RemainingSeconds == 10 &&
                game.StoredItems.Length == 0 && game.Score == initialMoney,
                "Catalog and unstarted preparation must freeze the day and remain free");
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            Check(pause.IsOpen && embroidery.IsOpen, "Pause must cover embroidery and preserve its preparation");
            pause.Close();
            await CancelLegacyCraft(game, "EmbroideryGame");
            Check(!Paused && game.Score == initialMoney && game.EnergyRemaining == initialEnergy,
                "Cancelling preparation and catalog must return to the world without a charge");
            foreach (Vector2I size in new[] { new Vector2I(1280,720), new Vector2I(960,540) })
            {
                Root.Size = size;
                game.GetItemFromAtelier(); SelectLegacyCraft(game, "hat");
                await ToSignal(this, SignalName.ProcessFrame);
                var surface = embroidery.GetNode<EmbroiderySurface>("Surface");
                Rect2 panel = surface.PanelRect;
                Check(new Rect2(Vector2.Zero, Root.GetVisibleRect().Size).Encloses(panel) &&
                    panel.Encloses(surface.QualityTrackRect.Grow(3)), "Workspace and quality meter must fit the viewport");
                foreach (string stat in new[] { "Time", "Stitches", "Misses" })
                {
                    var card = surface.GetNode<Control>("Panel/Stat" + stat);
                    Rect2 bounds = new(card.GlobalPosition - surface.GlobalPosition, card.Size);
                    Check(panel.Encloses(bounds) && bounds.End.Y < surface.FabricRect.Position.Y,
                        "Stat cards must fit above the fabric");
                }
                Check(surface.GetNode<Label>("Panel/StatTime/Value").Text == "10.0" &&
                    surface.GetNode<Label>("Panel/StatStitches/Value").Text == "0 / " + embroidery.Session.Points.Count &&
                    surface.GetNode<Label>("Panel/StatMisses/Value").Text == "0", "Prepared statistics must match the chosen pattern");
                await CancelLegacyCraft(game, "EmbroideryGame");
            }
            TranslationServer.SetLocale("en");
            game.GetItemFromAtelier(); SelectLegacyCraft(game, "hat");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(embroidery.GetNode<Label>("Surface/Panel/Title").Text.StartsWith("Embroidery:") &&
                embroidery.StartButton.Text == "Start" &&
                embroidery.GetNode<Label>("Surface/Panel/StatTime/Heading").Text == "TIME", "Preparation must localize action and selected title");
            await ClickControl(game, "EmbroideryGame/Surface/Panel/Buttons/Action");
            int cost = WorkshopItemCatalog.ItemCraftCost(WorkshopItemCatalog.IndexOf("hat"));
            embroidery.Activate();
            Check(game.Score == initialMoney - cost && game.StoredItems.Length == 1 && game.EnergyRemaining == initialEnergy,
                "Start and duplicate activation must debit only one selected item");
            var board = embroidery.GetNode<EmbroiderySurface>("Surface");
            var camera = game.GetNode<WorldCamera>("Camera2D");
            Vector2 cameraPosition = camera.Position, cameraZoom = camera.Zoom;
            Vector2 badPoint = board.FabricRect.Position + Vector2.One * 2;
            PushEmbroideryClick(badPoint);
            Check(embroidery.Session.Misses == 1, "A real click outside the target must count a miss");
            Root.PushInput(new InputEventMouseButton { Position = badPoint, ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
            Check(camera.Zoom == cameraZoom && camera.Position == cameraPosition, "Craft input must not pan or zoom the world");
            int count = embroidery.Session.Points.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = board.ScreenPoint(i);
                if (i == 0)
                {
                    Root.PushInput(new InputEventScreenTouch { Position = point, Index = 0, Pressed = true }, true);
                    Root.PushInput(new InputEventScreenTouch { Position = point, Index = 0, Pressed = false }, true);
                    await ToSignal(this, SignalName.ProcessFrame);
                }
                else PushEmbroideryClick(point);
            }
            embroidery._Process(0);
            Check(embroidery.Session.Complete && embroidery.Session.Stitches == count && embroidery.Session.Misses == 1 &&
                board.GetNode<Label>("Panel/StatStitches/Value").Text == $"{count} / {count}" &&
                board.GetNode<Label>("Panel/Quality").Text == embroidery.Session.Quality + "%",
                "Mouse and touch must complete the ordered selected pattern and update quality");
            await ToSignal(this, SignalName.ProcessFrame);
            // Actual collect clicks are covered before synthetic touch in TestPolishingStart/Reward.
            embroidery.Activate();
            Check(!Paused && !embroidery.IsOpen && game.StoredItems.Length == 1,
                "Collect must resume without duplicating the item after mouse/touch stitching");
            game.NewGame(); game.EndDay();
            game.GetItemFromAtelier(); SelectLegacyCraft(game, "hat");
            readyTime = game.DayRemainingSeconds;
            embroidery._Process(1);
            Check(game.DayRemainingSeconds == readyTime, "Preparation must freeze the day");
            embroidery.Activate(); embroidery._Process(2);
            Check(game.DayRemainingSeconds == readyTime - 2 && embroidery.Session.RemainingSeconds == 8,
                "Active play must advance both clocks equally");
            pause.Open(); double pausedTime = game.DayRemainingSeconds; embroidery._Process(1);
            Check(game.DayRemainingSeconds == pausedTime && embroidery.Session.RemainingSeconds == 8, "Pause must freeze both craft clocks");
            pause.Close(); embroidery._Process(8);
            double resultTime = game.DayRemainingSeconds; embroidery._Process(1);
            Check(game.DayRemainingSeconds == resultTime && embroidery.Session.RemainingSeconds == 0,
                "Result viewing must stop the active attempt clock");
            embroidery.Activate();
            game.NewGame(); game.EndDay(); game._Process(Math.Max(0, game.DayRemainingSeconds - .5));
            game.GetItemFromAtelier(); SelectLegacyCraft(game, "hat");
            embroidery._Process(.5);
            Check(!game.DayClosed && embroidery.IsOpen, "Preparation must freeze even near day expiry");
            embroidery.Activate(); embroidery._Process(.5);
            Check(game.DayClosed && embroidery.IsOpen && game.StoredItems.Length == 1,
                "Expiry during paid play must retain the exact paid item");
            embroidery._Process(10); embroidery.Activate(); embroidery.Activate();
            Check(!Paused && !embroidery.IsOpen && game.StoredItems.Length == 1 && game.DayClosed,
                "Late collect must neither lose nor duplicate the item");
            game.NewGame(); game.EndDay(); game.GetItemFromAtelier(); SelectLegacyCraft(game, "hat");
            game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame);
            Check(!Paused, "Removing preparation must release its pause");
        }
        finally
        {
            Paused = false; Root.Size = originalSize; TranslationServer.SetLocale(originalLocale);
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }

    private async Task TestPolishing()
    {
        var scene=GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game=scene.Instantiate<TradeScene>(); game.SavePath="";
        game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        Root.AddChild(game);
        await ToSignal(this,SignalName.ProcessFrame);
        Vector2I originalSize=Root.Size;
        string originalLocale=TranslationServer.GetLocale();
        const string path="user://polishing_progress_test.json";
        try
        {
            var polishing=game.GetNode<EmbroideryGame>("PolishingGame");
            if (!game.DayStarted) game.EndDay();
            game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar");
            Check(!polishing.IsOpen && !Paused,"Locked music workshop must not open polishing");
            game.Score=2000; game.TryUnlockMusicWorkshop();
            int initialEnergy = game.EnergyRemaining;
            foreach(string kind in PolishingSurface.InstrumentKinds)
            {
                var model=game.GetNode<PolishingSurface>("PolishingGame/Surface/Polishing").Begin(kind);
                Check(model.Cells.Count(cell=>cell.Inside)>100,
                    "Every new music item must supply a nonempty playable alpha silhouette");
            }
            foreach(Vector2I size in new[]{new Vector2I(1280,720),new Vector2I(960,540)})
            {
                Root.Size=size;
                await ClickControl(game,"World/MusicWorkshop/GetItemButton");
                SelectLegacyCraft(game, "guitar");
                Check(polishing.IsOpen && Paused && game.Score==1000 && game.StoredItems.Length==0,
                    "The real music create button must open the shared craft shell without payment yet");
                var surface=polishing.GetNode<EmbroiderySurface>("Surface");
                var board=surface.GetNode<PolishingSurface>("Polishing");
                Check(surface.FabricRect==board.GetRect() && surface.PanelRect.Encloses(surface.QualityTrackRect.Grow(3)) &&
                      board.GetRect().Size.X>board.GetRect().Size.Y,
                    "Polishing must occupy the existing fabric workspace with the same quality scale and layout");
                Check(surface.GetNode<Label>("Panel/StatStitches/Value").Text=="0%" &&
                      surface.GetNode<Label>("Panel/StatMisses/Value").Text=="0" &&
                      surface.GetNode<Label>("Panel/StatTime/Value").Text=="10.0",
                    "Shared cards must show polishing progress, misses and ten seconds");
                double remaining=game.DayRemainingSeconds;
                await ToSignal(this,SignalName.ProcessFrame);
                Check(game.DayRemainingSeconds==remaining,"The day clock must freeze on the polishing ready screen");
                game.GetItemFromAtelier(); SelectLegacyCraft(game, "hat");
                var pause = game.GetNode<PauseMenu>("PauseMenu");
                pause.Open();
                Check(!game.GetNode<EmbroideryGame>("EmbroideryGame").IsOpen && pause.IsOpen && polishing.IsOpen,
                    "Pause must cover polishing and another craft must not open");
                pause.Close();
                await CancelLegacyCraft(game, "PolishingGame");
                Check(!Paused && !polishing.IsOpen && game.Score==1000,"Cancel polishing must be free");
            }
            TranslationServer.SetLocale("en");
            game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar");
            await ToSignal(this,SignalName.ProcessFrame);
            Check(polishing.GetNode<Label>("Surface/Panel/Title").Text.StartsWith("Polishing:") &&
                  polishing.GetNode<Label>("Surface/Panel/StatStitches/Heading").Text=="CLEANED",
                "Polishing-specific content must follow RU/EN while sharing the shell");
            Root.PushInput(new InputEventKey { Keycode=Key.Escape,Pressed=true },true);
            var polishingPause = game.GetNode<PauseMenu>("PauseMenu");
            Check(polishingPause.IsOpen && polishing.IsOpen, "Escape must open pause over ready polishing");
            polishingPause.Close();
            await CancelLegacyCraft(game, "PolishingGame");
            game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar");
            await ClickControl(game,"PolishingGame/Surface/Panel/Buttons/Action");
            Check(game.Score==970 && game.EnergyRemaining==initialEnergy && game.StoredItems.Length==1 &&
                  game.StoredItems.All(index=>WorkshopItemCatalog.Items[index].WorkshopId=="music"),
                $"Polishing start must commit exactly the selected music item for 30 dollars without energy (score={game.Score}, energy={game.EnergyRemaining}, count={game.StoredItems.Length}, state={polishing.ActiveSession.State})");
            polishing.Activate();
            Check(game.Score==970 && game.StoredItems.Length==1,"Duplicate start must not buy another batch");
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
            Check(polishing.GetNode<Label>("Surface/Panel/StatMisses/Value").Text=="1" &&
                  polishing.GetNode<Label>("Surface/Panel/StatStitches/Value").Text==$"{Math.Round(polishing.Polishing.Progress*100)}%",
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
            await ClickControl(game,"PolishingGame/Surface/Panel/Buttons/Action");
            Check(!Paused && !polishing.IsOpen && game.Score==970 && game.StoredItems.Length==1,
                "Collect after polishing must resume the world without duplicating payment or goods");

            game.SetProcess(false); polishing.SetProcess(false);
            game.NewGame(); game.EndDay(); game.Score=2000; game.TryUnlockMusicWorkshop();
            game._Process(Math.Max(0, game.DayRemainingSeconds - .5)); game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar"); polishing._Process(.5);
            Check(!game.DayClosed && polishing.IsOpen && Paused && game.Score==1000,
                "Preparation near expiry must remain free and freeze the day");
            await CancelLegacyCraft(game, "PolishingGame");
            game.NewGame(); game.EndDay(); game.Score=2000; game.TryUnlockMusicWorkshop();
            game._Process(Math.Max(0, game.DayRemainingSeconds - .5)); game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar"); polishing.Activate(); polishing._Process(.5);
            Check(game.DayClosed && polishing.IsOpen && game.StoredItems.Length==1 && game.Score==970,
                "A started polishing craft must retain its batch when the day expires");
            polishing._Process(10); polishing.Activate(); polishing.Activate();
            Check(!Paused && game.Score==970 && game.StoredItems.Length==1,"Late collect must remain idempotent");

            game.NewGame(); game.EndDay(); game.Score=2000; game.TryUnlockMusicWorkshop(); game.SavePath=path;
            game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar"); polishing.Activate();
            int[] items=game.StoredItems;
            game.QueueFree(); await ToSignal(this,SignalName.ProcessFrame);
            Check(!Paused,"Leaving a started polishing scene must release its modal pause");
            game=scene.Instantiate<TradeScene>(); game.SavePath=path; game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        Root.AddChild(game); game.SetProcess(false);
            await ToSignal(this,SignalName.ProcessFrame);
            Check(game.Score==970 && game.EnergyRemaining==initialEnergy && game.StoredItems.SequenceEqual(items) &&
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

    private static Rect2 CraftBounds(Control surface, Control child) =>
        new(surface.GetGlobalTransform().AffineInverse() * child.GlobalPosition, child.Size);

    private static void SelectLegacyCraft(TradeScene game, string kind)
    {
        if (game.Crafting.IsCatalogOpen) game.Crafting.Select(kind);
    }

    private async Task CancelLegacyCraft(TradeScene game, string name)
    {
        await ClickControl(game, name + "/Surface/Panel/Buttons/Cancel");
        Check(game.Crafting.IsCatalogOpen && Paused, "Back from unpaid preparation must return to the paused catalog");
        game.Crafting.Catalog.Close();
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
        if (!game.DayStarted) game.EndDay();
        game.GetItemFromMusicWorkshop(); SelectLegacyCraft(game, "guitar");
        FinishMusic(game);
    }

    private static void FinishMusic(TradeScene game)
    {
        var polishing = game.GetNode<EmbroideryGame>("PolishingGame");
        SelectLegacyCraft(game, "guitar");
        if (!polishing.IsOpen) return;
        polishing.Activate();
        polishing._Process(polishing.ActiveSession.RemainingSeconds + .01);
        polishing.Activate();
    }

    private static void CraftAtelier(TradeScene game)
    {
        if (!game.DayStarted) game.EndDay();
        game.GetItemFromAtelier(); SelectLegacyCraft(game, "hat");
        FinishEmbroidery(game);
    }

    private static void FinishEmbroidery(TradeScene game)
    {
        var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
        SelectLegacyCraft(game, "hat");
        if (!embroidery.IsOpen) return;
        embroidery.Activate();
        embroidery._Process(embroidery.ActiveSession.RemainingSeconds + .01);
        embroidery.Activate();
    }
}
