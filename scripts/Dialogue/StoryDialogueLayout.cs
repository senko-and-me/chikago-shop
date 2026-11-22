using Godot;

// Shared presentation for the prologue, the visitor and the quota scenes.
internal static class StoryDialogueLayout
{
    public const int BodySize = 20;
    public const float InactivePortraitTone = 0.12f;
    public static readonly Color Shade = new(0.417f, 0.396f, 0.391f, 0.76f);
    public static readonly Color TextColor = new(0.93f, 0.91f, 0.88f);
    private static readonly Color SpeakerColor = new(0.88f, 0.82f, 0.72f);

    public static void FrameShowroom(TextureRect background)
    {
        // Frame the room around the cast, with the doorway edges near the screen edge.
        background.AnchorLeft = -0.071f;
        background.AnchorTop = -0.004f;
        background.AnchorRight = 1.071f;
        background.AnchorBottom = 1.138f;
    }

    public static void StyleHeading(Label heading)
    {
        heading.AnchorRight = 1;
        heading.OffsetTop = 28;
        heading.HorizontalAlignment = HorizontalAlignment.Center;
        heading.MouseFilter = Control.MouseFilterEnum.Ignore;
        heading.AddThemeColorOverride("font_color", SpeakerColor);
        heading.AddThemeConstantOverride("line_spacing", 8);
        heading.AddThemeFontSizeOverride("font_size", 18);
    }

    public static void StylePanel(PanelContainer panel, Label speaker)
    {
        // Draw the wispy top edge behind the portraits. The text starts in the
        // reserved lower band, where it cannot cover any of the character art.
        panel.GetParent().MoveChild(panel, panel.GetParent().GetNode("Shade").GetIndex() + 1);
        panel.AnchorLeft = 0.11f;
        panel.AnchorRight = 0.89f;
        panel.AnchorTop = 0.754f;
        panel.AnchorBottom = 1;
        panel.AddThemeStyleboxOverride("panel", GD.Load<StyleBoxTexture>("res://themes/story_dialogue_cloud.tres"));
        panel.Material = GD.Load<ShaderMaterial>("res://themes/story_dialogue_cloud_edges.tres");
        panel.GetNode<VBoxContainer>("Content").AddThemeConstantOverride("separation", 5);
        speaker.MouseFilter = Control.MouseFilterEnum.Ignore;
        speaker.AddThemeColorOverride("font_color", SpeakerColor);
        speaker.AddThemeFontSizeOverride("font_size", BodySize);
    }

    public static void AddPortrait(Control parent, string name, Texture2D texture, float center, float top = 0.235f, float bottom = 0.755f)
    {
        parent.AddChild(new TextureRect
        {
            Name = name,
            AnchorLeft = center - 0.10f,
            AnchorTop = top,
            AnchorRight = center + 0.10f,
            AnchorBottom = bottom,
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }
}
