using System;
using System.Linq;
using Godot;

public partial class CraftCatalog : CanvasLayer
{
    public CraftCoordinator Coordinator { get; set; }
    public bool IsOpen => _root?.Visible == true;
    public string Workshop { get; private set; }
    private Control _root;
    private MarginContainer _center;
    private TradeScene _game;
    private GridContainer _cards;
    private bool _ownsPause;
    private PanelContainer _panel;
    private ScrollContainer _scroll;
    private Label _buyerTime;
    private Vector2 _authoredPanelMinimum;
    private float _authoredScrollHeight;
    public Button ItemButton(string kind) => _cards.GetNode<Button>(kind);

    public override void _Ready()
    {
        _game = GetParent<TradeScene>();
        _root = GetNode<Control>("Root");
        _center = _root.GetNode<MarginContainer>("Center");
        _cards = GetNode<GridContainer>("Root/Center/Panel/Margin/Content/Scroll/Cards");
        _panel = GetNode<PanelContainer>("Root/Center/Panel");
        _scroll = _cards.GetParent<ScrollContainer>();
        _buyerTime = GetNode<Label>("Root/Center/Panel/Margin/Content/BuyerTime");
        _authoredPanelMinimum = _panel.CustomMinimumSize;
        _authoredScrollHeight = _scroll.CustomMinimumSize.Y;
        GetNode<Button>("Root/Center/Panel/Margin/Content/Back").Pressed += Close;
        _root.Resized += Reflow;
        _cards.MinimumSizeChanged += Reflow;
        _root.Hide();
    }

    public void Open(string workshop)
    {
        Workshop = workshop;
        foreach (Node child in _cards.GetChildren()) { _cards.RemoveChild(child); child.QueueFree(); }
        var scene = GD.Load<PackedScene>("res://scenes/UI/CraftCatalogCard.tscn");
        foreach (var entry in WorkshopItemCatalog.Items.Select((item, index) => (item, index))
            .Where(entry => entry.item.WorkshopId == workshop))
        {
            var card = scene.Instantiate<Button>();
            card.Name = entry.item.Kind;
            card.GetNode<TextureRect>("Content/Art").Texture = GD.Load<Texture2D>(entry.item.ArtPath);
            card.GetNode<Label>("Content/Name").Text = GameLocalization.Text(entry.item.Name);
            RefreshAvailability(card, entry.index);
            string kind = entry.item.Kind;
            card.Pressed += () => Coordinator.Select(kind);
            _cards.AddChild(card);
        }
        GetNode<Label>("Root/Center/Panel/Margin/Content/Title").Text = GameLocalization.Text(
            workshop == "music" ? "Выберите инструмент" : "Выберите предмет ателье");
        _ownsPause = true;
        GetTree().Paused = true;
        _root.Show();
        RefreshBuyerTime();
        Reflow();
        _scroll.ScrollVertical = 0;
        var first = _cards.GetChildren().OfType<Button>()
            .FirstOrDefault(button => !button.Disabled);
        first?.GrabFocus(hideFocus: true);
    }

    private void Reflow()
    {
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        int outerWidth = _center.GetThemeConstant("margin_left") + _center.GetThemeConstant("margin_right");
        int outerHeight = _center.GetThemeConstant("margin_top") + _center.GetThemeConstant("margin_bottom");
        float availableWidth = viewport.X - outerWidth;
        _cards.Columns = availableWidth < _authoredPanelMinimum.X ? 3 : 5;
        _panel.CustomMinimumSize = new Vector2(Mathf.Min(_authoredPanelMinimum.X, availableWidth), _authoredPanelMinimum.Y);
        float fixedHeight = _panel.GetCombinedMinimumSize().Y - _scroll.GetCombinedMinimumSize().Y;
        float availableHeight = Mathf.Max(1, viewport.Y - outerHeight - fixedHeight);
        float contentHeight = _cards.GetCombinedMinimumSize().Y + _scroll.GetThemeStylebox("panel").GetMinimumSize().Y;
        _scroll.CustomMinimumSize = new Vector2(0, Mathf.Min(contentHeight, Mathf.Min(_authoredScrollHeight, availableHeight)));
    }

    public override void _Process(double delta)
    {
        bool wasClosed = _game.DayClosed;
        _game.TickCraftClocks(delta);
        if (!IsOpen) return;
        RefreshBuyerTime();
        if (!wasClosed && _game.DayClosed)
            foreach (Button card in _cards.GetChildren())
                RefreshAvailability(card, WorkshopItemCatalog.IndexOf(card.Name));
    }

    private void RefreshBuyerTime() =>
        _buyerTime.Text = GameLocalization.Format("Клиент: {0}", _game.BuyerClockText);

    private void RefreshAvailability(Button card, int index)
    {
        string reason = Coordinator.UnavailableReason(index);
        var label = card.GetNode<Label>("Content/Reason");
        label.Text = GameLocalization.Text(reason);
        label.Visible = reason.Length != 0;
        card.Disabled = label.Visible;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationTranslationChanged || !IsOpen || Coordinator == null) return;
        RefreshBuyerTime();
        foreach (Button card in _cards.GetChildren())
        {
            int index = WorkshopItemCatalog.IndexOf(card.Name);
            var item = WorkshopItemCatalog.Items[index];
            card.GetNode<Label>("Content/Name").Text = GameLocalization.Text(item.Name);
            RefreshAvailability(card, index);
        }
        GetNode<Label>("Root/Center/Panel/Margin/Content/Title").Text = GameLocalization.Text(
            Workshop == "music" ? "Выберите инструмент" : "Выберите предмет ателье");
    }

    public void HideForSelection()
    {
        _root.Hide();
        // Pause ownership passes directly to the mini-game, without a running frame between.
        _ownsPause = false;
    }

    public void Close()
    {
        if (!IsOpen || GetParent().GetNode<PauseMenu>("PauseMenu").IsOpen ||
            GetParent().GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsSkipConfirmationOpen) return;
        CloseForReset();
        Coordinator.CancelPreparation();
    }

    public void CloseForReset()
    {
        _root?.Hide();
        if (_ownsPause && IsInsideTree()) GetTree().Paused = false;
        _ownsPause = false;
    }

    public override void _ExitTree()
    {
        if (_ownsPause) GetTree().Paused = false;
    }
}
