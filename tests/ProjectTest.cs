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
                  slot.Position.X >= 240 && slot.Position.X + slot.Size.X <= 1680 &&
                  slot.Position.Y >= 720 && slot.Position.Y + slot.Size.Y <= 970,
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
              atelierRarityPanel.Price4 == 200 && musicRarityPanel.Price0 == 36 &&
              musicRarityPanel.Price1 == 60 && musicRarityPanel.Price2 == 96 &&
              musicRarityPanel.Price3 == 144 && musicRarityPanel.Price4 == 240,
            "Rarity panels must use the atelier and music prices from the balance document");
        atelierButton.EmitSignal(Control.SignalName.MouseEntered);
        Check(!atelierRarityPanel.Visible, "Hovering the main create area must not open rarity information");
        var rarityHotspot = atelierButton.GetNode<Control>("RarityHotspot");
        Check(rarityHotspot.Size.X <= 72 && rarityHotspot.Size.Y <= 68 && rarityHotspot.TooltipText.Length == 0,
            "The rarity hover target must cover only the drawn question-mark circle without a system tooltip");
        foreach (string labelName in new[]
                 {
                     "Chance0", "Rerolls0", "Chance1", "Rerolls1", "Price2", "Chance2", "Rerolls2",
                     "Price3", "Chance3", "Rerolls3", "Price4", "Chance4", "Rerolls4"
                 })
            Check(atelierRarityPanel.GetNode<Label>($"Values/{labelName}").GetThemeColor("font_color").V < 0.2f,
                $"Rarity value must remain readable on white: {labelName}");
        rarityHotspot.EmitSignal(Control.SignalName.MouseEntered);
        Check(atelierRarityPanel.Visible, "Hovering the question-mark circle must reveal rarity information");
        rarityHotspot.EmitSignal(Control.SignalName.MouseExited);
        Check(!atelierRarityPanel.Visible, "Leaving the question-mark circle must hide rarity information");
        var placeButton = game.GetNode<Button>("World/ShowroomLeft/PlaceItemButton");
        var rightPlaceButton = game.GetNode<Button>("World/Showroom/PlaceItemButton");
        var trashButton = game.GetNode<Button>("World/Showroom/TrashBin");
        var leftTrashButton = game.GetNode<Button>("World/ShowroomLeft/TrashBin");
        Check(trashButton.Size.X <= 160 && leftTrashButton.Size.X <= 160 &&
              trashButton.Text == "мусор" && leftTrashButton.Text == "мусор",
            "Both showrooms must use a compact waste bin tucked against the wall");
        foreach (string kind in new[] { "Hat", "Gloves", "Guitar", "Trumpet" })
            Check(game.GetNode<Sprite2D>($"World/TradingRoom/Item/{kind}").Texture != null,
                $"The {kind} item must have an illustration");
        Check(TradeScene.GetItemArtPath(0).EndsWith("hat_cloche.png") &&
              TradeScene.GetItemArtPath(1).EndsWith("gloves.png"),
            "Atelier items must use the transparent-background PNG artwork");
        Check(!atelierButton.Disabled && !musicButton.Disabled, "Both workshops must provide items");
        Check(placeButton.Disabled && rightPlaceButton.Disabled && !game.TradeActive && game.StoredItems.Length == 0,
            "A new game must start without a free item to sell");
        Check(game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChildCount() == 0,
            "An empty inventory must not render placeholder slots");

        for (int i = 0; i < 20; i++) atelierButton.EmitSignal(Button.SignalName.Pressed);
        var inventoryScroll = game.GetNode<ScrollContainer>("HUD/InventoryUI/ScrollContainer");
        await ToSignal(this, SignalName.ProcessFrame);
        Check(game.StoredItems.Length == 20 &&
              game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChildCount() == 20,
            "Prototype inventory and UI must not restore the old six-item limit");
        inventoryScroll.EmitSignal(Control.SignalName.GuiInput,
            new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true });
        Check(inventoryScroll.ScrollHorizontal > 0,
            "Mouse wheel over an overflowing inventory must scroll it horizontally");
        game.NewGame();
        await ToSignal(this, SignalName.ProcessFrame);
        Check(game.StoredItems.Length == 0 &&
              game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChildCount() == 0,
            "New game must remove all dynamic inventory slots");

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

        atelierButton.EmitSignal(Button.SignalName.Pressed);
        musicButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.StoredItems.Length == 2 && game.StoredItems[0] is >= 0 and <= 1 &&
              game.StoredItems[1] is >= 2 and <= 3,
            "Each workshop must only produce items from its category");
        Check(game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer").GetChildCount() == 2,
            "Inventory UI must render exactly one slot per real item");
        int firstItem = game.StoredItems[0];
        int secondItem = game.StoredItems[1];
        Check(placeButton.Disabled, "Placing requires an inventory selection");
        SelectInventorySlot(game, 0);
        Check(!placeButton.Disabled, "Placing must become available after selecting an item");
        placeButton.EmitSignal(Button.SignalName.Pressed);
        Check(!game.TradeActive && game.ShowcaseItems[0] == firstItem &&
              game.StoredItems.SequenceEqual(new[] { secondItem }),
            "Placing must move the selected item into a showcase");
        Check(game.GetNode<TextureRect>("World/ShowroomLeft/Showcases/Slot1/Icon").Texture != null,
            "The showcase must display the item's illustration");
        game._Process(game.BuyerWaitSeconds / 2d);
        Check(!game.TradeActive, "A buyer must wait before choosing a showcase item");
        game._Process(game.BuyerWaitSeconds / 2d + 0.1);
        Check(game.TradeActive && game.ItemIndex == firstItem && game.ShowcaseItems.All(item => item == -1),
            "A buyer must choose a displayed item after the wait");
        Check(world.ItemVisible && placeButton.Visible &&
              game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").Disabled &&
              game.GetNode<Label>("World/ShowroomLeft/Showcases/Slot1/ItemLabel").Text == "НА СДЕЛКЕ",
            "The active item's showcase must stay reserved during the trade");
        game.GetItemFromAtelier();
        SelectInventorySlot(game, 1);
        game.PlaceItemInShowcase(0);
        Check(game.StoredItems.Length == 2 && game.ShowcaseItems[0] == -1,
            "The reserved showcase must reject a new item");
        Check(!placeButton.Disabled, "Another showcase must remain available during a trade");
        placeButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.TradeActive && game.ShowcaseItems[1] >= 0 && game.StoredItems.SequenceEqual(new[] { secondItem }),
            "The player must be able to stock another showcase during a trade");
        game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot2").EmitSignal(Button.SignalName.Pressed);
        Check(!trashButton.Disabled, "A displayed item must be selectable for disposal");
        trashButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.TradeActive && game.ShowcaseItems[1] == -1 &&
              game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").Disabled,
            "Trash must remove a displayed item without releasing the reserved slot");
        foreach (string kind in new[] { "hat", "gloves", "guitar", "trumpet" })
            Check(game.GetNode<Sprite2D>($"World/TradingRoom/Item/{char.ToUpperInvariant(kind[0])}{kind[1..]}").Visible ==
                  (kind == TradeScene.GetItemKind(firstItem)), "Only the selected item illustration must be visible");
        int[] prices = [4, 9, 18, 12];
        Check(game.Percentage is >= -30 and <= 30, "Offer percentage is out of range");
        Check(game.OfferedPrice == TradeScene.PriceForPercent(prices[firstItem], game.Percentage),
            "Offer price does not match the percentage");
        Check(game.GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/OfferLabel").Text == $"Получим: {game.OfferedPrice}$",
            "The panel must show the amount received");

        game.Percentage = -30;
        game.ShowTrade();
        for (int remaining = 2; remaining >= 0; remaining--)
        {
            int oldCustomer = game.CustomerIndex;
            int oldPrice = game.OfferedPrice;
            game.RerollCustomer();
            Check(game.ItemIndex == firstItem && game.CustomerIndex != oldCustomer, "Reroll must keep the item and replace the buyer");
            Check(game.OfferedPrice != oldPrice && game.RerollsRemaining == remaining,
                "Reroll must change the price and consume one attempt");
        }
        Check(game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/RerollButton").Disabled,
            "Reroll button must disable at zero");

        game.DeclineTrade();
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(new[] { secondItem }) &&
              game.ShowcaseItems[0] == firstItem,
            "A declined item must return to its showcase");
        Check(!world.ItemVisible && placeButton.Visible, "The sale controls must hide after declining");
        game._Process(game.BuyerWaitSeconds + 0.1);
        int acceptedOffer = game.OfferedPrice;
        game.AcceptTrade();
        Check(game.Accepted == 1 && game.Score == acceptedOffer, "Accepting must record the sale");
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(new[] { secondItem }) &&
              game.ShowcaseItems.All(item => item == -1), "A sold item must leave the showcase permanently");
        game.GetItemFromAtelier();
        SelectInventorySlot(game, 1);
        Check(!trashButton.Disabled, "An inventory item must be selectable for disposal");
        trashButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.StoredItems.SequenceEqual(new[] { secondItem }) && trashButton.Disabled,
            "Trash must remove only the selected inventory item");
        game.AcceptTrade();
        Check(game.Accepted == 1, "An inactive trade must not be accepted twice");
        game._Input(rKey);
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(new[] { secondItem }),
            "R must not bypass the stored-item queue");

        SelectInventorySlot(game, 0);
        game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot3").EmitSignal(Button.SignalName.Pressed);
        game.GetItemFromAtelier();
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(4);
        int[] displayed = game.ShowcaseItems;
        game._Process(game.BuyerWaitSeconds + 0.1);
        int[] remainingShowcases = game.ShowcaseItems;
        Check(game.TradeActive && displayed[2] >= 0 && displayed[4] >= 0 &&
              remainingShowcases.Count(item => item >= 0) == 1 &&
              ((remainingShowcases[2] == -1 && game.ItemIndex == displayed[2]) ||
               (remainingShowcases[4] == -1 && game.ItemIndex == displayed[4])),
            "A buyer must choose one of the occupied showcases");
        game.DeclineTrade();
        Check(game.ShowcaseItems.SequenceEqual(displayed), "Declining must restore the chosen showcase");

        var camera = game.GetNode<Camera2D>("Camera2D");
        Check(camera.Position.Y > 1625, "The camera must start in the lower-left showroom");
        float oldZoom = camera.Zoom.X;
        for (int frame = 0; frame < 120; frame++) game._Process(1d / 60d);
        Check(Mathf.IsEqualApprox(camera.Zoom.X, oldZoom) && camera.Zoom.X is >= 0.4f and <= 1.8f,
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
        float startingX = camera.Position.X;
        game._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
        game._Input(new InputEventMouseMotion { Relative = new Vector2(100, 0) });
        Check(Mathf.IsEqualApprox(camera.Position.X, startingX - 100f / camera.Zoom.X), "Dragging must pan the camera");
        camera.Zoom = Vector2.One * 1.8f;
        camera.Position = new Vector2(-100, -100);
        game.ClampCamera();
        Vector2 halfView = game.GetViewportRect().Size / (2f * camera.Zoom);
        Check(camera.Position.X - halfView.X >= -240.01f && camera.Position.X - halfView.X < -200f &&
              camera.Position.Y - halfView.Y >= -240.01f && camera.Position.Y - halfView.Y < -200f,
            "The camera must allow a small view beyond the outer room edges");
        camera.Position = new Vector2(4582, 1103);
        game.ClampCamera();
        Check(camera.Position.X > 3622 && camera.Position.Y < 1625, "The camera must reach the upper-right workshop");
        game.ClampCamera();
        camera.Position = new Vector2(4582, 2183);
        game.ClampCamera();
        Check(camera.Position.X > 3622 && camera.Position.Y > 1625,
            "The camera must reach the lower-right showroom");
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
        game.GetItemFromAtelier();
        game.GetItemFromMusicWorkshop();
        int[] itemsBeforePlacement = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(3);
        Check(!game.TradeActive && game.StoredItems.Length == 1 &&
              game.ShowcaseItems[3] == itemsBeforePlacement[0],
            "Placing must move the selected item into a showcase without starting a trade");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(!game.TradeActive && game.ShowcaseItems[3] == itemsBeforePlacement[0] &&
              game.StoredItems[0] == itemsBeforePlacement[1],
            "Showcase contents and inventory must survive a reload");
        game._Process(game.BuyerWaitSeconds + 0.1);
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0],
            "A buyer must start a trade after waiting");
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
        Check(game.ShowcaseItems[3] == itemsBeforePlacement[0] &&
              game.ShowcaseItems[5] == itemsBeforePlacement[1] && game.StoredItems.Length == 0,
            "Declining after a reload must restore the reserved item without moving other goods");

        game.CurrentDay = 4;
        game.Score = game.FirstQuota - 1;
        game.EndDay();
        Check(game.Failed && !game.TradeActive && game.ShowcaseItems[3] == itemsBeforePlacement[0] &&
              game.ShowcaseItems[5] == itemsBeforePlacement[1] && game.StoredItems.Length == 0,
            "Failure at the deadline must preserve both unsold showcase items");
        game.GetItemFromAtelier();
        game._Input(new InputEventKey { Keycode = Key.R, Pressed = true });
        Check(game.StoredItems.Length == 0,
            "Failure and R must not create items");
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 1 && game.Score == 0 && game.StoredItems.Length == 0 &&
              game.ShowcaseItems.All(item => item == -1),
            "First failure must restore the empty day-one checkpoint");

        game.GetItemFromAtelier();
        game.GetItemFromMusicWorkshop();
        int[] checkpointItems = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(2);
        game.CurrentDay = 4;
        game.Score = game.FirstQuota + 12;
        game.EndDay();
        Check(game.QuotaStage == 1 && game.CurrentDay == 5 && game.Score == 12,
            "Paying the first quota must open day five with the remaining cash");
        Check(game.StoredItems.SequenceEqual(new[] { checkpointItems[1] }) &&
              game.ShowcaseItems[2] == checkpointItems[0] && !game.TradeActive,
            "The checkpoint must retain unsold showroom items");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.CurrentDay == 5 && game.Score == 12 && game.ShowcaseItems[2] == checkpointItems[0] &&
              game.StoredItems.SequenceEqual(new[] { checkpointItems[1] }),
            "The showroom, inventory and progress must survive a scene reload");
        game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot3").EmitSignal(Button.SignalName.Pressed);
        game.GetNode<Button>("World/Showroom/TrashBin").EmitSignal(Button.SignalName.Pressed);
        Check(game.ShowcaseItems[2] == -1, "Trash must remove the selected showroom item");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.ShowcaseItems[2] == -1 && game.StoredItems.SequenceEqual(new[] { checkpointItems[1] }),
            "A discarded showroom item must stay discarded after reload");
        game.ReturnToCheckpoint();
        Check(game.ShowcaseItems[2] == checkpointItems[0],
            "Returning to the checkpoint must restore its showroom contents");
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(4);
        game.CurrentDay = 6;
        game.Score = 1000;
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 5 && game.Score == 12 && !game.TradeActive &&
              game.StoredItems.SequenceEqual(new[] { checkpointItems[1] }) &&
              game.ShowcaseItems[2] == checkpointItems[0] && game.ShowcaseItems[4] == -1,
            "Checkpoint must restore cash, inventory and showroom items");

        game.CurrentDay = 7;
        game.Score = game.SecondQuota;
        game.EndDay();
        Check(game.Completed && game.QuotaStage == 2 && game.Score == 0,
            "Paying the second quota must finish the game");
        game.NewGame();
        Check(game.CurrentDay == 1 && game.Score == 0 && game.StoredItems.Length == 0 && !game.TradeActive &&
              game.ShowcaseItems.All(item => item == -1),
            "New game must reset progress without giving a free item");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);

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
        await ToSignal(this, SignalName.ProcessFrame);
        return game;
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
