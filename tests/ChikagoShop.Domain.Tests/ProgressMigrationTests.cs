using System.Text.Json;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class ProgressMigrationTests
{
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

    [TestCase(5, -50, 3, false, 3)]
    [TestCase(6, 0, 3, false, 2)]
    [TestCase(7, 1, 6, true, 5)]
    [TestCase(11, -100, 6, true, 0)]
    [TestCase(11, 100, 6, true, 6)]
    [TestCase(12, 2, 3, false, 2)]
    [TestCase(13, 4, 4, false, 4)]
    public void EnergyMigrationPreservesSpentEnergy(int version, int saved, int capacity, bool music, int expected) =>
        Assert.That(ProgressMigration.Energy(version, saved, capacity, music), Is.EqualTo(expected));

    [Test]
    public void LegacyCustomersAndItemRerollsRemainCompatible()
    {
        var legacy = new ProgressData { CustomerIndex = 1, ItemIndex = 0, RerollsRemaining = 2 };
        Assert.That(ProgressMigration.CustomerIndex(legacy), Is.EqualTo(3));
        Assert.That(ProgressMigration.Rerolls(legacy, 0), Is.Zero);
        Assert.That(ProgressMigration.Rerolls(legacy, 3), Is.EqualTo(3));
        Assert.That(ProgressMigration.Rerolls(legacy, -1), Is.EqualTo(2));
        Assert.That(ProgressMigration.CustomerIndex(legacy with { SaveVersion = 3 }), Is.EqualTo(1));
        Assert.That(ProgressMigration.Rerolls(legacy with { SaveVersion = 2, RerollsRemaining = 10 }, 1), Is.EqualTo(3));
    }

    [Test]
    public void JsonRoundTripKeepsVersion13WireFieldsAndCheckpointState()
    {
        var data = new ProgressData
        {
            SaveVersion = ProgressJson.CurrentVersion, Day = 5, Score = 321, EnergyRemaining = 4,
            CheckpointDay = 5, CheckpointScore = 100, CheckpointEnergy = 5,
            MusicWorkshopUnlocked = true, CheckpointMusicWorkshopUnlocked = true,
            StoredItems = [0, 4, 9], CheckpointItems = [2, 3], ShowcaseItems = [1, -1, 8],
            CheckpointShowcaseItems = [-1, 7], TradeActive = true, ItemIndex = 8,
            CustomerIndex = 3, RerollsRemaining = 1, ActiveShowcaseIndex = 2, BuyerCountdown = 1.25,
            DayRemainingSeconds = 35.5, UpgradePending = true, SelectedUpgrade = (int)DayUpgrade.Energy,
            AtelierUpgradeLevel = 2, MusicUpgradeLevel = 3, AtelierPriceLevel = 4, MusicPriceLevel = 5,
            CheckpointAtelierUpgradeLevel = 1, CheckpointMusicUpgradeLevel = 2,
            CheckpointAtelierPriceLevel = 3, CheckpointMusicPriceLevel = 4
        };
        string json = ProgressJson.Serialize(data);
        var restored = ProgressJson.Deserialize(json, 3)!;
        Assert.That(ProgressJson.Serialize(restored), Is.EqualTo(json));
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("save_version").GetInt32(), Is.EqualTo(13));
        Assert.That(doc.RootElement.GetProperty("checkpoint_music_price_level").GetInt32(), Is.EqualTo(4));
        Assert.That(doc.RootElement.GetProperty("day_remaining_seconds").GetDouble(), Is.EqualTo(35.5));
        string[] fields = [
            "save_version", "upgrade_pending", "selected_upgrade", "atelier_price_level", "music_price_level",
            "checkpoint_atelier_price_level", "checkpoint_music_price_level", "atelier_upgrade_level",
            "music_upgrade_level", "checkpoint_atelier_upgrade_level", "checkpoint_music_upgrade_level",
            "day_remaining_seconds", "day_closed", "day", "score", "energy_remaining", "accepted", "declined",
            "quota_stage", "checkpoint_day", "checkpoint_score", "checkpoint_energy", "music_workshop_unlocked",
            "checkpoint_music_workshop_unlocked", "checkpoint_accepted", "checkpoint_declined", "failed", "completed",
            "status", "trade_active", "item_index", "customer_index", "percentage", "rerolls_remaining", "stored_items",
            "checkpoint_items", "showcase_items", "checkpoint_showcase_items", "active_showcase_index", "buyer_countdown"
        ];
        Assert.That(doc.RootElement.EnumerateObject().Select(property => property.Name),
            Is.EquivalentTo(fields), "Keep the exact existing save schema");
    }

    [Test]
    public void MissingFieldsUseEconomyDefaultsAndInvalidJsonIsRejected()
    {
        var data = ProgressJson.Deserialize("{}", 6)!;
        Assert.That(data.SaveVersion, Is.EqualTo(1));
        Assert.That(data.EnergyRemaining, Is.EqualTo(6));
        Assert.That(data.CheckpointEnergy, Is.EqualTo(6));
        Assert.That(data.ItemIndex, Is.EqualTo(-1));
        Assert.That(data.StoredItems, Is.Empty);
        Assert.That(ProgressJson.Deserialize("{\"energy_remaining\":0,\"checkpoint_energy\":0}", 6)!.EnergyRemaining, Is.Zero);
        Assert.That(ProgressJson.Deserialize("null", 3), Is.Null);
        Assert.Throws<JsonException>(() => ProgressJson.Deserialize("{broken", 3));
        Assert.Throws<JsonException>(() => ProgressJson.Deserialize("[]", 3));
    }
}
