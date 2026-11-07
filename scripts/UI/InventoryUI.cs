
using System;
using System.Collections.Generic;
using Godot;

public partial class InventoryUI : Control
{
	[Export] public PackedScene SlotScene { get; set; } = null!;
	[Export] public ScrollContainer Scroller { get; set; } = null!;
	[Export] public HBoxContainer Container { get; set; } = null!;

	private const float SlotSize = 108f;
	private const float SlotGap = 24f;
	private const float BottomMargin = 18f;

	private readonly List<SlotUI> _slots = [];
	private readonly List<int> _visibleItems = [];
	private readonly List<Button> _showcaseTargets = [];

	private TradeScene _tradeScene = null!;
	private ItemInfoPopup _itemPopup = null!;
	private TextureRect _slotBackground = null!;
	private Label _pageLabel = null!;
	private int _capacity = 3;
	private int _pageStart;
	private int _revision;

	public int SelectedIndex { get; private set; } = -1;
	public PanelContainer ItemPopup => _itemPopup;
	public string VisibleItemInfo => _itemPopup.ItemText;

	public override void _Ready()
	{
		Scroller.GuiInput += OnScrollerGuiInput;
		Scroller.MouseFilter = MouseFilterEnum.Pass;
		Scroller.AddThemeStyleboxOverride(
			"panel", new StyleBoxEmpty());
		Scroller.SelfModulate = Colors.White;
		Scroller.ScrollHorizontal = 0;

		Container.AddThemeConstantOverride(
			"separation", (int)SlotGap);

		_slotBackground = new TextureRect
		{
			Name = "InventorySlotBackground",
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale
		};

		AddChild(_slotBackground);
		MoveChild(_slotBackground, 0);
		_slotBackground.Show();
		_slotBackground.SelfModulate = Colors.White;
		_slotBackground.ZIndex = 0;
		Scroller.ZIndex = 1;

		_pageLabel = new Label
		{
			Name = "InventoryPageLabel",
			HorizontalAlignment = HorizontalAlignment.Right,
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false
		};

		_pageLabel.AddThemeColorOverride(
			"font_color", Colors.White);
		_pageLabel.AddThemeColorOverride(
			"font_outline_color", Colors.Black);
		_pageLabel.AddThemeConstantOverride(
			"outline_size", 5);
		_pageLabel.AddThemeFontSizeOverride(
			"font_size", 14);

		AddChild(_pageLabel);

		var layer = new CanvasLayer
		{
			Name = "ItemInfoLayer",
			Layer = 25
		};

		AddChild(layer);

		_itemPopup = new ItemInfoPopup
		{
			Name = "ItemPopup"
		};

		layer.AddChild(_itemPopup);
		ConfigureLayout();
	}

	public void Initialize(TradeScene tradeScene)
	{
		_tradeScene = tradeScene;

		foreach (string room in new[]
			{ "ShowroomLeft", "Showroom" })
		{
			foreach (Button target in tradeScene
				.GetNode<Node2D>($"World/{room}/Showcases")
				.GetChildren())
			{
				int index = _showcaseTargets.Count;
				_showcaseTargets.Add(target);

				target.SetDragForwarding(
					default,
					Callable.From<Vector2, Variant, bool>(
						(position, data) =>
							CanDropItem(index, data)),
					Callable.From<Vector2, Variant>(
						(position, data) =>
							DropItem(index, data))
				);
			}
		}

		UpdateUI(new List<int>(tradeScene.StoredItems));
	}

