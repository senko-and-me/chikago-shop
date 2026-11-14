using System.Linq;
using Godot;

// Observes gameplay during practice and resets the practice run through the existing NewGame action on exit.
public partial class FirstTradeTutorial : CanvasLayer
{
    public enum Step { Camera, Start, Craft, Select, Place, WaitBuyer, Offer, Summary }
    [Export] public double CameraTravelSeconds { get; set; } = 0.8;
    public Step CurrentStep { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsShowing => _root?.Visible == true;
    public string ProgressPath { get; set; }
    public Control Target { get; private set; }
    public bool CameraPanDone { get; private set; }
    public bool CameraZoomDone { get; private set; }
    public bool IsSkipConfirmationOpen => _confirmation?.Visible == true;
    private TradeScene _game;
    private Control _root;
    private PanelContainer _panel;
    private Panel _highlight;
    private Label _body;
    private Button _done;
    private bool _initialized;
    private int _acceptedAtStart;
    private bool _sawSale;
    private int _lastAccepted;
    private WorldCamera _camera;
    private Label _cameraProgress;
    private Step? _appliedCameraStep;
    private bool _ownsOverview;
    private float _panDistance;
    private Vector2 _viewportSize;
    private Control _confirmation;
    private bool _ownsConfirmationPause;
    private bool _finishing;

    public override void _Ready()
    {
        _game = GetParent<TradeScene>();
        _root = GetNode<Control>("Root");
        _panel = GetNode<PanelContainer>("Root/Panel");
        _highlight = GetNode<Panel>("Root/Highlight");
        _body = GetNode<Label>("Root/Panel/Content/Body");
        _done = GetNode<Button>("Root/Panel/Content/Actions/Done");
        _cameraProgress = GetNode<Label>("Root/Panel/Content/CameraProgress");
        _camera = _game.GetNode<WorldCamera>("Camera2D");
        _camera.UserPanned += OnUserPanned;
        _camera.UserZoomed += OnUserZoomed;
        _camera.OverviewReset += OnOverviewReset;
        _confirmation = GetNode<Control>("Root/SkipConfirmation");
        GetNode<Button>("Root/Panel/Content/Actions/Skip").Pressed += ShowSkipConfirmation;
        GetNode<Button>("Root/SkipConfirmation/Center/Panel/Content/Actions/Confirm").Pressed += ConfirmSkip;
        GetNode<Button>("Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel").Pressed += CancelSkip;
        _game.NewGameStarted += OnNewGameStarted;
        _done.Pressed += Finish;
        Callable.From(Initialize).CallDeferred();
    }

    private void Initialize()
    {
        if (!IsInsideTree()) return;
        ProgressPath ??= string.IsNullOrEmpty(_game.SavePath) ? "" : _game.SavePath + ".tutorial.cfg";
        var config = new ConfigFile();
        bool restored = _game.LoadedSavedGame && !string.IsNullOrEmpty(ProgressPath) && config.Load(ProgressPath) == Error.Ok;
        bool finished = restored && config.GetValue("tutorial", "finished", false).AsBool();
        CameraPanDone = restored && config.GetValue("tutorial", "camera_pan_done", false).AsBool();
        CameraZoomDone = restored && config.GetValue("tutorial", "camera_zoom_done", false).AsBool();
        _initialized = true;
        // Empty SavePath is a transient scene preview/test; explicit Restart still works there.
        if (!finished && !string.IsNullOrEmpty(ProgressPath) && (restored || _game.CurrentDay == 1))
        {
            IsActive = true;
            _acceptedAtStart = restored ? config.GetValue("tutorial", "accepted_at_start", _game.Accepted).AsInt32() : _game.Accepted;
            _lastAccepted = _game.Accepted;
            _sawSale = _game.Accepted > _acceptedAtStart;
            // Establish the run before its first day starts, so Continue can restore camera practice.
            _game.PersistProgress();
            Save(false);
            Refresh();
        }
        var replay = GetNode<Button>("Replay");
        replay.Reparent(_game.GetNode("PauseMenu/Root/Overlay/Center/Panel/Content/Menu"));
        replay.Name = "TutorialButton";
        replay.Show();
        replay.Pressed += () => { _game.GetNode<PauseMenu>("PauseMenu").Close(); Restart(); };
    }

    public void Restart()
    {
        CloseConfirmation();
        StopCamera();
        CameraPanDone = CameraZoomDone = false;
        _panDistance = 0;
        IsActive = true;
        _acceptedAtStart = _game.Accepted;
        _lastAccepted = _game.Accepted;
        _sawSale = false;
        Save(false);
        Refresh();
    }

    private void OnNewGameStarted()
    {
        if (!_finishing && _initialized && (IsActive || !string.IsNullOrEmpty(ProgressPath))) Restart();
    }

    private void ShowSkipConfirmation()
    {
        if (!IsActive || IsSkipConfirmationOpen || GetTree().Paused) return;
        StopCamera();
        _camera.NavigationLocked = true;
        _confirmation.Show();
        _ownsConfirmationPause = true;
        GetTree().Paused = true;
        GetNode<Button>("Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel").GrabFocus();
        Refresh();
    }

    private void CloseConfirmation()
    {
        if (GodotObject.IsInstanceValid(_confirmation)) _confirmation.Hide();
        if (_ownsConfirmationPause) GetTree().Paused = false;
        _ownsConfirmationPause = false;
    }

    private void CancelSkip()
    {
        if (!IsSkipConfirmationOpen) return;
        CloseConfirmation();
        Refresh();
    }

    private void ConfirmSkip()
    {
        if (!IsSkipConfirmationOpen) return;
        CloseConfirmation();
        Finish();
    }

    public override void _Input(InputEvent input)
    {
        if (!IsSkipConfirmationOpen || input is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) return;
        CancelSkip();
        GetViewport().SetInputAsHandled();
    }

    private void Finish()
    {
        if (!IsActive || _finishing) return;
        _finishing = true;
        StopCamera();
        IsActive = false;
        _root.Hide();
        Target = null;
        _camera.NavigationLocked = false;
        // Reuse the game's reset; suppress its tutorial replay notification for this one reset.
        // Preserve the view so the reset ends with a smooth flight rather than a camera jump.
        Vector2 position = _camera.Position, zoom = _camera.Zoom;
        try { _game.NewGame(); }
        finally { _finishing = false; }
        _camera.Position = position;
        _camera.Zoom = zoom;
        Save(true);
        _camera.ReturnToOverview(CameraTravelSeconds);
    }

    private void Save(bool finished)
    {
        if (string.IsNullOrEmpty(ProgressPath)) return;
        var config = new ConfigFile();
        config.SetValue("tutorial", "finished", finished);
        config.SetValue("tutorial", "accepted_at_start", _acceptedAtStart);
        config.SetValue("tutorial", "camera_pan_done", CameraPanDone);
        config.SetValue("tutorial", "camera_zoom_done", CameraZoomDone);
        Error error = config.Save(ProgressPath);
        if (error != Error.Ok) GD.PushWarning($"Tutorial progress could not be saved: {error}");
    }

    public override void _Process(double delta)
    {
        if (!_initialized || !IsActive) return;
        if (_game.Accepted < _lastAccepted) { _acceptedAtStart = _game.Accepted; _sawSale = false; Save(false); }
        _lastAccepted = _game.Accepted;
        _sawSale |= _game.Accepted > _acceptedAtStart;
        Refresh();
    }

    private void Refresh()
    {
        if (IsSkipConfirmationOpen)
        {
            _root.Show();
            _panel.Hide();
            _highlight.Hide();
            return;
        }
        _panel.Show();
        if (GetTree().Paused || _game.CraftMiniGameOpen || _game.DayTransitionOpen || _game.Failed || _game.Completed)
        {
            _root.Hide();
            StopCamera(cancelOverview: !_game.DayTransitionOpen);
            return;
        }
        Vector2 size = GetViewport().GetVisibleRect().Size;
        if (_viewportSize != size) { StopCamera(); _viewportSize = size; }
        _root.Show();
        Target = null;
        if (!CameraPanDone || !CameraZoomDone) SetStep(Step.Camera,
            "Осмотритесь: зажмите ЛКМ на свободном фоне и подвигайте камеру. Затем измените масштаб колесом мыши. Выполните оба действия, чтобы продолжить.", null);
        else if (_sawSale) SetStep(Step.Summary, "Первая продажа готова! Следите за энергией и суммой квоты в панели дня. К сроку на счёте должна быть нужная сумма. Нажмите «Понятно», чтобы начать первый день заново со стартовыми ресурсами.",
            _game.GetNode<Control>("HUD/Root/ProgressPanel"));
        else if (_game.TradeActive) SetStep(Step.Offer,
            "Покупатель предлагает цену. Кнопка «✓» продаёт товар. Кнопка «×» меняет покупателя и тратит попытку: новая цена может стать ниже. Продайте первый предмет.",
            _game.GetNode<Control>("World/TradingRoom/Item/TradePanel"));
        else if (_game.DayClosed) SetStep(Step.Start,
            "Магазин закрыт. Продайте оставшийся товар, если покупатель уже у кассы, затем перейдите к следующему дню через панель слева.",
            _game.GetNode<Control>("HUD/Root/ProgressPanel/Content/EndDayButton"));
        else if (!_game.DayStarted) SetStep(Step.Start,
            "Нажмите «Начать» на часах. Пока день не начат, время не идёт. После запуска на торговлю отведена короткая смена.",
            _game.GetNode<Control>("HUD/Root/DayTimer/StartButton"));
        else if (_game.ShowcaseItems.Any(item => item >= 0)) SetStep(Step.WaitBuyer,
            "Товар уже на витрине. Покупатель подойдёт сам через несколько секунд. Его предложение появится у кассы.",
            _game.GetNode<Control>("World/ShowroomLeft/Showcases/Slot1"));
        else if (_game.StoredItems.Length > 0)
        {
            if (_game.SelectedInventoryIndex < 0) SetStep(Step.Select,
                "Предмет готов. Нажмите на него в нижней панели инвентаря. Наведение покажет его редкость и стоимость.",
                _game.GetNode<InventoryUI>("HUD/InventoryUI").Container.GetChild<Control>(0));
            else SetStep(Step.Place,
                "Нажмите на свободную витрину в левой торговой комнате. Можно также перетащить предмет из инвентаря на витрину.",
                _game.GetNode<Control>("World/ShowroomLeft/Showcases/Slot1"));
        }
        else if (_game.EnergyRemaining <= 0) SetStep(Step.Start,
            "Энергия закончилась. Завершите день в панели слева, затем перейдите к следующему. Непроданные вещи сохраняются.",
            _game.GetNode<Control>("HUD/Root/ProgressPanel/Content/EndDayButton"));
        else SetStep(Step.Craft,
            "Нажмите «Создать предметы» в ателье и выполните мини-игру. Производство стоит денег и энергии; цена указана у кнопки.",
            _game.GetNode<Control>("World/Atelier/GetItemButton"));
        Layout();
    }

    private void SetStep(Step step, string message, Control target)
    {
        CurrentStep = step;
        _camera.NavigationLocked = step != Step.Camera;
        _body.Text = message;
        Target = target;
        _done.Visible = step == Step.Summary;
        _cameraProgress.Visible = step == Step.Camera;
        _cameraProgress.Text = GameLocalization.Format("Перемещение: {0}  •  Масштаб: {1}",
            CameraPanDone ? "✓" : "—", CameraZoomDone ? "✓" : "—");
        if (_appliedCameraStep != step && !_game.DayTransitionOpen)
        {
            GetNode<TutorialCameraCheckpoint>("CameraCheckpoints/" + step).MoveCamera(_game, _camera, CameraTravelSeconds);
            _ownsOverview = GetNode<TutorialCameraCheckpoint>("CameraCheckpoints/" + step).Overview;
            _appliedCameraStep = step;
        }
    }

    private bool Practicing => IsActive && IsShowing && CurrentStep == Step.Camera && !GetTree().Paused;

    private void OnUserPanned(Vector2 displacement)
    {
        if (!Practicing || CameraPanDone) return;
        _panDistance += displacement.Length();
        if (_panDistance < 64) return;
        CameraPanDone = true;
        Save(false);
    }

    private void OnUserZoomed()
    {
        if (!Practicing || CameraZoomDone) return;
        CameraZoomDone = true;
        Save(false);
    }

    private void StopCamera(bool cancelOverview = true)
    {
        _camera.CancelFocusTransition();
        if (_ownsOverview && cancelOverview) _camera.CancelOverviewTransition();
        _ownsOverview = false;
        _appliedCameraStep = null;
    }

    private void OnOverviewReset() => _appliedCameraStep = null;

    public override void _ExitTree()
    {
        CloseConfirmation();
        if (GodotObject.IsInstanceValid(_game)) _game.NewGameStarted -= OnNewGameStarted;
        if (!GodotObject.IsInstanceValid(_camera)) return;
        _camera.NavigationLocked = false;
        _camera.UserPanned -= OnUserPanned;
        _camera.UserZoomed -= OnUserZoomed;
        _camera.OverviewReset -= OnOverviewReset;
        StopCamera();
    }

    private void Layout()
    {
        Rect2 screen = GetViewport().GetVisibleRect();
        float width = Mathf.Min(420, screen.Size.X - 24);
        _panel.Size = new Vector2(width, 0);
        _panel.Position = new Vector2((screen.Size.X - width) / 2, 14);
        _highlight.Hide();
        if (Target == null || !Target.IsVisibleInTree()) return;
        Rect2 target = Target.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, Target.Size);
        Rect2 visible = target.Grow(6).Intersection(screen.Grow(-4));
        if (!visible.HasArea()) return;
        _highlight.Position = visible.Position;
        _highlight.Size = visible.Size;
        _highlight.Show();
        // Keep the hint away from the very control the player is asked to click.
        if (new Rect2(_panel.Position, _panel.Size).Intersects(visible))
            _panel.Position = new Vector2((screen.Size.X - width) / 2,
                Mathf.Clamp(visible.End.Y + 16, 14, Mathf.Max(14, screen.Size.Y - _panel.Size.Y - 14)));
    }
}
