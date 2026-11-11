using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task WaitForDayCardAnimation(Node game)
    {
        var card = game is DayTransitionCard direct ? direct : game.GetNodeOrNull<DayTransitionCard>("DayTransitionCard");
        double deadline = Time.GetTicksMsec() + 5000;
        while (card != null && GodotObject.IsInstanceValid(card) && card.IsAnimating)
        {
            Check(Time.GetTicksMsec() < deadline, "Day-card animation must finish while gameplay is paused");
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }

    private async Task PressEscapeForTest()
    {
        Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Escape }, true);
        Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Escape }, true);
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private static string ExpectedItemTooltip(int index) =>
        $"{TradeScene.GetItemName(index)}\nРедкость: {TradeScene.GetItemRarity(index)}\nСтоимость: {TradeScene.GetItemBasePrice(index)}$";

    private async Task ClickControl(Node game, string path)
    {
        if (path.StartsWith("DayTransitionCard/", StringComparison.Ordinal)) await WaitForDayCardAnimation(game);
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
        if (path.StartsWith("DayTransitionCard/", StringComparison.Ordinal)) await WaitForDayCardAnimation(game);
    }

    private static void AdvanceDayForTest(TradeScene game)
    {
        if (!game.DayClosed) game.EndDay();
        game.EndDay();
        var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
        if (card.IsOpen)
        {
            // Gameplay fixture setup skips presentation; dealing has its own actual-input tests.
            game.DayUpgrades.TrySkip();
            game.RefreshDayPresentation();
            card.Close();
        }
    }

    private async Task<TradeScene> CreateGame(PackedScene scene)
    {
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = SavePath;
        Root.AddChild(game);
        game.SetProcess(false);
        game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
        await ToSignal(this, SignalName.ProcessFrame);
        await WaitForDayCardAnimation(game);
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
