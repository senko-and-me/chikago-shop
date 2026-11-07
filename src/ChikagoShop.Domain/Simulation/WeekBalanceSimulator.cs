using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Trading;

namespace ChikagoShop.Domain.Simulation;

public sealed record BalanceProfile(string Name, double CycleSeconds, bool Reroll, bool UnlockMusic,
    bool BuyUpgrades = false, bool Produce = true);
public sealed record BalanceSummary(string Profile, int Runs, double FirstQuotaPassRate, double WinRate,
    int CashBeforeFinalP10, int CashBeforeFinalMedian, int CashBeforeFinalP90, double MeanItems);

public static class WeekBalanceSimulator
{
    // CycleSeconds includes a full 10-second craft and player interaction, but not
    // the actual random buyer delay or 0.8 seconds per reroll. No offline earnings.
    public static readonly BalanceProfile[] Profiles =
    [
        new("Ordinary: immediate sales", 15, false, true),
        new("Careful: reroll below +5%", 15, true, true),
        new("Careful + category investments", 15, true, true, true),
        new("Slow: rerolls", 23, true, true),
        new("Clothing only: rerolls", 15, true, false),
        new("No production", 15, false, false, false, false)
    ];

    public static BalanceSummary Run(EconomyConfig config, BalanceProfile profile, int runs = 10000, int seed = 20261107)
    {
        if (runs <= 0 || profile.CycleSeconds < 10) throw new ArgumentOutOfRangeException(nameof(runs));
        EconomyConfigValidator.Validate(config);
        var atelier = config.Workshops.Single(workshop => workshop.Id == "atelier");
        var music = config.Workshops.Single(workshop => workshop.Id == "music");
        int firstDeadline = config.Quotas.Min(quota => quota.DeadlineDay);
        int commonWeight = config.Rarities.Single(rarity => rarity.Id == "common").Weight;
        int firstPassed = 0, won = 0, items = 0;
        var cashAtFinal = new int[runs];
        for (int run = 0; run < runs; run++)
        {
            var rng = new Random(unchecked(seed + run * 8191));
            var upgrades = new DayUpgradeSession();
            int cash = config.StartingMoney;
            bool unlocked = false;
            for (int day = 1; day <= config.Days.TotalDays; day++)
            {
                upgrades.Reset(preserveCategories: true);
                if (day > 1)
                {
                    upgrades.Offer(day);
                    DayUpgrade choice = DayUpgrade.None;
                    if (profile.BuyUpgrades)
                    {
                        var priorities = new[] { DayUpgrade.Cash,
                            unlocked ? DayUpgrade.Music : DayUpgrade.Atelier,
                            unlocked ? DayUpgrade.MusicPrice : DayUpgrade.AtelierPrice };
                        choice = priorities.FirstOrDefault(option => upgrades.Options.Contains(option) &&
                            cash - DayUpgradeSession.Cost(option) >= (unlocked ? music.SpinCost : atelier.SpinCost) * 2 &&
                            (option == DayUpgrade.Cash || day < config.Days.TotalDays));
                    }
                    if (choice != DayUpgrade.None && upgrades.TryPurchase(choice, cash, out int balance))
                    {
                        cash = balance;
                        if (choice == DayUpgrade.Cash) cash += upgrades.CashReward;
                    }
                    else upgrades.TrySkip();
                }
                // Delay the investment until the second day and retain money to craft.
                if (profile.Produce && profile.UnlockMusic && !unlocked && day >= 2 && cash >= music.UnlockCost + music.SpinCost + 25)
                { cash -= music.UnlockCost; unlocked = true; }
                int energy = upgrades.EnergyCapacityFor(unlocked ? config.Days.EnergyAfterMusicUnlock : config.Days.EnergyBeforeMusicUnlock);
                double remaining = config.Days.DurationSeconds + (upgrades.Selected == DayUpgrade.Time ? 30 : 0);
                var tables = new Dictionary<string, WeightedItemTable>();
                foreach (var workshop in config.Workshops)
                    tables[workshop.Id] = new WeightedItemTable(config.Rarities.Select((rarity, index) =>
                        (index, upgrades.Weight(workshop.Id, rarity.Id, rarity.Weight, commonWeight))));
                while (profile.Produce && energy > 0)
                {
                    var workshop = unlocked && cash >= music.SpinCost ? music : atelier;
                    double cycle = profile.CycleSeconds + 0.5 + rng.NextDouble() * 1.5;
                    if (cash < workshop.SpinCost || remaining < cycle) break;
                    remaining -= cycle;
                    cash -= workshop.SpinCost;
                    energy--;
                    items++;
                    var table = tables[workshop.Id];
                    var rarity = config.Rarities[table.Select(rng.Next(table.TotalWeight))];
                    int basePrice = upgrades.Price(workshop.Id, rarity.BasePrices[workshop.Id]);
                    int percent = TradeOfferRules.InitialPercent(rng.Next(61));
                    int rerolls = rarity.Rerolls;
                    while (profile.Reroll && percent < 5 && rerolls-- > 0)
                    {
                        var options = TradeOfferRules.RerollOptions(basePrice, TradeOfferRules.Price(basePrice, percent),
                            rng.NextDouble() < TradeOfferRules.HigherRerollChance);
                        percent = options[rng.Next(options.Length)];
                        remaining -= 0.8; // The final active trade can finish after closing.
                    }
                    cash += TradeOfferRules.Price(basePrice, percent);
                }
                if (day == config.Days.TotalDays) cashAtFinal[run] = cash;
                var quota = config.Quotas.SingleOrDefault(quota => quota.DeadlineDay == day);
                if (quota == null) continue;
                if (cash < quota.Amount) break;
                cash -= quota.Amount;
                if (day == firstDeadline) firstPassed++;
                if (day == config.Days.TotalDays) won++;
            }
        }
        Array.Sort(cashAtFinal);
        return new BalanceSummary(profile.Name, runs, (double)firstPassed / runs, (double)won / runs,
            cashAtFinal[runs / 10], cashAtFinal[runs / 2], cashAtFinal[Math.Min(runs - 1, runs * 9 / 10)], (double)items / runs);
    }
}