	private void ConfigureLayout()
	{
		_capacity = _tradeScene == null
			? 3
			: _tradeScene.InventorySlotCapacity;

		float width =
			_capacity * SlotSize + (_capacity - 1) * SlotGap;

		float top = -BottomMargin - SlotSize;

		_slotBackground.AnchorLeft = 0.5f;
		_slotBackground.AnchorRight = 0.5f;
		_slotBackground.AnchorTop = 1;
		_slotBackground.AnchorBottom = 1;
		_slotBackground.OffsetLeft = -width / 2;
		_slotBackground.OffsetRight = width / 2;
		_slotBackground.OffsetTop = top;
		_slotBackground.OffsetBottom = -BottomMargin;

		string path = _capacity == 6
			? "res://art/ui/Slots6.png"
			: "res://art/ui/Slots3.png";

		if (ResourceLoader.Exists(path))
		{
			_slotBackground.Texture =
				GD.Load<Texture2D>(path);
			_slotBackground.Show();
		}
		else
		{
			_slotBackground.Texture = null;
			GD.PushWarning(
				$"Inventory background not found: {path}");
		}

		Scroller.AnchorLeft = 0.5f;
		Scroller.AnchorRight = 0.5f;
		Scroller.AnchorTop = 1;
		Scroller.AnchorBottom = 1;
		Scroller.OffsetLeft = -width / 2;
		Scroller.OffsetRight = width / 2;
		Scroller.OffsetTop = top;
		Scroller.OffsetBottom = -BottomMargin;
		Scroller.ScrollHorizontal = 0;

		_pageLabel.AnchorLeft = 0.5f;
		_pageLabel.AnchorRight = 0.5f;
		_pageLabel.AnchorTop = 1;
		_pageLabel.AnchorBottom = 1;
		_pageLabel.OffsetLeft = width / 2 - 75;
		_pageLabel.OffsetRight = width / 2;
		_pageLabel.OffsetTop = top - 26;
		_pageLabel.OffsetBottom = top - 3;
	}

	private bool CanTransfer(int index)
	{
		return _tradeScene != null &&
			!GetTree().Paused &&
			!_tradeScene.Failed &&
			!_tradeScene.Completed &&
			index >= 0 &&
			index < _visibleItems.Count;
	}

	private Variant BeginItemDrag(int index)
	{
		if (!CanTransfer(index))
			return default;

		OnSlotClicked(index);
		_itemPopup.HideInfo();

		return new Godot.Collections.Dictionary
		{
			["inventory"] = this,
			["index"] = index,
			["revision"] = _revision
		};
	}

	private bool CanDropItem(int targetIndex, Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary)
			return false;

		var payload = data.AsGodotDictionary();

