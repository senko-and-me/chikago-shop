using ChikagoShop.Domain.Persistence;
using System.Threading.Tasks;
using Godot;

public partial class TradeScene
{
    private QuotaVisitStage _quotaVisit;
    private QuotaVisitStage _checkpointQuotaVisit;
    private int _quotaDialoguePage;
    private bool _quotaDialogueRevealed;
    private string _quotaDialogueLanguage = "";
    private TextureButton _quotaVisitor = null!;
    private CanvasLayer _quotaDialogueLayer = null!;
    private DialoguePlayer _quotaDialogue;
    private bool _ownsQuotaDialoguePause;
    private bool _quotaCompletionInProgress;
    private bool _quotaHudWasVisible;
    private bool _quotaClockWasVisible;

    public bool MusicWorkshopAvailable => !Failed && !Completed && !MusicWorkshopUnlocked &&
        CurrentDay > FirstDeadline && QuotaStage >= 1 && _quotaVisit == QuotaVisitStage.Rewarded;

    public bool QuotaVisitPending => !Failed && !Completed && QuotaStage == 1 &&
        _quotaVisit is QuotaVisitStage.AwaitingVisitor or QuotaVisitStage.Dialogue;

    private void InitializeQuotaVisit()
    {
        var texture = GD.Load<Texture2D>("res://art/characters/quota_visitor.png");
        var tone = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/dialogue_portrait.gdshader") };
        tone.SetShaderParameter("lighten", 0f);
        var mask = new Bitmap();
        using (var image = texture.GetImage()) mask.CreateFromImageAlpha(image);
        Vector2 size = texture.GetSize();
        Vector2 feet = _customer.Position;
        _quotaVisitor = new TextureButton
        {
            Name = "QuotaVisitor",
            TextureNormal = texture,
            TextureClickMask = mask,
            IgnoreTextureSize = true,
            Size = size,
            Position = feet - new Vector2(size.X / 2, size.Y),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = tone,
            Visible = false
        };
        _customer.GetParent().AddChild(_quotaVisitor);
        _quotaDialogueLayer = new CanvasLayer { Name = "QuotaDialogue", Layer = 151, ProcessMode = ProcessModeEnum.Always };
        AddChild(_quotaDialogueLayer);
    }

    public void ResumeQuotaVisit()
    {
        if (!QuotaVisitPending || MafiaNoticePending || _quotaDialogue != null) return;
        _customer.Hide();
        _quotaVisitor.Hide();
        OpenQuotaDialogue();
    }

    private void OpenQuotaDialogue()
    {
        if (!QuotaVisitPending || MafiaNoticePending || _quotaDialogue != null) return;
        if (_quotaVisit != QuotaVisitStage.Dialogue) ClearQuotaDialoguePage();
        _quotaVisit = QuotaVisitStage.Dialogue;
        GetViewport().GuiReleaseFocus();
        _ownsQuotaDialoguePause = true;
        GetTree().Paused = true;
        var dialogue = GD.Load<PackedScene>("res://scenes/UI/FamilyDialoguePlayer.tscn").Instantiate<DialoguePlayer>();
        dialogue.DialoguePath = "res://dialogue/quota_visitor.ru.json";
        dialogue.IsPrologue = false;
        _quotaDialogue = dialogue;
        _quotaVisitor.Show();
        var room = _customer.GetParent().GetNode<Control>("RoomFrame");
        var camera = GetNode<WorldCamera>("Camera2D");
        camera.FrameStoryRoom(room.GetGlobalRect());
        var hud = GetNode<CanvasLayer>("HUD");
        _quotaHudWasVisible = hud.Visible;
        hud.Hide();
        var clock = GetNode<CanvasLayer>("HUD/DayClock");
        _quotaClockWasVisible = clock.Visible;
        clock.Hide();
        _quotaDialogueLayer.AddChild(dialogue);
        Callable.From(() =>
        {
            if (_quotaDialogue == dialogue && QuotaVisitPending) GetTree().Paused = true;
        }).CallDeferred();
        dialogue.Finished += CompleteQuotaVisit;
        dialogue.Cancelled += CancelQuotaDialogue;
        dialogue.PageProgressed += () =>
        {
            _quotaDialoguePage = dialogue.SavedPageStart;
            _quotaDialogueRevealed = dialogue.SavedPageRevealed;
            _quotaDialogueLanguage = GameLocalization.Language;
            SaveProgress();
        };
        if (_quotaDialogueLanguage == GameLocalization.Language)
            dialogue.CallDeferred(nameof(DialoguePlayer.RestorePage), _quotaDialoguePage, _quotaDialogueRevealed);
        else ClearQuotaDialoguePage();
        SaveProgress();
    }

