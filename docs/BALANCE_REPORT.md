# Economy calibration — 2026-11-07

Runs per profile: **10000**. Seed: **20261107**. Starting cash: **$220**. Day: **90s**. One item per craft.

- atelier: cost **$25**, mean base value **$45.30**, mean margin before buyer variance/rerolls **$20.30**.
- music: cost **$50**, mean base value **$67.95**, mean margin before buyer variance/rerolls **$17.95**.

Quotas: day 4: $250, day 7: $450.

| Strategy | First quota | Both quotas | Cash before final quota P10 / median / P90 | Mean items |
|---|---:|---:|---:|---:|
| Ordinary: immediate sales | 89.3 % | 47.9 % | 0 / 437 / 786 | 30.8 |
| Careful: reroll below +5% | 99.4 % | 88.0 % | 428 / 719 / 1072 | 32.8 |
| Careful + category investments | 99.0 % | 97.8 % | 884 / 1379 / 1865 | 32.7 |
| Slow: rerolls | 93.2 % | 38.7 % | 19 / 390 / 686 | 20.0 |
| Clothing only: rerolls | 100.0 % | 64.6 % | 352 / 498 / 699 | 21.0 |
| No production | 0.0 % | 0.0 % | 0 / 0 / 0 | 0.0 |

Assumptions: a full craft takes 10s; normal interaction adds 5s, slow interaction 13s. The actual 0.5–2s buyer delay is sampled; each reroll costs 0.8s. Energy is 3 / 6 after unlocking music. Music is opened from day 2 if cash covers unlock + one craft + $25 reserve. If music is unaffordable, clothing is used. Ordinary players accept the first offer; careful players reroll below +5%. Initial offers remain uniform -30…+30%; rerolls prefer a higher rounded price with probability 70%. The shared domain code supplies item weights, price rounding, reroll pools and upgrades. No hidden luck correction.

The investment profile buys cash cards or matching category cards when at least two crafts remain affordable. Other profiles skip all cards. Existing card prices/effects are unchanged. First-quota failures contribute zero to final-cash percentiles.

This is a money/time model, not a human playtest: it assumes one item is crafted and sold per cycle, no abandoned inventory, no reload retries, no macro-input or early craft completion. Real camera travel, player mistakes and deliberate hoarding can alter results. Probabilities are calibration evidence, not promised win rates.

Reproduce from repository root: `dotnet run --project tools/ChikagoShop.Balance -- --runs 10000 --seed 20261107 --output docs/BALANCE_REPORT.md`
