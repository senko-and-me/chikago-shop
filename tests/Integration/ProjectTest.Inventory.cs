using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
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
        await PressEscapeForTest();
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
        await PressEscapeForTest();
        await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/NewGameButton");
        Check(!game.MusicWorkshopUnlocked && game.Score == 220 && game.StoredItems.Length == 0,
            "New game handler must reset the state");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Mouse controls and phantom trade regression checks passed");
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
}
