# Оркестровый план: «Бакалея для гангстеров»

Версия плана: 1.1
Дата среза: 2026-10-31
Текущая стадия: `S1 — домен, конфигурация и воспроизводимость`
Источник истины: этот файл. Папка `source/` — локальный архив и не входит в Git.

## Текущий фронт

### HF-82 — hide doorways leading to locked rooms

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-09; HF-81 DONE. User requests no-opening sprites in open rooms while neighboring rooms remain locked. One executor; HF-81 assets retained.
CP-HF-82 read-set: current front; scripts/TradeScene.cs (existing music/right-showroom unlock and presentation refresh), scripts/Persistence/TradeScene.Persistence.cs; scenes/trade_scene.tscn; scripts/Presentation/; tests/ProjectTest.cs, tests/Integration/ProjectTest.Resources.cs, Persistence.cs; config/economy.v1.json; exact input новые спрайты комнаты/верх центр без проема.jpg and касса без проема.jpg plus doorway variants previously inspected; art/rooms/; .vscode/settings.json; git status/diff. Write-set: ORCHESTRATION_PLAN.md; art/rooms/{upper_corridor_closed,cashier_closed}.jpg and .import; scripts/Presentation/RoomDoorwayPresentation.cs and .uid; scenes/trade_scene.tscn (initial closed textures/presentation node); scripts/TradeScene.cs (presentation refresh call only); tests/ProjectTest.cs; tests/Integration/ProjectTest.Resources.cs. TEMP/chikago-hf82-* diagnostics/logs; temporary override.cfg only if absent, removed after checks.
Out of scope: source/, gameplay/unlock costs/save schema, new room state rules, other art editing, commit/push. Acceptance: initially corridor and cashier hide right exits to music/right-showroom; successful existing unlock reveals both immediately; failed unlock keeps them hidden; save/reload, checkpoint restoration and new game reproduce current lock state; scene defaults reflect closed state in editor; geometry/left doors and availability rules unchanged. Presentation consumes existing MusicWorkshopUnlocked without new domain logic in TradeScene. Checks: build; GPU --room-doorway-checks (state transitions/scene defaults/geometry), --persistence-checks, --balance-checks, --presentation-checks, --resource-checks; git diff --check.

```text
Task: HF-82
Recommended status: REVIEW
Changed: art/rooms/{upper_corridor_closed,cashier_closed}.jpg and .import; scripts/Presentation/RoomDoorwayPresentation.cs and .uid; scenes/trade_scene.tscn; scripts/TradeScene.cs (one presentation-refresh call); tests/ProjectTest.cs; tests/Integration/ProjectTest.Resources.cs; ORCHESTRATION_PLAN.md.
Checks: build -> PASS (0 warnings/errors); Godot import -> PASS; GPU --room-doorway-checks -> PASS; --persistence-checks -> PASS; --balance-checks -> PASS; --presentation-checks -> PASS; --resource-checks -> PASS; both supplied closed images SHA256 -> MATCH; git diff --check -> PASS. Evidence TEMP/chikago-hf82-{import,room-doorway,persistence,balance,presentation,resource}.log/.err.
Acceptance: scene defaults now show the supplied closed-right-door cashier/corridor artwork. A presentation-only node consumes the existing MusicWorkshopUnlocked state shared by music/right showroom and selects cached open/closed textures through the established showcase refresh path. No domain logic added to TradeScene and no new state persisted. Regression checks initial packed scene before Ready, fresh game, insufficient-funds unlock, successful100-dollar unlock, save/reload open, initial-checkpoint rollback closed, reload closed, unlock and new-game reset. Each transition checks exact texture paths,1398x1080 dimensions and unchanged sprite transforms.
Read-set exception: none; source/ and PSD files untouched.
Risks/blocker: none. Earlier background changes retained, temporary override removed, player saves unused. No commit/push.
Next: unlock the music workshop to see both right-hand doorways appear.
```

Координатор подтвердил HF-82 2026-11-09 по build и пяти GPU targets: REVIEW → DONE.

### HF-81 — replace room backgrounds with supplied artwork

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-09; HF-80 DONE. User supplies new room images in the explicitly named local folder and requests scene replacement. One executor.
CP-HF-81 read-set: current front; exact input files новые спрайты комнаты/ателье.jpg, муз комната.jpg, нижняя.jpg, касса.jpg, касса без проема.jpg, верх центр.jpg, верх центр без проема.jpg (visual comparison/dimensions); art/rooms/{atelier,music_room,showroom,cashier,upper_corridor,room_layout,room_bg}.jpg and existing import descriptors; scenes/trade_scene.tscn; scripts/CounterWorld.cs; tests/ProjectTest.cs, tests/Integration/ProjectTest.Resources.cs, Presentation.cs, Camera.cs; .vscode/settings.json; git status/diff. Write-set: ORCHESTRATION_PLAN.md; art/rooms/{atelier,music_room,showroom,cashier,upper_corridor}.jpg (exact supplied image bytes) and their import descriptors only if Godot changes them. TEMP/chikago-hf81-* diagnostics/logs; temporary override.cfg only if absent and removed afterward.
Out of scope: source/, PSD inspection, supplied originals modification/deletion, no-opening alternate variants, overall room layout, transforms/hitboxes/gameplay, commit/push. Acceptance: scene's existing five texture resources use new supplied images (both showcase rooms share showroom), all sizes preserved1920x1080 or1398x1080; doorway variants match existing connectivity; scene resources import/load, monochrome presentation works, visual inspection confirms replacements. Checks: source/destination SHA256 and image dimensions; Godot import; GPU --resource-checks/--presentation-checks; git diff --check. Artwork-only replacement adds no behavior or code.

```text
Task: HF-81
Recommended status: REVIEW
Changed: art/rooms/{atelier,cashier,showroom,upper_corridor}.jpg; ORCHESTRATION_PLAN.md. Supplied music-room bytes already match art/rooms/music_room.jpg, so no diff for that file.
Checks: all5 source/destination SHA256 pairs -> MATCH; source/current dimensions -> preserved1920x1080 or1398x1080; Godot import -> PASS; GPU --resource-checks -> PASS; GPU --presentation-checks -> PASS; temporary scene smoke -> PASS for six background nodes' texture paths, dimensions and transforms; git diff --check -> PASS. Evidence TEMP/chikago-hf81-{import,resource,presentation,scene}.log/.err and scene-contact.png.
Acceptance: existing scene texture paths now resolve to new supplied artwork, including both shared showcase backgrounds. Chose corridor/cashier doorway variants to preserve existing door connectivity. Canonical art paths/UIDs retained, no scene/script/hitbox/scale changes needed. Rendered scene montage inspected; replacements align with existing room rectangles. Supplied originals and PSDs untouched; no source/ access or image edits/generation. Artwork-only change; no new behavior introduced.
Read-set exception: rg --files scripts used only to locate CounterWorld.cs after initial guessed World/ path did not exist; no source/ traversal.
Risks/blocker: none for replacement. Existing overall-layout background, lock overlays and UI positions retained. Temporary override removed, isolated user directory used; no commit/push.
Next: open trade_scene.tscn in Godot to inspect the updated room backgrounds.
```

Координатор подтвердил HF-81 2026-11-09 по hashes, GPU regressions и rendered inspection: REVIEW → DONE.

### HF-80 — verify and publish accumulated fixes

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-09; HF-79 DONE. User authorizes commit and push to GitHub after verification.
CP-HF-80 read-set: current front and HF-68–79 evidence; git status/diff/log/remotes; tools/verify.ps1; tests/ProjectTest.cs; project/solution configuration; existing changed and untracked implementation/test files. Write-set: ORCHESTRATION_PLAN.md (verification/handoff); Git index/local commit and origin/main push for accumulated HF-68–79 changes. TEMP/chikago-hf80-* logs; isolated user directory and temporary override.cfg only if absent, removed after checks.
Out of scope: source/, gameplay changes, unrelated fixes, player saves, force push. Acceptance: review accumulated diff, build/domain and targeted UI/gameplay regressions pass; record any existing full-run failures accurately; commit intended files and push current main normally; verify origin/main matches HEAD and worktree is clean. Checks: tools/verify.ps1; targeted Godot checks; git diff --check; post-push status and remote ref.

```text
Task: HF-80
Recommended status: REVIEW
Changed: accumulated HF-68–79 implementation, tests, documentation and Godot UID files; no new gameplay changes in publication task.
Checks: tools/verify.ps1 -> build PASS, domain125/125 PASS, import PASS; full ProjectTest FAIL at existing TestUiLayoutInvalidation assertion "Showing a tooltip must immediately activate its presentation updates" (also recorded before this publication). GPU targets new-game-funds, progress-toggle, closed-reroll, pause-hotkey, pause-localization, button-feedback, balance, presentation, resource, persistence -> all10 PASS. git diff --check -> PASS. Evidence TEMP/chikago-hf80-{full,new-game-funds,progress-toggle,closed-reroll,pause-hotkey,pause-localization,button-feedback,balance,presentation,resource,persistence}.log and target .err files.
Acceptance: scoped fixes validated; remote fetched, main initially matched origin/main. Earlier player/editor changes preserved. No source/ files or player saves accessed; temporary override removed. Full suite is not claimed green.
Read-set exception: none.
Risks/blocker: existing full-run tooltip assertion remains outside this task. No publication blocker.
Next: fix the existing tooltip lifecycle assertion in a separate task.
```

Координатор подтвердил HF-80 2026-11-09: implementation commit89f4440999bad1ee40a7daff5ce04eecd2ade045 pushed normally to origin/main; ls-remote confirmed matching HEAD and worktree was clean. REVIEW → DONE. This closure record is published in a follow-up documentation commit.

### HF-79 — restore new-game configured funds after null scene overrides

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-09; HF-78 DONE. User reports new game starts at0 instead of220. Earlier edits retained.
CP-HF-79 read-set: current front; scenes/trade_scene.tscn; scripts/TradeScene.cs (exports/start/reset/availability), scripts/UI/RarityPanel.cs, scripts/MainMenu.cs (new-game routing), scripts/Persistence/TradeScene.Persistence.cs, ProgressSaveStore.cs; config/economy.v1.json; tests/ProjectTest.cs, tests/Integration/ProjectTest.Persistence.cs, Resources.cs; .vscode/settings.json; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scenes/trade_scene.tscn (remove erroneous null exported-value overrides only); tests/ProjectTest.cs; tests/Integration/ProjectTest.Persistence.cs. TEMP/chikago-hf79-* logs; temporary override.cfg only if absent and removed afterward.
Out of scope: source/, changes to domain/economy values, unrelated scene edits, player save migration/reset, commit/push. Acceptance: fresh packed-scene instance uses configured220 funds,250/450 quotas and valid save path; runtime new game restores220 and checkpoint220, persists/reloads correctly; rarity prices retain defaults. Checks: build; GPU --new-game-funds-checks (reproduce before fix then pass); --button-feedback-checks; git diff --check.

```text
Task: HF-79
Recommended status: REVIEW
Changed: scenes/trade_scene.tscn (remove14 erroneous null overrides); tests/ProjectTest.cs; tests/Integration/ProjectTest.Persistence.cs; ORCHESTRATION_PLAN.md.
Checks: build -> PASS (0 warnings/errors); GPU --new-game-funds-checks -> FAIL before fix at packed-scene defaults assertion, PASS after fix; GPU --button-feedback-checks -> PASS; git diff --check -> PASS. Evidence TEMP/chikago-hf79-{new-game-funds,button-feedback}.log/.err.
Acceptance: null exported-value overrides in the scene replaced C# defaults with0/null for starting money, both quotas, save path and price tables. Removing only these lines restores configured220 start/checkpoint,250/450 quotas, default save path and atelier prices30/50/80/120/200. Regression tests fresh instantiation, initial funds, runtime New Game from0, affordable craft availability, saved current/checkpoint funds and reload through a second instance. Domain/code defaults unchanged.
Read-set exception: none; source/ not scanned.
Risks/blocker: none for new-game startup/reset. Existing player saves deliberately untouched; previous unrelated edits retained. No commit/push; temporary override removed and isolated user directory used.
Next: start New Game to receive220 dollars.
```

Координатор подтвердил HF-79 2026-11-09 по воспроизведению, build и двум GPU targets: REVIEW → DONE.

### HF-78 — tighten showcase feedback to the display area

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-77 DONE. User requests a smaller outline around the showcase. Previous edits retained.
CP-HF-78 read-set: current front; scripts/Presentation/ButtonFeedbackController.cs; scripts/UI/SlotUI.cs, InventoryUI.cs; scripts/TradeScene.cs (showcase wiring only); scenes/UI/ShowcaseSlot.tscn, scenes/trade_scene.tscn; art/props/display_table.png; tests/ProjectTest.cs, tests/Integration/ProjectTest.ButtonFeedback.cs, Inventory.cs; .vscode/settings.json. Write-set: ORCHESTRATION_PLAN.md; scripts/Presentation/ButtonFeedbackController.cs (optional local feedback bounds); scenes/UI/ShowcaseSlot.tscn (visual feedback bounds and native focus style only); tests/ProjectTest.cs, tests/Integration/ProjectTest.ButtonFeedback.cs. TEMP/chikago-hf78-* logs; temporary override.cfg only if absent, removed afterward.
Out of scope: source/, gameplay, hitboxes, layout/art changes, other feedback policy, commit/push. Acceptance: showcase outline is narrower and slightly shorter, aligned with the glass display; hover/click/keyboard feedback uses the same frame; hitbox/actions unchanged and other buttons preserved. Checks: build; GPU --button-feedback-checks (scaled showcase hover/click/focus regression); git diff --check.

```text
Task: HF-78
Recommended status: REVIEW
Changed: scripts/Presentation/ButtonFeedbackController.cs; scenes/UI/ShowcaseSlot.tscn; tests/ProjectTest.cs; tests/Integration/ProjectTest.ButtonFeedback.cs; ORCHESTRATION_PLAN.md.
Checks: dotnet build ChikagoShop.csproj --no-restore -> PASS (0 warnings/errors); GPU --button-feedback-checks -> PASS; git diff --check -> PASS. Evidence TEMP/chikago-hf78-button-feedback.log/.err.
Acceptance: showcase local feedback rectangle is (65,0,310,200), replacing the full440x230 outline; native focus uses the transparent showcase style to avoid a second wide border. Optional metadata affects only configured controls and preserves transforms/clipping. Hitbox, artwork and layout unchanged. Regression verifies compact hover/click/keyboard bounds at two scales and preserves activation and control size. Existing world-button fixture now refreshes availability after assigning funds, removing dependence on prior user settings.
Read-set exception: necessary diagnostic reads of showcase wiring and inventory presentation are recorded in the package; source/ not scanned.
Risks/blocker: none; earlier edits retained, no commit/push, temporary override removed and player saves unused.
Next: hover a showcase to inspect the tighter frame.
```

Координатор подтвердил HF-78 2026-11-08 по build и GPU regressions: REVIEW → DONE.

### HF-77 — do not retain mouse-click focus on the progress toggle

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-76 DONE. User requests no persistent selection on the HUD >/< toggle. One executor, earlier uncommitted changes retained.
CP-HF-77 read-set: current front; scripts/UI/ProgressPanel.cs, scripts/Presentation/ButtonFeedbackController.cs (focus behavior); scenes/trade_scene.tscn (toggle declaration); tests/ProjectTest.cs, tests/Integration/ProjectTest.Ui.cs, ButtonFeedback.cs, Helpers.cs; .vscode/settings.json, ChikagoShop.csproj; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/ProgressPanel.cs (release mouse focus only); tests/ProjectTest.cs (target); tests/Integration/ProjectTest.Ui.cs (toggle regression). OS TEMP/chikago-hf77-* logs; temporary override.cfg only if absent, removed after checks.
Out of scope: source/, global focus/pause-menu selection, geometry/art/gameplay/save changes, commit/push. Acceptance: after mouse collapse/expand and pointer departure, toggle has no retained native focus/feedback; hover/press still work, keyboard focus/Space remains functional, size/state behavior preserved. Checks: build; GPU --progress-toggle-checks/--button-feedback-checks; git diff --check.

```text
Task: HF-77
Recommended status: REVIEW
Changed: scripts/UI/ProgressPanel.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.Ui.cs; ORCHESTRATION_PLAN.md.
Checks: dotnet build ChikagoShop.csproj --no-restore -> PASS (0 warnings/errors); GPU --progress-toggle-checks -> PASS; GPU --button-feedback-checks -> PASS; git diff --check -> PASS. Evidence OS TEMP/chikago-hf77-{progress-toggle,button-feedback}.log/.err.
Acceptance: left mouse press schedules release of the toggle's native focus, so leaving the arrow removes its outline after the short click animation. Hover/press feedback remains active. Regression repeats collapse/expand three times with real mouse events and checks focus/feedback after departure, hover restoration, keyboard Space activation with retained keyboard focus, panel sizing, resizing and state updates.
Read-set exception: none; source/ not scanned.
Risks/blocker: none. Previous edits retained; no commit/push, temporary override.cfg removed and genuine saves unused.
Next: click the HUD arrow and move the cursor away.
```

Координатор подтвердил HF-77 2026-11-08 по build и двум GPU targets: REVIEW → DONE.

### HF-76 — explicit day-card arrow navigation across all available choices

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-75 DONE. User reports arrows only alternate between the middle buy button and Skip. One executor, previous uncommitted work retained.
CP-HF-76 read-set: current front; scripts/UI/DayTransitionCard.cs, scripts/Presentation/ButtonFeedbackController.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.ButtonFeedback.cs; .vscode/settings.json, ChikagoShop.csproj, README.md; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/DayTransitionCard.cs (explicit focus traversal only); scripts/Presentation/ButtonFeedbackController.cs (notify keyboard modality for consumed card navigation only, coordinator expansion); tests/Integration/ProjectTest.ButtonFeedback.cs; README.md. OS TEMP/chikago-hf76-* logs; temporary override.cfg only if absent, removed after checks.
Out of scope: source/, gameplay/purchases/skip effects, other screen navigation, art/layout, commit/push. Acceptance: first navigation reveals focus as before; right/down advance and left/up retreat through enabled purchases then Skip with wrapping; Tab/Shift-Tab use the same ordered choices; disabled purchases skipped; all-disabled selects Skip; navigation never buys/skips and cannot escape to background controls. Checks: build; GPU --button-feedback-checks (all4 directions/full cycle, disabled cases, Tab, no auto-focus/no actions), --balance-checks; git diff --check.

```text
Task: HF-76
Recommended status: REVIEW
Changed: scripts/UI/DayTransitionCard.cs; scripts/Presentation/ButtonFeedbackController.cs (explicit keyboard-mode notification for consumed modal navigation); tests/Integration/ProjectTest.ButtonFeedback.cs; README.md; ORCHESTRATION_PLAN.md.
Checks: build -> PASS (0 warnings/errors); GPU --button-feedback-checks -> PASS; GPU --balance-checks -> PASS; git diff --check -> PASS. Evidence OS TEMP/chikago-hf76-{button-feedback,balance}.log/.err.
Acceptance: explicit ordered focus traversal replaces Godot's geometric nearest-neighbor search for the day-card screen only. Right/down advance; left/up retreat; Tab/Shift-Tab follow the same circular order of enabled purchases then Skip. Opening still leaves no selection until navigation. All navigation keys are consumed so focus cannot escape to background UI. Card notifies feedback's keyboard mode because consumed input does not reach the global observer; this prevents a stationary mouse over another choice from overriding the keyboard highlight. Tests traverse every entry and wrap in all4 directions at balances1000/20/0, verify disabled entries skipped and Skip-only fallback, Tab/reverse Tab, no purchase/skip, reopen reset, and keyboard feedback priority over another hovered choice.
Read-set exception: coordinator expanded read/write-set to ButtonFeedbackController for the necessary consumed-input modality notification; global navigation policy unchanged. source/ not scanned.
Risks/blocker: none for this task. No commit/push, earlier edits retained and temporary override.cfg removed; genuine saves unused.
Next: navigate all purchase buttons and Skip using either arrow pair.
```

Координатор подтвердил HF-76 2026-11-08 по build и targeted GPU regressions: REVIEW → DONE.

### HF-75 — block buyer rerolls once the day is closed

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-74 DONE. Direct user rule: no rerolling buyers after time expires. One executor, earlier uncommitted work retained.
CP-HF-75 read-set: current front; scripts/TradeScene.cs (reroll/UI/day synchronization), scripts/Days/DayCycleController.cs, src/ChikagoShop.Domain/Trading/TradeOfferRules.cs; tests/ChikagoShop.Domain.Tests/TradeOfferRulesTests.cs; tests/ProjectTest.cs, tests/Integration/ProjectTest.Days.cs, Helpers.cs; README.md, .vscode/settings.json, ChikagoShop.csproj; git status/diff. Write-set: ORCHESTRATION_PLAN.md; src/ChikagoShop.Domain/Trading/TradeOfferRules.cs (availability rule); scripts/TradeScene.cs (use domain rule in handler and UI only); tests/ChikagoShop.Domain.Tests/TradeOfferRulesTests.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.Days.cs; README.md. OS TEMP/chikago-hf75-* logs; temporary override.cfg only when absent, removed after checks.
Out of scope: source/, offer RNG/prices, sale after expiry, save schema, other gameplay/domain extraction, old timer/layout test failures, commit/push. No new domain logic in TradeScene.
Acceptance: timeout/manual day close immediately disables reroll button; clicks, F, RerollCustomer and DeclineTrade do not change buyer/offer/remaining rerolls or save; reload and language changes keep it disabled. Current offer remains sellable once; new open day restores reroll availability. Checks: build/domain; GPU --closed-reroll-checks (before/at timeout, all routes, manual close, reload, locale, final sale); --balance-checks; git diff --check.

```text
Task: HF-75
Recommended status: REVIEW
Changed: src/ChikagoShop.Domain/Trading/TradeOfferRules.cs; scripts/TradeScene.cs (consume shared availability rule in reroll handler/UI/day presentation); tests/ChikagoShop.Domain.Tests/TradeOfferRulesTests.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.Days.cs; README.md; ORCHESTRATION_PLAN.md.
Checks: build -> PASS (0 warnings/errors); domain tests -> PASS (125/125, including7 reroll availability cases); GPU --closed-reroll-checks -> PASS; GPU --balance-checks -> PASS; git diff --check -> PASS. Evidence OS TEMP/chikago-hf75-{closed-reroll,balance}.log/.err.
Acceptance: domain CanReroll now requires active trade, open day and remaining rerolls. UI and handler use the same rule; closing refresh disables the button immediately, and reload/locale refresh cannot re-enable it. No new domain logic in TradeScene. Target verifies positive time still permits reroll, expiry/manual close block real disabled-button click, F and both public routes without changing buyer/offer/count/save; reload and RU/EN changes preserve the block. Last offer remains sellable exactly once, and new open game restores reroll. Legacy integration assertions expecting rerolls after closing were updated to the new rule; unrelated old timer/layout expectations left out of scope. README updated.
Read-set exception: none; source/ not scanned.
Risks/blocker: none for this task; no commit/push, earlier edits retained, temporary override.cfg removed and player saves unused.
Next: let the clock reach00 during a deal and check the disabled reroll button and final sale.
```

Координатор подтвердил HF-75 2026-11-08 по domain и targeted GPU regressions: REVIEW → DONE.

### HF-74 — remember last hovered choice in the pause main menu only

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-73 DONE. User requests persistent selection on the last mouse-hovered button, limited to the pause menu shown. One executor, earlier uncommitted changes preserved.
CP-HF-74 read-set: current front; scripts/UI/PauseMenu.cs; scripts/Presentation/ButtonFeedbackController.cs (focus fallback behavior); tests/Integration/ProjectTest.ButtonFeedback.cs, Helpers.cs; tests/ProjectTest.cs; .vscode/settings.json, ChikagoShop.csproj; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/PauseMenu.cs; tests/Integration/ProjectTest.ButtonFeedback.cs. OS TEMP/chikago-hf74-* logs; temporary override.cfg only if absent, removed after checks.
Out of scope: source/, global feedback rules, settings/other menus, gameplay/actions/saves, art/layout, commit/push. Acceptance: hovering an enabled pause-main button focuses/remembers it; moving to blank space retains its highlight; hovering another transfers selection without executing actions; returning from settings/reopening pause restores remembered selection; settings buttons keep existing hover behavior. Checks: build; GPU --button-feedback-checks (hover/leave/transfer/scope/reopen regression), --pause-hotkey-checks, --pause-localization-checks; git diff --check.

```text
Task: HF-74
Recommended status: REVIEW
Changed: scripts/UI/PauseMenu.cs; tests/Integration/ProjectTest.ButtonFeedback.cs; ORCHESTRATION_PLAN.md.
Checks: build -> PASS (0 warnings/errors); GPU --button-feedback-checks -> PASS; GPU --pause-hotkey-checks -> PASS; GPU --pause-localization-checks -> PASS; git diff --check -> PASS. Evidence OS TEMP/chikago-hf74-{button-feedback,pause-hotkey,pause-localization}.log/.err.
Acceptance: only direct pause-main buttons acquire focus and are remembered on MouseEntered. Focus retains their grayscale highlight after cursor departure; ShowMenu restores remembered enabled selection, falling back to Resume. No global feedback change, action activation, settings hover change or geometry change. Actual-input tests hover each main action then blank space, assert retained focus/feedback with no flash or gameplay mutation, verify reopening and settings-back restore selection, and confirm a hovered settings language button does not gain sticky focus.
Read-set exception: none; source/ not scanned.
Risks/blocker: none for this task. Earlier edits retained; no commit/push. Temporary override.cfg removed, genuine saves unused.
Next: hover another pause-menu action and move the pointer away.
```

Координатор подтвердил HF-74 2026-11-08 по build и трём GPU targets: REVIEW → DONE.

### HF-73 — fit image-button feedback to visible artwork

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-72 DONE. User screenshot shows Back feedback border around a much wider control than its visible texture. One executor; previous uncommitted changes retained.
CP-HF-73 read-set: current front; scripts/Presentation/ButtonFeedbackController.cs; scripts/MainMenu.cs (TextureButton configuration), scenes/main_menu.tscn (button resources); tests/ProjectTest.cs, tests/Integration/ProjectTest.ButtonFeedback.cs, Presentation.cs, Resources.cs; art/ui/en/back.png, art/ui/button_back.png (dimensions/alpha only); .vscode/settings.json, ChikagoShop.csproj; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scripts/Presentation/ButtonFeedbackController.cs (texture-aware visual bounds only); tests/ProjectTest.cs (feedback target); tests/Integration/ProjectTest.ButtonFeedback.cs. OS TEMP/chikago-hf73-* logs/screenshots; temporary override.cfg only if absent, removed after checks.
Out of scope: source/, art edits, control sizes/hitboxes, menu layout, action/economy/save changes, other feedback policy, commit/push. Acceptance: TextureButton outline follows fitted visible artwork/alpha bounds rather than empty control margins, including localized Back; normal Button behavior and clipping preserved. Checks: build; GPU --button-feedback-checks with RU/EN Back960x540/1280x720 and rendered margin assertions, --resource-checks/--presentation-checks; git diff --check.

```text
Task: HF-73
Recommended status: REVIEW
Changed: scripts/Presentation/ButtonFeedbackController.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.ButtonFeedback.cs; ORCHESTRATION_PLAN.md.
Checks: build -> PASS (0 warnings/errors); GPU --button-feedback-checks -> PASS; GPU --resource-checks -> PASS; GPU --presentation-checks -> PASS; git diff --check -> PASS. Evidence OS TEMP/chikago-hf73-{button-feedback,resource,presentation}.log/.err and chikago-hf73-back-en.png.
Acceptance: feedback bounds for image buttons now use the displayed texture's stretch/alignment and nontransparent pixel bounds, with flips and parent/viewport clipping. Texture alpha bounds cached through weak keys, avoiding per-frame readback and retaining no texture lifetime. Normal Button visual policy, hitboxes and control sizes unchanged. Regression checks exercise real Russian/English Back at960x540/1280x720, confirm the narrow English outline hugs artwork, empty side-margin pixels remain identical to idle, size stays unchanged and Back still responds. Screenshot inspected: oversized horizontal rectangle gone.
Read-set exception: none; source/ not scanned.
Risks/blocker: none for this change; no commit/push, earlier edits preserved and temporary override.cfg removed.
Next: hover the English Back button in settings.
```

Координатор подтвердил HF-73 2026-11-08 по GPU tests, screenshot и build: REVIEW → DONE.

### HF-72 — remove duplicate text over illustrated craft buttons

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-71 DONE. User screenshot shows faint duplicate CREATE ITEM text over the lettering baked into the button image. One executor, earlier uncommitted work preserved.
CP-HF-72 read-set: current front; scripts/TradeScene.cs (RefreshCraftLabels only), scenes/trade_scene.tscn (GetItemButton/style references); tests/Integration/ProjectTest.Economy.cs, Trading.cs (craft button assertions); tests/ProjectTest.cs; .vscode/settings.json, ChikagoShop.csproj; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scripts/TradeScene.cs (clear the two button Text properties only); tests/Integration/ProjectTest.Economy.cs (RU/EN empty-text regression). OS TEMP/chikago-hf72-* logs; temporary override.cfg only if absent, removed after checks.
Out of scope: source/, bitmap edits, translations of baked-in art, price/chance tooltips, geometry, gameplay/economy/saves, other buttons, commit/push. No domain logic added to TradeScene.
Acceptance: both illustrated craft buttons draw only their existing image lettering, with no overlaid runtime text on initial load or language switch; price hints and craft actions remain functional. Checks: build; GPU --balance-checks (both workshops/languages including no duplicate text); git diff --check.

```text
Task: HF-72
Recommended status: REVIEW
Changed: scripts/TradeScene.cs (RefreshCraftLabels button Text assignments only); tests/Integration/ProjectTest.Economy.cs; ORCHESTRATION_PLAN.md.
Checks: dotnet build ChikagoShop.sln --no-restore -> PASS (0 warnings/errors); GPU --balance-checks -> PASS (both workshops in RU/EN, blank button text, hints/payment/crafting); git diff --check -> PASS. Evidence OS TEMP/chikago-hf72-balance.log/.err.
Acceptance: both craft button Text properties are cleared on initial UI setup and every language refresh; illustrated lettering stays unchanged. Regression asserts no duplicate runtime text in both languages while existing price hints, affordability and craft activation checks still pass.
Read-set exception: none; source/ not scanned.
Risks/blocker: none for this change. Earlier uncommitted work preserved; no commit/push; temporary override.cfg removed, genuine saves unused.
Next: reopen either workshop and check the create-item button.
```

Координатор подтвердил HF-72 2026-11-08 по build и targeted GPU regression: REVIEW → DONE.

### HF-71 — remove on-screen pause button, retain Escape

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-70 DONE. Direct user request; one executor, no active overlapping writer. Earlier uncommitted changes preserved.
CP-HF-71 read-set: current front; scripts/UI/PauseMenu.cs, RarityPanel.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.{Camera,Inventory,Menu,Helpers,ButtonFeedback,Localization,Presentation,Crafting}.cs; README.md, .vscode/settings.json, ChikagoShop.csproj; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/PauseMenu.cs (remove button only), RarityPanel.cs (remove obsolete obstacle); tests/ProjectTest.cs; tests/Integration/ProjectTest.{Camera,Inventory,Menu,Helpers}.cs (Escape regression and replace obsolete clicks); README.md. OS TEMP/chikago-hf71-* logs; temporary override.cfg only if absent, removed after checks.
Out of scope: source/, pause menu actions/localization, other shortcuts, gameplay/economy/saves, old unrelated test failures, commit/push. Acceptance: no visible/hidden PauseButton node; Esc opens pause and closes it, Esc from settings returns to pause menu, key-repeat does not toggle repeatedly; resume/clock behavior and popup placement work without button references. Checks: build; GPU --pause-hotkey-checks/--presentation-checks/--button-feedback-checks/--pause-localization-checks; stale-reference scan and git diff --check.

```text
Task: HF-71
Recommended status: REVIEW
Changed: scripts/UI/PauseMenu.cs; scripts/UI/RarityPanel.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.Camera.cs, Inventory.cs, Menu.cs, Helpers.cs; README.md; ORCHESTRATION_PLAN.md. Earlier uncommitted work retained.
Checks: build -> PASS (0 warnings/errors); GPU --pause-hotkey-checks -> PASS; GPU --presentation-checks -> PASS; GPU --button-feedback-checks -> PASS; GPU --pause-localization-checks -> PASS; stale-reference scan -> PASS (PauseButton occurs only in the regression assertion of absence); git diff --check -> PASS. Evidence OS TEMP/chikago-hf71-{pause-hotkey,presentation,button-feedback,pause-localization}.log/.err.
Acceptance: removed the screen button node, member, signal and show/hide/focus calls; pause remains available via existing Escape handling. Removed the obsolete button from rarity popup avoidance obstacles, and migrated integration clicks to actual Escape press/release input. GPU tests verify absence of the node, pause/open/resume/settings-back, key-repeat guard, frozen clock, Resume menu action, working popup placement, and Escape cancellation of crafting/cards without also opening pause. Existing palette, feedback and pause-language checks pass. README documents Escape only.
Read-set exception: none; source/ not scanned.
Risks/blocker: none for this task; unrelated known failures outside scope. Temporary override.cfg removed, genuine saves unused. No commit/push.
Next: open and close pause with Escape in the running game.
```

Координатор подтвердил HF-71 2026-11-08 по build, четырём GPU targets и проверке ссылок: REVIEW → DONE.

### HF-70 — language selection in in-game pause settings

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-69 DONE. Direct user request. One executor, no active overlapping writer; previous uncommitted HF-68/HF-69 changes preserved.
CP-HF-70 read-set: current front; scripts/UI/PauseMenu.cs, scripts/Localization/GameLocalization.cs, scripts/MainMenu.cs (language menu), tests/ProjectTest.cs, tests/Integration/ProjectTest.Localization.cs, Helpers.cs, Resources.cs; localization/en.tres (existing translation keys); .vscode/settings.json, ChikagoShop.csproj, README.md; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/PauseMenu.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.Localization.cs; README.md. OS TEMP/chikago-hf70-* logs/screenshots; temporary override.cfg only when absent and removed after checks.
Out of scope: source/, main menu redesign, translation catalog expansion, gameplay/save schema, pause semantics, existing tooltip/intro defects, commit/push. No new logic in TradeScene.
Acceptance: pause Settings contains native-name Russian/English toggle buttons; current language selected on open/reopen; switching immediately translates pause/HUD and persists to the existing settings.cfg, preserving unrelated settings and gameplay state/save; main menu uses the same preference; layout fits960x540/1280x720 and both languages, pause remains active. Checks: build; GPU --pause-localization-checks (actual clicks, language/live labels, selected state, save invariance, reload/menu sync, layout); --button-feedback-checks/--resource-checks; git diff --check.

```text
Task: HF-70
Recommended status: REVIEW
Changed: scripts/UI/PauseMenu.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.Localization.cs; README.md; ORCHESTRATION_PLAN.md. Prior HF-68/HF-69 work preserved.
Checks: dotnet build ChikagoShop.sln --no-restore -> PASS (0 warnings/errors); GPU --pause-localization-checks -> PASS; GPU --button-feedback-checks -> PASS; GPU --resource-checks -> PASS; git diff --check -> PASS. Evidence OS TEMP/chikago-hf70-{pause-localization,button-feedback,resource}.log/.err and chikago-hf70-pause-{en,ru}.png.
Acceptance: added LanguageRow with mutually exclusive native-name Russian/English buttons in in-game pause settings. Selection uses existing GameLocalization.SelectLanguage/settings.cfg; translation notification updates selected state, existing Godot translations update pause controls and dynamic HUD immediately. Opening settings refreshes selection. Tests use actual clicks for both languages and960x540/1280x720, verify one selected button/native names/live label and HUD translation, no overlap/offscreen controls, pause remains active, cash/time and gameplay save bytes unchanged, unrelated audio setting preserved. Reloaded preferences/reopened pause and recreated main menu reflect the same selected language. English screenshot visually inspected.
Read-set exception: initial listed pause/localization boundary inspection preceded CP creation; source/ never scanned.
Risks/blocker: none for this change; existing broader tooltip/intro issues outside scope. Temporary override.cfg removed; genuine player settings/saves unused. No commit/push.
Next: open Pause → Settings and switch language in the running game.
```

Координатор подтвердил HF-70 2026-11-08 по build, GPU actual-input regression и snapshot: REVIEW → DONE.

### HF-69 — reveal day-card keyboard selection only after navigation

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-68 DONE. User reports an immediate white selection border on opening day cards and wants it only after pressing arrow keys. Single executor, no parallel writers; prior uncommitted HF-68 changes preserved.
CP-HF-69 read-set: current front/CP-HF-68; scripts/UI/DayTransitionCard.cs, scripts/Presentation/ButtonFeedbackController.cs; tests/ProjectTest.cs, tests/Integration/ProjectTest.ButtonFeedback.cs, Presentation.cs, Days.cs; .vscode/settings.json, ChikagoShop.csproj, README.md; git status/diff. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/DayTransitionCard.cs; tests/Integration/ProjectTest.ButtonFeedback.cs; README.md. Logs/screenshots OS TEMP/chikago-hf69-*; temporary override.cfg only if absent, removed after isolated checks.
Out of scope: source/, other screen focus policies, action/economy/save changes, geometry/art, shader redesign, old tooltip failures, commit/push. No domain changes.
Acceptance: opening/reopening a day card with pointer away leaves no focused/outlined choice; first arrow (or Tab for keyboard accessibility) reveals initial enabled choice without buying; subsequent navigation uses standard focus behavior; mouse hover and press flashes remain; numeric purchase/Escape shortcuts unchanged. Checks: build; GPU --button-feedback-checks with open/reopen/no-focus, arrow navigation/no purchase and disabled-choice tests; GPU --balance-checks; git diff --check.

