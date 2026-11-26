using System.Text.Json;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class ProgressMigrationTests
{
    [Test]
    public void Version18CatalogueAliasesPreserveStockGradesAndCheckpointAcrossRewrite()
    {
        var legacy = new ProgressData
        {
            SaveVersion = 18, Day = 8, Score = 2333, QuotaStage = 1, ItemIndex = 15,
            StoredItems = [10, 11, 12, 13, 14, 15], StoredGrades = ["S", "A", "B", "C", "D", "S"],
            ShowcaseItems = [-1, 10, 11, 12, 13, 14], CheckpointItems = [15, 0, 11],
            CheckpointShowcaseItems = [10, 12, -1], TradeActive = true, ActiveTradeGrade = "A"
        };
        var migrated = ProgressMigration.Prepare(legacy);
        Assert.Multiple(() =>
        {
            Assert.That(migrated.StoredItems, Is.EqualTo(new[] { 1, 4, 5, 2, 8, 9 }));
            Assert.That(migrated.ShowcaseItems, Is.EqualTo(new[] { -1, 1, 4, 5, 2, 8 }));
            Assert.That(migrated.CheckpointItems, Is.EqualTo(new[] { 9, 0, 4 }));
            Assert.That(migrated.CheckpointShowcaseItems, Is.EqualTo(new[] { 1, 5, -1 }));
            Assert.That(migrated.ItemIndex, Is.EqualTo(9));
            Assert.That(migrated.StoredGrades, Is.EqualTo(legacy.StoredGrades));
            Assert.That(migrated.Score, Is.EqualTo(2333));
            Assert.That(migrated.TradeActive, Is.True);
            Assert.That(migrated.ActiveTradeGrade, Is.EqualTo("A"));
        });
        migrated.SaveVersion = ProgressJson.CurrentVersion;
        var reread = ProgressMigration.Prepare(ProgressJson.Deserialize(ProgressJson.Serialize(migrated))!);
        Assert.That(reread.StoredItems, Is.EqualTo(migrated.StoredItems));
        Assert.That(reread.CheckpointItems, Is.EqualTo(migrated.CheckpointItems));
        Assert.That(reread.ItemIndex, Is.EqualTo(migrated.ItemIndex));
        Assert.That(legacy.StoredItems, Is.EqualTo(new[] { 10, 11, 12, 13, 14, 15 }), "Migration must leave the source DTO intact");
    }

    [Test]
    public void Version19CanonicalIndicesNeverRemapAgain()
    {
        int[] items = Enumerable.Range(0, 10).ToArray();
        var prepared = ProgressMigration.Prepare(new ProgressData { SaveVersion = 19, StoredItems = items, ItemIndex = 9 });
        Assert.That(prepared.StoredItems, Is.EqualTo(items));
        Assert.That(prepared.ItemIndex, Is.EqualTo(9));
    }

    [Test]
    public void MissingDayStartedPreservesLegacyRunningDay()
    {
        var legacy = ProgressJson.Deserialize("{\"save_version\":13,\"day_remaining_seconds\":42}")!;
        Assert.That(ProgressMigration.Prepare(legacy).DayStarted, Is.True);
        foreach (bool started in new[] { false, true })
        {
            var restored = ProgressMigration.Prepare(ProgressJson.Deserialize(
                ProgressJson.Serialize(new ProgressData { SaveVersion = ProgressJson.CurrentVersion, DayStarted = started }))!);
            Assert.That(restored.DayStarted, Is.EqualTo(started));
        }
    }

    [TestCase(1, false, false, false, 0, null)]
    [TestCase(7, true, false, false, 0, null)]
    [TestCase(8, true, true, false, 0, 60)]
    [TestCase(9, true, true, true, (int)DayUpgrade.Time, 80)]
    [TestCase(12, true, true, true, (int)DayUpgrade.Time, 80)]
    [TestCase(13, true, true, true, (int)DayUpgrade.Time, 80)]
    public void LegacyFieldGatesAndTimeConversionRemainCompatible(int version, bool music,
        bool closed, bool pending, int selected, double? remaining)
    {
        var source = new ProgressData
        {
            SaveVersion = version, MusicWorkshopUnlocked = true, CheckpointMusicWorkshopUnlocked = true,
            DayClosed = true, UpgradePending = true, SelectedUpgrade = (int)DayUpgrade.Time,
            DayRemainingSeconds = version == 8 ? 60 : 80
        };
        var restored = ProgressMigration.Prepare(source);
        Assert.Multiple(() =>
        {
            Assert.That(restored.MusicWorkshopUnlocked, Is.EqualTo(music));
            Assert.That(restored.CheckpointMusicWorkshopUnlocked, Is.EqualTo(music));
            Assert.That(restored.DayClosed, Is.EqualTo(closed));
            Assert.That(restored.UpgradePending, Is.EqualTo(pending));
            Assert.That(restored.SelectedUpgrade, Is.EqualTo(selected));
            Assert.That(restored.DayRemainingSeconds, Is.EqualTo(remaining));
            Assert.That(source.SelectedUpgrade, Is.EqualTo((int)DayUpgrade.Time), "Migration must not alter its input");
        });
    }

    [TestCase(8, false, 60, 45)]
    [TestCase(10, true, 60, 45)]
    [TestCase(11, false, 30, 45)]
    [TestCase(12, true, 45, 45)]
    [TestCase(13, false, 30, 30)]
    public void OpenDaysKeepTheirHistoricalElapsedFraction(int version, bool music, double saved, double expected)
    {
        var restored = ProgressMigration.Prepare(new ProgressData
        {
            SaveVersion = version, MusicWorkshopUnlocked = music, DayRemainingSeconds = saved
        });
        Assert.That(restored.DayRemainingSeconds, Is.EqualTo(expected));
    }

    [Test]
    public void LegacyCustomersAndItemRerollsRemainCompatible()
    {
        var legacy = new ProgressData { CustomerIndex = 1, ItemIndex = 0, RerollsRemaining = 2 };
        Assert.That(ProgressMigration.CustomerIndex(legacy), Is.EqualTo(3));
        Assert.That(ProgressMigration.Rerolls(legacy, 0), Is.Zero);
        Assert.That(ProgressMigration.Rerolls(legacy, 3), Is.EqualTo(3));
        Assert.That(ProgressMigration.Rerolls(legacy, -1), Is.EqualTo(2));
        Assert.That(ProgressMigration.CustomerIndex(legacy with { SaveVersion = 3 }), Is.EqualTo(1));
        Assert.That(ProgressMigration.Rerolls(legacy with { SaveVersion = 2, RerollsRemaining = 10 }, 1), Is.EqualTo(10));
    }

    [Test]
    public void Version20SerializesDisplayStockAndIgnoresLegacyEnergy()
    {
        var legacy = ProgressJson.Deserialize("{\"energy_remaining\":3,\"stored_items\":[0],\"stored_grades\":[\"S\"]}")!;
        Assert.That(legacy.StoredItems, Is.EqualTo(new[] { 0 }));
        Assert.That(legacy.StoredGrades, Is.EqualTo(new[] { "S" }));
        var current = new ProgressData
        {
            SaveVersion = ProgressJson.CurrentVersion, ShowcaseItems = [0, -1, -1],
            ShowcaseGrades = ["A", "D", "D"], ShowcaseRerolls = [1, -1, -1],
            DeferredDisplayItems = [new(6, "S", 1)],
            CheckpointDeferredDisplayItems = [new(2, "B", 2)],
            ReturnedOfferPercent = 12, QuotaVisit = QuotaVisitStage.Rewarded,
            MusicWorkshopUnlocked = true
        };
        string json = ProgressJson.Serialize(current);
        Assert.That(json, Does.Not.Contain("energy").And.Not.Contain("stored_items"));
        Assert.That(ProgressJson.Serialize(ProgressJson.Deserialize(json)!), Is.EqualTo(json));
        Assert.That(ProgressJson.Deserialize("null"), Is.Null);
        Assert.Throws<JsonException>(() => ProgressJson.Deserialize("{broken"));
    }
}
