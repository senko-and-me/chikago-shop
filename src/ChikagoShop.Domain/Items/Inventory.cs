namespace ChikagoShop.Domain.Items;

public sealed class Inventory
{
    private readonly Dictionary<Guid, ItemInstance> _items = [];

    public IReadOnlyCollection<ItemInstance> Items => _items.Values;
    public int Count => _items.Count;

    public void Add(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.State != ItemState.Inventory)
            throw new DomainRuleException("Only an item in inventory state can be added.");
        if (!_items.TryAdd(item.InstanceId, item))
            throw new DomainRuleException($"Item instance '{item.InstanceId}' is already in inventory.");
    }

    public ItemInstance Get(Guid instanceId) => _items.TryGetValue(instanceId, out ItemInstance? item)
        ? item
        : throw new DomainRuleException($"Item instance '{instanceId}' is not in inventory.");

    public bool TransferToShowcase(Guid instanceId, Showcase showcase, int slot)
    {
        ItemInstance item = Get(instanceId);
        if (!showcase.TryPlace(item, slot)) return false;
        _items.Remove(instanceId);
        return true;
    }

    public ItemInstance GiveToNpc(Guid instanceId)
    {
        ItemInstance item = Get(instanceId);
        item.GiveToNpc();
        _items.Remove(instanceId);
        return item;
    }

    public ItemInstance Discard(Guid instanceId)
    {
        ItemInstance item = Get(instanceId);
        item.Discard();
        _items.Remove(instanceId);
        return item;
    }

    internal void ReceiveFromShowcase(ItemInstance item) => Add(item);
}
