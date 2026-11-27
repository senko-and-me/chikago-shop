using Godot;

public partial class MusicWorkshopUnlockPanel : PanelContainer
{
    public Button UnlockButton { get; private set; } = null!;

    public override void _Ready()
    {
        UnlockButton = GetNode<Button>("Content/UnlockButton");
        ButtonDropShadow.Attach(this);
    }

}
