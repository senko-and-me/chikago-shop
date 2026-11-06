using System;
using Godot;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
	[Export] public PackedScene SlotScene { get; set; } = null!;
	[Export] public ScrollContainer Scroller { get; set; } = null!;
	[Export] public HBoxContainer Container { get; set; } = null!;

	private readonly List<SlotUI> _slots = [];
	public int SelectedIndex { get; private set; } = -1;

	private TradeScene _tradeScene = null!;
	private PanelContainer _itemPopup = null!;
	private Label _itemInfo = null!;
	private SlotUI _hoveredSlot;
	private readonly List<Button> _showcaseTargets = [];
	private int _revision;
	public PanelContainer ItemPopup => _itemPopup;
	public string VisibleItemInfo => _itemInfo.Text;

	public override void _Ready()
	{
		Scroller.GuiInput += OnScrollerGuiInput;
		var layer = new CanvasLayer { Name = "ItemInfoLayer", Layer = 25 };
		AddChild(layer);
		_itemPopup = new PanelContainer { Name = "ItemPopup", Visible = false, MouseFilter = MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat { BgColor = new Color(0.12f, 0.1f, 0.09f, 0.94f),
			ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 };
		_itemPopup.AddThemeStyleboxOverride("panel", style);
		_itemInfo = new Label { MouseFilter = MouseFilterEnum.Ignore };
		_itemInfo.AddThemeFontSizeOverride("font_size", 18);
		_itemInfo.AddThemeColorOverride("font_color", Colors.White);
		layer.AddChild(_itemPopup);
		_itemPopup.AddChild(_itemInfo);
	}

	public override void _Notification(int what)
	{
		if ((what == NotificationPaused || what == NotificationDragBegin) && _itemPopup != null) HideItemInfo();
	}

	private void HideItemInfo()
	{
		_hoveredSlot = null;
		_itemPopup.Hide();
	}

	private void ShowItemInfo(SlotUI slot)
	{
		if (GetTree().Paused || GetViewport().GuiIsDragging() || slot.IsQueuedForDeletion()) return;
		_hoveredSlot = slot;
		UpdateItemInfo();
	}

	public override void _Process(double delta)
	{
		if (_hoveredSlot == null) return;
		if (!IsInstanceValid(_hoveredSlot) || _hoveredSlot.IsQueuedForDeletion() ||
			!_hoveredSlot.IsVisibleInTree() || GetViewport().GuiGetHoveredControl() != _hoveredSlot)
			HideItemInfo();
		else UpdateItemInfo();
	}

	private void UpdateItemInfo()
	{
		if (_hoveredSlot == null) return;
		_itemInfo.Text = _hoveredSlot.ItemInfo;
		_itemPopup.Size = _itemPopup.GetCombinedMinimumSize();
		Rect2 anchor = _hoveredSlot.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, _hoveredSlot.Size);
		Vector2 position = new(anchor.GetCenter().X - _itemPopup.Size.X / 2, anchor.Position.Y - _itemPopup.Size.Y - 8);
		position.X = Mathf.Clamp(position.X, 8, Mathf.Max(8, GetViewportRect().Size.X - _itemPopup.Size.X - 8));
		_itemPopup.Position = position;
		_itemPopup.Show();
	}

	public void Initialize(TradeScene tradeScene)
	{
		_tradeScene = tradeScene;
		foreach (string room in new[] { "ShowroomLeft", "Showroom" })
		foreach (Button target in tradeScene.GetNode<Node2D>($"World/{room}/Showcases").GetChildren())
		{
			int index = _showcaseTargets.Count;
			_showcaseTargets.Add(target);
			target.SetDragForwarding(default,
				Callable.From<Vector2, Variant, bool>((position, data) => CanDropItem(index, data)),
				Callable.From<Vector2, Variant>((position, data) => DropItem(index, data)));
		}
	}

	private bool CanTransfer(int index) => _tradeScene != null && !GetTree().Paused &&
		!_tradeScene.Failed && !_tradeScene.Completed && index >= 0 && index < _slots.Count;

	private Variant BeginItemDrag(int index)
	{
		if (!CanTransfer(index)) return default;
		OnSlotClicked(index);
		HideItemInfo();
		return new Godot.Collections.Dictionary { ["inventory"] = this, ["index"] = index, ["revision"] = _revision };
	}

	private bool CanDropItem(int targetIndex, Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary) return false;
		var payload = data.AsGodotDictionary();
		return payload.ContainsKey("inventory") && payload["inventory"].AsGodotObject() == this &&
			payload.ContainsKey("revision") && payload["revision"].AsInt32() == _revision &&
			payload.ContainsKey("index") && CanTransfer(payload["index"].AsInt32()) &&
			!_showcaseTargets[targetIndex].Disabled && _tradeScene.ShowcaseItems[targetIndex] == -1;
	}

	private void DropItem(int targetIndex, Variant data)
	{
		if (!CanDropItem(targetIndex, data)) return;
		OnSlotClicked(data.AsGodotDictionary()["index"].AsInt32());
		_tradeScene.PlaceItemInShowcase(targetIndex);
	}

	private void QuickMoveItem(int index)
	{
		if (!CanTransfer(index)) return;
		OnSlotClicked(index);
		for (int i = 0; i < _showcaseTargets.Count; i++)
		{
			if (_showcaseTargets[i].Disabled || _tradeScene.ShowcaseItems[i] != -1) continue;
			_tradeScene.PlaceItemInShowcase(i);
			return;
		}
	}

	// Вызывается из TradeScene, когда массив предметов меняется
	public void UpdateUI(List<int> storedItems)
	{
		_revision++;
		ResizeSlots(storedItems.Count);

		for (int i = 0; i < _slots.Count; i++)
		{
			int itemIndex = storedItems[i];
			_slots[i].SetItemTexture(GD.Load<Texture2D>(TradeScene.GetItemArtPath(itemIndex)));
			_slots[i].ItemInfo = $"{TradeScene.GetItemName(itemIndex)}\n" +
				$"Редкость: {TradeScene.GetItemRarity(itemIndex)}\n" +
				$"Стоимость: {TradeScene.GetItemBasePrice(itemIndex)}$";
		}
		UpdateItemInfo();

		// Сбрасываем выделение, если выбранный слот опустел
		if (SelectedIndex >= storedItems.Count)
		{
			DeselectAll();
		}
	}

	private void ResizeSlots(int itemCount)
	{
		while (_slots.Count < itemCount)
		{
			int index = _slots.Count;
			var slot = SlotScene.Instantiate<SlotUI>();
			slot.CreateDragData = BeginItemDrag;
			slot.QuickTransfer = QuickMoveItem;
			slot.SetIndex(index);
			slot.SlotSelected += OnSlotClicked;
			Container.AddChild(slot);
			slot.MouseEntered += () => ShowItemInfo(slot);
			slot.MouseExited += () => { if (_hoveredSlot == slot) HideItemInfo(); };
			_slots.Add(slot);
		}

		while (_slots.Count > itemCount)
		{
			int last = _slots.Count - 1;
			SlotUI slot = _slots[last];
			if (_hoveredSlot == slot) HideItemInfo();
			_slots.RemoveAt(last);
			slot.QueueFree();
		}
	}

	private void OnSlotClicked(int index)
	{
		// Если слот пустой (нет текстуры), игнорируем клик
		if (_slots[index].GetNode<TextureRect>("Icon").Texture == null) return;

		DeselectAll();
		SelectedIndex = index;
		_slots[index].SetSelected(true);

		// Сообщаем основной сцене, что выбран слот
		if (_tradeScene != null)
		{
			_tradeScene.SelectInventoryItem(index);
		}
	}

	private void OnScrollerGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true } wheel) return;

		int direction = wheel.ButtonIndex switch
		{
			MouseButton.WheelUp => -1,
			MouseButton.WheelDown => 1,
			_ => 0
		};
		if (direction == 0) return;

		Scroller.ScrollHorizontal = Math.Max(0, Scroller.ScrollHorizontal + direction * 70);
		Scroller.AcceptEvent();
	}

	public void DeselectAll()
	{
		foreach (var slot in _slots) slot.SetSelected(false);
		SelectedIndex = -1;
		if (_tradeScene != null)
		{
			_tradeScene.SelectedInventoryIndex = -1;
			_tradeScene.RefreshItemButtons();
		}
	}
}
