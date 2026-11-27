using System.Linq;
using Godot;

// Observes gameplay during practice and resets the practice run through the existing NewGame action on exit.
public partial class FirstTradeTutorial : CanvasLayer
{
    private bool _displayInspected;
    internal void OnShowcaseInspected(int slot)
    {
        if (!IsActive || CurrentStep != Step.Select || Target != _game.DisplayControl(slot)) return;
        _displayInspected = true;
        Save(false);
        Refresh();
    }

    internal bool CanSelectDisplay(int slot) => !IsActive || CurrentStep == Step.Place && Target == _game.DisplayControl(slot);

    public enum Step { Camera, Start, Craft, Select, Place, WaitBuyer, Offer, Summary, CameraZoom, RoomFocus, BuyerRequest, UpgradeChoice, Notebook, NotebookContents, EmbroideryStart, TimedSale, LateSale, MusicWorkshop, CardExplanation, CraftSelection }
    private const string BuyerRequestMessage = "Облачко показывает, что покупатель ищет одежду из ателье.";
    private const string NotebookMessage = "Наведите курсор на блокнот справа, чтобы открыть его.";
    private const float PanGoal = 3000;
    private const int ZoomGoal = 16;
    private static readonly (string Title, string Body)[] SummaryPages =
    [
        ("Дни и квоты", "Наведите мышь на пачку сигарет, чтобы узнать день, сумму и срок квоты."),
        ("Пауза и сохранение", "Esc открывает паузу: там есть настройки, чекпоинт и повтор обучения. Игра сохраняется автоматически. После обучения первый день начнётся заново.")
    ];
    [Export] public double CameraTravelSeconds { get; set; } = 0.8;
    public Step CurrentStep { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsShowing => _root?.Visible == true;
    public string ProgressPath { get; set; }
    public bool SkipOnInitialize { get; set; }
    public bool ProloguePending { get; set; }
    public event System.Action Finished;
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
    private TutorialSpotlight _spotlight;
    private EmbroideryGame _embroidery;
    private PauseMenu _pause;
    private int _regularLayer;
    private Label _body;
    private Button _done;
    private Button _skip;
    private Control _salePanel;
    private Control _actions;
    private bool _musicLessonPending, _musicLessonDone, _cardLessonDone;
    private int _musicPage, _cardPage;
    private DayTransitionCard _card;
    private ProgressPanel _pack;
    private bool _initialized;
    private int _acceptedAtStart;
    private bool _sawSale;
    private bool _buyerRequestRead;
    private bool _embroideryStarted;
    private bool _offerInstructionRead;
    private bool _upgradeLessonPending;
    private int _upgradeLessonDay;
    private int _offerPage;
    private PulloutNotebook _notebook;
    private double _packHoverSeconds;
    private bool _notebookLessonDone;
    public bool BlocksFirstOffer => IsActive && _cameraHandoffDone && !_sawSale &&
        (!_buyerRequestRead || !_offerInstructionRead);
    public bool DefersBuyerArrival => IsActive && !_game.TradeActive &&
        (_sawSale || _game.Accepted >= _acceptedAtStart + 2 ||
            !_buyerRequestRead && (!_cameraHandoffDone || !_game.ShowcaseItems.Any(item => item >= 0)));
    internal bool HoldTrainingBuyer => IsActive && !_sawSale && _game.Accepted == _acceptedAtStart;
    internal bool BlocksSaleActions => BlocksFirstOffer ||
        (IsActive && !_sawSale && _game.Accepted == _acceptedAtStart + 1 && _game.BuyerWaitRemaining > 0);
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
        _regularLayer = Layer;
        _spotlight = new TutorialSpotlight { Name = "Spotlight" };
        AddChild(_spotlight);
        MoveChild(_spotlight, _root.GetIndex());
        _panel = GetNode<PanelContainer>("Root/Panel");
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
        _actions = _done.GetParent<Control>();
        _skip = _root.GetNode<Button>("Skip");
        _skip.Pressed += ShowSkipConfirmation;
        _spotlight.SetAccessories(_panel, _skip, null);
        GetNode<Button>("Root/SkipConfirmation/Center/Panel/Content/Actions/Confirm").Pressed += ConfirmSkip;
        GetNode<Button>("Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel").Pressed += CancelSkip;
        _game.NewGameStarted += OnNewGameStarted;
        _done.Pressed += AdvanceInstruction;
        Callable.From(Initialize).CallDeferred();
    }

