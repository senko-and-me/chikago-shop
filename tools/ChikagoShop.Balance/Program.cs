using System.Globalization;
using System.Text;
using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Simulation;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string Value(string name, string fallback) => Array.IndexOf(args, name) is int i && i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
int runs = int.Parse(Value("--runs", "10000"));
int seed = int.Parse(Value("--seed", "20261107"));
var config = EconomyConfigLoader.Load(File.ReadAllText(Value("--config", "config/economy.v1.json")));
var report = new StringBuilder("# Economy calibration — 2026-11-07\n\n");
report.AppendLine($"Runs per profile: **{runs}**. Seed: **{seed}**. Starting cash: **${config.StartingMoney}**. Day: **{config.Days.DurationSeconds}s**. One item per craft.\n");
foreach (var workshop in config.Workshops)
{
    double mean = config.Rarities.Sum(rarity => (double)rarity.Weight * rarity.BasePrices[workshop.Id]) / config.Rarities.Sum(rarity => rarity.Weight);
    report.AppendLine($"- {workshop.Id}: cost **${workshop.SpinCost}**, mean base value **${mean:F2}**, mean margin before buyer variance/rerolls **${mean - workshop.SpinCost:F2}**.");
}
report.AppendLine($"\nQuotas: {string.Join(", ", config.Quotas.Select(quota => $"day {quota.DeadlineDay}: ${quota.Amount}"))}.\n");
report.AppendLine("| Strategy | First quota | Both quotas | Cash before final quota P10 / median / P90 | Mean items |\n|---|---:|---:|---:|---:|");
foreach (var profile in WeekBalanceSimulator.Profiles)
{
    var result = WeekBalanceSimulator.Run(config, profile, runs, seed);
    report.AppendLine($"| {result.Profile} | {result.FirstQuotaPassRate:P1} | {result.WinRate:P1} | {result.CashBeforeFinalP10} / {result.CashBeforeFinalMedian} / {result.CashBeforeFinalP90} | {result.MeanItems:F1} |");
}
report.AppendLine("\nAssumptions: a full craft takes 10s; normal interaction adds 5s, slow interaction 13s. The actual 0.5–2s buyer delay is sampled; each reroll costs 0.8s. Energy is 3 / 6 after unlocking music. Music is opened from day 2 if cash covers unlock + one craft + $25 reserve. If music is unaffordable, clothing is used. Ordinary players accept the first offer; careful players reroll below +5%. Initial offers remain uniform -30…+30%; rerolls prefer a higher rounded price with probability 70%. The shared domain code supplies item weights, price rounding, reroll pools and upgrades. No hidden luck correction.");
report.AppendLine("\nThe investment profile buys cash cards or matching category cards when at least two crafts remain affordable. Other profiles skip all cards. Existing card prices/effects are unchanged. First-quota failures contribute zero to final-cash percentiles.\n");
report.AppendLine("This is a money/time model, not a human playtest: it assumes one item is crafted and sold per cycle, no abandoned inventory, no reload retries, no macro-input or early craft completion. Real camera travel, player mistakes and deliberate hoarding can alter results. Probabilities are calibration evidence, not promised win rates.\n");
report.AppendLine("Reproduce from repository root: `dotnet run --project tools/ChikagoShop.Balance -- --runs 10000 --seed 20261107 --output docs/BALANCE_REPORT.md`");
Console.Write(report);
string output = Value("--output", "");
if (output.Length > 0) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!); File.WriteAllText(output, report.ToString()); }
