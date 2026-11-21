using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;

public sealed record CraftItem(int BasePrice, string RarityId, string Rarity, string Kind, string Name,
    string ArtPath, string WorkshopId, int Rerolls);

// Append-only indices preserve the prototype's existing inventory and checkpoint saves.
public static class WorkshopItemCatalog
{
    private static ChikagoShop.Domain.Configuration.EconomyConfig Config => EconomyConfigProvider.Load();
    public static IReadOnlyList<CraftItem> BaselineItems { get; } = System.Array.AsReadOnly(new[]
    {
        // Preserve the first ten save indices, internal kinds and rarity/price tiers.
        Item("atelier", "common", "hat", "Шляпка", "atelier_hat.png"),
        Item("atelier", "ordinary", "gloves", "Жилетка", "atelier_vest.png"),
        Item("music", "common", "guitar", "банджо", "maracas.png"),
        Item("music", "ordinary", "trumpet", "гитара", "metronome.png"),
        Item("atelier", "first_class", "clothing_first", "Перчатки", "atelier_gloves.png"),
        Item("atelier", "exclusive", "clothing_exclusive", "шляпа Маттео", "jacket.png"),
        Item("atelier", "unique", "clothing_unique", "лорнет", "fur_stole.png"),
        Item("music", "first_class", "music_first", "туба", "trumpet.png"),
        Item("music", "exclusive", "music_exclusive", "скрипка", "violin.png"),
        Item("music", "unique", "music_unique", "барабан", "gramophone.png"),
        Item("atelier", "ordinary", "cloche_hat", "Жилетка", "atelier_vest.png"),
        Item("atelier", "first_class", "shoes", "Перчатки", "atelier_gloves.png"),
        Item("atelier", "exclusive", "dress", "платье", "dress.png"),
        Item("music", "common", "tambourine", "гитара", "tambourine.png"),
        Item("music", "exclusive", "saxophone", "скрипка", "saxophone.png"),
        Item("music", "unique", "cello", "барабан", "cello.png")
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
        var available = Items.Where(item => item.WorkshopId == workshop).Select(item => item.RarityId).ToHashSet();
        var weights = Config.Rarities.ToDictionary(rarity => rarity.Id,
            rarity => available.Contains(rarity.Id) ? RarityWeight(workshop, rarity.Id, upgrades) : 0);
        ApplyHiddenShift(weights, hiddenShift);
        return weights;
    }

    private static void ApplyHiddenShift(Dictionary<string, int> weights, int requested)
    {
        if (requested == 0 || !weights.TryGetValue("common", out int common) || common <= 0) return;
        // Shift only existing, enabled rarity tiers. A player's card is applied first.
        string[] rare = ["first_class", "exclusive", "unique"];
        var enabled = rare.Where(id => weights.GetValueOrDefault(id) > 0).ToArray();
        if (enabled.Length == 0) return;
        if (requested > 0)
        {
            int amount = Math.Min(requested, Math.Max(0, common - 1));
            weights["common"] -= amount;
            for (int i = 0; i < amount; i++) weights[enabled[i % enabled.Length]]++;
        }
        else
        {
            int amount = Math.Min(-requested, enabled.Sum(id => Math.Max(0, weights[id] - 1)));
            // Preserve at least one ticket of each available rare tier.
            for (int i = 0; i < amount; i++)
            {
                string id = enabled.OrderByDescending(key => weights[key]).First();
                if (weights[id] <= 1) break;
                weights[id]--;
                weights["common"]++;
            }
        }
    }

    // The selection table expands tickets to distribute each rarity across its items.
    // Use the original rarity-weight sum (not its expanded ticket count) for a percentage-point shift.
    public static int RarityWeightTotal(string workshop, DayUpgradeSession upgrades = null) =>
        Config.Rarities.Where(rarity => Items.Any(item => item.WorkshopId == workshop && item.RarityId == rarity.Id))
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
        // Legacy save kinds do not always match the pictured instrument (for example guitar is maracas).
        return (index, System.IO.Path.GetFileNameWithoutExtension(Items[index].ArtPath));
    }
    public static int BaselineWeight(string rarityId) => Config.Rarities.Single(rarity => rarity.Id == rarityId).Weight;
    public static int RarityWeight(string workshop, string rarityId, DayUpgradeSession upgrades = null) =>
        upgrades?.Weight(workshop, rarityId, BaselineWeight(rarityId), BaselineWeight("common")) ?? BaselineWeight(rarityId);
    public static int RerollsFor(string rarityId) => Config.Rarities.Single(rarity => rarity.Id == rarityId).Rerolls;

    private static CraftItem Item(string workshop, string rarityId, string kind, string name, string art)
    {
        var rarity = EconomyConfigProvider.Baseline.Rarities.Single(entry => entry.Id == rarityId);
        return new CraftItem(rarity.BasePrices[workshop], rarityId, rarity.Name, kind, name,
            "res://art/items/" + art, workshop, rarity.Rerolls);
    }
}
