namespace ChikagoShop.Domain.Items;

public sealed class ItemInstance
{
    public ItemInstance(Guid instanceId, ItemDefinition definition, int basePrice, int rerollsRemaining)
    {
        if (instanceId == Guid.Empty) throw new ArgumentException("Instance ID cannot be empty.", nameof(instanceId));
        if (basePrice <= 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
        if (rerollsRemaining <= 0) throw new ArgumentOutOfRangeException(nameof(rerollsRemaining));

        InstanceId = instanceId;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        BasePrice = basePrice;
        RerollsRemaining = rerollsRemaining;
    }

    public Guid InstanceId { get; }
    public ItemDefinition Definition { get; }
    public int BasePrice { get; }
    public int RerollsRemaining { get; private set; }
    public ItemState State { get; private set; } = ItemState.Inventory;
    public int? ShowcaseSlot { get; private set; }
    public bool IsTerminal => State is ItemState.Sold or ItemState.GivenToNpc or ItemState.Discarded;

    public int ConsumeReroll()
    {
        RequireState(ItemState.Trading);
        if (RerollsRemaining <= 0) throw new DomainRuleException("The item has no rerolls remaining.");
        return --RerollsRemaining;
    }

    internal void PlaceInShowcase(int slot)
    {
        RequireState(ItemState.Inventory);
        State = ItemState.Showcase;
        ShowcaseSlot = slot;
    }

    internal void ReturnToInventory()
    {
        if (State is not (ItemState.Showcase or ItemState.Trading))
            throw new DomainRuleException($"Cannot return an item from state {State} to inventory.");
        State = ItemState.Inventory;
        ShowcaseSlot = null;
    }

    internal void StartTrading()
    {
        RequireState(ItemState.Showcase);
        State = ItemState.Trading;
    }

    internal void ReturnToShowcase()
    {
        RequireState(ItemState.Trading);
        State = ItemState.Showcase;
    }

    internal void MarkSold()
    {
        RequireState(ItemState.Trading);
        State = ItemState.Sold;
        ShowcaseSlot = null;
    }

    internal void GiveToNpc()
    {
        RequireState(ItemState.Inventory);
        State = ItemState.GivenToNpc;
    }

    internal void Discard()
    {
        if (State is not (ItemState.Inventory or ItemState.Trading))
            throw new DomainRuleException($"Cannot discard an item from state {State}.");
        State = ItemState.Discarded;
        ShowcaseSlot = null;
    }

    private void RequireState(ItemState expected)
    {
        if (State != expected)
            throw new DomainRuleException($"Expected item state {expected}, actual state is {State}.");
    }
}
