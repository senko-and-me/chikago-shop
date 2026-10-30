namespace ChikagoShop.Domain.Items;

public sealed class Showcase
{
    private readonly ItemInstance?[] _slots;

    public Showcase(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _slots = new ItemInstance[capacity];
    }

    public int Capacity => _slots.Length;
    public ItemInstance? this[int slot] => _slots[ValidateSlot(slot)];

    internal bool TryPlace(ItemInstance item, int slot)
    {
        int index = ValidateSlot(slot);
        if (_slots[index] != null) return false;
        item.PlaceInShowcase(index);
        _slots[index] = item;
        return true;
    }

    public void ReturnToInventory(int slot, Inventory inventory)
    {
        int index = ValidateSlot(slot);
        ItemInstance item = _slots[index] ?? throw new DomainRuleException($"Showcase slot {index} is empty.");
        item.ReturnToInventory();
        inventory.ReceiveFromShowcase(item);
        _slots[index] = null;
    }

    public ItemInstance StartTrading(int slot)
    {
        ItemInstance item = GetItem(slot);
        item.StartTrading();
        return item;
    }

    public void ReturnToShowcase(int slot)
    {
        ItemInstance item = GetItem(slot);
        item.ReturnToShowcase();
    }

    public ItemInstance CompleteSale(int slot)
    {
        int index = ValidateSlot(slot);
        ItemInstance item = GetItem(index);
        item.MarkSold();
        _slots[index] = null;
        return item;
    }

    public ItemInstance DiscardTradingItem(int slot)
    {
        int index = ValidateSlot(slot);
        ItemInstance item = GetItem(index);
        item.Discard();
        _slots[index] = null;
        return item;
    }

    private ItemInstance GetItem(int slot)
    {
        int index = ValidateSlot(slot);
        return _slots[index] ?? throw new DomainRuleException($"Showcase slot {index} is empty.");
    }

    private int ValidateSlot(int slot)
    {
        if (slot < 0 || slot >= _slots.Length) throw new ArgumentOutOfRangeException(nameof(slot));
        return slot;
    }
}
