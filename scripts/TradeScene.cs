
using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Trading;
using Godot;

public partial class TradeScene : Node2D
{
	private static readonly ChikagoShop.Domain.Configuration.EconomyConfig Economy = EconomyConfigProvider.Load();
	[Export(PropertyHint.Range, "1,9999,1")]
	public int StartingMoney { get; set; } = Economy.StartingMoney;

	[Export(PropertyHint.Range, "1,9999,1")]
	public int FirstQuota { get; set; } = Economy.Quotas.Single(entry => entry.Id == "first").Amount;

	[Export(PropertyHint.Range, "1,9999,1")]
	public int SecondQuota { get; set; } = Economy.Quotas.Single(entry => entry.Id == "final").Amount;

	[Export]
	public string SavePath { get; set; } = "user://trade_progress.json";

	private static readonly int FirstDeadline = Economy.Quotas.Single(entry => entry.Id == "first").DeadlineDay;
	private static readonly int FinalDay = Economy.Days.TotalDays;
	private const int ShowcaseCapacity = 6;
	private const int InitialShowcaseCapacity = 3;
	private static readonly int EnergyBeforeMusicUnlock = Economy.Days.EnergyBeforeMusicUnlock;
	private static readonly int EnergyAfterMusicUnlock = Economy.Days.EnergyAfterMusicUnlock;
	private static readonly int AtelierSpinCost = WorkshopItemCatalog.CraftCost("atelier");
	private static readonly int MusicSpinCost = WorkshopItemCatalog.CraftCost("music");
	private static readonly int MusicUnlockCost = Economy.Workshops.Single(entry => entry.Id == "music").UnlockCost;
	private static readonly int ItemsPerSpin = Economy.Days.ItemsPerCraft;

	private static readonly IReadOnlyList<CraftItem> Items =
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
	private Control _musicLockedOverlay = null!;
	private Button _musicUnlockButton = null!;

	private readonly Control[] _rarityPanels = new Control[2];
	private readonly Control[] _rarityHotspots = new Control[2];
	private readonly Control[] _priceHints = new Control[2];
	private readonly Button[] _showcaseSlots = new Button[ShowcaseCapacity];
	private readonly ColorRect[] _showcaseGlass = new ColorRect[ShowcaseCapacity];
	private readonly TextureRect[] _showcaseIcons = new TextureRect[ShowcaseCapacity];
	private readonly Label[] _showcaseLabels = new Label[ShowcaseCapacity];

	private Label _extraShowroomHint = null!;
	private ColorRect _rightShowroomOverlay = null!;
	private InventoryUI _inventoryUI = null!;

	private readonly RandomNumberGenerator _rng = new();
	private readonly RandomNumberGenerator _buyerArrivalRng = new();
	private readonly List<int> _storedItems = new();
	private readonly List<int> _checkpointItems = new();
	private readonly int[] _showcaseItems = new int[ShowcaseCapacity];
	private readonly int[] _checkpointShowcaseItems =
		new int[ShowcaseCapacity];

	private int _activeShowcaseIndex = -1;
	private double _buyerCountdown;
	private DayCycleController _dayCycle = null!;
	private EmbroideryGame _embroidery = null!;
	private EmbroideryGame _polishing = null!;
	private int _energyRemaining = EnergyBeforeMusicUnlock;
	private bool _localizationReady;

	private float NextBuyerDelay() =>
		_buyerArrivalRng.RandfRange(0.5f, 2f);

	public int SelectedInventoryIndex { get; set; } = -1;
	public bool CraftMiniGameOpen =>
		(_embroidery?.IsOpen ?? false) ||
		(_polishing?.IsOpen ?? false);

	public bool DayClosed => _dayCycle.Session.IsClosed;
	public bool DayTransitionOpen =>
		_dayCycle?.TransitionOpen ?? false;

	public ChikagoShop.Domain.Days.DayUpgradeSession DayUpgrades =>
		_dayCycle?.Upgrades;

	public double DayRemainingSeconds =>
		_dayCycle.Session.RemainingSeconds;

	public int[] StoredItems => _storedItems.ToArray();
	public int[] ShowcaseItems => (int[])_showcaseItems.Clone();
	public double BuyerCountdown => _buyerCountdown;

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