```text
Task: HF-69
Recommended status: REVIEW
Changed: scripts/UI/DayTransitionCard.cs; tests/Integration/ProjectTest.ButtonFeedback.cs; README.md; ORCHESTRATION_PLAN.md. Existing HF-68 work preserved.
Checks: dotnet build ChikagoShop.sln --no-restore -> PASS (0 warnings/errors); GPU --button-feedback-checks -> PASS; GPU --balance-checks -> PASS; git diff --check -> PASS. Evidence: OS TEMP/chikago-hf69-{button-feedback,balance}.log/.err; screenshot chikago-hf68-card-no-selection.png (shared feedback suite screenshot prefix).
Acceptance: removed unconditional first-choice GrabFocus on Open; Open releases prior focus, first arrow/Tab initializes an enabled choice and consumes only that initial navigation event, subsequent arrows use normal Godot navigation. Numeric shortcuts/Escape, hover and press feedback untouched. Regression checks confirm no focus/outline on first/repeated open even after keyboard use, first/right/down arrows reveal focus without any purchase, later right arrow changes selection without purchasing, unaffordable choices fall back to Skip. GPU screenshot inspected: all three buy buttons initially have no white selection border.
Read-set exception: none; source/ not scanned.
Risks/blocker: none for this change; broader known tooltip failure remains outside scope. No commit/push; temporary override.cfg removed and player saves unused.
Next: reopen the upgrade screen and navigate with arrow keys.
```

Координатор подтвердил HF-69 2026-11-08 по GPU regression, screenshot и build: REVIEW → DONE.

### HF-68 — button hover/press/focus feedback and brief action colors

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; HF-67 DONE. User clarified: hover/focus remain grayscale; color appears only as a short press flash. Green confirms/purchases, red refuses/exits, navigation neutral. One executor, no concurrent write-set owner.
CP-HF-68 read-set: current front/CP-HF-67; AGENTS.md; project.godot, .vscode/settings.json, ChikagoShop.csproj, tools/verify.ps1, README.md; scripts/Presentation/MonochromePresentation.cs, shaders/monochrome.gdshader; scripts/MainMenu.cs, scripts/TradeScene.cs (button names/wiring only), scripts/UI/PauseMenu.cs, DayTransitionCard.cs, EmbroideryGame.cs, SlotUI.cs, InventoryUI.cs; scripts/Camera/WorldCamera.cs (input boundaries); scenes/main_menu.tscn, trade_scene.tscn (button styles only); tests/ProjectTest.cs, tests/Integration/ProjectTest.Presentation.cs, Helpers.cs, Resources.cs, Days.cs; new write-set files; git status/diff. No source/ access.
Write-set: ORCHESTRATION_PLAN.md; project.godot (feedback autoload); scripts/Presentation/ButtonFeedbackController.cs[.uid], MonochromePresentation.cs; shaders/monochrome.gdshader; tests/ProjectTest.cs; tests/Integration/ProjectTest.ButtonFeedback.cs[.uid]; README.md. OS TEMP/chikago-hf68-* logs/screenshots; temporary override.cfg only if absent, removed after isolated checks. User changes preserved.
Out of scope: scene geometry/art, gameplay/actions/economy/save schema, action confirmation dialogs, audio, old tooltip/intro failures, commit/push. No domain logic in TradeScene. Button feedback must never emit or replay gameplay actions.
Acceptance: smooth grayscale hover and keyboard focus; visible grayscale depressed state; only brief semantic press flash, never persistent color while hovering/holding; disabled buttons neither flash nor behave as enabled; runtime-created Button/TextureButton/CheckButton and pause/crafting/card screens supported. No size/layout/input/hitbox changes or stale highlights after disable/hide/free/scene change. Existing clock/table exceptions preserved. Checks: build/domain/import; GPU --button-feedback-checks (rendered pixels + actual mouse/keyboard single-action/toggle/cancel tests, lifecycle/disabled/paused/zoom/resize); GPU --presentation-checks and --balance-checks/--resource-checks; full verify known failures recorded; git diff --check.

```text
Task: HF-68
Recommended status: REVIEW
Changed: project.godot (ButtonFeedback autoload); scripts/Presentation/ButtonFeedbackController.cs[.uid], MonochromePresentation.cs; shaders/monochrome.gdshader; tests/ProjectTest.cs; tests/Integration/ProjectTest.ButtonFeedback.cs[.uid]; README.md; ORCHESTRATION_PLAN.md.
Checks: build -> PASS (0 warnings/errors); domain tests -> PASS (118/118 through verify); Godot import -> PASS; GPU --button-feedback-checks -> PASS; GPU --presentation-checks -> PASS; GPU --resource-checks -> PASS; GPU --balance-checks -> PASS; git diff --check -> PASS. Full tools/verify.ps1 -> FAIL at existing TestUiLayoutInvalidation tooltip assertion after successful build/domain/import. Evidence OS TEMP/chikago-hf68-{import,button-feedback,presentation,resource,balance,verify}.log/.err; screenshots chikago-hf68-{hover-gray,confirm-flash,refuse-flash,held-gray,disabled-gray,keyboard-focus,texture-button,craft-focus,pause-focus,card-focus,minigame-focus}.png.
Acceptance: global feedback observes BaseButton signals and input modality, never emits or consumes gameplay input. Hover/focus fade in grayscale, held state darkens, semantic color pulses for0.18s after press/activation then fades even during a long hold. Green = confirm/buy/start/resume, red = refusal/exit/cancel/reset, neutral = other navigation. Button art/styles/layout and hitboxes remain untouched; final presentation shader draws the feedback after grayscale, so only the transient pulse gains color. Original clock/table exception policy remains intact. Buttons added dynamically are attached once, handlers removed on tree exit, hidden/disabled/paused-out controls lose feedback immediately. Screen bounds follow camera/resize and ancestor clipping. Pointer is a hand for available buttons and forbidden for disabled ones; keyboard focus takes priority after keyboard navigation.
Acceptance tests: actual GPU pixel checks verify gray hover/focus/held/disabled states and green/red/TextureButton flashes; long hold expires pulse; mouse release and Space invoke original action once, dragged-out release invokes none; CheckButton toggles once; re-enable under stationary mouse restores gray hover; hidden/free controls leave no flash; real create buttons track960x540/1280x720 and zoom0.25/1.8; real pause/card/crafting focus works while paused. Synthetic and real-card/create screenshots visually inspected.
Read-set exception: initial button/presentation boundary inspection before CP was needed to define exact implementation scope; source/ never scanned.
Risks/blocker: existing tooltip FAIL and duplicate root/ru texture UID warnings remain outside scope. User explicitly chose short color flashes only; no permanent color hover was added. No commit/push. Temporary override.cfg removed, player saves unused.
Next: try selling, rerolling, crafting and day-card purchases with the new button feedback.
```

Координатор подтвердил HF-68 2026-11-08 по GPU feedback/palette checks, input assertions, build/domain и existing targets: REVIEW → DONE. Прежний tooltip FAIL сохранён явно.

### HF-67 — monochrome presentation with clock/table exceptions

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08. Direct user request after HF-66 DONE; no gate dependencies or concurrent implementation tasks. Plan owned by coordinator, HF-64 gameplay/export work remains outside scope.
CP-HF-67 read-set: current front, AGENTS.md; project.godot, .vscode/settings.json, ChikagoShop.csproj, tools/verify.ps1, README.md; scripts/MainMenu.cs, scripts/TradeScene.cs (visual boundaries only), scripts/UI/RarityPanel.cs, EmbroideryGame.cs, DayTransitionCard.cs, PauseMenu.cs, ItemInfoPopup.cs, PolishingSurface.cs; scripts/Days/DayCycleController.cs; scripts/Dialogue/DialoguePlayer.cs; scenes/main_menu.tscn, trade_scene.tscn, scenes/UI/RarityPanel.tscn; themes/game_theme.tres; tests/ProjectTest.cs, tests/Integration/ProjectTest.{Ui,Days,Crafting,Helpers,Resources}.cs; art/ui/day_timer_empty.png[.import], day_timer_expired.png[.import] (resource identity/alpha only); new write-set files; git status/diff. Diagnostic screenshots/logs in OS TEMP/chikago-hf67-*.
Write-set: ORCHESTRATION_PLAN.md; project.godot (presentation autoload only); scripts/Presentation/MonochromePresentation.cs[.uid]; shaders/monochrome.gdshader[.uid]; scripts/UI/RarityPanel.cs (register table exception), EmbroideryGame.cs, DayTransitionCard.cs, PauseMenu.cs (register covering modal surfaces only); scripts/Days/DayCycleController.cs (register clock sprite exception only); tests/ProjectTest.cs (target/runner); tests/Integration/ProjectTest.Presentation.cs[.uid]; README.md. Temporary override.cfg only when absent, removed after isolated tests. No active overlapping writer; user/editor changes preserved.
Out of scope: source/, art edits/generation, gameplay/economy/save schema, scene layouts, old tooltip/intro defects, export, commit/push. No new domain logic in TradeScene.
Acceptance: final rendered game/menu/dialogue/minigames/pause/cards use continuous grayscale; only visible clock sprite and craft chance/category price tables preserve original colors. Existing alpha, layout, input and layer order remain unchanged; covered exceptions do not leak color through modal screens. Color preservation follows layout/resize/show/hide and scene changes. Tests: build/domain, Godot import, GPU --presentation-checks (rendered pixels with/without exceptions, real clock/table, resize/modal/scene lifecycle and screenshots), existing --balance-checks/--resource-checks, git diff --check. Full verify may retain documented unrelated failures.

```text
Task: HF-67
Recommended status: REVIEW
Changed: project.godot (presentation autoload); scripts/Presentation/MonochromePresentation.cs[.uid]; shaders/monochrome.gdshader[.uid]; scripts/Days/DayCycleController.cs (clock exception registration); scripts/UI/RarityPanel.cs (table exception registration); scripts/UI/EmbroideryGame.cs, DayTransitionCard.cs, PauseMenu.cs (covering modal registration); tests/ProjectTest.cs; tests/Integration/ProjectTest.Presentation.cs[.uid]; README.md; ORCHESTRATION_PLAN.md.
Checks: dotnet build ChikagoShop.sln --no-restore -> PASS (0 warnings/errors); domain tests -> PASS (118/118 through full verify); Godot import -> PASS; GPU --presentation-checks -> PASS (D3D12/RTX3060, actual rendered pixel checks); GPU --resource-checks -> PASS; GPU --balance-checks -> PASS (live economy, clock and paid cards); git diff --check -> PASS. Full tools/verify.ps1 -> FAIL only at previously documented tooltip assertion after successful build/domain/import. Evidence OS TEMP/chikago-hf67-{import,gpu,resource,balance,verify}.log/.err and screenshots chikago-hf67-{menu,dialogue,world,atelier,musicworkshop,pause,embroiderygame,polishinggame,day-card,probe-original}.png.
Acceptance: one final grayscale/luminance pass at CanvasLayer200 covers world, sprites, imported art, custom-drawn minigames, UI, menus and custom dialogue shaders. Exceptions use actual clock sprite alpha and visible screen-space rectangles of the two rarity/chance/category-price tables, preserving original green/red values. No assets/materials/layouts/gameplay/save data changed; source/ never scanned. Tables/clock stay in their existing canvases and input paths; full-screen filter ignores mouse and keyboard focus. Modal surfaces temporarily suppress exceptions to prevent color leaking into overlapping UI. RenderingServer.FramePreDraw updates masks while paused and after layout, resize, camera zoom, expiry and visibility changes.
Acceptance tests: deterministic RGB probe is colored before and gray after filter; menu/settings/intro pixels grayscale; real expired clock remains red, transparent clock corners over a magenta test patch become gray; both tables retain colored values at960x540/1280x720 and camera zoom0.25/1.8; pixels outside exceptions remain gray; pause, embroidery, polishing and day cards are gray; freed scene exceptions leave no colored holes on return to menu. World/table/embroidery screenshots visually inspected.
Read-set exception: initial inspection of listed presentation boundaries preceded CP creation to define exact write-set; no other exceptions.
Risks/blocker: full GUI runner retains old TestUiLayoutInvalidation tooltip FAIL; import retains four existing root/ru texture duplicate-UID warnings. Intro/HF-64 defects not changed. Final screen-reading shader adds one presentation pass. No commit/push; temporary override.cfg removed, genuine player saves unused.
Next: play the grayscale version and assess tonal readability during trading and both minigames.
```

Координатор подтвердил HF-67 2026-11-08 по GPU pixel tests, visual snapshots, build/domain и targeted regressions: REVIEW → DONE. Общий tooltip FAIL явно сохранён.

### HF-66 — persistence, subsystem tests and resource organization

