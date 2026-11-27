using Godot;

public partial class MusicWorkshopUnlockPanel : PanelContainer
{
    public Button UnlockButton { get; private set; } = null!;

    public override void _Ready()
    {
        UnlockButton = GetNode<Button>("Content/UnlockButton");
        ButtonDropShadow.Attach(this);
        UnlockButton.Pressed += ShowLockedNotice;
    }

    private void ShowLockedNotice()
    {
        for (Node parent = GetParent(); parent != null; parent = parent.GetParent())
            if (parent is TradeScene game)
            {
                if (!game.MusicWorkshopUnlocked)
                    game.GetNode<DayStartGate>("DayStartGate")
                        .ShowMessage("Мастерская откроется после первой недели.");
                return;
            }
    }
}
