using System.Collections.Generic;
using Godot;

// Only world-space UI needs compensation: the HUD already has a stable screen size.
public partial class ZoomResponsiveUi : Node
{
    private Camera2D _camera = null!;
    private readonly List<(Control Control, Vector2 Scale)> _elements = [];
    private readonly List<(CanvasItem Item, Color Color)> _headers = [];
    private float _lastZoom = float.NaN;
    private bool _layoutDirty = true;

    public override void _Ready()
    {
        var game = GetParent();
        _camera = game.GetNode<Camera2D>("Camera2D");
        string[] rooms = ["Atelier", "MusicWorkshop", "Showroom", "ShowroomLeft", "TradingRoom"];
        foreach (string room in rooms)
        {
            Node node = game.GetNode("World/" + room);
            foreach (string name in new[] { "Header", "NameLabel", "HintLabel", "ShowroomHeader",
                         "ShowroomLabel", "ShowroomHint", "SalesRoomLabel" })
                if (node.GetNodeOrNull<CanvasItem>(name) is { } header)
                    _headers.Add((header, header.Modulate));
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

    public override void _Process(double delta)
    {
        if (_layoutDirty || _lastZoom != _camera.Zoom.X) UpdateForZoom(_camera.Zoom.X);
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
        float alpha = Mathf.SmoothStep(0.23f, 0.35f, zoom);
        foreach (var entry in _headers)
        {
            entry.Item.Visible = zoom > 0.23f;
            Color color = entry.Color;
            color.A *= alpha;
            entry.Item.Modulate = color;
        }
    }
}