    private void CancelQuotaDialogue()
    {
        CloseQuotaDialogue();
        _quotaVisit = QuotaVisitStage.AwaitingVisitor;
        ClearQuotaDialoguePage();
        SaveProgress();
        Callable.From(ResumeQuotaVisit).CallDeferred();
    }

    private async void CompleteQuotaVisit()
    {
        if (_quotaVisit != QuotaVisitStage.Dialogue || _quotaCompletionInProgress || SceneTransition.IsActive(GetTree())) return;
        _quotaCompletionInProgress = true;
        var dialogue = _quotaDialogue;
        try
        {
            await SceneTransition.Run(this, () =>
            {
                if (_quotaDialogue != dialogue || _quotaVisit != QuotaVisitStage.Dialogue)
                    return Task.FromResult(false);
                CompleteQuotaVisitCore();
                return Task.FromResult(true);
            }, keepChangedPause: true);
        }
        finally { _quotaCompletionInProgress = false; }
    }

    private void CompleteQuotaVisitCore()
    {
        if (_quotaVisit != QuotaVisitStage.Dialogue) return;
        _quotaVisit = _checkpointQuotaVisit = QuotaVisitStage.Rewarded;
        ResetQuotaVisitPresentation();
        GetNode<WorldCamera>("Camera2D").ResetToOverview();
        _customer.Hide();
        RefreshDayPresentation();
        SaveProgress();
        _dayCycle.FinishQuotaVisit();
    }

    public void TryUnlockMusicWorkshop()
    {
        if (GetTree().Paused || DayTransitionOpen || !MusicWorkshopAvailable) return;
        OpenMusicWorkshop();
    }

    private void OpenMusicWorkshop()
    {
        if (MusicWorkshopUnlocked) return;
        MusicWorkshopUnlocked = true;
        CheckpointMusicWorkshopUnlocked = true;
        FillDeferredDisplays();
        _dayCycle.SynchronizeRoomDuration();
        RecalculateInvisibleHand();
        StatusText = "Музыкальная мастерская открыта.";
        _inventoryUI.UpdateUI(_storedItems);
        RefreshScore();
        RefreshProgress();
        RefreshItemButtons();
        _tutorial.OnMusicWorkshopUnlocked();
        SaveProgress();
    }

    private void CloseQuotaDialogue()
    {
        if (_quotaDialogue == null) return;
        _quotaDialogue.StopInteraction();
        _quotaDialogue.QueueFree();
        _quotaDialogue = null;
        GetNode<CanvasLayer>("HUD").Visible = _quotaHudWasVisible;
        GetNode<CanvasLayer>("HUD/DayClock").Visible = _quotaClockWasVisible;
        ReleaseQuotaDialoguePause();
    }

    private void ReleaseQuotaDialoguePause()
    {
        if (!_ownsQuotaDialoguePause) return;
        _ownsQuotaDialoguePause = false;
        GetTree().Paused = false;
    }

    public override void _ExitTree() => ReleaseQuotaDialoguePause();

    private void ClearQuotaDialoguePage()
    {
        _quotaDialoguePage = 0;
        _quotaDialogueRevealed = false;
        _quotaDialogueLanguage = "";
    }

    private void ResetQuotaVisitPresentation()
    {
        CloseQuotaDialogue();
        _quotaVisitor.Hide();
        ClearQuotaDialoguePage();
    }
}
