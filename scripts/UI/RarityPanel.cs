using System;
using System.Linq;
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
	private GridContainer _values = null!;
	private Control[] _hudControls;
	public PanelContainer Popup => _panel;
	public GridContainer Values => _values;

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
		_values = _panel.GetNode<GridContainer>("Values");
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
		RefreshChances();
		_layer.Visible = IsVisibleInTree();
		SetProcess(_layer.Visible);
		if (_layer.Visible) UpdatePlacement();
	}

	public override void _Process(double delta)
	{
		if (IsVisibleInTree()) { RefreshChances(); UpdatePlacement(); }
	}

	public void RefreshChances()
	{
		string workshop = GetParent().Name == "MusicWorkshop" ? "music" : "atelier";
		string[] rarities = ["common", "ordinary", "first_class", "exclusive", "unique"];
		var upgrades = (_game as TradeScene)?.DayUpgrades;
		var items = WorkshopItemCatalog.Items.Where(item => item.WorkshopId == workshop).ToArray();
		var populated = items.Select(item => item.RarityId).ToHashSet();
		double total = rarities.Where(populated.Contains).Sum(rarity => WorkshopItemCatalog.RarityWeight(workshop, rarity, upgrades));
		double baseTotal = rarities.Where(populated.Contains).Sum(WorkshopItemCatalog.BaselineWeight);
		for (int i = 0; i < rarities.Length; i++)
		{
			double baseline = populated.Contains(rarities[i]) && baseTotal > 0 ? WorkshopItemCatalog.BaselineWeight(rarities[i]) / baseTotal * 100 : 0;
			double current = populated.Contains(rarities[i]) && total > 0 ? WorkshopItemCatalog.RarityWeight(workshop, rarities[i], upgrades) / total * 100 : 0;
			var label = Values.GetNode<Label>($"Chance{i}");
			label.Text = $"{current:0.#}%";
			label.TooltipText = GameLocalization.Format("Базовый шанс: {0}%", baseline);
			label.AddThemeColorOverride("font_color", Colors.Black);
			var category = items.Where(item => item.RarityId == rarities[i]).ToArray();
			int average = category.Length > 0 ? (int)Math.Round(category.Average(item => item.BasePrice)) : 0;
			int price = upgrades?.Price(workshop, average) ?? average;
			var priceLabel = Values.GetNode<Label>($"Price{i}");
			priceLabel.Text = $"{price}$";
			priceLabel.AddThemeColorOverride("font_color", Colors.Black);
			Values.GetNode<Label>($"Rerolls{i}").Text = WorkshopItemCatalog.RerollsFor(rarities[i]).ToString();
		}
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
		// Resolve late-created siblings once, after the complete scene has entered the tree.
		_hudControls ??=
		[
			_game.GetNode<Control>("HUD/Root/ProgressPanel"),
			_game.GetNode<Control>("HUD/Root/ProgressToggleButton"),
			_game.GetNode<Control>("HUD/Root/ScorePanel"),
			_game.GetNode<Control>("HUD/Root/EnergyPanel"),
			_game.GetNode<Control>("HUD/InventoryUI/ScrollContainer")
		];
		Span<Rect2> obstacles = stackalloc Rect2[_hudControls.Length];
		int count = 0;
		foreach (Control control in _hudControls)
		{
			if (control.IsVisibleInTree()) obstacles[count++] = control.GetGlobalRect();
		}
		_panel.Position = PopupPlacement.FindPosition(viewport, anchor, size, obstacles[..count]);
	}
}
