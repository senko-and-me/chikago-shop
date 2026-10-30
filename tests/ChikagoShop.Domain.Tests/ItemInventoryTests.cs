using ChikagoShop.Domain.Items;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class ItemInventoryTests
{
    private static ItemInstance CreateItem(int rerolls = 3) => new(
        Guid.NewGuid(), new ItemDefinition("hat", "atelier", "common"), 30, rerolls);

    [Test]
    public void InventoryHasNoLogicalCapacityAndRejectsDuplicateInstance()
    {
        var inventory = new Inventory();
        for (int index = 0; index < 1000; index++) inventory.Add(CreateItem());

        ItemInstance duplicate = inventory.Items.First();
        Assert.Multiple(() =>
        {
            Assert.That(inventory.Count, Is.EqualTo(1000));
            Assert.That(() => inventory.Add(duplicate), Throws.TypeOf<DomainRuleException>());
        });
    }

    [Test]
    public void TransferToOccupiedShowcaseIsAtomic()
    {
        var inventory = new Inventory();
        var showcase = new Showcase(1);
        ItemInstance first = CreateItem();
        ItemInstance second = CreateItem();
        inventory.Add(first);
        inventory.Add(second);

        Assert.That(inventory.TransferToShowcase(first.InstanceId, showcase, 0), Is.True);
        Assert.That(inventory.TransferToShowcase(second.InstanceId, showcase, 0), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(showcase[0], Is.SameAs(first));
            Assert.That(first.State, Is.EqualTo(ItemState.Showcase));
            Assert.That(inventory.Get(second.InstanceId), Is.SameAs(second));
            Assert.That(second.State, Is.EqualTo(ItemState.Inventory));
        });
    }

    [Test]
    public void ShowcaseCanReturnTheSameInstanceToInventory()
    {
        var inventory = new Inventory();
        var showcase = new Showcase(1);
        ItemInstance item = CreateItem();
        inventory.Add(item);
        inventory.TransferToShowcase(item.InstanceId, showcase, 0);

        showcase.ReturnToInventory(0, inventory);

        Assert.Multiple(() =>
        {
            Assert.That(inventory.Get(item.InstanceId), Is.SameAs(item));
            Assert.That(item.State, Is.EqualTo(ItemState.Inventory));
            Assert.That(item.ShowcaseSlot, Is.Null);
            Assert.That(showcase[0], Is.Null);
        });
    }

    [Test]
    public void TradingTransitionsPreserveIdentityAndTerminalStateIsFinal()
    {
        var inventory = new Inventory();
        var showcase = new Showcase(1);
        ItemInstance item = CreateItem(2);
        inventory.Add(item);
        inventory.TransferToShowcase(item.InstanceId, showcase, 0);
        Assert.That(showcase.StartTrading(0), Is.SameAs(item));

        Assert.That(item.ConsumeReroll(), Is.EqualTo(1));
        Assert.That(showcase.CompleteSale(0), Is.SameAs(item));

        Assert.Multiple(() =>
        {
            Assert.That(item.IsTerminal, Is.True);
            Assert.That(item.State, Is.EqualTo(ItemState.Sold));
            Assert.That(item.ShowcaseSlot, Is.Null);
            Assert.That(showcase[0], Is.Null);
            Assert.That(() => showcase.StartTrading(0), Throws.TypeOf<DomainRuleException>());
        });
    }

    [Test]
    public void NpcTransferAndDiscardAreExplicitTerminalStates()
    {
        var inventory = new Inventory();
        ItemInstance questItem = CreateItem();
        ItemInstance discarded = CreateItem();
        inventory.Add(questItem);
        inventory.Add(discarded);

        Assert.That(inventory.GiveToNpc(questItem.InstanceId), Is.SameAs(questItem));
        Assert.That(inventory.Discard(discarded.InstanceId), Is.SameAs(discarded));

        Assert.Multiple(() =>
        {
            Assert.That(questItem.State, Is.EqualTo(ItemState.GivenToNpc));
            Assert.That(discarded.State, Is.EqualTo(ItemState.Discarded));
            Assert.That(inventory.Count, Is.Zero);
            Assert.That(() => inventory.GiveToNpc(questItem.InstanceId), Throws.TypeOf<DomainRuleException>());
        });
    }

    [Test]
    public void TradingItemCanReturnOrBeDiscardedWithoutLeavingStaleSlot()
    {
        var inventory = new Inventory();
        var showcase = new Showcase(1);
        ItemInstance item = CreateItem();
        inventory.Add(item);
        inventory.TransferToShowcase(item.InstanceId, showcase, 0);

        showcase.StartTrading(0);
        showcase.ReturnToShowcase(0);
        Assert.That(item.State, Is.EqualTo(ItemState.Showcase));
        showcase.StartTrading(0);
        Assert.That(showcase.DiscardTradingItem(0), Is.SameAs(item));

        Assert.Multiple(() =>
        {
            Assert.That(item.State, Is.EqualTo(ItemState.Discarded));
            Assert.That(item.IsTerminal, Is.True);
            Assert.That(showcase[0], Is.Null);
        });
    }
}
