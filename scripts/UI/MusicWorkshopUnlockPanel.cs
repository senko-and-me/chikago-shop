using Godot;

public partial class MusicWorkshopUnlockPanel : PanelContainer
{
    public Button UnlockButton { get; private set; } = null!;
    private int _price;

    public override void _Ready()
    {
        UnlockButton = GetNode<Button>("Content/UnlockButton");
        ButtonDropShadow.Attach(this);
    }

    public void SetOffer(int price, bool available)
    {
        _price = price;
        UnlockButton.Disabled = !available;
        UpdateCaption();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && UnlockButton != null) UpdateCaption();
    }

    private void UpdateCaption() => UnlockButton.Text = GameLocalization.Format("Открыть за {0}$", _price);
}
