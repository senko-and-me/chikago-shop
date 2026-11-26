# Economy calibration — v1.4

Runs per profile: **10000**. Seed: **20261107**. Starting cash: **$220**. Days: **14**. Day: **60s**, +30s after music unlock. One item per craft.

- atelier: cost **$25**, mean base value **$91.00**, mean margin before buyer variance/rerolls **$66.00**.
- music: cost **$50**, mean base value **$139.00**, mean margin before buyer variance/rerolls **$89.00**.

Quotas: day 7: $750, day 14: $1500.

| Strategy | First quota | Both quotas | Cash before final quota P10 / median / P90 | Mean items |
|---|---:|---:|---:|---:|
| Ordinary: immediate sales | 100.0 % | 100.0 % | 3676 / 4453 / 5283 | 70.0 |
| Careful: reroll below +5% | 100.0 % | 100.0 % | 4073 / 4876 / 5743 | 56.5 |
| Careful + category investments | 100.0 % | 100.0 % | 4717 / 5525 / 6393 | 56.5 |
| Slow: rerolls | 87.8 % | 84.3 % | 0 / 2415 / 3132 | 31.9 |
| Clothing only: rerolls | 100.0 % | 100.0 % | 2301 / 2835 / 3394 | 42.8 |
| No production | 0.0 % | 0.0 % | 0 / 0 / 0 | 0.0 |

Assumptions: a full craft takes 10s; normal interaction adds 5s, slow interaction 13s. Customers arrive immediately; their category-dependent (10–20s / 15–25s) bonus clock pauses during the craft and loses interaction/reroll time. Quality is assumed B (+$3). Speed bands, quality rewards and the Invisible Hand offer correction share the runtime rules. Music opens for free after the first quota. Each reroll costs 0.8s; careful profiles reroll below +5%. Expired patience or day closure forces a half-price offer with no speed/quality tips, through the same BuyerPatienceRules as runtime.

The investment profile buys cash cards or matching price cards when at least two crafts remain affordable. Other profiles skip all cards. Existing card prices/effects are unchanged. First-quota failures contribute zero to final-cash percentiles.

This is a money/time model, not a human playtest: it assumes one item is crafted and sold per cycle, a matching customer is available, no abandoned inventory, no reload retries, no macro-input or early craft completion. Real camera travel, player mistakes and deliberate hoarding can alter results. Probabilities are calibration evidence, not promised win rates.

Reproduce from repository root: `dotnet run --project tools/ChikagoShop.Balance -- --runs 10000 --seed 20261107 --output docs/BALANCE_REPORT.md`
