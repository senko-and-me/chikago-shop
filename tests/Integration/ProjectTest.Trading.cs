using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
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
}
