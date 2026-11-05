using Godot;

public partial class RarityPanel : Control
{
    [Export] public int Price0 { get; set; } = 30;
    [Export] public int Price1 { get; set; } = 50;
    [Export] public int Price2 { get; set; } = 80;
    [Export] public int Price3 { get; set; } = 120;
    [Export] public int Price4 { get; set; } = 200;

    private CanvasLayer _layer = null!;
    private PanelContainer _panel = null!;
    private Control _button = null!;
    private Camera2D _camera = null!;
    private Node _game = null!;
    public PanelContainer Popup => _panel;
    public GridContainer Values => _panel.GetNode<GridContainer>("Values");

    public override void _Ready()
    {
        if (GetParent().Name == "MusicWorkshop")
        {
            Price0 = 45;
            Price1 = 75;
            Price2 = 120;
            Price3 = 180;
            Price4 = 300;
        }

        _layer = GetNode<CanvasLayer>("PopupLayer");
        _panel = _layer.GetNode<PanelContainer>("Panel");
        _button = GetParent().GetNode<Control>("GetItemButton");
        _game = GetParent().GetParent().GetParent();
        _camera = _game.GetNode<Camera2D>("Camera2D");

        string[] headings = ["Редкость", "Ср. цена", "Шанс", "Рероллы"];
        for (int i = 0; i < headings.Length; i++) AddCell($"Heading{i}", headings[i], true, i == 0);
        string[] names = ["Ширпотреб", "Заурядная", "Первоклассная", "Эксклюзивная", "Неповторимая"];
        string[] chances = ["60%", "25%", "10%", "4%", "1%"];
        int[] rerolls = [3, 2, 2, 1, 1];
        int[] prices = [Price0, Price1, Price2, Price3, Price4];
        for (int i = 0; i < prices.Length; i++)
        {
            AddCell($"Rarity{i}", names[i], false, true);
            AddCell($"Price{i}", $"{prices[i]}$");
            AddCell($"Chance{i}", chances[i]);
            AddCell($"Rerolls{i}", rerolls[i].ToString());
        }
        VisibilityChanged += SyncVisibility;
        SyncVisibility();
    }

    private void AddCell(string name, string text, bool heading = false, bool firstColumn = false)
    {
        var label = new Label
        {
            Name = name, Text = text, MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(firstColumn ? 146 : 66, 26),
            HorizontalAlignment = firstColumn ? HorizontalAlignment.Left : HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", Colors.Black);
        label.AddThemeFontSizeOverride("font_size", heading ? 17 : 16);
        Values.AddChild(label);
    }

    private void SyncVisibility()
    {
        _layer.Visible = IsVisibleInTree();
        if (_layer.Visible) UpdatePlacement();
    }

    public override void _Process(double delta)
    {
        if (IsVisibleInTree()) UpdatePlacement();
    }

    public void UpdatePlacement()
    {
        // Screen-space placement keeps every column readable regardless of camera zoom.
        Rect2 viewport = GetViewportRect().Grow(-12);
        Vector2 size = _panel.GetCombinedMinimumSize();
        _panel.Size = size;
        float scale = Mathf.Clamp(1f + (1f - _camera.Zoom.X) * 0.08f, 0.95f, 1.05f);
        scale = Mathf.Min(scale, Mathf.Min(viewport.Size.X / size.X, viewport.Size.Y / size.Y));
        _panel.Scale = Vector2.One * scale;
        size *= scale;
        Rect2 anchor = _button.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, _button.Size);
        const float gap = 12;
        Vector2 center = anchor.GetCenter();
        Vector2[] candidates =
        [
            new(anchor.End.X + gap, center.Y - size.Y / 2),
            new(anchor.Position.X - size.X - gap, center.Y - size.Y / 2),
            new(center.X - size.X / 2, anchor.Position.Y - size.Y - gap),
            new(center.X - size.X / 2, anchor.End.Y + gap),
            new(anchor.End.X + gap, anchor.Position.Y - size.Y - gap),
            new(anchor.Position.X - size.X - gap, anchor.Position.Y - size.Y - gap)
        ];
        string[] hudPaths = ["HUD/Root/ProgressPanel", "HUD/Root/ProgressToggleButton", "HUD/Root/ScorePanel", "HUD/Root/EnergyPanel",
            "HUD/InventoryUI/ScrollContainer", "PauseMenu/Root/PauseButton"];
        Vector2 best = viewport.Position;
        float bestScore = float.PositiveInfinity;
        foreach (Vector2 candidate in candidates)
        {
            Vector2 position = candidate.Clamp(viewport.Position, viewport.End - size);
            Rect2 rect = new(position, size);
            float score = OverlapArea(rect, anchor) * 10 + candidate.DistanceSquaredTo(position);
            foreach (string path in hudPaths)
            {
                var control = _game.GetNode<Control>(path);
                if (control.IsVisibleInTree())
                    score += OverlapArea(rect, control.GetGlobalRect()) * 4;
            }
            if (score >= bestScore) continue;
            bestScore = score;
            best = position;
        }
        _panel.Position = best;
    }

    private static float OverlapArea(Rect2 a, Rect2 b) => a.Intersection(b).Area;
}
