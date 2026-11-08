using Godot;

// One final presentation pass covers imported art, runtime drawing and custom shaders.
// Exceptions stay in their original canvases, preserving their input paths and draw order.
public partial class MonochromePresentation : CanvasLayer
{
    private const string ClockGroup = "presentation_color_clock";
    private const string TableGroup = "presentation_color_table";
    private const string ModalGroup = "presentation_covering_modal";
    private ShaderMaterial _material;

    public static void PreserveClock(TextureRect clock) => clock.AddToGroup(ClockGroup);
    public static void PreserveTable(Control table) => table.AddToGroup(TableGroup);
    public static void CoverExceptions(Control modal) => modal.AddToGroup(ModalGroup);

    public override void _Ready()
    {
        Layer = 200; // Above the existing UI, pause (100), crafting (110), and day cards (120).
        ProcessMode = ProcessModeEnum.Always;
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/monochrome.gdshader") };
        var filter = new ColorRect
        {
            Name = "Filter", Material = _material, Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
        AddChild(filter);
        filter.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        RenderingServer.FramePreDraw += UpdateExceptions;
    }

    public override void _ExitTree() => RenderingServer.FramePreDraw -= UpdateExceptions;

    private void UpdateExceptions()
    {
        var feedback = ButtonFeedbackController.Instance?.GetVisualFrame() ?? default;
        _material.SetShaderParameter("button_rect", Normalize(feedback.Bounds));
        _material.SetShaderParameter("button_accent", feedback.Accent);
        _material.SetShaderParameter("button_strength", feedback.Strength);
        _material.SetShaderParameter("button_pressed", feedback.Pressed);
        _material.SetShaderParameter("button_flash", feedback.Flash);
        _material.SetShaderParameter("clock_rect", Vector4.Zero);
        _material.SetShaderParameter("table_rect_0", Vector4.Zero);
        _material.SetShaderParameter("table_rect_1", Vector4.Zero);
        foreach (var node in GetTree().GetNodesInGroup(ModalGroup))
            if (node is Control modal && modal.IsVisibleInTree()) return;

        foreach (var node in GetTree().GetNodesInGroup(ClockGroup))
        {
            if (node is not TextureRect clock || !clock.IsVisibleInTree() || clock.Texture == null) continue;
            // Clock uses KeepAspectCentered. Preserve only the fitted sprite, not the control padding.
            Vector2 textureSize = clock.Texture.GetSize();
            if (textureSize.X <= 0 || textureSize.Y <= 0) continue;
            float scale = Mathf.Min(clock.Size.X / textureSize.X, clock.Size.Y / textureSize.Y);
            Vector2 drawnSize = textureSize * scale;
            Rect2 rect = clock.GetGlobalTransformWithCanvas() * new Rect2((clock.Size - drawnSize) / 2, drawnSize);
            _material.SetShaderParameter("clock_rect", Normalize(rect));
            _material.SetShaderParameter("clock_texture", clock.Texture);
            break;
        }

        int index = 0;
        foreach (var node in GetTree().GetNodesInGroup(TableGroup))
        {
            if (node is not Control table || !table.IsVisibleInTree()) continue;
            Rect2 rect = table.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, table.Size);
            _material.SetShaderParameter(index == 0 ? "table_rect_0" : "table_rect_1", Normalize(rect));
            if (++index == 2) break;
        }
    }

    private Vector4 Normalize(Rect2 rect)
    {
        Vector2 size = GetViewport().GetVisibleRect().Size;
        return size.X > 0 && size.Y > 0
            ? new Vector4(rect.Position.X / size.X, rect.Position.Y / size.Y, rect.Size.X / size.X, rect.Size.Y / size.Y)
            : Vector4.Zero;
    }
}
