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
    public void PermissionAndOpenedStateRestoreIndependently(int day, int stage, QuotaVisitStage visit, bool expected)
    {
        var restoredVisit = ProgressMigration.RestoreQuotaVisit(day, stage, visit, 7);
        Assert.That(day > 7 && stage >= 1 && restoredVisit == QuotaVisitStage.Rewarded, Is.EqualTo(expected));
        Assert.That(ProgressMigration.StoryWorkshopUnlocked(false, day, stage, visit, 7), Is.False);
        Assert.That(ProgressMigration.StoryWorkshopUnlocked(true, day, stage, visit, 7), Is.EqualTo(expected));
    }
}
