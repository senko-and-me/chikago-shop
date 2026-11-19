using Godot;

// Scene-authored layout; runtime only reflects the current energy state.
public partial class EnergyBulbsPresentation : HBoxContainer
{
    [Export] public PackedScene BulbScene { get; set; } = null!;
    [Export] public Color SpentColor { get; set; } = new(0.28f, 0.28f, 0.28f);
    private TradeScene _game = null!;
    private Label _description = null!;
    public int LitCount { get; private set; }
    public int BulbCount { get; private set; }

    public override void _Ready()
    {
        for (Node ancestor = GetParent(); ancestor != null; ancestor = ancestor.GetParent())
            if (ancestor is TradeScene game) { _game = game; break; }
        _description = GetParent().GetNode<Label>("EnergyLabel");
        if (_game != null) Refresh();
    }

    public override void _Process(double delta)
    {
        if (_game != null) Refresh();
    }

    private void Refresh()
    {
        TooltipText = _description.Text;
        GetParent<Control>().TooltipText = TooltipText;
        if (LitCount == _game.EnergyRemaining && BulbCount == _game.EnergyCapacity) return;
        LitCount = _game.EnergyRemaining;
        BulbCount = _game.EnergyCapacity;
        // Debug balance may exceed the seven slots authored for the normal game.
        while (GetChildCount() < BulbCount) AddChild(BulbScene.Instantiate<TextureRect>());
        for (int index = 0; index < GetChildCount(); index++)
        {
            var bulb = GetChild<TextureRect>(index);
            bulb.Visible = index < BulbCount;
            bulb.SelfModulate = index < LitCount ? Colors.White : SpentColor;
        }
    }
}
