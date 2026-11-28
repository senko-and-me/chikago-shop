using System.Globalization;
using System.Linq;
using System.Text;
using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Simulation;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string Value(string name, string fallback) => Array.IndexOf(args, name) is int i && i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
int runs = int.Parse(Value("--runs", "10000"));
int seed = int.Parse(Value("--seed", "20261107"));
var config = EconomyConfigLoader.Load(File.ReadAllText(Value("--config", "config/economy.v1.json")));
if (args.Contains("--card-study")) { CardStudy.Run(config, runs, seed); return; }
var report = new StringBuilder("# Economy calibration — 2026-11-22\n\n");
report.AppendLine($"Runs per profile: **{runs}**. Seed: **{seed}**. Starting cash: **${config.StartingMoney}**. Days: **{config.Days.TotalDays}**. Day: **{config.Days.DurationSeconds}s** in week one and **{config.Days.SecondWeekDurationSeconds}s** in week two. One item per craft.\n");
foreach (var workshop in config.Workshops)
{
    foreach (var item in config.Items.Where(item => item.Workshop == workshop.Id))
        report.AppendLine($"- {item.Id}: craft **${config.CraftCosts[item.Id]}**, base sale **${config.BasePrice}**, margin before buyer variance/rerolls **${config.BasePrice - config.CraftCosts[item.Id]}**.");
}
report.AppendLine($"\nQuotas: {string.Join(", ", config.Quotas.Select(quota => $"day {quota.DeadlineDay}: ${quota.Amount}"))}.\n");
report.AppendLine("| Strategy | First quota | Both quotas | Cash before final quota P10 / median / P90 | Mean items |\n|---|---:|---:|---:|---:|");
foreach (var profile in WeekBalanceSimulator.Profiles)
{
    var result = WeekBalanceSimulator.Run(config, profile, runs, seed);
    report.AppendLine($"| {result.Profile} | {result.FirstQuotaPassRate:P1} | {result.WinRate:P1} | {result.CashBeforeFinalP10} / {result.CashBeforeFinalMedian} / {result.CashBeforeFinalP90} | {result.MeanItems:F1} |");
}
report.AppendLine("\nThe customer clock continues throughout crafting. Quality terms, rerolls, crafting charges, day cards and percentage offers use the domain economy. The music workshop opens free after the first quota. Use --card-study for paired card comparisons across skill levels.");
report.AppendLine("This is a money/time model, not a human playtest: it assumes one item is crafted and sold per cycle, a matching customer is available, no abandoned inventory, no reload retries, no macro-input or early craft completion. Real camera travel, player mistakes and deliberate hoarding can alter results. Probabilities are calibration evidence, not promised win rates.\n");
report.AppendLine("Reproduce from repository root: `dotnet run --project tools/ChikagoShop.Balance -- --runs 10000 --seed 20261107 --output docs/BALANCE_REPORT.md`");
Console.Write(report);
string output = Value("--output", "");
if (output.Length > 0) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!); File.WriteAllText(output, report.ToString()); }