	public int QuotaStage { get; private set; }
	public int CheckpointDay { get; private set; } = 1;
	public int CheckpointScore { get; private set; }
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

		EnsureMusicWorkshopUi();

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

		EnsureExtraShowroomOverlay();

		_inventoryUI = GetNode<InventoryUI>("HUD/InventoryUI");
		_inventoryUI.Initialize(this);

		_rng.Randomize();
		_buyerArrivalRng.Randomize();

		_acceptButton.Pressed += AcceptTrade;
		_declineButton.Pressed += RerollCustomer;
		_endDayButton.Pressed += EndDay;
		_atelierGetItemButton.Pressed += GetItemFromAtelier;
		_musicGetItemButton.Pressed += GetItemFromMusicWorkshop;
		_musicUnlockButton.Pressed += TryUnlockMusicWorkshop;

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

		_embroidery = new EmbroideryGame(
			() => AcquireItem("atelier", AtelierSpinCost));

		AddChild(_embroidery);

		_polishing = new EmbroideryGame(
			() => AcquireItem("music", MusicSpinCost),
			polishing: true);

		AddChild(_polishing);

		bool restored = LoadProgress();

		RefreshScore();
		RefreshProgress();

		if (restored)
		{
			if (TradeActive)
				ShowTrade();
			else
				FinishTrade();
		}
		else
		{
			FinishTrade();
		}

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

