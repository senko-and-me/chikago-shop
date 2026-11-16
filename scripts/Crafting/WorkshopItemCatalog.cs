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
        Item("atelier", "common", "hat", "кепка", "cap.png"),
        Item("atelier", "ordinary", "gloves", "рубашка", "shirt.png"),
        Item("music", "common", "guitar", "маракасы", "maracas.png"),
        Item("music", "ordinary", "trumpet", "метроном", "metronome.png"),
        Item("atelier", "first_class", "clothing_first", "перчатки", "gloves.png"),
        Item("atelier", "exclusive", "clothing_exclusive", "пиджак", "jacket.png"),
        Item("atelier", "unique", "clothing_unique", "меховая накидка", "fur_stole.png"),
        Item("music", "first_class", "music_first", "труба", "trumpet.png"),
        Item("music", "exclusive", "music_exclusive", "скрипка", "violin.png"),
        Item("music", "unique", "music_unique", "граммофон", "gramophone.png"),
        Item("atelier", "ordinary", "cloche_hat", "шляпка-клош", "cloche_hat.png"),
        Item("atelier", "first_class", "shoes", "туфли", "shoes.png"),
        Item("atelier", "exclusive", "dress", "платье", "dress.png"),
        Item("music", "common", "tambourine", "бубен", "tambourine.png"),
        Item("music", "exclusive", "saxophone", "саксофон", "saxophone.png"),
        Item("music", "unique", "cello", "виолончель", "cello.png")
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
    private static RarityItemTable Table(string workshop, DayUpgradeSession upgrades = null) => new(
        Items.Select((item, index) => (item, index)).Where(entry => entry.item.WorkshopId == workshop)
            .Select(entry => (entry.index, entry.item.RarityId)),
        Config.Rarities.ToDictionary(rarity => rarity.Id, rarity => RarityWeight(workshop, rarity.Id, upgrades)));
    public static int TotalWeight(string workshop, DayUpgradeSession upgrades = null) => Table(workshop, upgrades).TotalWeight;
    public static int CraftCost(string workshop) => Config.Workshops.Single(entry => entry.Id == workshop).SpinCost;
    public static int DayDurationSeconds => Config.Days.DurationSeconds;
    public static int Roll(string workshop, int ticket, DayUpgradeSession upgrades = null) => Table(workshop, upgrades).Select(ticket);
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
