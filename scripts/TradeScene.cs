
using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Trading;
using ChikagoShop.Domain.Economy;
using Godot;

public partial class TradeScene : Node2D
{
	public event Action BuyerArrived;
	public event Action BuyerOfferChanged;
	private static ChikagoShop.Domain.Configuration.EconomyConfig Economy => EconomyConfigProvider.Load();
	[Export(PropertyHint.Range, "1,9999,1")]
	public int StartingMoney { get; set; } = Economy.StartingMoney;

	[Export(PropertyHint.Range, "1,9999,1")]
	public int FirstQuota { get; set; } = Economy.Quotas.Single(entry => entry.Id == "first").Amount;

	[Export(PropertyHint.Range, "1,9999,1")]
	public int SecondQuota { get; set; } = Economy.Quotas.Single(entry => entry.Id == "final").Amount;

	[Export]
	public string SavePath { get; set; } = "user://trade_progress.json";
	public bool LoadedSavedGame { get; private set; }
	public event Action NewGameStarted;

	private static int FirstDeadline => Economy.Quotas.Single(entry => entry.Id == "first").DeadlineDay;
	private static int FinalDay => Economy.Days.TotalDays;
	private const int ShowcaseCapacity = 6;
	private const int InitialShowcaseCapacity = 3;
	private static int EnergyBeforeMusicUnlock => Economy.Days.EnergyBeforeMusicUnlock;
	private static int EnergyAfterMusicUnlock => Economy.Days.EnergyAfterMusicUnlock;
	private static int AtelierSpinCost => WorkshopItemCatalog.CraftCost("atelier");
	private static int MusicSpinCost => WorkshopItemCatalog.CraftCost("music");
	private static int MusicUnlockCost => Economy.Workshops.Single(entry => entry.Id == "music").UnlockCost;
	private static int ItemsPerSpin => Economy.Days.ItemsPerCraft;

	private static IReadOnlyList<CraftItem> Items =>
		WorkshopItemCatalog.Items;

	private CounterWorld _world = null!;
	private ModularCustomer _customer = null!;
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
	private RoomCurtainPresentation _musicLockedOverlay = null!;
	private MusicWorkshopUnlockPanel _musicUnlockPanel = null!;

	private readonly Control[] _rarityPanels = new Control[2];
	private readonly Control[] _rarityHotspots = new Control[2];
	private readonly Control[] _priceHints = new Control[2];
	private readonly Button[] _showcaseSlots = new Button[ShowcaseCapacity];
	private readonly ColorRect[] _showcaseGlass = new ColorRect[ShowcaseCapacity];
	private readonly TextureRect[] _showcaseIcons = new TextureRect[ShowcaseCapacity];
	private readonly Label[] _showcaseLabels = new Label[ShowcaseCapacity];

	private Label _extraShowroomHint = null!;
	private RoomCurtainPresentation _rightShowroomOverlay = null!;
	private InventoryUI _inventoryUI = null!;

	private readonly InvisibleHandDirector _invisibleHand = new();
	private readonly RandomNumberGenerator _rng = new();
	private readonly List<int> _storedItems = new();
	private readonly List<int> _checkpointItems = new();
	private readonly int[] _showcaseItems = new int[ShowcaseCapacity];
	private readonly int[] _checkpointShowcaseItems =
		new int[ShowcaseCapacity];

	private int _activeShowcaseIndex = -1;
	private DayCycleController _dayCycle = null!;
	private EmbroideryGame _embroidery = null!;
	private EmbroideryGame _polishing = null!;
	private int _energyRemaining = EnergyBeforeMusicUnlock;
	private bool _localizationReady;

	public void RecalculateInvisibleHand() => ObserveInvisibleHand(_invisibleHand);

	private void ObserveInvisibleHand(InvisibleHandDirector director)
	{
		if (_dayCycle == null || Failed || Completed) return;
		int stageStartDay = QuotaStage == 0 ? 1 : FirstDeadline + 1;
		int stageLastDay = QuotaStage == 0 ? FirstDeadline : FinalDay;
		int quota = QuotaStage == 0 ? FirstQuota : SecondQuota;
		double unsold = 0;
		foreach (int item in _storedItems) unsold += GetItemPrice(item);
		foreach (int item in _showcaseItems) if (item >= 0) unsold += GetItemPrice(item);
		if (ItemIndex >= 0 && (TradeActive || _activeShowcaseIndex >= 0))
			unsold += GetItemPrice(ItemIndex);
		double elapsed = _dayCycle.Session.Duration <= 0 ? 0 :
			1 - _dayCycle.Session.RemainingSeconds / _dayCycle.Session.Duration;
		director.Observe(Score, unsold, CheckpointScore,
			CurrentDay, stageStartDay, stageLastDay, elapsed, quota,
			DebugBalanceSettings.Number("hand.enabled") == 1);
	}

