using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Configuration;
using Godot;

public partial class DebugBalancePanel : CanvasLayer
{
	private const string Content = "Root/Inset/Panel/Content";
	private Control _root;
	private Label _status, _preview;
	private readonly Dictionary<string, LineEdit> _numbers = new();
	private readonly Dictionary<string, OptionButton> _rarities = new();
	private readonly List<(Node Node, ProcessModeEnum Mode)> _suspended = new();
	private TradeScene _game;
	private bool _wasPaused;
	public bool IsOpen => _root?.Visible == true;
	public LineEdit Editor(string key) => _numbers[key];
	public OptionButton RarityEditor(string kind) => _rarities[kind];

	public override void _Ready()
	{
		if (!OS.IsDebugBuild()) { QueueFree(); return; }
		_root = GetNode<Control>("Root");
		_status = GetNode<Label>(Content + "/Status");
		_preview = GetNode<Label>(Content + "/Preview");
		GetNode<Button>(Content + "/Header/Close").Pressed += Close;
		GetNode<Button>(Content + "/Actions/Apply").Pressed += () => Apply(false);
		GetNode<Button>(Content + "/Actions/NewGame").Pressed += () => Apply(true);
		GetNode<Button>(Content + "/Actions/Defaults").Pressed += () =>
		{
			Fill(new BalanceProfile());
			_status.Text = "Исходные значения загружены в поля. Для сохранения нажмите «Применить».";
		};
		GetNode<TabContainer>(Content + "/Tabs").TabChanged += _ => Preview();
		var rowScene = GD.Load<PackedScene>("res://scenes/UI/DebugBalanceNumberRow.tscn");
		foreach (var field in DebugBalanceSettings.Fields)
		{
			var row = rowScene.Instantiate<HBoxContainer>();
			row.Name = field.Key.Replace('.', '_');
			GetNode(Content + "/Tabs/" + field.Section + "/Rows").AddChild(row);
			row.GetNode<Label>("Label").Text = field.Label;
			var editor = row.GetNode<LineEdit>("Number");
			editor.Text = field.Default.ToString(System.Globalization.CultureInfo.InvariantCulture);
			editor.TooltipText = $"{field.Min} .. {field.Max} / {field.Step}";
			_numbers.Add(field.Key, editor);
			editor.TextChanged += _ => Preview();
			if (!field.Key.StartsWith("item.")) continue;
			string kind = field.Key.Split('.')[1];
			var rarity = row.GetNode<OptionButton>("Rarity");
			rarity.Show();
			foreach (var definition in EconomyConfigProvider.Baseline.Rarities) rarity.AddItem(definition.Name);
			_rarities.Add(kind, rarity);
			rarity.ItemSelected += _ => Preview();
		}
		Preview();
	}
	public override void _Input(InputEvent input)
	{
		if (!OS.IsDebugBuild() || input is not InputEventKey { Pressed: true, Echo: false } key) return;
		if (key.Keycode == Key.F10 && !key.CtrlPressed && !key.AltPressed && !key.ShiftPressed && !key.MetaPressed)
		{
			if (IsOpen) Close(); else Open();
			GetViewport().SetInputAsHandled();
		}
		else if (IsOpen && key.Keycode == Key.Escape)
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}
	public void Open()
	{
		if (IsOpen || !OS.IsDebugBuild()) return;
		_game = GetTree().CurrentScene as TradeScene ?? GetTree().Root.GetChildren().OfType<TradeScene>().FirstOrDefault();
		// Card tweens deliberately run while paused; do not interrupt their transaction/presentation.
		if (_game?.DayTransitionOpen == true) return;
		_wasPaused = GetTree().Paused;
		if (_game != null)
			foreach (var node in _game.GetChildren().Where(node => node is CanvasLayer || node is DebugHotkeysController))
			{
				_suspended.Add((node, node.ProcessMode));
				node.ProcessMode = ProcessModeEnum.Disabled;
			}
		GetTree().Paused = true;
		Fill(DebugBalanceSettings.Profile);
		bool blocked = _game?.CraftMiniGameOpen == true || _game?.GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsSkipConfirmationOpen == true;
		GetNode<Button>(Content + "/Actions/Apply").Disabled = blocked;
		GetNode<Button>(Content + "/Actions/NewGame").Disabled = blocked || _game == null;
		_status.Text = blocked ? "Мини-игра/подтверждение заморожены. Закройте их перед применением баланса." : "Изменения в полях ещё не применены.";
		_root.Show();
		GetNode<Button>(Content + "/Header/Close").GrabFocus();
	}
	public void Close()
	{
		if (!IsOpen) return;
		_root.Hide();
		foreach (var entry in _suspended)
			if (GodotObject.IsInstanceValid(entry.Node)) entry.Node.ProcessMode = entry.Mode;
		_suspended.Clear();
		GetTree().Paused = _wasPaused;
		_game = null;
	}
	private void Fill(BalanceProfile profile)
	{
		foreach (var field in DebugBalanceSettings.Fields) _numbers[field.Key].Text = profile.Number(field.Key, field.Default).ToString(System.Globalization.CultureInfo.InvariantCulture);
		foreach (var item in WorkshopItemCatalog.BaselineItems)
		{
			string id = profile.ItemRarities.GetValueOrDefault(item.Kind, item.RarityId);
			_rarities[item.Kind].Select(EconomyConfigProvider.Baseline.Rarities.FindIndex(rarity => rarity.Id == id));
		}
		Preview();
	}
	private BalanceProfile Draft()
	{
		var draft = new BalanceProfile
		{
			ItemRarities = _rarities.ToDictionary(entry => entry.Key,
				entry => EconomyConfigProvider.Baseline.Rarities[entry.Value.Selected].Id)
		};
		foreach (var (key, editor) in _numbers)
		{
			if (!TryNumber(editor.Text, out double value))
				throw new ArgumentException("Введите число: " + DebugBalanceSettings.Fields.Single(field => field.Key == key).Label);
			draft.Numbers[key] = value;
		}
		return draft;
	}
	private static bool TryNumber(string text, out double value) => double.TryParse(text.Replace(',', '.'),
		System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
	private double Number(string key) => TryNumber(_numbers[key].Text, out double value) ? value : 0;
	private void Apply(bool newGame)
	{
		if (_game?.CraftMiniGameOpen == true || _game?.DayTransitionOpen == true ||
			_game?.GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsSkipConfirmationOpen == true) return;
		BalanceProfile draft;
		try { draft = Draft(); }
		catch (ArgumentException exception) { _status.Text = "Не применено: " + exception.Message; return; }
		if (!DebugBalanceSettings.TrySave(draft, out string error)) { _status.Text = "Не применено: " + error; return; }
		Fill(DebugBalanceSettings.Profile);
		if (GodotObject.IsInstanceValid(_game)) _game.RefreshDebugBalance();
		_status.Text = "Сохранено. Новые действия используют этот баланс; деньги не выдаются повторно.";
		if (!newGame || _game == null) return;
		var game = _game;
		Close();
		game.GetNode<PauseMenu>("PauseMenu").Close();
		game.NewGame();
	}
	private void Preview()
	{
		if (_preview == null || _numbers.Count != DebugBalanceSettings.Fields.Count) return;
		string tab = GetNode<TabContainer>(Content + "/Tabs").GetCurrentTabControl().Name;
		if (tab == "Редкости")
		{
			var rarities = EconomyConfigProvider.Baseline.Rarities;
			double sum = rarities.Sum(rarity => Number($"rarity.{rarity.Id}.weight"));
			_preview.Text = "Вес 0 исключает редкость. При наличии всех категорий: " + string.Join(" · ", rarities.Select(rarity =>
				$"{rarity.Name} {(sum > 0 ? Number($"rarity.{rarity.Id}.weight") / sum * 100 : 0):0.#}%")) +
				". Пустые категории исключаются, остальные шансы нормируются внутри мастерской.";
		}
		else if (tab == "Карты")
		{
			double sum = DebugBalanceSettings.Cards.Sum(card => Number($"card.{card}.weight"));
			_preview.Text = "В режиме 1 выбираются до трёх разных карт без повторов. Шанс первого выбора: " +
				string.Join(" · ", DebugBalanceSettings.Cards.Select(card =>
					$"{DebugBalanceSettings.CardName(card)} {(sum > 0 ? Number($"card.{card}.weight") / sum * 100 : 0):0.#}%")) +
				". Один seed повторяет выбор при загрузке.";
		}
		else _preview.Text = "«Применить сейчас» меняет будущий крафт, цены и длительность дня с сохранением уже прошедшего времени. «Начать заново» сбрасывает партию. Новое время мини-игры применяется при её следующем открытии.";
	}
	public override void _ExitTree() { if (IsOpen) Close(); }
}
