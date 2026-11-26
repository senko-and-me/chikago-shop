using System.Collections.Generic;
using Godot;

// Only world-space UI needs compensation: the HUD already has a stable screen size.
public partial class ZoomResponsiveUi : Node
{
    private WorldCamera _camera = null!;
    private readonly List<(Control Control, Vector2 Scale)> _elements = [];
    private readonly List<(CanvasItem Item, Color Color, bool Visible)> _headers = [];
    private Control _sale = null!;
    private Control _bubble;
    private Vector2 _saleHome, _saleScale, _bubbleHome, _bubbleScale;
    private float _lastZoom = float.NaN;
    private bool _layoutDirty = true;

    public override void _Ready()
    {
        var game = GetParent();
        _camera = game.GetNode<WorldCamera>("Camera2D");
        GetViewport().SizeChanged += InvalidateLayout;
        string[] rooms = ["Atelier", "MusicWorkshop", "Showroom", "ShowroomLeft", "TradingRoom"];
        foreach (string room in rooms)
        {
            Node node = game.GetNode("World/" + room);
            foreach (string name in new[] { "Header", "NameLabel", "ShowroomHeader",
                         "ShowroomLabel", "SalesRoomLabel" })
                if (node.GetNodeOrNull<CanvasItem>(name) is { } header)
                    _headers.Add((header, header.Modulate, header.Visible));
        }
        foreach (string room in new[] { "Atelier", "MusicWorkshop" })
        {
            Register(game.GetNode<Control>($"World/{room}/GetItemButton/PriceHint"));
        }
        _sale = game.GetNode<Control>("World/TradingRoom/Item/TradePanel");
        _saleHome = _sale.Position;
        _saleScale = _sale.Scale;
        foreach (string room in new[] { "Showroom", "ShowroomLeft" })
            foreach (Node slot in game.GetNode($"World/{room}/Showcases").GetChildren())
                Register(slot.GetNode<Control>("ItemLabel"));
        UpdateForZoom(_camera.Zoom.X);
    }

    private void Register(Control control)
    {
        _elements.Add((control, control.Scale));
        control.Resized += InvalidateLayout;
        if (control.Name == "PriceHint")
            control.GetParent<Control>().Resized += InvalidateLayout;
    }

