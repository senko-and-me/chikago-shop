using ChikagoShop.Domain.Persistence;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class StoryWorkshopUnlockTests
{
    [TestCase(1, 0, QuotaVisitStage.Unavailable, false)]
    [TestCase(7, 0, QuotaVisitStage.Rewarded, false)]
    [TestCase(8, 0, QuotaVisitStage.Rewarded, false)]
    [TestCase(8, 1, QuotaVisitStage.AwaitingVisitor, false)]
    [TestCase(8, 1, QuotaVisitStage.Dialogue, false)]
    [TestCase(8, 1, QuotaVisitStage.Rewarded, true)]
    [TestCase(10, 1, QuotaVisitStage.Unavailable, true)]
    [TestCase(14, 2, QuotaVisitStage.Rewarded, true)]
    public void RestoreRequiresCompletedFirstWeekAndConversation(int day, int stage, QuotaVisitStage visit, bool expected) =>
        Assert.That(ProgressMigration.StoryWorkshopUnlocked(false, day, stage, visit, 7), Is.EqualTo(expected));

    [Test]
    public void PreviouslyPurchasedAccessIsPreserved() =>
        Assert.That(ProgressMigration.StoryWorkshopUnlocked(true, 2, 0, QuotaVisitStage.Unavailable, 7), Is.True);
}
