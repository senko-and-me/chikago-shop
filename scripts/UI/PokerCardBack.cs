using Godot;

// Code-drawn monochrome card backs; no additional bitmap assets are needed.
public partial class PokerCardBack : Control
{
    public bool IsDeck { get; set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
        QueueRedraw();
    }

    public override void _Draw()
    {
        int layers = IsDeck ? 3 : 1;
        for (int layer = layers - 1; layer >= 0; layer--)
        {
            var box = new StyleBoxFlat
            {
                BgColor = new Color("f4f2ed"), BorderColor = new Color("181818"),
                BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
            };
            DrawStyleBox(box, new Rect2(new Vector2(layer * 4, layer * -4), Size));
        }
        Rect2 ink = new Rect2(Vector2.Zero, Size).Grow(-8);
        DrawRect(ink, new Color("232323"));
        const float step = 12;
        for (float offset = -ink.Size.Y; offset < ink.Size.X; offset += step)
        {
            float start = Mathf.Max(0, -offset), end = Mathf.Min(ink.Size.Y, ink.Size.X - offset);
            if (end <= start) continue;
            DrawLine(ink.Position + new Vector2(offset + start, start),
                ink.Position + new Vector2(offset + end, end), new Color("777777"), 1, true);
            DrawLine(ink.Position + new Vector2(offset + start, ink.Size.Y - start),
                ink.Position + new Vector2(offset + end, ink.Size.Y - end), new Color("777777"), 1, true);
        }
        Vector2 center = Size / 2;
        float radius = Mathf.Min(Size.X, Size.Y) * 0.19f;
        DrawColoredPolygon(new[] { center + Vector2.Up * radius, center + Vector2.Right * radius,
            center + Vector2.Down * radius, center + Vector2.Left * radius }, new Color("ededed"));
    }
}
