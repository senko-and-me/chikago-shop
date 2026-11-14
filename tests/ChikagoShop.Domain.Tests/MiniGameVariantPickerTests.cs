using ChikagoShop.Domain.Crafting;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class MiniGameVariantPickerTests
{
    [Test]
    public void FirstDrawIsUniformAndEveryVariantIsReachable()
    {
        for (int index = 0; index < 4; index++)
            Assert.That(new MiniGameVariantPicker(4).Next((index + 0.5) / 4), Is.EqualTo(index));
    }

    [Test]
    public void ConsecutiveRepeatsKeepHalvingTheirWeightUntilThePositiveFloor()
    {
        var picker = new MiniGameVariantPicker(4);
        for (int run = 1; run <= 20; run++)
        {
            Assert.That(picker.Next(0), Is.EqualTo(0), "A repeat must remain possible even after a long streak");
            double weight = Math.Pow(0.5, Math.Min(run, 6));
            Assert.That(picker.RepeatWeight, Is.EqualTo(weight));
            Assert.That(picker.RepeatChance, Is.EqualTo(weight / (3 + weight)).Within(1e-12));
        }
        Assert.That(picker.Next(0.999), Is.EqualTo(3));
        Assert.That(picker.ConsecutiveCount, Is.EqualTo(1));
        Assert.That(picker.RepeatWeight, Is.EqualTo(0.5));
    }

    [Test]
    public void WeightedIntervalsPenalizeOnlyTheLastVariantAndHistoriesAreIndependent()
    {
        var embroidery = new MiniGameVariantPicker(4);
        embroidery.Next(0);
        embroidery.Next(0);
        // Last=0 has weight .25, others 1: the first boundary is .25/3.25.
        Assert.That(embroidery.Next(0.08), Is.EqualTo(1));
        var polishing = new MiniGameVariantPicker(4);
        Assert.That(polishing.LastIndex, Is.EqualTo(-1));
        Assert.That(polishing.Next(0.08), Is.EqualTo(0));
        Assert.That(embroidery.LastIndex, Is.EqualTo(1));
    }

    [TestCase(-0.1)] [TestCase(1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
    public void InvalidRollsDoNotChangeHistory(double roll)
    {
        var picker = new MiniGameVariantPicker(4);
        Assert.Throws<ArgumentOutOfRangeException>(() => picker.Next(roll));
        Assert.That(picker.LastIndex, Is.EqualTo(-1));
    }
}
