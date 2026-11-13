using Godot;

// The Russian lettering is baked into the artist PNG; English uses the matching blank panel.
public static class CraftButtonPresentation
{
    public static void Apply(Button button, bool english)
    {
        var texture = GD.Load<Texture2D>(english ? "res://art/ui/panel_small.png" : "res://art/ui/create_button.png");
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
        {
            var style = (StyleBoxTexture)button.GetThemeStylebox(state).Duplicate();
            style.Texture = texture;
            button.AddThemeStyleboxOverride(state, style);
        }
        button.Text = "";
        var caption = button.GetNodeOrNull<Label>("EnglishCaption");
        if (caption == null)
        {
            caption = new Label
            {
                Name = "EnglishCaption", Text = "CREATE\nITEMS", AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            caption.AddThemeColorOverride("font_color", Colors.Black);
            caption.AddThemeConstantOverride("outline_size", 0);
            caption.AddThemeFontSizeOverride("font_size", 60);
            button.AddChild(caption);
            caption.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            caption.OffsetLeft = 18;
            caption.OffsetRight = -60;
            caption.OffsetTop = 12;
            caption.OffsetBottom = -12;
        }
        caption.Visible = english;
        var hotspot = button.GetNode<Control>("RarityHotspot");
        var help = hotspot.GetNodeOrNull<TextureRect>("EnglishHelp");
        if (help == null)
        {
            help = new TextureRect
            {
                Name = "EnglishHelp",
                Texture = new AtlasTexture { Atlas = GD.Load<Texture2D>("res://art/ui/create_button.png"), Region = new Rect2(1690, 55, 250, 250) },
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            hotspot.AddChild(help);
            help.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        help.Visible = english;
    }
}
