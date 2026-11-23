using System.Linq;
using Godot;

// Observes gameplay during practice and resets the practice run through the existing NewGame action on exit.
public partial class FirstTradeTutorial : CanvasLayer
{
    public enum Step { Camera, Start, Craft, Select, Place, WaitBuyer, Offer, Summary, CameraZoom, RoomFocus, BuyerRequest, MusicBuyerRequest, UpgradeChoice, UpgradeIntroduction, Notebook }
    private const string BuyerRequestMessage = "Каждый покупатель ищет определённый тип товара: одежду или музыкальный инструмент. Сейчас этому покупателю нужна одежда из ателье.";
    private const string MusicRequestMessage = "Такому покупателю нужен инструмент из музыкальной мастерской.";
    private const string UpgradeIntroductionMessage = "Также между днями можно купить одно улучшение или пропустить выбор.";
    private const string NotebookMessage = "Наведитесь на блокнот, чтобы увидеть текущие задания и улучшения.";
    private const float PanGoal = 3000;
    private const int ZoomGoal = 16;
    private static readonly (string Title, string Body)[] SummaryPages =
    [
        ("Дни и квоты", "Наведите мышь на пачку сигарет, чтобы узнать день, сумму и срок квоты."),
        ("Музыкальная мастерская", "Открытие мастерской за деньги даёт новые витрины и больше энергии. Создание инструмента тратит деньги и энергию. Для полировки зажмите ЛКМ и протирайте инструмент туда-сюда."),
        ("Пауза и сохранение", "Esc ставит игру на паузу. В меню можно изменить настройки, вернуться к чекпоинту или повторить обучение. Прогресс сохраняется автоматически. После обучения первый день начнётся заново.")
    ];
    [Export] public double CameraTravelSeconds { get; set; } = 0.8;
    public Step CurrentStep { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsShowing => _root?.Visible == true;
    public string ProgressPath { get; set; }
    public Control Target { get; private set; }
    public bool CameraPanDone { get; private set; }
    public bool CameraZoomDone { get; private set; }
    public bool RoomFocusDone { get; private set; }
    public float CameraPanProgress => CameraPanDone ? 100 : _panDistance / PanGoal * 100;
    public float CameraZoomProgress => CameraZoomDone ? 100 : _zoomSteps * 100f / ZoomGoal;
    public bool IsCameraCompletionPending => IsActive && _cameraZoomStarted && CameraPanDone && CameraZoomDone && RoomFocusDone && !_cameraHandoffDone;
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
    private bool _buyerRequestRead;
    private bool _showMusicRequestExample;
    private bool _offerInstructionRead;
    private bool _upgradeLessonPending;
    private bool _upgradeIntroductionShown;
    private double _packHoverSeconds;
    private bool _notebookLessonDone;
    private double _notebookHoverSeconds;
    public bool BlocksFirstOffer => IsActive && _cameraHandoffDone && !_sawSale &&
        (!_buyerRequestRead || !_offerInstructionRead);
    public bool DefersBuyerArrival => IsActive && !_game.TradeActive &&
        (_sawSale || _game.Accepted > _acceptedAtStart ||
            !_buyerRequestRead && (!_cameraHandoffDone || !_game.ShowcaseItems.Any(item => item >= 0)));
    private int _lastAccepted;
    private WorldCamera _camera;
    private ProgressBar _cameraProgress;
    private Label _title;
    private int _zoomSteps;
    private bool _cameraZoomStarted;
    private double _panCompletionElapsed;
    private bool _progressDirty;
    private double _progressSaveElapsed;
    private Step? _appliedCameraStep;
    private bool _ownsOverview;
    private float _panDistance;
    private Vector2 _viewportSize;
    private Control _confirmation;
    private bool _ownsConfirmationPause;
    private bool _finishing;
    private bool _cameraHandoffDone;
    private int _summaryPage;

    public override void _Ready()
    {
        _game = GetParent<TradeScene>();
        _root = GetNode<Control>("Root");
        _panel = GetNode<PanelContainer>("Root/Panel");
        _highlight = GetNode<Panel>("Root/Highlight");
        _body = GetNode<Label>("Root/Panel/Content/Body");
        _done = GetNode<Button>("Root/Panel/Content/Actions/Done");
        _cameraProgress = GetNode<ProgressBar>("Root/Panel/Content/CameraProgress");
        _title = GetNode<Label>("Root/Panel/Content/Title");
        _camera = _game.GetNode<WorldCamera>("Camera2D");
        _camera.UserPanned += OnUserPanned;
        _camera.UserZoomed += OnUserZoomed;
        _camera.UserFocusedRoom += OnUserFocusedRoom;
        _camera.OverviewReset += OnOverviewReset;
        _confirmation = GetNode<Control>("Root/SkipConfirmation");
        GetNode<Button>("Root/Skip").Pressed += ShowSkipConfirmation;
        GetNode<Button>("Root/SkipConfirmation/Center/Panel/Content/Actions/Confirm").Pressed += ConfirmSkip;
        GetNode<Button>("Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel").Pressed += CancelSkip;
        _game.NewGameStarted += OnNewGameStarted;
        _done.Pressed += AdvanceInstruction;
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
        float storedPan = restored ? config.GetValue("tutorial", "camera_pan_distance", 0f).AsSingle() : 0;
        _panDistance = CameraPanDone ? PanGoal : float.IsFinite(storedPan) ? Mathf.Clamp(storedPan, 0, PanGoal) : 0;
        _zoomSteps = CameraZoomDone ? ZoomGoal : restored ? Mathf.Clamp(config.GetValue("tutorial", "camera_zoom_steps", 0).AsInt32(), 0, ZoomGoal) : 0;
        CameraPanDone |= _panDistance >= PanGoal;
        CameraZoomDone |= _zoomSteps >= ZoomGoal;
        _cameraZoomStarted = CameraPanDone && config.GetValue("tutorial", "camera_zoom_started", true).AsBool();
        // Older saves with both actions done have already moved on from camera practice.
        _cameraHandoffDone = CameraPanDone && CameraZoomDone &&
            config.GetValue("tutorial", "camera_handoff_done", true).AsBool();
        RoomFocusDone = restored && config.GetValue("tutorial", "room_focus_done", _cameraHandoffDone).AsBool();
        _cameraHandoffDone &= RoomFocusDone;
        _initialized = true;
        // Empty SavePath is a transient scene preview/test; explicit Restart still works there.
        if (!finished && !string.IsNullOrEmpty(ProgressPath) && (restored || _game.CurrentDay == 1))
        {
            IsActive = true;
            _acceptedAtStart = restored ? config.GetValue("tutorial", "accepted_at_start", _game.Accepted).AsInt32() : _game.Accepted;
            _lastAccepted = _game.Accepted;
            _sawSale = _game.Accepted > _acceptedAtStart;
            _buyerRequestRead = restored && config.GetValue("tutorial", "buyer_request_read", false).AsBool();
            _showMusicRequestExample = restored && !_buyerRequestRead && config.GetValue("tutorial", "music_request_example", false).AsBool();
            _offerInstructionRead = restored && _buyerRequestRead && config.GetValue("tutorial", "offer_instruction_read", false).AsBool();
            bool resetTrainingClock = !restored || config.GetValue("tutorial", "training_clock_duration", 0).AsDouble() != 180;
            _game.SynchronizeTutorialClock(restart: resetTrainingClock);
            _game.RefreshTutorialTradeActions();
            if (_sawSale && restored)
                _summaryPage = Mathf.Clamp(config.GetValue("tutorial", "summary_page", 0).AsInt32(), 0, SummaryPages.Length - 1);
            _upgradeLessonPending = restored && _sawSale && _summaryPage == 0 &&
                config.GetValue("tutorial", "upgrade_lesson_pending", false).AsBool();
            _upgradeIntroductionShown = restored && _sawSale && _summaryPage == 0 &&
                config.GetValue("tutorial", "upgrade_introduction_shown", false).AsBool();
            _notebookLessonDone = restored && config.GetValue("tutorial", "notebook_lesson_done", false).AsBool();
            // Establish the run before its first day starts, so Continue can restore camera practice.
            _game.PersistProgress();
            Save(false);
            Refresh();
        }
    }

    public void Restart()
    {
        CloseConfirmation();
        StopCamera();
        CameraPanDone = CameraZoomDone = false;
        RoomFocusDone = false;
        _cameraHandoffDone = false;
        _panDistance = 0;
        _zoomSteps = 0;
        _cameraZoomStarted = false;
        _panCompletionElapsed = 0;
        IsActive = true;
        _acceptedAtStart = _game.Accepted;
        _lastAccepted = _game.Accepted;
        _sawSale = false;
        _buyerRequestRead = false;
        _showMusicRequestExample = false;
        _offerInstructionRead = false;
        RefreshRequestExample();
        _summaryPage = 0;
        _upgradeLessonPending = false;
        _upgradeIntroductionShown = false;
        _packHoverSeconds = 0;
        _notebookLessonDone = false;
        _notebookHoverSeconds = 0;
        _game.SynchronizeTutorialClock(restart: true);
        _game.PersistProgress();
        _game.RefreshTutorialTradeActions();
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

    private void AdvanceInstruction()
    {
        if (!IsActive || !IsShowing || GetTree().Paused) return;
        if (CurrentStep == Step.UpgradeIntroduction)
        {
            _upgradeLessonPending = true;
            Save(false);
            OpenUpgradeLesson();
            return;
        }
        if (CurrentStep == Step.Offer && !_offerInstructionRead)
        {
            _offerInstructionRead = true;
            _game.RefreshTutorialTradeActions();
            Save(false);
            Refresh();
            return;
        }
        if (CurrentStep is Step.BuyerRequest or Step.MusicBuyerRequest)
        {
            _buyerRequestRead = _showMusicRequestExample;
            _showMusicRequestExample = !_buyerRequestRead;
            _game.RefreshTutorialTradeActions();
            Save(false);
            Refresh();
            return;
        }
        if (CurrentStep != Step.Summary || !_sawSale || _summaryPage == 0) return;
        if (_summaryPage == SummaryPages.Length - 1)
        {
            Finish();
            return;
        }

        _summaryPage++;
        Save(false);
        Refresh();
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
        _showMusicRequestExample = false;
        RefreshRequestExample();
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
        config.SetValue("tutorial", "camera_handoff_done", _cameraHandoffDone);
        config.SetValue("tutorial", "room_focus_done", RoomFocusDone);
        config.SetValue("tutorial", "camera_pan_distance", _panDistance);
        config.SetValue("tutorial", "camera_zoom_steps", _zoomSteps);
        config.SetValue("tutorial", "camera_zoom_started", _cameraZoomStarted);
        config.SetValue("tutorial", "summary_page", _summaryPage);
        config.SetValue("tutorial", "upgrade_lesson_pending", _upgradeLessonPending);
        config.SetValue("tutorial", "upgrade_introduction_shown", _upgradeIntroductionShown);
        config.SetValue("tutorial", "notebook_lesson_done", _notebookLessonDone);
        config.SetValue("tutorial", "buyer_request_read", _buyerRequestRead);
        config.SetValue("tutorial", "music_request_example", _showMusicRequestExample);
        config.SetValue("tutorial", "offer_instruction_read", _offerInstructionRead);
        config.SetValue("tutorial", "training_clock_duration", 180);
        Error error = config.Save(ProgressPath);
        if (error != Error.Ok) GD.PushWarning($"Tutorial progress could not be saved: {error}");
        _progressDirty = false;
        _progressSaveElapsed = 0;
    }

    public override void _Process(double delta)
    {
        if (!_initialized || !IsActive) return;
        if (_progressDirty && (_progressSaveElapsed += delta) >= 0.25) Save(false);
        if (CameraPanDone && !_cameraZoomStarted && IsShowing && !GetTree().Paused && !IsSkipConfirmationOpen &&
            !_game.CraftMiniGameOpen && !_game.DayTransitionOpen && !_game.Failed && !_game.Completed &&
            !_camera.IsDragging && !_camera.IsFocusing && !_camera.IsReturningToOverview)
        {
            _panCompletionElapsed += delta;
            if (_panCompletionElapsed >= 0.6)
            {
                _cameraZoomStarted = true;
                Save(false);
            }
        }
        if (IsCameraCompletionPending && !GetTree().Paused && !IsSkipConfirmationOpen &&
            !_game.CraftMiniGameOpen && !_game.DayTransitionOpen && !_game.Failed && !_game.Completed &&
            !_camera.IsDragging && !_camera.IsFocusing && !_camera.IsReturningToOverview)
        {
            _cameraHandoffDone = true;
            Save(false);
        }
        if (_game.Accepted < _lastAccepted)
        {
            _acceptedAtStart = _game.Accepted;
            _sawSale = false;
            _summaryPage = 0;
            _upgradeLessonPending = false;
            _upgradeIntroductionShown = false;
            _packHoverSeconds = 0;
            _notebookLessonDone = false;
            _notebookHoverSeconds = 0;
            Save(false);
        }
        _lastAccepted = _game.Accepted;
        _sawSale |= _game.Accepted > _acceptedAtStart;
        Refresh();
        if (_upgradeLessonPending)
        {
            OpenUpgradeLesson();
            return;
        }
        if (CurrentStep == Step.Notebook && IsShowing && !GetTree().Paused)
        {
            var notebook = _game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
            var hovered = GetViewport().GuiGetHoveredControl();
            if (notebook.Expanded && hovered != null && notebook.IsAncestorOf(hovered))
            {
                _notebookHoverSeconds += delta;
                if (_notebookHoverSeconds >= 1)
                {
                    _notebookLessonDone = true;
                    Save(false);
                    Refresh();
                }
            }
            else _notebookHoverSeconds = 0;
        }
        else _notebookHoverSeconds = 0;
        if (CurrentStep == Step.Summary && _summaryPage == 0 && IsShowing && !GetTree().Paused)
        {
            var pack = _game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            if (pack.HoveringHotspot && pack.Expanded)
            {
                _packHoverSeconds += delta;
                if (_packHoverSeconds >= 1)
                {
                    _upgradeIntroductionShown = true;
                    Save(false);
                    Refresh();
                }
            }
            else _packHoverSeconds = 0;
        }
        else _packHoverSeconds = 0;
    }

    private void OpenUpgradeLesson()
    {
        if (GetTree().Paused || _game.DayTransitionOpen) return;
        if (_game.OpenTutorialUpgradeChoice(CompleteUpgradeLesson))
        {
            CurrentStep = Step.UpgradeChoice;
            Refresh();
        }
    }

    private void CompleteUpgradeLesson()
    {
        if (!IsActive) return;
        _upgradeLessonPending = false;
        _upgradeIntroductionShown = false;
        _packHoverSeconds = 0;
        _summaryPage = 1;
        _notebookLessonDone = false;
        _notebookHoverSeconds = 0;
        Save(false);
        _game.PersistProgress();
        Refresh();
    }

    private void RefreshRequestExample() =>
        _game.GetNodeOrNull<BuyerWaitWidget>("World/TradingRoom/BuyerWaitingThought")?.SetTutorialCategoryPreview(
            IsActive && _game.TradeActive && _showMusicRequestExample && !_buyerRequestRead ? true : null);

    private void Refresh()
    {
        RefreshRequestExample();
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
        if (!_cameraHandoffDone)
        {
            if (!_cameraZoomStarted) SetStep(Step.Camera,
                CameraPanDone ? "Готово. Дальше — масштаб."
                    : "Зажмите ЛКМ на фоне и двигайте мышь до 100%.", null);
            else if (!CameraZoomDone)
                SetStep(Step.CameraZoom, "Крутите колесо в обе стороны до 100%.", null);
            else SetStep(Step.RoomFocus,
                "Дважды нажмите ЛКМ по комнате, чтобы приблизить её и расположить по центру экрана.", null);
        }
        else if (_sawSale && _summaryPage == 0 && _upgradeIntroductionShown)
            SetStep(Step.UpgradeIntroduction, UpgradeIntroductionMessage, null);
        else if (_sawSale && _summaryPage == 1 && !_notebookLessonDone)
            SetStep(Step.Notebook, NotebookMessage, _game.GetNode<Control>("HUD/Root/PulloutNotebook/Page"));
        else if (_sawSale) SetStep(Step.Summary, SummaryPages[_summaryPage].Body,
            _summaryPage == 0 ? _game.GetNode<Control>("HUD/Root/ProgressToggleButton") : null);
        else if (_game.TradeActive && !_buyerRequestRead) SetStep(
            _showMusicRequestExample ? Step.MusicBuyerRequest : Step.BuyerRequest,
            _showMusicRequestExample ? MusicRequestMessage : BuyerRequestMessage,
            _game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble"));
        else if (_game.TradeActive) SetStep(Step.Offer,
            "Нажмите «✓», чтобы принять предложение, или «×», чтобы поменять покупателя. В скобках указано количество возможных смен покупателя. Если время закончится, вам придётся принять предложение покупателя по заниженной стоимости.",
            _game.GetNode<Control>("World/TradingRoom/Item/TradePanel"));
        else if (_game.DayClosed) SetStep(Step.Start,
            "Время вышло. В паузе можно начать с чекпоинта и попробовать снова или пропустить обучение.", null);
        else if (!_game.DayStarted) SetStep(Step.Start,
            "Нажмите «Начать» на часах, чтобы открыть магазин. Часы показывают, сколько времени осталось до закрытия. После начала дня можно нажать на часы, чтобы завершить день досрочно.",
            _game.GetNode<Control>("HUD/DayClock/Root/DayTimer/StartButton"));
        else if (_game.ShowcaseItems.Any(item => item >= 0)) SetStep(Step.WaitBuyer,
            "Покупатель приходит сразу после открытия магазина или продажи. Когда он появляется, красная полоска отсчитывает 20 секунд для надбавки за скорость.",
            _game.GetNode<Control>("World/ShowroomLeft/Showcases/Slot1"));
        else if (_game.StoredItems.Length > 0)
        {
            if (_game.SelectedInventoryIndex < 0) SetStep(Step.Select,
                "Наведите мышь на предмет в инвентаре внизу, чтобы посмотреть его цену, редкость и качество. Затем нажмите на предмет, чтобы выбрать его.",
                _game.GetNode<InventoryUI>("HUD/InventoryUI").Container.GetChild<Control>(0));
            else SetStep(Step.Place,
                "Нажмите на свободную витрину слева или перетащите туда предмет.",
                _game.GetNode<Control>("World/ShowroomLeft/Showcases/Slot1"));
        }
        else if (_game.EnergyRemaining <= 0) SetStep(Step.Start,
            "Энергия закончилась. Она восстанавливается на следующий день. В паузе можно начать с чекпоинта и попробовать снова.", null);
        else SetStep(Step.Craft,
            "Нажмите «Создать предметы» в ателье. Создание предмета тратит деньги и одну энергию за раз. Значок «?» показывает шансы редкости.",
            _game.GetNode<Control>("World/Atelier/GetItemButton"));
        Layout();
    }

    private void SetStep(Step step, string message, Control target)
    {
        bool cameraStep = step is Step.Camera or Step.CameraZoom or Step.RoomFocus;
        bool keepChosenRoom = step == Step.Start && _cameraHandoffDone && !_game.DayClosed;
        bool continuingPractice = _appliedCameraStep == Step.Camera && cameraStep;
        CurrentStep = step;
        _camera.NavigationLocked = !cameraStep && !keepChosenRoom;
        _body.Text = message;
        Target = target;
        _panel.Visible = step != Step.Offer || !_offerInstructionRead;
        _done.Visible = step == Step.Summary && _summaryPage > 0 || step is Step.BuyerRequest or Step.MusicBuyerRequest or Step.UpgradeIntroduction ||
            step == Step.Offer && !_offerInstructionRead;
        _done.GetParent<Control>().Visible = _done.Visible;
        _done.Text = step == Step.UpgradeIntroduction ? "Открыть окно улучшений" :
            step is Step.BuyerRequest or Step.MusicBuyerRequest or Step.Offer || _summaryPage < SummaryPages.Length - 1 ? "Далее" : "Понятно";
        _title.Text = step switch
        {
            Step.Camera => "Передвижение камеры",
            Step.CameraZoom => "Масштабирование",
            Step.RoomFocus => "Приближение комнаты",
            Step.BuyerRequest => "Запрос покупателя",
            Step.MusicBuyerRequest => "Запрос инструмента",
            Step.UpgradeIntroduction => "Улучшения между днями",
            Step.Notebook => "Блокнот",
            Step.Summary => SummaryPages[_summaryPage].Title,
            _ => "Первая продажа"
        };
        _cameraProgress.Visible = cameraStep && step != Step.RoomFocus;
        _cameraProgress.Value = step == Step.Camera ? CameraPanProgress : CameraZoomProgress;
        Step cameraCheckpoint = cameraStep ? Step.Camera : step is Step.BuyerRequest or Step.MusicBuyerRequest ? Step.Offer :
            step is Step.UpgradeIntroduction or Step.Notebook ? Step.Summary : step;
        if (_appliedCameraStep != cameraCheckpoint && !_game.DayTransitionOpen)
        {
            // Restoring completed practice must not replay its entry flight before handing off.
            if (!IsCameraCompletionPending && !continuingPractice && !keepChosenRoom)
            {
                string checkpoint = "CameraCheckpoints/" + cameraCheckpoint;
                GetNode<TutorialCameraCheckpoint>(checkpoint).MoveCamera(_game, _camera, CameraTravelSeconds);
                _ownsOverview = GetNode<TutorialCameraCheckpoint>(checkpoint).Overview;
            }
            _appliedCameraStep = cameraCheckpoint;
        }
    }

    private bool Practicing => IsActive && IsShowing && (CurrentStep is Step.Camera or Step.CameraZoom or Step.RoomFocus) && !GetTree().Paused;

    private void OnUserFocusedRoom()
    {
        if (!Practicing || CurrentStep != Step.RoomFocus || RoomFocusDone) return;
        RoomFocusDone = true;
        Save(false);
    }

    private void OnUserPanned(Vector2 displacement)
    {
        if (!Practicing) return;
        if (CameraPanDone) return;
        _panDistance = Mathf.Min(PanGoal, _panDistance + displacement.Length());
        _progressDirty = true;
        if (_panDistance < PanGoal) return;
        CameraPanDone = true;
        Save(false);
    }

    private void OnUserZoomed()
    {
        if (!Practicing) return;
        if (CurrentStep != Step.CameraZoom || CameraZoomDone) return;
        _zoomSteps++;
        _progressDirty = true;
        if (_zoomSteps < ZoomGoal) return;
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
        if (IsActive && _progressDirty) Save(false);
        CloseConfirmation();
        if (GodotObject.IsInstanceValid(_game)) _game.NewGameStarted -= OnNewGameStarted;
        if (!GodotObject.IsInstanceValid(_camera)) return;
        _camera.NavigationLocked = false;
        _camera.UserPanned -= OnUserPanned;
        _camera.UserZoomed -= OnUserZoomed;
        _camera.UserFocusedRoom -= OnUserFocusedRoom;
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
