using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
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
}
