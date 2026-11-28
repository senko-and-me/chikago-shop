using System.Collections.Generic;
using Godot;

// The thought bubble keeps the requested item and its waiting timer together.
public partial class BuyerWaitWidget : Control
{
    // Edit ThoughtScale to change the thought bubble, item icon,
    // countdown text and waiting bar TOGETHER. 1 = old size; 2 = double.
    [Export(PropertyHint.Range, "0.5,4.0,0.1")]
    public float ThoughtScale { get; set; } = 2.0f;

    private static readonly Vector2 NativeBubbleSize = new(192, 125);

    private Control _thought = null!;
    private TextureRect _icon = null!;
    private ProgressBar _waitBar = null!;
    private Label _seconds = null!;
    private readonly Dictionary<int, Texture2D> _itemIcons = new();
    private int _itemIndex = -1;

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

        // The icon and timer share the upper row. The progress bar has its own row.
        var stack = new VBoxContainer
        {
            Name = "Stack",
            Position = new Vector2(32, 15),
            Size = new Vector2(146, 80),
            MouseFilter = MouseFilterEnum.Ignore
        };
        stack.AddThemeConstantOverride("separation", 3);
        _thought.AddChild(stack);
        var iconLine = new HBoxContainer { Name = "RequestRow", MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        stack.AddChild(iconLine);
        _icon = new TextureRect
        {
            Name = "ThoughtIcon", CustomMinimumSize = new Vector2(49, 50),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        iconLine.AddChild(_icon);
        var iconBackdrop = new Panel
        {
            ShowBehindParent = true,
            MouseFilter = MouseFilterEnum.Ignore
        };
        iconBackdrop.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("c6c6c6"),
            CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
        });
        _icon.AddChild(iconBackdrop);
        iconBackdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
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

    internal Rect2 TimerBounds() =>
        TutorialSpotlight.TargetBounds(_seconds).Merge(TutorialSpotlight.TargetBounds(_waitBar));

    public void SetState(int itemIndex, double remaining, double initial)
    {
        if (!Visible) Show();
        if (!_thought.Visible) _thought.Show();
        if (_itemIndex != itemIndex)
        {
            _itemIndex = itemIndex;
            _icon.Texture = GetRequestIcon(itemIndex);
        }
        _waitBar.MaxValue = initial;
        _waitBar.Value = initial - remaining;
        _seconds.Text = $"{Mathf.Max(0, (float)remaining):0.0}s";
        QueueRedraw();
    }

    private Texture2D GetRequestIcon(int itemIndex)
    {
        if (_itemIcons.TryGetValue(itemIndex, out var icon)) return icon;

        var artwork = GD.Load<Texture2D>(TradeScene.GetItemArtPath(itemIndex));
        using var image = artwork.GetImage();
        // Fit the drawn item into the shared icon size, removing each PNG's transparent padding.
        icon = new AtlasTexture { Atlas = artwork, Region = image.GetUsedRect(), FilterClip = true };
        _itemIcons.Add(itemIndex, icon);
        return icon;
    }

    public void EndTrade()
    {
        if (_thought != null) _thought.Hide();
        QueueRedraw();
    }

}