    internal bool HasPendingUpgradeLesson()
    {
        if (_initialized) return _upgradeLessonPending;
        if (!_game.LoadedSavedGame) return false;
        string path = ProgressPath ?? (_game.SavePath + ".tutorial.cfg");
        var config = new ConfigFile();
        return !string.IsNullOrEmpty(path) && config.Load(path) == Error.Ok &&
            !config.GetValue("tutorial", "finished", false).AsBool() &&
            (config.GetValue("tutorial", "upgrade_lesson_pending", false).AsBool() ||
                config.GetValue("tutorial", "upgrade_introduction_shown", false).AsBool());
    }

    private void Initialize()
    {
        if (!IsInsideTree()) return;
        _embroidery = _game.GetNode<EmbroideryGame>("EmbroideryGame");
        _embroidery.Started += OnEmbroideryStarted;
        _pause = _game.GetNode<PauseMenu>("PauseMenu");
        _card = _game.GetNode<DayTransitionCard>("DayTransitionCard");
        _pack = _game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
        _notebook = _game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
        _salePanel = _game.GetNode<Control>("World/TradingRoom/Item/TradePanel");
        ProgressPath ??= string.IsNullOrEmpty(_game.SavePath) ? "" : _game.SavePath + ".tutorial.cfg";
        var config = new ConfigFile();
        bool restored = _game.LoadedSavedGame && !string.IsNullOrEmpty(ProgressPath) && config.Load(ProgressPath) == Error.Ok;
        _musicLessonPending = restored && config.GetValue("tutorial", "music_lesson_pending", false).AsBool();
        _musicLessonDone = restored && config.GetValue("tutorial", "music_lesson_done", false).AsBool();
        _cardLessonDone = restored && config.GetValue("tutorial", "card_lesson_done", false).AsBool();
        bool finished = SkipOnInitialize || restored && config.GetValue("tutorial", "finished", false).AsBool();
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
        _displayInspected = restored && config.GetValue("tutorial", "display_inspected", false).AsBool();
        // Also covers older saves migrated from paid to story-based room access.
        _musicLessonPending |= _game.MusicWorkshopUnlocked && !_musicLessonDone;
        if (SkipOnInitialize)
        {
            Save(true);
            _game.PersistProgress();
        }
        // Empty SavePath is a transient scene preview/test; explicit Restart still works there.
        if (!finished && !string.IsNullOrEmpty(ProgressPath) && (restored || _game.CurrentDay == 1))
        {
            IsActive = true;
            _acceptedAtStart = restored ? config.GetValue("tutorial", "accepted_at_start", _game.Accepted).AsInt32() : _game.Accepted;
            _lastAccepted = _game.Accepted;
            _sawSale = _game.Accepted >= _acceptedAtStart + 2 ||
                (restored && _game.Accepted > _acceptedAtStart &&
                    !config.GetValue("tutorial", "two_sales", false).AsBool());
            _buyerRequestRead = restored && config.GetValue("tutorial", "buyer_request_read", false).AsBool();
            _embroideryStarted = restored && config.GetValue("tutorial", "embroidery_started", false).AsBool();
            _offerInstructionRead = restored && _buyerRequestRead && config.GetValue("tutorial", "offer_instruction_read", false).AsBool();
            bool resetTrainingClock = !restored || config.GetValue("tutorial", "training_clock_duration", 0).AsDouble() != DayCycleController.TutorialDuration;
            _game.SynchronizeTutorialClock(restart: resetTrainingClock);
            _game.RefreshTutorialTradeActions();
            if (_sawSale && restored)
                _summaryPage = Mathf.Clamp(config.GetValue("tutorial", "summary_page", 0).AsInt32(), 0, SummaryPages.Length - 1);
            _upgradeLessonPending = restored && _sawSale && _summaryPage == 0 &&
                config.GetValue("tutorial", "upgrade_lesson_pending", false).AsBool();
            _upgradeLessonPending |= restored && _sawSale && _summaryPage == 0 &&
                config.GetValue("tutorial", "upgrade_introduction_shown", false).AsBool();
            _upgradeLessonDay = restored ? config.GetValue("tutorial", "upgrade_lesson_day", _game.CurrentDay).AsInt32() : 0;
            if (_upgradeLessonPending && _game.CurrentDay > _upgradeLessonDay)
            {
                _upgradeLessonPending = false;
                _summaryPage = SummaryPages.Length - 1;
            }
            _offerPage = restored ? Mathf.Clamp(config.GetValue("tutorial", "offer_page", 0).AsInt32(), 0, 2) : 0;
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
        _embroideryStarted = false;
        _offerInstructionRead = false;
        _offerPage = 0;
        _cardPage = 0;
        _cardLessonDone = false;
        _summaryPage = 0;
        _upgradeLessonPending = false;
        _upgradeLessonDay = 0;
        _packHoverSeconds = 0;
        _notebookLessonDone = false;
        _game.SynchronizeTutorialClock(restart: true);
        _game.PersistProgress();
        _game.RefreshTutorialTradeActions();
        Save(false);
        Refresh();
    }

    private void OnNewGameStarted()
    {
        if (_finishing || !_initialized) return;
        _musicLessonPending = _musicLessonDone = _cardLessonDone = false;
        _displayInspected = false;
        _musicPage = _cardPage = 0;
        if (IsActive || !string.IsNullOrEmpty(ProgressPath)) Restart();
    }

    private void ShowSkipConfirmation()
    {
        if ((!IsActive && CurrentStep != Step.MusicWorkshop) || IsSkipConfirmationOpen ||
            (GetTree().Paused && CurrentStep != Step.EmbroideryStart)) return;
        bool music = CurrentStep == Step.MusicWorkshop;
        _confirmation.GetNode<Label>("Center/Panel/Content/Message").Text = music
            ? "Подсказки мастерской будут закрыты. Игра продолжится с текущего места."
            : "Первый день начнётся заново со стартовыми деньгами. Обучение можно повторить в паузе.";
        StopCamera();
        _camera.NavigationLocked = true;
        _confirmation.Show();
        _ownsConfirmationPause = !GetTree().Paused;
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
        if (CurrentStep != Step.MusicWorkshop || !TrySupplementalLesson()) Refresh();
    }

    private void ConfirmSkip()
    {
        if (!IsSkipConfirmationOpen) return;
        CloseConfirmation();
        if (CurrentStep == Step.MusicWorkshop)
        {
            CompleteMusicLesson();
            return;
        }
        if (_embroidery.IsReadyToStart) _embroidery.Close();
        Finish();
    }

    private void AdvanceInstruction()
    {
        if (!IsShowing || IsSkipConfirmationOpen) return;
        if (CurrentStep == Step.CardExplanation && !_cardLessonDone)
        {
            if (++_cardPage >= 2)
            {
                _cardLessonDone = true;
                _card.SetInstructionLocked(false);
                Save(!IsActive);
                _root.Hide();
                _spotlight.SetTarget(null);
            }
            return;
        }
        if (CurrentStep == Step.MusicWorkshop)
        {
            if (++_musicPage >= 4) CompleteMusicLesson();
            return;
        }
        if (!IsActive || GetTree().Paused) return;
        if (CurrentStep == Step.Offer && !_offerInstructionRead)
        {
            _offerInstructionRead = ++_offerPage >= 3;
            _game.RefreshTutorialTradeActions();
            Save(false);
            Refresh();
            return;
        }
        if (CurrentStep == Step.NotebookContents && _notebook.Expanded)
        {
            _notebook.HoldOpenForLesson = false;
            _notebookLessonDone = true;
            Save(false);
            Refresh();
            return;
        }
        if (CurrentStep == Step.BuyerRequest)
        {
            _buyerRequestRead = true;
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
        _notebook.HoldOpenForLesson = false;
        if (!IsActive || _finishing) return;
        _finishing = true;
        _musicLessonPending = false;
        StopCamera();
        IsActive = false;
        _root.Hide();
        Target = null;
        _spotlight.SetTarget(null);
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
        Finished?.Invoke();
    }

    private void Save(bool finished)
    {
        if (string.IsNullOrEmpty(ProgressPath)) return;
        var config = new ConfigFile();
        config.SetValue("tutorial", "finished", finished);
        config.SetValue("tutorial", "prologue_pending", ProloguePending);
        config.SetValue("tutorial", "two_sales", true);
        config.SetValue("tutorial", "music_lesson_pending", _musicLessonPending);
        config.SetValue("tutorial", "music_lesson_done", _musicLessonDone);
        config.SetValue("tutorial", "card_lesson_done", _cardLessonDone);
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
        config.SetValue("tutorial", "upgrade_lesson_day", _upgradeLessonDay);
        config.SetValue("tutorial", "offer_page", _offerPage);
        config.SetValue("tutorial", "display_inspected", _displayInspected);
        config.SetValue("tutorial", "notebook_lesson_done", _notebookLessonDone);
        config.SetValue("tutorial", "buyer_request_read", _buyerRequestRead);
        config.SetValue("tutorial", "embroidery_started", _embroideryStarted);
        config.SetValue("tutorial", "offer_instruction_read", _offerInstructionRead);
        config.SetValue("tutorial", "training_clock_duration", DayCycleController.TutorialDuration);
        Error error = config.Save(ProgressPath);
        if (error != Error.Ok) GD.PushWarning($"Tutorial progress could not be saved: {error}");
        _progressDirty = false;
        _progressSaveElapsed = 0;
    }

    public override void _Process(double delta)
    {
        if (!_initialized) return;
        if (!IsSkipConfirmationOpen && TrySupplementalLesson()) return;
        if (!IsActive)
        {
            if (IsSkipConfirmationOpen) Refresh();
            return;
        }
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
            _upgradeLessonDay = 0;
            _packHoverSeconds = 0;
            _notebookLessonDone = false;
            Save(false);
        }
        _lastAccepted = _game.Accepted;
        _sawSale |= _game.Accepted >= _acceptedAtStart + 2;
        Refresh();
        if (_upgradeLessonPending)
        {
            OpenUpgradeLesson();
            return;
        }
        if (CurrentStep == Step.Summary && _summaryPage == 0 && IsShowing && !GetTree().Paused)
        {
            var pack = _game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            if (pack.HoveringHotspot && pack.Expanded && !pack.IsAnimating)
            {
                _packHoverSeconds += delta;
                if (_packHoverSeconds >= 1)
                {
                    _upgradeLessonDay = _game.CurrentDay;
                    _upgradeLessonPending = true;
                    Save(false);
                    OpenUpgradeLesson();
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
            _card.SetInstructionLocked(!_cardLessonDone);
            CurrentStep = Step.UpgradeChoice;
            Refresh();
        }
    }

    private void CompleteUpgradeLesson()
    {
        if (!IsActive) return;
        _upgradeLessonPending = false;
        _upgradeLessonDay = 0;
        _packHoverSeconds = 0;
        _summaryPage = SummaryPages.Length - 1;
        _notebookLessonDone = false;
        Save(false);
        _game.PersistProgress();
        Refresh();
    }

    private void OnEmbroideryStarted()
    {
        if (!IsActive || _embroideryStarted) return;
        _embroideryStarted = true;
        Save(false);
        Refresh();
    }

    private void Refresh()
    {
        if (IsSkipConfirmationOpen)
        {
            _notebook.HoldOpenForLesson = false;
            _spotlight.SetAccessories(null, null, null);
            _root.Show();
            _panel.Hide();
            _skip.Hide();
            _spotlight.SetTarget(null);
            return;
        }
        _panel.Show();
        if (_cameraHandoffDone && !_embroideryStarted && _game.Crafting.IsCatalogOpen &&
            _game.Crafting.Catalog.Workshop == "atelier" && !_pause.IsOpen)
        {
            Layer = _game.Crafting.Catalog.Layer + 2;
            _root.Show();
            SetStep(Step.CraftSelection, "Выберите шляпку в каталоге ателье. Цена указана на карточке.",
                _game.Crafting.Catalog.ItemButton("hat"));
            Layout();
            return;
        }
        if (_cameraHandoffDone && !_embroideryStarted && _embroidery.IsReadyToStart && !_pause.IsOpen)
        {
            Layer = _embroidery.Layer + 2;
            _root.Show();
            SetStep(Step.EmbroideryStart, "Нажмите «Начать», чтобы приступить к вышивке", _embroidery.StartButton);
            Layout();
            return;
        }
        if (GetTree().Paused || _game.CraftMiniGameOpen || _game.DayTransitionOpen || _game.Failed || _game.Completed)
        {
            _notebook.HoldOpenForLesson = false;
            _spotlight.SetAccessories(null, null, null);
            _root.Hide();
            Target = null;
            _spotlight.SetTarget(null);
            StopCamera(cancelOverview: !_game.DayTransitionOpen);
            return;
        }
        Layer = _regularLayer;
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
        else if (_sawSale && _summaryPage == 1 && !_notebookLessonDone)
            SetStep(_notebook.Expanded ? Step.NotebookContents : Step.Notebook,
                _notebook.Expanded ? "В блокноте отображаются текущие задания, активные улучшения и негативные эффекты." : NotebookMessage, _notebook.GetNode<Control>("Page"));
        else if (_sawSale) SetStep(Step.Summary, SummaryPages[_summaryPage].Body,
            _summaryPage == 0 ? _game.GetNode<Control>("HUD/Root/ProgressToggleButton") : null);
        else if (_game.TradeActive && !_buyerRequestRead) SetStep(Step.BuyerRequest, BuyerRequestMessage,
            _game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble"));
        else if (_game.TradeActive && !_offerInstructionRead) ShowOfferLesson();
        else if (_game.TradeActive && HoldTrainingBuyer) SetStep(Step.TimedSale,
            "Продайте товар до истечения времени, чтобы получить надбавку за скорость.",
            _game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble"));
        else if (_game.TradeActive) SetStep(Step.LateSale,
            _game.BuyerWaitRemaining > 0 ? "Дождитесь конца отсчёта." :
            "Даже после истечения времени товар можно продать, но без надбавки за скорость.",
            _game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble"));
        else if (_game.DayClosed) SetStep(Step.Start,
            "Время вышло. В паузе можно начать с чекпоинта и попробовать снова или пропустить обучение.", null);
        else if (!_game.DayStarted) SetStep(Step.Start,
            "Нажмите «Начать» на часах, чтобы открыть магазин. Часы показывают, сколько времени осталось до закрытия.",
            _game.GetNode<Control>("HUD/DayClock/Root/DayTimer/StartButton"));
        else if (_game.FirstDisplayedItem("atelier") is int slot && slot >= 0)
            SetStep(_displayInspected ? Step.Place : Step.Select,
                _displayInspected ? "Нажмите на предмет, чтобы выбрать его для продажи." :
                    "Предмет автоматически занял свободную витрину. Наведите курсор, чтобы узнать цену и качество.",
                _game.DisplayControl(slot));
        else SetStep(Step.Craft,
            "Нажмите „Создать предметы“, чтобы выбрать предмет в каталоге. Для создания нужны деньги.",
            _game.GetNode<Control>("World/Atelier/GetItemButton"));
        Layout();
    }

    private void ShowOfferLesson()
    {
        const string content = "World/TradingRoom/Item/TradePanel/Content/";
        var (message, target) = _offerPage switch
        {
            0 => ("Нажмите „✓“, чтобы продать товар.", "Buttons/AcceptButton"),
            1 => ("Нажмите „×“, чтобы сменить покупателя и цену. Это тратит попытку, а таймер не сбрасывается. Цена может как вырасти, так и упасть.", "Buttons/DeclineButton"),
            _ => ("Чем выше качество товара, тем больше надбавка к его стоимости.", "OfferLabel")
        };
        SetStep(Step.Offer, message, _game.GetNode<Control>(content + target));
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
        _panel.Show();
        _done.Visible = step == Step.Summary && _summaryPage > 0 || step is Step.BuyerRequest or Step.NotebookContents ||
            step == Step.Offer && !_offerInstructionRead;

        _actions.Visible = _done.Visible;
        _done.Text = step is Step.BuyerRequest or Step.Offer or Step.NotebookContents || _summaryPage < SummaryPages.Length - 1 ? "Далее" : "Понятно";
        _title.Text = step switch
        {
            Step.Camera => "Передвижение камеры",
            Step.CameraZoom => "Масштабирование",
            Step.RoomFocus => "Приближение комнаты",
            Step.BuyerRequest => "Запрос покупателя",
            Step.EmbroideryStart => "Вышивка",
            Step.CraftSelection => "Выбор предмета",
            Step.Notebook or Step.NotebookContents => "Блокнот",
            Step.Summary => SummaryPages[_summaryPage].Title,
            _ => "Первая продажа"
        };
        _cameraProgress.Visible = cameraStep && step != Step.RoomFocus;
        _cameraProgress.Value = step == Step.Camera ? CameraPanProgress : CameraZoomProgress;
        Step cameraCheckpoint = cameraStep ? Step.Camera : step is Step.BuyerRequest or Step.TimedSale or Step.LateSale ? Step.Offer :
            step is Step.Notebook or Step.NotebookContents ? Step.Summary : step;
        if (step is not (Step.EmbroideryStart or Step.CraftSelection) && _appliedCameraStep != cameraCheckpoint && !_game.DayTransitionOpen)
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
        if (GodotObject.IsInstanceValid(_embroidery)) _embroidery.Started -= OnEmbroideryStarted;
        if (!GodotObject.IsInstanceValid(_camera)) return;
        _camera.NavigationLocked = false;
        _camera.UserPanned -= OnUserPanned;
        _camera.UserZoomed -= OnUserZoomed;
        _camera.UserFocusedRoom -= OnUserFocusedRoom;
        _camera.OverviewReset -= OnOverviewReset;
        StopCamera();
    }

    internal int ContextTooltipLayer(int normalLayer, Control tooltip)
    {
        _spotlight?.TrackTooltip(tooltip);
        return IsShowing && !GetTree().Paused && !IsSkipConfirmationOpen && Layer == _regularLayer
            ? Layer + 1 : normalLayer;
    }

    internal void OnMusicWorkshopUnlocked()
    {
        if (!_initialized || _musicLessonDone) return;
        _musicLessonPending = true;
        Save(!IsActive);
    }

    private void CompleteMusicLesson()
    {
        _musicLessonPending = false;
        _musicLessonDone = true;
        _camera.NavigationLocked = false;
        Save(!IsActive);
        _root.Hide();
        _spotlight.SetTarget(null);
    }

    private bool TrySupplementalLesson()
    {
        if (string.IsNullOrEmpty(ProgressPath)) return false;
        if (IsActive && _upgradeLessonPending && !_cardLessonDone && _card.IsOpen && !_card.IsAnimating)
        {
            _card.SetInstructionLocked(true);
            ShowSupplemental(Step.CardExplanation, "Улучшения между днями",
                _cardPage == 0
                    ? "В конце дня можно выбрать одно улучшение или пропустить выбор. Некоторые улучшения стоят денег."
                    : "Улучшения действуют в течение следующего дня. Денежные награды выдаются сразу.",
                _card.GetNode<Control>("Root/Center/Card/Content/Choices"), _card.Layer + 1);
            return true;
        }

        if (IsActive || !_musicLessonPending || _musicLessonDone || !_game.MusicWorkshopUnlocked) return false;
        if (GetTree().Paused || _game.CraftMiniGameOpen || _game.DayTransitionOpen || _game.Failed || _game.Completed)
        {
            _root.Hide();
            _spotlight.SetTarget(null);
            return true;
        }

        string message = _musicPage switch
        {
            0 => "Мастерская открывает новые инструменты и витрины. Выберите инструмент в каталоге: его цена указана на карточке.",
            1 => "В полировке удерживайте ЛКМ и водите тряпочкой по инструменту.",
            2 => "Готовый инструмент сам займёт свободную витрину. Наведите курсор для просмотра, нажмите для продажи.",
            _ => "Сверяйтесь с облачком покупателя. Надбавка за скорость доступна 10–20 секунд для одежды и 15–25 секунд для инструментов."
        };
        Control focus = _musicPage switch
        {
            0 => _game.GetNode<Control>("World/MusicWorkshop/GetItemButton"),
            1 => null,
            2 => _game.GetNode<Control>("World/Showroom/Showcases/Slot4"),
            _ => _game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble")
        };
        ShowSupplemental(Step.MusicWorkshop, "Музыкальная мастерская", message, focus, _regularLayer);
        return true;
    }

    private void ShowSupplemental(Step step, string title, string message, Control target, int layer)
    {
        CurrentStep = step;
        if (step == Step.MusicWorkshop) _camera.NavigationLocked = false;
        Layer = layer;
        _root.Show();
        _panel.Show();
        _title.Text = title;
        _body.Text = message;
        _cameraProgress.Hide();
        _actions.Show();
        _done.Show();
        _done.Text = "Далее";
        Target = target;
        Layout();
    }

    private void Layout()
    {
        Rect2 screen = GetViewport().GetVisibleRect();
        float width = Mathf.Min(420, screen.Size.X - 24);
        _panel.Size = new Vector2(width, 0);
        _panel.Position = new Vector2((screen.Size.X - width) / 2, 14);
        _skip.Visible = CurrentStep != Step.CardExplanation;
        bool packOpen = CurrentStep == Step.Summary && _summaryPage == 0 && _pack.Expanded;
        System.Func<Rect2> bounds = packOpen ? _pack.CigaretteBounds : null;
        _notebook.HoldOpenForLesson = IsActive && CurrentStep == Step.NotebookContents;
        Control sale = CurrentStep is Step.TimedSale or Step.LateSale ? _salePanel : null;
        _spotlight.SetAccessories(_panel, _skip, sale);
        _spotlight.SetTarget(Target, bounds);
        if (!GodotObject.IsInstanceValid(Target) || !Target.IsVisibleInTree()) return;
        Rect2 target = bounds?.Invoke() ?? TutorialSpotlight.TargetBounds(Target);
        if (sale?.IsVisibleInTree() == true)
            target = target.Merge(TutorialSpotlight.TargetBounds(sale));
        Rect2 visible = target.Grow(6).Intersection(screen.Grow(-4));
        if (!visible.HasArea()) return;
        // Keep the hint away from the very control the player is asked to click.
        if (new Rect2(_panel.Position, _panel.Size).Intersects(visible))
            _panel.Position = new Vector2((screen.Size.X - width) / 2,
                Mathf.Clamp(visible.End.Y + 16, 14, Mathf.Max(14, screen.Size.Y - _panel.Size.Y - 14)));
    }
}
