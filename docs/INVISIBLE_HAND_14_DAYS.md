# 14-day run, mafia collection, and Invisible Hand (implementation v1)

## 14 days and quota visits

- Days 1–7: first quota is collected **after day 7**, during the transition to day 8.
- Day 8: an on-screen mafia collection notice appears before the day-8 card choice.
- Days 8–14: second quota is collected **after day 14**; a final notice appears.
- Existing quota amounts are unchanged in baseline `config/economy.v1.json` ($250/$450). If the F10 profile has $400/$1000 (or any other amounts), those overrides still win.
- The mafia notice is a text-only placeholder in the current build, not a new character sprite/cutscene.
- The +$400 quest reward discussed in planning is **not** implemented.

## Invisible Hand

The hidden balancing model (`src/ChikagoShop.Domain/Economy/InvisibleHandDirector.cs`)
compares current money plus 80% of estimated value of unsold items against a smooth
trajectory toward the next quota. On meaningful actions, it moves its hidden
pressure by no more than 0.12 toward a target in [-1, +1]. Pressure is reset
after successful payment of the first quota, persisted across Continue, and
restored from the checkpoint.

The pressure affects only future random outcomes:

- Rarity weights: transfer up to 5 percentage points from common to existing
  higher tiers for a struggling player, or back for a player ahead. Price and
  rarity cards are applied **before** this transfer, so their effect is retained.
- Initial buyer offers: a change of no more than 3 percentage points to the
  original [-30%, +30%] offer. Reroll logic is unchanged.
- The system never directly changes the wallet, number of crafted items, energy,
  production costs, quota values, or mini-game evaluation.

F10 / Balance Debug includes:

- `Невидимая рука: 0 — выкл., 1 — вкл.` (on by default)
- `Невидимая рука: предел редкости (п.п.)` (5 default)
- `Невидимая рука: предел предложения (п.п.)` (3 default)

## Manual smoke test in Godot 4.7.2 Mono

1. Import a clean copy, let C# project rebuild, and start **New Game**.
2. Confirm HUD uses 14 days and that F10 labels first quota at day 7, final at day 14.
3. Play or use debug shortcuts to end days. Verify cards can be chosen on days 8–14.
4. End day 7 with enough cash: the quota is charged, the mafia notice appears,
   and dismissing it opens the day-8 card choice. Failing day 7 shows the
   failed-payment notice and the existing checkpoint-retry flow.
5. Save/Continue before and after mafia collection and while the card choice is
   pending. The event must not be duplicated once acknowledged.
6. End day 14 with enough cash to see the second collection and win screen.
7. In F10 toggle `hand.enabled` between 0 and 1 and compare repeated runs.
   This is statistical balancing, not a guarantee of any particular outcome.
8. Verify the established 60 s before / 90 s after music unlock, plus temporary
   one-day card bonuses, are unchanged from the previous revision.

**Environment note:** this source update has not been built with Godot/.NET in
this environment, so in-game behavior still needs the above manual smoke test.
Some older integration tests in `tests/Integration` still describe the previous
seven-day campaign and require wider test-suite maintenance.
