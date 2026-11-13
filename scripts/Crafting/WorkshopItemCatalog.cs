using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;

public sealed record CraftItem(int BasePrice, string RarityId, string Rarity, string Kind, string Name,
    string ArtPath, string WorkshopId, int Rerolls);

// Append-only indices preserve the prototype's existing inventory and checkpoint saves.
public static class WorkshopItemCatalog
{
    private static readonly ChikagoShop.Domain.Configuration.EconomyConfig Config = EconomyConfigProvider.Load();
    public static IReadOnlyList<CraftItem> Items { get; } = System.Array.AsReadOnly(new[]
    {
        Item("atelier", "common", "hat", "шляпка-клош", "hat_cloche.png"),
        Item("atelier", "ordinary", "gloves", "перчатки", "gloves.png"),
        Item("music", "common", "guitar", "гитара", "guitar.png"),
        Item("music", "ordinary", "trumpet", "труба", "trumpet.svg"),
        Item("atelier", "first_class", "clothing_first", "первоклассная одежда", "placeholder_clothing_first.svg"),
        Item("atelier", "exclusive", "clothing_exclusive", "эксклюзивная одежда", "placeholder_clothing_exclusive.svg"),
        Item("atelier", "unique", "clothing_unique", "неповторимая одежда", "placeholder_clothing_unique.svg"),
        Item("music", "first_class", "music_first", "первоклассный инструмент", "violin.png"),
        Item("music", "exclusive", "music_exclusive", "эксклюзивный инструмент", "drums.png"),
        Item("music", "unique", "music_unique", "неповторимый инструмент", "placeholder_music_unique.svg")
    });
    private static readonly IReadOnlyDictionary<string, WeightedItemTable> Tables = Config.Workshops.ToDictionary(
        workshop => workshop.Id, workshop => new WeightedItemTable(Items.Select((item, index) => (item, index))
            .Where(entry => entry.item.WorkshopId == workshop.Id)
            .Select(entry => (entry.index, Config.Rarities.Single(rarity => rarity.Id == entry.item.RarityId).Weight))));

    public static int TotalWeight(string workshop) => Tables[workshop].TotalWeight;
    public static int CraftCost(string workshop) => Config.Workshops.Single(entry => entry.Id == workshop).SpinCost;
    public static int DayDurationSeconds => Config.Days.DurationSeconds;
    public static int Roll(string workshop, int ticket, DayUpgradeSession upgrades = null) => upgrades == null
        ? Tables[workshop].Select(ticket)
        : new WeightedItemTable(Items.Select((item, index) => (item, index))
            .Where(entry => entry.item.WorkshopId == workshop)
            .Select(entry => (entry.index, RarityWeight(workshop, entry.item.RarityId, upgrades)))).Select(ticket);
    public static int BaselineWeight(string rarityId) => Config.Rarities.Single(rarity => rarity.Id == rarityId).Weight;
    public static int RarityWeight(string workshop, string rarityId, DayUpgradeSession upgrades = null) =>
        upgrades?.Weight(workshop, rarityId, BaselineWeight(rarityId), BaselineWeight("common")) ?? BaselineWeight(rarityId);
    public static int RerollsFor(string rarityId) => Config.Rarities.Single(rarity => rarity.Id == rarityId).Rerolls;

    private static CraftItem Item(string workshop, string rarityId, string kind, string name, string art)
    {
        var rarity = Config.Rarities.Single(entry => entry.Id == rarityId);
        return new CraftItem(rarity.BasePrices[workshop], rarityId, rarity.Name, kind, name,
            "res://art/items/" + art, workshop, rarity.Rerolls);
    }
}
