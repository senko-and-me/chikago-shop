using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public partial class TradeScene : Node2D
{
	[Export(PropertyHint.Range, "1,9999,1")] public int FirstQuota { get; set; } = 45;
	[Export(PropertyHint.Range, "1,9999,1")] public int SecondQuota { get; set; } = 90;
	[Export] public string SavePath { get; set; } = "user://trade_progress.json";
	[Export(PropertyHint.Range, "1,60,0.5")] public float BuyerWaitSeconds { get; set; } = 8f;

	private const float MinZoom = 0.4f;
	private const float MaxZoom = 1.8f;
	private const float ZoomStep = 1.12f;
	private const float ZoomSmoothness = 12f;
	private const float CameraPadding = 240f;
	private const int FirstDeadline = 4;
	private const int FinalDay = 7;
	private const int ShowcaseCapacity = 6;
	private static readonly (int BasePrice, string Kind, string Name)[] Items =
		[(4, "hat", "шляпа"), (9, "coat", "пальто"), (18, "guitar", "гитара"), (12, "trumpet", "труба")];
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		WriteIndented = true
	};

	private CounterWorld _world = null!;
	private ColorRect _wall = null!;
	private ModularCustomer _customer = null!;
	private Camera2D _camera = null!;
	private PanelContainer _tradePanel = null!;
	private Label _baseLabel = null!;
	private Label _percentLabel = null!;
	private Label _offerLabel = null!;
	private Button _acceptButton = null!;
	private Button _declineButton = null!;
	private Button _rerollButton = null!;
	private Label _scoreLabel = null!;
	private Label _dayLabel = null!;
	private Label _quotaLabel = null!;
	private Label _deadlineLabel = null!;
	private Label _statusLabel = null!;
	private Button _endDayButton = null!;
	private Button _atelierGetItemButton = null!;
	private Button _musicGetItemButton = null!;
	private Button _placeItemButton = null!;
	private Button _trashButton = null!;
	private readonly Button[] _showcaseSlots = new Button[ShowcaseCapacity];
	private readonly ColorRect[] _showcaseGlass = new ColorRect[ShowcaseCapacity];
	private readonly TextureRect[] _showcaseIcons = new TextureRect[ShowcaseCapacity];
	private readonly Label[] _showcaseLabels = new Label[ShowcaseCapacity];

	// --- ДОБАВЛЕНО: Ссылки на UI инвентаря ---
	private InventoryUI _inventoryUI = null!;
	public int SelectedInventoryIndex { get; set; } = -1; // Индекс выбранного слота (-1 если ничего не выбрано)
	// -----------------------------------------

	private readonly RandomNumberGenerator _rng = new();
	private readonly List<int> _storedItems = new();
	private readonly List<int> _checkpointItems = new();
	private readonly int[] _showcaseItems = new int[ShowcaseCapacity];
	private readonly int[] _checkpointShowcaseItems = new int[ShowcaseCapacity];
	private int _activeShowcaseIndex = -1;
	private int _selectedShowcaseIndex = -1;
	private double _buyerCountdown;
	public int[] StoredItems => _storedItems.ToArray();
	public int[] ShowcaseItems => (int[])_showcaseItems.Clone();
	public double BuyerCountdown => _buyerCountdown;
	public static string GetItemKind(int index) => Items[index].Kind;
	public static string GetItemName(int index) => Items[index].Name;
	public int ItemIndex { get; set; } = -1;
	public int CustomerIndex { get; set; } = -1;
	public int Percentage { get; set; }
	public int OfferedPrice { get; private set; }
	public int RerollsRemaining { get; set; }
	public int Score { get; set; }
	public int Accepted { get; private set; }
	public int Declined { get; private set; }
	public int CurrentDay { get; set; } = 1;
	public int QuotaStage { get; private set; }
	public int CheckpointDay { get; private set; } = 1;
	public int CheckpointScore { get; private set; }
	public int CheckpointAccepted { get; private set; }
	public int CheckpointDeclined { get; private set; }
	public bool Failed { get; private set; }
	public bool Completed { get; private set; }
	public string StatusText { get; private set; } = "Торгуйте и завершайте день.";
	public bool TradeActive { get; set; }
	private bool _dragging;
	private float _targetZoom;
	private Vector2 _zoomAnchor;

	public override void _Ready()
	{
		_world = GetNode<CounterWorld>("World");
		_wall = GetNode<ColorRect>("World/Wall");
		_customer = GetNode<ModularCustomer>("World/Customer");
		_camera = GetNode<Camera2D>("Camera2D");
		_tradePanel = GetNode<PanelContainer>("World/Item/TradePanel");
		_baseLabel = GetNode<Label>("World/Item/TradePanel/Content/BasePriceLabel");
		_percentLabel = GetNode<Label>("World/Item/TradePanel/Content/PercentLabel");
		_offerLabel = GetNode<Label>("World/Item/TradePanel/Content/OfferLabel");
		_acceptButton = GetNode<Button>("World/Item/TradePanel/Content/Buttons/AcceptButton");
		_declineButton = GetNode<Button>("World/Item/TradePanel/Content/Buttons/DeclineButton");
		_rerollButton = GetNode<Button>("World/Item/TradePanel/Content/RerollButton");
		_scoreLabel = GetNode<Label>("HUD/Root/ScorePanel/ScoreLabel");
		_dayLabel = GetNode<Label>("HUD/Root/ProgressPanel/Content/DayLabel");
		_quotaLabel = GetNode<Label>("HUD/Root/ProgressPanel/Content/QuotaLabel");
		_deadlineLabel = GetNode<Label>("HUD/Root/ProgressPanel/Content/DeadlineLabel");
		_statusLabel = GetNode<Label>("HUD/Root/ProgressPanel/Content/StatusLabel");
		_endDayButton = GetNode<Button>("HUD/Root/ProgressPanel/Content/EndDayButton");
		_atelierGetItemButton = GetNode<Button>("World/Atelier/GetItemButton");
		_musicGetItemButton = GetNode<Button>("World/MusicWorkshop/GetItemButton");
		_placeItemButton = GetNode<Button>("World/PlaceItemButton");
		_trashButton = GetNode<Button>("World/TrashBin");
		Array.Fill(_showcaseItems, -1);
		Array.Fill(_checkpointShowcaseItems, -1);
		for (int i = 0; i < ShowcaseCapacity; i++)
		{
			int slotIndex = i;
			_showcaseSlots[i] = GetNode<Button>($"World/Showcases/Slot{i + 1}");
			_showcaseGlass[i] = _showcaseSlots[i].GetNode<ColorRect>("Glass");
			_showcaseIcons[i] = _showcaseSlots[i].GetNode<TextureRect>("Icon");
			_showcaseLabels[i] = _showcaseSlots[i].GetNode<Label>("ItemLabel");
			_showcaseSlots[i].Pressed += () => OnShowcasePressed(slotIndex);
		}

		// --- ДОБАВЛЕНО: Инициализация инвентаря ---
		// Важно: убедись, что путь к InventoryUI правильный в твоей сцене
		_inventoryUI = GetNode<InventoryUI>("HUD/InventoryUI");
		_inventoryUI.Initialize(this); // Передаем ссылку на текущую сцену
		// -----------------------------------------

		_rng.Randomize();
		_acceptButton.Pressed += AcceptTrade;
		_declineButton.Pressed += DeclineTrade;
		_rerollButton.Pressed += RerollCustomer;
		_endDayButton.Pressed += EndDay;
		_atelierGetItemButton.Pressed += GetItemFromAtelier;
		_musicGetItemButton.Pressed += GetItemFromMusicWorkshop;
		_placeItemButton.Pressed += PlaceNextItem;
		_trashButton.Pressed += TrashSelectedItem;
		GetNode<Button>("HUD/Root/ProgressPanel/Content/CheckpointButton").Pressed += ReturnToCheckpoint;
		GetNode<Button>("HUD/Root/ProgressPanel/Content/NewGameButton").Pressed += NewGame;

		bool restored = LoadProgress();
		RefreshScore();
		RefreshProgress();
		if (restored)
		{
			if (TradeActive) ShowTrade();
			else FinishTrade();
		}
		else FinishTrade();
		RefreshItemButtons();
		LogStoredItems();

		_targetZoom = _camera.Zoom.X;
		_zoomAnchor = GetViewportRect().Size / 2f;
		GetViewport().SizeChanged += ClampCamera;
		ClampCamera();
	}

	public override void _Process(double delta)
	{
		TickBuyers(delta);
		float oldZoom = _camera.Zoom.X;
		if (Mathf.IsEqualApprox(oldZoom, _targetZoom)) return;
		float blend = 1f - MathF.Exp(-ZoomSmoothness * (float)delta);
		float newZoom = Mathf.Lerp(oldZoom, _targetZoom, blend);
		if (Mathf.Abs(newZoom - _targetZoom) < 0.0001f) newZoom = _targetZoom;
		Vector2 offset = _zoomAnchor - GetViewportRect().Size / 2f;
		_camera.Position += offset * (1f / oldZoom - 1f / newZoom);
		_camera.Zoom = Vector2.One * newZoom;
		ClampCamera();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false } key)
		{
			switch (key.Keycode)
			{
				case Key.Escape:
					GetTree().Quit();
					GetViewport().SetInputAsHandled();
					break;
				case Key.F:
					RerollCustomer();
					GetViewport().SetInputAsHandled();
					break;
			}
		}
		else if (@event is InputEventMouseButton { Pressed: true } wheel)
		{
			switch (wheel.ButtonIndex)
			{
				case MouseButton.WheelUp: ZoomAt(wheel.Position, ZoomStep); break;
				case MouseButton.WheelDown: ZoomAt(wheel.Position, 1f / ZoomStep); break;
			}
		}
		else if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left })
			_dragging = false;
		else if (@event is InputEventMouseMotion motion && _dragging)
		{
			_camera.Position -= motion.Relative / _camera.Zoom;
			ClampCamera();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
			_dragging = true;
	}

	private void ZoomAt(Vector2 mousePosition, float factor)
	{
		float newTarget = Mathf.Clamp(_targetZoom * factor, MinZoom, MaxZoom);
		if (Mathf.IsEqualApprox(newTarget, _targetZoom)) return;
		_targetZoom = newTarget;
		_zoomAnchor = mousePosition;
	}

	public void ClampCamera()
	{
		Rect2 bounds = _wall.GetGlobalRect().Grow(CameraPadding);
		Vector2 halfView = GetViewportRect().Size / (2f * _camera.Zoom);
		Vector2 position = _camera.Position;
		position.X = halfView.X >= bounds.Size.X / 2f
			? bounds.GetCenter().X
			: Mathf.Clamp(position.X, bounds.Position.X + halfView.X, bounds.End.X - halfView.X);
		position.Y = halfView.Y >= bounds.Size.Y / 2f
			? bounds.GetCenter().Y
			: Mathf.Clamp(position.Y, bounds.Position.Y + halfView.Y, bounds.End.Y - halfView.Y);
		_camera.Position = position;
	}

	public void GetItemFromAtelier() => AcquireItem("Ателье", 0, 1);

	public void GetItemFromMusicWorkshop() => AcquireItem("Муз. штуки", 2, 3);

	private void AcquireItem(string workshop, int firstIndex, int lastIndex)
	{
		if (Failed || Completed) return;
		
		// --- ДОБАВЛЕНО: Проверка на переполнение инвентаря ---
		if (_storedItems.Count >= 6)
		{
			GD.Print("Инвентарь полон!");
			return;
		}
		// -----------------------------------------------------

		int index = _rng.RandiRange(firstIndex, lastIndex);
		_storedItems.Add(index);
		GD.Print($"{workshop}: получен предмет {Items[index].Name} (индекс {index}).");
		LogStoredItems();
		RefreshItemButtons();
		
		// --- ДОБАВЛЕНО: Обновляем визуал инвентаря, чтобы появилась иконка ---
		_inventoryUI.UpdateUI(_storedItems);
		// ---------------------------------------------------------------------
		
		SaveProgress();
	}

	private void LogStoredItems()
	{
		var entries = _storedItems.ConvertAll(index => $"{index}:{Items[index].Name}");
		GD.Print($"Массив предметов: [{string.Join(", ", entries)}]");
	}

	public void PlaceNextItem()
	{
		PlaceItemInShowcase(FindFreeShowcaseSlot());
	}

	private int FindFreeShowcaseSlot()
	{
		for (int i = 0; i < ShowcaseCapacity; i++)
			if (i != _activeShowcaseIndex && _showcaseItems[i] == -1) return i;
		return -1;
	}

	public void PlaceItemInShowcase(int slotIndex)
	{
		if (Failed || Completed || slotIndex < 0 || slotIndex >= ShowcaseCapacity ||
			slotIndex == _activeShowcaseIndex ||
			_showcaseItems[slotIndex] != -1 || SelectedInventoryIndex < 0 ||
			SelectedInventoryIndex >= _storedItems.Count) return;

		bool firstItem = Array.TrueForAll(_showcaseItems, item => item == -1);
		_showcaseItems[slotIndex] = _storedItems[SelectedInventoryIndex];
		_storedItems.RemoveAt(SelectedInventoryIndex);
		_selectedShowcaseIndex = -1;
		_inventoryUI.DeselectAll();
		_inventoryUI.UpdateUI(_storedItems);
		if (firstItem) _buyerCountdown = Mathf.Max(1f, BuyerWaitSeconds);
		RefreshItemButtons();
		LogStoredItems();
		SaveProgress();
	}

	public void SelectInventoryItem(int index)
	{
		_selectedShowcaseIndex = -1;
		SelectedInventoryIndex = index;
		RefreshItemButtons();
	}

	private void OnShowcasePressed(int slotIndex)
	{
		if (Failed || Completed || slotIndex == _activeShowcaseIndex) return;
		if (_showcaseItems[slotIndex] < 0)
		{
			PlaceItemInShowcase(slotIndex);
			return;
		}
		bool deselect = _selectedShowcaseIndex == slotIndex;
		_inventoryUI.DeselectAll();
		_selectedShowcaseIndex = deselect ? -1 : slotIndex;
		RefreshItemButtons();
	}

	public void TrashSelectedItem()
	{
		if (Failed || Completed) return;
		if (SelectedInventoryIndex >= 0 && SelectedInventoryIndex < _storedItems.Count)
		{
			_storedItems.RemoveAt(SelectedInventoryIndex);
			_inventoryUI.DeselectAll();
			_inventoryUI.UpdateUI(_storedItems);
		}
		else if (_selectedShowcaseIndex >= 0 && _selectedShowcaseIndex < ShowcaseCapacity &&
			_selectedShowcaseIndex != _activeShowcaseIndex && _showcaseItems[_selectedShowcaseIndex] >= 0)
		{
			_showcaseItems[_selectedShowcaseIndex] = -1;
			_selectedShowcaseIndex = -1;
			if (!TradeActive && Array.TrueForAll(_showcaseItems, item => item == -1))
				_buyerCountdown = 0;
		}
		else return;
		RefreshItemButtons();
		LogStoredItems();
		SaveProgress();
	}

	private void TickBuyers(double delta)
	{
		if (Failed || Completed || TradeActive || Array.TrueForAll(_showcaseItems, item => item == -1)) return;
		_buyerCountdown -= delta;
		if (_buyerCountdown > 0) return;

		var occupied = new List<int>();
		for (int i = 0; i < ShowcaseCapacity; i++)
			if (_showcaseItems[i] >= 0) occupied.Add(i);
		_activeShowcaseIndex = occupied[_rng.RandiRange(0, occupied.Count - 1)];
		_selectedShowcaseIndex = -1;
		ItemIndex = _showcaseItems[_activeShowcaseIndex];
		_showcaseItems[_activeShowcaseIndex] = -1;
		_buyerCountdown = 0;
		RefreshShowcases();
		StartBuyerOffer();
	}

	private void StartBuyerOffer()
	{
		Percentage = _rng.RandiRange(-30, 30);
		RerollsRemaining = 3;
		CustomerIndex = _rng.RandiRange(0, _customer.GetAppearanceCount() - 1);
		TradeActive = true;
		ShowTrade();
		SaveProgress();
	}

	public void ShowTrade()
	{
		_world.ItemKind = Items[ItemIndex].Kind;
		_world.ItemVisible = true;
		_customer.SetAppearance(CustomerIndex);
		_customer.Visible = true;
		_tradePanel.Visible = true;
		_acceptButton.Disabled = false;
		_declineButton.Disabled = false;
		RefreshOffer();
		RefreshItemButtons();
	}

	public void RefreshItemButtons()
	{
		_atelierGetItemButton.Disabled = Failed || Completed;
		_musicGetItemButton.Disabled = Failed || Completed;
		_placeItemButton.Visible = true;
		_placeItemButton.Disabled = Failed || Completed ||
			SelectedInventoryIndex < 0 || SelectedInventoryIndex >= _storedItems.Count ||
			FindFreeShowcaseSlot() < 0;
		RefreshShowcases();
	}

	private void RefreshShowcases()
	{
		for (int i = 0; i < ShowcaseCapacity; i++)
		{
			int itemIndex = _showcaseItems[i];
			bool reserved = TradeActive && i == _activeShowcaseIndex;
			_showcaseIcons[i].Texture = reserved || itemIndex < 0 ? null :
				GD.Load<Texture2D>($"res://art/items/{Items[itemIndex].Kind}.svg");
			_showcaseLabels[i].Text = reserved ? "НА СДЕЛКЕ" :
				itemIndex < 0 ? "СВОБОДНО" : Items[itemIndex].Name.ToUpperInvariant();
			_showcaseGlass[i].Color = reserved ? new Color(0.7f, 0.68f, 0.68f, 0.78f) :
				_selectedShowcaseIndex == i ? new Color(1f, 0.86f, 0.45f, 0.78f) :
				new Color(0.77f, 0.9f, 0.88f, 0.58f);
			_showcaseSlots[i].Disabled = Failed || Completed || reserved ||
				(itemIndex < 0 && (SelectedInventoryIndex < 0 || SelectedInventoryIndex >= _storedItems.Count));
		}
		_trashButton.Disabled = Failed || Completed ||
			(SelectedInventoryIndex < 0 || SelectedInventoryIndex >= _storedItems.Count) &&
			(_selectedShowcaseIndex < 0 || _selectedShowcaseIndex >= ShowcaseCapacity ||
			 _selectedShowcaseIndex == _activeShowcaseIndex || _showcaseItems[_selectedShowcaseIndex] < 0);
	}

	private void RefreshOffer()
	{
		int basePrice = Items[ItemIndex].BasePrice;
		OfferedPrice = PriceForPercent(basePrice, Percentage);
		_baseLabel.Text = $"Обычно: {basePrice}$";
		_percentLabel.Text = $"К цене: {(Percentage >= 0 ? "+" : "")}{Percentage}%";
		_offerLabel.Text = $"Получим: {OfferedPrice}$";
		_rerollButton.Text = $"Другой покупатель ({RerollsRemaining})";
		_rerollButton.Disabled = RerollsRemaining <= 0;
	}

	public static int PriceForPercent(int basePrice, int value) =>
		Mathf.Max(1, Mathf.RoundToInt(basePrice * (1f + value / 100f)));

	public void RerollCustomer()
	{
		if (!TradeActive || RerollsRemaining <= 0) return;
		int basePrice = Items[ItemIndex].BasePrice;
		var higher = new List<int>();
		var lower = new List<int>();
		for (int value = -30; value <= 30; value++)
		{
			int newPrice = PriceForPercent(basePrice, value);
			if (newPrice > OfferedPrice) higher.Add(value);
			else if (newPrice < OfferedPrice) lower.Add(value);
		}
		List<int> pool = _rng.Randf() < 0.7f ? higher : lower;
		if (pool.Count == 0) pool = lower.Count > 0 ? lower : higher;
		if (pool.Count == 0) return;
		Percentage = pool[_rng.RandiRange(0, pool.Count - 1)];
		RerollsRemaining--;
		int appearanceCount = _customer.GetAppearanceCount();
		CustomerIndex = (CustomerIndex + _rng.RandiRange(1, appearanceCount - 1)) % appearanceCount;
		_customer.SetAppearance(CustomerIndex);
		RefreshOffer();
		SaveProgress();
	}

	public void AcceptTrade()
	{
		if (!TradeActive) return;
		Score += OfferedPrice;
		Accepted++;
		RefreshScore();
		FinishTrade();
		RefreshProgress();
		SaveProgress();
	}

	public void DeclineTrade()
	{
		if (!TradeActive) return;
		Declined++;
		ReturnActiveItemToStorage();
		FinishTrade();
		SaveProgress();
	}

	private void ReturnActiveItemToStorage()
	{
		if (!TradeActive || ItemIndex < 0 || ItemIndex >= Items.Length) return;
		if (_activeShowcaseIndex >= 0 && _activeShowcaseIndex < ShowcaseCapacity &&
			_showcaseItems[_activeShowcaseIndex] == -1)
			_showcaseItems[_activeShowcaseIndex] = ItemIndex;
		else
			_storedItems.Insert(0, ItemIndex); // Сохранения до появления витрин.
		LogStoredItems();
		_inventoryUI.UpdateUI(_storedItems);
		RefreshShowcases();
	}

	private void FinishTrade()
	{
		bool hadActiveTrade = TradeActive;
		TradeActive = false;
		ItemIndex = -1;
		_activeShowcaseIndex = -1;
		if (hadActiveTrade)
			_buyerCountdown = Array.TrueForAll(_showcaseItems, item => item == -1) ? 0 :
				Mathf.Max(1f, BuyerWaitSeconds);
		_world.ItemVisible = false;
		_customer.Visible = false;
		_tradePanel.Visible = false;
		_acceptButton.Disabled = true;
		_declineButton.Disabled = true;
		_rerollButton.Disabled = true;
		RefreshItemButtons();
	}

	private void RefreshScore() => _scoreLabel.Text = $"Счёт: {Score}$";

	private void RefreshProgress()
	{
		_dayLabel.Text = $"День: {CurrentDay} / {FinalDay}";
		if (Completed)
		{
			_quotaLabel.Text = "Все квоты выплачены";
			_deadlineLabel.Text = "Долг погашен";
		}
		else
		{
			int target = QuotaStage == 0 ? FirstQuota : SecondQuota;
			int deadline = QuotaStage == 0 ? FirstDeadline : FinalDay;
			_quotaLabel.Text = $"Квота {QuotaStage + 1} / 2: {target}$";
			_deadlineLabel.Text = $"Срок: конец дня {deadline}";
		}
		_statusLabel.Text = StatusText;
		_endDayButton.Disabled = Failed || Completed;
	}

	public void EndDay()
	{
		if (Failed || Completed) return;
		ReturnActiveItemToStorage();
		FinishTrade();
		int deadline = QuotaStage == 0 ? FirstDeadline : FinalDay;
		if (CurrentDay >= deadline)
		{
			int target = QuotaStage == 0 ? FirstQuota : SecondQuota;
			if (Score < target)
			{
				Failed = true;
				StatusText = $"Не хватает {target - Score}$ на квоту. Вернитесь к контрольной точке.";
			}
			else if (QuotaStage == 0)
			{
				Score -= target;
				QuotaStage = 1;
				CurrentDay = FirstDeadline + 1;
				CheckpointDay = CurrentDay;
				CheckpointScore = Score;
				CheckpointAccepted = Accepted;
				CheckpointDeclined = Declined;
				_checkpointItems.Clear();
				_checkpointItems.AddRange(_storedItems);
				Array.Copy(_showcaseItems, _checkpointShowcaseItems, ShowcaseCapacity);
				StatusText = "Первая квота выплачена.";
			}
			else
			{
				Score -= target;
				QuotaStage = 2;
				Completed = true;
				StatusText = "Все квоты выплачены. Победа!";
			}
		}
		else
		{
			CurrentDay++;
			StatusText = "Начался новый день.";
		}
		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		SaveProgress();
	}

	public void ReturnToCheckpoint()
	{
		FinishTrade();
		_storedItems.Clear();
		_storedItems.AddRange(_checkpointItems);
		Array.Copy(_checkpointShowcaseItems, _showcaseItems, ShowcaseCapacity);
		_selectedShowcaseIndex = -1;
		_buyerCountdown = Array.TrueForAll(_showcaseItems, item => item == -1) ? 0 :
			Mathf.Max(1f, BuyerWaitSeconds);
		_inventoryUI.DeselectAll();
		CurrentDay = CheckpointDay;
		Score = CheckpointScore;
		Accepted = CheckpointAccepted;
		Declined = CheckpointDeclined;
		QuotaStage = CheckpointDay == 1 ? 0 : 1;
		Failed = false;
		Completed = false;
		StatusText = "Возврат к контрольной точке.";
		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		LogStoredItems();
		
		// --- ДОБАВЛЕНО: Обновляем инвентарь после отката ---
		_inventoryUI.UpdateUI(_storedItems);
		RefreshShowcases();
		// --------------------------------------------------
		
		SaveProgress();
	}

	public void NewGame()
	{
		FinishTrade();
		_storedItems.Clear();
		_checkpointItems.Clear();
		Array.Fill(_showcaseItems, -1);
		Array.Fill(_checkpointShowcaseItems, -1);
		_selectedShowcaseIndex = -1;
		_buyerCountdown = 0;
		_inventoryUI.DeselectAll();
		CurrentDay = 1;
		Score = 0;
		Accepted = 0;
		Declined = 0;
		QuotaStage = 0;
		CheckpointDay = 1;
		CheckpointScore = 0;
		CheckpointAccepted = 0;
		CheckpointDeclined = 0;
		Failed = false;
		Completed = false;
		StatusText = "Новая игра.";
		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		LogStoredItems();
		
		// --- ДОБАВЛЕНО: Очищаем инвентарь визуально ---
		_inventoryUI.UpdateUI(_storedItems);
		RefreshShowcases();
		// ----------------------------------------------
		
		SaveProgress();
	}

	private void SaveProgress()
	{
		if (string.IsNullOrEmpty(SavePath)) return;
		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushWarning($"Не удалось сохранить прогресс: {SavePath}");
			return;
		}
		var data = new ProgressData
		{
			SaveVersion = 5, Day = CurrentDay, Score = Score, Accepted = Accepted,
			Declined = Declined, QuotaStage = QuotaStage, CheckpointDay = CheckpointDay,
			CheckpointScore = CheckpointScore, CheckpointAccepted = CheckpointAccepted,
			CheckpointDeclined = CheckpointDeclined, Failed = Failed, Completed = Completed,
			Status = StatusText, TradeActive = TradeActive, ItemIndex = ItemIndex,
			CustomerIndex = CustomerIndex, Percentage = Percentage, RerollsRemaining = RerollsRemaining,
			StoredItems = _storedItems.ToArray(), CheckpointItems = _checkpointItems.ToArray(),
			ShowcaseItems = (int[])_showcaseItems.Clone(),
			CheckpointShowcaseItems = (int[])_checkpointShowcaseItems.Clone(),
			ActiveShowcaseIndex = _activeShowcaseIndex, BuyerCountdown = _buyerCountdown
		};
		file.StoreString(JsonSerializer.Serialize(data, JsonOptions));
	}

	private bool LoadProgress()
	{
		if (string.IsNullOrEmpty(SavePath) || !FileAccess.FileExists(SavePath)) return false;
		ProgressData? data;
		try
		{
			data = JsonSerializer.Deserialize<ProgressData>(FileAccess.GetFileAsString(SavePath), JsonOptions);
		}
		catch (JsonException)
		{
			return false;
		}
		if (data == null) return false;
		_storedItems.Clear();
		_checkpointItems.Clear();
		foreach (int index in data.StoredItems ?? [])
			if (index >= 0 && index < Items.Length) _storedItems.Add(index);
		foreach (int index in data.CheckpointItems ?? [])
			if (index >= 0 && index < Items.Length) _checkpointItems.Add(index);
		for (int i = 0; i < Mathf.Min(ShowcaseCapacity, data.ShowcaseItems?.Length ?? 0); i++)
			if (data.ShowcaseItems![i] >= 0 && data.ShowcaseItems[i] < Items.Length)
				_showcaseItems[i] = data.ShowcaseItems[i];
		for (int i = 0; i < Mathf.Min(ShowcaseCapacity, data.CheckpointShowcaseItems?.Length ?? 0); i++)
			if (data.CheckpointShowcaseItems![i] >= 0 && data.CheckpointShowcaseItems[i] < Items.Length)
				_checkpointShowcaseItems[i] = data.CheckpointShowcaseItems[i];
		_buyerCountdown = Math.Clamp(data.BuyerCountdown, 0, Mathf.Max(1f, BuyerWaitSeconds));
		if (_buyerCountdown <= 0 && Array.Exists(_showcaseItems, item => item >= 0))
			_buyerCountdown = Mathf.Max(1f, BuyerWaitSeconds);
		_activeShowcaseIndex = data.ActiveShowcaseIndex >= 0 && data.ActiveShowcaseIndex < ShowcaseCapacity
			? data.ActiveShowcaseIndex : -1;
		CurrentDay = Mathf.Clamp(data.Day, 1, FinalDay);
		Score = Mathf.Max(0, data.Score);
		Accepted = Mathf.Max(0, data.Accepted);
		Declined = Mathf.Max(0, data.Declined);
		QuotaStage = Mathf.Clamp(data.QuotaStage, 0, 2);
		CheckpointDay = data.CheckpointDay > 1 ? FirstDeadline + 1 : 1;
		CheckpointScore = Mathf.Max(0, data.CheckpointScore);
		CheckpointAccepted = Mathf.Max(0, data.CheckpointAccepted);
		CheckpointDeclined = Mathf.Max(0, data.CheckpointDeclined);
		Failed = data.Failed;
		Completed = data.Completed;
		StatusText = data.Status;
		ItemIndex = data.ItemIndex;
		CustomerIndex = data.CustomerIndex;
		if (data.SaveVersion < 3 && CustomerIndex == 1) CustomerIndex = 3;
		Percentage = Mathf.Clamp(data.Percentage, -30, 30);
		RerollsRemaining = Mathf.Clamp(data.RerollsRemaining, 0, 3);
		if (data.SaveVersion < 2 && ItemIndex >= 0 && ItemIndex < Items.Length)
		{
			int oldLimit = Items[ItemIndex].BasePrice <= 9 ? 3 : 1;
			RerollsRemaining = Mathf.Clamp(RerollsRemaining + 3 - oldLimit, 0, 3);
		}
		TradeActive = data.TradeActive;
		if (ItemIndex < 0 || ItemIndex >= Items.Length || CustomerIndex < 0 ||
			CustomerIndex >= _customer.GetAppearanceCount() || Failed || Completed)
			TradeActive = false;
		if (!TradeActive) _activeShowcaseIndex = -1;
			
		// --- ДОБАВЛЕНО: Загружаем визуал инвентаря при старте игры ---
		if (_inventoryUI != null)
		{
			_inventoryUI.UpdateUI(_storedItems);
		}
		RefreshShowcases();
		// -------------------------------------------------------------
		
		return true;
	}

	private sealed class ProgressData
	{
		public int SaveVersion { get; set; } = 1;
		public int Day { get; set; } = 1;
		public int Score { get; set; }
		public int Accepted { get; set; }
		public int Declined { get; set; }
		public int QuotaStage { get; set; }
		public int CheckpointDay { get; set; } = 1;
		public int CheckpointScore { get; set; }
		public int CheckpointAccepted { get; set; }
		public int CheckpointDeclined { get; set; }
		public bool Failed { get; set; }
		public bool Completed { get; set; }
		public string Status { get; set; } = "Продолжайте торги.";
		public bool TradeActive { get; set; }
		public int ItemIndex { get; set; } = -1;
		public int CustomerIndex { get; set; } = -1;
		public int Percentage { get; set; }
		public int RerollsRemaining { get; set; }
		public int[] StoredItems { get; set; } = [];
		public int[] CheckpointItems { get; set; } = [];
		public int[] ShowcaseItems { get; set; } = [];
		public int[] CheckpointShowcaseItems { get; set; } = [];
		public int ActiveShowcaseIndex { get; set; } = -1;
		public double BuyerCountdown { get; set; }
	}
}
