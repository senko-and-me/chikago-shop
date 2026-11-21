using Godot;

// Presentation only. Bulbs always fit inside the same framed panel as the cash counter.
public partial class EnergyBulbsPresentation : HBoxContainer
{
    [Export] public PackedScene BulbScene { get; set; } = null!;
    [Export] public Texture2D LitTexture { get; set; } = null!;
    [Export] public Texture2D SpentTexture { get; set; } = null!;
    private TradeScene _game = null!;
    private Label _description = null!;
    private float _lastWidth = -1;
    private float _lastHeight = -1;
    public int LitCount { get; private set; } = -1;
    public int BulbCount { get; private set; } = -1;

    public override void _Ready()
    {
        for (Node ancestor = GetParent(); ancestor != null; ancestor = ancestor.GetParent())
            if (ancestor is TradeScene game) { _game = game; break; }
        _description = GetParent().GetNode<Label>("EnergyLabel");
        AddThemeConstantOverride("separation", 3);
        Resized += FitBulbs;
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
        int remaining = _game.EnergyRemaining;
        int capacity = _game.EnergyCapacity;
        if (remaining != LitCount || capacity != BulbCount)
        {
            LitCount = remaining;
            BulbCount = capacity;
            while (GetChildCount() < BulbCount)
                AddChild(BulbScene.Instantiate<TextureRect>());
            for (int index = 0; index < GetChildCount(); index++)
            {
                var bulb = GetChild<TextureRect>(index);
                bulb.Visible = index < BulbCount;
                // Spending goes from right to left; no slot moves while energy is spent.
                bulb.Texture = index < LitCount ? LitTexture : SpentTexture;
                bulb.SelfModulate = Colors.White;
            }
            FitBulbs();
        }
        if (!Mathf.IsEqualApprox(Size.X, _lastWidth) || !Mathf.IsEqualApprox(Size.Y, _lastHeight))
            FitBulbs();
    }

    private void FitBulbs()
    {
        _lastWidth = Size.X;
        _lastHeight = Size.Y;
        if (BulbCount <= 0) return;
        // The parent StyleBox reserves paper borders; these are content-area sizes.
        float width = Mathf.Max(30f, Size.X);
        float height = Mathf.Max(30f, Size.Y);
        const float edge = 5f;
        const float minimalGap = 3f;
        const float artworkAspect = 1057f / 1682f;
        float iconHeight = Mathf.Min(height - 3f,
            (width - 2f * edge - (BulbCount - 1) * minimalGap) / BulbCount / artworkAspect);
        float iconWidth = iconHeight * artworkAspect;
        float gap = BulbCount > 1
            ? Mathf.Max(minimalGap, (width - 2f * edge - iconWidth * BulbCount) / (BulbCount - 1))
            : 0f;
        AddThemeConstantOverride("separation", Mathf.RoundToInt(gap));
        Alignment = AlignmentMode.Center;
        for (int i = 0; i < GetChildCount(); i++)
        {
            var bulb = GetChild<TextureRect>(i);
            bulb.CustomMinimumSize = new Vector2(iconWidth, iconHeight);
            bulb.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            bulb.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            bulb.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            bulb.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        }
    }
}
