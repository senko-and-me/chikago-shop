using System.Collections.Generic;
using Godot;

// Only world-space UI needs compensation: the HUD already has a stable screen size.
public partial class ZoomResponsiveUi : Node
{
    private WorldCamera _camera = null!;
    private readonly List<(Control Control, Vector2 Scale)> _elements = [];
    private readonly List<(CanvasItem Item, Color Color)> _headers = [];
    private readonly List<(Control Plate, Label Title, Vector2 Center, float MaxWidth)> _titles = [];
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
            foreach (string name in new[] { "HintLabel", "ShowroomHint" })
                node.GetNodeOrNull<CanvasItem>(name)?.Hide();
            foreach (string name in new[] { "Header", "NameLabel", "ShowroomHeader",
                         "ShowroomLabel", "SalesRoomLabel" })
                if (node.GetNodeOrNull<CanvasItem>(name) is { } header)
                    _headers.Add((header, header.Modulate));
            var plate = node.GetNodeOrNull<Control>("Header") ?? node.GetNodeOrNull<Control>("ShowroomHeader");
            var title = node.GetNodeOrNull<Label>("NameLabel") ?? node.GetNodeOrNull<Label>("ShowroomLabel");
            if (plate != null && title != null)
                _titles.Add((plate, title, plate.Position + plate.Size * plate.Scale / 2, plate.Size.X * plate.Scale.X * 0.65f));
        }
        foreach (string room in new[] { "Atelier", "MusicWorkshop" })
        {
            Register(game.GetNode<Control>($"World/{room}/GetItemButton/PriceHint"));
        }
        Register(game.GetNode<Control>("World/TradingRoom/Item/TradePanel"));
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
    }

    public void UpdateForZoom(float zoom)
    {
        _lastZoom = zoom;
        if (_layoutDirty)
            foreach (var entry in _titles)
            {
                var font = entry.Title.GetThemeFont("font");
                int fontSize = entry.Title.GetThemeFontSize("font_size");
                float textWidth = font.GetStringSize(entry.Title.Tr(entry.Title.Text), fontSize: fontSize).X;
                float width = Mathf.Clamp(textWidth + 96, 320, entry.MaxWidth);
                Vector2 size = new(width, 160);
                entry.Plate.Size = size / entry.Plate.Scale;
                entry.Plate.Position = entry.Center - size / 2;
                entry.Title.Size = new Vector2(width - 80, Mathf.Max(78, font.GetHeight(fontSize)));
                entry.Title.Position = entry.Center - entry.Title.Size / 2;
                entry.Title.VerticalAlignment = VerticalAlignment.Center;
            }
        _layoutDirty = false;
        // Partial compensation retains a sense of depth without letting panels fill the room.
        float scale = Mathf.Clamp(1f / Mathf.Sqrt(Mathf.Max(zoom, 0.01f)), 1f, 1.65f);
        foreach (var entry in _elements)
        {
            entry.Control.PivotOffset = entry.Control.Size / 2f;
            entry.Control.Scale = entry.Scale * (entry.Control.Name == "TradePanel"
                ? Mathf.Clamp(1f / Mathf.Max(zoom, 0.01f), 0.6f, 1.2f)
                : scale);
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
        float hideAt = _camera.OverviewZoom * 1.2f;
        float alpha = Mathf.SmoothStep(hideAt, _camera.OverviewZoom * 1.6f, zoom);
        foreach (var entry in _headers)
        {
            entry.Item.Visible = zoom > hideAt;
            Color color = entry.Color;
            color.A *= alpha;
            entry.Item.Modulate = color;
        }
    }
}
