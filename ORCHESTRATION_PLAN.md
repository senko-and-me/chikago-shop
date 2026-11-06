# Оркестровый план: «Бакалея для гангстеров»

Версия плана: 1.1
Дата среза: 2026-10-31
Текущая стадия: `S1 — домен, конфигурация и воспроизводимость`
Источник истины: этот файл. Папка `source/` — локальный архив и не входит в Git.

## Текущий фронт

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
