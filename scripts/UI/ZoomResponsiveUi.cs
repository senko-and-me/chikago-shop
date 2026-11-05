using System.Collections.Generic;
using Godot;

// Only world-space UI needs compensation: the HUD already has a stable screen size.
public partial class ZoomResponsiveUi : Node
{
    private Camera2D _camera = null!;
    private readonly List<(Control Control, Vector2 Scale)> _elements = [];
    private readonly List<(CanvasItem Item, Color Color)> _headers = [];

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

    private void Register(Control control) => _elements.Add((control, control.Scale));

    public override void _Process(double delta) => UpdateForZoom(_camera.Zoom.X);

    public void UpdateForZoom(float zoom)
    {
        // Partial compensation retains a sense of depth without letting panels fill the room.
        float scale = Mathf.Clamp(1f / Mathf.Sqrt(Mathf.Max(zoom, 0.01f)), 1f, 1.65f);
        foreach (var entry in _elements)
        {
            entry.Control.PivotOffset = entry.Control.Size / 2f;
            entry.Control.Scale = entry.Scale * scale;
        }
        float alpha = Mathf.SmoothStep(0.3f, 0.5f, zoom);
        foreach (var entry in _headers)
        {
            entry.Item.Visible = zoom > 0.3f;
            Color color = entry.Color;
            color.A *= alpha;
            entry.Item.Modulate = color;
        }
    }
}