	public IReadOnlyDictionary<string, int> PreviewRarityWeights(string workshop)
	{
		// Preview the next roll without advancing the live director.
		var preview = new InvisibleHandDirector();
		preview.Restore(_invisibleHand.Pressure);
		ObserveInvisibleHand(preview);
		return WorkshopItemCatalog.RarityWeights(workshop, DayUpgrades, CraftRarityShift(workshop, preview));
	}

	private int CraftRarityShift(string workshop, InvisibleHandDirector director) =>
		director.RarityShift(WorkshopItemCatalog.RarityWeightTotal(workshop, DayUpgrades),
			DebugBalanceSettings.Number("hand.rarity"));

	public void AcknowledgeMafiaNotice()
	{
		MafiaNoticePending = false;
		SaveProgress();
	}

	public int SelectedInventoryIndex { get; set; } = -1;
	public bool CraftMiniGameOpen =>
		(_embroidery?.IsOpen ?? false) ||
		(_polishing?.IsOpen ?? false);

	public bool DayClosed => _dayCycle.Session.IsClosed;
	public bool DayStarted => _dayCycle.Started;
	public bool RequireStartedDay() => _dayCycle.RequireStartedDay();
	public bool DayTransitionOpen =>
		(_dayCycle?.TransitionOpen ?? false) || _quotaDialogue != null;

	public ChikagoShop.Domain.Days.DayUpgradeSession DayUpgrades =>
		_dayCycle?.Upgrades;

	public double DayRemainingSeconds =>
		_dayCycle.Session.RemainingSeconds;

	public int[] StoredItems => _storedItems.ToArray();
	public int[] ShowcaseItems => (int[])_showcaseItems.Clone();

	public static string GetItemKind(int index) => Items[index].Kind;
	public static string GetItemName(int index) =>
		GameLocalization.Text(Items[index].Name);
	public static string GetItemArtPath(int index) => Items[index].ArtPath;
	public static string GetItemRarity(int index) =>
		GameLocalization.Text(Items[index].Rarity);
	public static int GetItemBasePrice(int index) => Items[index].BasePrice;

	public int GetItemPrice(int index) =>
		DayUpgrades.Price(
			Items[index].WorkshopId,
			Items[index].BasePrice);

	public static int GetItemRerollLimit(int index) =>
		Items[index].Rerolls;

	public static int GetRerollsForRarity(string rarityId) =>
		WorkshopItemCatalog.RerollsFor(rarityId);

	public int ItemIndex { get; set; } = -1;
	public int CustomerIndex { get; set; } = -1;
	public int Percentage { get; set; }
	public int OfferedPrice { get; private set; }
	public int RerollsRemaining { get; set; }
	public int Score { get; set; }
	public int Accepted { get; private set; }
	public int Declined { get; private set; }
	public int CurrentDay { get; set; } = 1;

	public int EnergyCapacity =>
		_dayCycle?.Upgrades.EnergyCapacityFor(MusicWorkshopUnlocked
			? EnergyAfterMusicUnlock
			: EnergyBeforeMusicUnlock) ?? EnergyBeforeMusicUnlock;

	public int InventorySlotCapacity =>
		MusicWorkshopUnlocked ? 6 : 3;

	public int EnergyRemaining
	{
		get => Mathf.Clamp(
			_dayCycle?.Session.AvailableEnergy(_energyRemaining)
				?? _energyRemaining,
			0,
			EnergyCapacity);
		set => _energyRemaining =
			Mathf.Clamp(value, 0, EnergyCapacity);
	}

	public bool MafiaNoticePending { get; private set; }
	public int QuotaStage { get; private set; }
	public int CheckpointDay { get; private set; } = 1;
	public int CheckpointScore { get; private set; }
	private double _checkpointInvisibleHandPressure;
	public int CheckpointAccepted { get; private set; }
	public int CheckpointDeclined { get; private set; }

	public int CheckpointEnergy { get; private set; } =
		EnergyBeforeMusicUnlock;

	public bool MusicWorkshopUnlocked { get; private set; }
	public bool CheckpointMusicWorkshopUnlocked { get; private set; }
	public bool Failed { get; private set; }
	public bool Completed { get; private set; }
	public bool TradeActive { get; set; }

	public string StatusText { get; private set; } =
		"Торгуйте и завершайте день.";

