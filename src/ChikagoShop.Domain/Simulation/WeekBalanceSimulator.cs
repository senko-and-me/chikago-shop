using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Economy;
using ChikagoShop.Domain.Trading;

namespace ChikagoShop.Domain.Simulation;

public sealed record BalanceProfile(string Name, double CycleSeconds, bool Reroll, bool UnlockMusic,
    bool BuyUpgrades = false, bool Produce = true, string QualityRank = "B", bool HiddenBalance = true);
public sealed record BalanceSummary(string Profile, int Runs, double FirstQuotaPassRate, double WinRate,
    int CashBeforeFinalP10, int CashBeforeFinalMedian, int CashBeforeFinalP90, double MeanItems);

public static class WeekBalanceSimulator
{
    // A cycle includes crafting and player interaction. The buyer waits
    // throughout the cycle; rerolls consume another 0.8 seconds each.
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

        int firstPassed = 0, won = 0, items = 0;
        var cashAtFinal = new int[runs];
        for (int run = 0; run < runs; run++)
        {
            var rng = new Random(unchecked(seed + run * 8191));
            var upgrades = new DayUpgradeSession { Rules = new DayUpgradeRules { Terms = config.Cards } };
            int cash = config.StartingMoney;
            int checkpointCash = cash;
            var hand = new InvisibleHandDirector();
            bool unlocked = false;
            for (int day = 1; day <= config.Days.TotalDays; day++)
            {
                // Assume the first-quota story is completed before week two.
                unlocked = day > firstDeadline;
                upgrades.Reset(preserveCategories: true);
                if (day > 1)
                {
                    upgrades.Offer(day, unlocked);
                    DayUpgrade choice = DayUpgrade.None;
                    if (profile.BuyUpgrades)
                    {
                        var priorities = new[] { DayUpgrade.Cash,
                            unlocked ? DayUpgrade.MusicPrice : DayUpgrade.AtelierPrice,
                            unlocked ? DayUpgrade.MusicPrice : DayUpgrade.AtelierPrice };
                        choice = priorities.FirstOrDefault(option => upgrades.Options.Contains(option) &&
                            cash - upgrades.CostFor(option) >= config.CraftCost * 2 &&
                            (option == DayUpgrade.Cash || day < config.Days.TotalDays));
                    }
                    if (choice != DayUpgrade.None && upgrades.TryPurchase(choice, cash, out int balance))
                    {
                        cash = balance;
                        if (choice == DayUpgrade.Cash) cash += upgrades.CashReward;
                    }
                    else upgrades.TrySkip();
                }
                double remaining = config.Days.DurationFor(day, firstDeadline);
                double duration = remaining;
                void Observe(double stock = 0)
                {
                    bool first = day <= firstDeadline;
                    var quota = config.Quotas.Single(entry => entry.DeadlineDay == (first ? firstDeadline : config.Days.TotalDays));
                    hand.Observe(cash, stock, checkpointCash, day, first ? 1 : firstDeadline + 1,
                        quota.DeadlineDay, 1 - remaining / duration, quota.Amount, profile.HiddenBalance);
                }
                Observe();
                while (profile.Produce && remaining > 0)
                {
                    var workshop = unlocked && profile.UnlockMusic && rng.Next(2) == 0 ? music : atelier;
                    int cost = upgrades.CraftPrice(config.CraftCost);
                    if (cash < cost) break;
                    double cycle = profile.CycleSeconds;
                    if (!double.IsFinite(cycle) || cycle <= 0 || remaining < cycle) break;
                    Observe();
                    if (!config.Items.Any(item => item.Workshop == workshop.Id)) break;
                    remaining -= cycle;
                    cash -= cost;
                    items++;
                    int basePrice = upgrades.Price(workshop.Id, config.BasePrice);
                    Observe(basePrice);
                    int percent = hand.AdjustInitialOffer(TradeOfferRules.InitialPercent(rng.Next(61)), 3);
                    double waitDuration = rng.Next(config.BuyerWaitMinSeconds, config.BuyerWaitMaxSeconds + 1) + upgrades.WaitExtension;
                    double buyerRemaining = Math.Max(0, waitDuration - cycle);
                    int rerolls = config.RerollsPerItem;
                    while (profile.Reroll && percent < 5 &&
                        BuyerPatienceRules.CanReroll(true, false, rerolls, buyerRemaining))
                    {
                        bool guaranteed = upgrades.GuaranteesReroll(rerolls);
                        rerolls--;
                        var options = TradeOfferRules.RerollOptions(basePrice, TradeOfferRules.Price(basePrice, percent),
                            rng.NextDouble() < TradeOfferRules.HigherRerollChance);
                        if (guaranteed)
                        {
                            int minimum = upgrades.MinimumFinalOffer(percent);
                            options = options.Where(option => option >= minimum).DefaultIfEmpty(minimum).ToArray();
                        }
                        if (options.Length == 0) break;
                        percent = options[rng.Next(options.Length)];
                        remaining -= 0.8; // The final active trade can finish after closing.
                        buyerRemaining -= 0.8;
                    }
                    buyerRemaining = BuyerPatienceRules.RemainingAfterDayClosure(buyerRemaining, remaining <= 0);
                    if (buyerRemaining <= 0) percent = upgrades.ExpiredOffer(percent);
                    cash += BuyerPatienceRules.TotalPayout(TradeOfferRules.Price(basePrice, percent), buyerRemaining,
                        RunRewardRules.SpeedBonus(buyerRemaining, waitDuration), RunRewardRules.QualityBonus(profile.QualityRank) +
                        upgrades.SaleBonus(workshop.Id, profile.QualityRank, buyerRemaining));
                    Observe();
                }
                if (day == config.Days.TotalDays) cashAtFinal[run] = cash;
                var quota = config.Quotas.SingleOrDefault(quota => quota.DeadlineDay == day);
                if (quota == null) continue;
                if (cash < quota.Amount) break;
                cash -= quota.Amount;
                if (day == firstDeadline)
                {
                    firstPassed++;
                    checkpointCash = cash;
                    hand.Reset();
                }
                if (day == config.Days.TotalDays) won++;
            }
        }
        Array.Sort(cashAtFinal);
        return new BalanceSummary(profile.Name, runs, (double)firstPassed / runs, (double)won / runs,
            cashAtFinal[runs / 10], cashAtFinal[runs / 2], cashAtFinal[Math.Min(runs - 1, runs * 9 / 10)], (double)items / runs);
    }
}
