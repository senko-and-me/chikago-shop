using Godot;

// Interactive poster on the cashier room wall. It lightly highlights on hover
// and opens a centered full-size poster with a small fade, like the card overlays.
public partial class TradingRoomPosterHotspot : Node2D
{
    [Export] public string ThumbnailPath { get; set; } = "res://art/ui/poster_thumb.png";
    [Export] public string EnglishPosterPath { get; set; } = "res://art/ui/poster_show_en.png";
    [Export] public string RussianPosterPath { get; set; } = "res://art/ui/poster_show_ru.png";
    [Export] public float HoverScale { get; set; } = 1.035f;
    [Export] public float ShadeAlpha { get; set; } = 0.46f;

    private Sprite2D _posterSprite = null!;
    private Area2D _hitArea = null!;
    private CanvasLayer _overlay = null!;
    private Control _overlayRoot = null!;
    private ColorRect _shade = null!;
    private TextureRect _fullPoster = null!;
    private bool _hovered;
    private bool _open;
    private bool _ownsPause;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _posterSprite = GetNode<Sprite2D>("PosterSprite");
        _hitArea = GetNode<Area2D>("HitArea");
        _posterSprite.Centered = false;
        _posterSprite.Texture = GD.Load<Texture2D>(ThumbnailPath);
        _hitArea.InputPickable = true;
        _hitArea.MouseEntered += OnMouseEntered;
        _hitArea.MouseExited += OnMouseExited;
        _hitArea.InputEvent += OnHitAreaInput;
        BuildOverlay();
    }

    private void BuildOverlay()
    {
        _overlay = new CanvasLayer();
        _overlay.Name = "PosterOverlay";
        _overlay.Layer = 160;
        _overlay.ProcessMode = ProcessModeEnum.Always;
        AddChild(_overlay);

        _overlayRoot = new Control();
        _overlayRoot.Name = "Root";
        _overlayRoot.Visible = false;
        _overlayRoot.MouseFilter = Control.MouseFilterEnum.Stop;
        _overlay.AddChild(_overlayRoot);
        _overlayRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _shade = new ColorRect();
        _shade.Name = "Shade";
        _shade.Color = new Color(0, 0, 0, 0);
        _shade.MouseFilter = Control.MouseFilterEnum.Stop;
        _overlayRoot.AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var center = new CenterContainer();
        center.Name = "Center";
        center.MouseFilter = Control.MouseFilterEnum.Ignore;
        _overlayRoot.AddChild(center);
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var posterMargin = new MarginContainer();
        posterMargin.Name = "PosterMargin";
        posterMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
        posterMargin.AddThemeConstantOverride("margin_left", 52);
        posterMargin.AddThemeConstantOverride("margin_top", 36);
        posterMargin.AddThemeConstantOverride("margin_right", 52);
        posterMargin.AddThemeConstantOverride("margin_bottom", 36);
        center.AddChild(posterMargin);

        _fullPoster = new TextureRect();
        _fullPoster.Name = "FullPoster";
        _fullPoster.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _fullPoster.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _fullPoster.MouseFilter = Control.MouseFilterEnum.Ignore;
        _fullPoster.CustomMinimumSize = new Vector2(620, 920);
        _fullPoster.Modulate = new Color(1, 1, 1, 0);
        posterMargin.AddChild(_fullPoster);

        _overlayRoot.GuiInput += OnOverlayGuiInput;
        _shade.GuiInput += OnOverlayGuiInput;
        _overlayRoot.Resized += UpdatePosterFit;
    }

    private void OnMouseEntered()
    {
        if (!_open)
            _hovered = true;
    }

    private void OnMouseExited()
    {
        _hovered = false;
    }

    private void OnHitAreaInput(Node viewport, InputEvent inputEvent, long shapeIndex)
    {
        if (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            OpenPoster();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnOverlayGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            ClosePoster();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!_open)
            return;
        if (inputEvent is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            ClosePoster();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        Vector2 targetScale = _hovered && !_open ? new Vector2(HoverScale, HoverScale) : Vector2.One;
        _posterSprite.Scale = _posterSprite.Scale.Lerp(targetScale, (float)(delta * 10.0));
        Color targetColor = _hovered && !_open ? new Color(1.10f, 1.05f, 0.96f, 1.0f) : Colors.White;
        _posterSprite.SelfModulate = _posterSprite.SelfModulate.Lerp(targetColor, (float)(delta * 12.0));
    }

    private void OpenPoster()
    {
        if (_open)
            return;
        _open = true;
        _hovered = false;
        _fullPoster.Texture = GD.Load<Texture2D>(GameLocalization.Language == "en" ? EnglishPosterPath : RussianPosterPath);
        _overlayRoot.Visible = true;
        UpdatePosterFit();
        _fullPoster.Modulate = new Color(1, 1, 1, 0);
        _shade.Color = new Color(0, 0, 0, 0);
        if (!GetTree().Paused)
        {
            _ownsPause = true;
            GetTree().Paused = true;
        }
        var tween = CreateTween();
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_shade, "color:a", ShadeAlpha, 0.16f);
        tween.Parallel().TweenProperty(_fullPoster, "modulate:a", 1.0f, 0.16f);
    }

    private void ClosePoster()
    {
        if (!_open)
            return;
        _open = false;
        var tween = CreateTween();
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_shade, "color:a", 0.0f, 0.12f);
        tween.Parallel().TweenProperty(_fullPoster, "modulate:a", 0.0f, 0.12f);
        tween.Finished += () =>
        {
            if (!GodotObject.IsInstanceValid(_overlayRoot))
                return;
            _overlayRoot.Visible = false;
            if (_ownsPause)
                GetTree().Paused = false;
            _ownsPause = false;
        };
    }

    private void UpdatePosterFit()
    {
        if (_overlayRoot == null)
            return;
        Vector2 viewportSize = _overlayRoot.Size;
        _fullPoster.CustomMinimumSize = new Vector2(
            Mathf.Max(280, viewportSize.X - 104),
            Mathf.Max(420, viewportSize.Y - 72));
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_hitArea))
        {
            _hitArea.MouseEntered -= OnMouseEntered;
            _hitArea.MouseExited -= OnMouseExited;
            _hitArea.InputEvent -= OnHitAreaInput;
        }
        if (GodotObject.IsInstanceValid(_overlayRoot))
            _overlayRoot.GuiInput -= OnOverlayGuiInput;
        if (GodotObject.IsInstanceValid(_shade))
            _shade.GuiInput -= OnOverlayGuiInput;
        if (_ownsPause && GetTree() != null)
            GetTree().Paused = false;
        _ownsPause = false;
    }
}
