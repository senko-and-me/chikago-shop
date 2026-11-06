using ChikagoShop.Domain.Crafting;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class WeightedItemTableTests
{
    [Test]
    public void EveryTicketRespectsConfiguredWeightsAndCategoryIndices()
    {
        foreach (int[] indices in new[] { new[] { 0,1,4,5,6 }, new[] { 2,3,7,8,9 } })
        {
            int[] weights = [60,25,10,4,1];
            var table = new WeightedItemTable(indices.Select((index,i) => (index,weights[i])));
            Assert.That(table.TotalWeight, Is.EqualTo(100));
            int offset = 0;
            for(int i=0;i<weights.Length;i++)
            {
                for(int ticket=offset;ticket<offset+weights[i];ticket++)
                    Assert.That(table.Select(ticket), Is.EqualTo(indices[i]));
                offset += weights[i];
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => table.Select(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.Select(100));
        }
    }

    [Test]
    public void RejectsInvalidTablesAndSupportsRelativeWeights()
    {
        Assert.Throws<ArgumentException>(() => new WeightedItemTable([]));
        Assert.Throws<ArgumentException>(() => new WeightedItemTable([(0,0)]));
        Assert.Throws<ArgumentException>(() => new WeightedItemTable([(0,-1)]));
        Assert.Throws<ArgumentException>(() => new WeightedItemTable([(-1,1)]));
        Assert.Throws<ArgumentException>(() => new WeightedItemTable([(0,1),(0,2)]));
        var table = new WeightedItemTable([(10,2),(20,3)]);
        Assert.That(table.Select(1), Is.EqualTo(10));
        Assert.That(table.Select(2), Is.EqualTo(20));
    }
}
