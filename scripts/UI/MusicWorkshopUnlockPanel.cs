using Godot;

public partial class MusicWorkshopUnlockPanel : PanelContainer
{
    public Button UnlockButton { get; private set; } = null!;

    public override void _Ready()
    {
        UnlockButton = GetNode<Button>("Content/UnlockButton");
        ButtonDropShadow.Attach(this);
        UnlockButton.Pressed += OpenWorkshop;
    }

    private void OpenWorkshop()
    {
        for (Node parent = GetParent(); parent != null; parent = parent.GetParent())
            if (parent is TradeScene game)
            {
                game.TryUnlockMusicWorkshop();
                return;
            }
    }
}
