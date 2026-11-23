# Economy calibration — 2026-11-22

Historical simulation: these results predate the 2026-11-23 random buyer patience (clothing 10–20s, instruments 15–25s) and clocks continuing during minigames. The table has not been recalculated for that runtime behavior.

Runs per profile: **10000**. Seed: **20261107**. Starting cash: **$220**. Days: **14**. Day: **60s**, +30s after music unlock. One item per craft.

- atelier: cost **$25**, mean base value **$40.30**, mean margin before buyer variance/rerolls **$15.30**.
- music: cost **$50**, mean base value **$62.95**, mean margin before buyer variance/rerolls **$12.95**.

Quotas: day 7: $750, day 14: $1500.

| Strategy | First quota | Both quotas | Cash before final quota P10 / median / P90 | Mean items |
|---|---:|---:|---:|---:|
| Ordinary: immediate sales | 26.7 % | 0.1 % | 0 / 0 / 885 | 31.5 |
| Careful: reroll below +5% | 50.8 % | 3.1 % | 0 / 562 / 1332 | 38.2 |
| Careful + category investments | 74.4 % | 17.9 % | 0 / 1202 / 1614 | 46.7 |
| Slow: rerolls | 0.0 % | 0.0 % | 0 / 0 / 0 | 14.0 |
| Clothing only: rerolls | 51.1 % | 36.8 % | 0 / 1347 / 1722 | 31.7 |
| No production | 0.0 % | 0.0 % | 0 / 0 / 0 | 0.0 |

Assumptions: a full craft takes 10s; normal interaction adds 5s, slow interaction 13s. Customers arrive immediately; their 10s bonus clock pauses during the craft and loses interaction/reroll time. Quality is assumed B (+$3). Speed bands, quality reward, $1000 gift after the first quota, rarity transfers and offer correction use shared domain rules. Hidden balance is enabled with its default 5/3 point caps. Energy is 3 / 6. Music is opened from day 2 if cash covers unlock + one craft + $25 reserve. Each reroll costs 0.8s; careful profiles reroll below +5%. Expired patience or day closure forces a half-price offer with no speed/quality tips, through the same BuyerPatienceRules as runtime.

The investment profile buys cash cards or matching category cards when at least two crafts remain affordable. Other profiles skip all cards. Existing card prices/effects are unchanged. First-quota failures contribute zero to final-cash percentiles.

This is a money/time model, not a human playtest: it assumes one item is crafted and sold per cycle, a matching customer is available, no abandoned inventory, no reload retries, no macro-input or early craft completion. Real camera travel, player mistakes and deliberate hoarding can alter results. Probabilities are calibration evidence, not promised win rates.

Reproduce from repository root: `dotnet run --project tools/ChikagoShop.Balance -- --runs 10000 --seed 20261107 --output docs/BALANCE_REPORT.md`
