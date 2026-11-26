using Godot;

public partial class MusicWorkshopUnlockPanel : PanelContainer
{
    [Export] public Texture2D PanelTexture { get; set; }
    [Export] public Texture2D ButtonTexture { get; set; }

    public Button UnlockButton { get; private set; } = null!;

    public override void _Ready()
    {
        UnlockButton = GetNode<Button>("Content/UnlockButton");
        AddThemeStyleboxOverride("panel", MakePaper(PanelTexture, Colors.White, 46, 34));
        UpdateCaption();
        ButtonDropShadow.Attach(this);
        UnlockButton.AddThemeStyleboxOverride("normal", MakePaper(ButtonTexture, new Color("dedede"), 32, 16));
        UnlockButton.AddThemeStyleboxOverride("hover", MakePaper(ButtonTexture, Colors.White, 32, 16));
        UnlockButton.AddThemeStyleboxOverride("pressed", MakePaper(ButtonTexture, new Color("b8b8b8"), 32, 16));
        UnlockButton.AddThemeStyleboxOverride("disabled", MakePaper(ButtonTexture, new Color("c5c5c5"), 32, 16));
    }

    private void UpdateCaption() => UnlockButton.Text = "Открыть мастерскую";

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