	private void EnsureMusicWorkshopUi()
	{
		Node2D workshop =
			GetNode<Node2D>("World/MusicWorkshop");

		RefreshCraftLabels();

		if (!workshop.HasNode("LockedOverlay"))
		{
			ColorRect overlay = new()
			{
				Name = "LockedOverlay",
				Position = Vector2.Zero,
				Size = new Vector2(1920f, 1080f),
				Color = new Color(
					0.015f, 0.015f, 0.015f, 0.97f),
				MouseFilter = Control.MouseFilterEnum.Pass,
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

			title.AddThemeColorOverride(
				"font_color", Colors.White);

			title.AddThemeFontSizeOverride(
				"font_size", 52);

			overlay.AddChild(title);

			Button unlockButton = new()
			{
				Name = "UnlockButton",
				Position = new Vector2(660f, 470f),
				Size = new Vector2(600f, 170f),
				Text = "ОТКРЫТЬ ЗА 100$",
				TooltipText =
					"Открыть музыкальную комнату за 100$",
				MouseDefaultCursorShape =
					Control.CursorShape.PointingHand
			};

			unlockButton.AddThemeColorOverride(
				"font_color", Colors.White);

			unlockButton.AddThemeFontSizeOverride(
				"font_size", 38);

			overlay.AddChild(unlockButton);
		}

		_musicLockedOverlay =
			workshop.GetNode<Control>("LockedOverlay");

		_musicUnlockButton =
			workshop.GetNode<Button>(
				"LockedOverlay/UnlockButton");
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
			? $"Clothing  •  1 for ${AtelierSpinCost}"
			: $"Одежда  •  {AtelierSpinCost}$ за 1";

		musicHint.Text = english
			? $"Instruments  •  1 for ${MusicSpinCost}"
			: $"Инструменты  •  {MusicSpinCost}$ за 1";

		atelierPrice.Text = english
			? $"1 item  •  ${AtelierSpinCost}"
			: $"1 предмет  •  {AtelierSpinCost}$";

		musicPrice.Text = english
			? $"1 item  •  ${MusicSpinCost}"
			: $"1 предмет  •  {MusicSpinCost}$";

		_atelierGetItemButton.Text = english
			? "CREATE ITEM"
			: "СОЗДАТЬ ПРЕДМЕТ";

		_musicGetItemButton.Text = english
			? "CREATE ITEM"
			: "СОЗДАТЬ ПРЕДМЕТ";
	}

	private void EnsureExtraShowroomOverlay()
	{
		Node2D showroom =
			GetNode<Node2D>("World/Showroom");

		_rightShowroomOverlay =
			showroom.GetNodeOrNull<ColorRect>("LockedOverlay");

		if (_rightShowroomOverlay != null)
			return;

		_rightShowroomOverlay = new ColorRect
		{
			Name = "LockedOverlay",
			Position = Vector2.Zero,
			Size = new Vector2(1920, 1080),
			Color = new Color(
				0.015f, 0.015f, 0.015f, 0.90f),
			MouseFilter = Control.MouseFilterEnum.Stop,
			ZIndex = 100
		};

		showroom.AddChild(_rightShowroomOverlay);

		Label title = new Label
		{
			Name = "LockTitle",
			Position = new Vector2(320, 410),
			Size = new Vector2(1280, 170),
			Text = "ВИТРИНЫ ЗАКРЫТЫ",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		title.AddThemeFontSizeOverride(
			"font_size", 48);

		title.AddThemeColorOverride(
			"font_color", Colors.White);

		_rightShowroomOverlay.AddChild(title);
	}

	public override void _Process(double delta)
	{
		if (GetTree().Paused)
			return;

		_dayCycle.Tick(delta);
		TickBuyers(delta);
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
		_embroidery.Open();
	}

	public void GetItemFromMusicWorkshop()
	{
		if (!MusicWorkshopUnlocked)
			return;

		_polishing.Open();
	}

	public void TryUnlockMusicWorkshop()
	{
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

		StatusText = "Музыкальная мастерская открыта.";

		_inventoryUI.UpdateUI(_storedItems);

		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		SaveProgress();
	}

	private void AcquireItem(string workshop, int cost)
	{
		if (Failed ||
			Completed ||
			EnergyRemaining <= 0 ||
			Score < cost)
			return;

		Score -= cost;
		EnergyRemaining--;

		for (int itemNumber = 0;
			 itemNumber < ItemsPerSpin;
			 itemNumber++)
		{
			int index = WorkshopItemCatalog.Roll(
				workshop,
				_rng.RandiRange(
					0,
					WorkshopItemCatalog.TotalWeight(workshop) - 1),
				DayUpgrades);

			_storedItems.Add(index);

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

		bool firstItem = Array.TrueForAll(
			_showcaseItems, item => item == -1);

		_showcaseItems[slotIndex] =
			_storedItems[SelectedInventoryIndex];

		_storedItems.RemoveAt(SelectedInventoryIndex);

		_inventoryUI.DeselectAll();
		_inventoryUI.UpdateUI(_storedItems);

		if (firstItem &&
			!TradeActive &&
			_dayCycle.CanReceiveBuyers)
		{
			_buyerCountdown = NextBuyerDelay();
		}

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
		if (!_dayCycle.CanReceiveBuyers ||
			TradeActive ||
			Array.TrueForAll(
				_showcaseItems, item => item == -1))
			return;

		_buyerCountdown -= delta;

		if (_buyerCountdown > 0)
			return;

		var occupied = new List<int>();

		for (int i = 0; i < ShowcaseCapacity; i++)
		{
			if (_showcaseItems[i] >= 0)
				occupied.Add(i);
		}

		_activeShowcaseIndex = occupied[
			_rng.RandiRange(0, occupied.Count - 1)];

		ItemIndex = _showcaseItems[_activeShowcaseIndex];
		_showcaseItems[_activeShowcaseIndex] = -1;
		_buyerCountdown = 0;

		RefreshShowcases();
		StartBuyerOffer();
	}

	private void StartBuyerOffer()
	{
		Percentage = TradeOfferRules.InitialPercent(_rng.RandiRange(0, 60));
		RerollsRemaining = GetItemRerollLimit(ItemIndex);

		CustomerIndex = _rng.RandiRange(
			0, _customer.GetAppearanceCount() - 1);

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

		_musicLockedOverlay.Visible = !MusicWorkshopUnlocked;

		_musicUnlockButton.Disabled =
			Failed ||
			Completed ||
			Score < MusicUnlockCost;

		RefreshShowcases();
	}

	private void RefreshShowcases()
	{
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
				new Color(0.77f, 0.9f, 0.88f, 0.58f);

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

		_rightShowroomOverlay.Visible = !MusicWorkshopUnlocked;

		if (_rightShowroomOverlay.GetNodeOrNull<Label>(
			"LockTitle") is Label lockTitle)
		{
			lockTitle.Text =
				GameLocalization.Language == "en"
					? "DISPLAYS LOCKED"
					: "ВИТРИНЫ ЗАКРЫТЫ";
		}

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
				_showcaseItems[i] = -1;
			}
		}

		if (!CheckpointMusicWorkshopUnlocked)
		{
			for (int i = InitialShowcaseCapacity;
				 i < ShowcaseCapacity; i++)
			{
				if (_checkpointShowcaseItems[i] < 0)
					continue;

				_checkpointItems.Add(
					_checkpointShowcaseItems[i]);

				_checkpointShowcaseItems[i] = -1;
			}
		}
	}

	private void RefreshOffer()
	{
		int basePrice = GetItemPrice(ItemIndex);

		OfferedPrice = PriceForPercent(
			basePrice, Percentage);

		_baseLabel.Text = GameLocalization.Format(
			"Обычно: {0}$", basePrice);

		_percentLabel.Text = GameLocalization.Format(
			"К цене: {0}{1}%",
			Percentage >= 0 ? "+" : "",
			Percentage);

		_offerLabel.Text = GameLocalization.Format(
			"Получим: {0}$", OfferedPrice);

		_declineButton.Text = $"× ({RerollsRemaining})";
		_declineButton.Disabled = RerollsRemaining <= 0;
	}

	public static int PriceForPercent(int basePrice, int value)
	{
		return TradeOfferRules.Price(basePrice, value);
	}

	public void RerollCustomer()
	{
		if (!TradeActive || RerollsRemaining <= 0)
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
	}

	public void AcceptTrade()
	{
		if (!TradeActive)
			return;

		Score += OfferedPrice;
		Accepted++;

		RefreshScore();
		FinishTrade();
		RefreshProgress();
		SaveProgress();
	}

	public void DeclineTrade()
	{
		RerollCustomer();
	}

	private void FinishTrade()
	{
		bool hadActiveTrade = TradeActive;
		TradeActive = false;
		ItemIndex = -1;
		_activeShowcaseIndex = -1;

		if (hadActiveTrade)
		{
			_buyerCountdown =
				_dayCycle.CanReceiveBuyers &&
				Array.Exists(
					_showcaseItems, item => item >= 0)
					? NextBuyerDelay()
					: 0;
		}

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
		_buyerCountdown = 0;
		RefreshDayPresentation();
	}

	public void RefreshDayPresentation()
	{
		_inventoryUI.UpdateUI(_storedItems);
		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		SaveProgress();
	}

	public void AdvanceToNextDay()
	{
		if (Failed || Completed || TradeActive)
			return;

		FinishTrade();

		int deadline = QuotaStage == 0
			? FirstDeadline
			: FinalDay;

		if (CurrentDay >= deadline)
		{
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
				CurrentDay = FirstDeadline + 1;
				CheckpointDay = CurrentDay;
				CheckpointScore = Score;
				CheckpointAccepted = Accepted;
				CheckpointDeclined = Declined;

				EnergyRemaining = EnergyCapacity;
				CheckpointEnergy = EnergyRemaining;
				CheckpointMusicWorkshopUnlocked =
					MusicWorkshopUnlocked;

				_dayCycle.CaptureCheckpointUpgrades();

				_checkpointItems.Clear();
				_checkpointItems.AddRange(_storedItems);

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

		_buyerCountdown = Array.Exists(
			_showcaseItems,
			item => item >= 0)
			? NextBuyerDelay()
			: 0;

		RefreshScore();
		RefreshProgress();
		RefreshItemButtons();
		SaveProgress();
	}

	public void ReturnToCheckpoint()
	{
		_dayCycle.Reset();
		FinishTrade();

		_storedItems.Clear();
		_storedItems.AddRange(_checkpointItems);

		Array.Copy(
			_checkpointShowcaseItems,
			_showcaseItems,
			ShowcaseCapacity);

		_buyerCountdown = Array.Exists(
			_showcaseItems, item => item >= 0)
			? NextBuyerDelay()
			: 0;

		_inventoryUI.DeselectAll();
		CurrentDay = CheckpointDay;
		_dayCycle.RestoreCheckpointUpgrades();

		Score = CheckpointScore;
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
	}

	public void NewGame()
	{
		_dayCycle.Reset();
		FinishTrade();

		_storedItems.Clear();
		_checkpointItems.Clear();
		Array.Fill(_showcaseItems, -1);
		Array.Fill(_checkpointShowcaseItems, -1);
		_buyerCountdown = 0;
		_inventoryUI.DeselectAll();

		CurrentDay = 1;
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
	}

}
