using ChikagoShop.Domain.Configuration;
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
    // A cycle includes a full 10-second craft and player interaction. The buyer
    // timer pauses during crafting; rerolls consume another 0.8 seconds each.
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
            var upgrades = new DayUpgradeSession();
            int cash = config.StartingMoney;
            int checkpointCash = cash;
            var hand = new InvisibleHandDirector();
            bool unlocked = false;
            for (int day = 1; day <= config.Days.TotalDays; day++)
            {
                upgrades.Reset(preserveCategories: true);
                if (day > 1)
                {
                    upgrades.Offer(day, unlocked);
                    DayUpgrade choice = DayUpgrade.None;
                    if (profile.BuyUpgrades)
                    {
                        var priorities = new[] { DayUpgrade.Cash,
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
                if (profile.Produce && profile.UnlockMusic && day > firstDeadline) unlocked = true;
                double remaining = config.Days.DurationSeconds + (unlocked ? 30 : 0);
                double duration = remaining;
                void Observe(double stock = 0)
                {
                    bool first = day <= firstDeadline;
                    var quota = config.Quotas.Single(entry => entry.DeadlineDay == (first ? firstDeadline : config.Days.TotalDays));
                    hand.Observe(cash, stock, checkpointCash, day, first ? 1 : firstDeadline + 1,
                        quota.DeadlineDay, 1 - remaining / duration, quota.Amount, profile.HiddenBalance);
                }
                Observe();
                while (profile.Produce)
                {
                    var workshop = unlocked && cash >= music.SpinCost ? music : atelier;
                    double cycle = profile.CycleSeconds;
                    if (cash < workshop.SpinCost || remaining < cycle) break;
                    Observe();
                    var available = config.Items.Where(item => item.Workshop == workshop.Id).ToArray();
                    var item = available[rng.Next(available.Length)];
                    remaining -= cycle;
                    cash -= workshop.SpinCost;
                    items++;
                    int basePrice = upgrades.Price(workshop.Id, item.BasePrice);
                    Observe(basePrice);
                    int percent = hand.AdjustInitialOffer(TradeOfferRules.InitialPercent(rng.Next(61)), 3);
                    var wait = RunRewardRules.BuyerWaitRange(workshop.Id == "music");
                    double initialWait = rng.Next(wait.Minimum, wait.Maximum + 1);
                    double buyerRemaining = initialWait - Math.Max(0, cycle - 10);
                    int rerolls = item.Rerolls;
                    while (profile.Reroll && percent < 5 &&
                        BuyerPatienceRules.CanReroll(true, false, rerolls, buyerRemaining))
                    {
                        rerolls--;
                        var options = TradeOfferRules.RerollOptions(basePrice, TradeOfferRules.Price(basePrice, percent),
                            rng.NextDouble() < TradeOfferRules.HigherRerollChance);
                        percent = options[rng.Next(options.Length)];
                        remaining -= 0.8; // The final active trade can finish after closing.
                        buyerRemaining -= 0.8;
                    }
                    buyerRemaining = BuyerPatienceRules.RemainingAfterDayClosure(buyerRemaining, remaining <= 0);
                    percent = BuyerPatienceRules.OfferPercent(percent, buyerRemaining);
                    cash += BuyerPatienceRules.TotalPayout(TradeOfferRules.Price(basePrice, percent), buyerRemaining,
                        RunRewardRules.SpeedBonus(buyerRemaining, initialWait), RunRewardRules.QualityBonus(profile.QualityRank));
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