		return payload.ContainsKey("inventory") &&
			payload["inventory"].AsGodotObject() == this &&
			payload.ContainsKey("revision") &&
			payload["revision"].AsInt32() == _revision &&
			payload.ContainsKey("index") &&
			CanTransfer(payload["index"].AsInt32()) &&
			!_showcaseTargets[targetIndex].Disabled &&
			_tradeScene.ShowcaseItems[targetIndex] == -1;
	}

	private void DropItem(int targetIndex, Variant data)
	{
		if (!CanDropItem(targetIndex, data))
			return;

		OnSlotClicked(
			data.AsGodotDictionary()["index"].AsInt32());

		_tradeScene.PlaceItemInShowcase(targetIndex);
	}

	private void QuickMoveItem(int index)
	{
		if (!CanTransfer(index))
			return;

		OnSlotClicked(index);
		int[] showcaseItems = _tradeScene.ShowcaseItems;

		for (int i = 0; i < _showcaseTargets.Count; i++)
		{
			if (_showcaseTargets[i].Disabled ||
				showcaseItems[i] != -1)
				continue;

			_tradeScene.PlaceItemInShowcase(i);
			return;
		}
	}

	public void UpdateUI(List<int> storedItems)
	{
		_visibleItems.Clear();
		_visibleItems.AddRange(storedItems);
		_revision++;

		int capacity = _tradeScene == null
			? 3
			: _tradeScene.InventorySlotCapacity;

		if (capacity != _capacity)
		{
			_pageStart = 0;
			DeselectAll();
		}

		ConfigureLayout();
		ResizeSlots();

		_pageStart = Math.Min(
			_pageStart,
			Math.Max(
				0,
				((_visibleItems.Count - 1) / _capacity) *
				_capacity
			)
		);

		RenderPage();

		if (SelectedIndex >= _visibleItems.Count)
			DeselectAll();

		_itemPopup.Refresh();
	}

	private void ResizeSlots()
	{
		while (_slots.Count < _capacity)
		{
			var slot = SlotScene.Instantiate<SlotUI>();

			slot.CreateDragData = BeginItemDrag;
			slot.QuickTransfer = QuickMoveItem;
			slot.SlotSelected += OnSlotClicked;

			slot.CustomMinimumSize =
				new Vector2(SlotSize, SlotSize);

			slot.MouseDefaultCursorShape =
				CursorShape.PointingHand;

			Container.AddChild(slot);

			slot.MouseEntered += () =>
				_itemPopup.ShowFor(slot);

			slot.MouseExited += () =>
				_itemPopup.HideFor(slot);

			_slots.Add(slot);
		}

		while (_slots.Count > _capacity)
		{
			int last = _slots.Count - 1;
			SlotUI slot = _slots[last];

			_itemPopup.HideFor(slot);
			_slots.RemoveAt(last);
			slot.QueueFree();
		}
	}

	private void RenderPage()
	{
		for (int i = 0; i < _slots.Count; i++)
		{
			SlotUI slot = _slots[i];
			int index = _pageStart + i;

			slot.SetIndex(index);

			bool occupied = index < _visibleItems.Count;

			slot.MouseFilter = occupied
				? MouseFilterEnum.Stop
				: MouseFilterEnum.Ignore;

			slot.SetSelected(
				occupied && index == SelectedIndex);

			if (!occupied)
			{
				slot.SetItemTexture(null);
				slot.ItemInfo = "";
				continue;
			}

			int itemIndex = _visibleItems[index];

			slot.SetItemTexture(
				GD.Load<Texture2D>(
					TradeScene.GetItemArtPath(itemIndex))
			);

			slot.ItemInfo = GameLocalization.Format(
				"{0}\nРедкость: {1}\nСтоимость: {2}$",
				TradeScene.GetItemName(itemIndex),
				TradeScene.GetItemRarity(itemIndex),
				_tradeScene.GetItemPrice(itemIndex)
			);
		}

		int pageCount = Math.Max(
			1,
			(_visibleItems.Count + _capacity - 1) /
			_capacity
		);

		_pageLabel.Visible = pageCount > 1;
		_pageLabel.Text =
			$"{_pageStart / _capacity + 1}/{pageCount}";
	}

	private void OnSlotClicked(int index)
	{
		if (!CanTransfer(index))
			return;

		DeselectAll();
		SelectedIndex = index;

		if (index >= _pageStart &&
			index < _pageStart + _slots.Count)
		{
			_slots[index - _pageStart].SetSelected(true);
		}

		_tradeScene.SelectInventoryItem(index);
	}

	private void OnScrollerGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton
			{ Pressed: true } wheel)
			return;

		int direction = wheel.ButtonIndex switch
		{
			MouseButton.WheelUp => -1,
			MouseButton.WheelDown => 1,
			_ => 0
		};

		if (direction == 0 ||
			_visibleItems.Count <= _capacity)
			return;

		int maxPage =
			(_visibleItems.Count - 1) / _capacity;

		int currentPage = _pageStart / _capacity;

		int nextPage = Math.Clamp(
			currentPage + direction,
			0,
			maxPage
		);

		if (nextPage == currentPage)
			return;

		_pageStart = nextPage * _capacity;
		_revision++;

		DeselectAll();
		RenderPage();

		Scroller.ScrollHorizontal = 0;
		Scroller.AcceptEvent();
	}

	public void DeselectAll()
	{
		foreach (var slot in _slots)
			slot.SetSelected(false);

		SelectedIndex = -1;

		if (_tradeScene != null)
		{
			_tradeScene.SelectedInventoryIndex = -1;
			_tradeScene.RefreshItemButtons();
		}
	}
}
