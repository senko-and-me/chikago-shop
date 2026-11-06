using System;
using Godot;

public partial class SlotUI : Button
{
	public string ItemInfo { get; set; } = "";
	public Func<int, Variant> CreateDragData { get; set; }
	public Action<int> QuickTransfer { get; set; }
	private bool _shiftClick;
	private bool _draggingItem;
	// Сигнал, который сообщит инвентарю, что этот слот нажали
	[Signal] public delegate void SlotSelectedEventHandler(int slotIndex);

	private TextureRect _icon;
	private int _myIndex = -1;
	private bool _isSelected = false;

	public override void _Ready()
	{
		_icon = GetNode<TextureRect>("Icon");

		// Подключаем сигналы мыши
		MouseEntered += OnHover;
		MouseExited += OnUnhover;

		// Встроенный сигнал нажатия кнопки
		Pressed += () =>
		{
			bool quick = _shiftClick;
			_shiftClick = false;
			if (quick) QuickTransfer?.Invoke(_myIndex);
			else EmitSignal(SignalName.SlotSelected, _myIndex);
		};
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse)
			_shiftClick = mouse.ShiftPressed;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		_shiftClick = false;
		Variant data = CreateDragData?.Invoke(_myIndex) ?? default;
		if (data.VariantType == Variant.Type.Nil) return data;
		// The wrapper fixes the screen-space bounds before assigning a large source texture.
		var preview = new Control { Name = "InventoryDragPreview", Size = new Vector2(50, 50),
			MouseFilter = MouseFilterEnum.Ignore };
		var image = new TextureRect { Name = "Icon", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore,
			Texture = _icon.Texture };
		preview.AddChild(image);
		image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		// Godot places the preview root at the pointer; offset the artwork inside it.
		image.Position = -preview.Size / 2;
		SetDragPreview(preview);
		_draggingItem = true;
		_icon.Hide();
		SelfModulate = Colors.White;
		return data;
	}

	public override void _Notification(int what)
	{
		if (what != NotificationDragEnd || !_draggingItem) return;
		_draggingItem = false;
		_icon.Show();
		SetSelected(_isSelected);
	}

	// Устанавливаем индекс слота (0-5)
	public void SetIndex(int index) => _myIndex = index;

	// Устанавливаем картинку предмета
	public void SetItemTexture(Texture2D texture)
	{
		_icon.Texture = texture;
	}

	// Логика выделения (когда предмет выбран)
	public void SetSelected(bool selected)
	{
		_isSelected = selected;
		// Если выбран — желтоватый оттенок, если нет — обычный белый
		SelfModulate = selected ? new Color(1.5f, 1.5f, 0.5f) : new Color(1, 1, 1);
	}

	// Эффект при наведении
	private void OnHover()
	{
		if (!_isSelected) SelfModulate = new Color(1.2f, 1.2f, 1.2f);
	}

	private void OnUnhover()
	{
		if (!_isSelected) SelfModulate = new Color(1, 1, 1);
	}
}
