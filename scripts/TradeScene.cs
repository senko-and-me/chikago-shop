using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public partial class TradeScene : Node2D
{
	[Export(PropertyHint.Range, "1,9999,1")] public int StartingMoney { get; set; } = 220;
	[Export(PropertyHint.Range, "1,9999,1")] public int FirstQuota { get; set; } = 200;
	[Export(PropertyHint.Range, "1,9999,1")] public int SecondQuota { get; set; } = 400;
	[Export] public string SavePath { get; set; } = "user://trade_progress.json";
	private float NextBuyerDelay() => _buyerArrivalRng.RandfRange(0.5f, 2f);

	private const float MaxZoom = 1.8f;
	private const float ZoomStep = 1.12f;
	private const float ZoomSmoothness = 12f;
	private const int FirstDeadline = 4;
	private const int FinalDay = 7;
	private const int ShowcaseCapacity = 6;
	private const int EnergyPerDay = 2;
	private const int AtelierSpinCost = 90;
	private const int MusicSpinCost = 140;
	private const int MusicUnlockCost = 100;
	private const int ItemsPerSpin = 3;
	private static readonly Dictionary<string, int> RerollsByRarity = new(StringComparer.Ordinal)
	{
		["common"] = 3,
		["ordinary"] = 2,
		["first_class"] = 2,
		["exclusive"] = 1,
		["unique"] = 1
	};
	private static readonly (int BasePrice, string RarityId, string Rarity, string Kind, string Name, string ArtPath)[] Items =
	[
		(30, "common", "Ширпотреб", "hat", "шляпка-клош", "res://art/items/hat_cloche.png"),
		(50, "ordinary", "Заурядная", "gloves", "перчатки", "res://art/items/gloves.png"),
		(45, "common", "Ширпотреб", "guitar", "гитара", "res://art/items/guitar.svg"),
		(75, "ordinary", "Заурядная", "trumpet", "труба", "res://art/items/trumpet.svg")
	];
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		WriteIndented = true
	};

	private CounterWorld _world = null!;
	private Sprite2D _roomLayout = null!;
	private Rect2 CameraBounds => _roomLayout.GlobalTransform * _roomLayout.GetRect();
	private float MinZoom => GetViewportRect().Size.X / CameraBounds.Size.X;
	private float ZoomLimit => Mathf.Max(MaxZoom, MinZoom);
	private ModularCustomer _customer = null!;
	private Camera2D _camera = null!;
	private PanelContainer _tradePanel = null!;
	private Label _baseLabel = null!;
	private Label _percentLabel = null!;
	private Label _offerLabel = null!;
	private Button _acceptButton = null!;
	private Button _declineButton = null!;
	private Label _scoreLabel = null!;
	private Label _dayLabel = null!;
	private Label _energyLabel = null!;
	private Label _quotaLabel = null!;
	private Label _deadlineLabel = null!;
	private Label _statusLabel = null!;
	private Button _endDayButton = null!;
	private Button _atelierGetItemButton = null!;
	private Button _musicGetItemButton = null!;
	private Control _musicLockedOverlay = null!;
	private Button _musicUnlockButton = null!;
	private readonly Control[] _rarityPanels = new Control[2];
	private readonly Control[] _rarityHotspots = new Control[2];
	private readonly Control[] _priceHints = new Control[2];
	private readonly Button[] _showcaseSlots = new Button[ShowcaseCapacity];
	private readonly ColorRect[] _showcaseGlass = new ColorRect[ShowcaseCapacity];
	private readonly TextureRect[] _showcaseIcons = new TextureRect[ShowcaseCapacity];
	private readonly Label[] _showcaseLabels = new Label[ShowcaseCapacity];

	// --- ДОБАВЛЕНО: Ссылки на UI инвентаря ---
	private InventoryUI _inventoryUI = null!;
	public int SelectedInventoryIndex { get; set; } = -1; // Индекс выбранного слота (-1 если ничего не выбрано)
	// -----------------------------------------

	private readonly RandomNumberGenerator _rng = new();
	private readonly RandomNumberGenerator _buyerArrivalRng = new();
	private readonly List<int> _storedItems = new();
	private readonly List<int> _checkpointItems = new();
	private readonly int[] _showcaseItems = new int[ShowcaseCapacity];
	private readonly int[] _checkpointShowcaseItems = new int[ShowcaseCapacity];
	private int _activeShowcaseIndex = -1;
	private double _buyerCountdown;
	public int[] StoredItems => _storedItems.ToArray();
	public int[] ShowcaseItems => (int[])_showcaseItems.Clone();
	public double BuyerCountdown => _buyerCountdown;
	public static string GetItemKind(int index) => Items[index].Kind;
	public static string GetItemName(int index) => Items[index].Name;
	public static string GetItemArtPath(int index) => Items[index].ArtPath;
	public static string GetItemRarity(int index) => Items[index].Rarity;
	public static int GetItemRerollLimit(int index) => GetRerollsForRarity(Items[index].RarityId);
	public static int GetRerollsForRarity(string rarityId) => RerollsByRarity[rarityId];
	public int ItemIndex { get; set; } = -1;
	public int CustomerIndex { get; set; } = -1;
	public int Percentage { get; set; }
	public int OfferedPrice { get; private set; }
	public int RerollsRemaining { get; set; }
	public int Score { get; set; }
	public int Accepted { get; private set; }
	public int Declined { get; private set; }
	public int CurrentDay { get; set; } = 1;
	public int EnergyRemaining { get; set; } = EnergyPerDay;
	public int QuotaStage { get; private set; }
	public int CheckpointDay { get; private set; } = 1;
	public int CheckpointScore { get; private set; }
	public int CheckpointAccepted { get; private set; }
	public int CheckpointDeclined { get; private set; }
	public int CheckpointEnergy { get; private set; } = EnergyPerDay;
	public bool MusicWorkshopUnlocked { get; private set; }
	public bool CheckpointMusicWorkshopUnlocked { get; private set; }
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
		_roomLayout = GetNode<Sprite2D>("World/RoomLayout");
		_camera = GetNode<Camera2D>("Camera2D");
		_targetZoom = Mathf.Clamp(_camera.Zoom.X, MinZoom, ZoomLimit);
		_camera.Zoom = Vector2.One * _targetZoom;
		_zoomAnchor = GetViewportRect().Size / 2f;
		_customer = GetNode<ModularCustomer>("World/TradingRoom/Customer");
		_tradePanel = GetNode<PanelContainer>("World/TradingRoom/Item/TradePanel");
		_baseLabel = GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/BasePriceLabel");
		_percentLabel = GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/PercentLabel");
		_offerLabel = GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/OfferLabel");
		_acceptButton = GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
		_declineButton = GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton");
		_scoreLabel = GetNode<Label>("HUD/Root/ScorePanel/ScoreLabel");
		_dayLabel = GetNode<Label>("HUD/Root/ProgressPanel/Content/DayLabel");
		_energyLabel = GetNode<Label>("HUD/Root/EnergyPanel/EnergyLabel");
		_quotaLabel = GetNode<Label>("HUD/Root/ProgressPanel/Content/QuotaLabel");
		_deadlineLabel = GetNode<Label>("HUD/Root/ProgressPanel/Content/DeadlineLabel");
		_statusLabel = GetNode<Label>("HUD/Root/ProgressPanel/Content/StatusLabel");
		_endDayButton = GetNode<Button>("HUD/Root/ProgressPanel/Content/EndDayButton");
		_atelierGetItemButton = GetNode<Button>("World/Atelier/GetItemButton");
		_musicGetItemButton = GetNode<Button>("World/MusicWorkshop/GetItemButton");
		EnsureMusicWorkshopUi();
		_rarityPanels[0] = GetNode<Control>("World/Atelier/RarityPanel");
		_rarityPanels[1] = GetNode<Control>("World/MusicWorkshop/RarityPanel");
		_rarityHotspots[0] = GetNode<Control>("World/Atelier/GetItemButton/RarityHotspot");
		_rarityHotspots[1] = GetNode<Control>("World/MusicWorkshop/GetItemButton/RarityHotspot");
		_priceHints[0] = GetNode<Control>("World/Atelier/GetItemButton/PriceHint");
		_priceHints[1] = GetNode<Control>("World/MusicWorkshop/GetItemButton/PriceHint");
		Array.Fill(_showcaseItems, -1);
		Array.Fill(_checkpointShowcaseItems, -1);
		NormalizeLeftShowcaseNames();
		string[] showcasePaths =
		[
			"World/ShowroomLeft/Showcases/Slot1",
			"World/ShowroomLeft/Showcases/Slot2",
			"World/ShowroomLeft/Showcases/Slot3",
			"World/Showroom/Showcases/Slot4",
			"World/Showroom/Showcases/Slot5",
			"World/Showroom/Showcases/Slot6"
		];
		for (int i = 0; i < ShowcaseCapacity; i++)
		{
			int slotIndex = i;
			_showcaseSlots[i] = GetNode<Button>(showcasePaths[i]);
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
		_buyerArrivalRng.Randomize();
		_acceptButton.Pressed += AcceptTrade;
		_declineButton.Pressed += RerollCustomer;
		_endDayButton.Pressed += EndDay;
		_atelierGetItemButton.Pressed += GetItemFromAtelier;
		_musicGetItemButton.Pressed += GetItemFromMusicWorkshop;
		_musicUnlockButton.Pressed += TryUnlockMusicWorkshop;
		_atelierGetItemButton.MouseEntered += () => _priceHints[0].Show();
		_atelierGetItemButton.MouseExited += () => _priceHints[0].Hide();
		_musicGetItemButton.MouseEntered += () => _priceHints[1].Show();
		_musicGetItemButton.MouseExited += () => _priceHints[1].Hide();
		_rarityHotspots[0].MouseEntered += () => _rarityPanels[0].Show();
		_rarityHotspots[0].MouseExited += () => _rarityPanels[0].Hide();
		_rarityHotspots[1].MouseEntered += () => _rarityPanels[1].Show();
		_rarityHotspots[1].MouseExited += () => _rarityPanels[1].Hide();
		GetNode<Button>("HUD/Root/ProgressPanel/Content/CheckpointButton").Pressed += ReturnToCheckpoint;
		GetNode<Button>("HUD/Root/ProgressPanel/Content/NewGameButton").Pressed += NewGame;

		Score = StartingMoney;
		CheckpointScore = StartingMoney;
		EnergyRemaining = EnergyPerDay;
		CheckpointEnergy = EnergyPerDay;
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

		GetViewport().SizeChanged += ClampCamera;
		ClampCamera();
	}

	private void NormalizeLeftShowcaseNames()
	{
		Node showcases = GetNode("World/ShowroomLeft/Showcases");
		string[] editorNames = ["ShowcaseSlot", "ShowcaseSlot2", "ShowcaseSlot3"];
		for (int index = 0; index < editorNames.Length; index++)
		{
			string runtimeName = $"Slot{index + 1}";
			if (!showcases.HasNode(runtimeName) && showcases.HasNode(editorNames[index]))
				showcases.GetNode(editorNames[index]).Name = runtimeName;
		}
	}

	private void EnsureMusicWorkshopUi()
	{
		Node2D workshop = GetNode<Node2D>("World/MusicWorkshop");
		workshop.GetNode<Label>("HintLabel").Text = "Гитара или труба  •  140$ за 3";
		workshop.GetNode<Label>("GetItemButton/PriceHint/Label").Text = "3 предмета  •  140$";

		if (!workshop.HasNode("LockedOverlay"))
		{
			ColorRect overlay = new()
			{
				Name = "LockedOverlay",
				Position = Vector2.Zero,
				Size = new Vector2(1920f, 1080f),
				Color = new Color(0.015f, 0.015f, 0.015f, 0.97f),
				MouseFilter = Control.MouseFilterEnum.Stop,
				ZIndex = 100
			};
			workshop.AddChild(overlay);

			Label title = new()
			{
				Name = "LockTitle",
				Position = new Vector2(510f, 330f),
				Size = new Vector2(900f, 110f),
				Text = "КОМНАТА ЗАКРЫТА",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			title.AddThemeColorOverride("font_color", Colors.White);
			title.AddThemeFontSizeOverride("font_size", 52);
			overlay.AddChild(title);

			Button unlockButton = new()
			{
				Name = "UnlockButton",
				Position = new Vector2(660f, 470f),
				Size = new Vector2(600f, 170f),
				Text = "ОТКРЫТЬ ЗА 100$",
				TooltipText = "Открыть музыкальную комнату за 100$",
				MouseDefaultCursorShape = Control.CursorShape.PointingHand
			};
			unlockButton.AddThemeColorOverride("font_color", Colors.White);
			unlockButton.AddThemeFontSizeOverride("font_size", 38);
			overlay.AddChild(unlockButton);
		}

		_musicLockedOverlay = workshop.GetNode<Control>("LockedOverlay");
		_musicUnlockButton = workshop.GetNode<Button>("LockedOverlay/UnlockButton");
	}

	public override void _Process(double delta)
	{
		TickBuyers(delta);
		ClampCamera();
		_targetZoom = Mathf.Clamp(float.IsFinite(_targetZoom) ? _targetZoom : _camera.Zoom.X,
			MinZoom, ZoomLimit);
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
		float newTarget = Mathf.Clamp(_targetZoom * factor, MinZoom, ZoomLimit);
		if (Mathf.IsEqualApprox(newTarget, _targetZoom)) return;
		_targetZoom = newTarget;
		_zoomAnchor = mousePosition;
	}

	public void ClampCamera()
	{
		Rect2 bounds = CameraBounds;
		float zoom = Mathf.Clamp(_camera.Zoom.X, MinZoom, ZoomLimit);
		_camera.Zoom = Vector2.One * zoom;
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

	public void GetItemFromAtelier() => AcquireItem("Ателье", 0, 1, AtelierSpinCost);

	public void GetItemFromMusicWorkshop()
	{
		if (!MusicWorkshopUnlocked) return;
		AcquireItem("Муз. штуки", 2, 3, MusicSpinCost);
	}

	public void TryUnlockMusicWorkshop()
	{
		if (MusicWorkshopUnlocked || Failed || Completed || Score < MusicUnlockCost) return;
		Score -= MusicUnlockCost;
		MusicWorkshopUnlocked = true;
		StatusText = "Музыкальная мастерская открыта.";
		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		SaveProgress();
	}

	private void AcquireItem(string workshop, int firstIndex, int lastIndex, int cost)
	{
		if (Failed || Completed || EnergyRemaining <= 0 || Score < cost) return;

		Score -= cost;
		EnergyRemaining--;
		for (int itemNumber = 0; itemNumber < ItemsPerSpin; itemNumber++)
		{
			int index = _rng.RandiRange(firstIndex, lastIndex);
			_storedItems.Add(index);
			GD.Print($"{workshop}: получен предмет {Items[index].Name} (индекс {index}).");
		}
		LogStoredItems();
		RefreshScore();
		RefreshProgress();
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

	public void PlaceItemInShowcase(int slotIndex)
	{
		if (Failed || Completed || slotIndex < 0 || slotIndex >= ShowcaseCapacity ||
			slotIndex == _activeShowcaseIndex ||
			_showcaseItems[slotIndex] != -1 || SelectedInventoryIndex < 0 ||
			SelectedInventoryIndex >= _storedItems.Count) return;

		bool firstItem = Array.TrueForAll(_showcaseItems, item => item == -1);
		_showcaseItems[slotIndex] = _storedItems[SelectedInventoryIndex];
		_storedItems.RemoveAt(SelectedInventoryIndex);
		_inventoryUI.DeselectAll();
		_inventoryUI.UpdateUI(_storedItems);
		if (firstItem && !TradeActive) _buyerCountdown = NextBuyerDelay();
		RefreshItemButtons();
		LogStoredItems();
		SaveProgress();
	}

	public void SelectInventoryItem(int index)
	{
		SelectedInventoryIndex = index;
		RefreshItemButtons();
	}

	private void OnShowcasePressed(int slotIndex)
	{
		PlaceItemInShowcase(slotIndex);
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
		ItemIndex = _showcaseItems[_activeShowcaseIndex];
		_showcaseItems[_activeShowcaseIndex] = -1;
		_buyerCountdown = 0;
		RefreshShowcases();
		StartBuyerOffer();
	}

	private void StartBuyerOffer()
	{
		Percentage = _rng.RandiRange(-30, 30);
		RerollsRemaining = GetItemRerollLimit(ItemIndex);
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
		_atelierGetItemButton.Disabled = Failed || Completed || EnergyRemaining <= 0 || Score < AtelierSpinCost;
		_musicGetItemButton.Disabled = !MusicWorkshopUnlocked || Failed || Completed ||
			EnergyRemaining <= 0 || Score < MusicSpinCost;
		_musicLockedOverlay.Visible = !MusicWorkshopUnlocked;
		_musicUnlockButton.Disabled = Failed || Completed || Score < MusicUnlockCost;
		RefreshShowcases();
	}

	private void RefreshShowcases()
	{
		for (int i = 0; i < ShowcaseCapacity; i++)
		{
			int itemIndex = _showcaseItems[i];
			bool reserved = TradeActive && i == _activeShowcaseIndex;
			_showcaseIcons[i].Texture = reserved || itemIndex < 0 ? null :
				GD.Load<Texture2D>(Items[itemIndex].ArtPath);
			_showcaseLabels[i].Text = reserved ? "НА СДЕЛКЕ" :
				itemIndex < 0 ? "СВОБОДНО" : Items[itemIndex].Name.ToUpperInvariant();
			_showcaseGlass[i].Color = reserved ? new Color(0.7f, 0.68f, 0.68f, 0.78f) :
				new Color(0.77f, 0.9f, 0.88f, 0.58f);
			_showcaseSlots[i].Disabled = Failed || Completed || reserved ||
				(itemIndex < 0 && (SelectedInventoryIndex < 0 || SelectedInventoryIndex >= _storedItems.Count));
		}
	}

	private void RefreshOffer()
	{
		int basePrice = Items[ItemIndex].BasePrice;
		OfferedPrice = PriceForPercent(basePrice, Percentage);
		_baseLabel.Text = $"Обычно: {basePrice}$";
		_percentLabel.Text = $"К цене: {(Percentage >= 0 ? "+" : "")}{Percentage}%";
		_offerLabel.Text = $"Получим: {OfferedPrice}$";
		_declineButton.Text = $"× ({RerollsRemaining})";
		_declineButton.Disabled = RerollsRemaining <= 0;
	}

	public static int PriceForPercent(int basePrice, int value) =>
		Mathf.Max(1, Mathf.RoundToInt(basePrice * (1f + value / 100f)));

	public void RerollCustomer()
	{
		if (!TradeActive || RerollsRemaining <= 0) return;
		RerollsRemaining--;
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

	public void DeclineTrade() => RerollCustomer();

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
			_buyerCountdown = Array.Exists(_showcaseItems, item => item >= 0) ? NextBuyerDelay() : 0;
		_world.ItemVisible = false;
		_customer.Visible = false;
		_tradePanel.Visible = false;
		_acceptButton.Disabled = true;
		_declineButton.Disabled = true;
		RefreshItemButtons();
	}

	private void RefreshScore() => _scoreLabel.Text = $"Счёт: {Score}$";

	private void RefreshProgress()
	{
		_dayLabel.Text = $"День: {CurrentDay} / {FinalDay}";
		_energyLabel.Text = $"Энергия: {EnergyRemaining} / {EnergyPerDay}";
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
				EnergyRemaining = EnergyPerDay;
				CheckpointEnergy = EnergyRemaining;
				CheckpointMusicWorkshopUnlocked = MusicWorkshopUnlocked;
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
			EnergyRemaining = EnergyPerDay;
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
		_buyerCountdown = Array.Exists(_showcaseItems, item => item >= 0) ? NextBuyerDelay() : 0;
		_inventoryUI.DeselectAll();
		CurrentDay = CheckpointDay;
		Score = CheckpointScore;
		EnergyRemaining = CheckpointEnergy;
		MusicWorkshopUnlocked = CheckpointMusicWorkshopUnlocked;
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
		_buyerCountdown = 0;
		_inventoryUI.DeselectAll();
		CurrentDay = 1;
		Score = StartingMoney;
		EnergyRemaining = EnergyPerDay;
		Accepted = 0;
		Declined = 0;
		QuotaStage = 0;
		CheckpointDay = 1;
		CheckpointScore = StartingMoney;
		CheckpointEnergy = EnergyPerDay;
		MusicWorkshopUnlocked = false;
		CheckpointMusicWorkshopUnlocked = false;
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
			SaveVersion = 7, Day = CurrentDay, Score = Score, EnergyRemaining = EnergyRemaining,
			Accepted = Accepted,
			Declined = Declined, QuotaStage = QuotaStage, CheckpointDay = CheckpointDay,
			CheckpointScore = CheckpointScore, CheckpointEnergy = CheckpointEnergy,
			MusicWorkshopUnlocked = MusicWorkshopUnlocked,
			CheckpointMusicWorkshopUnlocked = CheckpointMusicWorkshopUnlocked,
			CheckpointAccepted = CheckpointAccepted,
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
		ProgressData data;
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
		_buyerCountdown = Array.Exists(_showcaseItems, item => item >= 0)
			? double.IsFinite(data.BuyerCountdown) && data.BuyerCountdown > 0 && data.BuyerCountdown <= 2
				? data.BuyerCountdown : NextBuyerDelay()
			: 0;
		_activeShowcaseIndex = data.ActiveShowcaseIndex >= 0 && data.ActiveShowcaseIndex < ShowcaseCapacity
			? data.ActiveShowcaseIndex : -1;
		CurrentDay = Mathf.Clamp(data.Day, 1, FinalDay);
		Score = Mathf.Max(0, data.Score);
		EnergyRemaining = data.SaveVersion < 6 ? EnergyPerDay : Mathf.Clamp(data.EnergyRemaining, 0, EnergyPerDay);
		Accepted = Mathf.Max(0, data.Accepted);
		Declined = Mathf.Max(0, data.Declined);
		QuotaStage = Mathf.Clamp(data.QuotaStage, 0, 2);
		CheckpointDay = data.CheckpointDay > 1 ? FirstDeadline + 1 : 1;
		CheckpointScore = Mathf.Max(0, data.CheckpointScore);
		CheckpointEnergy = data.SaveVersion < 6 ? EnergyPerDay : Mathf.Clamp(data.CheckpointEnergy, 0, EnergyPerDay);
		MusicWorkshopUnlocked = data.SaveVersion >= 7 && data.MusicWorkshopUnlocked;
		CheckpointMusicWorkshopUnlocked = data.SaveVersion >= 7 && data.CheckpointMusicWorkshopUnlocked;
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
			RerollsRemaining = GetItemRerollLimit(ItemIndex);
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
		public int EnergyRemaining { get; set; } = EnergyPerDay;
		public int Accepted { get; set; }
		public int Declined { get; set; }
		public int QuotaStage { get; set; }
		public int CheckpointDay { get; set; } = 1;
		public int CheckpointScore { get; set; }
		public int CheckpointEnergy { get; set; } = EnergyPerDay;
		public bool MusicWorkshopUnlocked { get; set; }
		public bool CheckpointMusicWorkshopUnlocked { get; set; }
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
