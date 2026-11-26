using System;
using System.IO;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task<Image> CapturePresentation(string name = null)
    {
        // Allow layout, popup placement and screen-texture sampling to settle.
        for (int frame = 0; frame < 3; frame++) await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = Root.GetTexture().GetImage();
        Check(image != null && !image.IsEmpty(), "Presentation tests require a rendered viewport (run without --headless)");
        if (name != null)
            image.SavePng(Path.Combine(Path.GetTempPath(), "chikago-hf67-" + name + ".png"));
        return image;
    }

    private static bool HasChroma(Color color) =>
        Mathf.Max(color.R, Mathf.Max(color.G, color.B)) - Mathf.Min(color.R, Mathf.Min(color.G, color.B)) > 2.1f / 255;

    private static Rect2 ScreenBounds(Control control) =>
        control.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, control.Size);

    private void CheckRenderedGray(Image image, params Rect2[] exceptions)
    {
        Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
        for (int y = 1; y < image.GetHeight(); y += 3)
        for (int x = 1; x < image.GetWidth(); x += 3)
        {
            Vector2 point = new Vector2(x, y) / scale;
            bool excepted = false;
            foreach (Rect2 exception in exceptions) excepted |= exception.Grow(1).HasPoint(point);
            if (!excepted)
                Check(!HasChroma(image.GetPixel(x, y)), $"Unexpected color outside exceptions at {x},{y}");
        }
    }

    private int CountRenderedColor(Image image, Rect2 bounds)
    {
        Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
        int count = 0;
        for (int y = Math.Max(0, (int)(bounds.Position.Y * scale.Y)); y < Math.Min(image.GetHeight(), bounds.End.Y * scale.Y); y++)
        for (int x = Math.Max(0, (int)(bounds.Position.X * scale.X)); x < Math.Min(image.GetWidth(), bounds.End.X * scale.X); x++)
            if (HasChroma(image.GetPixel(x, y))) count++;
        return count;
    }

}
