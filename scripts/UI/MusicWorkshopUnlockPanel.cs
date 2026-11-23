using Godot;

public partial class MusicWorkshopUnlockPanel : PanelContainer
{
    [Export] public Texture2D PanelTexture { get; set; }
    [Export] public Texture2D ButtonTexture { get; set; }

    public Button UnlockButton { get; private set; } = null!;
    private int _price;

    public override void _Ready()
    {
        UnlockButton = GetNode<Button>("Content/UnlockButton");
        AddThemeStyleboxOverride("panel", MakePaper(PanelTexture, Colors.White, 46, 34));
        ButtonDropShadow.Attach(this);
        UnlockButton.AddThemeStyleboxOverride("normal", MakePaper(ButtonTexture, new Color("dedede"), 32, 16));
        UnlockButton.AddThemeStyleboxOverride("hover", MakePaper(ButtonTexture, Colors.White, 32, 16));
        UnlockButton.AddThemeStyleboxOverride("pressed", MakePaper(ButtonTexture, new Color("b8b8b8"), 32, 16));
        UnlockButton.AddThemeStyleboxOverride("disabled", MakePaper(ButtonTexture, new Color("c5c5c5"), 32, 16));
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

    private static StyleBoxTexture MakePaper(Texture2D texture, Color tint, float horizontal, float vertical) => new()
    {
        Texture = texture,
        ModulateColor = tint,
        ContentMarginLeft = horizontal,
        ContentMarginRight = horizontal,
        ContentMarginTop = vertical,
        ContentMarginBottom = vertical
    };
}