	public override void _Ready()
	{
		SceneConfigurationDefaults.Apply(this, Economy);
		_world = GetNode<CounterWorld>("World");
		_customer = GetNode<ModularCustomer>(
			"World/TradingRoom/Customer");

		_tradePanel = GetNode<PanelContainer>(
			"World/TradingRoom/Item/TradePanel");

		_baseLabel = GetNode<Label>(
			"World/TradingRoom/Item/TradePanel/Content/BasePriceLabel");

		_percentLabel = GetNode<Label>(
			"World/TradingRoom/Item/TradePanel/Content/PercentLabel");

		_offerLabel = GetNode<Label>(
			"World/TradingRoom/Item/TradePanel/Content/OfferLabel");

		_acceptButton = GetNode<Button>(
			"World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");

		_declineButton = GetNode<Button>(
			"World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton");

		_scoreLabel = GetNode<Label>(
			"HUD/Root/ScorePanel/ScoreLabel");

		_dayLabel = GetNode<Label>(
			"HUD/Root/ProgressPanel/Content/DayLabel");

		_energyLabel = GetNode<Label>(
			"HUD/Root/EnergyPanel/EnergyLabel");

		_quotaLabel = GetNode<Label>(
			"HUD/Root/ProgressPanel/Content/QuotaLabel");

		_deadlineLabel = GetNode<Label>(
			"HUD/Root/ProgressPanel/Content/DeadlineLabel");

		_statusLabel = GetNode<Label>(
			"HUD/Root/ProgressPanel/Content/StatusLabel");

		_endDayButton = GetNode<Button>(
			"HUD/Root/ProgressPanel/Content/EndDayButton");

		_atelierGetItemButton = GetNode<Button>(
			"World/Atelier/GetItemButton");

		_musicGetItemButton = GetNode<Button>(
			"World/MusicWorkshop/GetItemButton");

		BindMusicWorkshopUi();

		_rarityPanels[0] = GetNode<Control>(
			"World/Atelier/RarityPanel");

		_rarityPanels[1] = GetNode<Control>(
			"World/MusicWorkshop/RarityPanel");

		_rarityHotspots[0] = GetNode<Control>(
			"World/Atelier/GetItemButton/RarityHotspot");

		_rarityHotspots[1] = GetNode<Control>(
			"World/MusicWorkshop/GetItemButton/RarityHotspot");

		_priceHints[0] = GetNode<Control>(
			"World/Atelier/GetItemButton/PriceHint");

		_priceHints[1] = GetNode<Control>(
			"World/MusicWorkshop/GetItemButton/PriceHint");

		Array.Fill(_showcaseItems, -1);
		Array.Fill(_showcaseGrades, "D");
		Array.Fill(_checkpointShowcaseGrades, "D");
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
			_showcaseGlass[i] =
				_showcaseSlots[i].GetNode<ColorRect>("Glass");
			_showcaseIcons[i] =
				_showcaseSlots[i].GetNode<TextureRect>("Icon");
			_showcaseLabels[i] =
				_showcaseSlots[i].GetNode<Label>("ItemLabel");

			_showcaseSlots[i].Pressed +=
				() => OnShowcasePressed(slotIndex);
		}

		_extraShowroomHint = GetNode<Label>(
			"World/Showroom/ShowroomHint");

		BindExtraShowroomOverlay();

		_inventoryUI = GetNode<InventoryUI>("HUD/InventoryUI");
		_inventoryUI.Initialize(this);
		InitializeBuyerWaitWidget();

		_rng.Randomize();

		_acceptButton.Pressed += AcceptTrade;
		_declineButton.Pressed += RerollCustomer;
		_endDayButton.Pressed += EndDay;
		_atelierGetItemButton.Pressed += GetItemFromAtelier;
		_musicGetItemButton.Pressed += GetItemFromMusicWorkshop;
		_musicUnlockPanel.UnlockButton.Pressed += TryUnlockMusicWorkshop;

		for (int i = 0; i < _rarityHotspots.Length; i++)
		{
			int index = i;

			Button button = i == 0
				? _atelierGetItemButton
				: _musicGetItemButton;

			button.MouseEntered += () =>
				_priceHints[index].Visible =
					!_rarityPanels[index].Visible;

			button.MouseExited += () =>
				_priceHints[index].Hide();

			_rarityHotspots[index].MouseEntered += () =>
			{
				_priceHints[index].Hide();
				_rarityPanels[index].Show();
			};

			_rarityHotspots[index].MouseExited += () =>
			{
				_rarityPanels[index].Hide();
				_priceHints[index].Visible = button.IsHovered();
			};
		}

		GetNode<Button>(
            "HUD/Root/ProgressPanel/Content/CheckpointButton"
		).Pressed += ReturnToCheckpoint;

		GetNode<Button>(
            "HUD/Root/ProgressPanel/Content/NewGameButton"
		).Pressed += NewGame;

		Score = StartingMoney;
		CheckpointScore = StartingMoney;
		EnergyRemaining = EnergyCapacity;
		CheckpointEnergy = EnergyCapacity;

		_dayCycle = new DayCycleController(this);
		InitializeQuotaVisit();

		_embroidery = new EmbroideryGame(
			() => AcquireItem("atelier", AtelierSpinCost));

		AddChild(_embroidery);

		_polishing = new EmbroideryGame(
			() => AcquireItem("music", MusicSpinCost, _polishing.PreparedItemIndex),
			polishing: true);

		AddChild(_polishing);
		DebugHotkeysController.Attach(this, _dayCycle);

		bool restored = LoadProgress();
		LoadedSavedGame = restored;

