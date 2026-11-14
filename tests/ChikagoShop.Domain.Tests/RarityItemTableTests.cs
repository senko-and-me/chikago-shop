using ChikagoShop.Domain.Crafting;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public class RarityItemTableTests
{
    [Test]
    public void MultipleItemsShareTheirRarityProbabilityEqually()
    {
        var table = new RarityItemTable(new[] { (0, "common"), (1, "common"),
            (2, "rare"), (3, "rare"), (4, "rare") },
            new Dictionary<string, int> { ["common"] = 60, ["rare"] = 1 });
        var counts = Enumerable.Range(0, table.TotalWeight).Select(table.Select)
            .GroupBy(index => index).ToDictionary(group => group.Key, group => group.Count());
        Assert.That(table.TotalWeight, Is.EqualTo(366));
        Assert.That(counts[0], Is.EqualTo(180));
        Assert.That(counts[1], Is.EqualTo(counts[0]));
        Assert.That(counts[2], Is.EqualTo(2));
        Assert.That(counts[3], Is.EqualTo(counts[2]));
        Assert.That(counts[4], Is.EqualTo(counts[2]));
        Assert.That((counts[0] + counts[1]) / (counts[2] + counts[3] + counts[4]), Is.EqualTo(60));
    }

    [Test]
    public void SingleItemPerRarityKeepsOriginalTicketsAndRejectsInvalidTickets()
    {
        var table = new RarityItemTable(new[] { (7, "common"), (11, "rare") },
            new Dictionary<string, int> { ["common"] = 60, ["rare"] = 1 });
        Assert.That(table.TotalWeight, Is.EqualTo(61));
        Assert.That(table.Select(59), Is.EqualTo(7));
        Assert.That(table.Select(60), Is.EqualTo(11));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Select(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Select(61));
    }
}
