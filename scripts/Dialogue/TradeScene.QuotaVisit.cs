using ChikagoShop.Domain.Persistence;
using Godot;

public partial class TradeScene
{
    public const int QuotaGift = ChikagoShop.Domain.Economy.RunRewardRules.FirstQuotaGift;
    private QuotaVisitStage _quotaVisit;
    private QuotaVisitStage _checkpointQuotaVisit;
    private int _quotaDialoguePage;
    private bool _quotaDialogueRevealed;
    private string _quotaDialogueLanguage = "";
    private TextureButton _quotaVisitor = null!;
    private CanvasLayer _quotaDialogueLayer = null!;
    private DialoguePlayer _quotaDialogue;
    private bool _ownsQuotaDialoguePause;

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
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            Material = tone,
            Visible = false
        };
        _customer.GetParent().AddChild(_quotaVisitor);
        _quotaVisitor.MouseEntered += () => tone.SetShaderParameter("lighten", 0.16f);
        _quotaVisitor.MouseExited += () => tone.SetShaderParameter("lighten", 0f);
        _quotaVisitor.Pressed += OpenQuotaDialogue;
        _quotaDialogueLayer = new CanvasLayer { Name = "QuotaDialogue", Layer = 151, ProcessMode = ProcessModeEnum.Always };
        AddChild(_quotaDialogueLayer);
    }

    public void ResumeQuotaVisit()
    {
        if (!QuotaVisitPending || MafiaNoticePending || _quotaDialogue != null || _quotaVisitor.Visible) return;
        _customer.Hide();
        _quotaVisitor.Show();
        var frame = _customer.GetParent().GetNode<Control>("RoomFrame");
        var camera = GetNode<WorldCamera>("Camera2D");
        camera.FocusOn(frame.GetGlobalRect().GetCenter(), camera.CloseupSize, _quotaVisit == QuotaVisitStage.Dialogue ? 0 : 0.55);
        if (_quotaVisit == QuotaVisitStage.Dialogue) OpenQuotaDialogue();
    }

    private void OpenQuotaDialogue()
    {
        if (!QuotaVisitPending || MafiaNoticePending || _quotaDialogue != null ||
            GetTree().Paused && _quotaVisit != QuotaVisitStage.Dialogue) return;
        if (_quotaVisit != QuotaVisitStage.Dialogue) ClearQuotaDialoguePage();
        _quotaVisit = QuotaVisitStage.Dialogue;
        GetViewport().GuiReleaseFocus();
        _ownsQuotaDialoguePause = true;
        GetTree().Paused = true;
        var dialogue = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
        dialogue.DialoguePath = "res://dialogue/quota_visitor.ru.json";
        dialogue.IsPrologue = false;
        dialogue.StoryPortrait = _quotaVisitor.TextureNormal;
        dialogue.StoryBackground = _quotaVisitor.GetParent().GetNode<Sprite2D>("TradingRoomBackground").Texture;
        _quotaDialogue = dialogue;
        _quotaVisitor.Hide();
        _quotaDialogueLayer.AddChild(dialogue);
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
        _quotaVisitor.Show();
        ClearQuotaDialoguePage();
        SaveProgress();
    }

    private void CompleteQuotaVisit()
    {
        if (_quotaVisit != QuotaVisitStage.Dialogue) return;
        _quotaVisit = _checkpointQuotaVisit = QuotaVisitStage.Rewarded;
        Score += QuotaGift;
        CheckpointScore += QuotaGift;
        ResetQuotaVisitPresentation();
        _customer.Hide();
        RefreshDayPresentation();
        _dayCycle.FinishQuotaVisit();
    }

    private void CloseQuotaDialogue()
    {
        if (_quotaDialogue == null) return;
        _quotaDialogue.StopInteraction();
        _quotaDialogue.QueueFree();
        _quotaDialogue = null;
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