Status: `DONE`; coordinator/executor `/root`; READY 2026-11-08; depends on HF-65 DONE. Single executor; overlapping HF-64 files are owned serially by the same coordinator, no parallel writes.
CP-HF-66 read-set: current front, AGENTS.md, README.md, ChikagoShop.csproj, ChikagoShop.sln, project.godot, export_presets.cfg, .vscode/settings.json, .gitignore, tools/verify.ps1; scripts/TradeScene.cs, scripts/MainMenu.cs, scripts/Days/DayCycleController.cs, DayNightBackground.cs; scripts/UI/InventoryUI.cs, EmbroideryGame.cs, PolishingSurface.cs; tests/ProjectTest.cs, tests/ChikagoShop.Domain.Tests/*.cs and csproj; src/ChikagoShop.Domain/Days/*.cs, Configuration/*.cs; scenes/*.tscn, scenes/UI/*.tscn, themes/game_theme.tres (resource references); art file metadata/names/SHA256/.import, MyFont_Completed.ttf[.import], art/fonts/game.ttf[.import]; new write-set files, git status/diff.
Write-set: ORCHESTRATION_PLAN.md; scripts/TradeScene.cs (persistence extraction only); scripts/Persistence/TradeScene.Persistence.cs, ProgressSaveStore.cs; src/ChikagoShop.Domain/Persistence/ProgressData.cs, ProgressMigration.cs, ProgressJson.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.{Crafting,Camera,Ui,Inventory,Trading,Days,Economy,Menu,Localization,Persistence,Resources,Helpers}.cs; tests/ChikagoShop.Domain.Tests/ProgressMigrationTests.cs; rename art files and their .import to snake_case, move art/npc/* to art/reference/characters/*; remove duplicate MyFont_Completed.ttf[.import] after glyph verification; scripts/MainMenu.cs, scripts/Days/DayNightBackground.cs, scripts/UI/InventoryUI.cs, EmbroideryGame.cs, PolishingSurface.cs and scenes/main_menu.tscn (resource paths only); README.md. OS TEMP/chikago-hf66-* tooling/logs. Temporary override.cfg only if absent, removed after isolated smoke checks.
Out of scope: source/, bitmap edits, design/balance changes, new save schema, unrelated tooltip failure, export/commit/push. No new domain logic in TradeScene. Preserve previous HTML deletions and user edits.
Acceptance: unchanged version13 JSON contract and legacy behavior; DTO/JSON/migrations outside scene, separate Godot I/O; subsystem integration files retain runner/flags/checks; snake_case resources retain bytes/UIDs and all static/dynamic references; runtime characters distinguished from references; one font with confirmed Latin/Cyrillic glyphs. Checks: build/domain tests, Godot import, persistence/resource targeted integration, existing targets, full verify with known failures reported, diff check and resource byte/reference verification.

```text
Task: HF-66
Recommended status: REVIEW
Changed: scripts/TradeScene.cs; scripts/Persistence/TradeScene.Persistence.cs, ProgressSaveStore.cs; src/ChikagoShop.Domain/Persistence/ProgressData.cs, ProgressJson.cs, ProgressMigration.cs; tests/ProjectTest.cs; 12 subsystem partial files in tests/Integration/; tests/ChikagoShop.Domain.Tests/ProgressMigrationTests.cs; 35 art resource renames and associated .import; art/npc moved to art/reference/characters; duplicate root MyFont_Completed.ttf[.import] deleted; resource path updates in MainMenu, DayNightBackground, InventoryUI, EmbroideryGame, PolishingSurface, main_menu.tscn; README.md; ORCHESTRATION_PLAN.md. Godot generated adjacent .cs.uid files are part of new source artifacts. Prior HF-65 HTML deletions retained.
Checks: dotnet build ChikagoShop.sln --no-restore -> PASS (0 warnings/errors); domain tests -> PASS (118/118, including 21 migration/JSON cases); Godot import -> PASS; headless --resource-checks -> PASS (own Latin/Cyrillic font glyphs, all art loads, localized menu); headless --persistence-checks -> PASS (existing save/checkpoint tests + versions1/5/6/7/8/9/11/12/13, invalid JSON, repeated migration/reload, stock, price upgrades); GPU --balance-checks -> PASS (live economy, clock and paid cards); SHA256 and UID manifest verification -> PASS (35/35); exact-case static resource path verification -> PASS; original 42 integration methods retained once -> PASS; original 40 DTO properties retained in order and JSON names verified -> PASS; git diff --check and --cached --check -> PASS.
Acceptance: TradeScene core 1601 ->1258 lines; persistence adapter is a separate partial file, JSON/I/O/legacy interpretation are separate components. ProjectTest runner 3293 ->104 lines; original tests distributed by subsystem, flags and order retained, new persistence/resource targets added. TestProgress stale numeric expectations now derive from current economy. SaveVersion13 and wire fields unchanged. Every renamed bitmap retains original bytes and UID. Runtime art/characters vs art/reference/characters documented. Both original fonts have identical SHA256 c453f9141c92ff051fcf893fbabb7d35fde97d57b1b2681b6755b2d0baaf3faf and direct cmap coverage for A-Z/a-z/0-9 and Russian including Ё/ё; canonical art/fonts/game.ttf retained. No bitmap generation/editing.
Read-set exception: initial boundary/metadata inspection before CP creation was necessary to define exact extraction scope. source/ never scanned. Godot process metadata inspected to diagnose concurrent main_menu.tscn save.
Risks/blocker: full verify still FAIL at existing TestUiLayoutInvalidation tooltip assertion after build/domain/import/new resource+save checks pass. GPU intro-transition target FAIL at serialized GameScene reference assertion: main_menu.tscn acquired concurrent editor changes during this task (GameScene=null and Title anchors/position changed); these edits were preserved, not overwritten. MainMenu's existing null fallback remains unchanged. Import reports existing duplicate UIDs for four root/ru button textures. These are outside this refactoring. Case-only asset renames recorded with git mv (index contains those renames), other edits remain unstaged; no commit/push.
Next: resolve the menu scene's serialized GameScene setting together with the outstanding HF-64 work.
```

Координатор подтвердил HF-66 2026-11-08: REVIEW → DONE для согласованного рефакторинга; отдельные tooltip/intro результаты выше не объявлены зелёными. Evidence: OS TEMP/chikago-hf66-{import,resource,persistence,intro-transition,balance,verify}.log и .err; resource-manifest.json. Временный override.cfg удалён, настоящие пользовательские saves не использовались.

### HF-65 — удалить HTML-прототипы и проверить структуру папок

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-08. Прямое поручение пользователя, task/gate-зависимостей нет; HF-64 не изменяется. Активных пересечений файлов реализации нет; план принадлежит координатору `/root`.
CP-HF-65: read-set — текущий фронт, AGENTS.md, README.md, project.godot, export_presets.cfg, .gitignore, ChikagoShop.csproj, src/ChikagoShop.Domain/ChikagoShop.Domain.csproj, tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj, tests/ChikagoShop.Domain.Tests/ArchitectureTests.cs, tools/ChikagoShop.Balance/ChikagoShop.Balance.csproj, scripts/TradeScene.cs (границы ответственности), tests/ProjectTest.cs (число строк), MyFont_Completed.ttf и art/fonts/game.ttf (только SHA256); metadata/listing scripts/, src/, scenes/, tests/, tools/, art/, config/, dialogue/, localization/, themes/, docs/ без bin/obj; поиск ссылок на HTML и шрифты в runtime/test/tool текстах перечисленных каталогов; git status/diff. Write-set — ORCHESTRATION_PLAN.md; удаление atelier_embroidery_v4.html и atelier_polishing_v8.html. Out of scope — source/, generated builds/.godot/bin/obj, перемещение ресурсов/скриптов, рефакторинг, изменение игрового поведения, commit/push.
Приёмка: два HTML-прототипа удалены, в проектных каталогах вне архивов/generated больше нет HTML; runtime-ссылки на удаляемые файлы отсутствуют; дано заключение по папкам с подтверждёнными проблемами. Проверки: rg --files с исключением source/generated; поиск ссылок; dotnet build ChikagoShop.sln; dotnet test tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj --no-build --filter FullyQualifiedName~ArchitectureTests; git diff --check. Изменений поведения нет, новые тесты не требуются.


```text
Task: HF-65
Recommended status: REVIEW
Changed: ORCHESTRATION_PLAN.md; удалены atelier_embroidery_v4.html и atelier_polishing_v8.html.
Checks: поиск runtime/test/tool ссылок на HTML -> PASS (нет совпадений); поиск оставшихся HTML вне source/generated -> PASS (0 файлов); dotnet build ChikagoShop.sln --no-restore -> PASS (0 warnings/errors); dotnet test tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj --no-build --filter FullyQualifiedName~ArchitectureTests -> PASS (1/1); git diff --check -> PASS.
Acceptance: HTML-прототипы удалены. Структура папок пригодна для текущего масштаба: scripts/scenes — Godot, src/ChikagoShop.Domain — независимый домен, tests — проверки, tools — verify/CLI, art — ресурсы, config/dialogue/localization/themes — данные. Подтверждена независимость domain assembly от Godot. Замечания: TradeScene.cs 1601 строк совмещает orchestration/UI, торговлю, save/load/migration и DTO; tests/ProjectTest.cs 3293 строки; корневой MyFont_Completed.ttf побайтно дублирует art/fonts/game.ttf (SHA256 совпадает), runtime-ссылки используют art/fonts/game.ttf; art/npc и art/characters требуют явного разграничения назначения (customer.tscn использует characters). Названия ресурсов смешивают PascalCase/snake_case/кириллицу и регистр PNG. Переносы требуют отдельной задачи с обновлением Godot paths/UID.
Read-set exception: предварительный metadata/dependency обзор перечисленных файлов до создания CP понадобился для определения точного пакета проверки; source/ не сканировался.
Risks/blocker: аудит папок и границ проектов не подтверждает корректность всех игровых сценариев; полный GUI runner не запускался. Игровое поведение не изменялось.
Next: отдельной задачей выделить сохранения из TradeScene с регрессионными тестами миграций.
```

Координатор подтвердил HF-65 2026-11-08 по сборке, architecture test и проверкам удаления: REVIEW → DONE. Изменения не закоммичены и не отправлены.
### HF-64 — исправить переход после диалога и собрать Windows билд

Статус: `IN_PROGRESS`; координатор/исполнитель `/root`; HF-63 DONE; READY 2026-11-07. Пользователь сообщил возврат к NewGame после вступления и запросил рабочий билд.
CP-HF-64: read-set — текущий фронт, scripts/MainMenu.cs (menu/intro/EnterGame), scripts/Dialogue/DialoguePlayer.cs (Finished/Cancelled), scenes/main_menu.tscn, scenes/trade_scene.tscn (ресурс назначения), project.godot, export_presets.cfg, .gitignore, .vscode/settings.json, ChikagoShop.csproj, tests/ProjectTest.cs (startup/runner), README.md, git status/diff. Диагностика: metadata установленного Godot/.NET/export templates и существующего Desktop/new_pop (только список/лог), OS TEMP/chikago-hf64-* logs. Write-set — ORCHESTRATION_PLAN.md, scripts/MainMenu.cs (разрешение GameScene перед изменением save), scripts/Dialogue/DialoguePlayer.cs (не перехватывать click кнопки Skip поверх диалога), scenes/main_menu.tscn (только ссылка GameScene/extresource), export_presets.cfg (include_filter нужныхJSON без изменения пользовательского export_path), .gitignore (только /builds/), tests/ProjectTest.cs (startup regression/runner), README.md (путь нового билда); builds/windows-fixed/* и builds/ChikagoShop-windows-fixed.zip как generated artifacts; временный override.cfg (только если отсутствовал, удаляется после smoke) для изоляции userdata в ChikagoShopStartupSmoke; такой же временный override рядом с новым exe. Настоящие user saves тестами не открываются. Владелец сцены/плана `/root`, активных пересечений нет. Локальные HF-61…63 и пользовательские project/export/scene правки сохраняются.
Out of scope: source/, арт/баланс/схема saves, удаление старого билда/сохранений пользователя, исправление постороннего tooltip-теста, commit/push.
Diagnosis: scenes/main_menu.tscn содержит GameScene=null, перекрывая инициализатор C#; EnterGame получает null и возвращает ShowGameChoice. При исправлении fallback определяется до reset сохранения. В экспорт должны входить config/*.json и dialogue/*.json.
Приёмка: обычное завершение диалога и Skip после NewGame открывают TradeScene; Continue работает; при ошибочной пустой inspector-ссылке применяется default scene без возврата в menu. Сохранения пользователя не используются тестами/не удаляются. Windows release экспортируется в новый folder внутриworkspace, фактический exported exe проходит отдельный startup-smoke с packed resources и .NET libraries; runnable folder/zip доступны пользователю. Проверки: build/domain, targeted intro/skip/fallback smoke в редакторном и экспортном режиме, git diff --check; полная прежняя tooltip-регрессия фиксируется отдельно.
### HF-63 — 90 секунд и баланс крафта одного предмета

Статус: `DONE`; координатор/исполнитель `/root`; HF-62 DONE; READY 2026-11-07. Пользователь поручил90сек с первого дня, оставить1предмет за крафт, обосновать стоимость и умеренную сложность квот; суммы квот можно корректировать.
CP-HF-63: read-set — текущий фронт, config/economy.v1.json, scripts/TradeScene.cs (цены/энергия/квоты/крафт/initial offer/reroll/save duration), scripts/Days/DayCycleController.cs, scripts/UI/EmbroideryGame.cs, scripts/Crafting/WorkshopItemCatalog.cs, scripts/Configuration/EconomyConfigProvider.cs, src/ChikagoShop.Domain/Configuration/EconomyConfig.cs, EconomyConfigLoader.cs, EconomyConfigValidator.cs в той же папке, src/ChikagoShop.Domain/Days/DaySession.cs, DayUpgradeSession.cs в той же папке, src/ChikagoShop.Domain/Economy/MoneyMath.cs, src/ChikagoShop.Domain/Crafting/WeightedItemTable.cs, tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, DayUpgradeSessionTests.cs, EconomyConfigTests.cs, DayCardPurchaseTests.cs в той же папке, tests/ProjectTest.cs, ChikagoShop.csproj, src/ChikagoShop.Domain/ChikagoShop.Domain.csproj, localization/en.tres, README.md, tools/verify.ps1, scenes/trade_scene.tscn (только проверка override цен), git status/diff. Write-set — ORCHESTRATION_PLAN.md, config/economy.v1.json, scripts/TradeScene.cs (только подключение config/готовых domain правил/динамические labels и миграция времени), scripts/Days/DayCycleController.cs, scripts/UI/EmbroideryGame.cs, scripts/Crafting/WorkshopItemCatalog.cs, src/ChikagoShop.Domain/Configuration/EconomyConfig.cs, EconomyConfigValidator.cs в той же папке, src/ChikagoShop.Domain/Days/DaySession.cs, src/ChikagoShop.Domain/Trading/TradeOfferRules.cs, src/ChikagoShop.Domain/Simulation/WeekBalanceSimulator.cs, tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, DayUpgradeSessionTests.cs, EconomyConfigTests.cs, EconomyBalanceTests.cs, TradeOfferRulesTests.cs в той же папке, tests/ProjectTest.cs, ChikagoShop.csproj (исключить CLI sources), tools/ChikagoShop.Balance/ChikagoShop.Balance.csproj, Program.cs, .gdignore в той же папке, docs/BALANCE_REPORT.md, localization/en.tres, README.md. OS TEMP/chikago-hf63-* logs и отчёты. Владелец `/root`, активных пересечений нет; незакоммиченные HF-61/HF-62 сохраняются.
Результат явно включает извлечение существующего price/initial-percent/reroll-pool из TradeScene в Godot-независимый TradeOfferRules, чтобы игра и симулятор считали одинаково; новой доменной логики TradeScene не добавляется. Общий economy config задаёт цены/энергию/квоты/длительность/1предмет. До правок прототипный расчёт показал отрицательную доходность90$/140$ против средних45.3$/67.95$.
Out of scope: source/, арты/сцены/новые механики/рандомное подкручивание, изменение существующих rarity/upgrade эффектов и цен карт, исправление несвязанных старых GUI тестов, commit/push.
Приёмка: новый день90сек независимо от мастерской (+30 карты остаётся), legacy saves сохраняют долю оставшегося времени, clock90→00; один крафт=один предмет, честные RU/EN labels и disabled/purchase границы по общей цене. Воспроизводимая CLI симуляция минимум10000 seeds/профиль с фактическими весами/офферами/рероллами/энергией/временем/квотами/картами, показывает pass rates обеих квот для пассивной/обычной/умелой/медленной/без музыки стратегий; документированы допущения темпа и ограничение модели. Новые domain/targeted Godot balance+clock+paid-card tests PASS; полный verify выполняется и любые оставшиеся прежние GUI FAIL честно фиксируются; git diff --check PASS.
```text
Task: HF-63
Recommended status: REVIEW
Changed: config/economy.v1.json; src/ChikagoShop.Domain/Configuration/EconomyConfig.cs, EconomyConfigValidator.cs; src/ChikagoShop.Domain/Days/DaySession.cs; src/ChikagoShop.Domain/Trading/TradeOfferRules.cs; src/ChikagoShop.Domain/Simulation/WeekBalanceSimulator.cs; scripts/TradeScene.cs (config/готовые domain calls/labels/save migration); scripts/Days/DayCycleController.cs; scripts/UI/EmbroideryGame.cs; scripts/Crafting/WorkshopItemCatalog.cs; tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, DayUpgradeSessionTests.cs, EconomyConfigTests.cs, EconomyBalanceTests.cs, TradeOfferRulesTests.cs; tests/ProjectTest.cs; ChikagoShop.csproj; tools/ChikagoShop.Balance/ChikagoShop.Balance.csproj, Program.cs, .gdignore; docs/BALANCE_REPORT.md; README.md; ORCHESTRATION_PLAN.md. Локальные HF-61/HF-62 сохранены.
Checks: build -> PASS; domain tests -> PASS (97/97, прежние14 ожиданий таймера обновлены по90сек); Godot import -> PASS; GPU ProjectTest --balance-checks -> PASS (Live economy, clock and paid upgrade regression checks passed); CLI --runs10000 --seed20261107 -> PASS (60000 серий); git diff --check -> PASS. Полный tools/verify.ps1 -> FAIL после успешных build/domain/import на GUI assertion «Showing a tooltip must immediately activate its presentation updates» (TestUiLayoutInvalidation), вне нового balance target. Evidence: OS TEMP/chikago-hf63-verify.log, chikago-hf63-gui.log, chikago-hf63-simulation.log.
Acceptance: числа уже вставлены в игру, как дополнительно уточнил пользователь: каждый день90сек с самого первого, +30 карты остаётся; ровно1предмет за25$ atelier /50$ music и1энергию; старт220$, энергия3/6 и открытие100$ сохранены; квоты250$ после4,450$ после7. Config одновременно задаёт фактическую оплату, доступность мини-игр, RU/EN labels и CLI симуляцию. Средняя база45.30$/67.95$ оставляет20.30$/17.95$ ожидаемой маржи до оффера; вероятность отдельного убытка сохраняется. Общий TradeOfferRules вынесен из старого TradeScene: price half-up, initial uniform -30…30, pool реролла70% выше/30% ниже; игра и модель используют тот же код. Save v13 сохраняет долю остатка60/90/120-секундных legacy дней, timeout остаётся0. Новые domain tests проверяют конфиг/цены/офферы/детерминированность/сложность/миграцию. GPU target проверяет оба workshop при точной цене и цене−1, один item/energy/payment при повторном старте, оба языка, реальные10сек крафта, квоты ровно и−1, legacy half-day; затем clock и платные карты/skip/reload.
Calibration:10000 seeds на профиль — первая/обе квоты: ordinary89.3%/47.9%, careful rerolls99.4%/88.0%, careful+category investments99.0%/97.8%, slow93.2%/38.7%, clothing-only100%/64.6%, passive0%/0%. Отчёт фиксирует темп15+buyer-delay (обычный),23+delay (медленный),10сек крафт внутри цикла,0.8сек на reroll. Это оценки модели, не обещание win rate игрокам; скрытой подгонки нет.
Read-set exception: нет; source/ не читался. Извлечение существующей логики из TradeScene заранее включено в CP/write-set.
Risks/blocker: численный баланс provisional, нужен живой playtest; общий GUI runner не зелёный из-за проверки tooltip вне нового target. Имеющиеся asset warnings HF-60 не правились. Commit/push не выполнялись.
Next: сыграть неделю с25$/50$ и квотами250$/450$, затем корректировать по фактическому темпу игрока.
```

Координатор подтвердил HF-63 2026-11-07 по97 domain tests, targeted GPU и60000 simulation runs: REVIEW → DONE. Пользователь попросил вставить примерные числа; это выполнено. Остаточный общий GUI FAIL явно сохранён.
### HF-62 — секундный таймер в часах слева снизу

Статус: `DONE`; координатор/исполнитель `/root`; HF-61 DONE; READY 2026-11-07.
CP-HF-62: read-set — текущий фронт, scripts/Days/DayCycleController.cs, scripts/UI/EmbroideryGame.cs (копирование DayTimer), scripts/UI/InventoryUI.cs (нижняя полоса), scenes/trade_scene.tscn, scenes/UI/InventoryUI.tscn (позиции), tests/ProjectTest.cs (таймер/runner), src/ChikagoShop.Domain/Days/DaySession.cs (текущие длительности), README.md, .vscode/settings.json, ПустаяОсноваЧасов.png, ЗаполненнаяОсноваЧасов.png и их .import при наличии, git status/diff. Write-set — ORCHESTRATION_PLAN.md, scripts/Days/DayCycleController.cs, tests/ProjectTest.cs, README.md; перенос (без изменения bitmap) ПустаяОсноваЧасов.png[.import] → art/ui/day_timer_empty.png[.import], ЗаполненнаяОсноваЧасов.png[.import] → art/ui/day_timer_expired.png[.import]. Диагностические logs/screenshots OS TEMP/chikago-hf62-* и chikago-clock-{active,expired}.png. Владелец `/root`, пересечений нет; локальные HF-61 и пользовательские правки сохранены.
Out of scope: source/, редактирование изображений/сцен, стрелки, смена длительности дней/баланса/save schema, посторонние14 старых тестов, commit/push.
Приёмка: HUD/Root/DayTimer внизу слева; внутри пользовательского пустого циферблата ceil оставшихся секунд с минимум2цифрами (90…01…00), при0 заполненный красный ассет; без стрелок и mm:ss. Reset/reload/timeout/manual close/time-bonus и копия в мини-игре согласованы. При960×540/1280×720 и3/6слотах/zoom нет перекрытия инвентаря или перехвата mouse. Файлы перенесены в art/ui без изменения bytes. Проверки: build, import, targeted GPU ProjectTest --day-timer-checks; snapshots; git diff --check; известный full verify FAIL фиксируется отдельно.
```text
Task: HF-62
Recommended status: REVIEW
Changed: scripts/Days/DayCycleController.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md; пользовательские PNG перенесены из корня в art/ui/day_timer_empty.png и art/ui/day_timer_expired.png, созданы соответствующие Godot .import.
Checks: dotnet build -> PASS (0 warnings/errors); Godot import -> PASS; GPU ProjectTest --day-timer-checks --clock-preview -> PASS (exit0); screenshots OS TEMP/chikago-clock-active.png и chikago-clock-expired.png осмотрены; SHA256 до/после переноса обоих PNG совпадает; git diff --check -> PASS. Evidence: OS TEMP/chikago-hf62-build.log, chikago-hf62-import.log, chikago-hf62-gui.log.
Acceptance: Label HUD/Root/DayTimer сохранён для совместимости, перенесён BottomLeft с отступом12; пользовательская картинка без стрелок рисуется дочерним Clock за цифрами. Числа ceil(seconds) минимум2цифры:90,89,…01,00; на0 активируется красная заполненная основа. Изображения не редактировались. Формат автоматически копируется в мини-игру существующим кодом. Реальные60/90сек и +30 карты не меняются. Clock-frame адаптируется к свободному месту слева от инвентаря; targeted GUI при960×540/1280×720, zoom0.25/1.8 и3/6слотах подтверждает bounds/неперекрытие/ignore mouse/font>=18. Проверены пауза, дробные секунды, timeout/manual close, reset/следующий день, reload точного остатка, купленные120сек и copy в embroidery. GPU screenshot90/00 показывает правильное положение цифр в предоставленных циферблатах.
Read-set exception: нет; source/ не читался. Проверены абсолютные source/destination перед Move-Item; файлы остались внутри workspace/art/ui.
Risks/blocker: общие14 прежних доменных FAIL HF-60/HF-61 остаются; домен/тайминги в этой задаче не менялись. При960px и6слотах часы уменьшаются, чтобы не перекрыть инвентарь. Commit/push не выполнялись.
Next: перезапустить сцену и посмотреть часы слева снизу.
```

Координатор подтвердил HF-62 2026-11-07 по build/import, targeted GPU regression, screenshots и diff-check: REVIEW → DONE.
### HF-61 — платные карточки и бесплатный пропуск

Статус: `DONE`; координатор/исполнитель `/root`; HF-60 DONE; READY 2026-11-07.
CP-HF-61: read-set — текущий фронт, src/ChikagoShop.Domain/Days/DayUpgradeSession.cs, scripts/UI/DayTransitionCard.cs, scripts/Days/DayCycleController.cs, tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs, tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj, tests/ProjectTest.cs (карточки/runner), localization/en.tres, README.md, .vscode/settings.json, git status/diff; write-set — ORCHESTRATION_PLAN.md, src/ChikagoShop.Domain/Days/DayUpgradeSession.cs, scripts/UI/DayTransitionCard.cs, scripts/Days/DayCycleController.cs, tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs, tests/ChikagoShop.Domain.Tests/DayCardPurchaseTests.cs, tests/ProjectTest.cs, localization/en.tres, README.md. Расширение CP координатором: read-set — scripts/TradeScene.cs (EnergyCapacity/save/load), src/ChikagoShop.Domain/Days/DaySession.cs (текущий таймер); write-set — scripts/TradeScene.cs только forwarding расчёта EnergyCapacity в domain. Причина: новая платная energy-карта обрезалась текущим лимитом3/6 и не давала оплаченного бонуса. Диагностика: tools/verify.ps1, OS TEMP/chikago-hf61-*.log и preview screenshot. Владелец `/root`, активных пересечений нет; пользовательские изменения HF-60 сохраняются.
Out of scope: source/, сцены/ассеты, смена таймера/фона/арха/баланса HF-60, исправление14 известных старых тестов, новая доменная логика TradeScene, commit/push.
Приёмка: на каждой карте цена (cash20$,energy/time40$,rarity80$,price100$), покупка строго одной с атомарной проверкой денег/списанием/эффектом; недоступные кнопки disabled, hotkey/direct purchase не обходят баланс. Бесплатный «Пропустить»/Esc оставляет деньги/бонусы, снимает паузу, pending=false сохраняется и после reload не повторяется. RU/EN/layout при960×540/1280×720; targeted domain и GUI purchase/skip/reload checks PASS; полный verify фиксирует прежние14 FAIL отдельно от новой задачи. Staged/working diff-check PASS.
```text
Task: HF-61
Recommended status: REVIEW
Changed: src/ChikagoShop.Domain/Days/DayUpgradeSession.cs; scripts/UI/DayTransitionCard.cs; scripts/Days/DayCycleController.cs; scripts/TradeScene.cs (только domain forwarding EnergyCapacity); tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs; tests/ChikagoShop.Domain.Tests/DayCardPurchaseTests.cs; tests/ProjectTest.cs; localization/en.tres; README.md; ORCHESTRATION_PLAN.md.
Checks: targeted dotnet test --filter FullyQualifiedName~DayCardPurchaseTests -> PASS (11 tests); build -> PASS; Godot import -> PASS; GPU ProjectTest --upgrade-purchase-checks --day-card-preview -> PASS (exit0, Paid upgrade and skip checks passed); screenshot OS TEMP/chikago-paid-cards-preview.png осмотрен; git diff --check -> PASS. Полный tools/verify.ps1 -> прежний FAIL (14 failed,67 passed из81); старые14 не исправлялись. Evidence: OS TEMP/chikago-hf61-verify.log, chikago-hf61-domain.log, chikago-hf61-gui.log, chikago-hf61-gui-error.log.
Acceptance: каждая карта имеет кнопку «Купить заN$», сверху текущий баланс; cash20$,energy/time40$,rarity80$,price100$. Domain TryPurchase проверяет pending/наличие в предложении/баланс до изменения уровней и возвращает баланс после оплаты; controller присваивает его и применяет эффект синхронно с сохранением. Платный cash даёт50$ после оплаты20$. Disabled-кнопки/hotkey/direct requests не обходят недостаток денег. «Пропустить»/Esc бесплатно завершает предложение без эффекта; pending=false сохраняется существующей схемой, reload не открывает shop повторно. Ни повторный клик, ни reload не списывают/начисляют бонус дважды. В тестах все7 покупок с точной суммой, reload до/после выбора, zero-balance/skip мышью/Esc, RU/EN и960×540/1280×720. Energy-card увеличивает фактический лимит3/6 до4/7 черезdomain и сохраняется; тексты energy/time больше не обещают устаревшие базовые значения.
Read-set exception: диагностика показала обрезание energy-бонуса в новом пользовательском EnergyCapacity; координатор расширил CP до forwarding-правки. source/ не читался.
Risks/blocker: общий runner по-прежнему останавливается на14 старых тестах таймера; целевые новые проверки пройдены. Godot сообщает прежние предупреждения пользовательских ассетов о дублирующихся UID и несовпадении регистра EmbroideryHoop; ассеты не менялись. Цены демонстрационные; commit/push не выполнялись.
Next: завершить день, купить карту или нажать «Пропустить».
```

Координатор подтвердил HF-61 2026-11-07 по targeted domain/GPU checks, screenshot и diff-check: REVIEW → DONE. Известный FAIL общего runner HF-60 сохранён отдельно.
### HF-60 — публикация пользовательского обновления арта и UI

Статус: `DONE`; координатор/исполнитель `/root`; HF-59 DONE; READY 2026-11-07. Пользователь явно запросил push текущих изменений, исправления поведения не поручал.
CP-HF-60: read-set — текущий фронт, tools/verify.ps1, git status/diff/log/branches/remotes, .vscode/settings.json, OS TEMP/chikago-hf60-verify.log. Write-set — ORCHESTRATION_PLAN.md, Git index/commit, main и origin/main; публикация точного набора файлов ниже без редактирования их содержимого. Единственный владелец плана/index `/root`; пересечений нет.
Out of scope: source/, ignored/generated files, исправление пользовательского кода или тестов, force push, удаление веток.
Приёмка: результат tools/verify.ps1 честно зафиксирован (не является требованием исправить тесты в задаче публикации), staged diff-check PASS; все текущие неигнорируемые изменения закоммичены и отправлены в origin/main без force; чистое дерево и main...origin/main=0/0. Известно до публикации: build PASS; dotnet test FAIL (14 failed,56 passed), runner останавливается до Godot integration. Пользователь уведомлён.
Файлы для публикации:
- art/minigames/EmbroideryHoop.PNG
- art/minigames/EmbroideryHoop.PNG.import
- art/minigames/PolishingCloth.PNG
- art/minigames/PolishingCloth.PNG.import
- art/minigames/RankEmpty.png
- art/minigames/RankEmpty.png.import
- art/minigames/RankFull.png
- art/minigames/RankFull.png.import
- art/minigames/Violin.PNG
- art/minigames/Violin.PNG.import
- art/rooms/DayTime.png
- art/rooms/DayTime.png.import
- art/rooms/music_room.jpg
- art/rooms/music_room.jpg.import
- art/rooms/NightTime.png
- art/rooms/NightTime.png.import
- art/ui/en/Back.png
- art/ui/en/Back.png.import
- art/ui/en/Continue.png
- art/ui/en/Continue.png.import
- art/ui/en/Exit.png
- art/ui/en/Exit.png.import
- art/ui/en/Fullscreen.png
- art/ui/en/Fullscreen.png.import
- art/ui/en/Fullscreen2.png
- art/ui/en/Fullscreen2.png.import
- art/ui/en/NewGame.png
- art/ui/en/NewGame.png.import
- art/ui/en/Play.png
- art/ui/en/Play.png.import
- art/ui/en/Settings.png
- art/ui/en/Settings.png.import
- art/ui/en/Skip.png
- art/ui/en/Skip.png.import
- art/ui/ru/button_back.png
- art/ui/ru/button_back.png.import
- art/ui/ru/button_play.png
- art/ui/ru/button_play.png.import
- art/ui/ru/button_quit.png
- art/ui/ru/button_quit.png.import
- art/ui/ru/button_settings.png
- art/ui/ru/button_settings.png.import
- art/ui/ru/ContinueRu.png
- art/ui/ru/ContinueRu.png.import
- art/ui/ru/FullScreenRu.png
- art/ui/ru/FullScreenRu.png.import
- art/ui/ru/FullScreenRu2.png
- art/ui/ru/FullScreenRu2.png.import
- art/ui/ru/NewGameRu.png
- art/ui/ru/NewGameRu.png.import
- art/ui/ru/SkipRu.png
- art/ui/ru/SkipRu.png.import
- art/ui/Slots3.png
- art/ui/Slots3.png.import
- art/ui/Slots6.png
- art/ui/Slots6.png.import
- scenes/main_menu.tscn
- scenes/trade_scene.tscn
- scripts/Days/DayCycleController.cs
- scripts/Days/DayNightBackground.cs
- scripts/Days/DayNightBackground.cs.uid
- scripts/MainMenu.cs
- scripts/TradeScene.cs
- scripts/UI/EmbroideryGame.cs
- scripts/UI/InventoryUI.cs
- scripts/UI/PolishingSurface.cs
- src/ChikagoShop.Domain/Days/DaySession.cs

```text
Task: HF-60
Recommended status: REVIEW
Changed: commit74e59f7,68файлов: пользовательские арт/UI/мини-игры/фон дня-ночи/длительность дня и запись плана; содержимое продуктовых файлов при публикации не редактировалось.
Checks: tools/verify.ps1 -> FAIL на dotnet test (14 failed,56 passed; build PASS; Godot import/integration runner не запускал); staged diff-check -> PASS; git push origin main -> PASS (eac88f0..74e59f7, без force); main...origin/main=0/0; рабочее дерево чистое. Evidence: OS TEMP/chikago-hf60-verify.log.
Acceptance: текущие пользовательские изменения опубликованы по запросу; результат падающей проверки явно сообщён пользователю. DONE означает только завершённую публикацию, не исправление или подтверждение игрового поведения.
Read-set exception: нет; source/ не читался. .vscode/settings.json отображался modified до add, фактического содержательного diff не было и в commit он не вошёл.
Risks/blocker:14 доменных тестов падают, включая прежние ожидания120/150сек при изменённом таймере; полная проверка проекта не пройдена. Исправление не входило в запрос push.
Next: отдельной задачей разобрать14 упавших тестов.
```

Координатор подтвердил HF-60 2026-11-07 как завершённую публикацию по фактическому push и чистому дереву; FAIL проверки сохранён без изменения статусов продуктовых задач. Эта запись публикуется отдельным документационным commit.
### HF-59 — публикация карточек улучшений и исправлений диалога

Статус: `DONE`; координатор/исполнитель `/root`; HF-54…HF-58 DONE; READY 2026-11-07. Пользователь явно поручил push на GitHub.
CP-HF-59: read-set — текущий фронт, git status/diff/log/branches/remotes, tools/verify.ps1, OS TEMP/chikago-hf58-verify.log и chikago-hf58-preview.log. Write-set — ORCHESTRATION_PLAN.md, Git index/commit, main и origin/main; публикация всех текущих16 файлов: ORCHESTRATION_PLAN.md, README.md, localization/en.tres, scripts/Crafting/WorkshopItemCatalog.cs, scripts/Days/DayCycleController.cs, scripts/Dialogue/DialoguePlayer.cs, scripts/TradeScene.cs, scripts/UI/InventoryUI.cs, scripts/UI/PauseMenu.cs, scripts/UI/RarityPanel.cs, src/ChikagoShop.Domain/Days/DaySession.cs, tests/ProjectTest.cs, scripts/UI/DayTransitionCard.cs[.uid], src/ChikagoShop.Domain/Days/DayUpgradeSession.cs, tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs. Единственный владелец index/плана `/root`; пересечений нет.
Out of scope: source/, ignored/generated files, новая продуктовая логика, force push, удаление веток.
Приёмка: полный verify и GPU evidence HF-58 PASS, staged diff-check PASS, commit включает все изменения HF-54…58; push origin main без force; чистое дерево и main...origin/main=0/0.
```text
Task: HF-59
Recommended status: REVIEW
Changed: все16 файлов HF-54…HF-58 опубликованы commit c1c9e9e в origin/main.
Checks: финальный tools/verify.ps1 HF-58 -> PASS (exit 0, build, 70 domain tests, Godot import/integration); GPU ProjectTest -> PASS; staged diff-check -> PASS; git fetch origin -> PASS; git push origin main -> PASS (7ae3f04..c1c9e9e, без force); main...origin/main=0/0; рабочее дерево чистое.
Acceptance: исправления диалоговой прокрутки, выбор улучшений, постоянные редкие шансы и цены категорий, UI/локализация/сохранения/тесты отправлены на GitHub. Продуктовых изменений во время публикации не было.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет.
Next: продолжить по запросу пользователя.
```

Координатор подтвердил HF-59 2026-11-07 по verify evidence, фактическому push и чистому дереву: REVIEW → DONE. Эта запись публикуется отдельным документационным commit.
### HF-58 — карты стоимости одежды и инструментов

Статус: `DONE`; координатор/исполнитель `/root`; HF-57 DONE; READY 2026-11-07.
CP-HF-58: read-set — текущий фронт, src/ChikagoShop.Domain/Days/DayUpgradeSession.cs, scripts/UI/DayTransitionCard.cs, scripts/Days/DayCycleController.cs, scripts/Crafting/WorkshopItemCatalog.cs, scripts/UI/RarityPanel.cs, scripts/UI/InventoryUI.cs, scripts/UI/SlotUI.cs, scripts/CounterWorld.cs (поиск использования стоимости), scripts/TradeScene.cs (цены/торговля/forwarding/save/checkpoint), tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs, tests/ProjectTest.cs, localization/en.tres, README.md, tools/verify.ps1, git status/diff. Write-set — ORCHESTRATION_PLAN.md, src/ChikagoShop.Domain/Days/DayUpgradeSession.cs, scripts/UI/DayTransitionCard.cs, scripts/Days/DayCycleController.cs, scripts/UI/RarityPanel.cs, scripts/UI/InventoryUI.cs, scripts/TradeScene.cs (готовый domain price/forwarding/refresh/save fields), tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs, tests/ProjectTest.cs, localization/en.tres, README.md. Единственный владелец save schema/плана `/root`; пересечений нет. Предыдущие локальные изменения сохраняются.
Out of scope: source/, сцены/ассеты, стоимость крафта/открытия комнаты, новый баланс вероятностей, новая доменная логика TradeScene, commit/push.
Приёмка: две новые карты +20% базовой стоимости atelier/music за выбор, складываются до конца игры; три предложения из пула7 на каждом переходе. Эффект действует на старые/новые вещи категории, инвентарные подсказки/таблицы/офферы/продажа/reload согласованы. Повышенные цены зелёные, ниже baseline красные, неизменные нейтральные. Выбор/уровни/checkpoint/newgame/старые saves совместимы. Domain rounding/stack/isolation и GUI выбор/reload/торговля/все редкости; tools/verify.ps1 и git diff --check. Evidence: OS TEMP/chikago-hf58-verify.log и GPU preview logs/screenshot.
```text
Task: HF-58
Recommended status: REVIEW
Changed: src/ChikagoShop.Domain/Days/DayUpgradeSession.cs; scripts/UI/DayTransitionCard.cs; scripts/Days/DayCycleController.cs; scripts/UI/RarityPanel.cs; scripts/UI/InventoryUI.cs; scripts/TradeScene.cs (вызовы готовой цены/refresh/save/checkpoint); tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs; tests/ProjectTest.cs; localization/en.tres; README.md; ORCHESTRATION_PLAN.md. Предыдущие локальные изменения сохранены.
Checks: tools/verify.ps1 -> PASS (exit 0, build, 70 domain tests, Godot import/integration); GPU ProjectTest gl_compatibility/--day-card-preview -> PASS (exit 0, stderr пуст); screenshots OS TEMP/chikago-price-upgrade-preview.png и chikago-price-table-preview.png осмотрены; git diff --check -> PASS. Evidence: OS TEMP/chikago-hf58-verify.log, chikago-hf58-preview.log.
Acceptance: карты «Имя на этикетке» (одежда) и «Коллекционный звук» (инструменты) добавляют20п.п. базовой стоимости категории за выбор, действуют до конца игры и складываются аддитивно. Пул7, одновременно3 предложения; atelier price появляется4→5, music price5→6. Domain рассчитывает цены с decimal/half-up и ограничением уровня6; статический базовый каталог не меняется. Текущая цена применяется к обеим точкам расчёта оффера, старым/новым товарам и tooltip; таблица показывает все5 повышенных цен зелёным, другую категорию нейтральной. Save v10 и checkpoint сохраняют оба уровня; v1…9 читаются с нулевыми price-level по умолчанию. Domain тестирует stack/isolation/однократность/rounding/overflow/restore; GUI обе карты кнопкой, все10 товаров/5 редкостей, реальные таблицы/цвета, активную сделку и reload, продажу ровноодинраз, создание новых вещей, повторную карту40%, перенос на новый день, сохранение/rollback checkpoint/newgame. Крафт90$/140$, вероятности и предыдущие улучшения сохранены. GPU screenshots подтверждают карту +20% и таблицу36/60/96/144/240$ зелёным для одежды.
Read-set exception: нет; source/ не читался.
Risks/blocker:20% — демонстрационный баланс, стоимость крафта не меняется; commit/push не выполнялись.
Next: при переходе4→5 выбрать «Имя на этикетке» и открыть таблицу одежды.
```

Координатор подтвердил HF-58 2026-11-07 по verify, GPU regression и screenshots: REVIEW → DONE.
### HF-57 — выбор одного из трёх улучшений между днями

Статус: `DONE`; координатор/исполнитель `/root`; HF-56 DONE; READY 2026-11-07. Пользователь уточнил: карточки должны предлагать три улучшения, сюжетные заставки HF-56 заменяются.
CP-HF-57: read-set — текущий фронт, scripts/UI/DayTransitionCard.cs, scripts/Days/DayCycleController.cs, scripts/TradeScene.cs (подключение, энергия/HUD, переходы, save/load), scripts/UI/PauseMenu.cs, src/ChikagoShop.Domain/Days/DaySession.cs, tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, tests/ProjectTest.cs (дни/карточки/save), localization/en.tres, README.md, tools/verify.ps1, .vscode/settings.json, git status/diff; write-set — ORCHESTRATION_PLAN.md, scripts/UI/DayTransitionCard.cs, scripts/Days/DayCycleController.cs, scripts/TradeScene.cs (только forwarding, save/load полей и HUD), src/ChikagoShop.Domain/Days/DayUpgradeSession.cs, src/ChikagoShop.Domain/Days/DaySession.cs, tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs, tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, tests/ProjectTest.cs, localization/en.tres, README.md. OS TEMP/chikago-hf57-verify.log и preview logs/screenshot. Единственный владелец схемы/плана `/root`; пересечений нет. Локальные HF-54…56 сохраняются.
Уточнение пользователя: категорийные улучшения повышают редкие шансы одежды/инструментов до конца игры, выше baseline зелёным, ниже красным. Read/write-set дополнен: scripts/Crafting/WorkshopItemCatalog.cs, scripts/UI/RarityPanel.cs; read-set: config/economy.v1.json, src/ChikagoShop.Domain/Configuration/EconomyConfig.cs, src/ChikagoShop.Domain/Crafting/WeightedItemTable.cs, scenes/trade_scene.tscn (только пути панелей). Диагностика: точечный rg --files scripts для поиска RarityPanel.cs. Save schema и checkpoint fields принадлежат только `/root`.
Out of scope: source/, сцены/ассеты, финальный баланс, новая доменная логика в TradeScene, commit/push.
Приёмка: на переходах1→2…6→7 одновременно три карточки, выбрать ровно одну; пул демо:50$ сразу /1 энергия на следующий день /30сек на следующий день /постоянные редкие шансы atelier /постоянные редкие шансы music. Эффект однократный, UI показывает его срок/величину; pending/выбор/эффект переживают reload, нет обхода через перезапуск; квоты/final/failed/старые saves совместимы. Проверки: domain одноразовости/invalid/restore/time cap; integration три эффекта/reload/все6 переходов/RU/EN/mouse/keyboard/layout и полный verify/diff-check; GPU screenshot.
```text
Task: HF-57
Recommended status: REVIEW
Changed: scripts/UI/DayTransitionCard.cs; scripts/Days/DayCycleController.cs; src/ChikagoShop.Domain/Days/DayUpgradeSession.cs; src/ChikagoShop.Domain/Days/DaySession.cs; scripts/Crafting/WorkshopItemCatalog.cs; scripts/UI/RarityPanel.cs; scripts/TradeScene.cs (forwarding, применение готового roll, HUD, save/checkpoint fields); tests/ChikagoShop.Domain.Tests/DayUpgradeSessionTests.cs; tests/ProjectTest.cs; localization/en.tres; README.md; ORCHESTRATION_PLAN.md. Локальные предыдущие изменения сохранены.
Checks: tools/verify.ps1 -> PASS (exit 0, build, 67 domain tests, Godot import/integration); GPU ProjectTest с gl_compatibility/--day-card-preview -> PASS (exit 0, stderr пуст), screenshot OS TEMP/chikago-day-card-preview.png осмотрен; git diff --check -> PASS. Evidence: OS TEMP/chikago-hf57-verify.log, chikago-hf57-preview.log.
Acceptance: повествовательные заставки заменены на три одновременно видимых карточки, выбрать ровно одну кнопкой/1–3; набор детерминированно меняется по дням из пяти вариантов (50$, энергия, время, одежда, инструменты). Базовые деньги/энергия/время подтверждены пользователем; категорийные бонусы действуют до конца игры и складываются. Первый уровень даёт45/25/18/9/3 вместо60/25/10/4/1; domain переносит до15п.п. из common к трём редким классам, сохраняет сумму100 и положительность. Общий источник весов используется roll и таблицей: выше baseline зелёный, ниже красный, обычные значения чёрные. Save v9 хранит pending/выбор/уровни/снимок checkpoint; старые saves загружаются без бонусов. Перезапуск до выбора возвращает те же три карточки; после выбора не повторяет награду, сохраняет3 энергии или150сек; новые дни сбрасывают временные эффекты, сохраняют категории. Checkpoint восстанавливает снимок категорий, новая игра сбрасывает. Domain тестирует одноразовость/непредлагаемый выбор/stack/restore/time bounds; integration все6 переходов, RU/EN, 960×540/1280×720, кнопки/клавиши/echo/Escape, failed/final, reload каждого из5 бонусов, checkpoint/newgame и все100 tickets обеих категорий против отображённых весов/цветов.
Read-set exception: точечно найдены/прочитаны RarityPanel.cs, config и catalog для уточнения категорийного улучшения; read/write-set расширен координатором в CP до правок; source/ не читался.
Risks/blocker: баланс демонстрационный; набор из5 вариантов ротируется по дням, без RNG. Обновлённые механики доменные, TradeScene не получает новых правил. Commit/push не выполнялись.
Next: завершить день и выбрать улучшение; категорийные карточки появляются с перехода2→3.
```

Координатор подтвердил HF-57 2026-11-07 по verify, GPU regression и screenshot: REVIEW → DONE. HF-56 заменён уточнённым требованием пользователя.
### HF-56 — демо-карточки между днями 1–7

Статус: `DONE`; координатор/исполнитель `/root`; HF-55 DONE; READY 2026-11-06.
CP-HF-56: read-set — текущий фронт, scripts/Days/DayCycleController.cs, scripts/TradeScene.cs (подключение/EndDay/AdvanceToNextDay), scripts/UI/PauseMenu.cs, scripts/UI/EmbroideryGame.cs (стиль/модальность), scripts/Localization/GameLocalization.cs, src/ChikagoShop.Domain/Days/DaySession.cs, tests/ProjectTest.cs, localization/en.tres, README.md, tools/verify.ps1, git status/diff; render logs и screenshot OS TEMP/chikago-day-card-preview.png; write-set — ORCHESTRATION_PLAN.md, scripts/UI/DayTransitionCard.cs[.uid], scripts/Days/DayCycleController.cs, scripts/TradeScene.cs (только forwarding свойства модальности), scripts/UI/PauseMenu.cs (блокировка поверх карточки), tests/ProjectTest.cs, localization/en.tres, README.md. Владелец `/root`, пересечений нет; локальные HF-54/HF-55 сохраняются.
Out of scope: source/, сцены/ассеты, новый баланс/сюжетные последствия, save schema, новая доменная логика в TradeScene, commit/push.
Приёмка: шесть отдельных демо-карточек1→2…6→7, заголовок/атмосферный текст/кнопка продолжения RU/EN, viewport960×540/1280×720. Успешный переход фиксируется существующим кодом, до продолжения часы/покупатели/камера/пауза блокированы; повторное нажатие не перескакивает день. Нет карточки после7/при поражении; reload/checkpoint/new game/освобождение не оставляют pause. Проверки: integration всех переходов/квоты/failed/final/reload/mouse/keyboard/layout, полный tools/verify.ps1 и git diff --check. Evidence: OS TEMP/chikago-hf56-verify.log.
```text
Task: HF-56
Recommended status: REVIEW
Changed: scripts/UI/DayTransitionCard.cs[.uid]; scripts/Days/DayCycleController.cs; scripts/TradeScene.cs (forwarding свойства DayTransitionOpen); scripts/UI/PauseMenu.cs; tests/ProjectTest.cs; localization/en.tres; README.md; ORCHESTRATION_PLAN.md. Локальные HF-54/HF-55 сохранены.
Checks: tools/verify.ps1 -> PASS (exit 0, build, 59 domain tests, Godot import/integration); дополнительный build и GPU ProjectTest с gl_compatibility/--day-card-preview -> PASS (exit 0, stderr пуст); screenshot OS TEMP/chikago-day-card-preview.png осмотрен; git diff --check -> PASS. Evidence: OS TEMP/chikago-hf56-verify.log, chikago-hf56-preview.log.
Acceptance: шесть отдельных карточек со своим атмосферным текстом: Первый след, Город присматривается, Цена обещания, Долг закрыт, После полуночи, Последний рассвет. Тёмный фон, золотая рамка/номера дней, общий шрифт, без scroll. После успешного перехода существующая логика сохраняет новый день; CanvasLayer120 замораживает мир до кнопки/Space/Enter. Повторный EndDay/Close не пропускает день; pause/craft поверх карточки не открываются. Квота после4, проигрыш и победа после7 сохранены. Integration всех6 переходов при960×540/1280×720 наRU/EN проверяет разные заголовки, route/кнопку, bounds, блокировку повторов/часов/действий, mouse/keyboard, квоту, final/failed, checkpoint/newgame, free/reload без оставшегося pause и повторной оплаты. GPU screenshot6→7 подтверждает внешний вид и читаемость.
Read-set exception: точечно прочитан .vscode/settings.json для запуска GPU-проверки настроенным Godot; source/ не читался.
Risks/blocker: тексты демо, без сюжетных последствий и новых иллюстраций; при reload уже сохранённого перехода карточка не повторяется. Commit/push не выполнялись.
Next: завершить первый день и нажать «Начать следующий день» для просмотра первой карточки.
```

Координатор подтвердил HF-56 2026-11-06 по verify, GPU regression и screenshot: REVIEW → DONE.
### HF-55 — устойчивое место под три строки диалога

Статус: `DONE`; координатор/исполнитель `/root`; HF-54 DONE; READY 2026-11-06.
CP-HF-55: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), tools/verify.ps1, git status/diff; write-set — ORCHESTRATION_PLAN.md, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs. Владелец `/root`, активных пересечений нет; локальные HF-54 сохраняются.
Out of scope: source/, сюжет/домен, ассеты, остальные экраны, commit/push.
Приёмка: текстовое поле гарантированно вмещает три строки фактического шрифта с межстрочными интервалами; три примера пользователя проверены при переходах и resize, scrollbar только при реальном переполнении. Проверки: GUI integration последовательности при960×540,1096×616,1280×720,1600×900; прежний long overflow; tools/verify.ps1; git diff --check. Evidence: OS TEMP/chikago-hf55-verify.log.
```text
Task: HF-55
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md. Локальные HF-54 сохранены.
Checks: tools/verify.ps1 -> PASS (exit 0, build, 59 domain tests, Godot import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf55-verify.log.
Acceptance: RichTextLabel получает минимальную высоту по фактическим метрикам трёх строк шрифта20, межстрочным интервалам и theme padding; VBox/Panel не сжимают поле ниже неё. Три реплики из screenshots проходят последовательно на960×540,1096×616,1280×720,1600×900 и после resize: текст целиком, при числе строк до3 scroll выключен и scrollbar скрыт. Прежние fitting/long-overflow tests и полный prologue проходят; очень длинное предложение доступно через scroll.
Read-set exception: нет; source/ не читался.
Risks/blocker: screenshots запущенного пользователем процесса не воспроизведены как live GUI; проверено headless через настоящий Godot layout. Commit/push не выполнялись.
Next: перезапустить игровую сцену и оценить обновлённый диалог.
```

Координатор подтвердил HF-55 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-54 — прокрутка только при переполнении диалога

Статус: `DONE`; координатор/исполнитель `/root`; HF-53 DONE; READY 2026-11-06.
CP-HF-54: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), tools/verify.ps1, git status/diff; write-set — ORCHESTRATION_PLAN.md, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs. Единственный владелец `/root`, пересечений нет.
Out of scope: source/, сценарий/домен, ассеты, другие экраны, commit/push.
Приёмка: помещающиеся реплики, включая три строки и пример пользователя, без scroll; переполняющее предложение целиком доступно через прежний scrollbar, шрифт20 и управление сохранены. Проверки: integration fitting/overflow на 1280×720 и 960×540, tools/verify.ps1, git diff --check. Evidence: OS TEMP/chikago-hf54-verify.log.
```text
Task: HF-54
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 -> PASS (exit 0, build, 59 domain tests, Godot import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf54-verify.log.
Acceptance: удалено включение scroll по трём строкам; минимальная высота облачка260 (максимум280) вмещает три полные строки с прежним шрифтом20 и внутренними отступами. Integration проверяет точную реплику со screenshot при1280×720, три строки при960×540: весь текст помещается, scroll выключен, scrollbar скрыт. Длинное предложение остаётся целым, scrollbar и доступность конца/защита от случайного клика проверены на обоих размерах. Существующие сценарий/управление/полировка проходят.
Read-set exception: нет; source/ не читался.
Risks/blocker: ручной visual smoke не выполнялся; commit/push не выполнялись.
Next: посмотреть реплику Кастелло в игровом сеансе.
```

Координатор подтвердил HF-54 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-53 — checkpoint и merge feature/dialogue-system

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-06. Пользователь поручил пустой checkpoint перед merge. Checkpoint main: fea23cb.
CP-HF-53: read-set — текущий фронт обеих веток ORCHESTRATION_PLAN.md, AGENTS.md, tools/verify.ps1, git status/diff/log/branches/remotes, OS TEMP/chikago-hf53-verify.log; write-set — ORCHESTRATION_PLAN.md, Git index/commits, main и origin/main; merge только ORCHESTRATION_PLAN.md, README.md, dialogue/intro.ru.json, scenes/trade_scene.tscn, scripts/Dialogue/DialoguePlayer.cs[.uid], scripts/MainMenu.cs, scripts/TradeScene.cs, scripts/UI/ZoomResponsiveUi.cs, src/ChikagoShop.Domain/Dialogue/DialogueSession.cs, src/ChikagoShop.Domain/Dialogue/SentencePages.cs, tests/ChikagoShop.Domain.Tests/DialogueSessionTests.cs, tests/ChikagoShop.Domain.Tests/SentencePagesTests.cs, tests/ProjectTest.cs. Зависимости HF-52 DONE в исходной ветке; активных пересечений нет. Владелец плана/index `/root`.
Out of scope: source/, новая продуктовая логика, force push, удаление веток.
Приёмка: пустой checkpoint предшествует merge, feature/dialogue-system является предком main, tools/verify.ps1 и git diff --check PASS, push origin main без force, чистое дерево и main...origin/main=0/0.

```text
Task: HF-53
Recommended status: REVIEW
Changed: checkpoint fea23cb (empty), merge 95a576a; ORCHESTRATION_PLAN.md и tests/ProjectTest.cs — конфликты разрешены с сохранением обоих наборов записей и тестов.
Checks: tools/verify.ps1 -> PASS (exit 0, build, 59 domain tests, Godot import/integration); staged diff-check -> PASS; push origin main -> PASS; merge-base --is-ancestor origin/feature/dialogue-system main -> PASS; main...origin/main=0/0; дерево чистое. Evidence: OS TEMP/chikago-hf53-verify.log.
Acceptance: пустой checkpoint предшествует merge, новая диалоговая ветка целиком включена в main и опубликована без force; прежняя полировка также проходит regression.
Read-set exception: нет; source/ не читался. Конфликтующие номера входящих HF-37/HF-38 обозначены HF-37-D/HF-38-D с сохранением истории.
Risks/blocker: нет.
Next: продолжить по запросу пользователя.
```

Координатор подтвердил HF-53 2026-11-06 по verify и фактическому push: REVIEW → DONE.
### HF-52 — перетаскивание камеры поверх закрытой комнаты

Статус: `DONE`; координатор/исполнитель `/root`; зависимости отсутствуют; READY 2026-11-06. CP-HF-52: read-set — текущий фронт, scripts/TradeScene.cs (EnsureMusicWorkshopUi), scripts/Camera/WorldCamera.cs, tests/ProjectTest.cs (camera/mouse controls), tools/verify.ps1; write-set — scripts/TradeScene.cs (только MouseFilter overlay), tests/ProjectTest.cs, ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: source/, новая доменная логика/рефакторинг TradeScene, экономика/покупка комнаты, ассеты, посторонние изменения, публикация. Приёмка: ЛКМ-drag с чёрной поверхности закрытой комнаты двигает камеру; overlay блокирует нижележащие кнопки, покупка отдельной кнопкой/GUI/пауза сохранены. Проверки: реальный Root.PushInput drag с overlay, существующие camera/lock/mouse regression, полный verify и diff-check.


```text
Task: HF-52
Recommended status: REVIEW
Changed: scripts/TradeScene.cs (MouseFilter locked overlay), tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 53 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf52-verify.log.
Acceptance: overlay переведён Stop → Pass: пустая закрытая поверхность передаёт необработанный ЛКМ камере, нижележащие GUI-кнопки не становятся кликабельными. Новый тест реальными Root.PushInput проверяет drag на чёрном фоне и заголовке закрытия, остановку после отпускания, отсутствие списания/крафта и рабочую покупку комнаты после drag. Прежние тесты блокировки камеры на GUI/паузе и полной мышиной торговли проходят.
Read-set exception: координатор точечно нашёл путь WorldCamera после диагностики, source/ не читался.
Risks/blocker: нет; пользовательские изменения сохранены, публикация не выполнялась.
Next: проверить перетаскивание закрытой комнаты в игровом сеансе.
```

Координатор подтвердил HF-52 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-51 — компактная табличка сделки в центре комнаты

Статус: `DONE`; координатор/исполнитель `/root`; HF-50 DONE; READY 2026-11-06. CP-HF-51: read-set — текущий фронт, scenes/trade_scene.tscn (TradingRoom/TradePanel styles), scripts/UI/ZoomResponsiveUi.cs, tests/ProjectTest.cs (зум/покупатели), art/rooms/cashier.jpg, tools/verify.ps1; write-set — сцена, ZoomResponsiveUi.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md. Владелец сцены `/root`, пересечений нет. Out of scope: source/, TradeScene/домен, ассеты/положение NPC и предмета, пользовательские изменения, публикация. Приёмка: надпись удалена; уменьшенная панель по центру свободной части комнаты, без пересечения с кассой/товарами/внешностями, ограниченное экранное увеличение при zoom-in. Проверки: геометрия и GUI trade/localization regression, полный verify, diff-check.


```text
Task: HF-51
Recommended status: REVIEW
Changed: scenes/trade_scene.tscn, scripts/UI/ZoomResponsiveUi.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 53 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf51-verify.log.
Acceptance: SalesRoomLabel удалён; табличка 320×200 вместо 420×280, центр по X комнаты, над зоной товара и кассы. Шрифт 22/26 вместо 27/33, кнопки высотой 44 вместо 78, знаки 30 вместо 48. Для TradePanel масштаб ограничен 0.6–1.2 с компенсацией zoom-in: при 1.8 экранный множитель 1.08 вместо 1.8. Регрессия проверяет отсутствие пересечения с зоной кассы (по исходному фону), всеми спрайтами товара и всеми 18 покупателями при zoom 0.25/0.7/1/1.8; компактность, удаление заголовка, GUI-клики и RU/EN проходят.
Read-set exception: координатор прочитал art/rooms/cashier.jpg для определения зоны кассы; source/ не читался.
Risks/blocker: ручной visual smoke не выполнялся; пользовательские изменения сохранены, публикация не выполнялась.
Next: оценить компактную табличку над кассой при приближении.
```

Координатор подтвердил HF-51 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-50 — окно сделки не перекрывает покупателя

Статус: `DONE`; координатор/исполнитель `/root`; зависимости отсутствуют; READY 2026-11-06. CP-HF-50: read-set — текущий фронт, scenes/trade_scene.tscn (TradingRoom), scenes/customer.tscn, scripts/UI/ZoomResponsiveUi.cs, tests/ProjectTest.cs (покупатель/зум), tools/verify.ps1, git status/diff; write-set — scenes/trade_scene.tscn (только offsets TradePanel), tests/ProjectTest.cs, ORCHESTRATION_PLAN.md. Владелец сцены `/root`, пересечений нет. Out of scope: source/, TradeScene.cs/домен/экономика, ассеты и положение NPC, другие пользовательские изменения, публикация. Приёмка: панель слева от покупателя без пересечения со всеми 18 внешностями при диапазоне компенсации зума; прежние GUI-продажа/реролл проходят. Проверки: геометрическая regression, полный verify, diff-check.


```text
Task: HF-50
Recommended status: REVIEW
Changed: scenes/trade_scene.tscn (TradePanel offsets), tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 53 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf50-verify.log.
Acceptance: панель сделки сдвинута влево на 597 world units без изменения размера/высоты и положения NPC. Геометрический тест проверяет отсутствие пересечения с bounds тела/одежды/волос всех 18 внешностей при zoom 0.25/0.7/1/1.8 (включая максимум UI-компенсации 1.65). Прежние реальные GUI-клики продажи/реролла, торговля и полная регрессия проходят.
Read-set exception: координатор точечно прочитал сцену покупателя и zoom UI перед CP для диагностики геометрии. source/ не читался.
Risks/blocker: ручной visual smoke не выполнялся; пользовательские изменения сохранены, публикация не выполнялась.
Next: посмотреть покупателя рядом с окном сделки в игровом окне.
```

Координатор подтвердил HF-50 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-49 — стилизованная прокрутка длинных предложений

Статус: `DONE`; координатор/исполнитель `/root`; HF-48 DONE; READY 2026-11-06. CP-HF-49: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), README.md, tools/verify.ps1; write-set — эти три файла и ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: ассеты/source/, сценарий/домен, остальные изменения, публикация. Приёмка: шрифт всегда 20, предложения от трёх строк прокручиваются в пределах облачка, тонкий золотистый scrollbar; короткие страницы без scroll, границы предложений/отступы сохранены; drag scrollbar не переключает окно. Проверки: integration шрифта/вместимости/scrollbar/управления двух размеров, verify, diff-check.


```text
Task: HF-49
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, README.md, ORCHESTRATION_PLAN.md.
Checks: финальный tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 53 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf49-verify.log.
Acceptance: уменьшение шрифта удалено, каждая страница использует 20. При трёх строках или overflow включается ScrollActive; текст принимает колесо, VScrollBar имеет тёмный желобок и золотистый ползунок с hover/pressed состояниями. Каждый новый экран сбрасывает scroll наверх. ЛКМ по scrollbar не переключает диалог; прочее управление сохранено. Отдельный integration с длинным предложением из временного JSON подтверждает весь текст на одной странице, стандартный шрифт, видимый диапазон прокрутки и доступность конца; полные прогоны исходного сценария двух размеров сохраняют границы/содержимое. DialoguePath позволяет загружать диалог из заданного ресурса, по умолчанию прежний intro.
Read-set exception: нет. Временный тестовый user:// JSON удаляется в finally.
Risks/blocker: ручной visual smoke не выполнялся; если весь текст уже помещается, движок не показывает ненужный scrollbar.
Next: оценить прокрутку длинного предложения в игровом окне.
```

Координатор подтвердил HF-49 2026-11-06 по финальному verify и diff-check: REVIEW → DONE.
### HF-48 — целые предложения и действия без заголовка

Статус: `DONE`; координатор/исполнитель `/root`; HF-47 DONE; READY 2026-11-06. CP-HF-48: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, src/ChikagoShop.Domain/Dialogue/DialogueSession.cs, tests/ChikagoShop.Domain.Tests/DialogueSessionTests.cs, tests/ProjectTest.cs (диалог), dialogue/intro.ru.json, README.md, tools/verify.ps1; write-set — перечисленные файлы, новые src/ChikagoShop.Domain/Dialogue/SentencePages.cs и tests/ChikagoShop.Domain.Tests/SentencePagesTests.cs, ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: source/, ассеты, save/экономика, остальные сюжетные реплики, публикация. Приёмка: удалён начальный двухабзацный кадр, действия без имени; страницы заканчиваются на границе предложения, слишком длинное одиночное предложение помещается уменьшением шрифта целиком; текст не теряется. Проверки: unit границ/слишком длинного предложения/ремарок и integration всего текста/геометрии/запуска двух размеров, verify, diff-check.


```text
Task: HF-48
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, src/ChikagoShop.Domain/Dialogue/DialogueSession.cs, SentencePages.cs в той же папке, tests/ChikagoShop.Domain.Tests/DialogueSessionTests.cs, SentencePagesTests.cs в той же папке, tests/ProjectTest.cs, dialogue/intro.ru.json, README.md, ORCHESTRATION_PLAN.md.
Checks: финальный tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 53 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf48-verify.log.
Acceptance: удалены оба абзаца начальной постановки, первый экран — покупательница; осталось 24 записи, включая 22 неизменённые реплики. Ремарки имеют пустое имя, Label скрывается. SentencePages выбирает последние целые предложения, помещающиеся на странице, с закрывающими кавычками/маркерами/пробелами. Одиночное длинное предложение остаётся целым, при необходимости уменьшается шрифт страницы. Unit-тесты покрывают границы, многоточие, кавычки, действия и пустое имя; полный integration проверяет конец каждого продолжающегося окна, отсутствие потерь текста/scroll/обрезки, старт и весь сценарий при двух размерах.
Read-set exception: повторно прочитан пользовательский сценарий.doc для восстановления JSON после ошибки PowerShell-конвертации массива; восстановлены исходные 22 реплики и сохранённые ремарки, удалены только два согласованных начальных абзаца. source/ не читался.
Risks/blocker: ручной visual smoke не выполнялся; очень длинное предложение имеет меньший шрифт, чтобы не прерывать его и сохранить компактное облачко.
Next: оценить переходы между целыми предложениями в игровом окне.
```

Координатор подтвердил HF-48 2026-11-06 по финальному verify и diff-check: REVIEW → DONE.
### HF-47 — компактное облачко для коротких реплик

Статус: `DONE`; координатор/исполнитель `/root`; HF-46 DONE; READY 2026-11-06. CP-HF-47: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (геометрия), tools/verify.ps1; write-set — эти два C# файла и ORCHESTRATION_PLAN.md. Единственный владелец `/root`. Out of scope: source/, ассеты/шрифт/отступы, сценарий/домен, посторонние изменения, публикация. Приёмка: ниже само облачко, прежние отступы и полный текст страницами; проверки: полный verify двух размеров и diff-check.


```text
Task: HF-47
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf47-verify.log.
Acceptance: высота 220–250 вместо 280–310, отступы 112/64 сохранены. Полные прогоны сценария при двух размерах подтверждают читаемость страниц без обрезки/scroll/потери текста, геометрия компактной панели проверена.
Read-set exception: нет.
Risks/blocker: ручной visual smoke не выполнялся.
Next: оценить компактное облачко в игровом окне.
```

Координатор подтвердил HF-47 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-46 — больше отступов и ниже облачко

Статус: `DONE`; координатор/исполнитель `/root`; HF-45 DONE; READY 2026-11-06. CP-HF-46: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), tools/verify.ps1; write-set — эти два C# файла и ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: шрифт/ассеты/source/, сценарий/домен, пользовательские изменения, публикация. Приёмка: высота уменьшена, равные боковые и вертикальные отступы увеличены, весь текст доступен страницами без scroll. Проверки: геометрия/полный текст двух размеров, verify, diff-check.


```text
Task: HF-46
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf46-verify.log.
Acceptance: высота облачка 280–310 вместо 320–370, боковые отступы 112 вместо 80, сверху/снизу 64 вместо 56. Общий шрифт/размер сохранён. Проверены новые отступы и полный проход всех страниц без обрезки, scroll или потери текста при двух разрешениях.
Read-set exception: нет.
Risks/blocker: ручной visual smoke не выполнялся.
Next: оценить новые пропорции в игровом окне.
```

Координатор подтвердил HF-46 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-45 — более мелкая и ровная типографика диалога

Статус: `DONE`; координатор/исполнитель `/root`; HF-44 DONE; READY 2026-11-06. CP-HF-45: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), tools/verify.ps1; write-set — scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (уточнение прежней проверки пагинации), ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: ассеты/source/, сценарий, новые элементы UI, домен, пользовательские изменения, публикация. Приёмка: немного меньший общий текст, симметричные отступы сверху/снизу, одинаковое положение начала коротких и длинных реплик; сохранены пагинация и читаемость. Проверки: существующий полный прогон всех реплик/страниц двух размеров, verify, diff-check.


```text
Task: HF-45
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: финальный tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf45-verify.log.
Acceptance: общий проектный шрифт сохранён; имя/реплики 20 вместо 23, титр 18 вместо 20, межстрочное расстояние 5 вместо 6. Отступы сверху/снизу уравнены до 56, боковые остались 80. Короткие и длинные реплики начинаются на одних направляющих, без смены масштаба/выравнивания при печати. Полный прогон текста подтверждает вместимость и сохранность при двух разрешениях. Удалено устаревшее требование теста обязательно видеть несколько страниц: при новом шрифте исходный сценарий помещается целыми репликами; постраничный алгоритм и проверки высоты/полноты сохранены.
Read-set exception: нет; координатор расширил write-set на уточнение теста по результатам verify.
Risks/blocker: ручной visual smoke не выполнялся; пользовательские изменения сохранены.
Next: оценить уменьшенный текст в игровом окне.
```

Координатор подтвердил HF-45 2026-11-06 по финальному verify и diff-check: REVIEW → DONE.
### HF-44 — диалог без служебных элементов и прокрутки

Статус: `DONE`; координатор/исполнитель `/root`; HF-43 DONE; READY 2026-11-06. CP-HF-44: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), README.md (управление), tools/verify.ps1; write-set — эти три файла и ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: исходные ассеты/source/, сюжетные тексты, save/домен, пользовательские изменения, публикация. Приёмка: только имя/реплика, без счётчика/кнопки/подсказок/scroll; Space/ЛКМ раскрывают текст и переключают страницы; длинные реплики делятся по вместимости с сохранением всего текста, resize пересчитывает текущую страницу; отступы сохранены. Проверки: integration пагинации/управления/геометрии обоих размеров, полный verify, diff-check.


```text
Task: HF-44
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, README.md, ORCHESTRATION_PLAN.md.
Checks: финальный tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf44-verify.log.
Acceptance: убраны Actions/кнопка/счётчик/Hint; в Content только имя и реплика. ScrollActive=false, текст занимает всю оставшуюся высоту. Space и ЛКМ раскрывают текущую страницу/переключают следующую, echo игнорируется; Esc по-прежнему возвращает в меню. Пагинация измеряет реальную высоту RichTextLabel с общим шрифтом, переносами и межстрочным расстоянием, разделяет по пробелам и пересчитывает страницу при resize. Полные прогоны диалога при 960×540 и исходном 1280×720 подтверждают вместимость каждой страницы, конечность переходов и точное восстановление всех исходных реплик конкатенацией страниц. Реальный GUI-клик по тексту, Space, отмена/повтор и переход в gameplay проходят. Горизонтальные отступы 80, верхний 64 и нижний 48 сохранены; доступная высота текста >150.
Read-set exception: нет.
Risks/blocker: ручной visual smoke не выполнялся; пользовательские изменения сохранены, commit/push не выполнялись.
Next: оценить чтение диалога по страницам в игровом окне.
```

Координатор подтвердил HF-44 2026-11-06 по финальному verify и diff-check: REVIEW → DONE.
### HF-43 — немного уменьшить окно диалога

Статус: `DONE`; координатор/исполнитель `/root`; HF-42 DONE; READY 2026-11-06. CP-HF-43: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), tools/verify.ps1; write-set — эти два C# файла и ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: ассеты/source/, шрифт/тексты/отступы, доменная логика, пользовательские изменения, публикация. Приёмка: панель немного уже и ниже, весь контент внутри, читаемость сохранена при 1280×720/960×540. Проверки: layout/integration, verify, diff-check.


```text
Task: HF-43
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf43-verify.log.
Acceptance: ширина уменьшена с 84% до 78% viewport, высота с 54% (340–420) до 47% (320–370). Шрифт/отступы сохранены, подсказки внутри; layout двух разрешений и полная регрессия проходят.
Read-set exception: нет.
Risks/blocker: ручной visual smoke не выполнялся.
Next: оценить размер окна в игре.
```

Координатор подтвердил HF-43 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-42 — высота облачка под весь текст диалога

Статус: `DONE`; координатор/исполнитель `/root`; HF-41 DONE; READY 2026-11-06. CP-HF-42: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), tools/verify.ps1; write-set — эти два C# файла и ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: исходные ассеты/source/, сценарий/домен, прочие пользовательские изменения, публикация. Приёмка: облачко выше, имя/реплики/счётчик/кнопка/подсказки внутри него с отступами, без выхода за viewport при 1280×720/960×540. Проверки: layout/integration и полный verify, diff-check.


```text
Task: HF-42
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf42-verify.log.
Acceptance: высота облачка 340–420 вместо 280–340, нижняя граница в 16 px от viewport. Подсказки перемещены внутрь общего фона; отступы сверху/снизу 64/48 удерживают текст на плотной части облачка. Layout-тест проверяет нахождение подсказок и кнопки внутри панели и доступность текста при обоих разрешениях, регрессия проходит.
Read-set exception: нет.
Risks/blocker: ручной visual smoke не выполнялся.
Next: оценить высоту облачка в игровом окне.
```

Координатор подтвердил HF-42 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-41 — внутренние отступы облачка

Статус: `DONE`; координатор/исполнитель `/root`; HF-40 DONE; READY 2026-11-06. CP-HF-41: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), tools/verify.ps1; write-set — эти два C# файла и ORCHESTRATION_PLAN.md; единственный владелец `/root`, пересечений нет. Out of scope: ассеты, source/, логика/сценарий, пользовательские изменения, публикация. Приёмка: контент дальше от мягких краёв, доступная высота текста >80 при двух поддерживаемых размерах. Проверки: существующий layout/integration, полный verify, diff-check.


```text
Task: HF-41
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf41-verify.log.
Acceptance: внутренние отступы увеличены с 48/48/24/24 до 80/80/40/36 (слева/справа/сверху/снизу). Существующий layout-тест подтверждает доступность текста/кнопки при 1280×720 и 960×540, прежний диалоговый цикл проходит.
Read-set exception: нет.
Risks/blocker: ручной visual smoke не выполнялся.
Next: оценить отступы в игровом окне.
```

Координатор подтвердил HF-41 2026-11-06 по verify и diff-check: REVIEW → DONE.
### HF-40 — шрифт и края диалогового облачка

Статус: `DONE`; координатор/исполнитель `/root`; HF-39 DONE; READY 2026-11-06. CP-HF-40: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), themes/game_theme.tres, art/ui/dialog_panel.png.import, tools/verify.ps1, скриншот пользователя; write-set — ORCHESTRATION_PLAN.md, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs. Единственный владелец `/root`, пересечений нет. Out of scope: исходный PNG/шрифт, source/, сюжет/домен, пользовательские изменения, публикация. Приёмка: явная общая тема и шрифт реплик проекта, отсутствие Rule, плавное затухание альфы на границах спрайта без прямого среза; прежние размеры/управление. Проверки: integration шрифта/оформления/layout, полный verify, diff-check.


```text
Task: HF-40
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: финальный tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf40-verify.log.
Acceptance: общая Theme проекта явно подключена к DialoguePlayer, все начертания RichTextLabel используют её DefaultFont; проверено совпадение font resource для имени, реплик и кнопки. Rule удалён. На самой панели canvas shader плавно обнуляет альфу по всем границам исходного PNG, убирая прямоугольный срез; дочерние тексты/кнопка материал не наследуют. Layout и управление проходят прежнюю регрессию.
Read-set exception: нет.
Risks/blocker: ручной visual smoke не выполнялся; исходные ассеты и пользовательские изменения сохранены.
Next: оценить края облачка в игровом окне.
```

Координатор подтвердил HF-40 2026-11-06 по финальному verify и diff-check: REVIEW → DONE.
### HF-39 — композиция и оформление диалога

Статус: `DONE`; координатор/исполнитель `/root`; HF-38-D DONE; READY проверен 2026-11-06. CP-HF-39: read-set — текущий фронт, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), tools/verify.ps1, приложенный скриншот; write-set — эти два C# файла и ORCHESTRATION_PLAN.md. Владелец `/root`, пересечений нет. Out of scope: исходные PNG/сценарий, доменные правила, source/, пользовательские изменения, публикация. Приёмка: компактная нижняя панель со спрайтом, оформленные имя/разделитель/кнопка, второстепенные подсказки снаружи; текст/кнопка доступны при 1280×720/960×540. Проверки: layout integration, полный verify с Godot .NET и roll-forward, diff-check.

```text
Task: HF-39
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, build, 47 domain tests, import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf39-verify.log.
Acceptance: нижняя панель высотой 280–340 вместо растянутой центральной; сохранён PNG. Более тёмный тёплый фон, двухстрочный титр, имя и тонкий золотистый разделитель, кремовые реплики с увеличенным межстрочным расстоянием; компактная кнопка с отдельными hover/pressed стилями. Счётчик и подсказки приглушены, подсказки вынесены за панель. Проверены положение/высота, текст/кнопка и подсказки при двух разрешениях, прежний диалоговый цикл и регрессия.
Read-set exception: нет.
Risks/blocker: ручной visual smoke не выполнялся; изменения пользователя сохранены, публикация не выполнялась.
Next: оценить оформление в игровом окне.
```

Координатор подтвердил HF-39 2026-11-06 по verify и diff-check: REVIEW → DONE.

### HF-38-D — спрайт окна диалога

Статус: `DONE`; координатор/исполнитель `/root`; HF-37-D DONE; READY проверен 2026-11-06. Единственный владелец write-set `/root`, пересечений нет.

#### CP-HF-38-D

Read-set: текущий фронт плана, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (диалог), art/ui/dialog_panel.png, tools/verify.ps1, ранее определённый путь Godot .NET, git status/diff.
Write-set: ORCHESTRATION_PLAN.md, scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs (проверка текстуры панели).
Out of scope: исходный спрайт, сценарий/логика переходов, source/, прочие пользовательские изменения, commit/push.
Приёмка: фон Panel использует прикреплённый dialog_panel.png вместо программной заливки/рамки; текст и навигация внутри панели при 1280×720/960×540. Проверки: существующие integration layout/клики и проверка ресурса StyleBoxTexture, tools/verify.ps1 с Godot .NET и DOTNET_ROLL_FORWARD=Major, git diff --check.

```text
Task: HF-38-D
Recommended status: REVIEW
Changed: scripts/Dialogue/DialoguePlayer.cs, tests/ProjectTest.cs, ORCHESTRATION_PLAN.md.
Checks: tools/verify.ps1 с локальным Godot 4.7.2 .NET и DOTNET_ROLL_FORWARD=Major -> PASS (exit 0, 47 domain tests, build/import/integration); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf38-verify.log.
Acceptance: StyleBoxTexture использует исходный dialog_panel.png с прозрачностью и растяжением по панели; программная заливка/рамка заменена спрайтом. Отступы 48 по горизонтали и 28 по вертикали удерживают контент внутри мягких краёв. Проверены текстура, доступность текста/кнопок при двух размерах окна и все прежние переходы/клики.
Read-set exception: нет; source/ не читался.
Risks/blocker: ручной visual smoke не выполнялся; исходный PNG и пользовательские изменения сохранены.
Next: посмотреть спрайт в игровом окне.
```

Координатор подтвердил HF-38-D 2026-11-06 по verify и diff-check: REVIEW → DONE; основные ворота без изменений.

### HF-37-D — вступительный диалог по сценарию

Статус: `DONE`; координатор/исполнитель `/root`; зависимости отсутствуют; READY проверен 2026-11-06, пакет CP-HF-37-D создан до реализации. Единственный владелец плана и перечисленных файлов `/root`, активных пересечений нет.

#### CP-HF-37-D

Read-set: этот план, AGENTS.md, scripts/MainMenu.cs, scenes/main_menu.tscn, ChikagoShop.csproj, tests/ProjectTest.cs, tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, tools/verify.ps1, .vscode/settings.json, README.md; пользовательский C:/Users/artem/Downloads/сценарий.doc (текст вступления/раскадровки); список путей art/ и существующий art/rooms/showroom.jpg, art/fonts/game.ttf; новые файлы write-set.
Write-set: ORCHESTRATION_PLAN.md, scripts/MainMenu.cs, scripts/Dialogue/DialoguePlayer.cs[.uid], src/ChikagoShop.Domain/Dialogue/DialogueSession.cs, tests/ChikagoShop.Domain.Tests/DialogueSessionTests.cs, tests/ProjectTest.cs, dialogue/intro.ru.json, README.md.
Out of scope: source/, project.godot и его пользовательские изменения, TradeScene.cs, save schema, экономика, обучение и диалоги следующих дней, новый арт/озвучка, публикация. Существующие изображения временно используются как фон, новые иллюстрации раскадровки не создаются.
Приёмка: «Играть» запускает раскадровку и все реплики вступления в исходном порядке, до завершения игровая сцена/таймер не создаются; посимвольный вывод, следующий шаг кнопкой/Enter/Space, возврат в меню через Escape; финал однократно открывает прежнюю игровую сцену. Данные отдельно от UI, последовательность вне MainMenu/TradeScene. Проверки: domain tests загрузки/порядка/границ, Godot integration запуска/ввода/отмены/завершения/повторного запуска, tools/verify.ps1, git diff --check.
Read-set exception: координатор точечно искал расположение меню/тестов и инструменты чтения DOC перед оформлением CP. DOC прочитан с FileShare.ReadWrite, так как открыт другим приложением; source/ не читался.

```text
Task: HF-37-D
Recommended status: REVIEW
Changed: ORCHESTRATION_PLAN.md, README.md, scripts/MainMenu.cs, scripts/Dialogue/DialoguePlayer.cs[.uid], src/ChikagoShop.Domain/Dialogue/DialogueSession.cs, tests/ChikagoShop.Domain.Tests/DialogueSessionTests.cs, tests/ProjectTest.cs, dialogue/intro.ru.json.
Checks: tools/verify.ps1 -GodotPath <локальный Godot 4.7.2 .NET console> с DOTNET_ROLL_FORWARD=Major -> PASS (build: 0 ошибок/предупреждений; 47 domain tests; Godot import и полная integration regression); git diff --check -> PASS. Evidence: OS TEMP/chikago-hf37-verify.log.
Acceptance: «Играть» открывает 26 последовательных записей (22 исходные реплики и 4 ремарки), имена говорящих, печать текста, раскрытие/переход кнопкой или Enter/Space. Esc возвращает в меню, повторный запуск начинает заново; повторный Play/финальный Advance не дублируют переход. Gameplay создаётся только после последней записи; прежний save загружается без изменений. Проверены реальные GUI-клики, клавиатура/игнорирование echo, текст всех записей, доступность панели/кнопки при 1280×720 и 960×540, отмена/повтор и возврат из gameplay.
Read-set exception: дополнительно просмотрен art/ui/dialog_panel.png для оценки пригодности (не использован); точечный поиск Godot .NET в Downloads для диагностики toolchain. Configured Steam Godot не поддерживает C#, первый verify с ним непригоден; выбран установленный Godot .NET через существующий параметр -GodotPath. На машине только runtime .NET 10, поэтому testhost запущен с roll-forward без изменения проекта. Первый sandbox-запуск Godot не мог записать системные логи, финальная проверка выполнена с одобренным повышением доступа. source/ не читался.
Risks/blocker: ручной visual smoke не выполнен. Фон существующий временный, действия раскадровки представлены текстом; отдельные иллюстрации, звонок/озвучка и дальнейшее обучение вне scope. Текст сценария пока русский при обоих языках UI. Пользовательский project.godot сохранён; commit/push не выполнялись.
Next: проверить вступление в игровом окне и согласовать следующую итерацию постановки.
```

Координатор подтвердил HF-37-D 2026-11-06 по финальному verify (exit 0), integration acceptance и diff-check: REVIEW → DONE. Статус задачи: `DONE`; основные ворота без изменений.



### HF-38 — слияние всей рабочей ветки и публикация в main

Статус: `DONE`; координатор/исполнитель `/root`; HF-37 DONE; READY проверен 2026-11-06. Пользователь явно поручил включить все локальные изменения, слить feature/second-minigame в main и отправить на GitHub.

#### CP-HF-38

Read-set: ORCHESTRATION_PLAN.md, AGENTS.md, tools/verify.ps1, git status/diff/log/branches/remotes; OS TEMP/chikago-hf38-verify.log. Содержимое source/ не читается.
Write-set: ORCHESTRATION_PLAN.md; Git index/commits, feature/second-minigame, main и origin/main. В commit включается весь текущий неигнорируемый набор git status: README.md, export_presets.cfg, localization/en.tres, project.godot, scenes/trade_scene.tscn, scripts/Days/DayCycleController.cs, scripts/TradeScene.cs, scripts/UI/EmbroideryGame.cs, scripts/UI/PauseMenu.cs, src/ChikagoShop.Domain/Crafting/EmbroiderySession.cs, tests/ProjectTest.cs; MyFont_Completed.ttf[.import], atelier_embroidery_v4.html, atelier_polishing_v8.html, scripts/UI/PolishingSurface.cs[.uid], src/ChikagoShop.Domain/Crafting/ICraftSession.cs, src/ChikagoShop.Domain/Crafting/PolishingSession.cs, tests/ChikagoShop.Domain.Tests/PolishingSessionTests.cs; art/minigames/polishing_guitar.svg[.import], polishing_guitar_mask.svg[.import], polishing_sax.svg[.import], polishing_sax_mask.svg[.import], polishing_trumpet.svg[.import], polishing_trumpet_mask.svg[.import], polishing_violin.svg[.import], polishing_violin_mask.svg[.import]. Владелец плана/index `/root`; активных пересечений нет. Содержимое продуктовых файлов не меняется.
Out of scope: source/, ignored/generated files, новая продуктовая логика, force push, удаление веток.
Приёмка: полный tools/verify.ps1 PASS, git diff --check PASS; все неигнорируемые локальные изменения закоммичены; feature/second-minigame слита в main; push origin main без force; чистое рабочее дерево и main...origin/main=0/0.


```text
Task: HF-38
Recommended status: REVIEW
Changed: 37 файлов в e0790fa; merge fd4d7c7 в main; origin/main обновлён без force.
Checks: tools/verify.ps1 -> PASS (exit 0, build, 47 domain tests, Godot import/integration); staged diff-check -> PASS; push origin main -> PASS; рабочее дерево чистое; main...origin/main=0/0; feature/second-minigame является предком main.
Acceptance: все неигнорируемые локальные изменения, включая пользовательские настройки/сцену/шрифт и оба HTML-референса, опубликованы в main по прямому запросу пользователя.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет.
Next: продолжить по запросу пользователя.
```

Координатор подтвердил HF-38 2026-11-06 по verify, успешному merge/push и чистому дереву: REVIEW → DONE. Эта запись публикуется отдельным документационным commit.
### HF-37 — полировка инструментов в общем окне крафта

Статус: `DONE`; координатор/исполнитель `/root`; HF-36 DONE; READY проверен 2026-11-06; ветка feature/second-minigame.

#### CP-HF-37

Read-set: текущий фронт плана, AGENTS.md, atelier_polishing_v8.html, scripts/UI/EmbroideryGame.cs, src/ChikagoShop.Domain/Crafting/EmbroiderySession.cs, scripts/TradeScene.cs, scripts/Days/DayCycleController.cs, scripts/UI/PauseMenu.cs, tests/ProjectTest.cs, tests/ChikagoShop.Domain.Tests/EmbroiderySessionTests.cs, README.md, localization/en.tres, tools/verify.ps1, git status/diff; render/verify logs и screenshot OS TEMP/chikago-polishing-preview.png для визуальной диагностики.
Write-set: ORCHESTRATION_PLAN.md; scripts/UI/EmbroideryGame.cs (общая оболочка двух мини-игр); scripts/UI/PolishingSurface.cs[.uid]; src/ChikagoShop.Domain/Crafting/ICraftSession.cs, EmbroiderySession.cs, PolishingSession.cs; tests/ChikagoShop.Domain.Tests/PolishingSessionTests.cs; scripts/TradeScene.cs (подключение второй оболочки/кнопки и общий флаг модальности); scripts/Days/DayCycleController.cs; scripts/UI/PauseMenu.cs; tests/ProjectTest.cs; localization/en.tres; README.md; art/minigames/polishing_{violin,trumpet,sax,guitar}.svg[.import] и polishing_{violin,trumpet,sax,guitar}_mask.svg[.import], извлечённые из точного HTML-референса. Диагностические logs и screenshot OS TEMP/chikago-polishing-preview.png.
Out of scope: source/, HTML-референсы, .tscn, пользовательские project/export/font, новые правила выпадения/цены/редкости, save schema, публикация. Владелец `/root`; активных пересечений нет.
Приёмка: музыкальный крафт открывает полировку с содержимым HTML v8 (четыре инструмента, тряпка, два слоя налёта, повторные быстрые проходы/развороты, допуск края47, промахи, цель85%, 10с). Рамка/палитра/карточки/шкала/кнопки/старт/результат общие с вышивкой, средняя карточка показывает очищенный процент. День продолжает идти; камера/другие действия заблокированы; отмена до старта бесплатна, старт фиксирует3 инструмента за140$ и1 энергию однократно. Закрытие дня, повторы, RU/EN, mouse/touch, save/reload совместимы. Domain regressions и реальные GUI events для полировки/вышивки; tools/verify.ps1 и git diff --check.

```text
Task: HF-37
Recommended status: REVIEW
Changed: scripts/UI/EmbroideryGame.cs; scripts/UI/PolishingSurface.cs[.uid]; src/ChikagoShop.Domain/Crafting/ICraftSession.cs, EmbroiderySession.cs, PolishingSession.cs; tests/ChikagoShop.Domain.Tests/PolishingSessionTests.cs; scripts/TradeScene.cs; scripts/Days/DayCycleController.cs; scripts/UI/PauseMenu.cs; tests/ProjectTest.cs; localization/en.tres; README.md; ORCHESTRATION_PLAN.md; восемь art/minigames/polishing_*.svg[.import] (четыре изображения и четыре точные маски из HTML).
Checks: tools/verify.ps1 -> PASS (exit 0, 47 domain tests, build, Godot import/integration); отдельный запуск ProjectTest с gl_compatibility и --polishing-preview -> все checks PASS, stderr пуст; screenshot OS TEMP/chikago-polishing-preview.png осмотрен; git diff --check -> PASS. Итоговый лог OS TEMP/chikago-hf37-verify-final.log.
Acceptance: музыка открывает полировку в том же классе оболочки, что вышивка: изменены только рабочая область, заголовок/подсказка и средняя карточка (очищено вместо стежков). Четыре SVG/маски извлечены из пользовательского v8, отдельно показана тряпка; shader последовательно открывает материал сквозь тёмный и матовый слои. Pure domain рассчитывает прогресс по маске, усилие скорости/разворота, два слоя, радиус34, ограничение длины прохода34, допуск47, cooldown промаха240ms, цель85% и оценку v8. Общие 10с, S–D, статистика, кнопки и двухминутные часы сохранены. Domain tests проверяют неподвижное удержание/отпускание, очередность слоёв, успех/terminal, скорость/разворот, ограничение прыжка, допуск/промахи и invalid input. Integration проверяет все четыре mask-ресурса, реальную кнопку музыки, общий layout при двух размерах, RU/EN, held mouse/touch events и отпускание вне области, idempotent покупку, оба timeout дня и reload оплаченной партии. Прежняя вышивка/торговля/сохранения проходят. Визуально проверена настоящая отрисовка саксофона с налётом в общей оболочке.
Read-set exception: нет; source/ не читался. HTML не изменён, новый контент извлечён только из названного референса. GPU smoke запущен в скрытом/offscreen окне, чтобы не мешать пользователю.
Risks/blocker: ручной playtest сложности не выполнялся; покрытие слоёв семплируется с шагом7, поэтому это порт механики v8, а не побитовое совпадение canvas. Качество по-прежнему не влияет на редкость/цену. Пользовательские project/export/scene/font и HTML сохранены; commit/push не выполнялись.
Next: пройти полировку через кнопку создания музыкальных инструментов в игровом сеансе.
```

Координатор подтвердил HF-37 2026-11-06 по финальному verify (exit 0), GUI regression и GPU screenshot: REVIEW → DONE; основные ворота без изменений.

### HF-36 — публикация вышивки, двухминутного дня и редких предметов

Статус: `DONE`; координатор/исполнитель `/root`; HF-31…HF-35 DONE; READY проверен 2026-11-06. Пользователь явно запросил push.

#### CP-HF-36

Read-set: текущий фронт плана, git status/diff/log/remotes, файлы и verify evidence HF-31…HF-35, tools/verify.ps1; отдельный временный worktree выбранного commit для проверки публикации.
Write-set: ORCHESTRATION_PLAN.md; Git index/commit и origin/main только для файлов HF-31…HF-35: README.md, localization/en.tres, scripts/CounterWorld.cs, scripts/Days/DayCycleController.cs, scripts/TradeScene.cs, scripts/UI/PauseMenu.cs, scripts/UI/EmbroideryGame.cs[.uid], scripts/Crafting/WorkshopItemCatalog.cs[.uid], src/ChikagoShop.Domain/Days/DaySession.cs, src/ChikagoShop.Domain/Crafting/EmbroiderySession.cs, WeightedItemTable.cs в той же папке; tests/ProjectTest.cs, tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, EmbroiderySessionTests.cs и WeightedItemTableTests.cs в той же папке; шесть art/items/placeholder_{clothing,music}_{first,exclusive,unique}.svg[.import]. Отдельный worktree в OS TEMP для проверки итогового commit. Владелец плана/index `/root`, пересечений нет.
Out of scope: source/, пользовательские export_presets.cfg/project.godot/scenes/trade_scene.tscn/MyFont_Completed.ttf[.import], исходный atelier_embroidery_v4.html, новые продуктовые изменения, force push.
Приёмка: выбранные файлы составляют самостоятельный commit; полный tools/verify.ps1 в отдельном worktree PASS, staged diff-check PASS; push origin/main без force, пользовательские файлы сохранены локально.

```text
Task: HF-36
Recommended status: REVIEW
Changed: commit 064ab30 содержит 30 файлов HF-31…HF-35 и текущую запись плана. Пользовательские project.godot/export_presets.cfg/scenes/trade_scene.tscn/MyFont_Completed.ttf[.import] и исходный HTML не включены.
Checks: git fetch origin -> PASS, перед commit HEAD...origin/main=0/0; staged diff-check -> PASS; tools/verify.ps1 в отдельном worktree 064ab30 -> PASS (exit 0, 41 domain tests, build, Godot import/integration); git push origin main -> PASS (8d6a047..064ab30, exit 0); после push HEAD...origin/main=0/0.
Acceptance: вышивка со шкалой/карточками, двухминутный день с непрерывным отсчётом при крафте и шесть высокоредких квадратов опубликованы в origin/main. Полная проверка выполнена на точном опубликованном наборе без пользовательских локальных правок.
Read-set exception: первый import чистого worktree сообщил об отсутствующем font cache .godot; сам import создал cache, последующий полный verify exit 0 без этих ошибок. Первая обёртка PowerShell вернула exit 1 из-за native stderr, хотя внутренние шаги завершились; успешный финальный лог OS TEMP/chikago-hf36-verify-final.log. source/ не читался.
Risks/blocker: нет; пользовательские изменения сохранены локально.
Next: продолжить по запросу пользователя.
```

Координатор подтвердил HF-36 2026-11-06 по точному commit, финальному verify (exit 0) и успешному push: REVIEW → DONE. Evidence публикуется отдельным документационным коммитом.

### HF-35 — цветные заглушки высоких редкостей в обеих мастерских

Статус: `DONE`; координатор/исполнитель `/root`; HF-34 DONE; READY проверен 2026-11-06.

#### CP-HF-35

Read-set: текущий фронт плана, scripts/TradeScene.cs, scripts/CounterWorld.cs, scripts/UI/InventoryUI.cs, scripts/UI/SlotUI.cs, scripts/Configuration/EconomyConfigProvider.cs, config/economy.v1.json, src/ChikagoShop.Domain/Configuration/EconomyConfig.cs, src/ChikagoShop.Domain/Items/ItemDefinition.cs, scenes/trade_scene.tscn (только позиции предметов), tests/ProjectTest.cs, tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj, localization/en.tres, README.md, tools/verify.ps1, git status/diff; новые файлы write-set.
Write-set: ORCHESTRATION_PLAN.md; scripts/Crafting/WorkshopItemCatalog.cs[.uid]; src/ChikagoShop.Domain/Crafting/WeightedItemTable.cs; tests/ChikagoShop.Domain.Tests/WeightedItemTableTests.cs; scripts/TradeScene.cs (извлечение таблицы предметов в каталог, вызовы выбора и исправление подсказок); scripts/CounterWorld.cs; tests/ProjectTest.cs; localization/en.tres; README.md; art/items/placeholder_clothing_first.svg[.import], placeholder_clothing_exclusive.svg[.import], placeholder_clothing_unique.svg[.import], placeholder_music_first.svg[.import], placeholder_music_exclusive.svg[.import], placeholder_music_unique.svg[.import] в том же art/items/.
Out of scope: source/, .tscn, пользовательские project/export/font, существующие изображения, формула качества вышивки, новый баланс цен/весов, save schema, публикация. Владелец `/root`, пересечений нет.
Приёмка: добавлены шесть цветных квадратов (по first_class/exclusive/unique для atelier/music), полный дроп всех пяти редкостей с весами/ценами config; крафт/инвентарь/витрина/сделка отображают новые вещи, tooltip/рероллы/продажа и reload работают. Старые индексы 0–3 сохранены, новые добавлены в конец; каталог/выбор вне TradeScene. Проверки: domain тесты всех весовых границ, integration принудительных новых предметов и полного игрового цикла, tools/verify.ps1, git diff --check.

```text
Task: HF-35
Recommended status: REVIEW
Changed: scripts/Crafting/WorkshopItemCatalog.cs[.uid]; src/ChikagoShop.Domain/Crafting/WeightedItemTable.cs; tests/ChikagoShop.Domain.Tests/WeightedItemTableTests.cs; scripts/TradeScene.cs; scripts/CounterWorld.cs; tests/ProjectTest.cs; localization/en.tres; README.md; ORCHESTRATION_PLAN.md; шесть art/items/placeholder_{clothing,music}_{first,exclusive,unique}.svg[.import].
Checks: tools/verify.ps1 -> PASS (41 domain tests, build, Godot import/integration); git diff --check -> PASS. Лог: OS TEMP/chikago-hf35-verify.log.
Acceptance: по три временных предмета каждой мастерской с отдельными цветами; одежда 80/120/200$, инструменты 120/180/300$, rerolls 2/1/1 из общего config. Каталог хранит прежние индексы 0–3, дополняет их 4–9; цены/названия редкостей/веса/рероллы берутся из config. Все 100 tickets каждой категории проверены на веса 60/25/10/4/1. Domain selector не зависит от Godot. Интеграция принудительно проверяет все шесть предметов: прозрачное поле/различимость цветов, inventory icon/tooltip, showcase icon, единственный нужный trade sprite, цену оффера, reroll, active-trade reload и продажу. Существующие saves/локализация/торговля/вышивка/таймер проходят. RU/EN названия новых предметов, подсказки комнат больше не обещают лишь два вида вещей.
Read-set exception: нет; source/ не читался. Существующие scene sprites прочитаны только для определения размещения, .tscn не редактировалась.
Risks/blocker: ручной visual playtest не выполнялся; квадраты намеренно временные. Качество вышивки остаётся независимой оценкой; дроп определяется существующими весами редкостей. Пользовательские изменения сохранены, commit/push не выполнялись.
Next: проверить выпадение цветных предметов в обеих мастерских игрового сеанса.
```

Координатор подтвердил HF-35 2026-11-06 по финальному verify (exit 0), полной регрессии и diff-check: REVIEW → DONE; основные ворота без изменений.

### HF-34 — двухминутный день без остановки часов на мини-игре

Статус: `DONE`; координатор/исполнитель `/root`; HF-33 DONE; READY проверен 2026-11-06.

#### CP-HF-34

Read-set: текущий фронт плана, src/ChikagoShop.Domain/Days/DaySession.cs, scripts/Days/DayCycleController.cs, scripts/UI/EmbroideryGame.cs, scripts/UI/PauseMenu.cs, scripts/TradeScene.cs, tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, tests/ProjectTest.cs, README.md, tools/verify.ps1, git status/diff.
Write-set: ORCHESTRATION_PLAN.md, src/ChikagoShop.Domain/Days/DaySession.cs, scripts/Days/DayCycleController.cs, scripts/UI/EmbroideryGame.cs, scripts/TradeScene.cs (только forwarding таймера контроллеру), tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, tests/ProjectTest.cs, README.md.
Out of scope: source/, HTML, сцены, пользовательские настройки/art/font, формула качества/таймер вышивки, save schema, публикация. Владелец `/root`, пересечений нет.
Приёмка: полный день 120 секунд, HUD 02:00; часы идут на всех экранах мини-игры, но обычная пауза замораживает их. Нельзя начать крафт после закрытия; запущенный до закрытия крафт оплачивается/фиксируется при «Начать» и может завершиться после 00:00 без повторной награды/списания. Часы видны поверх модального экрана. Существующие saves сохраняют остаток, старые без clock получают 120. Domain/GUI regressions, tools/verify.ps1, git diff --check.

```text
Task: HF-34
Recommended status: REVIEW
Changed: src/ChikagoShop.Domain/Days/DaySession.cs; scripts/Days/DayCycleController.cs; scripts/UI/EmbroideryGame.cs; scripts/TradeScene.cs; tests/ChikagoShop.Domain.Tests/DaySessionTests.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: tools/verify.ps1 -> PASS (39 domain tests, build, Godot import/integration); git diff --check -> PASS. Лог: OS TEMP/chikago-hf34-verify.log.
Acceptance: новый день/чекпоинт/legacy reset получают 120 секунд, HUD 02:00. На ready/play/result вышивки день тикает через DayCycleController, обычная пауза сохраняет заморозку, двойного тика нет. Часы продублированы в верхнем правом углу модального окна. Покупка партии фиксируется существующим AcquireItem и save при «Начать», повторное нажатие не дублирует её; после 00:00 готовую партию можно забрать, день остаётся закрыт. Timeout до начала бесплатно закрывает окно; новые крафты закрытого дня запрещены. GUI regression покрывает все три экрана, обе границы timeout, одну покупку, 00:00 в модальном окне, следующий двухминутный день. Save/reload точной доли секунды (109.75), legacy saves, обычная пауза, последние сделки и вся прежняя регрессия проходят.
Read-set exception: нет; source/ не читался.
Risks/blocker: ручной visual playtest не выполнялся. Существующие saves с clock сохраняют прежний остаток, полные две минуты даются новому дню. Незавершённая вышивка при перезапуске не восстанавливается, но уже оплаченная партия сохранена в инвентаре. Посторонние изменения сохранены, публикация не выполнялась.
Next: проверить двухминутный день и окончание времени во время вышивки в игровом сеансе.
```

Координатор подтвердил HF-34 2026-11-06 по полному verify (exit 0) и diff-check: REVIEW → DONE; основные ворота без изменений.

### HF-33 — карточки времени, стежков и промахов

Статус: `DONE`; координатор/исполнитель `/root`; HF-32 DONE; READY проверен 2026-11-06.

#### CP-HF-33

Read-set: текущий фронт плана, scripts/UI/EmbroideryGame.cs, tests/ProjectTest.cs (Embroidery), localization/en.tres, tools/verify.ps1, git status/diff; приложенный референс.
Write-set: ORCHESTRATION_PLAN.md, scripts/UI/EmbroideryGame.cs, tests/ProjectTest.cs, localization/en.tres. Владелец `/root`; активных пересечений нет.
Out of scope: source/, HTML, сцены, игровые правила/баланс/save schema, пользовательские изменения, публикация.
Приёмка: вместо одной строки три компактные карточки «ВРЕМЯ / СТЕЖКИ / ПРОМАХИ» в текущей палитре, с крупными актуальными числами и RU/EN; карточки не перекрывают ткань/шкалу. Проверки: GUI regression для начальных значений, попаданий, промахов, времени и размещения при 1280×720/960×540; tools/verify.ps1; git diff --check.

```text
Task: HF-33
Recommended status: REVIEW
Changed: scripts/UI/EmbroideryGame.cs; tests/ProjectTest.cs; localization/en.tres; ORCHESTRATION_PLAN.md
Checks: финальный tools/verify.ps1 -> PASS (39 domain tests, build, Godot import/integration); git diff --check -> PASS. Лог: OS TEMP/chikago-hf33-verify.log.
Acceptance: три карточки с тёмным фоном, тонкой приглушённой золотистой рамкой, малыми заголовками и крупными кремовыми числами над тканью. Время с десятыми долями, стежки выполнено/всего и промахи обновляются из текущей сессии. RU/EN. GUI regression подтверждает стартовые/конечные значения, промахи, формат времени, перевод и размещение при двух размерах экрана; прежние проверки проходят.
Read-set exception: нет; source/ не читался.
Risks/blocker: ручной visual playtest не выполнялся; посторонние изменения сохранены.
Next: посмотреть карточки в мини-игре ателье.
```

Координатор подтвердил HF-33 2026-11-06 по финальному verify (exit 0) и diff-check: REVIEW → DONE; основные ворота без изменений.

### HF-32 — шкала качества S–D в палитре игры

Статус: `DONE`; координатор/исполнитель `/root`; HF-31 DONE; READY проверен 2026-11-06.

#### CP-HF-32

Read-set: текущий фронт плана, scripts/UI/EmbroideryGame.cs, tests/ProjectTest.cs (Embroidery), tools/verify.ps1, git status/diff; приложенный пользователем референс.
Write-set: ORCHESTRATION_PLAN.md, scripts/UI/EmbroideryGame.cs, tests/ProjectTest.cs. Единственный владелец `/root`, пересечений нет.
Out of scope: source/, HTML, сцены, баланс/формулы качества/крафт/save schema, посторонние пользовательские изменения, публикация.
Приёмка: высокая вертикальная шкала с золотистой рамкой, пятью секциями и буквами S/A/B/C/D слева; приглушённые шалфейный/охра/медь/терракота/бордовый в текущей палитре. Текущий ранг выделен, заполнение соответствует качеству, процент под шкалой. Проверки: существующий GUI regression расширен на размещение и выделение ранга, tools/verify.ps1, git diff --check.

```text
Task: HF-32
Recommended status: REVIEW
Changed: scripts/UI/EmbroideryGame.cs; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: tools/verify.ps1 -> PASS (39 domain tests, build, Godot import/integration); git diff --check -> PASS. Лог: OS TEMP/chikago-hf32-verify.log.
Acceptance: шкала высотой с ткань, золотистая рамка, градиент шалфейный → охра → медь → терракота → бордовый, пять равных секций. Слева S/A/B/C/D с золотым выделением текущего ранга, процент снизу. Пустая часть затемняется при падении качества. GUI regression подтверждает геометрию и выравнивание букв на 1280×720/960×540, смену выделения S на текущий ранг и процент.
Read-set exception: нет; source/ не читался.
Risks/blocker: ручной visual playtest не выполнялся. Ранее существовавшие изменения сохранены.
Next: посмотреть обновлённую шкалу в мини-игре ателье.
```

Координатор подтвердил HF-32 2026-11-06 по полному verify (exit 0) и diff-check: REVIEW → DONE; основные ворота без изменений.

### HF-31 — вышивка при создании одежды в Godot

Статус: `DONE`; координатор/исполнитель `/root`; READY проверен 2026-11-06. HF-30 не решает уточнённую задачу пользователя: HTML является референсом, цель — игровой крафт.

#### CP-HF-31

Read-set: текущий фронт плана, AGENTS.md, atelier_embroidery_v4.html, scripts/TradeScene.cs, scripts/Days/DayCycleController.cs, scripts/UI/PauseMenu.cs, scripts/Camera/WorldCamera.cs, scripts/Localization/GameLocalization.cs, localization/en.tres, tests/ProjectTest.cs, ChikagoShop.csproj, src/ChikagoShop.Domain/ChikagoShop.Domain.csproj, tests/ChikagoShop.Domain.Tests/DaySessionTests.cs, tools/verify.ps1, .vscode/settings.json, README.md, git status/diff; новые файлы write-set.
Write-set: ORCHESTRATION_PLAN.md; src/ChikagoShop.Domain/Crafting/EmbroiderySession.cs; tests/ChikagoShop.Domain.Tests/EmbroiderySessionTests.cs; scripts/UI/EmbroideryGame.cs[.uid]; scripts/TradeScene.cs (только подключение контроллера к кнопке); scripts/UI/PauseMenu.cs (защита владения паузой); tests/ProjectTest.cs; localization/en.tres; README.md; atelier_embroidery_v4.html (только отмена собственных ошибочных изменений HF-30 по уточнению пользователя).
Out of scope: source/, .tscn, пользовательские project/export/font, save schema, новый баланс/редкости/цены от оценки вышивки, музыкальный крафт, публикация. Активных пересечений нет, владелец `/root`.
Приёмка: кнопка ателье открывает модальную мини-игру с пяльцами, тканью, точками и нитью; 10 секунд, последовательные попадания/промахи, оценка, краткие RU/EN подписи. До начала можно отменить без затрат; после старта завершение или timeout выдаёт прежние три вещи за 90$ и 1 энергию ровно один раз при подтверждении результата. Мир и таймер дня приостановлены, мышь/touch не управляют миром; выход освобождает паузу. Domain tests и Godot GUI regression для интеграции/повтора/отмены/timeout/геометрии; полный tools/verify.ps1 и git diff --check.

```text
Task: HF-31
Recommended status: REVIEW
Changed: src/ChikagoShop.Domain/Crafting/EmbroiderySession.cs; tests/ChikagoShop.Domain.Tests/EmbroiderySessionTests.cs; scripts/UI/EmbroideryGame.cs[.uid]; scripts/TradeScene.cs; scripts/UI/PauseMenu.cs; tests/ProjectTest.cs; localization/en.tres; README.md; ORCHESTRATION_PLAN.md; atelier_embroidery_v4.html (отмена HF-30).
Checks: tools/verify.ps1 -> PASS (39 domain tests, build без предупреждений, Godot import и полная integration regression); git diff --check -> PASS. Лог: OS TEMP/chikago-hf31-verify.log.
Acceptance: игровой вход ателье открывает отдельный CanvasLayer с пяльцами, тканью, четырьмя случайными узорами, нитью и пульсирующей целью. Правила последовательности, таймера 10 секунд и оценки живут в чистом домене. Промах обозначается крестиком, качество падает; результат показывает процент и ранг. До старта отмена кнопкой/Esc бесплатна. После старта финал/timeout выдаёт прежнюю партию при «Забрать», однократно списывает 90$ и энергию, сохраняет прежним способом. GUI regression проверяет настоящую кнопку ателье, mouse/touch, заморозку дня/камеры, ownership паузы, выход из сцены, RU/EN и размеры 1280×720/960×540; прежняя торговля/save/день также PASS. Доменные тесты покрывают все четыре узора, границу попадания, terminal-state, invalid input и timeout. HTML восстановлен как исходный референс.
Read-set exception: нет; source/ не читался.
Risks/blocker: ручной visual playtest не выполнялся. Качество является оценкой результата без влияния на цену/редкость; новый баланс не запрашивался. Незавершённая вышивка не сохраняется и не выдаёт товары/списания при перезапуске. Пользовательские project.godot/export/scene/font сохранены; commit/push не выполнялись.
Next: проверить вышивку через «Создать» в ателье игрового сеанса.
```

Координатор подтвердил HF-31 2026-11-06 по финальному verify (exit 0) и diff-check: REVIEW → DONE; основные стадии и ворота не менялись.

### HF-30 — лаконичный экран вышивки

Статус: `DONE`; координатор/исполнитель `/root`; независимая правка HTML по прямому запросу пользователя; READY проверен 2026-11-06.

#### CP-HF-30

Read-set: ORCHESTRATION_PLAN.md (координатор), AGENTS.md, atelier_embroidery_v4.html, git status/diff, наличие локального Node.js.
Write-set: atelier_embroidery_v4.html, ORCHESTRATION_PLAN.md. Владелец `/root`; пересечений с активными задачами нет.
Out of scope: source/, Godot/C# и сцены, игровые правила/таймер/подсчёт качества, публикация.
Приёмка: сохранены стиль, узоры и управление; сокращены стартовые/итоговые тексты, удалены повторные пояснения во время крафта; доступные названия сохранены. Проверки: Node.js syntax/runtime smoke с DOM/canvas stub для старта, попадания, промаха, завершения, повтора и выбора узора; git diff --check.

```text
Task: HF-30
Recommended status: REVIEW
Changed: atelier_embroidery_v4.html; ORCHESTRATION_PLAN.md
Checks: Node.js syntax/runtime smoke (initialization/start/miss/all hits/restart/timeout/pattern change) -> PASS; git diff --check -> PASS.
Acceptance: одна короткая подсказка перед стартом; убраны длинный help/footer, подзаголовок, дублирование ранга и текст над точкой/промахом. Результат сокращён до качества и ранга. Пяльцы, ткань, палитра, счётчики, четыре узора и правила сохранены; кнопка повтора и выбор узора имеют доступные названия.
Read-set exception: нет; координатор прочитал план; source/ не читался. Git найден по стандартному пути после отсутствия в PATH.
Risks/blocker: браузерный visual smoke не выполнялся; runtime проверен с DOM/canvas stub. Пользовательские изменения остальных файлов сохранены.
Next: открыть HTML в браузере для визуальной оценки.
```

Координатор подтвердил HF-30 2026-11-06 по runtime smoke и проверке изменений: REVIEW → DONE. Основная стадия и её ворота без изменений.

### HF-29 — раздельная публикация локализации и таймера

Статус: `DONE`; координатор/исполнитель `/root`; HF-27/HF-28 = DONE; `CP-HF-29`, READY проверен 2026-11-06. Пользователь явно разрешил push раздельных изменений.

#### CP-HF-29

Read-set: текущий фронт плана, git status/diff/log/remotes/show, файлы и verify evidence HF-27/HF-28, tools/verify.ps1.
Write-set: ORCHESTRATION_PLAN.md, Git index/commit и origin/main для файлов HF-27/HF-28; временные точные снимки README.md, scripts/TradeScene.cs, tests/ProjectTest.cs, localization/en.tres в папке ОС TEMP для разделения index; отдельный git worktree в TEMP для проверки промежуточного коммита. Рабочие версии сохраняются. Владелец index/плана `/root`; пересечений нет.
Out of scope: новые продуктовые изменения, source/, пользовательские export_presets.cfg/scenes/trade_scene.tscn/MyFont_Completed.ttf[.import], force push.
Приёмка: отдельный самостоятельный commit локализации без таймера, отдельный commit таймера поверх него; проверка промежуточной локализации и итоговый verify PASS; staged diff-check PASS; push origin/main без force; посторонние изменения сохранены.


```text
Task: HF-29
Recommended status: REVIEW
Changed: два отдельных feature commits для файлов HF-27/HF-28; пользовательские изменения вне этих задач не публиковались.
Checks: git fetch origin -> PASS; HEAD...origin/main перед commit = 0/0; localization-only tools/verify.ps1 в отдельном worktree -> PASS (19 domain tests + import/integration); итоговый tools/verify.ps1 -> PASS (33 domain tests + import/integration); оба staged diff-check -> PASS; git push origin main -> PASS (exit 0).
Acceptance: 192cea6 содержит только локализацию с её каталогом/проверками/docs; 14403f7 содержит минутный таймер, закрытие дня, save v8 и соответствующие проверки/docs. Оба опубликованы в origin/main без force. Промежуточный commit локализации самостоятельно собирается и проходит Godot regression без timer-классов. Пользовательские export_presets.cfg/scenes/trade_scene.tscn и исходный MyFont_Completed.ttf[.import] сохранены локально.
Read-set exception: диагностировался первый clean import временного worktree: отсутствовал font cache .godot до импорта; импорт создал cache, повторный полный verify завершился exit 0. Итоговые проверки выполнены последовательно, чтобы не делить test user:// files между процессами. source/ не читался.
Risks/blocker: нет.
Next: продолжить работу по запросу пользователя.
```

Координатор подтвердил HF-29 2026-11-06 по раздельным commit diffs, обеим проверкам и успешному push: REVIEW → DONE; evidence публикуется документационным коммитом.

### HF-28 — минутный день и завершение последней сделки

Статус: `DONE`; координатор/исполнитель `/root`; HF-27 = DONE; `CP-HF-28`, READY проверен 2026-11-06. Длительность дня — 60 секунд по ответу пользователя.

#### CP-HF-28

Read-set: текущий фронт плана, AGENTS.md из запроса, `scripts/TradeScene.cs`, `scripts/UI/PauseMenu.cs`, `scripts/UI/ProgressPanel.cs`, `scripts/UI/InventoryUI.cs`, `scripts/Localization/GameLocalization.cs`, `localization/en.tres`, `tests/ProjectTest.cs`, `tests/ChikagoShop.Domain.Tests/MoneyEnergyTests.cs`, `src/ChikagoShop.Domain/ChikagoShop.Domain.csproj`, `ChikagoShop.csproj`, `config/economy.v1.json`, `tools/verify.ps1`, `.vscode/settings.json`, `README.md`; новые файлы write-set; git status/diff. Диагностика координатора до пакета перечислена здесь.
Write-set: `src/ChikagoShop.Domain/Days/DaySession.cs`, `tests/ChikagoShop.Domain.Tests/DaySessionTests.cs`, `scripts/Days/DayCycleController.cs[.uid]`, `scripts/TradeScene.cs` (адаптация нового контроллера, сохранение clock state и существующий переход дня), `scripts/UI/PauseMenu.cs` (сохранение остатка при паузе), `tests/ProjectTest.cs`, `localization/en.tres`, `README.md`, `ORCHESTRATION_PLAN.md`. Единственный владелец save schema и плана `/root`; активных пересечений нет. Текущие изменения HF-27 сохраняются.
Out of scope: source/, новая доменная логика внутри TradeScene, остальные доменные извлечения, баланс/стоимость/квоты/RNG, изображения, .tscn и пользовательские export/font, публикация.
Приёмка: обратный отсчёт 01:00 → 00:00 по центру сверху; pause останавливает время. Ручное завершение и timeout закрывают текущий день, обнуляют энергию и отменяют новые приходы покупателей; активная сделка остаётся доступна для реролла/продажи. После последней продажи новые покупатели не приходят; явная кнопка следующего дня запускает свежую минуту и восстанавливает энергию. Квота проверяется после завершения последней сделки при переходе дня. Закрытие идемпотентно, timeout обрабатывается раньше прихода покупателя в том же кадре. Остаток времени/закрытие сохраняются, старые saves получают полную минуту, checkpoint/new game сбрасывают clock. Переводы RU/EN; headless regressions для ручного/авто закрытия, last-sale/quota, pause, reload/migration, UI centering/format. Проверки: полный tools/verify.ps1, git diff --check.


```text
Task: HF-28
Recommended status: REVIEW
Changed: src/ChikagoShop.Domain/Days/DaySession.cs; tests/ChikagoShop.Domain.Tests/DaySessionTests.cs; scripts/Days/DayCycleController.cs[.uid]; scripts/TradeScene.cs; scripts/UI/PauseMenu.cs; tests/ProjectTest.cs; localization/en.tres; README.md; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (33 domain tests, build, Godot import/integration); git diff --check -> PASS.
Acceptance: день длится 60 секунд по ответу пользователя; поверх HUD — только текстовый Label 01:00 → 00:00 по центру сверху, не зависит от zoom и не перехватывает мышь. Новые clock/closing rules находятся в DaySession, Godot orchestration/UI — в DayCycleController; TradeScene делегирует правила и сохраняет состояние. Ручное завершение и timeout закрывают магазин, делают энергию 0, отменяют buyer countdown, оставляют действующую сделку и её рероллы/продажу. После продажи магазин остаётся закрыт до явного «Начать следующий день»; квота учитывает последнюю продажу, новый день восстанавливает минуту/энергию и случайную задержку покупателя. v8 сохраняет остаток и closed state; pause сохраняет точную долю секунды; autosave при смене секунды; старые saves получают минуту без восстановления потраченной энергии; checkpoint/new game сбрасывают clock. Новые строки переведены RU/EN. Domain tests проверяют границы времени, идемпотентность, invalid delta и restore. Integration проверяет оба способа закрытия, реальные GUI reroll/sale/next-day, F и исчерпание рероллов после closing, quota после последней продажи, отсутствие следующих покупателей, timeout против pending buyer в одном кадре, pause/reload/legacy, центр таймера при 1280×720 и 960×540 с zoom 0.25/1.8. Прежняя регрессия и HF-27 проходят.
Read-set exception: диагностика координатора перед CP, перечисленная в read-set; source/ не читался. Verify log записан во временную папку ОС.
Risks/blocker: ручной visual playtest не выполнялся; NU1900 из-за недоступного vulnerability feed NuGet, ошибок компиляции нет. Локализация HF-27 и пользовательские export/scene/font изменения сохранены; публикация не выполнялась.
Next: проверить минутный торговый день в игровом сеансе.
```

Координатор подтвердил HF-28 2026-11-06 по финальному verify (exit 0), domain/integration regression и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-27 — русский и английский интерфейс

Статус: `DONE`; координатор/исполнитель `/root`; HF-26 = DONE; `CP-HF-27`, READY проверен 2026-11-06.

#### CP-HF-27

Read-set: текущий фронт плана, AGENTS.md из запроса; точечная инвентаризация scripts/scenes/tests/config/themes/tools/.vscode без source/; `project.godot`, `ChikagoShop.csproj`, `README.md`, `scripts/MainMenu.cs`, `scripts/TradeScene.cs`, `scripts/UI/InventoryUI.cs`, `scripts/UI/ItemInfoPopup.cs`, `scripts/UI/PauseMenu.cs`, `scripts/UI/RarityPanel.cs`, `scripts/UI/ProgressPanel.cs`, `scenes/main_menu.tscn`, `scenes/trade_scene.tscn`, `scenes/UI/ShowcaseSlot.tscn`, `tests/ProjectTest.cs`, `tests/ChikagoShop.Domain.Tests/ArchitectureTests.cs`, `tools/verify.ps1`, `.vscode/settings.json`; список art/ui и изображения button_play.png/button_settings.png/create_button.png/settings_panel.png/logo.png; локальная GodotSharp.xml для API перевода; новые файлы write-set; git status/diff. Диагностика координатора перед пакетом перечислена здесь.
Write-set: `ORCHESTRATION_PLAN.md`, `project.godot`, `scripts/Localization/GameLocalization.cs[.uid]`, `localization/en.tres`, `scripts/MainMenu.cs`, `scripts/TradeScene.cs` (только представление текстов), `scripts/UI/InventoryUI.cs`, `tests/ProjectTest.cs`, `README.md`. Владелец плана `/root`; активных пересечений нет.
Out of scope: source/, экономика/RNG/правила торговли, save schema, переписывание сохранений при смене языка, изменение пользовательских export/scene/font, публикация, перевод логотипа и любых кнопок-картинок (прямое уточнение пользователя).
Приёмка: кнопки «Русский» / «English» в настройках главного меню, мгновенное переключение и выделение текущего языка; русский по умолчанию, выбор сохраняется между запусками отдельно от игры. Переведены статические и динамические тексты игры, меню паузы, item tooltip/редкости/статусы; сохранения совместимы, gameplay не меняется. Кнопки-картинки сохраняются без изменений: их перевод отложен пользователем. Regression проверяет оба языка, persistence/fallback, переводы HUD/товаров/офферов/старых статусов, GUI переключение и переходы меню/игра, геометрию 1280×720 и 960×540. Проверки: полный tools/verify.ps1 и git diff --check.


```text
Task: HF-27
Recommended status: REVIEW
Changed: project.godot; scripts/Localization/GameLocalization.cs[.uid]; localization/en.tres; scripts/MainMenu.cs; scripts/TradeScene.cs; scripts/UI/InventoryUI.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, build, Godot import и вся integration-регрессия); git diff --check -> PASS.
Acceptance: добавлены toggle-кнопки Русский/English в настройках главного меню, мгновенный перевод и отражение выбора. Autoload регистрирует Godot Translation resource; русский — default/fallback, язык хранится отдельно в user://settings.cfg, другие секции настроек сохраняются. Переведены статические scene/UI строки, pause, rarity, HUD/offer, имена и подсказки предметов, сообщения прогресса. Старые русские статусы, включая сумму недостающей квоты, переводятся только в presentation: save schema и данные не меняются. При смене locale обновляются динамические подписи и текущая подсказка. Реальные GUI-click tests подтверждают оба языка и размещение controls при 1280×720/960×540, persistence/reload/recreated menu; проверяются placeholder parity всего каталога, перевод item/offer/статуса после reload, отсутствие изменения игрового состояния и записи save при переключении. Тесты восстанавливают исходный settings.cfg byte-for-byte. По уточнению пользователя все кнопки-картинки и логотип оставлены без изменений.
Read-set exception: диагностика координатора перед CP, перечисленная в read-set; source/ не читался. Verify log записан во временную папку ОС.
Risks/blocker: ручной visual playtest не выполнялся; NU1900 из-за недоступного vulnerability feed NuGet, ошибок компиляции нет. Перевод картинок отложен пользователем. Пользовательские export/scene/font изменения сохранены.
Next: проверить переключение языка в главном меню игрового сеанса.
```

Координатор подтвердил HF-27 2026-11-06 по финальному verify (exit 0), localization regression и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-26 — публикация свободной витрины и выхода в меню

Статус: `DONE`; координатор/исполнитель `/root`; HF-25 = DONE; `CP-HF-26`, READY проверен 2026-11-06. Commit/push явно разрешён пользователем.

#### CP-HF-26

Read-set: текущий фронт плана, git status/diff/log/remotes/show, verify evidence HF-25.
Write-set: `ORCHESTRATION_PLAN.md`, Git index/commit и origin/main для публикации `scripts/TradeScene.cs`, `scripts/UI/PauseMenu.cs`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Владелец плана и index `/root`; пересечений нет.
Out of scope: новые продуктовые изменения, пользовательские export_presets.cfg/scenes/trade_scene.tscn/MyFont_Completed.ttf[.import], source/, force push.
Приёмка: финальный verify HF-25 PASS; staged diff-check PASS; проверенный commit опубликован в origin/main без force; прочие локальные изменения сохранены.


```text
Task: HF-26
Recommended status: REVIEW
Changed: опубликованы ORCHESTRATION_PLAN.md; scripts/TradeScene.cs; scripts/UI/PauseMenu.cs; tests/ProjectTest.cs.
Checks: git fetch origin -> PASS; HEAD...origin/main перед commit = 0/0; git diff --cached --check -> PASS; git push origin main -> PASS (exit 0).
Acceptance: edb5068 опубликован в origin/main без force. Product-код после финального verify HF-25 не менялся. Пользовательские export_presets.cfg, scenes/trade_scene.tscn и исходный MyFont_Completed.ttf[.import] сохранены локально.
Read-set exception: нет.
Risks/blocker: нет.
Next: продолжить работу по запросу пользователя.
```

Координатор подтвердил HF-26 2026-11-06 по успешному push: REVIEW → DONE; evidence публикуется отдельным документационным коммитом.

### HF-25 — свободная витрина во время сделки и выход в меню

Статус: `DONE`; координатор/исполнитель `/root`; HF-23/HF-24 = DONE; `CP-HF-25`, READY проверен 2026-11-06; исполнитель назначен после проверки пакета.

#### CP-HF-25

Read-set: план, AGENTS.md из запроса, `scripts/TradeScene.cs`, `scripts/UI/PauseMenu.cs`, `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `scripts/MainMenu.cs`, `tests/ProjectTest.cs`, `scenes/trade_scene.tscn`, `scenes/main_menu.tscn`, `tools/verify.ps1`, `.vscode/settings.json`; git status/diff. Диагностика этих файлов до пакета выполнена координатором; source/ не читается.
Write-set: `scripts/TradeScene.cs` (удаление резервирования витрины и согласованность завершения дня), `scripts/UI/PauseMenu.cs`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Владелец плана `/root`; активных пересечений нет.
Out of scope: новая доменная логика в TradeScene, извлечение домена, save schema, экономика/RNG, сцены/арт, source/, публикация и пользовательские изменения export/scene/font.
Приёмка: начало сделки немедленно освобождает слот для обычного размещения, drag/drop и Shift; повторное заполнение и продажа не теряют и не дублируют товары, в том числе после reload. Состояния «НА СДЕЛКЕ» у витрины нет. В паузе доступна кнопка «В главное меню»: реальный переход, снятие паузы, повторный вход с сохранённым прогрессом. Завершение дня согласуется с ответом пользователя на обнаруженный возврат активного товара. Проверки: regression tests в `tests/ProjectTest.cs`, полный `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1`, git diff --check.


```text
Task: HF-25
Recommended status: REVIEW
Changed: scripts/TradeScene.cs; scripts/UI/PauseMenu.cs; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, build, Godot import/integration); git diff --check -> PASS.
Acceptance: резервирование и текст «НА СДЕЛКЕ» удалены; слот сразу свободен и принимает обычное размещение, drag/drop и Shift-перенос. Regression проверяет заполнение всех шести слотов во время сделки, refill исходного слота, reload с двумя отдельными товарами (в сделке и в исходном слоте), однократное начисление продажи и сохранность нового товара. Удалён возврат активного товара при EndDay; завершение дня блокируется до продажи, кнопка содержит пояснение. Отправлен optional-вопрос об этом дополнительном сценарии, ответа к реализации не поступило; применён предложенный вариант в соответствии с отсутствием возврата из запроса. Кнопка «В главное меню» проверена реальным GUI click: смена сцены, удаление игровой сцены, снятие паузы, работоспособность настроек и повторный вход с сохранённым прогрессом.
Read-set exception: точечная диагностика координатора перед CP, перечисленная в read-set; source/ не читался. Временный hf25-verify.log использовался только для вывода проверки и удалён.
Risks/blocker: ручной visual playtest не выполнялся; NU1900 из-за недоступного vulnerability feed NuGet, ошибок компиляции нет. Пользовательские export/scene/font файлы сохранены.
Next: проверить обновлённое меню и торговый цикл в игровом сеансе.
```

Координатор подтвердил HF-25 2026-11-06 по финальному verify (exit 0), regression и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-24 — публикация рефакторинга UI и камеры

Статус: `DONE`; координатор/исполнитель `/root`; HF-22/HF-23 = DONE; `CP-HF-24`, READY проверен 2026-11-06. Commit/push явно разрешён пользователем.

#### CP-HF-24

Read-set: этот фронт плана, git status/diff/log/remotes/show; файлы HF-22/HF-23 и их verify evidence.
Write-set: `ORCHESTRATION_PLAN.md`, Git index/commit и origin/main для публикации файлов HF-22/HF-23: `README.md`, `scripts/Camera/WorldCamera.cs[.uid]`, `scripts/TradeScene.cs`, `scripts/UI/InventoryUI.cs`, `scripts/UI/ItemInfoPopup.cs[.uid]`, `scripts/UI/PopupPlacement.cs[.uid]`, `scripts/UI/RarityPanel.cs`, `scripts/UI/ZoomResponsiveUi.cs`, `scripts/UI/PauseMenu.cs`, `tests/ProjectTest.cs`, только camera script/BoundsSource hunks `scenes/trade_scene.tscn`. Владелец плана и index `/root`; пересечений нет.
Out of scope: новые продуктовые изменения, export_presets.cfg, пользовательские serialization/layout hunks trade_scene, исходный MyFont_Completed.ttf[.import], force push, source/.
Приёмка: финальный verify HF-23 PASS, staged diff-check PASS, commit опубликован в origin/main без force; чужие изменения сохранены локально.

```text
Task: HF-24
Recommended status: REVIEW
Changed: опубликованы 15 файлов HF-22/HF-23, перечисленных в write-set.
Checks: git fetch origin -> PASS; HEAD...origin/main перед commit = 0/0; git diff --cached --check -> PASS; git push origin main -> PASS (exit 0).
Acceptance: d4735c7 опубликован в origin/main без force. В trade_scene закоммичены только camera script/BoundsSource hunks; пользовательские serialization/layout изменения сцены, export и исходные font files сохранены локально. Product-код после финального verify HF-23 не менялся.
Read-set exception: нет.
Risks/blocker: нет.
Next: продолжить работу по запросу пользователя.
```

Координатор подтвердил HF-24 2026-11-06 по успешному push: REVIEW → DONE; evidence публикуется отдельным документационным коммитом.

### HF-23 — извлечь управление камерой из TradeScene

Статус: `DONE`; координатор/исполнитель `/root`; HF-22 = DONE; `CP-HF-23`, READY проверен 2026-11-06.

#### CP-HF-23

Read-set: текущий фронт плана, AGENTS.md из запроса, `scripts/TradeScene.cs`, `scripts/UI/ZoomResponsiveUi.cs`, `scripts/UI/RarityPanel.cs`, `scripts/UI/PauseMenu.cs`, `scenes/trade_scene.tscn`, `tests/ProjectTest.cs`, `README.md`, ранее прочитанные verify/settings; новые файлы write-set; git status/diff.
Write-set: `scripts/Camera/WorldCamera.cs[.uid]`, `scripts/TradeScene.cs` (извлечение камеры и удаление её API), `scripts/UI/PauseMenu.cs` (удаление camera-coupling), `scenes/trade_scene.tscn` (script/BoundsSource существующего Camera2D), `tests/ProjectTest.cs`, `README.md`, `ORCHESTRATION_PLAN.md`. Владелец сцены и плана `/root`; пересекающиеся задачи не исполняются. Пользовательские изменения сцены сохраняются.
Out of scope: изменение camera tuning/UX/порогов UI, доменная логика торговли, экономика/save/RNG, source/, остальные локальные изменения, публикация.
Приёмка: WorldCamera : Camera2D самостоятельно владеет вводом/drag/zoom/viewport lifecycle и экспортируемой ссылкой на RoomLayout, без зависимости от TradeScene; в TradeScene нет camera fields/math/resize/input-pan. Старый Camera2D node path и все tuning constants сохранены. Pause через notification прекращает drag без связи меню с камерой. Проверки: существующие camera zoom/bounds/resize/layout transform tests перенесены на самостоятельный компонент без ослабления; новая standalone-scene camera проверка, zoom anchor/pause-resume/GUI drag regression, полный tools/verify.ps1, git diff --check. Baseline HF-22 verify PASS непосредственно перед задачей.
Read-set exception: диагностика координатора перечисленных файлов перед CP; source/ не читался.

```text
Task: HF-23
Recommended status: REVIEW
Changed: scripts/Camera/WorldCamera.cs[.uid]; scripts/TradeScene.cs; scripts/UI/PauseMenu.cs; scenes/trade_scene.tscn; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import/integration); git diff --check -> PASS; rg Camera/camera/Zoom/zoom/ClampCamera/CancelCameraDrag в TradeScene/PauseMenu -> совпадений нет.
Acceptance: скрипт WorldCamera : Camera2D подключён к прежнему Camera2D node с экспортируемой ссылкой на RoomLayout. Весь camera state/math/мышь/viewport subscription удалён из TradeScene; в его _Process остался TickBuyers, в _Input — игровая клавиша F. WorldCamera сам сбрасывает drag при pause и отписывается от SizeChanged при удалении. Максимум 1.8, шаг 1.12, smoothness 12, zoom к курсору, bounds по transform/rect схемы и width-based minimum сохранены. Старые проверки камеры перенаправлены на компонент; static UI tests останавливают именно camera processing, игровая сцена независимо управляет buyers. Новый тест создаёт камеру вообще без TradeScene, проверяет native wheel anchor, GUI isolation, world pan, pause freeze/resume и resize после удаления. Все прежние drag/Shift/hover/tooltip/save/checkpoint/экономика и HF-22 regression проходят.
Read-set exception: диагностика координатора перед CP, перечисленная выше.
Risks/blocker: ручной visual playtest не выполнялся; NU1900 из-за недоступного vulnerability feed NuGet, ошибок компиляции нет. Пользовательские изменения сцены и незавершённые к публикации изменения HF-22 сохранены.
Next: проверить привычное управление камерой в игровом сеансе.
```

Координатор подтвердил HF-23 2026-11-06 по финальному verify (exit 0), standalone/regression camera tests и diff-check: REVIEW → DONE; владение сценой освобождено, основной фронт и ворота без изменений.

### HF-22 — безопасный рефакторинг и оптимизация UI

Статус: `DONE`; координатор/исполнитель `/root`; HF-15–HF-20 = DONE; `CP-HF-22`, READY проверен 2026-11-06.

#### CP-HF-22

Read-set: этот план, AGENTS.md из запроса, `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `scripts/UI/RarityPanel.cs`, `scripts/UI/ZoomResponsiveUi.cs`, `scripts/UI/ProgressPanel.cs`, `scripts/TradeScene.cs` (каталог/public snapshots), `tests/ProjectTest.cs`, `tests/ChikagoShop.Domain.Tests/ArchitectureTests.cs`, `ChikagoShop.csproj`, `README.md`, `tools/verify.ps1`, `.vscode/settings.json`; точечная инвентаризация scripts/src/tests без source/; новые файлы write-set.
Write-set: `scripts/UI/InventoryUI.cs`, `scripts/UI/ItemInfoPopup.cs[.uid]`, `scripts/UI/PopupPlacement.cs[.uid]`, `scripts/UI/RarityPanel.cs`, `scripts/UI/ZoomResponsiveUi.cs`, `tests/ProjectTest.cs`, `README.md`, `ORCHESTRATION_PLAN.md`. Владелец плана `/root`; пересекающиеся задачи не исполняются. Tab-форматирование пользовательского RarityPanel сохраняется.
Out of scope: новая функциональность, монолитная переработка TradeScene/domain/save/RNG, экономика, локализация, сцены/арт, source/, публикация и чужие изменения вне write-set.
Приёмка: tooltip presentation вынесен из InventoryUI; placement math вынесен в самостоятельный helper с прежним scoring; RarityPanel не ищет HUD nodes в каждом candidate/frame и не создаёт candidate/HUD массивы каждый кадр; zoom обновляет свойства только при zoom/layout change, включая resize при постоянном zoom; Shift-перенос делает один showcase snapshot на операцию. Baseline и финальный verify зелёные; сохранены drag/cancel/Shift, tooltip lifecycle/позиция, hover, placement near edges/HUD, save/checkpoint/экономика. Дополнительные regression: popup resize/scroll/pause, popup movement с изменением HUD, constant-zoom resize. Проверки: полный tools/verify.ps1 до/после, git diff --check.
Read-set exception: координатор прочитал перечисленные файлы до CP для выбора минимального безопасного среза; source/ не читался.

```text
Task: HF-22
Recommended status: REVIEW
Changed: scripts/UI/InventoryUI.cs; scripts/UI/ItemInfoPopup.cs[.uid]; scripts/UI/PopupPlacement.cs[.uid]; scripts/UI/RarityPanel.cs; scripts/UI/ZoomResponsiveUi.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: baseline и финальный powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import и вся integration-регрессия); git diff --check -> PASS. Измерение GC.GetAllocatedBytesForCurrentThread: 1000 warmed-up вызовов PopupPlacement.FindPosition -> 0 managed bytes.
Acceptance: ItemInfoPopup владеет стилем/позиционированием/lifecycle и отключает _Process, пока скрыт; InventoryUI отвечает за ячейки/выбор/перенос. PopupPlacement отделяет геометрию от scene lookup, порядок candidates/scoring сохранён. RarityPanel кеширует 6 HUD references после полной инициализации сцены вместо 36 GetNode на каждый visible frame; obstacle bounds читаются один раз за frame; candidates/obstacles используют stackalloc. Скрытая rarity table не выполняет _Process. ZoomResponsiveUi пропускает повторные записи при неизменном zoom/layout; Resize сигналов достаточно для обновления центров и зазора ценника при постоянном zoom. Shift-перенос получает один snapshot вместо snapshot для каждого проверяемого свободного/занятого слота. Новые тесты подтверждают constant-zoom resize, следование tooltip при scroll/resize, немедленное открытие и выключение при pause, актуальность cached HUD geometry при перемещении препятствия, allocation-free placement. Все прежние drag/Shift/tooltip/hover/save/checkpoint/trade/economy тесты проходят без ослабления.
Read-set exception: диагностика координатора перед CP, перечисленная выше.
Risks/blocker: FPS/CPU всей игры не профилировались, ручной visual playtest не выполнялся. NU1900 из-за недоступного vulnerability feed NuGet, компиляция без ошибок. Экономика/RNG/save schema/TradeScene не менялись; пользовательские export/trade_scene/font originals и tab-форматирование RarityPanel сохранены.
Next: продолжать извлечение TradeScene только отдельной задачей основного плана с проверками соответствующего доменного сценария.
```

Координатор подтвердил HF-22 2026-11-06 по baseline/final verify (exit 0), новым regression и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-21 — публикация drag/drop, шрифта и hover-исправлений

Статус: `DONE`; координатор/исполнитель `/root`; HF-15–HF-20 = DONE; `CP-HF-21`, READY проверен 2026-11-06. Commit/push явно разрешён пользователем.

#### CP-HF-21

Read-set: этот фронт плана, git status/diff/log/remotes; проверенные файлы HF-15–HF-20; diagnostic diff пользовательских export_presets.cfg/scenes/trade_scene.tscn/scripts/UI/RarityPanel.cs.
Write-set: `ORCHESTRATION_PLAN.md`, Git index/commit и origin/main для публикации `ORCHESTRATION_PLAN.md`, `art/fonts/game.ttf`, `scenes/main_menu.tscn`, `scripts/TradeScene.cs`, `scripts/UI/InventoryUI.cs`, `scripts/UI/PauseMenu.cs`, `scripts/UI/SlotUI.cs`, `tests/ProjectTest.cs`. Владелец плана `/root`; пересечений нет.
Out of scope: пользовательские изменения export/trade_scene/formatting RarityPanel, дубли исходного MyFont_Completed.ttf[.import], force push, source/.
Приёмка: финальный verify HF-20 PASS, diff-check PASS, проверенный commit опубликован в origin/main без force; остальные локальные изменения сохранены.

```text
Task: HF-21
Recommended status: REVIEW
Changed: опубликованы восемь файлов write-set HF-21.
Checks: git fetch origin -> PASS; HEAD...origin/main перед commit = 0/0; git diff --cached --check -> PASS; git push origin main -> PASS (exit 0).
Acceptance: cb83985 опубликован в origin/main без force; drag/drop, Shift-перенос, новый runtime font, контраст fullscreen, hover-взаимоисключение и regression tests отправлены. Product-код после финального verify HF-20 не менялся. Пользовательские изменения вне scope и исходный MyFont_Completed.ttf[.import] сохранены локально.
Read-set exception: нет.
Risks/blocker: нет.
Next: продолжить работу по запросу пользователя.
```

Координатор подтвердил HF-21 2026-11-06 по успешному push: REVIEW → DONE; evidence публикуется отдельным документационным коммитом.

### HF-20 — скрывать ценник при наведении на вопрос

Статус: `DONE`; координатор/исполнитель `/root`; HF-13 = DONE; `CP-HF-20`, READY проверен 2026-11-06.

#### CP-HF-20

Read-set: текущий фронт плана, AGENTS.md из запроса, `scripts/TradeScene.cs` (связи UI hover), `scripts/UI/RarityPanel.cs`, `tests/ProjectTest.cs` (hover regression), ранее прочитанные verify/settings; локальная XML-документация GodotSharp BaseButton.IsHovered; git status/diff.
Write-set: `scripts/TradeScene.cs` (только hover-связи UI), `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Владелец плана `/root`; пересечений с активными задачами нет.
Out of scope: доменная логика, экономика, save schema, изменение размещения таблицы, source/, прочие локальные изменения.
Приёмка: в обеих мастерских при наведении на ? видна таблица и скрыт ценник; при переходе обратно на кнопку ценник возвращается, таблица скрывается; при выходе наружу скрыты обе подсказки, включая прямой вход/выход через ?. Проверки: реальные GUI motion-события на двух viewport, существующий popup geometry suite, полный tools/verify.ps1, git diff --check.

```text
Task: HF-20
Recommended status: REVIEW
Changed: scripts/TradeScene.cs (только UI hover-связи); tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import/integration); git diff --check -> PASS.
Acceptance: оба workshop hover-сценария проверены настоящими событиями мыши при viewport 1280×720 и 960×540: кнопка → ? скрывает ценник и показывает rarity; ? → кнопка возвращает ценник; кнопка → наружу и прямой ? → наружу скрывают обе подсказки. Прямой вход на ? не оставляет ценник видимым. Адаптивная геометрия таблицы и прежние регрессии проходят. Доменная логика TradeScene не менялась.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся; прочие локальные изменения сохранены.
Next: проверить hover в игровом сеансе.
```

Координатор подтвердил HF-20 2026-11-06 по verify (exit 0) и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-19 — центр drag preview и пустая исходная ячейка

Статус: `DONE`; координатор/исполнитель `/root`; HF-18 = DONE; `CP-HF-19`, READY проверен 2026-11-06.

#### CP-HF-19

Read-set: текущий фронт плана, AGENTS.md из запроса, `scripts/UI/SlotUI.cs`, drag/Shift tests в `tests/ProjectTest.cs`, ранее прочитанные verify/settings; git status/diff.
Write-set: `scripts/UI/SlotUI.cs`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Владелец плана `/root`; пересечений нет.
Out of scope: доменная логика, save schema, source/, остальные локальные изменения.
Приёмка: центр картинки drag совпадает с курсором во время движения при разных zoom; исходная ячейка без картинки и выделения до конца drag; отмена возвращает текущую картинку, успешный перенос не оставляет скрытой следующую. Размер preview 50×50 и Shift-перенос сохранены. Проверки: native GUI geometry/lifecycle regression, полный tools/verify.ps1, git diff --check.

```text
Task: HF-19
Recommended status: REVIEW
Changed: scripts/UI/SlotUI.cs; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import/integration); git diff --check -> PASS.
Acceptance: отрицательное смещение перенесено с preview root на artwork: движок позиционирует root в курсор, центр картинки совпадает с ним при начале и после движения. Source icon скрыт, SelfModulate белый; NotificationDragEnd восстанавливает текущую картинку и актуальное выделение после отмены/успешного drop. Native GUI tests проверяют центрирование, пустую ячейку, восстановление картинки, обе текстуры, zoom 0.25/0.7/1.8, размер 50×50 и прежние Shift/drop-регрессии.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся; остальные локальные изменения сохранены.
Next: проверить жест в игровом сеансе.
```

Координатор подтвердил HF-19 2026-11-06 по verify (exit 0) и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-18 — ограничить масштаб drag preview

Статус: `DONE`; координатор/исполнитель `/root`; HF-16 = DONE; `CP-HF-18`, READY проверен 2026-11-06.

#### CP-HF-18

Read-set: текущий фронт плана, AGENTS.md из запроса, `scripts/UI/SlotUI.cs`, `scripts/UI/InventoryUI.cs`, `scripts/CounterWorld.cs`, `scripts/TradeScene.cs` (RefreshItem/Showcases), `scenes/trade_scene.tscn` (Item sprites), `scenes/customer.tscn` (поиск Scale), `tests/ProjectTest.cs`, ранее прочитанные verify/settings; git status/diff.
Write-set: `scripts/UI/SlotUI.cs`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Владелец плана `/root`; пересекающиеся задачи не исполняются.
Out of scope: экономика, save schema, изменение камеры/мировых объектов, source/, прочие локальные изменения.
Приёмка: drag preview ограничен 50×50 экранными пикселями сразу и после кадра layout, независимо от zoom/размера исходной текстуры; после drop/cancel preview удаляется, исходные масштабы inventory/showcase/trade sprites не меняются. Проверки: regression preview geometry на настоящем GUI drag для обеих atelier textures и разных zoom, полный tools/verify.ps1, git diff --check.
Read-set exception: точечная диагностика координатором перечисленных файлов до CP; source/ не читался.

```text
Task: HF-18
Recommended status: REVIEW
Changed: scripts/UI/SlotUI.cs; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: baseline geometry regression -> FAIL (drag preview 1448×1086 вместо 50×50); после исправления powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import/integration); git diff --check -> PASS.
Acceptance: создание TextureRect с Texture до ExpandMode раздувало Size до исходного minimum texture. Preview теперь состоит из фиксированного Control 50×50 и дочернего TextureRect с IgnoreSize до назначения текстуры. Native GUI drag проверяет размеры root/artwork сразу и после layout для hat/gloves при zoom 0.25/1.8 и прежних сценариях 0.7; drop/cancel удаляют preview. Проверены неизменность Scale всех world Sprite2D и размеров showcase icon, размещение, отмена, отказные случаи и Shift-перенос.
Read-set exception: первичная диагностика координатора, перечисленная выше.
Risks/blocker: ручной visual playtest не выполнялся; NU1900 из-за недоступности vulnerability feed NuGet. Прочие локальные изменения сохранены.
Next: перезапустить игровой сеанс для загрузки исправленного preview.
```

Координатор подтвердил HF-18 2026-11-06 по финальному verify (exit 0) и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-17 — заменить игровой шрифт

Статус: `DONE`; координатор/исполнитель `/root`; HF-16 = DONE; `CP-HF-17`, READY проверен 2026-11-06.

#### CP-HF-17

Read-set: текущий фронт плана, AGENTS.md из запроса, `MyFont_Completed.ttf[.import]`, `art/fonts/game.ttf[.import]`, `themes/game_theme.tres`, `scenes/main_menu.tscn`, `scenes/UI/ShowcaseSlot.tscn`, `tests/ProjectTest.cs`, `.gitignore`, ранее прочитанные verify/settings; точечный поиск font references в project.godot/themes/scenes/scripts/tests/README.md; git status/diff.
Write-set: `art/fonts/game.ttf`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Единственный владелец плана `/root`; пересечений с активными задачами нет.
Out of scope: локализация/переводы, размеры/цвета UI, перемещение пользовательского исходного шрифта, source/.
Приёмка: канонический art/fonts/game.ttf побайтово совпадает с MyFont_Completed.ttf; все три existing references сохраняют корректный UID и используют новый файл; default theme поддерживает латинские/русские буквы, GUI-регрессии зелёные. Проверки: SHA256 обоих файлов, font glyph integration test, полный tools/verify.ps1, git diff --check.

```text
Task: HF-17
Recommended status: REVIEW
Changed: art/fonts/game.ttf; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: SHA256 source/runtime -> MATCH (C453F9141C92FF051FCF893FBABB7D35FDE97D57B1B2681B6755B2D0BAAF3FAF); powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import/integration); git diff --check -> PASS.
Acceptance: runtime font заменён байтами MyFont_Completed.ttf, UID и все ссылки на game.ttf сохранены; шрифт общей темы и explicit menu font совпадают, все заглавные/строчные латинские и русские буквы включая Ё/ё присутствуют. GUI-регрессии размеров/подсказок/drag-and-drop проходят. Переводы и локализация не добавлялись.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся; исходный пользовательский MyFont_Completed.ttf[.import] сохранён на месте.
Next: локализация по отдельному запросу пользователя.
```

Координатор подтвердил HF-17 2026-11-06 по verify (exit 0), SHA256 и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-16 — drag-and-drop, Shift-перенос и контраст настроек

Статус: `DONE`; координатор/исполнитель `/root`; HF-15 = DONE; `CP-HF-16`, READY проверен 2026-11-06.

#### CP-HF-16

Read-set: этот план, AGENTS.md из запроса, `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `scripts/UI/PauseMenu.cs`, `scripts/MainMenu.cs`, `scripts/TradeScene.cs` (существующий placement API/инициализация/disabled states), `scenes/UI/ShowcaseSlot.tscn`, `scenes/main_menu.tscn`, `tests/ProjectTest.cs`, ранее прочитанные verify/settings; официальная документация Godot Control drag/drop; git status/diff.
Write-set: `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `scripts/UI/PauseMenu.cs`, `scenes/main_menu.tscn`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Владелец сцены и плана `/root`; пересекающиеся задачи не исполняются.
Out of scope: новая доменная логика TradeScene, возврат с витрины, экономика/save schema, source/, чужие изменения вне write-set.
Приёмка: drag предмета на свободную витрину с иконкой-превью; отмена/занятый/reserved слот не теряют товар; Shift+клик переносит в первый свободный слот среди шести, полные витрины без потерь. Пауза/terminal states блокируют перенос, устаревший drag payload отклоняется. Старый выбор+клик сохранён. Fullscreen-текст контрастен во всех состояниях main menu и pause. Проверки: GUI drag/Shift, отказные случаи/повторный drop, theme checks, полный tools/verify.ps1, git diff --check.

```text
Task: HF-16
Recommended status: REVIEW
Changed: scripts/UI/InventoryUI.cs; scripts/UI/SlotUI.cs; scripts/UI/PauseMenu.cs; scenes/main_menu.tscn; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (0 ошибок компиляции, 19 domain tests, Godot import/integration); git diff --check -> PASS.
Acceptance: native GUI drag через viewport переносит предмет, вне витрины отменяется, занятый/reserved слот отклоняется, replay старого payload не переносит следующий предмет. Реальные Shift+клики заполняют обе комнаты, пропуская reserved; при заполнении товар остаётся в inventory. Пауза и поражение отклоняют drag и quick move. Прежние GUI выбор+размещение/продажа и tooltip regression проходят. Во всех normal/hover/pressed/hover_pressed/disabled состояниях fullscreen имеет светлую непрозрачную подложку и тёмный текст, включая focus и оба toggle-state; проверено в main menu и pause. TradeScene не изменён: адаптер использует существующий placement API.
Read-set exception: дополнительно прочитаны локальная XML-документация GodotSharp Viewport drag API и официальный viewport.cpp для диагностики синтетических drag-событий.
Risks/blocker: ручной visual playtest не выполнялся; NU1900 из-за недоступности vulnerability feed NuGet. Новые пользовательские MyFont_Completed.ttf[.import] и прочие изменения вне write-set сохранены.
Next: проверить drag и Shift+клик в игровом сеансе.
```

Координатор подтвердил HF-16 2026-11-06 по финальному verify (exit 0) и diff-check: REVIEW → DONE; владение сценой освобождено, основной фронт и ворота без изменений.

### HF-15 — восстановить сборку подсказок inventory

Статус: `DONE`; координатор/исполнитель `/root`; зависимость HF-13 = DONE; `CP-HF-15`, READY проверен 2026-11-06.

#### CP-HF-15

Read-set: текущий фронт плана, AGENTS.md из запроса, `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `tests/ProjectTest.cs`, `ChikagoShop.csproj`, ранее прочитанные `tools/verify.ps1`, `.vscode/settings.json`; git status/diff/show для этих файлов и diagnostic diff `scripts/UI/RarityPanel.cs`.
Write-set: `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `ORCHESTRATION_PLAN.md`. Единственный владелец плана `/root`; пересекающиеся задачи не исполняются.
Out of scope: экономика, save schema, прочие локальные изменения, source/.
Приёмка: устранены все 11 воспроизведённых CS1061 (ItemPopup/VisibleItemInfo/ItemInfo), восстановлена реализация мгновенной подсказки HF-13 с сохранением текущего tab-форматирования. Существующие regression tests HF-13 остаются без ослабления. Проверки: полный tools/verify.ps1, git diff --check, git diff -w относительно HEAD не показывает поведенческих изменений в двух восстановленных файлах.
Read-set exception: точечная диагностика координатором перечисленных файлов до CP; source/ не читался.

```text
Task: HF-15
Recommended status: REVIEW
Changed: scripts/UI/InventoryUI.cs; scripts/UI/SlotUI.cs; ORCHESTRATION_PLAN.md
Checks: baseline dotnet build ChikagoShop.csproj --no-restore -> FAIL (11 CS1061, воспроизведено); после исправления powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (0 ошибок сборки, 19 domain tests, Godot import и integration); git diff --check -> PASS; git diff -w по двум UI-файлам -> пусто относительно HEAD.
Acceptance: восстановлены ItemPopup/VisibleItemInfo/ItemInfo и весь код мгновенной подсказки HF-13; GUI-регрессии проходят без изменений тестов. Сохранены tab-отступы; исправлены хвостовые пробелы только в write-set. Прочие локальные изменения не затронуты.
Read-set exception: диагностика координатора до CP, перечисленная выше.
Risks/blocker: GUI build callback редактора отдельно не запускался; проверена сборка той же csproj и headless Godot. Остаётся NU1900 из-за недоступности vulnerability feed NuGet; ошибок компиляции нет.
Next: повторно запустить игру в открытом редакторе Godot.
```

Координатор подтвердил HF-15 2026-11-06 по финальному verify (exit 0) и diff-check: REVIEW → DONE; основной фронт и ворота без изменений.

### HF-14 — публикация исправлений UI

Статус: `DONE`; координатор/исполнитель `/root`; зависимости HF-11/HF-12/HF-13 = DONE; `CP-HF-14`, READY проверен 2026-11-06. Commit/push явно разрешён пользователем.

#### CP-HF-14

Read-set: этот план, git status/diff/log/remotes; ранее проверенные файлы HF-11–HF-13; diff пользовательских `export_presets.cfg`, `scenes/trade_scene.tscn`, `scripts/UI/PauseMenu.cs` для определения границ публикации.
Write-set: `ORCHESTRATION_PLAN.md`, Git index/commit и origin/main для публикации только `ORCHESTRATION_PLAN.md`, `scenes/UI/RarityPanel.tscn`, `scenes/UI/ShowcaseSlot.tscn`, `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `scripts/UI/ZoomResponsiveUi.cs`, `tests/ProjectTest.cs`. Владелец плана `/root`; пересечений с активными задачами нет.
Out of scope: включение пользовательских изменений export/pause/trade_scene, force push, source/.
Приёмка: проверки HF-13 PASS, diff-check PASS, commit отправлен в origin/main без force; пользовательские изменения остаются в рабочем дереве.

```text
Task: HF-14
Recommended status: REVIEW
Changed: опубликованы семь файлов HF-11–HF-13, перечисленных в write-set.
Checks: git fetch origin -> PASS; HEAD...origin/main перед commit = 0/0; git diff --cached --check -> PASS; git push origin main -> PASS (exit 0).
Acceptance: commit c10e21e отправлен в origin/main без force; отдельные пользовательские изменения сохранены вне коммита. Полный verify HF-13 ранее PASS, product-код после него не менялся.
Read-set exception: нет.
Risks/blocker: нет.
Next: продолжить основной фронт по запросу пользователя.
```

Координатор подтвердил HF-14 2026-11-06 по успешному push: REVIEW → DONE. Эта запись публикуется отдельным документационным коммитом.

### HF-13 — мгновенные подсказки и отступы UI

Статус: `DONE`; координатор/исполнитель `/root`; зависимости HF-08/HF-10/HF-12 = DONE; `CP-HF-13`, READY проверен 2026-11-06.

#### CP-HF-13

Read-set: этот план, AGENTS.md из запроса, `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `scripts/UI/ZoomResponsiveUi.cs`, `scripts/UI/RarityPanel.cs`, `scenes/UI/RarityPanel.tscn`, `scenes/UI/InventoryUI.tscn`, `scenes/UI/SlotUI.tscn`, `scenes/trade_scene.tscn`, `themes/game_theme.tres`, `tests/ProjectTest.cs`, ранее прочитанные `tools/verify.ps1`, `.vscode/settings.json`; git status/diff.
Write-set: `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `scripts/UI/ZoomResponsiveUi.cs`, `scripts/UI/RarityPanel.cs`, `scenes/UI/RarityPanel.tscn`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Единственный владелец сцены и плана `/root`; пересекающиеся VF-задачи не исполняются.
Out of scope: экономика, save schema, изменение арта, source/, пользовательские изменения.
Приёмка: информация inventory сразу при наведении, центрирована над ячейкой с зазором 8 px, скрывается при выходе и обновляется при смене предмета; ценник отделён от кнопки на 8 screen px при zoom; заголовки плавно проявляются при zoom 0.23–0.35; текст rarity table имеет безопасные отступы и раздельные колонки. Проверки: новые GUI/geometry regression, полный tools/verify.ps1, git diff --check.
Read-set exception: координатор точечно прочитал перечисленные UI-файлы перед CP для диагностики скриншотов; source/ не читался.

```text
Task: HF-13
Recommended status: REVIEW
Changed: scripts/UI/InventoryUI.cs; scripts/UI/SlotUI.cs; scripts/UI/ZoomResponsiveUi.cs; scenes/UI/RarityPanel.tscn; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import/integration); git diff --check -> PASS.
Acceptance: GUI mouse enter показывает custom popup синхронно без кадра ожидания; системная задержанная подсказка отключена. Проверены центрирование/зазор 8 px над ячейкой, скрытие при выходе, актуальность после создания/перемещения/reload. Ценники обеих мастерских сохраняют центр и экранный зазор 8 px при zoom 0.23/0.3/0.5/1/1.8. Все 13 элементов заголовков скрыты при 0.23, плавно проявлены при 0.29, полностью видны при 0.35. Обе таблицы rarity сохраняют внутренний отступ и расстояния колонок при 36 сочетаниях zoom/позиции/viewport 1280×720 и 960×540; адаптивное размещение и GUI hover сохранены.
Read-set exception: диагностика координатора перед CP, перечисленная выше.
Risks/blocker: ручной visual playtest не выполнялся. NU1900 из-за недоступного vulnerability feed NuGet, ошибок сборки и предупреждений компилятора нет. У края viewport item popup сдвигается по X для сохранения текста на экране.
Next: проверить внешний вид в новом игровом сеансе.
```

Координатор подтвердил HF-13 2026-11-06 по финальному verify (exit 0) и diff-check: REVIEW → DONE; владение сценой освобождено. Пользовательские изменения сохранены; основной фронт и ворота без изменений.

### HF-12 — убрать подсветку витрин при нажатии

Статус: `DONE`; координатор/исполнитель `/root`; зависимость HF-11 = DONE; `CP-HF-12`, READY проверен 2026-11-06.

#### CP-HF-12

Read-set: этот план, ранее прочитанные инструкции AGENTS.md, `scenes/UI/ShowcaseSlot.tscn`, `tests/ProjectTest.cs`, `tools/verify.ps1`, `.vscode/settings.json`; git status/diff.
Write-set: `scenes/UI/ShowcaseSlot.tscn`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Единственный владелец сцены и плана `/root`; пересекающиеся VF-задачи не исполняются.
Out of scope: inventory, торговая логика, пользовательские изменения, source/.
Приёмка: фон слотов прозрачен при нажатии, включая нажатие с наведением; клики размещения работают. Проверки: scene-contract всех шести слотов, полный `tools/verify.ps1`, `git diff --check`.

```text
Task: HF-12
Recommended status: REVIEW
Changed: scenes/UI/ShowcaseSlot.tscn; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS; git diff --check -> PASS.
Acceptance: pressed и hover_pressed прозрачны без рамок на всех шести слотах; GUI-размещение и торговля проходят, 19 domain tests зелёные.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся; NU1900 при проверке vulnerability feed NuGet, build без ошибок.
Next: перезапустить игровой сеанс.
```

Координатор подтвердил HF-12 2026-11-06 по verify (exit 0) и diff-check: REVIEW → DONE; владение сценой освобождено, основные ворота без изменений.

### HF-11 — убрать подсветку витрин при наведении

Статус: `DONE`; координатор/исполнитель `/root`; зависимости отсутствуют; `CP-HF-11`, READY проверен 2026-11-06.

#### CP-HF-11

Read-set: этот план, инструкции AGENTS.md из запроса, `scenes/UI/ShowcaseSlot.tscn`, `tests/ProjectTest.cs`, `tools/verify.ps1`, `.vscode/settings.json`; точечный поиск обработчиков в `scripts/TradeScene.cs`, `scripts/UI/SlotUI.cs`; git status/diff.
Write-set: `scenes/UI/ShowcaseSlot.tscn`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`. Единственный владелец сцены и плана `/root`; пересекающиеся VF-задачи не исполняются.
Out of scope: подсветка inventory, эффект нажатия, торговая логика, пользовательские изменения, source/.
Приёмка: при наведении все шесть витрин используют прозрачный фон без выделения; размещение товара работает. Проверки: scene-contract для hover всех шести слотов, существующие GUI-тесты размещения, полный `tools/verify.ps1`, `git diff --check`.
Read-set exception: точечная диагностика перечисленных файлов координатором до оформления CP; source/ не читался.

```text
Task: HF-11
Recommended status: REVIEW
Changed: scenes/UI/ShowcaseSlot.tscn; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import и integration); git diff --check -> PASS.
Acceptance: прозрачный hover без рамки проверен для всех шести витрин; существующие GUI-тесты размещения и торговли проходят.
Read-set exception: диагностика координатора до CP, перечисленная выше.
Risks/blocker: ручной visual playtest не выполнялся; build выдал NU1900 из-за недоступности vulnerability feed api.nuget.org, ошибок нет.
Next: перезапустить игровой сеанс для загрузки сцены.
```

Координатор подтвердил HF-11 2026-11-06 по verify (exit 0) и diff-check: REVIEW → DONE. Основной фронт и ворота не изменены; владение сценой освобождено. Пользовательские изменения export_presets.cfg, scenes/trade_scene.tscn и scripts/UI/PauseMenu.cs сохранены.

### HF-10 — раннее проявление заголовков и публикация UI

Статус: `DONE`; координатор/исполнитель `/root`; зависимости HF-08/HF-09 = DONE; `CP-HF-10`, READY проверен 2026-11-05.
Read-set: этот план, ранее прочитанный `AGENTS.md`, `scripts/UI/ZoomResponsiveUi.cs`, `tests/ProjectTest.cs`, `README.md`, ранее прочитанные `tools/verify.ps1`, `.vscode/settings.json`; diff/status накопленных HF-08/HF-09.
Write-set: `scripts/UI/ZoomResponsiveUi.cs`, `tests/ProjectTest.cs`, `README.md`, `ORCHESTRATION_PLAN.md`. Единственный владелец плана `/root`; пересекающиеся задачи не исполняются. Commit/push накопленных HF-08/HF-09/HF-10 в origin/main явно разрешён пользователем.
Out of scope: экономика, save schema, прочие пороги UI, source/.
Приёмка: заголовки вместе с подложками/описаниями плавно проявляются при zoom 0.3–0.5 вместо 0.4–0.6; тесты границ/промежуточной прозрачности и полный `tools/verify.ps1` зелёные; `git diff --check`; commit отправлен в origin/main без force.

```text
Task: HF-10
Recommended status: REVIEW
Changed: scripts/UI/ZoomResponsiveUi.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md; накопленные изменения HF-08/HF-09 включены в публикацию.
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (0 ошибок/предупреждений, 19 domain tests, import, все Godot integration); git diff --check -> PASS.
Acceptance: проверены скрытие при 0.3, промежуточная прозрачность при 0.4, полная видимость при 0.5 всех 13 элементов заголовков. Перед публикацией origin/main и HEAD совпадают.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся.
Next: выполнить разрешённый commit/push в origin/main.
```

Координатор подтвердил HF-10 2026-11-05: verify/diff-check PASS, commit `b22ac38` со всеми HF-08/HF-09/HF-10 успешно отправлен в origin/main без force (exit 0). REVIEW → DONE. Основной фронт и ворота не изменены.

### HF-09 — сворачиваемая компактная панель прогресса

Статус: `DONE`; координатор/исполнитель `/root`; зависимости отсутствуют; `CP-HF-09`, READY проверен 2026-11-05.
Read-set: этот план, ранее прочитанный `AGENTS.md`, `scenes/trade_scene.tscn`, `scripts/TradeScene.cs`, `scripts/UI/RarityPanel.cs`, `tests/ProjectTest.cs`, `README.md`, ранее прочитанные `tools/verify.ps1`, `.vscode/settings.json`; новые файлы write-set.
Write-set: `scripts/UI/ProgressPanel.cs`, `scripts/UI/ProgressPanel.cs.uid`, `scenes/trade_scene.tscn`, `scripts/UI/RarityPanel.cs`, `tests/ProjectTest.cs`, `README.md`, `ORCHESTRATION_PLAN.md`. Единственный владелец сцены и плана `/root`; пересекающиеся задачи не исполняются. Существующие изменения HF-08 сохраняются.
Out of scope: экономика, save schema, меню паузы, source/, сохранение состояния сворачивания между запусками.
Приёмка: `<` скрывает панель слева сверху, `>` возвращает её; кнопка остаётся доступной, игровой state не меняется. Высота раскрытой панели соответствует оставшемуся содержимому, включая длинный статус; нет прежнего пустого места от удалённых кнопок. Проверки: GUI toggle/repeat, размеры/вложенность контента при длинном статусе, resize, полный `tools/verify.ps1`, `git diff --check`.
Read-set exception: перечисленные UI-файлы открыты координатором для диагностики перед оформлением CP; source/ не читался.

```text
Task: HF-09
Recommended status: REVIEW
Changed: scripts/UI/ProgressPanel.cs[.uid]; scenes/trade_scene.tscn; scripts/UI/RarityPanel.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (0 ошибок/предупреждений, 19 domain tests, import, Godot integration); git diff --check -> PASS.
Acceptance: < скрывает ProgressPanel, > остаётся у левого края и восстанавливает панель; проверены три повторных цикла через реальные GUI-клики без изменения дня/счёта. Высота равна minimum содержимого, после EndDay остаётся только padding рамки; длинный статус расширяет высоту, короткий возвращает прежнюю. Обновление дня скрытой панели и работа стрелок после resize на 960×540 проверены. Tooltip учитывает стрелку при выборе позиции. Изменения HF-08 сохранены; полная прежняя регрессия проходит.
Read-set exception: диагностика координатора до CP, перечисленная выше; source/ не читался.
Risks/blocker: ручной visual playtest не выполнялся; состояние сворачивания между запусками не сохраняется (вне scope).
Next: проверить внешний вид раскрытой/свёрнутой панели в игровом сеансе.
```

Координатор подтвердил HF-09 по финальному verify (exit 0) 2026-11-05; REVIEW → DONE. Основной фронт и ворота не изменены; владение сценой освобождено.

### HF-08 — компактная таблица редкостей рядом с кнопкой

Статус: `DONE`; координатор/исполнитель `/root`; зависимости отсутствуют; пакет `CP-HF-08`, READY проверен 2026-11-05.
Read-set: этот план, ранее прочитанный `AGENTS.md`, `scripts/UI/RarityPanel.cs`, `scenes/UI/RarityPanel.tscn`, `scripts/UI/ZoomResponsiveUi.cs`, `scripts/TradeScene.cs`, `scenes/trade_scene.tscn`, `scenes/UI/InventoryUI.tscn`, `tests/ProjectTest.cs`, `tools/verify.ps1`, `.vscode/settings.json`, `themes/game_theme.tres`, `README.md`.
Write-set: `scripts/UI/RarityPanel.cs`, `scenes/UI/RarityPanel.tscn`, `scripts/UI/ZoomResponsiveUi.cs`, `tests/ProjectTest.cs`, `README.md`, `ORCHESTRATION_PLAN.md`. Владелец сцены и плана — `/root`; пересекающиеся задачи приостановлены на время HF-08.
Out of scope: торговая логика/экономика, save schema, pause, изменение исходного арта, source/.
Приёмка: таблица компактнее, все подписи/цифры выводятся читаемым текстом движка; умеренный ограниченный масштаб при зуме; позиция следует кнопке при перемещении/зуме/resize, удерживается в viewport и выбирается с учётом кнопки/HUD; отдельный слой поверх фонов и прочего world UI, под паузой; наведение и цены обеих мастерских сохранены. Проверки: integration regression для размеров/содержимого/позиции/слоёв/hover обеих таблиц; полный `tools/verify.ps1`; `git diff --check`.
Read-set exception: координатор выполнил точечную диагностику перечисленных файлов до оформления CP; source/ не читался.

```text
Task: HF-08
Recommended status: REVIEW
Changed: scripts/UI/RarityPanel.cs; scenes/UI/RarityPanel.tscn; scripts/UI/ZoomResponsiveUi.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (0 ошибок/предупреждений, 19 domain tests, import без ошибок, Godot integration); git diff --check -> PASS.
Acceptance: вместо растровых заголовков/редкостей используются 24 читаемые текстовые ячейки, рамка из существующей panel_large.png, размер 420×228. Отдельный CanvasLayer 20 исключает перекрытие фонами, inventory и ценовой подсказкой; pause остаётся сверху. Screen-space масштаб ограничен 0.95–1.05, позиция выбирается среди шести вариантов рядом с кнопкой, учитывает viewport/HUD и обновляется при движении камеры/zoom/resize. Проверены 36 комбинаций двух мастерских, viewport 1280×720/960×540, zoom 0.25/0.7/1.8 и положения у обоих краёв/в центре; все ячейки внутри панели, таблица в viewport, baseline не перекрывает кнопку/HUD. Реальное GUI-наведение/выход проверены в обеих мастерских при двух разрешениях. Цены, проценты, рероллы и прежняя регрессия сохранены.
Read-set exception: диагностика координатора перед CP, перечисленная выше; source/ не читался.
Risks/blocker: ручной visual playtest не выполнялся. При нехватке свободного места алгоритм минимизирует перекрытия, сохраняя таблицу целиком внутри viewport.
Next: проверить внешний вид подсказки в игровом сеансе.
```

Координатор подтвердил HF-08 по финальному verify (exit 0) 2026-11-05; REVIEW → DONE. Основной фронт и ворота не изменены; владение сценой освобождено.

### HF-07 — увеличить окно продажи

Статус: `DONE`; исполнитель/координатор `/root`; зависимости отсутствуют; `CP-HF-07`, READY проверен 2026-11-05.
Read-set: этот план, `AGENTS.md`, `scenes/trade_scene.tscn`, `scripts/UI/ZoomResponsiveUi.cs`, `tests/ProjectTest.cs`, ранее прочитанные `tools/verify.ps1` и `.vscode/settings.json`.
Write-set: `scenes/trade_scene.tscn`, `ORCHESTRATION_PLAN.md`. Единственный владелец сцены — `/root`; пересекающиеся VF/HF-задачи не исполняются.
Out of scope: торговая логика, pause/inventory, изменение zoom-компенсации, арт, source/.
Приёмка: окно продажи примерно в 1.5 раза крупнее, увеличены шрифты цен, отступы и кнопки; центр окна сохранён. Полный `tools/verify.ps1` подтверждает прежние GUI-клики продажи/реролла и zoom-регрессию; `git diff --check`.
Read-set exception: точечная диагностика этих UI-файлов координатором перед оформлением CP для выбора размеров.

```text
Task: HF-07
Recommended status: REVIEW
Changed: scenes/trade_scene.tscn; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (0 ошибок/предупреждений, 19 domain tests, import и Godot integration); git diff --check -> PASS.
Acceptance: базовая панель увеличена с 280×180 до 420×280 при том же центре; шрифты цен 27/33 вместо 18/22, кнопки высотой 78 вместо 52, увеличены интервалы/отступы. Ограничение высоты удалено для свободной компоновки содержимого. GUI-продажа/реролл и zoom-регрессия проходят.
Read-set exception: диагностика координатора до оформления CP; source/ не читался.
Risks/blocker: ручной visual playtest не выполнялся.
Next: перезапустить игровой сеанс.
```

Координатор подтвердил HF-07 по verify (exit 0) 2026-11-05; REVIEW → DONE. Основной фронт и ворота не изменены; владение сценой освобождено.

### HF-06 — пауза, подсказки товаров и читаемость при зуме

Статус: `DONE`; исполнитель `/root` (координатор и исполнитель); зависимости отсутствуют; пакет `CP-HF-06`, READY проверен 2026-11-05. Другие VF/HF-задачи с пересекающимися файлами на время работы не исполнялись.

#### CP-HF-06

Read-set: `scripts/TradeScene.cs`, `scripts/MainMenu.cs`, `scripts/UI/InventoryUI.cs`, `scripts/UI/SlotUI.cs`, `scenes/trade_scene.tscn`, `scenes/main_menu.tscn`, `themes/game_theme.tres`, `tests/ProjectTest.cs`, `tools/verify.ps1`, `.vscode/settings.json`, `project.godot`, `README.md`, `AGENTS.md`, этот план. Новые файлы write-set также разрешены для чтения.
Write-set: `scripts/TradeScene.cs`, `scripts/UI/InventoryUI.cs`, `scripts/UI/PauseMenu.cs`, `scripts/UI/PauseMenu.cs.uid`, `scripts/UI/ZoomResponsiveUi.cs`, `scripts/UI/ZoomResponsiveUi.cs.uid`, `scenes/trade_scene.tscn`, `tests/ProjectTest.cs`, `README.md`, `ORCHESTRATION_PLAN.md`. Единственный владелец сцены и плана — `/root`.
Out of scope: экономика, save schema, новая доменная логика, арт, source/, полный S5-05 и сохранение настроек между запусками.
Приёмка: Esc/кнопка открывают модальную паузу; время и игровой ввод остановлены; доступны настройки полного экрана, checkpoint и новая игра, продолжение через Esc/кнопку. Подсказки предметов содержат название, редкость и базовую стоимость и обновляются после перемещения/reload. Мелкие world UI умеренно увеличиваются при zoom < 1 и возвращаются к исходному масштабу при zoom >= 1; HUD не зависит от камеры. Заголовки комнат с подложками/описаниями скрыты при zoom <= 0.4, плавно проявляются до zoom 0.6. Проверки: новые Godot integration tests, полный `tools/verify.ps1`, `git diff --check`.
Read-set exception: координатор открыл реализацию и сделал точечный поиск UI-файлов до оформления CP для определения read/write-set; source/ не читался.

```text
Task: HF-06
Recommended status: REVIEW
Changed: scripts/UI/PauseMenu.cs[.uid]; scripts/UI/ZoomResponsiveUi.cs[.uid]; scripts/TradeScene.cs; scripts/UI/InventoryUI.cs; scenes/trade_scene.tscn; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (0 ошибок/предупреждений, 19 domain tests, import, Godot integration); git diff --check -> PASS.
Acceptance: Esc и видимая кнопка открывают паузу; проверены блокировка таймера/камеры/дня/реролла, настройки и возврат через Esc, продолжение, checkpoint/new game через реальные GUI-клики, снятие паузы при удалении сцены. Подсказки содержат имя/редкость/цену при наведении на иконку, после сдвига товаров и reload. Проверены все 13 элементов заголовков при zoom 0.4/0.5/0.6, ограничение компенсации 1.65 и возврат к масштабу 1 при приближении; HUD неизменен. Полная прежняя регрессия проходит.
Read-set exception: первичная диагностика координатора до CP; дополнительно прочитаны scenes/UI/ShowcaseSlot.tscn для проверки геометрии подписей и scripts/CounterWorld.cs для исключения перехвата ввода. Для диагностики синтетического события колеса прочитан официальный исходник Godot viewport.cpp; source/ не читался.
Risks/blocker: ручной visual playtest не выполнялся; fullscreen использует существующий DisplayServer API и не сохраняется между запусками (вне scope).
Next: перезапустить игровой сеанс и проверить внешний вид меню.
```

Координатор подтвердил HF-06 по финальному verify (exit 0) 2026-11-05; REVIEW → DONE. Основной фронт и ворота не изменены; владение сценой освобождено.

### HF-05 — чёрные числа редкостей и подсказка инвентаря

Статус: `DONE`; исполнитель `/root`; зависимости отсутствуют; пакет `CP-HF-05`.
Read-set: `scripts/UI/RarityPanel.cs`, `scripts/UI/SlotUI.cs`, `scripts/UI/InventoryUI.cs`, `scenes/UI/SlotUI.tscn`, `scenes/UI/RarityPanel.tscn`, `tests/ProjectTest.cs`, `tools/verify.ps1`, `.vscode/settings.json`, этот план и ранее прочитанный `AGENTS.md`.
Write-set: `scripts/UI/InventoryUI.cs`, `scenes/UI/SlotUI.tscn`, `scenes/UI/RarityPanel.tscn`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`.
Out of scope: экономика, изменение редкостей, эффекты следующего дня, TradeScene, source/. Другие задачи с этим write-set на время HF-05 не исполняются; обеими сценами владеет `/root`.
Приёмка: все 15 чисел в каждой из двух таблиц чёрные; при наведении на картинку предмета отображается название его редкости; текст обновляется вместе с предметом после размещения/загрузки. Проверка: полный `tools/verify.ps1`, `git diff --check`.

```text
Task: HF-05
Recommended status: REVIEW
Changed: scripts/UI/InventoryUI.cs; scenes/UI/SlotUI.tscn; scenes/UI/RarityPanel.tscn; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (build без предупреждений, 19 domain tests, import, Godot integration); git diff --check -> PASS
Acceptance: все значения обеих таблиц имеют Colors.Black. Ячейки инвентаря используют TooltipText с названием редкости; картинка пропускает мышь к кнопке. Проверено GUI-наведение на предмет, тексты подсказок после создания, сдвига предметов и reload; прежняя регрессия проходит.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся; отображение текста использует штатную подсказку Godot.
Next: перезапустить игровой сеанс.
```

Координатор подтвердил HF-05 по финальному verify (exit 0) 2026-11-02; REVIEW → DONE. Основной фронт и ворота не изменены.

### HF-04 — задержка покупателей 0,5–2 секунды

Статус: `DONE`; исполнитель `/root`; зависимости отсутствуют; пакет `CP-HF-04`.
Read-set: `scripts/TradeScene.cs`, `tests/ProjectTest.cs`, `README.md`, `tools/verify.ps1`, `.vscode/settings.json`, этот план и ранее прочитанный `AGENTS.md`.
Write-set: `scripts/TradeScene.cs`, `tests/ProjectTest.cs`, `README.md`, `ORCHESTRATION_PLAN.md`.
Out of scope: экономика/офферы, сцены, камера, новая save schema, source/. На время HF-04 другие задачи с этим write-set не исполняются.
Приёмка: случайное ожидание 0,5–2 секунды перед первым покупателем и между продажами при наличии выставленных товаров; реролл текущего покупателя остаётся мгновенным; пустые витрины не запускают сделки; загрузка сохраняет валидный остаток таймера, старый нулевой таймер заменяется новой задержкой. Проверки: полный `tools/verify.ps1`, `git diff --check`.

Решение владельца 2026-11-02 заменяет D-012: покупатели приходят через случайные 0,5–2 секунды, а не на следующем кадре. Таймер относится к представлению; случайность задержки не расходует текущий RNG товаров/офферов.

```text
Task: HF-04
Recommended status: REVIEW
Changed: scripts/TradeScene.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (build без предупреждений, 19 domain tests, import, Godot integration); git diff --check -> PASS
Acceptance: отдельный RNG выбирает 0,5–2 с перед первым покупателем и после продажи с оставшимися товарами. Проверены отсутствие раннего прихода, истечение выбранного интервала, пауза между продажами, отсутствие таймера при пустых витринах, reload/checkpoint, миграция нулевого таймера и сохранение положительного остатка 0,125 с. Реролл и активные сделки проходят прежние проверки.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся.
Next: перезапустить игровой сеанс.
```

Координатор подтвердил HF-04 по финальному verify (exit 0) 2026-11-02; REVIEW → DONE. Основной фронт и ворота не изменены.

### HF-03 — удалить мусорки и автоматическое размещение

Статус: `DONE`; исполнитель `/root`; зависимости отсутствуют; пакет `CP-HF-03`.
Read-set: `scripts/TradeScene.cs`, `tests/ProjectTest.cs`, `scenes/trade_scene.tscn`, `README.md`, `tools/verify.ps1`, `.vscode/settings.json`, этот план и ранее прочитанный `AGENTS.md`; поиск ссылок на удаляемые API в `scripts/`, `scenes/`, `tests/`.
Write-set: `scripts/TradeScene.cs`, `tests/ProjectTest.cs`, `README.md`, `ORCHESTRATION_PLAN.md`.
Out of scope: восстановление удалённых кнопок, изменение сцен, экономика, save schema, камера, source/. Другие задачи с этим write-set на время HF-03 не исполняются.
Приёмка: нет обращений к TrashBin/PlaceItemButton и методов удаления/автовыбора ячейки; создание, ручное размещение в выбранную витрину, торговля, save/checkpoint, новый день работают без кнопок. Тесты используют ручное размещение и не удаляют предметы через старый API. Проверка: полный `tools/verify.ps1`, `git diff --check`.

```text
Task: HF-03
Recommended status: REVIEW
Changed: scripts/TradeScene.cs; tests/ProjectTest.cs; README.md; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (build без ошибок/предупреждений, 19 domain tests, Godot import и ProjectTest); git diff --check -> PASS
Acceptance: удалены ссылки на кнопки, обработчики, TrashSelectedItem, PlaceNextItem, FindFreeShowcaseSlot и выбор витрины для удаления. Ручное размещение сохранено. GUI-тест кликает по конкретной витрине в каждой комнате; тесты проверяют сохранность остатков, отказ замены занятой/reserved ячейки, торговлю, reload и checkpoint без удаления товаров. README соответствует управлению. В сцене пользовательские удаления сохранены.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся.
Next: перезапустить игровой сеанс.
```

Координатор подтвердил HF-03 по финальному verify (exit 0) 2026-11-02; REVIEW → DONE. Основной фронт и ворота не изменены.

### HF-02 — камера по RoomLayout

Статус: `DONE`; исполнитель `/root`; зависимости отсутствуют; пакет `CP-HF-02`.

Read-set: `scripts/TradeScene.cs`, `tests/ProjectTest.cs`, `tools/verify.ps1`, `.vscode/settings.json`, этот план и ранее прочитанный `AGENTS.md`.
Write-set: `scripts/TradeScene.cs`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`.
Out of scope: экономика, save schema, сцены и арт. На время HF-02 другие задачи с этим write-set не исполняются.
Приёмка: камера ограничена мировым прямоугольником Sprite2D RoomLayout без padding Wall; фиксированный нижний zoom удалён, предел отдаления равен ширине viewport / мировой ширине RoomLayout; при слишком высоком viewport камера центрируется по Y. Учитываются позиция/масштаб объекта и resize viewport. Проверки: регрессия камеры и полный `tools/verify.ps1`.

```text
Task: HF-02
Recommended status: REVIEW
Changed: scripts/TradeScene.cs; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS (19 domain tests, Godot import/integration, build без предупреждений); git diff --check -> PASS
Acceptance: границы берутся из GlobalTransform * RoomLayout.GetRect(); padding удалён; MinZoom вычисляется по ширине viewport/layout. Тесты подтверждают оба угла, отдаление ниже старого 0.3, плавность/stability, перемещение и масштаб layout, ограничение только по ширине короткого layout и пересчёт после resize.
Read-set exception: нет.
Risks/blocker: ручной visual playtest не выполнялся.
Next: загрузить исправленный скрипт в новом игровом сеансе.
```

Координатор подтвердил HF-02 по verify (exit 0) 2026-11-02; REVIEW → DONE. Основной фронт и ворота не изменены.

### Внеочередное исправление UI 2026-11-02

| ID | Статус | Зависит | Исполнитель | Контекст |
|---|---|---|---|---|
| `HF-01` | `DONE` | — | `/root` | `CP-HF-01`: исправлены фантомная сделка и неработающие кнопки; verify PASS 2026-11-02. |

#### CP-HF-01

Read-set: `scenes/trade_scene.tscn`, `scripts/TradeScene.cs`, `scripts/CounterWorld.cs`, `scripts/UI/InventoryUI.cs`, `scenes/UI/InventoryUI.tscn`, `tests/ProjectTest.cs`, `tools/verify.ps1`, `.vscode/settings.json`, `project.godot`, `ChikagoShop.csproj`, `AGENTS.md`; этот план читает координатор.

Write-set: `scenes/trade_scene.tscn`, `scripts/CounterWorld.cs`, `tests/ProjectTest.cs`, `ORCHESTRATION_PLAN.md`.

Out of scope: экономика, save schema, новая доменная логика, перестановка комнат и арта, `source/`. Активные VF-задачи на время HF-01 не исполняются; владелец сцены один — `/root`.

Приёмка: новая игра не показывает предмет/сделку до выставления товара; обе комнаты имеют работающие кнопки размещения и удаления; HUD reset/checkpoint подключены; мышью проходит цикл создания, выбора, выставления, реролла и продажи. Проверка: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1` (build, 19 domain tests, import, Godot integration).

Handoff HF-01:

```text
Task: HF-01
Recommended status: REVIEW
Changed: scenes/trade_scene.tscn; scripts/CounterWorld.cs; tests/ProjectTest.cs
Checks: powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -> PASS; git diff --check -> PASS
Acceptance: восстановлены оба PlaceItemButton и видимость урн; устранён обрыв _Ready из-за отсутствующих узлов; предмет и trade panel скрыты по умолчанию. Headless-тест посылает события мыши через viewport: обе мастерские, inventory, обе кнопки размещения, reroll, sale, обе урны, end day, checkpoint, new game проходят. 19 domain tests и вся прежняя Godot-регрессия проходят; build без предупреждений.
Read-set exception: первичная диагностика TradeScene/сцены/toolchain выполнена координатором до оформления CP, чтобы определить точный набор исправления; source/ не читался.
Risks/blocker: ручной visual playtest окна не выполнялся; клики проверены headless через GUI dispatch. Пользовательские изменения сохранены.
Next: перезапустить текущий игровой сеанс для загрузки исправленной сцены.
```

Координатор подтвердил приёмку HF-01 по финальному verify (exit 0) 2026-11-02 и перевёл задачу REVIEW → DONE. Основной фронт S1 и ворота не изменены; блокировка VF-владения сценой снята.

| Состояние | Задача | Пакет | Исполнитель |
|---|---|---|---|
| `DONE` | `S1-01` — независимые domain/test projects | `CP-S1-01` | `/root` |
| `DONE` | `S1-02` — versioned economy config | `CP-S1-02` | `/root` |
| `DONE` | `S1-03` — item/inventory economy models | `CP-S1-03` | `/root` |
| `NEXT` | `S1-04` — named RNG streams | после подготовки `CP-S1-04` | — |

Текущие ворота: `G-S0 = PASSED`, `G-S1 = OPEN`. При назначении координатор вписывает сюда каноническое имя агента; роль в таблице стадии исполнителем не является.

### Ускоренный visual pass вне очереди

По решению владельца продукта от 2026-10-31 часть `S5` начата до закрытия `G-S1–G-S4`, чтобы быстрее получить демонстрируемую сцену. Это не переводит исходные задачи `S5-01`–`S5-06` в `DONE`: их полная приёмка всё ещё обязательна.

| ID | Статус | Сделано | Что осталось |
|---|---|---|---|
| `VF-01` | `DONE` | В runtime перенесены ателье, касса, витринный столик/блик, манекены, фон/логотип меню, UI-панели и шрифт; сцены скомпонованы и проверены visual smoke в 1280×720. | Зафиксировать права/лицензии и финальные import-настройки в `S5-00`/`S5-01`. |
| `VF-02` | `IN_PROGRESS` | Утверждённая `СхемаКомнат.jpg` перенесена в runtime как общий архитектурный фон; шесть корневых комнат выровнены по её проёмам без изменения дочернего содержимого. Шесть рабочих витрин разделены между торговыми комнатами по три. | Правый `Showroom` всё ещё использует общий placeholder; нужно финально развести визуальную роль двух торговых комнат и вручную проверить композицию на полном фоне. |
| `VF-03` | `IN_PROGRESS` | Подключён глобальный командный шрифт; готовый UI команды используется в меню, настройках, HUD, кнопках создания и hover-подсказках редкостей. Инвентарь без фиксированного лимита листается горизонтально; в витринных комнатах используются компактные урны. Регрессии покрыты сценовыми тестами. | Стилизовать оставшиеся системные кнопки и trade panel; подключить `dialog_panel.png` после появления механики реплик; проверить безопасные зоны на других 16:9 разрешениях и провести ручной visual playtest. |
| `VF-04` | `IN_PROGRESS` | Шляпка-клош и перчатки с прозрачным фоном подключены к сделке, inventory и showcase; второй временный слот ателье показывает `gloves` вместо несуществующего арта `coat`. | Нужны готовые гитара/труба и решение `S5-00` по финальной матрице: оставить `gloves` или вернуть `coat`. |
| `VF-05` | `IN_PROGRESS` | Восстановлены границы `Wall` 5243×2160, новые scene paths синхронизированы с C#/тестами; исправлена регрессия бесконечного отдаления и добавлен 120-frame zoom stability test. | Провести ручной playtest кликабельности и устранить перекрытия HUD с интерактивными объектами; затем закрывать полную приёмку `S5-02`, `S5-03`, `S5-05`, `S5-06`. |
| `VF-06` | `DONE` | В runtime перенесены 2 основы, 6 костюмов и 6 причёсок; реализованы и визуально проверены 18 комбинаций покупателя с совместимостью старых индексов 0–5. | Финальный crop/оптимизация и права на ассеты остаются частью `S5-00`/`S5-01`. |
| `VF-07` | `DONE` | Ускоренно подключён платный цикл мастерских: 2 энергии на день, цена 90$/140$, три товара за прокрутку, HUD, save/checkpoint и блокировка при нехватке ресурсов; provisional-баланс 220$/200$/400$ покрыт регрессией. Музыкальная комната закрыта чёрной панелью до постоянной покупки за 100$; доступ входит в save/checkpoint. Музыкальные базовые цены повышены до 45/75/120/180/300$. Энергия получила отдельную панель в стиле счёта; цены прокруток видны в заголовках и hover-плашках. Красная кнопка сделки рероллит и показывает лимит редкости; при нуле рероллов товар остаётся в обязательной продаже по текущей цене. | В `S2-01` перенести правила из prototype adapter в общий domain/config flow; в `S4-05` откалибровать старт, unlock, цены и квоты симулятором. Карточки не реализовывались. |

Manifest текущего переноса: `арт команды/Комнаты/Готовое/{ателье.jpg,касса.jpg}` → `art/rooms/{atelier.jpg,cashier.jpg}`; `Предметы/Интерьер/{столик,блики стекла,манекен для вещей,манекен портновскийй}.png` → `art/props/*`; товары ателье без фона → `art/items/{hat_cloche,gloves}.png`; готовые основы, `одежда 1..3` и `прич 1..3` → `art/characters/*`; готовые меню, кнопки, панели, окно редкостей, кнопка создания, диалоговая рамка и `Myfont-Complete-Updated` → `art/ui/*` и `art/fonts/game.ttf`. Runtime не ссылается на `source/`.

Технический остаток исходного плана не отменён: после visual pass по-прежнему нужны `S1-04`–`S1-06`, все задачи `S2`–`S4`, незакрытые части `S5` и релизная стадия `S6`.

## Как пользоваться планом

Операционный протокол, Definition of Ready и handoff находятся в `AGENTS.md`. Этот файл отвечает только за спецификацию, решения, задачи, зависимости и evidence ворот. Новое продуктовое правило сначала попадает в журнал решений, затем — в конфигурацию и тесты.

Статусы:

| Статус | Значение |
|---|---|
| `TODO` | Задача известна, но её входы или зависимости ещё не готовы. |
| `READY` | Результат, контекст, границы и проверка определены. |
| `IN_PROGRESS` | В «Текущем фронте» указан ровно один исполнитель. |
| `BLOCKED` | Указан конкретный внешний блокер или требуемое решение. |
| `REVIEW` | Реализация закончена, но критерии ещё подтверждает другой агент/координатор. |
| `DONE` | Критерии приёмки и проверки выполнены. |
| `DROPPED` | Явно исключено из MVP с записью причины. |

`NEXT` — метка очереди в «Текущем фронте», не статус задачи. Ворота стадий имеют отдельный цикл: `LOCKED` (предыдущие ворота не пройдены) → `OPEN` (стадия активна) → `PASSED` (критерии подтверждены датой и evidence).

## Быстрый контекст

### Идея и игровой цикл

«Бакалея для гангстеров» (`Groceries for Gangsters` — временное название) — casual management/simulator о владельце магазина в Чикаго 1920–30-х, который должен закрыть долг перед мафией.

Главные особенности:

- товары получаются платными гача-прокрутками в мастерских;
- покупатель предлагает цену, а игрок может потратить реролл и заменить покупателя вместе с предложением;
- прохождение ограничено семью днями и двумя долговыми квотами.

Цикл дня:

1. Восстановить энергию и активировать купленную накануне карточку.
2. Распределить энергию между открытыми мастерскими.
3. Оплатить прокрутки и получить товары.
4. Переместить товары из инвентаря на витрины.
5. Последовательно принять предложение либо потратить реролл; при нуле рероллов товар можно только продать по текущему офферу (`D-011`).
6. Обновить квесты, выполнить покупку/переходы и при необходимости выплатить квоту.
7. Выбрать максимум одну из трёх карточек следующего дня либо пропустить выбор.
8. Сохранить состояние и начать следующий день, победить или проиграть.

### Граница MVP

В MVP входят:

- 7 игровых дней, первый день выполняет роль обучения;
- ателье и музыкальная мастерская;
- энергия: 1 до покупки второй мастерской/комнаты, затем 2;
- платные прокрутки по 3 товара и аварийная бесплатная прокрутка;
- редкости, инвентарь, витрины, торговля, предложения и рероллы;
- 2 торговые комнаты и отдельная касса; минимум 6 витрин в одной комнате;
- 2 проверки квоты; число и поведение rollback-checkpoint уточняются в `Q-007`;
- сюжетный квест и по одному побочному квесту на мастерскую;
- карточки следующего дня;
- касса с NPC, модульные покупатели, камера, меню и настройки;
- сохранение, загрузка, новая игра, победа и поражение;
- воспроизводимая desktop-сборка и полный семидневный smoke-test.

В `Q-004` остаётся определить число витрин и роль второй торговой комнаты; сокращение до одной комнаты возможно только явным изменением scope через `D-*`. Скрытая коррекция случайности, дополнительный контент, анимированные персонажи, Steam-интеграция и новые мастерские не блокируют MVP.

### Базовая спецификация экономики v0.5

Это наиболее точный исходный документ и приоритетный baseline, но открытые `Q-*` ещё могут заменить отдельные правила явной записью `D-*`. Все деньги и процентные модификаторы — целые числа. Параметры обязаны находиться в одной конфигурации, которую читают игра, тесты и баланс-симулятор.

| ID | Редкость | Вес | Ателье | Музыка | Рероллы товара |
|---:|---|---:|---:|---:|---:|
| 0 | Ширпотреб | 60 | 30 | 36 | 3 |
| 1 | Заурядная | 25 | 50 | 60 | 2 |
| 2 | Первоклассная | 10 | 80 | 96 | 2 |
| 3 | Эксклюзивная | 4 | 120 | 144 | 1 |
| 4 | Неповторимая | 1 | 200 | 240 | 1 |

- Стоимость прокрутки: ателье — `$90`, музыка — `$110`.
- Одна платная прокрутка независимо генерирует 3 редкости по весам `[60, 25, 10, 4, 1]`.
- Гарантия минимальной редкости меняет ровно 1 или 2 настроенных слота только одной выбранной платной прокрутки за день и нормализует относительные веса оставшихся редкостей. Например, минимум «Эксклюзивная» даёт 80% / 20% между двумя верхними редкостями.
- Диапазон предложения сначала выбирается по весам: `−20…−1%` — 20%, `0…+10%` — 70%, `+11…+30%` — 10%; внутри диапазона целое значение равновероятно.
- Формула: `FinalPrice = RoundHalfUp(BasePrice × (100 + BuyerPercent + SumOfBonuses) / 100)`.
- Процентные бонусы складываются в процентных пунктах, не перемножаются.
- Реролл полностью заменяет предложение; вернуться к предыдущему нельзя.
- Непроданные предметы и оставшиеся рероллы экземпляра сохраняются между днями.

Карточки:

- после каждого дня, кроме последнего, генерируются 3 разные карточки;
- можно купить максимум одну либо пропустить;
- цена списывается сразу, эффект действует только следующий день;
- редкости по вечерам 1–2: 70/30/0%, 3–4: 60/30/10%, 5–6: 50/33/17%;
- каждая карточка имеет `FirstAvailableDay`;
- реролл карточек заменяет весь набор из трёх.

Квесты:

- сюжетный после первой квоты: награда `$35`, открывает покупку музыкальной комнаты за `$45`;
- ателье: продать 6 товаров после принятия, награда `$5`, постоянные `+6 п.п.` к предложениям одежды;
- музыка: продать 6 инструментов, награда `$5`, один бесплатный реролл набора карточек каждый вечер;
- награды выдаются ровно один раз, постоянные эффекты не применяются задним числом;
- сюжетный квест завершается фактической проверкой игрового условия, а не автоматической наградой после квоты;
- если условие требует передать предмет NPC, отдельная операция удаляет его из инвентаря, выдаёт заданную компенсацию и не считает продажей.

Минимальное состояние экземпляра товара:

`InstanceId`, `ItemDefinitionId`, мастерская, редкость, базовая цена, оставшиеся рероллы, состояние (`Inventory`, `Showcase`, `Trading`, `Sold`, `GivenToNpc`).

## Текущий baseline

### Проверено 2026-10-31

| Проверка | Результат |
|---|---|
| `dotnet build ChikagoShop.csproj` | Проходит, 0 ошибок и 0 предупреждений. |
| Headless-импорт Godot 4.7.2 Mono | Проходит. |
| `tests/ProjectTest.cs` | Проходит через `tools/verify.ps1`; торговля, save/checkpoint, квоты, камера и меню зелёные. |
| Visual smoke `Showroom` | Проходит: фон находится между `Wall` и контролами, заголовок и 6 витрин видимы. |
| Git | `main` и `origin/main` содержат выполненный фронт S0 до коммита `e2317de`. |

Устранённый разрыв baseline:

- C# и тесты переведены на `World/Showroom/...`;
- мир расширен до `5760×2160`, а четыре текущих room background проверяются на попадание в `Wall`;
- камера достигает правых комнат у `X=3840`;
- порядок слоёв закреплён как `Wall < ShowroomBackground < controls`.

### Что уже есть и как это использовать

| Артефакт | Решение |
|---|---|
| `project.godot`, `.csproj`, стартовая сцена | Сохранить как основу Godot 4.7.2 .NET / C# / .NET 8. |
| `MainMenu.cs`, `main_menu.tscn` | Переиспользовать; добавить pause/back-flow, сохранение настроек и проверку разрешений. |
| `TradeScene.cs` | Сохранить как executable specification и фасад интеграции. Не наращивать доменную логику; постепенно извлекать системы. |
| Камера drag/zoom/clamp | Переиспользовать после исправления мировых границ и тестов разных разрешений. |
| `ModularCustomer.cs`, `customer.tscn` | Переиспользовать 2 тела × 3 причёски как placeholder; позже убрать жёсткое число вариантов и валидировать индексы. |
| `InventoryUI.cs`, `SlotUI.cs`, UI-сцены | Переиспользовать представление; заменить фиксированные 6 слотов, индексные товары и прямую связь с `TradeScene` на сигналы/модель. |
| 6 витрин и торговые действия | Переиспользовать UX-прототип и сценарии, но перейти на экземпляры товаров и конфигурацию. |
| JSON-save и checkpoint-код | Переиспользовать версионирование; схему расширять по стадиям, а prototype-save либо поддержать одной явной миграцией, либо безопасно сбрасывать. |
| `tests/ProjectTest.cs` | Сохранить как ценную регрессионную основу и разделить по системам; сначала синхронизировать с новой сценой. |
| `art/` | Использовать текущие изображения как placeholder; импортировать только утверждённые runtime-ассеты. |
| `source/арт команды/**/Готовое` | Кандидаты на перенос: ателье, касса, UI-окна, меню, шрифт, модульные персонажи. Сначала права, обрезка и оптимизация. |

Не переносить в целевую экономику без переписывания:

- hardcoded-массив из 4 товаров и цены `4/9/18/12`;
- бесплатную бесконечную выдачу одного предмета по кнопке;
- равномерный оффер `−30…+30%` и реролл с искусственными 70% на повышение;
- лимит рероллов уже зависит от редкости, но в prototype catalog пока присутствуюют только первые две редкости;
- пробные квоты `$45/$90`;
- сохранение товаров только индексами массива.

### Ограничения арта

- Нет готового фона музыкальной мастерской, толпы, кассира/NPC, инструментов и полного набора товаров.
- Фото шляпки и перчаток стилистически расходятся с карандашной графикой.
- В UI-кнопки вшит русский текст; в runtime текст должен выводить движок.
- Часть текстур слишком велика для назначения и требует crop/9-slice/import-настроек.
- У персонажей нет анимационных кадров; женская основа содержит большой пустой холст.
- До публикации нужно подтвердить лицензии шрифта, фотографий и внешних материалов.
- PSD остаются в локальном архиве; в `art/` попадают только подготовленные экспорты.

## Карта стадий

`S0 Канон и baseline → S1 Домен и данные → S2 Один полный день → S3 Семидневная прогрессия → S4 Вторая мастерская и мета → S5 Контент и UX → S6 Стабилизация и MVP-сборка`

| Gate | Статус | Дата | Evidence |
|---|---|---|---|
| `G-S0` | `PASSED` | 2026-10-31 | `tools/verify.ps1` PASS; `S0-01…S0-07` DONE; решения `D-006…D-008`. |
| `G-S1` | `OPEN` | 2026-10-31 | Первый фронт: `S1-01` / `CP-S1-01`. |
| `G-S2` | `LOCKED` | — | — |
| `G-S3` | `LOCKED` | — | — |
| `G-S4` | `LOCKED` | — | — |
| `G-S5` | `LOCKED` | — | — |
| `G-S6` | `LOCKED` | — | — |

## S0 — канонизация и зелёный baseline

Цель: сохранить пользовательскую реорганизацию, восстановить запускаемый прототип и закрыть решения, которые меняют архитектуру или баланс.

| ID | Статус | Зависит | Роль | Результат | Контекст | Приёмка |
|---|---|---|---|---|---|---|
| `S0-01` | `DONE` | — | Аналитик | Выжимка двух DOCX, инвентаризация 59 исходных файлов, список противоречий. | Этот план; локально `source/*.docx`. | Существенные правила перенесены в быстрый контекст и `Q-*`. |
| `S0-02` | `DONE` | — | Координатор | Корневая запись `/source/` в `.gitignore`. | `.gitignore`. | `git check-ignore source/<file>` подтверждает правило; ранее файлы не отслеживались. |
| `S0-03` | `DONE` | — | QA/архитектор | Аудит кода, сцен, арта и живой baseline build/import/test. | `scripts/`, `scenes/`, `tests/`, `art/`. | Результаты и разрыв scene-contract зафиксированы выше. |
| `S0-04` | `DONE` | `S0-03` | Интегратор Godot | Синхронизировать текущую иерархию `Showroom` с C#/тестами/README и привести world bounds к фактической компоновке без выбора финального числа комнат. | `CP-S0-04`. | Build/import/test зелёные; contract-test доказывает bounds/достижимость комнат; visual smoke подтверждает слои `Showroom`; реорганизация арта не отменена. |
| `S0-05` | `DONE` | — | Владелец продукта + координатор | Принять только ранние решения `Q-002`, `Q-007`, `Q-008` и записать `D-*`. | `D-006…D-008`. | Определены item/reroll state, checkpoint contract и inventory capacity; остальные вопросы остаются у своих стадий. |
| `S0-06` | `DONE` | `S0-03` | DevEx/QA | Добавить переносимый `tools/verify.ps1`, который находит .NET/Godot и одной командой запускает build, import и headless-тест. | `CP-S0-06`. | Скрипт без hardcoded machine-path находит toolchain, сохраняет exit code каждого шага и полностью проходит после `S0-04`. |
| `S0-07` | `DONE` | `S0-04`, `S0-05`, `S0-06` | Координатор/QA | Зафиксировать baseline, границу ближайшего фронта и evidence `G-S0`. | План, README, результаты verify. | `G-S0=PASSED`; `S1-01` получил `CP-S1-01` и `READY`. |

Ворота S0:

- build/import/test зелёные;
- решения `Q-002`, `Q-007`, `Q-008` зафиксированы;
- текущая перестройка сцен и ассетов сохранена, README соответствует факту;
- ни одна runtime-сцена не ссылается на `source/`;
- одна переносимая команда проверки воспроизводит evidence.

## S1 — домен, конфигурация и воспроизводимость

Цель: отделить правила игры от Godot UI и сделать одну конфигурацию для игры, тестов и симулятора.

| ID | Статус | Зависит | Роль | Результат | Контекст | Приёмка |
|---|---|---|---|---|---|---|
| `S1-01` | `DONE` | `G-S0` | Архитектор/QA | Создать Godot-независимую .NET 8 domain library и test project, зафиксировать минимальные seams и добавить `dotnet test` в `tools/verify.ps1`. | `CP-S1-01`. | Verify выполняет 1 architecture test и всю Godot-регрессию; domain не ссылается на Godot. |
| `S1-02` | `DONE` | `S1-01` | Data/economy | Версионированный расширяемый config contract и реализованные секции ближайшего среза: дни, мастерские, редкости, офферы, provisional-квоты. | `CP-S1-02`. | Один JSON проходит Godot integration и 6 config unit-тестов; вместе с architecture test выполняется 7 domain tests. |
| `S1-03` | `DONE` | `S1-01`, `S1-02` | Domain | Модели `ItemDefinition`, `ItemInstance`, inventory/showcase, деньги, энергия и точный `RoundHalfUp`. | `CP-S1-03`. | 12 новых unit-тестов покрывают identity, переходы, terminal-state, неограниченный inventory, showcase, деньги, энергию и `RoundHalfUp`; полный verify выполняет 19 domain tests и Godot-регрессию. |
| `S1-04` | `TODO` | `S1-02` | Domain/QA | Seeded RNG с независимыми именованными потоками `economy/gameplay` и `presentation`. | Редкости/офферы, текущий `_rng`. | Косметический вызов не меняет экономический результат; economy-state сохраняется; тесты покрывают веса и гарантии. |
| `S1-05` | `TODO` | `S1-03`, `S1-04` | Persistence | Версионированный `GameState`/save repository для уже реализованных полей плюс контракт безопасного расширения. | Текущий `ProgressData`, `D-*` checkpoint. | Round-trip сохраняет state ближайшего среза и economy RNG; неизвестная/повреждённая версия безопасно восстанавливается или сбрасывается по принятому правилу. |
| `S1-06` | `TODO` | `S1-02`, `S1-03`, `S1-04` | Balance/tools | Минимальный simulator/parity harness для редкости, оффера и одной продажи на общем config/domain/seed. | Domain без Godot UI. | Игра-домен и harness дают одинаковые товары/оффер/цену на corpus seeds; отчёт пригоден для раннего anti-softlock анализа. |

Ворота S1:

- экономика меняется без переписывания сцен или доменной логики;
- `dotnet test` выполняет детерминированные доменные тесты;
- game state сериализуется и загружается без потери данных;
- ранний parity harness использует общий config/domain/RNG;
- новые правила не добавляются непосредственно в `TradeScene.cs`.

## S2 — вертикальный срез одного дня

Цель: один полный день от платной прокрутки ателье до выбора завершения дня на новых моделях.

| ID | Статус | Зависит | Роль | Результат | Контекст | Приёмка |
|---|---|---|---|---|---|---|
| `S2-00` | `TODO` | `G-S1` | Владелец продукта + координатор | Принять `Q-005` и `Q-013`: завершение дня и наличие/цена отказа от оффера. | Открытые решения, v0.5, прототип. | Два решения записаны как `D-*`; UI и state machine не имеют двусмысленной ветки. |
| `S2-01` | `TODO` | `G-S1` | Gameplay/domain | Энергия, оплата прокрутки ателье и генерация трёх экземпляров по редкости. | Конфиг мастерской/редкостей. | Деньги и энергия списываются один раз; три независимых результата; нехватка средств не меняет state; задача поставляет тесты. |
| `S2-02` | `TODO` | `G-S1` | Gameplay/UI | Инвентарь и витрины работают по `InstanceId`: перенос, выбор и возврат. | Inventory/Slot/Showcase prototype. | Один экземпляр не может одновременно находиться в двух местах; пустой/занятый/reserved slot валидируется тестами. |
| `S2-03` | `TODO` | `S2-00`, `S2-02` | Trade/domain | Покупатель, взвешенный оффер, число рероллов по редкости и принятые торговые действия. | Формула v0.5, существующая trade panel. | Диапазоны и `RoundHalfUp` точны; реролл независимо повышает или снижает оффер, меняет покупателя и не меняет товар. |
| `S2-04` | `TODO` | `S2-00`, `S2-01`, `S2-03` | Application | Машина состояний одного дня и безопасное завершение дня. | `D-*` из `S2-00`, game state. | Нельзя дважды списать/продать; активная сделка и неизрасходованная энергия обрабатываются по принятому правилу. |
| `S2-05` | `TODO` | `S2-01`, `S2-02`, `S2-03`, `S2-04` | Интегратор/QA | UI и headless-сценарий `spin → inventory → showcase → offer/reroll → sale → end/reload`. | Trade scene/UI, domain, tests. | Показаны базовая цена, оффер и остаток рероллов; товар подсвечивается/reserved; следующий товар выбирается автоматически; reload/end-day ничего не теряет. |

Ворота S2: один день полностью играется, сохраняется и повторяемо тестируется; временный визуал допустим, временная экономика — нет.

## S3 — семидневная прогрессия и защита от softlock

Цель: законченный каркас прохождения с квотами, checkpoint, обучением и аварийной прокруткой.

| ID | Статус | Зависит | Роль | Результат | Контекст | Приёмка |
|---|---|---|---|---|---|---|
| `S3-00` | `TODO` | `G-S2` | Владелец продукта + координатор | Принять `Q-001`, `Q-006`, `Q-011` и provisional-значения `Q-003`. | Открытые решения и ранний simulator. | Сроки, quota-flow и emergency spin записаны как `D-*`; числа помечены provisional до `S4-05`. |
| `S3-01` | `TODO` | `S3-00` | Progression | Каркас 7 дней, две проверки квоты, победа и поражение. | Config/решения дней и квот. | Платёж выполняется ровно по принятой модели; нельзя продолжить после terminal-state; граничная сумма проходит. |
| `S3-02` | `TODO` | `S1-05`, `S3-01` | Persistence | Checkpoint/rollback для уже реализованных систем и правило включения каждого нового state-раздела. | `D-*` из `Q-007`. | Деньги, товары, витрины, день и economy RNG совпадают после reload/rollback; новые S4-секции обязаны добавлять свои round-trip тесты. |
| `S3-03` | `TODO` | `S3-01` | UX/gameplay | Обучающий первый день: контекстные шаги без отдельной экономики. | Progression events. | Обучение можно завершить/пропустить; оно не выдаёт повторных денег/товаров. |
| `S3-04` | `TODO` | `S3-00`, `S2-01` | Economy | Аварийная прокрутка и защита от технического softlock. | Решение `Q-006`, config. | Доступна только при точном условии, не создаёт бесконечную генерацию и всегда позволяет дойти до следующего валидного шага/terminal-state. |
| `S3-05` | `TODO` | `S3-01`, `S3-02`, `S3-03`, `S3-04` | QA/integrator | Headless E2E каркаса 7 дней без ещё не реализованных карточек/квестов: победа, оба провала, reload, rollback, новая игра. | Progression и tests. | Все ветви каркаса зелёные; полный MVP E2E выполняется позже в `S4-06`. |

Ворота S3: семидневный progression-каркас с provisional-балансом работает и сохраняется; отсутствие ещё не реализованных систем S4 явно отражено в тестах.

## S4 — вторая мастерская, карточки, квесты и баланс

Цель: реализовать полный набор систем MVP и подтвердить, что они используют общий resolver эффектов и общий конфиг.

| ID | Статус | Зависит | Роль | Результат | Контекст | Приёмка |
|---|---|---|---|---|---|---|
| `S4-00` | `TODO` | `G-S3` | Game design/product | Утвердить целевые метрики для ещё provisional `Q-003` и закрыть `Q-009`: каталог 6–9 карточек с ID, ценой, эффектом, редкостью, `FirstAvailableDay` и числом гарантированных слотов. | Ранний simulator, базовая v0.5. | Card design pack записан в config/`D-*`; каждый эффект имеет проверяемую длительность/stacking rule; деньги и квоты ещё не объявлены финальными. |
| `S4-01` | `TODO` | `G-S3` | Gameplay | Перенести из проверенного prototype-среза состояния `locked/unlocked/purchased` музыкальной комнаты, покупку за `$100`, энергию 2 и spin `$140`; сюжетную награду не выдаёт. | Room/workshop config. | Комната визуально закрыта до покупки; обе энергии можно вложить в одну мастерскую; покупка и энергия сохраняются с тестом. |
| `S4-02` | `TODO` | `S4-00`, `S1-02` | Effects/cards | Общий resolver эффектов, 3 карточки без дублей, buy/skip, доступность по дню и полный reroll набора. | Утверждённый catalog. | Цена списывается раз; эффект живёт только следующий день; гарантия затрагивает ровно 1/2 слота одной выбранной прокрутки и не дублируется второй энергией. |
| `S4-03` | `TODO` | `S4-01`, `S4-02`, `S2-03` | Quests | Сюжетный и 2 побочных квеста, фактические условия, unlock-команда комнаты, передача предмета NPC, одноразовые награды и постоянные эффекты через общий resolver. | Базовые параметры квестов. | Никакой автонаграды сразу после квоты; сюжетный квест сам инициирует unlock/награду; шестая продажа выдаёт награду раз; передача удаляет предмет и не считается продажей. |
| `S4-04` | `TODO` | `S1-06`, `S4-01`, `S4-02`, `S4-03` | Balance/tools | Расширить ранний simulator до 7 дней, двух мастерских, карточек, квестов и стратегий. | Общие config/domain/RNG. | Parity corpus остаётся зелёным; отчёт показывает win-rate, emergency-use и технический softlock-rate. |
| `S4-05` | `TODO` | `S4-00`, `S4-04` | Balance/product | Откалибровать и закрыть `Q-003`: заморозить стартовые деньги, обе квоты и целевые метрики. | Simulator report + короткий playtest. | Финальные значения записаны в config/`D-*`; владелец продукта утвердил диапазоны сложности. |
| `S4-06` | `TODO` | `S3-02`, `S4-01`, `S4-02`, `S4-03`, `S4-05` | Интегратор/QA | Полный headless E2E семи дней со второй мастерской, карточками, квестами, save/reload и rollback. | Application/persistence/integration tests. | Победа, оба провала и checkpoint сохраняют все S4-секции без дублирования наград, эффектов, денег или товаров. |

Ворота S4: все механики MVP работают в одном семидневном прохождении; игра и симулятор не расходятся.

## S5 — контент, арт и пользовательский опыт

Цель: заменить технический интерфейс утверждённым контентом и сделать весь MVP доступным только через UI.

| ID | Статус | Зависит | Роль | Результат | Контекст | Приёмка |
|---|---|---|---|---|---|---|
| `S5-00` | `TODO` | `G-S4` | Product/art lead | Закрыть `Q-004`, `Q-010`, `Q-012`; создать asset manifest с точными локальными source-файлами, runtime ID и подтверждением происхождения/лицензии. | План и уже выполненная инвентаризация; без glob-сканирования. | Scope комнат/товаров/платформы утверждён; у каждого импортируемого файла есть точный путь и право использования. |
| `S5-01` | `TODO` | `S5-00` | Art tech | Перенести только manifest-ассеты, выполнить crop, безопасные имена, import-настройки и 9-slice. | Точные пути из asset manifest, `art/`. | Нет PSD, лишних холстов и вшитых локализуемых надписей; runtime не ссылается на `source/`. |
| `S5-02` | `TODO` | `S0-04`, `S5-00`, `S5-01` | Scene | Финальные комнаты, касса, витрины, world bounds и камера для утверждённого 16:9 baseline. | Trade scene и фоновые ассеты. | Rect каждой комнаты внутри мира; все зоны достижимы; камера не показывает пустоту сверх допуска; scene-contract тест зелёный. |
| `S5-03` | `TODO` | `G-S4`, `S5-01` | UI gameplay | HUD денег/дня/квоты/энергии, inventory, showcase и trade panel. | Отдельные UI-сцены/signals. | Базовая цена, оффер, редкость и рероллы читаемы; торговый цикл выполняется без debug-кнопок. |
| `S5-04` | `TODO` | `G-S4`, `S5-01` | UI meta | Cards, quests, quota/checkpoint dialogs и подтверждения. | Отдельные UI-сцены/signals. | Все meta-решения доступны через UI; повторный клик/закрытие окна не дублируют действие. |
| `S5-05` | `TODO` | `S3-03`, `S5-01` | UI shell | Главное меню, pause/back-flow, минимальные настройки, новая игра и onboarding presentation. | Menu/settings/tutorial scenes. | Esc не завершает игру без подтверждения; fullscreen/настройки сохраняются; обучение можно пропустить. |
| `S5-06` | `TODO` | `S4-03`, `S5-01`, `S5-02` | Content/presentation | Модульные покупатели, кассир/NPC, серая толпа, появление покупателя, фокус камеры, подсветка и исчезновение после сделки. | Customer/NPC scenes и dialogue catalog. | Нет неверных комбинаций; trade focus не ломает camera control/state; reload восстанавливает нужную фазу. |
| `S5-07` | `TODO` | `S4-01`, `S5-00`, `S5-01` | Content | Утверждённый минимальный каталог товаров и музыкальная мастерская без обязательного аудио. | Content matrix и runtime assets. | Каждый `ItemDefinitionId` имеет имя и допустимый visual; обязательных неизвестных/фото-placeholder нет. |
| `S5-08` | `TODO` | `S5-02`, `S5-03`, `S5-04`, `S5-05`, `S5-06`, `S5-07` | QA/UX | UI/content smoke семи дней на 16:9. | Полная сцена и UI. | Все 7 дней проходят без инспектора/debug API; читабельность и критический путь подтверждены. |

Ворота S5: content complete; ни одна обязательная система не требует placeholder-действия или ручной правки данных.

## S6 — стабилизация и MVP-сборка

Цель: закрыть регрессии, зафиксировать баланс и получить воспроизводимый релизный артефакт.

| ID | Статус | Зависит | Роль | Результат | Контекст | Приёмка |
|---|---|---|---|---|---|---|
| `S6-01` | `TODO` | `G-S5` | QA domain | Аудит покрытия и закрытие пробелов обязательной регрессии экономики; функциональные тесты уже поставляются предыдущими задачами. | Domain/parity/integration tests. | Каждый пункт списка ниже связан с зелёным тестом; flaky seed отсутствует. |
| `S6-02` | `TODO` | `S4-06`, `G-S5` | QA persistence | Save/reload/rollback soak, неизвестная/повреждённая schema, новая игра и повторные награды. | Текущая schema и минимальные fixtures. | Нет краша/дублирования/смешения checkpoint; старый неподдерживаемый save безопасно сбрасывается либо мигрирует по явно поддержанному пути. |
| `S6-03` | `TODO` | `G-S5` | QA UI | Аудит UI-critical regression на утверждённом 16:9 baseline. | Release-like build и UI tests. | Полный UI-путь зелёный; ввод не блокируется overlays; отсутствуют blocker/critical. |
| `S6-04` | `TODO` | `S4-05`, `S5-08`, `S6-01`, `S6-02`, `S6-03` | Balance/product | Подтвердить freeze серией симуляций и коротких ручных playtest; менять только config с новой записью решения. | Reports без персональных данных. | Утверждены win-rate, длительность и сложность; правила после freeze не расходятся. |
| `S6-05` | `TODO` | `S6-01`, `S6-02`, `S6-03`, `S6-04` | Release | Переносимый export preset, release build, версия, asset notices и release notes. | Export/project config, asset manifest. | Headless export из чистого клона создаёт запускаемый Windows-артефакт без локального Desktop path и `source/`. |
| `S6-06` | `TODO` | `S6-05` | Независимый QA | Финальный smoke релизного бинарника от новой игры до победы и обоих поражений. | Только release artifact и acceptance list. | Нет blocker/critical; MVP DoD выполнен; evidence приложено к `G-S6`, после чего создаётся tag. |

Ворота S6 = MVP готов; `G-S6` получает дату и ссылки на release/evidence.

## Обязательная регрессия экономики

Перед `S6-06` автоматизированы как минимум следующие сценарии:

1. Стартовых денег и худшего допустимого результата первого дня хватает на следующую платную прокрутку согласно принятому дизайну.
2. Все продажи, списания и бонусы используют точный `RoundHalfUp`.
3. Гарантия меняет ровно настроенные 1/2 слота одной выбранной прокрутки и при двух энергиях не применяется второй раз.
4. Шестая подходящая продажа выдаёт награду квеста ровно один раз.
5. Постоянный квестовый бонус действует только на последующие предложения.
6. Однодневная карточка исчезает после следующего дня.
7. Реролл карточек заменяет все три предложения без дублей.
8. Каждая квота оплачивается/резервируется ровно один раз по принятой в `Q-011` модели.
9. Музыкальную комнату можно купить позже, а не только сразу после открытия доступа.
10. Инвентарь и витрины сохраняются между днями, reload и checkpoint.
11. Аварийная прокрутка не создаёт бесконечный источник денег/товаров.
12. Save/reload посреди сделки не дублирует товар, оффер, деньги или рероллы.
13. Сюжетный квест не завершается автоматически после квоты; передача предмета NPC удаляет его один раз и не считается продажей.

## Открытые решения

Владелец продукта может принять рекомендуемый вариант либо записать другой. Координатор создаёт `D-*` в указанной milestone-задаче; только `Q-002`, `Q-007`, `Q-008` блокируют `S0-05`, остальные нужны позже.

| ID | Нужно решить | Рекомендуемый вариант | Решается в |
|---|---|---|---|
| `Q-001` | Первая квота: конец дня 3 или 4? | День 3, потому что это прямое правило v0.5; день 4 допустим только явным переопределением `D-*`. | `S3-00` |
| `Q-002` | Рероллы обновляются ежедневно или принадлежат товару? Что происходит после последнего отказа? | Решено в `D-011`: рероллы принадлежат товару; последний реролл меняет покупателя, после чего остаётся только продажа по текущему офферу. | `DONE` |
| `Q-003` | Стартовые деньги и размеры двух квот. | Старт задать минимально безопасным, квоты подобрать simulator; до `S4-05` использовать явно provisional-значения. | `S4-05` |
| `Q-004` | Сколько витрин и какая функция у второй из двух торговых комнат? | Сохранить исходный scope: две торговые комнаты с отдельной центральной кассой; в первой 6 витрин, число/роль витрин второй утвердить отдельно. Одна торговая комната — только явное сокращение MVP через `D-*`. | `S5-00` |
| `Q-005` | Когда разрешено завершить день? | Вручную при отсутствии активной сделки; при неизрасходованной энергии — подтверждение; непроданные товары сохраняются. | `S2-00` |
| `Q-006` | Частота и цена аварийной прокрутки. | Не более 1 раза за день, только если денег меньше самой дешёвой доступной платной прокрутки; тратит 1 энергию и даёт 1 товар. | `S3-00` |
| `Q-007` | Число checkpoint, состав snapshot и failure-flow. | Решено в `D-007`: checkpoint — полное сохранение после успешной выплаты квоты. | `DONE` |
| `Q-008` | Вместимость инвентаря и переполнение. | Решено в `D-008`: логически не ограничен, пустые ячейки не отображаются. | `DONE` |
| `Q-009` | Каталог карточек: ID, цена, эффект, редкость, `FirstAvailableDay`, 1/2 гарантированных слота. | Короткий design pack на 6–9 эффектов без сложных комбинаций. | `S4-00` |
| `Q-010` | Целевая платформа, управление, разрешения и локализация. | Windows desktop, 1280×720 logical 16:9 с масштабированием, мышь+клавиатура; русский MVP, строки вынести из текстур/кода. | `S5-00` |
| `Q-011` | Квота резервируется постепенно, платится вручную или автоматически списывается в срок? | Для MVP автоматически проверить `money >= quota` и один раз списать всю сумму в контрольный день; отдельный debt-pool не вводить. | `S3-00` |
| `Q-012` | Минимальный каталог товаров и связь с визуалом/редкостями. | Начать с 4 стабильных типов прототипа (`hat`, `coat`, `guitar`, `trumpet`) во всех допустимых редкостях; расширять только по утверждённой content matrix. | `S5-00` |
| `Q-013` | Существует ли бесплатный отказ от оффера и что делать при нуле рероллов? | Решено в `D-011`: отдельного отказа нет; при нуле рероллов красная кнопка блокируется, товар разрешено только продать. | `DONE` |

## Реестр рисков

| ID | Риск | Вероятность/влияние | Снижение |
|---|---|---|---|
| `R-001` | Продолжение разработки в монолитном `TradeScene` ускорит связность и поломки NodePath. | Высокая/высокое | Инкрементально извлекать чистые системы только в назначенных задачах; сцена — фасад. |
| `R-002` | Игровая экономика и симулятор разойдутся. | Средняя/высокое | Общие config/domain/RNG и parity-тест на seed. |
| `R-003` | Изменение иерархии сцены снова сломает C# и тесты. | Высокая/среднее | Уникальные имена/экспортируемые ссылки или адаптер scene binding; один contract-test. |
| `R-004` | Индексные товары сломают save после расширения каталога. | Высокая/высокое | Стабильные строковые ID, version contract и либо одна явно поддержанная миграция, либо безопасный reset prototype-save. |
| `R-005` | Неопределённые квоты создадут непроходимый или тривиальный MVP. | Высокая/высокое | Simulator + seed corpus + утверждённые целевые метрики. |
| `R-006` | Арт без проверенных прав нельзя публиковать. | Средняя/высокое | Asset register и лицензии до переноса в runtime. |
| `R-007` | Жёсткий лимит 6 предметов конфликтует с переносом остатков и spin по 3. | Высокая/среднее | Решить `Q-008`, тестировать overflow до UI-полировки. |
| `R-008` | Параллельные агенты будут конфликтовать в `.tscn` и плане. | Высокая/среднее | Один владелец сцены, disjoint write-set, план меняет только координатор. |
| `R-009` | Косметический RNG изменит экономическую последовательность и разрушит parity. | Средняя/высокое | Независимые именованные economy/presentation streams и seed corpus. |
| `R-010` | Неутверждённый каталог товаров/карточек заставит переделывать config, UI и save. | Высокая/среднее | Provisional ID не переименовывать; закрыть `S4-00`/`S5-00` до массового контента. |

## Контекст-пакеты READY-задач

### CP-S0-04 — восстановить scene-contract

Результат: текущая пользовательская компоновка запускается и проходит проверки без отката реорганизации и без преждевременного решения о финальном числе комнат.

Прочитать:

- разделы «Текущий baseline» и `S0` этого плана;
- `scripts/TradeScene.cs` — `_Ready`, camera bounds и scene paths;
- `scenes/trade_scene.tscn` — `Wall`, комнаты и `Showroom`;
- `tests/ProjectTest.cs` — пути узлов и проверки камеры;
- `README.md` — описание структуры сцены;
- `git diff -- scenes/trade_scene.tscn scenes/main_menu.tscn scenes/customer.tscn`.

Разрешённый write-set:

- `scripts/TradeScene.cs`;
- `scenes/trade_scene.tscn`;
- `tests/ProjectTest.cs`;
- `README.md`.

Не входит:

- новая экономика, энергия, карточки или квесты;
- удаление/возврат пользовательских файлов арта;
- изменение визуального стиля;
- выбор финального room-count из `Q-004`;
- массовый архитектурный рефакторинг.

Факты:

- новые пути: `World/Showroom/PlaceItemButton`, `World/Showroom/TrashBin`, `World/Showroom/Showcases/Slot*`;
- `_Ready` всё ещё содержит старые `GetNode("World/PlaceItemButton|TrashBin|Showcases/...")`;
- тесты ищут старые `World/ShowroomBackground` и `World/Showcases/...`;
- `RoomSeparator` имеет ширину около 5760, `Wall` — 3840, а `MusicWorkshop`/`Showroom` начинаются около X=3840: это может означать намеренно расширенный мир, поэтому не следует автоматически возвращать layout к 2×2;
- непрозрачный `ShowroomBackground` расположен в дереве после `ShowroomHeader`/`ShowroomLabel` и может их перекрывать: фон должен рисоваться первым либо иметь более низкий `z_index`;
- пользовательские перемещения `art/* → art/npc|rooms|ui` нужно сохранить.

Добавить contract-проверку: bounds фона и интерактивной области каждой текущей комнаты полностью входят в `Wall`, а камера реально достигает их центра. Проверки только координаты камеры недостаточно. После headless-проверок выполнить короткий visual smoke слоёв/кликабельности `Showroom`.

Проверки:

```powershell
dotnet build ChikagoShop.csproj
godot --headless --editor --path . --quit
godot --headless --path . --script res://tests/ProjectTest.cs
```

Если `godot` не в `PATH`, использовать путь из `.vscode/settings.json`. В PowerShell GUI-executable следует запускать с ожиданием завершения (`Start-Process -Wait -NoNewWindow`), иначе exit code теста потеряется.

### CP-S0-06 — единая команда проверки

Результат: новый агент не тратит ход на поиск toolchain и не теряет exit code Godot.

Прочитать:

- `.vscode/settings.json`;
- `ChikagoShop.csproj`;
- `README.md` — текущие команды проверки;
- результаты baseline в этом плане.

Разрешённый write-set: только `tools/verify.ps1`.

Не входит: установка SDK/Godot, изменение product-кода, CI, исправление падающих тестов, hardcoded machine-path в скрипте.

Скрипт должен:

- принимать необязательные `-DotnetPath`/`-GodotPath`;
- находить `dotnet` в порядке: параметр, `PATH`, `DOTNET_ROOT`, стандартный путь на основе `ProgramFiles`;
- находить Godot в порядке: параметр, переменная окружения, `PATH`, tracked `.vscode/settings.json`, не копируя найденное значение в код;
- последовательно выполнять build, headless import и `ProjectTest`;
- для GUI-executable ждать завершения и возвращать реальный exit code;
- печатать короткий `PASS/FAIL` по шагам и завершаться ненулевым кодом при первой ошибке.

Проверка:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1
```

Review `S0-06` подтверждает, что скрипт корректно передаёт текущий ненулевой exit code `ProjectTest`; исправлять тест не входит в задачу. После `S0-04` координатор в `S0-07` повторяет ту же команду и ожидает `0`.

На этой стадии test project ещё отсутствует. `S1-01` расширяет тот же скрипт шагом `dotnet test`, не создавая второй runner.

### CP-S1-01 — независимые domain/test projects

Результат: solution содержит минимальный Godot-независимый domain assembly и реально исполняемый test project; единая verify-команда запускает их до Godot integration tests.

Прочитать:

- `ChikagoShop.sln` и `ChikagoShop.csproj`;
- `tools/verify.ps1`;
- `scripts/TradeScene.cs` только для фиксации границы: ничего из него пока не переносить;
- строку `S1-01`, `D-003`, `D-006…D-008` этого плана.

Разрешённый write-set:

- `ChikagoShop.sln`;
- `ChikagoShop.csproj` (только исключение новых independent-project исходников из Godot compile glob);
- `.gitignore` (только вложенные `bin/obj`);
- `src/.gdignore` и `tests/ChikagoShop.Domain.Tests/.gdignore`;
- `src/ChikagoShop.Domain/ChikagoShop.Domain.csproj`;
- `src/ChikagoShop.Domain/DomainAssembly.cs`;
- `tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj`;
- `tests/ChikagoShop.Domain.Tests/ArchitectureTests.cs`;
- `tools/verify.ps1`.

Не входит: перенос gameplay-кода, модели товара, economy config, RNG, save schema, изменение Godot-сцен или существующих integration tests.

Приёмка:

- domain project targeting .NET 8 не имеет ссылки на Godot;
- тест архитектуры загружает domain assembly и подтверждает отсутствие Godot references;
- `dotnet test` действительно обнаруживает и выполняет хотя бы один тест;
- `tools/verify.ps1` выполняет `dotnet test` после build и до Godot import;
- полный `tools/verify.ps1` остаётся зелёным.

Проверка:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1
```

### CP-S1-02 — versioned economy config

Результат: числа ближайшего вертикального среза находятся в одном JSON-файле с чистым domain loader/validator; Godot проверяет загрузку через тонкий adapter, не дублируя значения.

Прочитать:

- раздел «Базовая спецификация экономики v0.5» и `D-003`, `D-006…D-008`;
- `src/ChikagoShop.Domain/ChikagoShop.Domain.csproj`;
- `tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj`;
- `ChikagoShop.csproj` и `tests/ProjectTest.cs` только для integration seam.

Разрешённый write-set:

- `config/economy.v1.json`;
- `src/ChikagoShop.Domain/Configuration/*.cs`;
- `tests/ChikagoShop.Domain.Tests/EconomyConfigTests.cs`;
- `ChikagoShop.csproj` (ProjectReference/domain + config copy/import metadata);
- `scripts/Configuration/EconomyConfigProvider.cs`;
- `tests/ProjectTest.cs` (только config integration check).

Не входит: замена hardcoded экономики `TradeScene`, генерация предметов/RNG, item instances, save schema, UI и изменение provisional-чисел через балансировку.

Контракт ближайшего среза:

- `schema_version = 1`, `is_provisional = true`;
- 7 дней, tutorial day 1;
- мастерские `atelier` (`$90`, доступна сразу) и `music` (`$140`, постоянное открытие за `$100`);
- пять стабильных rarity ID, веса `[60,25,10,4,1]`, цены/рероллы v0.5;
- offer ranges с шансами 20/70/10 и целочисленными границами;
- provisional quotas используют baseline v0.5 (дни 3 и 7), суммы временные `$45/$90` до `Q-003`;
- loader принимает stream/string и не зависит от файловой системы или Godot;
- validator различает percentage distribution (сумма 100) и относительные положительные веса для будущей нормализации.

Приёмка:

- валидный файл загружается domain tests и Godot adapter;
- тесты отклоняют неизвестную schema, дубли ID, отрицательные цены/веса, неверные диапазоны и percentage sum не 100;
- относительные веса не обязаны давать 100, но обязаны быть положительными;
- ни одно число JSON не копируется в adapter;
- полный `tools/verify.ps1` зелёный.

### CP-S1-03 — item/inventory economy models

Результат: чистый домен выражает стабильные определения и уникальные экземпляры товаров, их допустимые состояния, неограниченный inventory, фиксированное число showcase slots, деньги/энергию и точное денежное округление.

Прочитать:

- `D-006`, `D-008` и минимальное состояние экземпляра из быстрого контекста;
- `config/economy.v1.json` и `src/ChikagoShop.Domain/Configuration/*.cs`;
- существующие inventory/showcase/trade сценарии в `tests/ProjectTest.cs` только как behavioral reference.

Разрешённый write-set:

- `src/ChikagoShop.Domain/Items/*.cs`;
- `src/ChikagoShop.Domain/Economy/*.cs`;
- `tests/ChikagoShop.Domain.Tests/ItemInventoryTests.cs`;
- `tests/ChikagoShop.Domain.Tests/MoneyEnergyTests.cs`.

Не входит: Godot adapter/UI, изменение `TradeScene`, RNG, генерация предметов, торговый offer flow, save schema и миграция старых индексных товаров.

Обязательные инварианты:

- `ItemDefinition` использует стабильный строковый ID, workshop ID и rarity ID;
- `ItemInstance` имеет уникальный ID, базовую цену, оставшиеся рероллы и ровно одно состояние: `Inventory`, `Showcase`, `Trading`, `Sold`, `GivenToNpc`, `Discarded`;
- переходы terminal-state необратимы; один экземпляр не может находиться в inventory и showcase одновременно;
- inventory логически не ограничен и содержит только реальные экземпляры (`D-008`);
- showcase имеет заданное число слотов, отклоняет занятый слот и сохраняет identity экземпляра;
- деньги/энергия не уходят ниже нуля, неуспешное списание не меняет state;
- `RoundHalfUp` работает целочисленно/decimal и не использует float/banker's rounding.

Приёмка:

- тесты покрывают допустимые/запрещённые переходы, уникальность, overflow-free inventory, занятый showcase, атомарное списание и границы округления;
- domain API не зависит от Godot и не использует индекс товара как identity;
- полный `tools/verify.ps1` зелёный.

Рабочий протокол и handoff-шаблон не дублируются здесь: см. `AGENTS.md`.

## Definition of Done MVP

MVP считается готовым только если одновременно:

- выполняются все обещания раздела «Граница MVP» и принятые `D-*`;
- `dotnet build`, `dotnet test`, Godot import и все headless-тесты проходят без ошибок одной verify-командой;
- экономика игры совпадает с симулятором на общем config/seed;
- новый игрок может пройти все 7 дней только через UI;
- победа, оба поражения, save/reload, checkpoint и новая игра проверены;
- нет blocker/critical дефектов и известных способов дублировать деньги, товары или награды;
- нет технического softlock: игрок всегда может выполнить допустимый следующий шаг, завершить день либо дойти до валидной победы/поражения; законный проигрыш из-за плохой экономики допустим;
- release export воспроизводится из чистого клона без `source/` и локальных абсолютных путей;
- лицензии runtime-ассетов известны;
- README содержит актуальные запуск, управление, сохранение и сборку;
- `G-S6=PASSED` содержит дату и ссылки на release/evidence.

## Журнал решений

| ID | Дата | Решение | Причина и последствия |
|---|---|---|---|
| `D-001` | 2026-10-31 | `source/` — локальный игнорируемый архив; каноническая выжимка хранится в этом плане. | Чистый клон не зависит от 68+ МБ DOCX/PSD/референсов; агенты не перечитывают их. |
| `D-002` | 2026-10-31 | Существующий прототип не выбрасывается; он служит executable specification и разбирается инкрементально. | Сохраняются рабочие UX-наработки и интеграционные сценарии без закрепления текущей архитектуры. |
| `D-003` | 2026-10-31 | Игра, тесты и баланс-симулятор используют одну economy-конфигурацию и один доменный код. | Предотвращает расхождение модели и реального прохождения. |
| `D-004` | 2026-10-31 | Незакоммиченная реорганизация арта/сцен считается пользовательской и не откатывается. | Любая стабилизация адаптируется к ней и не уничтожает чужую работу. |
| `D-005` | 2026-10-31 | Скрытая коррекция случайности отложена за MVP. | Сначала нужен прозрачный детерминированный baseline и измеримый баланс. |
| `D-006` | 2026-10-31 | Рероллы принадлежат экземпляру товара. Отказ расходует реролл и меняет покупателя; если отказ израсходовал последний реролл, товар автоматически удаляется вместо появления следующего покупателя. | Сохраняет полезность текущего предложения и задаёт конечную торговую последовательность без отдельной бесплатной кнопки мусора в сделке. |
| `D-007` | 2026-10-31 | Checkpoint — полное сохранение игрового состояния сразу после успешной выплаты квоты. | Возврат восстанавливает согласованный snapshot после контрольной точки; точный состав расширяется вместе с новыми системами. |
| `D-008` | 2026-10-31 | Логический инвентарь не ограничен. UI создаёт ячейки только для существующих товаров и не показывает пустые. | Устраняет overflow при переносе остатков; позднее представление может использовать scroll/grid без изменения модели. |
| `D-009` | 2026-10-31 | До калибровки `S4-05` игра использует 2 энергии в каждый день, старт 220$, прокрутки 90$/110$ по 3 товара и квоты 200$/400$. | Это provisional-срез для немедленно играемого цикла; значения не считаются финальным балансом. |
| `D-010` | 2026-10-31 | `D-009` уточнено: музыкальная комната закрыта чёрной панелью и навсегда открывается за 100$; её прокрутка стоит 140$, базовые цены редкостей повышены до 45/75/120/180/300$. | Вторая комната становится отдельной целью прогрессии и более дорогим экономическим уровнем; доступ сохраняется и входит в checkpoint. Значения остаются provisional до `S4-05`. |
| `D-011` | 2026-10-31 | `D-006` заменено: последний реролл меняет покупателя и доводит счётчик до нуля, но не удаляет товар. При нуле рероллов красная кнопка блокируется; активный товар можно только продать по текущему офферу. | Исключает уничтожение товара через торговую панель и делает исчерпание рероллов обязательством принять актуальную цену. |
| `D-012` | 2026-10-31 | Таймер ожидания покупателя убран: после выставления товара сделка начинается на следующем игровом кадре. | Ускоряет основной торговый цикл; состояние ожидания и настраиваемая задержка больше не являются частью MVP-поведения. |
| `D-013` | 2026-11-02 | Заменяет `D-012`: перед первым покупателем и между продажами выбирается случайное ожидание 0,5–2 секунды при наличии выставленных товаров. Реролл активной сделки остаётся мгновенным. | Прямое решение владельца продукта; отдельный RNG задержек, восстановление оставшегося таймера из существующего поля сохранения. |

Новые решения добавляются следующими номерами; старые записи не переписываются, а заменяются новой записью со ссылкой.

## После MVP

В backlog после `S6`, не смешивая с MVP:

- ограниченная скрытая коррекция случайности с логированием фактических шансов;
- дополнительные мастерские, комнаты, товары, карточки и квесты;
- полноценная анимация толпы и покупателей;
- музыка, расширенный SFX и аудиомикшер, если владелец продукта отдельно не вернёт их в MVP;
- другие языки, controller/mobile UX;
- Steam-функции, достижения, облачные сохранения и production telemetry.
