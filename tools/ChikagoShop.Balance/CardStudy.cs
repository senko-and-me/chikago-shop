using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Economy;
using ChikagoShop.Domain.Trading;

static class CardStudy
{
    private sealed record Strategy(string Name, int FirstWeek, int SecondWeek, string[] Grades, int RerollBelow);
    private static readonly Strategy[] Strategies =
    [
        new("Beginner", 2, 3, ["D", "C", "C", "B"], -5),
        new("Typical", 3, 4, ["C", "B", "B", "A"], 8),
        new("Expert", 4, 5, ["B", "A", "A", "S"], 16),
        new("Rush D", 6, 8, ["D"], 2)
    ];

    private sealed record Order(bool Music, double Cycle, string Grade, int Wait, int Offer, double[] Rolls);
    private readonly record struct DayResult(int Cash, int Sold, int Late, int Rerolls, bool Stalled);
    private readonly record struct CampaignResult(bool PaidFirst, bool Won, int BeforeFirst, int AfterFirst, int BeforeFinal, bool Stalled);

    public static void Run(EconomyConfig config, int runs, int seed)
    {
        var rules = new DayUpgradeRules { Terms = config.Cards };
        Console.WriteLine("CARD|Strategy|Week|Card|Net gain|Positive runs|Sales|Late|Rerolls");
        foreach (var strategy in Strategies)
        {
            var estimates = new Dictionary<(int Week, DayUpgrade Card), double>();
            foreach (int week in new[] { 1, 2 })
            {
                int duration = week == 1 ? config.Days.DurationSeconds : config.Days.SecondWeekDurationSeconds;
                var samples = Enumerable.Range(0, runs).Select(i => Orders(config, strategy, week, duration,
                    new Random(unchecked(seed + i * 8191 + week * 109)))).ToArray();
                var baselines = samples.Select(orders => Play(config, rules, strategy, orders, DayUpgrade.None,
                    week, duration, config.StartingMoney)).ToArray();
                foreach (var card in DayUpgradeRules.Available.Prepend(DayUpgrade.None))
                {
                    if (week == 1 && card == DayUpgrade.MusicPrice) continue;
                    double delta = 0, positive = 0, sold = 0, late = 0, rerolls = 0;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        var result = card == DayUpgrade.None ? baselines[i] : Play(config, rules, strategy, samples[i],
                            card, week, duration, config.StartingMoney);
                        int gain = result.Cash - baselines[i].Cash;
                        delta += gain;
                        if (gain > 0) positive++;
                        sold += result.Sold;
                        late += result.Late;
                        rerolls += result.Rerolls;
                    }
                    estimates[(week, card)] = delta / runs;
                    Console.WriteLine($"CARD|{strategy.Name}|{week}|{card}|{delta / runs:F2}|{positive / runs:P1}|{sold / runs:F2}|{late / runs:F2}|{rerolls / runs:F2}");
                }
            }
            foreach (bool hand in new[] { true, false })
            {
                var campaigns = Enumerable.Range(0, runs).Select(i => Campaign(config, rules, strategy, estimates,
                    new Random(unchecked(seed + i * 65537)), i, hand)).ToArray();
                var weekTwo = campaigns.Where(result => result.PaidFirst).ToArray();
                Console.WriteLine($"CAMPAIGN|{strategy.Name}|{hand}|{campaigns.Count(result => result.PaidFirst) / (double)runs:P1}|" +
                    $"{campaigns.Count(result => result.Won) / (double)runs:P1}|{campaigns.Average(result => result.BeforeFirst):F2}|" +
                    $"{(weekTwo.Length == 0 ? 0 : weekTwo.Average(result => result.AfterFirst)):F2}|" +
                    $"{(weekTwo.Length == 0 ? 0 : weekTwo.Average(result => result.BeforeFinal)):F2}|" +
                    $"{campaigns.Count(result => result.Stalled) / (double)runs:P1}|" +
                    $"{(weekTwo.Length == 0 ? 0 : weekTwo.Count(result => result.AfterFirst < config.CraftCost) / (double)weekTwo.Length):P1}");
            }
        }
    }

    private static Order[] Orders(EconomyConfig config, Strategy strategy, int week, int duration, Random rng)
    {
        int plannedSales = week == 1 ? strategy.FirstWeek : strategy.SecondWeek;
        return Enumerable.Range(0, plannedSales).Select(_ => new Order(
            week == 2 && rng.Next(2) == 0, duration / (double)plannedSales - 2.2 + rng.NextDouble() * 2 - 1,
            strategy.Grades[rng.Next(strategy.Grades.Length)],
            rng.Next(config.BuyerWaitMinSeconds, config.BuyerWaitMaxSeconds + 1),
            TradeOfferRules.InitialPercent(rng.Next(61)),
            Enumerable.Range(0, config.RerollsPerItem * 2).Select(_ => rng.NextDouble()).ToArray())).ToArray();
    }

    private static CampaignResult Campaign(EconomyConfig config, DayUpgradeRules rules, Strategy strategy,
        Dictionary<(int Week, DayUpgrade Card), double> estimates, Random rng, int run, bool useHand)
    {
        var first = config.Quotas.Single(quota => quota.Id == "first");
        var final = config.Quotas.Single(quota => quota.Id == "final");
        var hand = new InvisibleHandDirector();
        int cash = config.StartingMoney;
        int checkpoint = cash, beforeFirst = cash, afterFirst = 0;
        bool stalled = false;
        for (int day = 1; day <= config.Days.TotalDays; day++)
        {
            int week = day <= first.DeadlineDay ? 1 : 2;
            int duration = config.Days.DurationFor(day, first.DeadlineDay);
            var choice = day == 1 ? DayUpgrade.None : rules.Options(day + run * 17, week == 2)
                .Where(card => estimates.GetValueOrDefault((week, card)) > 0 && cash >= rules.Cost(card) &&
                    cash - rules.Cost(card) + (card == DayUpgrade.Cash ? rules.For(card).Amount : 0) >=
                    (card == DayUpgrade.Materials ? Math.Max(0, config.CraftCost - rules.For(card).Amount) : config.CraftCost))
                .OrderByDescending(card => estimates[(week, card)]).FirstOrDefault();
            var quota = week == 1 ? first : final;
            void Observe(int balance, int stock, double elapsed) =>
                hand.Observe(balance, stock, checkpoint, day, week == 1 ? 1 : first.DeadlineDay + 1,
                    quota.DeadlineDay, elapsed / duration, quota.Amount, useHand);

            var result = Play(config, rules, strategy, Orders(config, strategy, week, duration, rng),
                choice, week, duration, cash, hand, Observe);
            cash = result.Cash;
            stalled |= result.Stalled;
            if (day != first.DeadlineDay) continue;
            beforeFirst = cash;
            if (cash < first.Amount) return new(false, false, cash, 0, 0, stalled);
            checkpoint = afterFirst = cash -= first.Amount;
            hand.Reset();
        }
        return new(true, cash >= final.Amount, beforeFirst, afterFirst, cash, stalled);
    }

    private static DayResult Play(EconomyConfig config, DayUpgradeRules rules, Strategy strategy, Order[] orders,
        DayUpgrade card, int week, double duration, int cash, InvisibleHandDirector? hand = null,
        Action<int, int, double>? observe = null)
    {
        var upgrades = new DayUpgradeSession { Rules = rules };
        if (card != DayUpgrade.None && cash >= rules.Cost(card))
        {
            cash -= rules.Cost(card);
            upgrades.Restore(false, card, musicUnlocked: week == 2);
            if (card == DayUpgrade.Cash) cash += upgrades.CashReward;
        }
        double elapsed = 0;
        int sold = 0, late = 0, rerolls = 0;
        bool stalled = false;
        int price = config.BasePrice;
        observe?.Invoke(cash, 0, elapsed);
        foreach (var order in orders)
        {
            int cost = upgrades.CraftPrice(config.CraftCost);
            if (elapsed + order.Cycle > duration) break;
            if (cash < cost) { stalled = true; break; }
            cash -= cost;
            elapsed += order.Cycle;
            int initial = order.Wait + upgrades.WaitExtension;
            double wait = initial - order.Cycle;
            int quality = RunRewardRules.QualityBonus(order.Grade);
            observe?.Invoke(cash, price + quality, elapsed);
            int percent = hand?.AdjustInitialOffer(order.Offer, 3) ?? order.Offer;
            int targetOffer = card == DayUpgrade.Bargaining
                ? Math.Max(strategy.RerollBelow, upgrades.MinimumFinalOffer(-30) / 2) : strategy.RerollBelow;
            for (int attempt = 0; attempt < config.RerollsPerItem && wait > 0.8 && elapsed + 0.8 < duration && percent < targetOffer; attempt++)
            {
                var options = TradeOfferRules.RerollOptions(price, TradeOfferRules.Price(price, percent),
                    order.Rolls[2 * attempt] < TradeOfferRules.HigherRerollChance);
                if (upgrades.GuaranteesReroll(config.RerollsPerItem - attempt))
                {
                    int floor = upgrades.MinimumFinalOffer(percent);
                    options = options.Where(option => option >= floor).DefaultIfEmpty(floor).ToArray();
                }
                if (options.Length == 0) break;
                percent = options[(int)(options.Length * order.Rolls[2 * attempt + 1])];
                elapsed += 0.8;
                wait -= 0.8;
                rerolls++;
            }
            wait = BuyerPatienceRules.RemainingAfterDayClosure(wait, elapsed >= duration);
            if (wait <= 0) { percent = upgrades.ExpiredOffer(percent); late++; }
            cash += BuyerPatienceRules.TotalPayout(TradeOfferRules.Price(price, percent), wait,
                RunRewardRules.SpeedBonus(wait, initial),
                quality + upgrades.SaleBonus(order.Music ? "music" : "atelier", order.Grade, wait));
            sold++;
            observe?.Invoke(cash, 0, elapsed);
        }
        return new(cash, sold, late, rerolls, stalled);
    }
}

