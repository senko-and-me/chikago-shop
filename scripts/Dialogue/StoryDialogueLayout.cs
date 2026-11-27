using Godot;

// Shared presentation for the prologue, the visitor and the quota scenes.
internal static class StoryDialogueLayout
{
    public const int BodySize = 20;
    public const float InactivePortraitTone = 0.12f;
    public static readonly Color Shade = new(0.417f, 0.396f, 0.391f, 0.76f);
    public static readonly Color TextColor = new(0.93f, 0.91f, 0.88f);
    private static readonly Color SpeakerColor = new(0.88f, 0.82f, 0.72f);

    public static string LineText(string key)
    {
        string text = GameLocalization.Text(key);
        return text.StartsWith('—') ? text[1..].TrimStart() : text;
    }

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

    public static void StyleSkipButton(TextureButton button)
    {
        button.TextureNormal = GD.Load<Texture2D>(GameLocalization.Language == "en"
            ? "res://art/ui/en/skip.png" : "res://art/ui/ru/skip.png");
        button.IgnoreTextureSize = true;
        button.StretchMode = TextureButton.StretchModeEnum.Scale;
        button.AnchorLeft = button.AnchorRight = 1;
        button.OffsetLeft = -204;
        button.OffsetRight = -24;
        button.OffsetTop = 24;
        button.OffsetBottom = 78;
        button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        if (!button.HasNode("DropShadow")) ButtonDropShadow.Attach(button);
    }

    public static void StylePanel(PanelContainer panel, Label speaker)
    {
        // Tuck the cut-off portrait edges behind the cloud's wispy foreground.
        var characters = panel.GetParent().GetNode<Control>("Characters");
        foreach (TextureRect portrait in characters.GetChildren())
        {
            portrait.AnchorTop += 0.035f;
            portrait.AnchorBottom += 0.035f;
        }
        panel.GetParent().MoveChild(panel, characters.GetIndex() + 1);
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
