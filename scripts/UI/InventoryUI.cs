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

    public override void _Ready() => Scroller.GuiInput += OnScrollerGuiInput;

    public void Initialize(TradeScene tradeScene)
    {
        _tradeScene = tradeScene;
    }

    // Вызывается из TradeScene, когда массив предметов меняется
    public void UpdateUI(List<int> storedItems)
    {
        ResizeSlots(storedItems.Count);

        for (int i = 0; i < _slots.Count; i++)
        {
            int itemIndex = storedItems[i];
            _slots[i].SetItemTexture(GD.Load<Texture2D>(TradeScene.GetItemArtPath(itemIndex)));
            _slots[i].TooltipText = TradeScene.GetItemRarity(itemIndex);
        }
        
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
            slot.SetIndex(index);
            slot.SlotSelected += OnSlotClicked;
            Container.AddChild(slot);
            _slots.Add(slot);
        }

        while (_slots.Count > itemCount)
        {
            int last = _slots.Count - 1;
            SlotUI slot = _slots[last];
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
