using Godot;

// The original localized label remains the source of the accessible numeric description.
public partial class EnergyBulbsPresentation : Control
{
    private TradeScene _game = null!;
    private Label _description = null!;
    private Texture2D _texture = null!;
    private int _remaining = -1;
    private int _capacity = -1;
    private static readonly Rect2 ArtworkRegion = new(526, 213, 984, 1612);

    public int LitCount => _remaining;
    public int BulbCount => _capacity;

    public static void Attach(TradeScene game)
    {
        var panel = game.GetNode<PanelContainer>("HUD/Root/EnergyPanel");
        var description = panel.GetNode<Label>("EnergyLabel");
        description.Hide();
        var style = (StyleBox)panel.GetThemeStylebox("panel").Duplicate();
        style.ContentMarginLeft = style.ContentMarginRight = 10;
        style.ContentMarginTop = style.ContentMarginBottom = 6;
        panel.AddThemeStyleboxOverride("panel", style);
        panel.MouseFilter = MouseFilterEnum.Pass;
        panel.AddChild(new EnergyBulbsPresentation
        {
            Name = "EnergyBulbs", _game = game, _description = description,
            ProcessMode = ProcessModeEnum.Always, MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill
        });
    }

    public override void _Ready()
    {
        _texture = GD.Load<Texture2D>("res://art/ui/energy_bulb.png");
        Resized += QueueRedraw;
        Refresh();
    }

    public override void _Process(double delta) => Refresh();

    private void Refresh()
    {
        TooltipText = _description.Text;
        GetParent<Control>().TooltipText = TooltipText;
        if (_remaining == _game.EnergyRemaining && _capacity == _game.EnergyCapacity) return;
        _remaining = _game.EnergyRemaining;
        _capacity = _game.EnergyCapacity;
        QueueRedraw();
    }

    public Rect2 BulbRect(int index)
    {
        int count = Mathf.Max(1, _capacity);
        float gap = count <= 3 ? 12 : 5;
        float height = Mathf.Min(46, Size.Y - 10);
        float width = height * ArtworkRegion.Size.X / ArtworkRegion.Size.Y;
        width = Mathf.Min(width, (Size.X - 16 - gap * (count - 1)) / count);
        height = width * ArtworkRegion.Size.Y / ArtworkRegion.Size.X;
        float rowWidth = width * count + gap * (count - 1);
        return new Rect2((Size.X - rowWidth) / 2 + index * (width + gap), (Size.Y - height) / 2, width, height);
    }

    public override void _Draw()
    {
        if (_texture == null) return;
        for (int index = 0; index < _capacity; index++)
            DrawTextureRectRegion(_texture, BulbRect(index), ArtworkRegion,
                index < _remaining ? Colors.White : new Color(0.28f, 0.28f, 0.28f));
    }
}
