using Godot;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
    [Export] public PackedScene SlotScene { get; set; } // Сюда перетащи SlotUI.tscn
    [Export] public HBoxContainer Container { get; set; } // Сюда перетащи HBoxContainer

    private List<SlotUI> _slots = new();
    public int SelectedIndex { get; private set; } = -1;

    // Ссылка на основную сцену
    private TradeScene _tradeScene;

    public override void _Ready()
    {
        // Создаем 6 слотов
        for (int i = 0; i < 6; i++)
        {
            var slot = SlotScene.Instantiate<SlotUI>();
            slot.SetIndex(i);
            slot.SlotSelected += OnSlotClicked;
            Container.AddChild(slot);
            _slots.Add(slot);
        }
    }

    public void Initialize(TradeScene tradeScene)
    {
        _tradeScene = tradeScene;
    }

    // Вызывается из TradeScene, когда массив предметов меняется
    public void UpdateUI(List<int> storedItems)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < storedItems.Count)
            {
                int itemIndex = storedItems[i];
                
                // Берем строковое имя предмета ("hat", "coat" и т.д.)
                string kind = TradeScene.GetItemKind(itemIndex); 
                
                // Загружаем SVG из папки art. 
                // ВАЖНО: убедись, что имена файлов совпадают (hat.svg, coat.svg и т.д.)
                Texture2D icon = GD.Load<Texture2D>($"res://art/items/{kind}.svg");
                
                _slots[i].SetItemTexture(icon);
            }
            else
            {
                _slots[i].SetItemTexture(null); // Пустой слот
            }
        }
        
        // Сбрасываем выделение, если выбранный слот опустел
        if (SelectedIndex >= storedItems.Count)
        {
            DeselectAll();
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
            _tradeScene.SelectedInventoryIndex = index;
            _tradeScene.RefreshItemButtons(); // Обновляем кнопку "Разместить"
        }
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