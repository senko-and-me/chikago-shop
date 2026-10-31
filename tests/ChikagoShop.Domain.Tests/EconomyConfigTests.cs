using System.Text.Json.Nodes;
using ChikagoShop.Domain.Configuration;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class EconomyConfigTests
{
    private static string ConfigPath => Path.Combine(
        TestContext.CurrentContext.TestDirectory, "config", "economy.v1.json");

    [Test]
    public void CanonicalConfigLoads()
    {
        EconomyConfig config = EconomyConfigLoader.Load(File.ReadAllText(ConfigPath));

        Assert.Multiple(() =>
        {
            Assert.That(config.SchemaVersion, Is.EqualTo(1));
            Assert.That(config.IsProvisional, Is.True);
            Assert.That(config.Days.TotalDays, Is.EqualTo(7));
            Assert.That(config.StartingMoney, Is.EqualTo(220));
            Assert.That(config.Workshops.Select(value => value.Id), Is.EqualTo(new[] { "atelier", "music" }));
            Assert.That(config.Workshops.Select(value => value.SpinCost), Is.EqualTo(new[] { 90, 140 }));
            Assert.That(config.Workshops.Select(value => value.UnlockCost), Is.EqualTo(new[] { 0, 100 }));
            Assert.That(config.Quotas.Select(value => value.Amount), Is.EqualTo(new[] { 200, 400 }));
            Assert.That(config.Rarities.Select(value => value.Weight), Is.EqualTo(new[] { 60, 25, 10, 4, 1 }));
            Assert.That(config.OfferRanges.Sum(value => value.ChancePercent), Is.EqualTo(100));
        });
    }

    [Test]
    public void UnknownSchemaIsRejected()
    {
        JsonObject json = ReadConfig();
        json["schema_version"] = 99;

        Assert.That(() => EconomyConfigLoader.Load(json.ToJsonString()),
            Throws.TypeOf<EconomyConfigException>());
    }

    [Test]
    public void DuplicateIdsAreRejected()
    {
        JsonObject json = ReadConfig();
        JsonArray workshops = json["workshops"]!.AsArray();
        workshops.Add(workshops[0]!.DeepClone());

        Assert.That(() => EconomyConfigLoader.Load(json.ToJsonString()),
            Throws.TypeOf<EconomyConfigException>());
    }

    [Test]
    public void NegativePricesAreRejected()
    {
        JsonObject json = ReadConfig();
        json["workshops"]![0]!["spin_cost"] = -1;

        Assert.That(() => EconomyConfigLoader.Load(json.ToJsonString()),
            Throws.TypeOf<EconomyConfigException>());

		json = ReadConfig();
		json["workshops"]![1]!["unlock_cost"] = -1;
		Assert.That(() => EconomyConfigLoader.Load(json.ToJsonString()),
			Throws.TypeOf<EconomyConfigException>());
    }

    [Test]
    public void PercentageDistributionMustSumToOneHundred()
    {
        JsonObject json = ReadConfig();
        json["offer_ranges"]![0]!["chance_percent"] = 19;

        Assert.That(() => EconomyConfigLoader.Load(json.ToJsonString()),
            Throws.TypeOf<EconomyConfigException>());
    }

    [Test]
    public void RelativeWeightsNeedNotSumToOneHundred()
    {
        JsonObject json = ReadConfig();
        json["rarities"]![0]!["weight"] = 4;
        json["rarities"]![1]!["weight"] = 1;

        Assert.DoesNotThrow(() => EconomyConfigLoader.Load(json.ToJsonString()));
        json["rarities"]![1]!["weight"] = 0;
        Assert.That(() => EconomyConfigLoader.Load(json.ToJsonString()),
            Throws.TypeOf<EconomyConfigException>());
    }

    private static JsonObject ReadConfig() =>
        JsonNode.Parse(File.ReadAllText(ConfigPath))!.AsObject();
}
