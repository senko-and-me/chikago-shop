using System.Collections.Generic;
using Godot;

// Guards player actions while the clock is waiting for Start; navigation remains available.
public partial class DayStartGate : CanvasLayer
{
    private const string Message = "Сначала начните день — нажмите «Начать» на часах.";
    private readonly List<Control> _targets = new();
    private TradeScene _game;
    private PanelContainer _notice;
    private Label _message;
    private double _remaining;

    public override void _Ready()
    {
        _game = GetParent<TradeScene>();
        Layer = 15;
        foreach (string path in new[] {
            "World/Atelier/GetItemButton", "World/MusicWorkshop/GetItemButton",
            "World/TradingRoom/Item/TradePanel" })
            _targets.Add(_game.GetNode<Control>(path));
        foreach (string room in new[] { "ShowroomLeft", "Showroom" })
            foreach (Node slot in _game.GetNode($"World/{room}/Showcases").GetChildren())
                if (slot is Control target) _targets.Add(target);

        var root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _notice = new PanelContainer { Name = "Notice", Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(_notice);
        _notice.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = Colors.White, BorderColor = Colors.Black,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 12, ContentMarginBottom = 12
        });
        _message = new Label { Name = "Message", AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled };
        _message.AddThemeColorOverride("font_color", Colors.Black);
        _message.AddThemeConstantOverride("outline_size", 0);
        _message.AddThemeFontSizeOverride("font_size", 20);
        _notice.AddChild(_message);
    }

    public bool AllowAction()
    {
        if (_game.DayStarted) return true;
        if (!_game.Failed && !_game.Completed && !_game.GetTree().Paused && !_game.DayTransitionOpen)
        {
            _remaining = 3.5;
            RefreshNotice();
            _notice.Show();
        }
        return false;
    }

    public void Dismiss()
    {
        _remaining = 0;
        _notice.Hide();
    }

    private void RefreshNotice()
    {
        float width = Mathf.Min(620, GetViewport().GetVisibleRect().Size.X - 24);
        _notice.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _notice.OffsetLeft = -width / 2;
        _notice.OffsetRight = width / 2;
        _notice.OffsetTop = -210;
        _notice.OffsetBottom = -130;
        _message.Text = GameLocalization.Text(Message);
    }

    public override void _Process(double delta)
    {
        if (!_notice.Visible) return;
        _remaining -= delta;
        if (_remaining <= 0 || _game.DayStarted || _game.Failed || _game.Completed || _game.DayTransitionOpen)
            Dismiss();
        else RefreshNotice();
    }

    public override void _Input(InputEvent input)
    {
        if (_game.DayStarted || GetTree().Paused || _game.DayTransitionOpen) return;
        bool blocked = false;
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click)
        {
            Control hovered = GetViewport().GuiGetHoveredControl();
            foreach (var target in _targets)
                if ((hovered == null || hovered == target || target.IsAncestorOf(hovered)) &&
                    target.IsVisibleInTree() && new Rect2(Vector2.Zero, target.Size).HasPoint(
                    target.GetGlobalTransformWithCanvas().AffineInverse() * click.Position))
                { blocked = true; break; }
        }
        else if (input is InputEventKey { Pressed: true, Echo: false } key)
        {
            var focused = GetViewport().GuiGetFocusOwner();
            blocked = key.Keycode == Key.F || input.IsActionPressed("ui_accept") &&
                focused != null && _targets.Exists(target => target == focused || target.IsAncestorOf(focused));
        }
        if (!blocked) return;
        AllowAction();
        GetViewport().SetInputAsHandled();
    }
}
