using Godot;

public partial class SlotUI : Button
{
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
        Pressed += () => EmitSignal(SignalName.SlotSelected, _myIndex);
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