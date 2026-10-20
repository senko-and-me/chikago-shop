using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public partial class TradeScene : Node2D
{
	[Export(PropertyHint.Range, "1,9999,1")] public int FirstQuota { get; set; } = 45;
	[Export(PropertyHint.Range, "1,9999,1")] public int SecondQuota { get; set; } = 90;
	[Export] public string SavePath { get; set; } = "user://trade_progress.json";

	private const float MinZoom = 0.7f;
	private const float MaxZoom = 1.8f;
	private const float ZoomStep = 1.15f;
	private const int FirstDeadline = 4;
	private const int FinalDay = 7;
	private const float RoomWidth = 1920f;
	private const float RoomHeight = 1080f;
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

	private readonly RandomNumberGenerator _rng = new();
	private readonly List<int> _storedItems = new();
	private readonly List<int> _checkpointItems = new();
	public int[] StoredItems => _storedItems.ToArray();
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

		_rng.Randomize();
		_acceptButton.Pressed += AcceptTrade;
		_declineButton.Pressed += DeclineTrade;
		_rerollButton.Pressed += RerollCustomer;
		_endDayButton.Pressed += EndDay;
		_atelierGetItemButton.Pressed += GetItemFromAtelier;
		_musicGetItemButton.Pressed += GetItemFromMusicWorkshop;
		_placeItemButton.Pressed += PlaceNextItem;
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

		GetViewport().SizeChanged += ClampCamera;
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
		float oldZoom = _camera.Zoom.X;
		float newZoom = Mathf.Clamp(oldZoom * factor, MinZoom, MaxZoom);
		if (Mathf.IsEqualApprox(oldZoom, newZoom)) return;
		Vector2 offset = mousePosition - GetViewportRect().Size / 2f;
		_camera.Position += offset * (1f / oldZoom - 1f / newZoom);
		_camera.Zoom = Vector2.One * newZoom;
		ClampCamera();
	}

	public void ClampCamera()
	{
		Rect2 bounds = _wall.GetGlobalRect();
		Vector2 halfView = GetViewportRect().Size / (2f * _camera.Zoom);
		Vector2 position = _camera.Position;
		position.X = halfView.X >= bounds.Size.X / 2f
			? bounds.GetCenter().X
			: Mathf.Clamp(position.X, bounds.Position.X + halfView.X, bounds.End.X - halfView.X);
		position.Y = halfView.Y >= bounds.Size.Y / 2f
			? bounds.GetCenter().Y
			: Mathf.Clamp(position.Y, bounds.Position.Y + halfView.Y, bounds.End.Y - halfView.Y);
		// The lower-right quadrant is outside the three-room layout.
		float leftRoomMaxX = halfView.X >= RoomWidth / 2f ? RoomWidth / 2f : RoomWidth - halfView.X;
		float upperRoomMaxY = halfView.Y >= RoomHeight / 2f ? RoomHeight / 2f : RoomHeight - halfView.Y;
		if (position.X > leftRoomMaxX && position.Y > upperRoomMaxY)
		{
			if (position.X - leftRoomMaxX < position.Y - upperRoomMaxY)
				position.X = leftRoomMaxX;
			else
				position.Y = upperRoomMaxY;
		}
		_camera.Position = position;
	}

	public void GetItemFromAtelier() => AcquireItem("Ателье", 0, 1);

	public void GetItemFromMusicWorkshop() => AcquireItem("Муз. штуки", 2, 3);

	private void AcquireItem(string workshop, int firstIndex, int lastIndex)
	{
		if (Failed || Completed) return;
		int index = _rng.RandiRange(firstIndex, lastIndex);
		_storedItems.Add(index);
		GD.Print($"{workshop}: получен предмет {Items[index].Name} (индекс {index}).");
		LogStoredItems();
		RefreshItemButtons();
		SaveProgress();
	}

	private void LogStoredItems()
	{
		var entries = _storedItems.ConvertAll(index => $"{index}:{Items[index].Name}");
		GD.Print($"Массив предметов: [{string.Join(", ", entries)}]");
	}

	public void PlaceNextItem()
	{
		if (Failed || Completed || TradeActive || _storedItems.Count == 0) return;
		ItemIndex = _storedItems[0];
		_storedItems.RemoveAt(0);
		LogStoredItems();
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

	private void RefreshItemButtons()
	{
		_atelierGetItemButton.Disabled = Failed || Completed;
		_musicGetItemButton.Disabled = Failed || Completed;
		_placeItemButton.Visible = !TradeActive;
		_placeItemButton.Disabled = _storedItems.Count == 0 || Failed || Completed;
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
		ReturnActiveItemToArray();
		FinishTrade();
		SaveProgress();
	}

	private void ReturnActiveItemToArray()
	{
		if (!TradeActive || ItemIndex < 0 || ItemIndex >= Items.Length) return;
		_storedItems.Insert(0, ItemIndex);
		LogStoredItems();
	}

	private void FinishTrade()
	{
		TradeActive = false;
		ItemIndex = -1;
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
		ReturnActiveItemToArray();
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
		SaveProgress();
	}

	public void NewGame()
	{
		FinishTrade();
		_storedItems.Clear();
		_checkpointItems.Clear();
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
			SaveVersion = 4, Day = CurrentDay, Score = Score, Accepted = Accepted,
			Declined = Declined, QuotaStage = QuotaStage, CheckpointDay = CheckpointDay,
			CheckpointScore = CheckpointScore, CheckpointAccepted = CheckpointAccepted,
			CheckpointDeclined = CheckpointDeclined, Failed = Failed, Completed = Completed,
			Status = StatusText, TradeActive = TradeActive, ItemIndex = ItemIndex,
			CustomerIndex = CustomerIndex, Percentage = Percentage, RerollsRemaining = RerollsRemaining,
			StoredItems = _storedItems.ToArray(), CheckpointItems = _checkpointItems.ToArray()
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
	}
}