		RefreshScore();
		RefreshProgress();

		if (restored)
		{
			if (TradeActive) ShowTrade();
			else if (_waitingBuyer && _dayCycle.CanReceiveBuyers) ShowWaitingBuyer();
			else
			{
				FinishTrade();
				SynchronizeDayState();
			}
		}
		else FinishTrade();

		RefreshItemButtons();
		LogStoredItems();
		_localizationReady = true;
		_dayCycle.ResumeUpgradeChoice();
	}

	public override void _Notification(int what)
	{
		if (what != NotificationTranslationChanged ||
			!_localizationReady)
			return;

		RefreshScore();
		RefreshProgress();
		RefreshCraftLabels();
		RefreshShowcases();

		if (TradeActive)
			RefreshOffer();

		_inventoryUI.UpdateUI(_storedItems);
	}

	private void NormalizeLeftShowcaseNames()
	{
		Node showcases =
			GetNode("World/ShowroomLeft/Showcases");

		string[] editorNames =
		[
			"ShowcaseSlot",
			"ShowcaseSlot2",
            "ShowcaseSlot3"
		];

		for (int index = 0; index < editorNames.Length; index++)
		{
			string runtimeName = $"Slot{index + 1}";

			if (!showcases.HasNode(runtimeName) &&
				showcases.HasNode(editorNames[index]))
			{
				showcases.GetNode(editorNames[index]).Name =
					runtimeName;
			}
		}
	}

	private void BindMusicWorkshopUi()
	{
		Node2D workshop =
			GetNode<Node2D>("World/MusicWorkshop");

		RefreshCraftLabels();

		_musicLockedOverlay =
			workshop.GetNode<RoomCurtainPresentation>("LockedOverlay");

		_musicUnlockPanel = workshop.GetNode<MusicWorkshopUnlockPanel>("LockedOverlay/UnlockPanel");
	}

	private void RefreshCraftLabels()
	{
		bool english = GameLocalization.Language == "en";

		Label atelierHint = GetNode<Label>(
			"World/Atelier/HintLabel");

		Label musicHint = GetNode<Label>(
			"World/MusicWorkshop/HintLabel");

		Label atelierPrice = GetNode<Label>(
			"World/Atelier/GetItemButton/PriceHint/Label");

		Label musicPrice = GetNode<Label>(
			"World/MusicWorkshop/GetItemButton/PriceHint/Label");

		atelierHint.AutoTranslateMode =
			Node.AutoTranslateModeEnum.Disabled;
		musicHint.AutoTranslateMode =
			Node.AutoTranslateModeEnum.Disabled;
		atelierPrice.AutoTranslateMode =
			Node.AutoTranslateModeEnum.Disabled;
		musicPrice.AutoTranslateMode =
			Node.AutoTranslateModeEnum.Disabled;

		_atelierGetItemButton.AutoTranslateMode =
			Node.AutoTranslateModeEnum.Disabled;
		_musicGetItemButton.AutoTranslateMode =
			Node.AutoTranslateModeEnum.Disabled;

		atelierHint.Text = english
			? $"Clothing  •  ${AtelierSpinCost} + 1 energy per item"
			: $"Одежда  •  {AtelierSpinCost}$ + 1 энергия за предмет";

		musicHint.Text = english
			? $"Instruments  •  ${MusicSpinCost} + 1 energy per item"
			: $"Инструменты  •  {MusicSpinCost}$ + 1 энергия за предмет";

		atelierPrice.Text = english
			? $"{ItemsPerSpin} {(ItemsPerSpin == 1 ? "item" : "items")}  •  ${AtelierSpinCost} + 1 energy"
			: $"{ItemsPerSpin} предмет  •  {AtelierSpinCost}$ + 1 энергия";

		musicPrice.Text = english
			? $"{ItemsPerSpin} {(ItemsPerSpin == 1 ? "item" : "items")}  •  ${MusicSpinCost} + 1 energy"
			: $"{ItemsPerSpin} предмет  •  {MusicSpinCost}$ + 1 энергия";

		CraftButtonPresentation.Apply(_atelierGetItemButton, english);
		CraftButtonPresentation.Apply(_musicGetItemButton, english);
	}

	private void BindExtraShowroomOverlay()
	{
		_rightShowroomOverlay = GetNode<RoomCurtainPresentation>("World/Showroom/LockedOverlay");
	}
	public override void _Process(double delta)
	{
		if (GetTree().Paused)
			return;

		_dayCycle.Tick(delta);
		TickBuyerWait(delta);
		RefreshBuyers();
	}

	public override void _Input(InputEvent @event)
	{
		if (GetTree().Paused)
			return;

		if (@event is InputEventKey
			{ Pressed: true, Echo: false } key &&
			key.Keycode == Key.F)
		{
			RerollCustomer();
			GetViewport().SetInputAsHandled();
		}
	}

	public void GetItemFromAtelier()
	{
		if (!RequireStartedDay()) return;
		_embroidery.Open();
	}

	public void GetItemFromMusicWorkshop()
	{
		if (!RequireStartedDay()) return;
		if (!MusicWorkshopUnlocked)
			return;

		_polishing.Open();
	}

	public void TryUnlockMusicWorkshop()
	{
		if (!RequireStartedDay()) return;
		if (MusicWorkshopUnlocked ||
			Failed ||
			Completed ||
			Score < MusicUnlockCost)
			return;

		int availableEnergy = EnergyRemaining;

		Score -= MusicUnlockCost;
		MusicWorkshopUnlocked = true;

		EnergyRemaining = availableEnergy +
			EnergyAfterMusicUnlock -
			EnergyBeforeMusicUnlock;

		_dayCycle.SynchronizeRoomDuration();
		RecalculateInvisibleHand();

		StatusText = "Музыкальная мастерская открыта.";

		_inventoryUI.UpdateUI(_storedItems);

		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		SaveProgress();
	}

    // The polishing mini-game reveals the instrument before acquisition;
    // roll that prepared item through the same hidden rarity adjustment.
    public (int ItemIndex, string InstrumentKind) PreparePolishingItem()
    {
        RecalculateInvisibleHand();
        int hiddenShift = CraftRarityShift("music", _invisibleHand);
        return WorkshopItemCatalog.PreparePolishing(
            GD.RandRange(0, WorkshopItemCatalog.TotalWeight("music", DayUpgrades, hiddenShift) - 1),
            DayUpgrades, hiddenShift);
    }

	private void AcquireItem(string workshop, int cost, int? preparedItemIndex = null)
	{
		if (!RequireStartedDay()) return;
		if (Failed ||
			Completed ||
			EnergyRemaining <= 0 ||
			Score < cost)
			return;

		RecalculateInvisibleHand();
		int hiddenShift = CraftRarityShift(workshop, _invisibleHand);
		Score -= cost;
		EnergyRemaining--;

		for (int itemNumber = 0;
			 itemNumber < ItemsPerSpin;
			 itemNumber++)
		{
			int index = preparedItemIndex ?? WorkshopItemCatalog.Roll(
				workshop,
				_rng.RandiRange(
					0,
					WorkshopItemCatalog.TotalWeight(workshop, DayUpgrades, hiddenShift) - 1),
				DayUpgrades, hiddenShift);

			_storedItems.Add(index);
			_storedGrades.Add("D");
			_pendingCraftStorageIndex = _storedGrades.Count - 1;

			GD.Print(
				$"{workshop}: получен предмет {Items[index].Name} (индекс {index}).");
		}

		LogStoredItems();
		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		_inventoryUI.UpdateUI(_storedItems);
		SaveProgress();
	}

	private void LogStoredItems()
	{
		var entries = _storedItems.ConvertAll(
			index => $"{index}:{Items[index].Name}");

		GD.Print(
			$"Массив предметов: [{string.Join(", ", entries)}]");
	}

	public void PlaceItemInShowcase(int slotIndex)
	{
		if (!RequireStartedDay()) return;
		if (Failed ||
			Completed ||
			slotIndex < 0 ||
			slotIndex >= ShowcaseCapacity ||
			(slotIndex >= InitialShowcaseCapacity &&
			 !MusicWorkshopUnlocked) ||
			_showcaseItems[slotIndex] != -1 ||
			SelectedInventoryIndex < 0 ||
			SelectedInventoryIndex >= _storedItems.Count)
			return;

		_showcaseItems[slotIndex] =
			_storedItems[SelectedInventoryIndex];

		_showcaseGrades[slotIndex] = GetStoredItemQualityRank(SelectedInventoryIndex);
		_storedItems.RemoveAt(SelectedInventoryIndex);
		_storedGrades.RemoveAt(SelectedInventoryIndex);
		if (_pendingCraftStorageIndex >= SelectedInventoryIndex) _pendingCraftStorageIndex--;

		_inventoryUI.DeselectAll();
		_inventoryUI.UpdateUI(_storedItems);

		RefreshItemButtons();
		LogStoredItems();
		SaveProgress();
	}

	public void SelectInventoryItem(int index)
	{
		if (!RequireStartedDay()) return;
		SelectedInventoryIndex = index;
		RefreshItemButtons();
	}

	private void OnShowcasePressed(int slotIndex)
	{
		PlaceItemInShowcase(slotIndex);
	}

	private void RefreshBuyers()
	{
		if (!_dayCycle.CanReceiveBuyers || GetTree().Paused || DayTransitionOpen || TradeActive) return;
		if (!_waitingBuyer) StartWaitingBuyer();
		TryServeWaitingBuyer();
	}

	private void StartBuyerOffer()
	{
		RecalculateInvisibleHand();
		Percentage = _invisibleHand.AdjustInitialOffer(
			TradeOfferRules.InitialPercent(_rng.RandiRange(0, 60)),
			DebugBalanceSettings.Number("hand.offer"));
		RerollsRemaining = GetItemRerollLimit(ItemIndex);

		_waitingBuyer = false;
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
		_buyerWaitWidget.SetState(Items[ItemIndex].WorkshopId == "music", _buyerWaitRemaining);

		_acceptButton.Disabled = false;
		_declineButton.Disabled = false;

		RefreshOffer();
		RefreshItemButtons();
		RefreshProgress();
	}

	public void RefreshItemButtons()
	{
		_atelierGetItemButton.Disabled =
			Failed ||
			Completed ||
			EnergyRemaining <= 0 ||
			Score < AtelierSpinCost;

		_musicGetItemButton.Disabled =
			!MusicWorkshopUnlocked ||
			Failed ||
			Completed ||
			EnergyRemaining <= 0 ||
			Score < MusicSpinCost;

		_musicLockedOverlay.SetLocked(!MusicWorkshopUnlocked);

		_musicUnlockPanel.SetOffer(MusicUnlockCost, !Failed && !Completed && Score >= MusicUnlockCost);

		RefreshShowcases();
	}

	private void RefreshShowcases()
	{
		GetNode<RoomDoorwayPresentation>("RoomDoorways").Refresh(MusicWorkshopUnlocked);
		for (int i = 0; i < ShowcaseCapacity; i++)
		{
			bool locked =
				i >= InitialShowcaseCapacity &&
				!MusicWorkshopUnlocked;

			int itemIndex = _showcaseItems[i];

			_showcaseIcons[i].Texture = itemIndex < 0
				? null
				: GD.Load<Texture2D>(
					Items[itemIndex].ArtPath);

			_showcaseLabels[i].Text = locked
				? GameLocalization.Language == "en"
					? "LOCKED"
					: "ЗАКРЫТО"
				: itemIndex < 0
					? "СВОБОДНО"
					: GetItemName(itemIndex).ToUpperInvariant();

			_showcaseGlass[i].Color =
				new Color(0.870918f, 0.870918f, 0.870918f, 0.58f);

			_showcaseSlots[i].Disabled =
				locked ||
				Failed ||
				Completed ||
				(itemIndex < 0 &&
				 (SelectedInventoryIndex < 0 ||
				  SelectedInventoryIndex >= _storedItems.Count));

			_showcaseSlots[i].Modulate = locked
				? new Color(0.45f, 0.45f, 0.45f, 1f)
				: Colors.White;
		}

		_rightShowroomOverlay.SetLocked(!MusicWorkshopUnlocked);

		_extraShowroomHint.Text = !MusicWorkshopUnlocked
			? GameLocalization.Language == "en"
				? "Unlock the music shop to use these displays"
				: "Откройте музыкальный магазин, чтобы пользоваться витринами"
			: GameLocalization.Language == "en"
				? "Select an inventory item and an empty display"
				: "Выберите предмет в инвентаре и свободную витрину";
	}

	private void MoveItemsOutOfLockedShowcases()
	{
		if (!MusicWorkshopUnlocked)
		{
			for (int i = InitialShowcaseCapacity;
				 i < ShowcaseCapacity; i++)
			{
				if (_showcaseItems[i] < 0)
					continue;

				_storedItems.Add(_showcaseItems[i]);
				_storedGrades.Add(NormalizeRank(_showcaseGrades[i]));
				_showcaseItems[i] = -1;
				_showcaseGrades[i] = "D";
			}
		}

		if (!CheckpointMusicWorkshopUnlocked)
		{
			for (int i = InitialShowcaseCapacity;
				 i < ShowcaseCapacity; i++)
			{
				if (_checkpointShowcaseItems[i] < 0)
					continue;

				_checkpointItems.Add(_checkpointShowcaseItems[i]);
				_checkpointGrades.Add(NormalizeRank(_checkpointShowcaseGrades[i]));
				_checkpointShowcaseItems[i] = -1;
				_checkpointShowcaseGrades[i] = "D";
			}
		}
	}

	private void RefreshOffer()
	{
		SynchronizeBuyerPatienceWithDay();
		Percentage = BuyerPatienceRules.OfferPercent(Percentage, _buyerWaitRemaining);
		int basePrice = GetItemPrice(ItemIndex);

		OfferedPrice = PriceForPercent(
			basePrice, Percentage);

		_baseLabel.Text = GameLocalization.Format(
			"Обычно: {0}$", basePrice);

		_percentLabel.Text = GameLocalization.Format(
			"К цене: {0}{1}%",
			Percentage >= 0 ? "+" : "",
			Percentage);

		// Present the magnitude of the existing offer; no effect on the trading formula.
		float intensity = Mathf.Clamp(Mathf.Abs(Percentage) / 30f, 0f, 1f);
		Color ink = new Color("202020");
		Color strong = Percentage >= 0 ? new Color("268d48") : new Color("c32b38");
		_percentLabel.AddThemeColorOverride("font_color", ink.Lerp(strong, intensity));
		_percentLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(22f * (1f + 0.10f * intensity)));

		RefreshTotalOfferDisplay();

		_declineButton.Text = $"× ({RerollsRemaining})";
		_declineButton.Disabled = !CanRerollCurrentBuyer;
	}

	public static int PriceForPercent(int basePrice, int value)
	{
		return TradeOfferRules.Price(basePrice, value);
	}

	public void RerollCustomer()
	{
		if (!RequireStartedDay()) return;
		if (!CanRerollCurrentBuyer)
			return;

		RerollsRemaining--;
		int basePrice = GetItemPrice(ItemIndex);

		int[] pool = TradeOfferRules.RerollOptions(basePrice, OfferedPrice,
			_rng.Randf() < TradeOfferRules.HigherRerollChance);
		if (pool.Length == 0)
			return;

		Percentage = pool[
			_rng.RandiRange(0, pool.Length - 1)];

		int appearanceCount =
			_customer.GetAppearanceCount();

		CustomerIndex =
			(CustomerIndex +
			 _rng.RandiRange(1, appearanceCount - 1)) %
			appearanceCount;

		_customer.SetAppearance(CustomerIndex);
		RefreshOffer();
		SaveProgress();
		BuyerOfferChanged?.Invoke();
	}

	public void AcceptTrade()
	{
		if (!RequireStartedDay()) return;
		if (!TradeActive)
			return;

		Score += CurrentBuyerPayout;
		Accepted++;

		RefreshScore();
		FinishTrade();
		RecalculateInvisibleHand();
		RefreshProgress();
		SaveProgress();
	}

	public void DeclineTrade()
	{
		RerollCustomer();
	}

	private void FinishTrade()
	{
		_waitingBuyer = false;
		TradeActive = false;
		ItemIndex = -1;
		_activeShowcaseIndex = -1;
		_activeTradeGrade = "D";
		_buyerWaitWidget?.EndTrade();

		_world.ItemVisible = false;
		_customer.Visible = false;
		_tradePanel.Visible = false;
		_acceptButton.Disabled = true;
		_declineButton.Disabled = true;

		RefreshItemButtons();
		RefreshProgress();
	}

	private void RefreshScore()
	{
		_scoreLabel.Text = GameLocalization.Format(
			"Счёт: {0}$", Score);
	}

	private void RefreshProgress()
	{
		_dayLabel.Text = GameLocalization.Format(
			"День: {0} / {1}",
			CurrentDay, FinalDay);

		_energyLabel.Text = GameLocalization.Format(
			"Энергия: {0} / {1}",
			EnergyRemaining, EnergyCapacity);

		if (Completed)
		{
			_quotaLabel.Text = "Все квоты выплачены";
			_deadlineLabel.Text = "Долг погашен";
		}
		else
		{
			int target = QuotaStage == 0
				? FirstQuota
				: SecondQuota;

			int deadline = QuotaStage == 0
				? FirstDeadline
				: FinalDay;

			_quotaLabel.Text = GameLocalization.Format(
				"Квота {0} / 2: {1}$",
				QuotaStage + 1, target);

			_deadlineLabel.Text = GameLocalization.Format(
				"Срок: конец дня {0}", deadline);
		}

		_statusLabel.Text =
			_dayCycle.DisplayStatus(StatusText);

		_dayCycle.UpdateEndButton(_endDayButton);
	}

	public void EndDay()
	{
		_dayCycle.EndDay();
	}

	public void TickCraftDay(double delta)
	{
		_dayCycle.Tick(delta);
	}

	public void PersistProgress()
	{
		SaveProgress();
	}

	public void SynchronizeDayState()
	{
		if (!_dayCycle.CanReceiveBuyers)
		{
			if (_waitingBuyer) HideWaitingBuyer();
		}
		else RefreshBuyers();
		RefreshDayPresentation();
	}

	public void RefreshDayPresentation()
	{
		RefreshClosedBuyerOffer();
		_declineButton.Disabled = !CanRerollCurrentBuyer;
		_inventoryUI.UpdateUI(_storedItems);
		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		SaveProgress();
	}

	public void AdvanceToNextDay()
	{
		if (Failed || Completed || TradeActive || QuotaVisitPending)
			return;

		FinishTrade();

		int deadline = QuotaStage == 0
			? FirstDeadline
			: FinalDay;

		if (CurrentDay >= deadline)
		{
			MafiaNoticePending = true;
			int target = QuotaStage == 0
				? FirstQuota
				: SecondQuota;

			if (Score < target)
			{
				Failed = true;
				StatusText =
					$"Не хватает {target - Score}$ на квоту. Вернитесь к контрольной точке.";
			}
			else if (QuotaStage == 0)
			{
				Score -= target;
				QuotaStage = 1;
				_checkpointQuotaVisit = _quotaVisit = ChikagoShop.Domain.Persistence.QuotaVisitStage.AwaitingVisitor;
				CurrentDay = FirstDeadline + 1;
				CheckpointDay = CurrentDay;
				CheckpointScore = Score;
				// Each quota stage starts with a neutral hidden balance state.
				_invisibleHand.Reset();
				_checkpointInvisibleHandPressure = 0;
				CheckpointAccepted = Accepted;
				CheckpointDeclined = Declined;

				EnergyRemaining = EnergyCapacity;
				CheckpointEnergy = EnergyRemaining;
				CheckpointMusicWorkshopUnlocked =
					MusicWorkshopUnlocked;

				_dayCycle.CaptureCheckpointUpgrades();

				_checkpointItems.Clear();
				_checkpointItems.AddRange(_storedItems);
				_checkpointGrades.Clear();
				_checkpointGrades.AddRange(_storedGrades);
				Array.Copy(_showcaseGrades, _checkpointShowcaseGrades, ShowcaseCapacity);

				Array.Copy(
					_showcaseItems,
					_checkpointShowcaseItems,
					ShowcaseCapacity);

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
			EnergyRemaining = EnergyCapacity;
			StatusText = "Начался новый день.";
		}


		RecalculateInvisibleHand();
		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		SaveProgress();
	}

	public void ReturnToCheckpoint()
	{
		ResetQuotaVisitPresentation();
		_quotaVisit = _checkpointQuotaVisit;
		_dayCycle.Reset();
		FinishTrade();

		_storedItems.Clear();
		_storedItems.AddRange(_checkpointItems);
		_storedGrades.Clear();
		_storedGrades.AddRange(_checkpointGrades);
		Array.Copy(_checkpointShowcaseGrades, _showcaseGrades, ShowcaseCapacity);
		_pendingCraftStorageIndex = -1;

		Array.Copy(
			_checkpointShowcaseItems,
			_showcaseItems,
			ShowcaseCapacity);


		_inventoryUI.DeselectAll();
		CurrentDay = CheckpointDay;
		_dayCycle.RestoreCheckpointUpgrades();

		Score = CheckpointScore;
		_invisibleHand.Restore(_checkpointInvisibleHandPressure);
		MafiaNoticePending = false;
		MusicWorkshopUnlocked =
			CheckpointMusicWorkshopUnlocked;

		MoveItemsOutOfLockedShowcases();
		_dayCycle.SynchronizeRoomDuration();

		EnergyRemaining = CheckpointEnergy;
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

		_inventoryUI.UpdateUI(_storedItems);
		RefreshShowcases();
		SaveProgress();
		_dayCycle.ResumeUpgradeChoice();
	}

	public void RefreshDebugBalance()
	{
		StartingMoney = Economy.StartingMoney;
		FirstQuota = Economy.Quotas.Single(entry => entry.Id == "first").Amount;
		SecondQuota = Economy.Quotas.Single(entry => entry.Id == "final").Amount;
		_dayCycle.ApplyDebugBalance();
		RecalculateInvisibleHand();
		RefreshCraftLabels();
		RefreshDayPresentation();
		RefreshShowcases();
		_inventoryUI.UpdateUI(_storedItems);
		if (TradeActive) RefreshOffer();
	}

	public void NewGame()
	{
		ResetQuotaVisitPresentation();
		_checkpointQuotaVisit = _quotaVisit = ChikagoShop.Domain.Persistence.QuotaVisitStage.Unavailable;
		LoadedSavedGame = false;
		GetNode<WorldCamera>("Camera2D").ResetToOverview();
		_dayCycle.Reset();
		FinishTrade();

		_storedItems.Clear();
		_checkpointItems.Clear();
		_storedGrades.Clear();
		_checkpointGrades.Clear();
		_pendingCraftStorageIndex = -1;
		Array.Fill(_showcaseGrades, "D");
		Array.Fill(_checkpointShowcaseGrades, "D");
		Array.Fill(_showcaseItems, -1);
		Array.Fill(_checkpointShowcaseItems, -1);
		_inventoryUI.DeselectAll();

		CurrentDay = 1;
		_invisibleHand.Reset();
		_checkpointInvisibleHandPressure = 0;
		MafiaNoticePending = false;
		Score = StartingMoney;
		MusicWorkshopUnlocked = false;
		CheckpointMusicWorkshopUnlocked = false;

		_dayCycle.SynchronizeRoomDuration();

		EnergyRemaining = EnergyCapacity;
		Accepted = 0;
		Declined = 0;
		QuotaStage = 0;
		CheckpointDay = 1;
		CheckpointScore = StartingMoney;
		CheckpointEnergy = EnergyCapacity;
		CheckpointAccepted = 0;
		CheckpointDeclined = 0;

		Failed = false;
		Completed = false;
		StatusText = "Новая игра.";

		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		LogStoredItems();

		_inventoryUI.UpdateUI(_storedItems);
		RefreshShowcases();
		SaveProgress();
		NewGameStarted?.Invoke();
	}

}
