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
                game.GetItemFromMusicWorkshop();
                Check(craft.IsOpen, "Unlocked room must open polishing");
                await ToSignal(this, SignalName.ProcessFrame);
                var cloth = craft.GetNode<Sprite2D>("Surface/PolishingCursor");
                Check(Input.MouseMode == Input.MouseModeEnum.Visible && !cloth.Visible,
                    "Ready buttons need the native pointer: the centered cloth must not hide their click hotspot");
                await ClickControl(game, "PolishingGame/Surface/Cancel");
                Check(!craft.IsOpen && !Paused && Input.MouseMode == Input.MouseModeEnum.Visible,
                    "Cancel must restore the pointer and leave the preview reusable");
                game.GetItemFromMusicWorkshop();
                await ToSignal(this, SignalName.ProcessFrame);
                int money = game.Score, energy = game.EnergyRemaining;
                var button = craft.GetNode<Button>("Surface/Action");
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
                Check(game.StoredItems.Length == EconomyConfigProvider.Load().Days.ItemsPerCraft,
                    "Start must grant the paid batch exactly once");
                Check(game.Score == money - WorkshopItemCatalog.CraftCost("music") && game.EnergyRemaining == energy - 1,
                    "Start must charge its price and one energy exactly once");
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
                await ClickControl(game, "PolishingGame/Surface/Action");
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
        Root.AddChild(game);
        game.SetProcess(false);
        await ToSignal(this, SignalName.ProcessFrame);
        const string savePath = "user://polishing_reward_test.json";
        try
        {
            var craft = game.GetNode<EmbroideryGame>("PolishingGame");
            craft.SetProcess(false);
            var board = craft.GetNode<PolishingSurface>("Surface/Polishing");
            int[] music = WorkshopItemCatalog.Items.Select((item, index) => (item, index))
                .Where(entry => entry.index < WorkshopItemCatalog.ActiveItemCount && entry.item.WorkshopId == "music").Select(entry => entry.index).ToArray();
            foreach (int level in new[] { 0, 3 })
            {
                var upgrades = new DayUpgradeSession();
                upgrades.Restore(false, DayUpgrade.None, musicLevel: level);
                int total = WorkshopItemCatalog.TotalWeight("music", upgrades);
                var counts = Enumerable.Range(0, total).Select(ticket => WorkshopItemCatalog.PreparePolishing(ticket, upgrades))
                    .GroupBy(prepared => WorkshopItemCatalog.Items[prepared.ItemIndex].RarityId)
                    .ToDictionary(group => group.Key, group => group.Count());
                var rarities = EconomyConfigProvider.Load().Rarities;
                int weights = rarities.Sum(rarity => WorkshopItemCatalog.RarityWeight("music", rarity.Id, upgrades));
                foreach (var rarity in rarities)
                    Check((counts.TryGetValue(rarity.Id, out int amount) ? amount : 0) * weights ==
                        total * WorkshopItemCatalog.RarityWeight("music", rarity.Id, upgrades),
                        "Preparing the visible reward must preserve exact rarity probabilities, including music upgrades");
            }
            int iteration = 0;
            foreach (int level in new[] { 0, 3 })
            foreach (int expected in music)
            {
                game.NewGame();
                Check(!craft.PreparedItemIndex.HasValue, "A new run must clear the previous paid polishing selection");
                game.EndDay();
                game.Score = 10000;
                game.TryUnlockMusicWorkshop();
                game.DayUpgrades.Restore(false, DayUpgrade.None, musicLevel: level);
                int total = WorkshopItemCatalog.TotalWeight("music", game.DayUpgrades);
                ulong seed = 1;
                for (; seed < 100000; seed++)
                {
                    GD.Seed(seed);
                    int ticket = GD.RandRange(0, total - 1);
                    if (WorkshopItemCatalog.Roll("music", ticket, game.DayUpgrades) == expected) break;
                }
                Check(seed < 100000, "Every music rarity and instrument must remain reachable with existing weighted rolls");
                GD.Seed(seed);
                craft.Open();
                string expectedArt = System.IO.Path.GetFileNameWithoutExtension(WorkshopItemCatalog.Items[expected].ArtPath);
                Check(craft.IsOpen && craft.PreparedItemIndex == expected && board.InstrumentKind == expectedArt,
                    $"Weighted item {expected} must display its actual art, including legacy catalog kinds");
                int money = game.Score, energy = game.EnergyRemaining, count = game.StoredItems.Length;
                await ClickControl(game, "PolishingGame/Surface/Cancel");
                Check(!craft.IsOpen && !Paused && game.Score == money && game.EnergyRemaining == energy && game.StoredItems.Length == count,
                    "Cancelling an unstarted polishing preview must remain free");
                GD.Seed(seed + 1000);
                craft.Open();
                Check(craft.PreparedItemIndex == expected && board.InstrumentKind == expectedArt,
                    "Free cancellation must not reroll the preview into a rarer paid reward");
                await ClickControl(game, "PolishingGame/Surface/Action");
                int batch = EconomyConfigProvider.Load().Days.ItemsPerCraft;
                Check(game.Score == money - WorkshopItemCatalog.CraftCost("music") && game.EnergyRemaining == energy - 1 &&
                    game.StoredItems.Length == count + batch && game.StoredItems.Skip(count).All(index => index == expected),
                    "Starting must charge the configured cost and energy once and grant exactly the displayed instrument for the configured batch");
                craft.Activate();
                Check(game.Score == money - WorkshopItemCatalog.CraftCost("music") && game.StoredItems.Length == count + batch,
                    "Repeated start while playing must not pay or grant twice");
                bool success = iteration++ % 2 == 0;
                if (success) craft.FinishForTesting(true);
                else craft._Process(craft.ActiveSession.RemainingSeconds + .1);
                int quality = craft.ActiveSession.Quality;
                Check(craft.ActiveSession.State == EmbroideryState.Finished &&
                    craft.GetNode<Label>("Surface/Quality").Text == $"{quality}%" &&
                    game.StoredItems.Skip(count).All(index => index == expected),
                    "Success and timeout must preserve the polished item and report the session quality");
                await ClickControl(game, "PolishingGame/Surface/Action");
                craft.Activate();
                Check(!craft.IsOpen && !Paused && game.StoredItems.Length == count + batch &&
                    game.Score == money - WorkshopItemCatalog.CraftCost("music") && game.EnergyRemaining == energy - 1,
                    "Collect and repeated activation after close must not change the paid batch or resources");
            }

            craft.Open();
            int unpaid = craft.PreparedItemIndex.Value;
            await ClickControl(game, "PolishingGame/Surface/Cancel");
            game.NewGame();
            Check(!craft.PreparedItemIndex.HasValue, "A new run must also clear an unpaid cancelled polishing preview");
            game.EndDay();
            game.Score = 10000;
            game.TryUnlockMusicWorkshop();
            int nextTotal = WorkshopItemCatalog.TotalWeight("music", game.DayUpgrades);
            ulong nextSeed = 1;
            int nextExpected;
            do
            {
                GD.Seed(nextSeed++);
                nextExpected = WorkshopItemCatalog.Roll("music", GD.RandRange(0, nextTotal - 1), game.DayUpgrades);
            } while (nextExpected == unpaid);
            GD.Seed(nextSeed - 1);
            craft.Open();
            Check(craft.PreparedItemIndex == nextExpected &&
                board.InstrumentKind == System.IO.Path.GetFileNameWithoutExtension(WorkshopItemCatalog.Items[nextExpected].ArtPath),
                "The new run must prepare its own weighted item instead of reusing the old unpaid preview");
            craft.FinishForTesting(true);
            await ClickControl(game, "PolishingGame/Surface/Action");
            Check(game.StoredItems.All(index => index == nextExpected), "The reset preview must remain the exact paid reward");

            game.SavePath = savePath;
            game.PersistProgress();
            int[] saved = game.StoredItems;
            int savedMoney = game.Score, savedEnergy = game.EnergyRemaining;
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = scene.Instantiate<TradeScene>();
            game.SavePath = savePath;
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.StoredItems.SequenceEqual(saved) && game.Score == savedMoney && game.EnergyRemaining == savedEnergy &&
                !game.CraftMiniGameOpen && !Paused, "Reload must preserve the exact polished item without duplicating its batch");
        }
        finally
        {
            Paused = false;
            GD.Randomize();
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
                craft.Open();
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
                    Check(Pixel(surface.GetNode<Control>("Stat" + stat).Position + new Vector2(10, 10)).R > 0.9f,
                        "Statistics must have light cards behind their black headings and values");
                Rect2 panel = surface.PanelRect, fabric = surface.FabricRect;
                var title = surface.GetNode<Label>("Title");
                var firstStat = surface.GetNode<Control>("StatTime");
                var lastStat = surface.GetNode<Control>("StatMisses");
                var hint = surface.GetNode<Label>("Hint");
                Check(title.GetRect().End.X + 8 <= firstStat.Position.X &&
                    lastStat.GetRect().End.X + 8 <= panel.End.X &&
                    firstStat.GetRect().End.Y + 20 <= fabric.Position.Y,
                    "Title and three statistics must share a separated header above the workspace");
                Check(panel.Encloses(fabric.Grow(7)) && fabric.End.Y + 7 <= hint.Position.Y &&
                    surface.QualityTrackRect.Position.X >= fabric.End.X + 16 &&
                    surface.GetNode<Label>("Quality").GetRect().End.Y <= fabric.End.Y + 1,
                    "Workspace, quality sidebar and footer must stay separated inside the modal");
                var clockLayer = game.GetNode<CanvasLayer>("HUD/DayClock");
                var clockAction = game.GetNode<Button>("HUD/DayClock/Root/ClockActionButton");
                var clock = game.GetNode<Label>("HUD/DayClock/Root/DayTimer");
                Check(!surface.HasNode("DayTimer") && !surface.HasNode("DayHeading") &&
                    clockLayer.Layer > craft.Layer && clock.IsVisibleInTree() && clockAction.Disabled &&
                    surface.GetNode<Control>("StatTime").Visible,
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
                foreach (string color in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color", "font_disabled_color" })
                    Check(surface.GetNode<Button>(buttonName).GetThemeColor(color) ==
                        (state == "ready" && buttonName == "Action" ? Colors.White : Colors.Black),
                        "Start must have white text on dark fill; secondary and collect buttons keep black text");
                var action = surface.GetNode<Button>("Action");
                var cancel = surface.GetNode<Button>("Cancel");
                if (state == "ready")
                {
                    Check(action.GetRect().End.X < cancel.Position.X && action.HasFocus() &&
                        surface.PanelRect.Encloses(action.GetRect()) && surface.PanelRect.Encloses(cancel.GetRect()),
                        "Start must be left of Back, initially focused, with both buttons inside the modal");
                    Check(Pixel(action.Position + new Vector2(10, 10)).R < 0.35f &&
                        Pixel(cancel.Position + new Vector2(10, 10)).R > 0.8f,
                        "The rendered Start button must remain clearly darker than Back through the grayscale shader");
                    if (size.X == 960)
                        frame.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chikago-hf179-{name}-{locale}.png"));
                }
                else if (action.Visible)
                    Check(Mathf.Abs(action.GetRect().GetCenter().X - surface.PanelRect.GetCenter().X) < 0.1f &&
                        surface.PanelRect.End.Y - action.GetRect().End.Y >= 35,
                        "Collect must remain centered with a clear margin from the lower frame");
                if (locale == "ru" && size.X == 960 && state == "timeout")
                    frame.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chikago-hf166-{name}.png"));
                string closeButton = state == "ready" ? "Cancel" : "Action";
                if (state == "ready" && locale == "ru" && size.X == 960)
                {
                    int money = game.Score, energy = game.EnergyRemaining, count = game.StoredItems.Length;
                    Rect2 beforeStart = surface.FabricRect;
                    await ClickControl(game, name + "/Surface/Action");
                    Check(surface.FabricRect == beforeStart, "Starting must keep the workspace and hit targets in place");
                    Check(craft.ActiveSession.State == EmbroideryState.Playing && !action.Visible && !cancel.Visible &&
                        game.Score < money && game.EnergyRemaining == energy - 1 && game.StoredItems.Length == count + 1,
                        "The highlighted left Start must begin the real game, charge once and hide both buttons");
                    craft.FinishForTesting(false);
                    closeButton = "Action";
                }
                await ClickControl(game, "HUD/DayClock/Root/ClockActionButton");
                Check(craft.IsOpen && !game.DayClosed, "Visible shared clock must not close the day through a real click during craft");
                int items = game.StoredItems.Length;
                if (state == "playing") craft.FinishForTesting(false);
                await ClickControl(game, name + "/Surface/" + closeButton);
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
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            Check(pause.IsOpen && embroidery.IsOpen, "Pause must cover embroidery while preserving the ready state");
            pause.Close();
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
            Check(pause.IsOpen && embroidery.IsOpen, "Escape must open pause over the ready craft");
            pause.Close();
            await ClickControl(game, "EmbroideryGame/Surface/Cancel");

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
                  game.GetNode<Label>("HUD/DayClock/Root/DayTimer").Text == "00",
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
                game.GetItemFromAtelier();
                var pause = game.GetNode<PauseMenu>("PauseMenu");
                pause.Open();
                Check(!game.GetNode<EmbroideryGame>("EmbroideryGame").IsOpen && pause.IsOpen && polishing.IsOpen,
                    "Pause must cover polishing and another craft must not open");
                pause.Close();
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
            var polishingPause = game.GetNode<PauseMenu>("PauseMenu");
            Check(polishingPause.IsOpen && polishing.IsOpen, "Escape must open pause over ready polishing");
            polishingPause.Close();
            await ClickControl(game, "PolishingGame/Surface/Cancel");
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
        if (!game.DayStarted) game.EndDay();
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
        if (!game.DayStarted) game.EndDay();
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
