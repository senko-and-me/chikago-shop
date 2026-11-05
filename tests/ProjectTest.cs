using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest : SceneTree
{
    private const string SavePath = "user://trade_progress_test.json";

    public override void _Initialize() => Callable.From(Run).CallDeferred();

    private async void Run()
    {
        try
        {
            TestEconomyConfig();
            await TestPauseAndZoomUi();
            await TestMouseControls();
            await TestTradeScene();
            await TestProgress();
            await TestMainMenu();
            GD.Print("All C# project checks passed");
            Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            Quit(1);
        }
    }

    private static void TestEconomyConfig()
    {
        var config = EconomyConfigProvider.Load();
        Check(config.SchemaVersion == 1 && config.IsProvisional,
            "The game must load the versioned provisional economy config");
        Check(config.Workshops.Count == 2 && config.Rarities.Count == 5 && config.OfferRanges.Count == 3,
            "The game and domain tests must share the complete economy v1 source");
        Check(config.StartingMoney == 220 && config.Workshops[0].SpinCost == 90 &&
              config.Workshops[1].SpinCost == 140 && config.Workshops[1].UnlockCost == 100 &&
		      config.Quotas[0].Amount == 200 &&
              config.Quotas[1].Amount == 400,
            "The runtime must load the provisional paid-spin and raised-quota balance");
    }

    private static string ExpectedItemTooltip(int index) =>
        $"{TradeScene.GetItemName(index)}\nРедкость: {TradeScene.GetItemRarity(index)}\nСтоимость: {TradeScene.GetItemBasePrice(index)}$";

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
            "ShowroomLeft/ShowroomHeader", "ShowroomLeft/ShowroomLabel", "ShowroomLeft/ShowroomHint",
            "TradingRoom/SalesRoomLabel" }.Select(path => game.GetNode<CanvasItem>("World/" + path)).ToArray();
        ui.UpdateForZoom(0.4f);
        Check(headers.All(header => !header.Visible && Mathf.IsZeroApprox(header.Modulate.A)),
            "All room titles, descriptions and backplates must disappear at zoom 0.4");
        Check(panel.Scale.X > 1 && panel.Scale.X <= 1.65f && hud.Scale == Vector2.One,
            "Small world UI must grow moderately on zoom-out while HUD stays fixed");
        ui.UpdateForZoom(0.5f);
        Check(headers.All(header => header.Visible && header.Modulate.A > 0 && header.Modulate.A < 1),
            "Room headings must fade in gradually above 0.4");
        ui.UpdateForZoom(0.6f);
        Check(headers.All(header => header.Visible && Mathf.IsEqualApprox(header.Modulate.A, 1)),
            "Room headings must reach full opacity at 0.6");
        ui.UpdateForZoom(0.1f);
        Check(Mathf.IsEqualApprox(panel.Scale.X, 1.65f), "Zoom compensation must be capped");
        foreach (float zoom in new[] { 1f, 1.8f })
        {
            ui.UpdateForZoom(zoom);
            Check(panel.Scale == Vector2.One, "Zooming in must restore the original UI scale without cumulative growth");
        }
        Check(TradeScene.GetItemBasePrice(0) == 30 && TradeScene.GetItemBasePrice(1) == 50 &&
              TradeScene.GetItemBasePrice(2) == 45 && TradeScene.GetItemBasePrice(3) == 75,
            "Inventory prices must match the current trade catalog");

        game.GetItemFromAtelier();
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
        Check(game.StoredItems.Length == 3 && game.Score == 130 && game.EnergyRemaining == 1,
            "A mouse click must create three paid atelier items");
        await ClickControl(game, "HUD/InventoryUI/ScrollContainer/HBoxContainer/" +
            game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChild(0).Name);
        Check(game.SelectedInventoryIndex == 0, "Inventory must accept mouse selection");
        Check(Root.GuiGetHoveredControl()?.GetTooltip() == ExpectedItemTooltip(game.StoredItems[0]),
            "Hovering the inventory image must expose its name, rarity and price");
        await ClickControl(game, "World/ShowroomLeft/Showcases/Slot1");
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
        Check(game.CurrentDay == 2, "End day must accept mouse input");
        await ClickControl(game, "PauseMenu/Root/PauseButton");
        await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/CheckpointButton");
        Check(game.CurrentDay == 1 && game.Score == 220, "Checkpoint handler must be connected");
        await ClickControl(game, "World/MusicWorkshop/LockedOverlay/UnlockButton");
        Check(game.MusicWorkshopUnlocked, "Unlock must accept mouse input");
        game.Score = 140;
        game.RefreshItemButtons();
        await ClickControl(game, "World/MusicWorkshop/GetItemButton");
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

    private async Task ClickControl(TradeScene game, string path)
    {
        var control = game.GetNode<Control>(path);
        if (path.StartsWith("World/", StringComparison.Ordinal))
        {
            game.GetNode<Camera2D>("Camera2D").Position = control.GetGlobalRect().GetCenter();
            game.ClampCamera();
        }
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(control.IsVisibleInTree(), $"Mouse target must be visible: {path}");
        Vector2 point = control.GetGlobalTransformWithCanvas() * (control.Size / 2);
        Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
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
        }
        var atelierButton = game.GetNode<Button>("World/Atelier/GetItemButton");
        var musicButton = game.GetNode<Button>("World/MusicWorkshop/GetItemButton");
        Check(atelierButton.GetThemeStylebox("normal") is StyleBoxTexture &&
              musicButton.GetThemeStylebox("normal") is StyleBoxTexture,
            "Both workshops must use the art team's create-item button");
        var atelierRarityPanel = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
        var musicRarityPanel = game.GetNode<RarityPanel>("World/MusicWorkshop/RarityPanel");
        Check(!atelierRarityPanel.Visible && atelierRarityPanel.GetNode<GridContainer>("Values").GetChildCount() == 15,
            "The rarity artwork must provide all five price, chance and reroll rows");
        Check(atelierRarityPanel.GetNode<Label>("Values/Price0").CustomMinimumSize.Y == 63 &&
              atelierRarityPanel.GetNode<Label>("Values/Price4").CustomMinimumSize.Y == 63,
            "Rarity values must follow the row spacing drawn into the panel artwork");
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
            foreach (Label value in panel.GetNode<GridContainer>("Values").GetChildren().OfType<Label>())
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
		game.GetItemFromMusicWorkshop();
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
            atelierButton.EmitSignal(Button.SignalName.Pressed);
            game.EndDay();
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
        musicButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.StoredItems.Length == 6 && game.StoredItems.Take(3).All(index => index is >= 0 and <= 1) &&
              game.StoredItems.Skip(3).All(index => index is >= 2 and <= 3) && game.Score == 670 &&
              game.EnergyRemaining == 0 && atelierButton.Disabled && musicButton.Disabled,
            "Unlocking and two paid spins must consume 330 dollars and both energy, producing three category items each");
        game.GetItemFromAtelier();
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
              game.GetNode<Label>("World/ShowroomLeft/Showcases/Slot1/ItemLabel").Text == "НА СДЕЛКЕ",
            "The active item's showcase must stay reserved during the trade");
        game.GetItemFromAtelier();
        SelectInventorySlot(game, 1);
        game.PlaceItemInShowcase(0);
        Check(game.StoredItems.Length == 8 && game.ShowcaseItems[0] == -1,
            "The reserved showcase must reject a new item");
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
              game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").Disabled,
            "Clicking an occupied showcase must neither replace nor discard goods");
        foreach (string kind in new[] { "hat", "gloves", "guitar", "trumpet" })
            Check(game.GetNode<Sprite2D>($"World/TradingRoom/Item/{char.ToUpperInvariant(kind[0])}{kind[1..]}").Visible ==
                  (kind == TradeScene.GetItemKind(firstItem)), "Only the selected item illustration must be visible");
        int[] prices = [30, 50, 45, 75];
        Check(game.Percentage is >= -30 and <= 30, "Offer percentage is out of range");
        Check(game.OfferedPrice == TradeScene.PriceForPercent(prices[firstItem], game.Percentage),
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
        game.GetItemFromAtelier();
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

        var camera = game.GetNode<Camera2D>("Camera2D");
        Rect2 cameraBounds = roomLayout.GlobalTransform * roomLayout.GetRect();
        float widthZoom = game.GetViewportRect().Size.X / cameraBounds.Size.X;
        Check(camera.Position.Y > 1625, "The camera must start in the lower-left showroom");
        float oldZoom = camera.Zoom.X;
        for (int frame = 0; frame < 120; frame++) game._Process(1d / 60d);
        Check(Mathf.IsEqualApprox(camera.Zoom.X, oldZoom) && camera.Zoom.X >= widthZoom && camera.Zoom.X <= 1.8f,
            "Camera zoom must remain stable without wheel input");
        game._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Position = new Vector2(640, 360), Pressed = true });
        Check(Mathf.IsEqualApprox(camera.Zoom.X, oldZoom), "Wheel input must not jump to the target zoom");
        game._Process(1d / 60d);
        Check(camera.Zoom.X > oldZoom && camera.Zoom.X < oldZoom * 1.12f,
            "Mouse wheel zoom must advance smoothly between frames");
        float zoomAfterStep = camera.Zoom.X;
        game._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Position = new Vector2(640, 360), Pressed = true });
        game._Process(1d / 60d);
        Check(camera.Zoom.X < zoomAfterStep && camera.Zoom.X > oldZoom,
            "Zooming back out must also advance smoothly");
        for (int step = 0; step < 100; step++)
            game._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Position = new Vector2(640, 360), Pressed = true });
        for (int frame = 0; frame < 180; frame++) game._Process(1d / 60d);
        Check(Mathf.IsEqualApprox(camera.Zoom.X, widthZoom, 0.001f) && widthZoom < 0.3f &&
              Mathf.IsEqualApprox(camera.Position.X, cameraBounds.GetCenter().X),
            "Zoom-out must stop exactly at the layout width, beyond the old fixed 0.3 limit");
        camera.Zoom = Vector2.One * 1.8f;
        camera.Position = cameraBounds.GetCenter();
        game.ClampCamera();
        float startingX = camera.Position.X;
        game._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
        game._Input(new InputEventMouseMotion { Relative = new Vector2(100, 0) });
        Check(Mathf.IsEqualApprox(camera.Position.X, startingX - 100f / camera.Zoom.X), "Dragging must pan the camera");
        camera.Zoom = Vector2.One * 1.8f;
        camera.Position = new Vector2(-100, -100);
        game.ClampCamera();
        Vector2 halfView = game.GetViewportRect().Size / (2f * camera.Zoom);
        Check((camera.Position - halfView).IsEqualApprox(cameraBounds.Position),
            "The top-left camera boundary must match RoomLayout without padding");
        camera.Position = new Vector2(100000, 100000);
        game.ClampCamera();
        Check((camera.Position + halfView).IsEqualApprox(cameraBounds.End),
            "The bottom-right camera boundary must match RoomLayout without padding");
        camera.Position = new Vector2(4582, 1103);
        game.ClampCamera();
        Check(camera.Position.X > 3622 && camera.Position.Y < 1625, "The camera must reach the upper-right workshop");
        game.ClampCamera();
        camera.Position = new Vector2(4582, 2183);
        game.ClampCamera();
        Check(camera.Position.X > 3622 && camera.Position.Y > 1625,
            "The camera must reach the lower-right showroom");

        Vector2 layoutPosition = roomLayout.Position;
        Vector2 layoutScale = roomLayout.Scale;
        roomLayout.Position += new Vector2(200, 150);
        roomLayout.Scale *= new Vector2(0.5f, 0.2f);
        cameraBounds = roomLayout.GlobalTransform * roomLayout.GetRect();
        camera.Zoom = Vector2.One * 0.001f;
        game.ClampCamera();
        Check(Mathf.IsEqualApprox(camera.Zoom.X, game.GetViewportRect().Size.X / cameraBounds.Size.X) &&
              camera.Position.IsEqualApprox(cameraBounds.GetCenter()),
            "Bounds must follow the layout transform; zoom-out depends only on width even for a short layout");
        Vector2I windowSize = Root.Size;
        Root.Size = new Vector2I(960, 900);
        camera.Zoom = Vector2.One * 0.001f;
        game.ClampCamera();
        Check(Mathf.IsEqualApprox(camera.Zoom.X, game.GetViewportRect().Size.X / cameraBounds.Size.X),
            "Resizing the viewport must recalculate the width-based zoom boundary");
        Root.Size = windowSize;
        roomLayout.Position = layoutPosition;
        roomLayout.Scale = layoutScale;
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Trade scene checks passed");
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
        game.GetItemFromAtelier();
        game.GetItemFromMusicWorkshop();
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
              game.GetNode<Label>("World/Showroom/Showcases/Slot4/ItemLabel").Text == "НА СДЕЛКЕ",
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
        game.Score = game.FirstQuota - 1;
        game.EndDay();
        Check(game.Failed && !game.TradeActive && game.ShowcaseItems[3] == itemsBeforePlacement[0] &&
              game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(2)),
            "Failure at the deadline must preserve both unsold showcase items");
        game.GetItemFromAtelier();
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
        game.GetItemFromAtelier();
        game.GetItemFromMusicWorkshop();
        int[] checkpointItems = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(2);
        game.CurrentDay = 4;
        game.Score = game.FirstQuota + 12;
        game.EndDay();
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
        game.EndDay();
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
        menu.OnBackPressed();
        Check(mainView.Visible && !settingsView.Visible, "Back must return to the main menu");
        menu.OnPlayPressed();
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene is TradeScene, "Play must enter the game");
        GD.Print("Main menu checks passed");
    }

    private async Task<TradeScene> CreateGame(PackedScene scene)
    {
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = SavePath;
        Root.AddChild(game);
        game.SetProcess(false);
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
            Check(slot.GetTooltip(slot.Size / 2) == ExpectedItemTooltip(items[i]),
                "Inventory tooltips must follow each current item's name, rarity and price after creation, movement and reload");
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
