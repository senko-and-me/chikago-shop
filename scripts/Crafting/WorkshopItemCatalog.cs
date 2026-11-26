using System;
using System.Collections.Generic;
using System.Linq;

public sealed record CraftItem(int BasePrice, string Kind, string Name,
    string ArtPath, string WorkshopId, int Rerolls);

// Keep the canonical indices stable. Historical aliases are resolved by ProgressMigration.
public static class WorkshopItemCatalog
{
    public const int ActiveItemCount = 10;
    public static IReadOnlyList<CraftItem> ActiveItems => Items;
    private static ChikagoShop.Domain.Configuration.EconomyConfig Config => EconomyConfigProvider.Load();
    public static IReadOnlyList<CraftItem> BaselineItems { get; } = System.Array.AsReadOnly(new[]
    {
        Item("hat", "Шляпка"),
        Item("vest", "Жилетка"),
        Item("guitar", "Гитара"),
        Item("drum", "Барабан"),
        Item("gloves", "Перчатки"),
        Item("matteo_hat", "Шляпа Маттео"),
        Item("lorgnette", "Лорнет"),
        Item("banjo", "Банджо"),
        Item("violin", "Скрипка"),
        Item("tuba", "Туба"),
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
            _items = Array.AsReadOnly(BaselineItems.Select(item => item with
            {
                BasePrice = (int)profile.Number($"item.{item.Kind}.price", item.BasePrice)
            }).ToArray());
            _revision = revision;
            return _items;
        }
    }
    public static int CraftCost(string workshop) => Config.Workshops.Single(entry => entry.Id == workshop).SpinCost;
    public static int DayDurationSeconds => Config.Days.DurationSeconds;
    private static readonly Dictionary<string, int[]> ItemIndices = BaselineItems
        .Select((item, index) => (item, index)).GroupBy(entry => entry.item.WorkshopId)
        .ToDictionary(group => group.Key, group => group.Select(entry => entry.index).ToArray());
    public static int Count(string workshop) => ItemIndices[workshop].Length;
    public static int Roll(string workshop, int ticket) => ItemIndices[workshop][ticket];

    private static CraftItem Item(string kind, string name)
    {
        var item = EconomyConfigProvider.Baseline.Items.Single(entry => entry.Id == kind);
        return new CraftItem(item.BasePrice, kind, name, $"res://art/items/{kind}.png", item.Workshop, item.Rerolls);
    }
}