    private void InvalidateLayout() => _layoutDirty = true;

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged) InvalidateLayout();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= InvalidateLayout;

    public override void _Process(double delta)
    {
        if (_layoutDirty || _lastZoom != _camera.Zoom.X) UpdateForZoom(_camera.Zoom.X);
        else LayoutSale(_camera.Zoom.X);
    }

    public void UpdateForZoom(float zoom)
    {
        _lastZoom = zoom;
        _layoutDirty = false;
        // Partial compensation retains a sense of depth without letting panels fill the room.
        float scale = Mathf.Clamp(1f / Mathf.Sqrt(Mathf.Max(zoom, 0.01f)), 1f, 1.65f);
        foreach (var entry in _elements)
        {
            entry.Control.PivotOffset = entry.Control.Size / 2f;
            entry.Control.Scale = entry.Scale * scale;
            if (entry.Control.Name == "PriceHint")
            {
                // Keep a constant screen-space gap after scaling around the center.
                var button = entry.Control.GetParent<Control>();
                float gap = 8 / Mathf.Max(zoom, 0.01f);
                Vector2 pivot = entry.Control.PivotOffset;
                entry.Control.Position = new Vector2((button.Size.X - entry.Control.Size.X) / 2,
                    -gap - pivot.Y - (entry.Control.Size.Y - pivot.Y) * entry.Control.Scale.Y);
            }
        }
        LayoutSale(zoom);
        float hideAt = _camera.OverviewZoom * 1.2f;
        float alpha = Mathf.SmoothStep(hideAt, _camera.OverviewZoom * 1.6f, zoom);
        foreach (var entry in _headers)
        {
            entry.Item.Visible = entry.Visible && zoom > hideAt;
            Color color = entry.Color;
            color.A *= alpha;
            entry.Item.Modulate = color;
        }
    }

    private void LayoutSale(float zoom)
    {
        if (_bubble == null)
        {
            _bubble = GetParent().GetNodeOrNull<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble");
            if (_bubble != null)
            {
                _bubbleHome = _bubble.Position;
                _bubbleScale = _bubble.Scale;
                _bubble.ZAsRelative = false;
                _bubble.ZIndex = 126;
            }
        }

        _sale.PivotOffset = new Vector2(_sale.Size.X / 2f, _sale.Size.Y);
        float inverseZoom = 1f / Mathf.Max(0.01f, zoom);
        Vector2 saleScale = _saleScale * Mathf.Clamp(inverseZoom, 1f, 2.6f);
        if (!_sale.IsVisibleInTree() && _bubble?.IsVisibleInTree() != true)
        {
            _sale.Scale = saleScale;
            return;
        }

        Rect2 screen = GetViewport().GetVisibleRect().Grow(-12);
        Rect2 sale = Project(_sale, _saleHome, saleScale);
        bool showBubble = _bubble?.IsVisibleInTree() == true;
        const float gap = 12;
        if (!showBubble)
        {
            Place(_sale, _saleHome, saleScale, sale, ClampTo(sale, screen));
            return;
        }

        _bubble.PivotOffset = _bubble.Size / 2f;
        Vector2 bubbleHome = _bubbleHome + _bubble.PivotOffset * (_bubbleScale - Vector2.One);
        Vector2 bubbleScale = _bubbleScale * Mathf.Clamp(Mathf.Sqrt(inverseZoom), 1f, 1.65f);
        Rect2 bubble = Project(_bubble, bubbleHome, bubbleScale);
        if (_sale.IsVisibleInTree())
        {
            float fit = Mathf.Min(1, Mathf.Min(
                (screen.Size.X - gap) / (sale.Size.X + bubble.Size.X),
                screen.Size.Y / Mathf.Max(sale.Size.Y, bubble.Size.Y)));
            if (fit < 1)
            {
                saleScale *= fit;
                bubbleScale *= fit;
                sale = Project(_sale, _saleHome, saleScale);
                bubble = Project(_bubble, bubbleHome, bubbleScale);
            }
        }

        Vector2 salePosition = ClampTo(sale, screen);
        Vector2 bubblePosition = ClampTo(bubble, screen);
        bubblePosition.X = Mathf.Min(screen.End.X - bubble.Size.X,
            bubblePosition.X + 22f * Mathf.Clamp(1f - zoom, 0f, 1f));
        bool sharedRows = salePosition.Y < bubblePosition.Y + bubble.Size.Y &&
            bubblePosition.Y < salePosition.Y + sale.Size.Y;
        if (_sale.IsVisibleInTree() && sharedRows)
        {
            salePosition.X = Mathf.Min(salePosition.X, screen.End.X - bubble.Size.X - gap - sale.Size.X);
            bubblePosition.X = Mathf.Max(bubblePosition.X, salePosition.X + sale.Size.X + gap);
        }
        Place(_sale, _saleHome, saleScale, sale, salePosition);
        Place(_bubble, bubbleHome, bubbleScale, bubble, bubblePosition);
    }

    private static Rect2 Project(Control control, Vector2 home, Vector2 scale)
    {
        Vector2 origin = home + control.PivotOffset * (Vector2.One - scale);
        return control.GetParent<CanvasItem>().GetGlobalTransformWithCanvas() *
            new Rect2(origin, scale * control.Size);
    }

    private static Vector2 ClampTo(Rect2 bounds, Rect2 screen) =>
        bounds.Position.Clamp(screen.Position, screen.Position.Max(screen.End - bounds.Size));

    private static void Place(Control control, Vector2 home, Vector2 scale, Rect2 bounds, Vector2 position)
    {
        var parent = control.GetParent<CanvasItem>().GetGlobalTransformWithCanvas().AffineInverse();
        control.Scale = scale;
        control.Position = home + parent.BasisXform(position - bounds.Position);
    }

}
