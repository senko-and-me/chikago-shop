using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{

    private async Task CheckRenderedRoomStroke(NinePatchRect original, Rect2 room)
    {
        // Render the actual nine-patch in isolation: measure painted pixels, not the Control's bounds.
        var viewport = new SubViewport { Size = new Vector2I(512, 384), TransparentBg = true,
            World2D = new World2D(), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        Root.AddChild(viewport);
        try
        {
            var copy = (NinePatchRect)original.Duplicate();
            const float scale = 0.2f;
            Vector2 start = new(48, 48);
            copy.Position = start + (original.Position - room.Position) * scale;
            copy.Scale = original.Scale * scale;
            viewport.AddChild(copy);
            for (int i = 0; i < 3; i++) await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var pixels = viewport.GetTexture().GetImage();
            Vector2 end = start + room.Size * scale;
            for (int y = 0; y < pixels.GetHeight(); y++)
            for (int x = 0; x < pixels.GetWidth(); x++)
            {
                Color pixel = pixels.GetPixel(x, y);
                Check(pixel.A < 0.1f || (pixel.R < 0.01f && pixel.G < 0.01f && pixel.B < 0.01f),
                    "The rendered frame must contain no opaque white flecks at its joints");
            }
            foreach (var edge in new[] { (true, start.X), (true, end.X), (false, start.Y), (false, end.Y) })
            {
                float mass = 0, weighted = 0;
                int from = (int)(edge.Item1 ? start.Y + room.Size.Y * scale / 4 : start.X + room.Size.X * scale / 4);
                int to = (int)(edge.Item1 ? end.Y - room.Size.Y * scale / 4 : end.X - room.Size.X * scale / 4);
                for (int across = Mathf.FloorToInt(edge.Item2 - 8); across <= Mathf.CeilToInt(edge.Item2 + 8); across++)
                for (int along = from; along < to; along++)
                {
                    float alpha = pixels.GetPixel(edge.Item1 ? across : along, edge.Item1 ? along : across).A;
                    mass += alpha;
                    weighted += (across + 0.5f) * alpha;
                }
                Check(mass > 0 && Mathf.Abs(weighted / mass - edge.Item2) < 0.8f,
                    "The painted stroke must be centered on each room edge, with no inset or displaced outline");
                if (!edge.Item1)
                    Check(mass / (to - from) < 4.0f,
                        "Horizontal strokes must stay thinner than four pixels in the reference render");
                int first = Mathf.CeilToInt(edge.Item1 ? start.Y : start.X);
                int last = Mathf.FloorToInt(edge.Item1 ? end.Y : end.X);
                for (int along = first; along <= last; along++)
                    Check(pixels.GetPixel(edge.Item1 ? Mathf.FloorToInt(edge.Item2) : along,
                            edge.Item1 ? along : Mathf.FloorToInt(edge.Item2)).A > 0.5f,
                        $"The black seam must remain continuous through room corners without transparent pinholes: {original.GetParent().Name} edge {edge}, along {along}, end {end}");
            }
        }
        finally { viewport.QueueFree(); }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
