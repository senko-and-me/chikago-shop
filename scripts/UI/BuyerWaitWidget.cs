using Godot;

// World-space thought bubble: an icon for the requested workshop, a red-filling
// speed-bonus countdown; sale bonuses are already included in the dynamic offer.
public partial class BuyerWaitWidget : Control
{
    // Edit ThoughtScale to change the thought bubble, category icon,
    // countdown text and waiting bar TOGETHER. 1 = old size; 2 = double.
    [Export(PropertyHint.Range, "0.5,4.0,0.1")]
    public float ThoughtScale { get; set; } = 2.0f;

    private static readonly Vector2 NativeBubbleSize = new(192, 125);

    private Control _thought = null!;
    private TextureRect _icon = null!;
    private ProgressBar _waitBar = null!;
    private Label _seconds = null!;
    private bool _music;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Theme = GD.Load<Theme>("res://themes/game_theme.tres");
        // Use the player's original hand-drawn thought texture instead of
        // a rounded Godot StyleBox. The texture already includes trailing dots.
        float scale = Mathf.Max(0.1f, ThoughtScale);
        _thought = new Control
        {
            Name = "ThoughtBubble",
            // Scale about the old bubble center, not the top-left corner.
            // This keeps the bubble above its buyer instead of drifting away.
            Position = -NativeBubbleSize * ((scale - 1f) * 0.5f),
            Size = NativeBubbleSize,
            Scale = Vector2.One * scale,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_thought);

        var background = new TextureRect
        {
            Name = "ThoughtBubbleArtwork",
            Texture = GD.Load<Texture2D>("res://art/ui/buyer_thought_bubble.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _thought.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Preserve the category icon, countdown and red waiting indicator
        // inside the oval portion of the supplied thought artwork.
        var stack = new VBoxContainer
        {
            Name = "Stack",
            Position = new Vector2(32, 15),
            Size = new Vector2(146, 80),
            MouseFilter = MouseFilterEnum.Ignore
        };
        stack.AddThemeConstantOverride("separation", 3);
        _thought.AddChild(stack);
        var iconLine = new HBoxContainer { Name = "Category", MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        stack.AddChild(iconLine);
        _icon = new TextureRect
        {
            Name = "ThoughtIcon", CustomMinimumSize = new Vector2(49, 50),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        iconLine.AddChild(_icon);
        _seconds = new Label
        {
            Name = "RemainingSeconds", CustomMinimumSize = new Vector2(53, 27),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _seconds.AddThemeFontSizeOverride("font_size", 20);
        _seconds.AddThemeColorOverride("font_color", new Color("222222"));
        iconLine.AddChild(_seconds);
        _waitBar = new ProgressBar
        {
            Name = "RedWaitingBar", MinValue = 0, MaxValue = TradeScene.BuyerWaitDuration,
            Value = 0, ShowPercentage = false,
            CustomMinimumSize = new Vector2(73, 15),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore
        };
        var track = new StyleBoxFlat
        {
            BgColor = new Color("f4f4f4"), BorderColor = Colors.Black,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2
        };
        var fill = new StyleBoxFlat { BgColor = new Color("c43d3d") };
        _waitBar.AddThemeStyleboxOverride("background", track);
        _waitBar.AddThemeStyleboxOverride("fill", fill);
        stack.AddChild(_waitBar);
        _thought.Hide();
        Show();
    }

    public void SetState(bool music, double remaining)
    {
        if (!Visible) Show();
        if (!_thought.Visible) _thought.Show();
        if (_music != music || _icon.Texture == null)
        {
            _music = music;
            _icon.Texture = GD.Load<Texture2D>(music
                ? "res://art/ui/buyer_wish_music.png" : "res://art/ui/buyer_wish_atelier.png");
            _icon.TooltipText = music ? "Музыкальные инструменты" : "Ателье";
        }
        _waitBar.Value = _waitBar.MaxValue - remaining;
        _seconds.Text = $"{Mathf.Max(0, (float)remaining):0.0}s";
        QueueRedraw();
    }

    public void EndTrade()
    {
        if (_thought != null) _thought.Hide();
        QueueRedraw();
    }

}
