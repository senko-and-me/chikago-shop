using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;

public sealed record CraftItem(int BasePrice, string RarityId, string Rarity, string Kind, string Name,
    string ArtPath, string WorkshopId, int Rerolls);

// Keep the canonical indices stable. Historical aliases are resolved by ProgressMigration.
public static class WorkshopItemCatalog
{
    public const int ActiveItemCount = 10;
    public static IReadOnlyList<CraftItem> ActiveItems => Items;
    private static ChikagoShop.Domain.Configuration.EconomyConfig Config => EconomyConfigProvider.Load();
    public static IReadOnlyList<CraftItem> BaselineItems { get; } = System.Array.AsReadOnly(new[]
    {
        Item("atelier", "common", "hat", "Шляпка"),
        Item("atelier", "ordinary", "vest", "Жилетка"),
        Item("music", "common", "guitar", "Гитара"),
        Item("music", "ordinary", "drum", "Барабан"),
        Item("atelier", "first_class", "gloves", "Перчатки"),
        Item("atelier", "exclusive", "matteo_hat", "Шляпа Маттео"),
        Item("atelier", "unique", "lorgnette", "Лорнет"),
        Item("music", "first_class", "banjo", "Банджо"),
        Item("music", "exclusive", "violin", "Скрипка"),
        Item("music", "unique", "tuba", "Туба"),
    });
    private static int _revision = -1;
    private static IReadOnlyList<CraftItem> _items;
    public static IReadOnlyList<CraftItem> Items
    {
        get
        {
            int revision = DebugBalanceSettings.Revision;
            if (_items != null && _revision == revision) return _items;
            var profile = DebugBalanceSettings.Profile;
            _items = System.Array.AsReadOnly(BaselineItems.Select(item =>
            {
                var rarity = Config.Rarities.Single(entry => entry.Id == profile.ItemRarities.GetValueOrDefault(item.Kind, item.RarityId));
                return item with { BasePrice = (int)profile.Number($"item.{item.Kind}.price", item.BasePrice),
                    RarityId = rarity.Id, Rarity = rarity.Name, Rerolls = rarity.Rerolls };
            }).ToArray());
            _revision = revision;
            return _items;
        }
    }
    private static RarityItemTable Table(string workshop, DayUpgradeSession upgrades = null, int hiddenShift = 0)
    {
        var entries = Items.Select((item, index) => (item, index))
            .Where(entry => entry.item.WorkshopId == workshop).ToArray();
        return new RarityItemTable(entries.Select(entry => (entry.index, entry.item.RarityId)),
            RarityWeights(workshop, upgrades, hiddenShift));
    }

    public static IReadOnlyDictionary<string, int> RarityWeights(string workshop, DayUpgradeSession upgrades = null, int hiddenShift = 0)
    {
        var available = ActiveItems.Where(item => item.WorkshopId == workshop).Select(item => item.RarityId).ToHashSet();
        var weights = Config.Rarities.ToDictionary(rarity => rarity.Id,
            rarity => available.Contains(rarity.Id) ? RarityWeight(workshop, rarity.Id, upgrades) : 0);
        ApplyHiddenShift(weights, hiddenShift);
        return weights;
    }

    private static void ApplyHiddenShift(Dictionary<string, int> weights, int requested) =>
        ChikagoShop.Domain.Economy.InvisibleHandDirector.ApplyRarityShift(weights, requested);

    // The selection table expands tickets to distribute each rarity across its items.
    // Use the original rarity-weight sum (not its expanded ticket count) for a percentage-point shift.
    public static int RarityWeightTotal(string workshop, DayUpgradeSession upgrades = null) =>
        Config.Rarities.Where(rarity => ActiveItems.Any(item => item.WorkshopId == workshop && item.RarityId == rarity.Id))
            .Sum(rarity => RarityWeight(workshop, rarity.Id, upgrades));
    public static int TotalWeight(string workshop, DayUpgradeSession upgrades = null, int hiddenShift = 0) =>
        Table(workshop, upgrades, hiddenShift).TotalWeight;
    public static int CraftCost(string workshop) => Config.Workshops.Single(entry => entry.Id == workshop).SpinCost;
    public static int DayDurationSeconds => Config.Days.DurationSeconds;
    public static int Roll(string workshop, int ticket, DayUpgradeSession upgrades = null, int hiddenShift = 0) =>
        Table(workshop, upgrades, hiddenShift).Select(ticket);
    public static (int ItemIndex, string InstrumentKind) PreparePolishing(int ticket, DayUpgradeSession upgrades = null, int hiddenShift = 0)
    {
        int index = Roll("music", ticket, upgrades, hiddenShift);
        // The polishing silhouette uses the actual displayed artwork, not the internal legacy Kind ID.
        return (index, Items[index].Kind);
    }
    public static int BaselineWeight(string rarityId) => Config.Rarities.Single(rarity => rarity.Id == rarityId).Weight;
    public static int RarityWeight(string workshop, string rarityId, DayUpgradeSession upgrades = null) =>
        upgrades?.Weight(workshop, rarityId, BaselineWeight(rarityId), BaselineWeight("common")) ?? BaselineWeight(rarityId);
    public static int RerollsFor(string rarityId) => Config.Rarities.Single(rarity => rarity.Id == rarityId).Rerolls;

    private static CraftItem Item(string workshop, string rarityId, string kind, string name)
    {
        var rarity = EconomyConfigProvider.Baseline.Rarities.Single(entry => entry.Id == rarityId);
        return new CraftItem(rarity.BasePrices[workshop], rarityId, rarity.Name, kind, name,
            $"res://art/items/{kind}.png", workshop, rarity.Rerolls);
    }
}
