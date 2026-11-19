# Оркестровый план: «Бакалея для гангстеров»

Версия плана: 1.2
Обновлено: 2026-11-18
Стадия по реестру: `S1 — домен, конфигурация и воспроизводимость`

Этот файл хранит требования, решения, стадии и текущий фронт. Рабочий протокол — в [AGENTS.md](AGENTS.md), подробные завершённые отчёты — в [архиве](docs/archive/ORCHESTRATION_HISTORY_2026-11-12.md). `source/` остаётся локальным архивом и не входит в Git.

Требования и таблицы ниже сохраняют первоначальный план от 31.10.2026. Внеочередные HF-задачи развивали прототип дальше этого среза; старые числа и описания не следует принимать за актуальную конфигурацию runtime. Текущие игровые механики описаны в [docs/GAMEPLAY.md](docs/GAMEPLAY.md), числа — в `config/economy.v1.json`. Завершение функции в HF не заменяет приёмку соответствующей задачи стадии.

## Текущий фронт
### HF-187 — публикация учебной блокировки и общих часов

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF185/HF186 DONE; пользователь разрешил commit/push; пересечений нет.

CP-HF-187 read-set: текущий фронт, готовые diff/evidence HF185/HF186, Git status/refs, основные сцены только hash. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md; публикация localization/en.tres, scripts/Days/DayCycleController.cs, scripts/UI/{EmbroideryGame,FirstTradeTutorial}.cs, tests/Integration/ProjectTest.{Crafting,DayExpiry,DayStart,DayStartGate,Days,Debug,DebugBalance,Economy,Helpers,Menu,Presentation,Tutorial,Ui}.cs. Out of scope: source/, пользовательские основные сцены, новые изменения поведения, force push.

Приёмка: готовые изменения HF185/HF186 опубликованы в origin/main; refs совпадают, пользовательские сцены сохранены вне коммита. Проверки: fetch/refs, hash с tested isolated copy, предыдущие build и rendered craft-contrast/day-start/tutorial PASS; staged diff --check; push/readback/hash сцен.

```text
Task: HF-187
Recommended status: REVIEW → принято координатором, DONE
Changed: ad736c6 опубликован в origin/main: запрет завершать день во время обучения с подсказкой, общие часы над мини-играми и удаление дубля дня внутри, обновлённые regression tests/переводы/пути. Запись завершения публикуется отдельным documentation commit.
Checks: fetch refs0/0; SHA256 всех17 runtime/test/localization файлов совпали с tested isolated copy; предыдущие local/isolated build и rendered craft-contrast/day-start/tutorial PASS, stderr пусты. Staged ровно18 разрешённых файлов, diff --check PASS; обычный push PASS, refs0/0. SHA256 двух пользовательских сцен совпадают с HF184: main_menu 97BECA896A74173514B87915BAD731C10FA88AC6CBBCC079C63706F00A2A807F; trade_scene E1BF47D6CA6CC2A6664C2FD0D1329B6C2D8BCFCF27BE6775F6A9E2A4719EC8A1.
Acceptance: обе готовые правки доступны в main, основные сцены остались локально вне коммита.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых.
Next: пользовательская проверка опубликованной версии.
```

### HF-186 — общие часы поверх мини-игр

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF185 DONE; пересечений нет.

CP-HF-186 read-set: текущий фронт; scripts/Days/DayCycleController.cs; scripts/UI/{EmbroideryGame,FirstTradeTutorial,PauseMenu}.cs; scripts/Presentation/MonochromePresentation.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.{Crafting,DayStart,DayExpiry,DayStartGate,Days,Debug,DebugBalance,Economy,Helpers,Menu,Presentation,Ui,Tutorial}.cs (ссылки на часы и focused checks); localization/en.tres; основные сцены только hash. Write-set: ORCHESTRATION_PLAN.md; DayCycleController.cs (слой часов, блокировка input при craft); EmbroideryGame.cs (убрать duplicate day timer, header); FirstTradeTutorial.cs (пути); перечисленные test файлы (пути, regression); localization/en.tres (удалить неиспользуемый перевод); .local/checks/hf186; isolated copy тех же файлов. Out of scope: source/, пользовательские сцены, gameplay/баланс/schema, commit/push.

Приёмка: один общий clock перед craft UI во всех состояниях обеих мини-игр, без дублирования дня внутри modal; таймер самой мини-игры сохранён. Clock не завершает день во время craft, после закрытия обычное управление возвращается; pause/cards перекрывают clock. RU/EN/три размера. Проверки: local/isolated build; rendered craft-contrast/day-start/tutorial; screenshot; diff/hash.

```text
Task: HF-186
Recommended status: REVIEW → принято координатором, DONE
Changed: DayCycleController — существующие часы и кнопка перенесены в runtime CanvasLayer HUD/DayClock, слой111 при craft и HUD+1 иначе; ввод отключён в craft, общий EndDay также guarded. EmbroideryGame — удалён duplicate day timer/heading, три stat cards сдвинуты вправо, обновление presentation при open/close; время самой мини-игры сохранено. FirstTradeTutorial и test ссылки обновлены на новый путь; переводы очищены. Crafting regressions проверяют actual bright pixels часов вне modal, слои/отсутствие дубля/клики/возврат ввода; устаревшие copy-clock ожидания заменены. DayStart fixture явно отключает onboarding, которому теперь запрещено закрытие дня HF185.
Checks: local/isolated build PASS (0 warnings/errors); rendered --craft-contrast-checks PASS (2 игры ×4 состояния ×2 языка ×3 размера, реальные Start/Back/Collect/Clock); --day-start-checks PASS (hover, реальное закрытие, переход/карты, resize/reload/legacy); --tutorial-checks PASS (полный onboarding с новой блокировкой и reload/replay/skip). stderr пусты. Просмотрен embroidery-ready-ru: общий clock яркий поверх затемнения, duplicate отсутствует; screenshots/logs .local/checks/hf186. git diff --check PASS; SHA256 обеих основных сцен прежние.
Acceptance: общие часы постоянно видны во время мини-игры; внутри остаётся только собственный таймер задания; обучение блокирует завершение дня.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых; пользовательские сцены сохранены; commit/push не выполнялись.
Next: пользовательская проверка часов во время мини-игры.
```

### HF-185 — запрет завершать день во время обучения

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF184 DONE; пересечений нет.

CP-HF-185 read-set: текущий фронт; scripts/Days/DayCycleController.cs; scripts/UI/FirstTradeTutorial.cs; scripts/TradeScene.cs (EndDay/StatusText read-only); tests/Integration/ProjectTest.{Tutorial,Helpers,Presentation}.cs; tests/ProjectTest.cs (runner); localization/en.tres; основные сцены только hash. Write-set: ORCHESTRATION_PLAN.md; scripts/Days/DayCycleController.cs; localization/en.tres; tests/Integration/ProjectTest.Tutorial.cs; .local/checks/hf185; isolated .local/checks/git-review-20261118/project (те же файлы). Out of scope: source/, основные сцены, TradeScene, баланс/таймер/schema/шаги обучения, commit/push.

Приёмка: во время активного обучения EndDay/часы/legacy-кнопка не закрывают и не переключают день, сообщают причину; Start доступен. Проверка после reload, в Summary и после завершения/пропуска; обучение полностью проходимо. Проверки: local/isolated build; rendered --tutorial-checks; git diff --check; SHA256 сцен.

```text
Task: HF-185
Recommended status: REVIEW → принято координатором, DONE
Changed: DayCycleController — guard в общем EndDay после разрешённого Start, проверяет IsActive обучения; локализованные tooltip и трёхсекундная надпись на часах. EN переводы; Tutorial tests проверяют реальный click, direct и legacy signal в Craft/после reload/Offer/Summary, прежние ресурсы/день/таймер/шаг; после completion/skip день закрывается нормально.
Checks: local/isolated build PASS (0 warnings/errors); rendered --tutorial-checks PASS, stderr пуст; полный tutorial/reload/replay/skip lifecycle пройден. Финальный diff/hash проверяется вместе с последующей задачей часов HF186.
Acceptance: игрок не завершает день при активном обучении, Start и обычная игра после выхода доступны.
Read-set exception: DayAnnouncementOverlay.cs только публичный API для диагностики подходящей подсказки; не изменён.
Risks/blocker: нет новых; commit/push не выполнялись.
Next: HF186 — общие часы поверх мини-игры.
```

### HF-184 — публикация подписей сигарет и компоновки мини-игр

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF182/HF183 DONE; пользователь разрешил commit/push; пересечений нет.

CP-HF-184 read-set: текущий фронт, готовые diff/evidence HF182/HF183, Git status/refs, основные сцены только hash. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md; публикация localization/en.tres, scripts/UI/{EmbroideryGame,ProgressPanel}.cs, tests/Integration/ProjectTest.{Crafting,Ui}.cs. Out of scope: source/, пользовательские основные сцены, новые изменения поведения, force push.

Приёмка: проверенные изменения HF182/HF183 опубликованы в origin/main, refs совпадают; пользовательские сцены сохранены вне коммита. Проверки: fetch/ref, hash пяти файлов с tested isolated copy, готовые build/rendered checks, staged diff --check, push/readback/hash сцен.

```text
Task: HF-184
Recommended status: REVIEW → принято координатором, DONE
Changed: ea3c755 опубликован в origin/main: постоянные подписи на сигаретах и новая компоновка вышивки/полировки, переводы и regression tests. Запись завершения публикуется отдельным documentation commit.
Checks: fetch refs0/0; SHA256 всех пяти runtime/test/localization файлов совпали с tested isolated copy. Предыдущие local/isolated build и rendered progress-toggle/quota-countdown/tutorial/craft-contrast PASS. Staged diff --check PASS; staged только шесть разрешённых файлов. Обычный push PASS, refs0/0. SHA256 основных сцен сохранены: main_menu 97BECA896A74173514B87915BAD731C10FA88AC6CBBCC079C63706F00A2A807F; trade_scene E1BF47D6CA6CC2A6664C2FD0D1329B6C2D8BCFCF27BE6775F6A9E2A4719EC8A1.
Acceptance: обе готовые правки доступны в main; пользовательские сцены вне коммита.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых.
Next: пользовательская проверка опубликованной версии.
```

### HF-183 — компоновка вышивки и полировки

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-182 DONE; пересечений нет.

CP-HF-183 read-set: текущий фронт; scripts/UI/EmbroideryGame.cs; tests/Integration/ProjectTest.{Crafting,Helpers,Presentation}.cs; tests/ProjectTest.cs (runner); localization/en.tres; пользовательский screenshot; scenes/{main_menu,trade_scene}.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/EmbroideryGame.cs (presentation); tests/Integration/ProjectTest.Crafting.cs; localization/en.tres (подписи); .local/checks/hf183; изолированный .local/checks/git-review-20261118/project (те же файлы). Out of scope: source/, gameplay/баланс/schema/арт/основные сцены/предыдущие правки/commit/push.

Приёмка: компактная общая строка названия и статистики, подписанные время дня и качество, увеличенное поле без скачка при старте, подсказка во время игры; Start слева выделен, Collect по центру. RU/EN, обе мини-игры, ready/playing/timeout/success и разные размеры окна: читаемый текст, без пересечений. Проверки: local/isolated build; rendered --craft-contrast-checks; screenshots; git diff --check; SHA256 основных сцен.

```text
Task: HF-183
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/UI/EmbroideryGame.cs — общая верхняя строка названия/трёх карточек/подписанного времени дня, увеличенное поле, компактная шкала с заголовком и процентом, согласованный footer с подсказкой и действиями. Геометрия поля постоянна при старте; подсказка остаётся во время игры. Заголовки показателей увеличены для читаемости. localization/en.tres — два перевода. ProjectTest.Crafting.cs — границы/отступы/header/footer/перевод/стабильность поля и третий размер окна.
Checks: local/isolated dotnet build PASS (0 warnings/errors); rendered --craft-contrast-checks PASS: обе мини-игры × RU/EN ×960×540/1280×720/1000×800 ×ready/playing/timeout/success; реальные Start/Cancel/Collect, контраст текста/кнопок/шкалы и отсутствие повторной выдачи. Финальный stderr пуст. Просмотрены embroidery-ready-ru и polishing-playing-ru; screenshots/logs в .local/checks/hf183. git diff --check PASS; SHA256 двух пользовательских сцен совпадают с исходным snapshot.
Acceptance: единая компактная компоновка обеих мини-игр, понятные подписи, больше полезного поля, нет прежней большой пустоты во время игры.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых; предыдущие правки и пользовательские сцены сохранены; commit/push не выполнялись.
Next: пользовательская проверка нового вида мини-игр.
```

### HF-182 — постоянный текст на сигаретах при движении

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-181 DONE; пересечений нет.

CP-HF-182 read-set: текущий фронт; scripts/UI/ProgressPanel.cs; scenes/UI/CigaretteProgress.tscn; tests/Integration/ProjectTest.{Ui,Presentation,Helpers}.cs; tests/ProjectTest.cs (focused checks); scenes/{main_menu,trade_scene}.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/ProgressPanel.cs (только visibility captions); tests/Integration/ProjectTest.Ui.cs (progress caption regression); .local/checks/hf182; изолированный .local/checks/git-review-20261118/project (те же runtime/test файлы). Out of scope: source/, геометрия/арт/сцены/скорость анимации, квоты/баланс/schema/прочие изменения/commit/push.

Приёмка: Caption постоянно видим как дочерний элемент сигареты, без отдельного Hide/Show при готовности/hover/уходе; в закрытом виде естественно перекрывается передней частью пачки/границей экрана. Во время выдвижения и возврата текст остаётся на прежнем месте относительно каждой сигареты. RU/EN, текущие значения/пауза/hover/pin/reload сохраняются. Проверки: local/isolated build; rendered --progress-toggle-checks/--quota-countdown-checks/--tutorial-checks; screenshot движения; git diff --check; SHA256 основных сцен.

```text
Task: HF-182
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/UI/ProgressPanel.cs — удалены Caption.Hide в Ready и Caption.Visible=expanded в SetExpanded; подписи постоянно остаются дочерними элементами сигарет и естественно перекрываются передней частью пачки при возврате. tests/Integration/ProjectTest.Ui.cs — постоянная видимость, parent/локальное положение/alpha в покое/выдвижении/реальном возврате/после возврата; fixture явно раскрывает пачку перед проверкой ретракции и ждёт наблюдаемое промежуточное положение.
Checks: local/isolated dotnet build PASS (0 warnings/errors); rendered --progress-toggle-checks PASS: caption attachment во всех фазах, RU/EN, hover/stagger/pin/keyboard/pause/reload/recovery; --quota-countdown-checks PASS; --tutorial-checks PASS. Финальные stderr пусты. Просмотрен captions-during-retraction.png: текст виден на движущихся сигаретах. git diff --check PASS; SHA256 обеих пользовательских основных сцен сохранён. Evidence .local/checks/hf182.
Acceptance: текст не отключается при начале возврата или в закрытой пачке; всегда двигается вместе с соответствующей сигаретой.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых; commit/push не выполнялись.
Next: пользовательская проверка текста при выдвижении и возврате сигарет.
```
### HF-181 — публикация изменений до старта дня, квот и интерфейса комнат

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF176/HF177/HF178/HF179/HF180 DONE; пользователь разрешил commit/push; пересечений нет.

CP-HF-181 read-set: текущий фронт; Git status/diff/refs; готовые изменения и evidence HF176…HF180; scenes/{main_menu,trade_scene}.tscn только hash. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md; публикация готовых docs/GAMEPLAY.md, localization/en.tres, scripts/Camera/WorldCamera.cs, scripts/Days/DayCycleController.cs, scripts/Presentation/RoomDoorwayPresentation.cs, scripts/TradeScene.cs, scripts/UI/{DayStartGate.cs/.uid,EmbroideryGame.cs,InventoryUI.cs,ProgressPanel.cs,ZoomResponsiveUi.cs}, tests/ProjectTest.cs, tests/Integration/ProjectTest.{Camera,Crafting,DayStart,DayStartGate,Inventory,Resources,Ui}.cs и ProjectTest.DayStartGate.cs.uid; .local/checks/hf181. Out of scope: source/, пользовательские основные сцены, новые изменения поведения/баланса/schema, force push.

Приёмка: все готовые проверенные правки опубликованы обычным push в origin/main; HEAD=origin/main; пользовательские сцены побайтно сохранены вне коммита. Проверки: SHA25619 runtime/test/docs файлов совпадают с tested isolated copy,2 новых UID валидны; прежние build/gate/inventory/doorway/quota/craft/titles/focus/tutorial PASS; staged diff --check, push/ref/status/hash readback.

```text
Task: HF-181
Recommended status: REVIEW → принято координатором, DONE
Changed: HF176…HF180 опубликованы коммитом2c03172 в origin/main: блокировка игровых действий до Start и локализованная подсказка, тёмные шторы, корректный срок квоты, Start слева с выделением, компактные названия комнат без подзаголовков и скрытие при отдалении; regression tests/docs/UID включены. Пользовательские основные сцены вне коммита. Запись завершения публикуется отдельным documentation commit.
Checks: fetch0/0; все19 runtime/test/docs SHA256 совпадают с tested isolated copy;2 UID прочитаны и валидны. Прежние local/isolated build, rendered day-start-gate/inventory/room-doorway/quota-countdown/craft-contrast/room-title/room-focus/tutorial PASS. Staged diff --check PASS; staged paths точно совпадают с22 разрешёнными файлами. Обычный push origin main PASS; refs0/0. SHA256 двух пользовательских основных сцен совпадают со snapshot HF181; только они остались изменёнными.
Acceptance: все готовые проверенные правки доступны в main на GitHub; main_menu.tscn/trade_scene.tscn сохранены локально побайтно.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых.
Next: пользовательская проверка опубликованной версии.
```
### HF-180 — компактные названия комнат без подзаголовков, скрытие в общем ракурсе

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-179 DONE; пересечений нет.

CP-HF-180 read-set: текущий фронт; scripts/UI/ZoomResponsiveUi.cs; scripts/Camera/WorldCamera.cs; scenes/trade_scene.tscn только Header/NameLabel/HintLabel/ShowroomHeader/ShowroomLabel/ShowroomHint read-only и hash; scripts/TradeScene.cs только RefreshCraftLabels/RefreshShowcases read-only; tests/Integration/ProjectTest.{Camera,Helpers,Presentation,Ui}.cs; tests/ProjectTest.cs; docs/GAMEPLAY.md; пользовательский screenshot; scenes/main_menu.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/ZoomResponsiveUi.cs; scripts/Camera/WorldCamera.cs (только публичное readonly OverviewZoom); tests/Integration/ProjectTest.Camera.cs; tests/ProjectTest.cs; docs/GAMEPLAY.md; .local/checks/hf180; изолированный .local/checks/git-review-20261118/project (те же runtime/test/docs файлы). Out of scope: source/, пользовательские основные сцены/позиции комнат/арт, баланс/schema/новый gameplay/прочие предыдущие изменения/commit/push.

Приёмка: названия и подложки всех существующих комнат скрыты в общем ракурсе, проявляются при приближении относительно актуального overview (1,2→1,6×) при разных размерах окна; подзаголовки всегда скрыты, включая language/reload/refresh. Плашки уже/ниже, центр сохранён, RU/EN название без обрезки; цена остаётся в hover подсказке кнопки. Камера/кнопки/пауза/обучение прежние. Проверки: build; rendered --room-title-checks/--room-focus-checks/--tutorial-checks; screenshots; git diff --check; SHA256 пользовательских сцен.

```text
Task: HF-180
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/UI/ZoomResponsiveUi.cs — подзаголовки исключены из reveal и постоянно скрыты; плашки под локализованный текст+96px padding, максимум65% прежней ширины,160px высота в local room units, центр сохранён. Layout обновляется при языке/resize, без накопленного уменьшения; существующий PriceHint не изменён. Заголовки/фон скрываются до1,2× актуального OverviewZoom и плавно появляются к1,6×. WorldCamera — readonly OverviewZoom поверх существующего MinZoom. Новый --room-title-checks и обновлён старый zoom contract; docs описывают управление/цену/пороги.
Checks: local/isolated dotnet build PASS (0 warnings/errors); rendered --room-title-checks PASS:4 room headings × RU/EN ×960×540/1280×720/1000×800, реальный overview скрывает, середина fade частичная, closeup непрозрачен; ширина<=66%, высота<=161, центр/границы/весь локализованный текст помещаются; refresh не показывает subtitle. --room-focus-checks PASS для6 комнат, UI/pause/modal; --tutorial-checks PASS. Все stderr пусты. Просмотрены atelier-ru и overview, сохранён music-en. git diff --check PASS; SHA256 обеих пользовательских основных сцен сохранён. Evidence .local/checks/hf180.
Acceptance: на отдалении названия исчезают при всех проверенных размерах окна, вблизи показываются компактные плашки без подзаголовков. Цена остаётся в hover подсказке у создания.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых; предыдущие изменения сохранены; commit/push не выполнялись.
Next: пользовательская проверка общего ракурса и приближения к ателье.
```
### HF-179 — «Начать» слева и контрастное выделение в мини-играх

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-178 DONE; пересечений нет.

CP-HF-179 read-set: текущий фронт; scripts/UI/EmbroideryGame.cs; scripts/Presentation/ButtonFeedbackController.cs (локальный feedback); tests/Integration/ProjectTest.Crafting.cs; tests/Integration/ProjectTest.{Helpers,Presentation}.cs; tests/ProjectTest.cs (craft checks entrypoint); пользовательский screenshot; scenes/{main_menu,trade_scene}.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/EmbroideryGame.cs (только порядок/стили кнопок); tests/Integration/ProjectTest.Crafting.cs (craft contrast/layout/input tests); .local/checks/hf179; изолированный .local/checks/git-review-20261118/project (те же runtime/test файлы). Out of scope: source/, игровые правила/цены/время/schema/переводы/пользовательские сцены/предыдущие изменения/commit/push.

Приёмка: перед стартом «Начать» слева, «Назад» справа, Start заметно выделен чёрной заливкой с белой надписью в grayscale; hover/press/focus читаемы, начальный keyboard focus остаётся на Start. Во время игры кнопки скрыты; итоговое «Забрать» как прежде по центру и нейтральное. Общий интерфейс вышивки/полировки согласован, RU/EN960×540/1280×720 без обрезки. Реальный Start начинает/списывает один раз, Cancel бесплатно закрывает, collect не дублирует предмет. Проверки: build; rendered --craft-contrast-checks и --tutorial-checks; pixel before/after/buttons и screenshots; git diff --check; SHA256 основных сцен.

```text
Task: HF-179
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/UI/EmbroideryGame.cs — Start слева/Back справа в общем ready layout вышивки и полировки. Start выделен чёрной заливкой, белым текстом,2px рамкой и различимыми hover/press/focus; стили переключаются только при смене ready state, на collect возвращается прежний нейтральный стиль/центр. tests/Integration/ProjectTest.Crafting.cs — проверка порядка/focus/bounds/цветов во всех состояниях и actual grayscale pixels, реальный левый Start запускает мини-игру и списывает один раз, Cancel/Collect прежние; screenshots ready RU/EN.
Checks: local/isolated dotnet build PASS (0 warnings/errors); rendered --craft-contrast-checks PASS: обе мини-игры, RU/EN960×540/1280×720, ready/playing/timeout/success, black fill/white Start, порядок/фокус, реальные Start/Cancel/Collect, без дублирования предметов; --tutorial-checks PASS; stderr обеих групп пусты. Просмотрен embroidery-ready-ru, сохранён polishing-ready-en. git diff --check PASS; SHA256 двух пользовательских основных сцен сохранён. Evidence .local/checks/hf179.
Acceptance: Start слева и визуально основной, Back справа; использование мыши/клавиатуры и сценарии мини-игр сохранены.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых; предыдущие изменения сохранены; commit/push не выполнялись.
Next: пользовательская проверка кнопок перед стартом мини-игры.
```
### HF-178 — корректный срок квоты при окончании дня и проигрыше

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-177 DONE; пересечений нет.

CP-HF-178 read-set: текущий фронт; scripts/UI/ProgressPanel.cs; scripts/TradeScene.cs (AdvanceToNextDay read-only); scripts/Days/DayCycleController.cs (Tick/EndDay read-only); tests/Integration/ProjectTest.{Ui,Helpers,Presentation}.cs; tests/ProjectTest.cs; localization/en.tres; docs/GAMEPLAY.md; config/economy.v1.json только quota/days; scenes/{main_menu,trade_scene}.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/ProgressPanel.cs; tests/Integration/ProjectTest.Ui.cs; tests/ProjectTest.cs (quota-countdown entrypoint); localization/en.tres; docs/GAMEPLAY.md; .local/checks/hf178; изолированный .local/checks/git-review-20261118/project (те же runtime/test/localization/docs файлы). Out of scope: source/, сумма/даты квоты/баланс/порядок списания/TradeScene/схема/пользовательские сцены/прочие незакоммиченные изменения/commit/push.

Приёмка: первый/финальный deadline во время доступного последнего дня обозначен «сегодня», на00:00 нет обещания ещё одного дня, проигрыш явно сообщает истёкший срок и корректен после reload; следующий этап/победа/checkpoint/new game сбрасывают прежний статус; более ранние дни сохраняют счётчик включая текущий пока он доступен, закрытый день не учитывается. RU/EN. Проверки: build; rendered --quota-countdown-checks/--progress-toggle-checks/--tutorial-checks; screenshots; git diff --check; hash основных сцен.

```text
Task: HF-178
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/UI/ProgressPanel.cs — закрытый день не добавляется к числу доступных дней; последний доступный день подписан «Квота: сегодня», закрытый deadline «Квота: сейчас», проигрыш «Срок квоты истёк». RefreshCaptions ждёт Ready родителя перед чтением DayClosed. localization/en.tres —3 перевода. tests/Integration/ProjectTest.Ui.cs — обновлены deadline/failure ожидания; добавлен полный lifecycle обеих квот и custom first quota1000; tests/ProjectTest.cs — --quota-countdown-checks. Docs объясняют сроки.
Checks: local/isolated dotnet build PASS (0 warnings/errors). Rendered --quota-countdown-checks PASS: первый и финальный сроки, живой предпоследний день2/закрытый1, последний день «сегодня» до последней0,01с,00:00 «сейчас», реальный проигрыш без дополнительного дня, RU/EN, failed reload/checkpoint, final payment/victory/new run. Rendered --progress-toggle-checks PASS; --tutorial-checks PASS; успешные stderr пусты. Просмотрены first-quota-expired-ru и final-quota-expired-en. git diff --check PASS; hash двух пользовательских основных сцен сохранён. Evidence .local/checks/hf178.
Acceptance: пример пользователя больше не показывает ещё1день после поражения; во время дня4 явно обозначен сегодняшний срок. Суммы/даты/правила оплаты не менялись; иные прежние изменения сохранены.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых; commit/push не выполнялись.
Next: пользовательская проверка подписи квоты в последний день и после проигрыша.
```
### HF-177 — приглушённые шторы закрытых комнат

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-176 DONE; пересечений нет.

CP-HF-177 read-set: текущий фронт; scripts/Presentation/RoomDoorwayPresentation.cs; tests/Integration/ProjectTest.Resources.cs (только TestRoomDoorways); tests/Integration/ProjectTest.Presentation.cs (capture/bounds helpers); tests/ProjectTest.cs (room-doorway entrypoint); пользовательский screenshot; scenes/{main_menu,trade_scene}.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; scripts/Presentation/RoomDoorwayPresentation.cs; tests/Integration/ProjectTest.Resources.cs (только TestRoomDoorways); .local/checks/hf177; изолированный .local/checks/git-review-20261118/project (те же runtime/test файлы). Out of scope: source/, пиксели арта/пользовательские сцены, предыдущие HF176 изменения, gameplay/баланс/schema/commit/push.

Приёмка: обе закрытые шторы заметно темнее, фактура читается; затемнение только текстуры, кнопка открытия и остальной мир прежние; нет нового перехвата ввода; unlock скрывает шторы, new game/checkpoint/reload возвращают тёмные. Проверки: local/isolated build; rendered --room-doorway-checks с pixel before/after и lifecycle; --room-focus-checks; screenshot review; git diff --check; hash пользовательских сцен.

```text
Task: HF-177
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/Presentation/RoomDoorwayPresentation.cs — SelfModulate0,55 у обеих LockedCurtain, эквивалент45% чёрного затемнения только ткани без нового перехватчика ввода/изменения кнопок. tests/Integration/ProjectTest.Resources.cs — runtime tint/lifecycle assertion, rendered before/after pixel check обеих штор; unlock fixtures явно начинают день по новому HF176 правилу.
Checks: local/isolated dotnet build PASS (0 warnings/errors); rendered --room-doorway-checks PASS: >100 потемневших светлых пикселей у каждой шторы, фактура видима, исходное закрытие/неуспешный и успешный unlock/reload/checkpoint/new game. --room-focus-checks PASS:6 комнат/2 размера/pause/modal/GUI. Оба stderr пусты. Просмотрен curtains-darkened.png. git diff --check PASS; SHA256 обеих пользовательских основных сцен сохранены. Evidence .local/checks/hf177.
Acceptance: обе шторы приглушены приблизительно на45%; кнопка открытия остаётся прежней, после открытия шторы исчезают, при восстановлении закрытых комнат снова тёмные. Двойной клик/ввод работают.
Read-set exception: rg по tests/Integration только для поиска TestRoomDoorways после отсутствующего предполагаемого имени файла; source/ не читался.
Risks/blocker: нет новых; прежние HF176 изменения сохранены; commit/push не выполнялись.
Next: пользовательская визуальная проверка затемнения.
```
### HF-176 — блокировка действий до начала дня и подсказка

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-175 DONE; пересечений нет.

CP-HF-176 read-set: текущий фронт; scripts/TradeScene.cs (action adapters/Ready); scripts/Days/DayCycleController.cs; scripts/UI/{EmbroideryGame,InventoryUI,SlotUI,FirstTradeTutorial}.cs; scripts/Camera/WorldCamera.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.{Helpers,DayStart,Crafting,Camera,Tutorial,Inventory,Presentation}.cs; localization/en.tres; docs/GAMEPLAY.md; ChikagoShop.csproj; .vscode/settings.json; scenes/{main_menu,trade_scene}.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; новый scripts/UI/DayStartGate.cs/.uid; scripts/Days/DayCycleController.cs (инициализация gate/делегат); scripts/TradeScene.cs (только тонкие вызовы gate в action adapters, без новой доменной логики); scripts/UI/{EmbroideryGame,InventoryUI}.cs; tests/ProjectTest.cs; новый tests/Integration/ProjectTest.DayStartGate.cs/.uid; tests/Integration/ProjectTest.{DayStart,Crafting,Camera,Inventory}.cs (актуализация fixtures явным Start); localization/en.tres; docs/GAMEPLAY.md; .local/checks/hf176; изолированный .local/checks/git-review-20261118/project (те же новые/изменённые runtime/test/localization/docs файлы). Out of scope: source/, пользовательские основные сцены, предыдущие изменения/баланс/schema/TradeScene domain extraction/commit/push.

Приёмка: до Start ни создание/открытие мини-игры, ни unlock, ни выбор/перенос/drag предмета, ни продажа/reroll не меняют состояние; прямые публичные вызовы также защищены. Реальные клики (включая disabled controls)/клавиатура/drag показывают короткую RU/EN подсказку нажать Start на часах; повтор не создаёт копии. После Start gate и сообщение снимаются, обычные действия доступны. Камера/двойной клик/zoom, пауза/настройки/информация доступны. Новый день/checkpoint/reload учитывают Started. Проверки: build; rendered --day-start-gate-checks/--day-start-checks/--tutorial-checks/--room-focus-checks и затронутые crafting checks; screenshots RU/EN960/1280; git diff --check, SHA256 пользовательских сцен.

```text
Task: HF-176
Recommended status: REVIEW → принято координатором, DONE
Changed: новый scripts/UI/DayStartGate.cs — общий gate по DayStarted, перехват действий через mouse/keyboard с учётом foreground GUI, включая disabled кнопки и inventory; единая неперехватывающая ввод подсказка3,5с над инвентарём. DayCycleController создаёт gate, делегирует проверку и немедленно скрывает сообщение при Start/reset. TradeScene содержит только тонкие вызовы RequireStartedDay в adapters craft/acquire/unlock/select/place/reroll/accept; новая доменная логика туда не добавлена. EmbroideryGame.Open и InventoryUI.CanTransfer защищены. RU/EN translation/docs обновлены. Новый --day-start-gate-checks; fixtures DayStart/Crafting/Camera/Inventory явно запускают день перед разрешёнными действиями, повторный EndDay не закрывает уже запущенный fixture день.
Checks: local/isolated dotnet build PASS (0 warnings/errors). Rendered --day-start-gate-checks PASS: прямые вызовы/настоящие клики по craft/unlock/disabled showcase/inventory, keyboard Space/F, запрет drag, неизменность money/energy/items/time, RU/EN960×540/1280×720, отсутствие перекрытия tutorial, foreground GUI поверх world button, timeout/Start dismiss, работа после Start и running reload, повторный gate на новом дне/checkpoint; навигация/пауза доступны. --day-start-checks PASS; финальный --tutorial-checks PASS; --room-focus-checks PASS на6 комнатах в2 размерах; --craft-contrast-checks PASS; --inventory-aspect-checks PASS включая реальные drag/drop/cancel/stale payload/Shift-transfer после старта. stderr успешных проверок пуст. Просмотрены notice RU960/EN1280. git diff --check PASS; SHA256 обеих пользовательских основных сцен сохранены. Evidence .local/checks/hf176.
Acceptance: до Start разрешена навигация и служебный UI, игровые действия не меняют состояние и объясняют, как начать день. После Start действия доступны без повторной загрузки. Новый день/reload/checkpoint сохраняют нужный gate.
Read-set exception: проверена сцена scenes/UI/FirstTradeTutorial.tscn (только header/layer) для диагностики перекрытия подсказки; source/ не читался. Координатор расширил write-set Inventory fixture для явного Start перед unlock и предотвращения повторного EndDay после подготовки предметов.
Risks/blocker: нет новых; commit/push не выполнялись.
Next: пользовательская проверка попытки создания до «Начать».
```
### HF-175 — публикация пачки и двойного клика комнаты

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF172/HF173/HF174 DONE, пользователь разрешил commit/push; пересечений нет.

CP-HF-175 read-set: текущий фронт; Git status/diff/refs; готовые изменения и evidence HF172/HF173/HF174; scenes/{main_menu,trade_scene}.tscn только hash. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md; публикация готовых docs/GAMEPLAY.md, scenes/UI/CigaretteProgress.tscn, scripts/Camera/WorldCamera.cs, scripts/UI/ProgressPanel.cs, tests/Integration/ProjectTest.{Camera,Tutorial,Ui}.cs, tests/ProjectTest.cs; .local/checks/hf175. Out of scope: source/, пользовательские основные сцены, новые изменения поведения/баланса/schema, force push.

Приёмка: готовые проверенные файлы опубликованы обычным push, HEAD=origin/main; пользовательские основные сцены побайтно сохранены локально вне коммита. Проверки: SHA256 runtime/test/docs с tested isolated copy; прежние build/progress/tutorial/room-focus/camera-limits PASS; staged diff --check, push/ref/status/hash readback.

```text
Task: HF-175
Recommended status: REVIEW → принято координатором, DONE
Changed: HF172/HF173/HF174 опубликованы коммитом77e78ea в origin/main: исходная ориентация и совмещение слоёв пачки, выступающие кончики, hover по ним и плавное наведение на6 комнат по двойному клику, regression tests и gameplay docs. Основные пользовательские сцены вне коммита. Запись завершения публикуется отдельным documentation commit.
Checks: fetch0/0; SHA256 всех8 runtime/test/docs файлов совпадает с tested isolated copy. Прежние build/progress-toggle/tutorial/room-focus/camera-limits PASS. Staged diff --check PASS; обычный push origin main PASS; refs0/0; две пользовательские сцены сохранены по SHA256 snapshot HF175; только они остаются изменёнными.
Acceptance: проверенные изменения доступны в main на GitHub; локальные пользовательские main_menu/trade_scene сохранены.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых.
Next: пользовательская проверка опубликованной версии.
```
### HF-174 — наведение камеры по двойному клику комнаты

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-173 DONE, пересечений нет.

CP-HF-174 read-set: текущий фронт; scripts/Camera/WorldCamera.cs; scenes/trade_scene.tscn (World/комнаты/RoomFrame/Camera2D read-only); scripts/Presentation/RoomDoorwayPresentation.cs; scripts/UI/FirstTradeTutorial.cs; tests/Integration/ProjectTest.{Camera,CameraLimits,RoomFrames,Helpers,Presentation}.cs; tests/ProjectTest.cs; .vscode/settings.json; ChikagoShop.csproj; project.godot; docs/GAMEPLAY.md (управление); scenes/main_menu.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; scripts/Camera/WorldCamera.cs; tests/Integration/ProjectTest.Camera.cs; tests/ProjectTest.cs; docs/GAMEPLAY.md; .local/checks/hf174; изолированный .local/checks/git-review-20261118/project (те же runtime/test/docs файлы). Out of scope: source/, пользовательские сцены/прежние изменения, TradeScene/домен/баланс/schema/арт/commit/push.

Приёмка: двойной левый клик по фону комнаты плавно центрирует и приближает камеру до существующего CloseupSize; все6 комнат определяются по реальным scene bounds, клики по UI/кнопкам не наводят камеру. Одиночный клик/drag/wheel прежние; pause/modal/NavigationLocked блокируют; resize и границы камеры сохраняются. Проверки: local/isolated dotnet build; rendered --room-focus-checks/--camera-limits-checks/--tutorial-checks; screenshot review; git diff --check; SHA256 пользовательских сцен.

```text
Task: HF-174
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/Camera/WorldCamera.cs — double-left-click в unhandled world input ищет видимую RoomFrame по реальным world bounds, плавно FocusOn(center, CloseupSize,0,45с). Для закрытой витрины Stop-overlay обрабатывается только double-click его фона через GuiInput; кнопки/drag фильтруются прежним GUI. Подписки снимаются в ExitTree. tests/Integration/ProjectTest.Camera.cs — six-room regression, два viewport, плавность/центр/zoom, настоящий hover/click, одиночный клик, HUD и showcase UI, pause-flight, navigation lock, modal и отсутствие ложных tutorial pan/zoom событий; tests/ProjectTest.cs — --room-focus-checks; docs/GAMEPLAY.md — управление.
Checks: local/isolated dotnet build PASS (0 warnings/errors). Rendered --room-focus-checks PASS для всех6 комнат включая закрытые в1280×720/960×540. Rendered --camera-limits-checks PASS: standalone camera, cursor zoom anchor, drag, GUI isolation, pause lifecycle, limits/resize. Rendered --tutorial-checks PASS: полный walkthrough/practice/flights/пауза/reload. Все verified stderr пусты. git diff --check PASS; SHA256 обеих пользовательских основных сцен сохранены. Просмотрен cashier-1280: фокус соответствует пользовательскому примеру; evidence .local/checks/hf174.
Acceptance: двойной клик по фону любой из6 комнат за0,45с приближает и центрирует камеру; одиночный клик не меняет ракурс, UI/modal/пауза/tutorial lock сохранены, ручной drag/wheel и границы камеры работают.
Read-set exception: для проверки команды закрытия crafting fixture прочитаны только сигнатуры scripts/UI/EmbroideryGame.cs; тест использует прежний Escape helper. source/ не читался.
Risks/blocker: нет новых; commit/push не выполнялись.
Next: пользовательская проверка двойного клика в игре.
```
### HF-173 — исходная ориентация пачки по пользовательскому образцу

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-172 DONE; пересечений нет.

CP-HF-173 read-set: текущий фронт и CP-HF-172; scenes/UI/CigaretteProgress.tscn; scripts/UI/ProgressPanel.cs; tests/Integration/ProjectTest.{Ui,Presentation,Helpers}.cs; tests/ProjectTest.cs; art/ui/cigarette_{pack_front,pack_back,stick}.png; две пользовательские картинки в чате; .vscode/settings.json; project.godot; ChikagoShop.csproj; scenes/{main_menu,trade_scene}.tscn только hash. Write-set: ORCHESTRATION_PLAN.md; scenes/UI/CigaretteProgress.tscn; scripts/UI/ProgressPanel.cs; tests/Integration/ProjectTest.{Ui,Tutorial}.cs (в Tutorial только начальная позиция мыши fixture); .local/checks/hf173; изолированный .local/checks/git-review-20261118/project (те же runtime/test файлы). Out of scope: source/, изменение PNG, пользовательские основные сцены, домен/TradeScene/schema/баланс/commit/push.

Приёмка: убрать поворот90°; обе части без искажения пропорций совмещены по координатам общего исходного canvas; белый фасад и верхняя грань слева, чёрная боковина справа; сигареты между слоями, кончики немного выходят за чёрную боковину. Hover доступен по всем видимым кончикам; подписи скрыты в покое, читаемы при раскрытии; прежние hover/pin/pause/локали/размеры/обучение работают. Проверки: local/isolated dotnet build; rendered --progress-toggle-checks/--tutorial-checks; screenshot review; git diff --check; hash пользовательских сцен.

```text
Task: HF-173
Recommended status: REVIEW → принято координатором, DONE
Changed: scenes/UI/CigaretteProgress.tscn — поворот90° убран у обеих частей; общий масштаб0,16 и единый origin исходного canvas, верхняя грань сверху и чёрная боковина справа. Сигареты y32/88/144, кончики x72 выступают за заднюю часть x50,32. scripts/UI/ProgressPanel.cs — ClosedX -286, область hover/toggle автоматически включает кончики; подписи скрыты в покое. tests/Integration/ProjectTest.Ui.cs — регрессия исходной ориентации/aspect/canvas alignment, выступающих кончиков/пикселей, hover по кончикам и видимости подписей. tests/Integration/ProjectTest.Tutorial.cs — явная стартовая позиция мыши при создании/reload fixture.
Checks: local/isolated dotnet build PASS (0 warnings/errors); rendered --progress-toggle-checks PASS: orientation/aspect/source alignment, pixel tips/black side, hover на выступающих кончиках, stagger/pause/pin/keyboard/локали/размеры/recovery/reload. Rendered --tutorial-checks PASS после детерминированной инициализации мыши fixture; final stderr пуст. git diff --check PASS; SHA256 обеих пользовательских основных сцен совпадает с before-scenes.json. Просмотрены full-pack-render/collapsed-detail/expanded; evidence .local/checks/hf173.
Acceptance: геометрия соответствует второму пользовательскому изображению: белый фасад и верхняя грань не повёрнуты, чёрная боковина справа, сигареты между слоями, кончики выступают за боковину; прежний узкий край пачки слева экрана сохранён.
Read-set exception: для диагностики cursor-dependent tutorial failure прочитаны tests/Integration/ProjectTest.{Tutorial,ButtonFeedback}.cs; координатор расширил write-set только на инициализацию мыши Tutorial fixture. source/ не читался.
Risks/blocker: нет новых; commit/push не выполнялись.
Next: пользовательская визуальная проверка ориентации.
```
### HF-172 — глубина пачки и видимые кончики сигарет

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-19; HF-171 DONE, активных пересечений нет.

CP-HF-172 read-set: текущий фронт и HF150/151; scenes/UI/CigaretteProgress.tscn; scripts/UI/ProgressPanel.cs; tests/Integration/ProjectTest.{Ui,Presentation,Helpers}.cs; tests/ProjectTest.cs; project.godot; ChikagoShop.csproj; .vscode/settings.json; scenes/trade_scene.tscn только ProgressPanel/Toggle и hash; scenes/main_menu.tscn только hash; art/ui/cigarette_{pack_front,pack_back,stick}.png; публичная Drive-папка 1uVwhfIKYE4L8Hfqf5yk1l9oS-C8EOQnX и её три PNG. Write-set: ORCHESTRATION_PLAN.md; scenes/UI/CigaretteProgress.tscn; scripts/UI/ProgressPanel.cs; tests/Integration/ProjectTest.Ui.cs; .local/checks/hf172; изолированная копия .local/checks/git-review-20261118/project (только те же runtime/test файлы). Out of scope: source/, пиксели PNG, пользовательские основные сцены, TradeScene/домен/баланс/schema, commit/push.

Приёмка: сверить три исходника с Drive; передняя часть закрывает основания сигарет, задняя чёрная часть создаёт глубину за ними; в покое кончики немного выступают из отверстия, при hover три сигареты последовательно выходят из него и возвращаются; подписи читаемы только при раскрытии. Сохраняются hover/pin/keyboard/pause, RU/EN и 960×540/1280×720. Проверки: dotnet build ChikagoShop.csproj; rendered --progress-toggle-checks в изолированном user://; screenshot review; git diff --check; SHA256 пользовательских сцен до/после.

```text
Task: HF-172
Recommended status: REVIEW → принято координатором, DONE
Changed: scenes/UI/CigaretteProgress.tscn — передняя кромка x24, задняя x46; три сигареты в покое заканчиваются на x38 внутри чёрного промежутка. scripts/UI/ProgressPanel.cs — ClosedX -320 синхронно со сценой. tests/Integration/ProjectTest.Ui.cs — draw order, совпадение высоты слоёв, глубина проёма, видимые кончики, перекрытие основания раскрытых сигарет и pixel-проверка каждого кончика на чёрном фоне.
Checks: local/isolated dotnet build PASS (0 warnings/errors); rendered --progress-toggle-checks PASS включая pixel probes, hover/stagger/pause/pin/keyboard, RU/EN960×540/1280×720 и recovery/reload; rendered --tutorial-checks PASS; stderr обеих групп пуст. git diff --check PASS; SHA256 двух пользовательских основных сцен сохранены. Все три Drive PNG скачаны и SHA256 совпадают с локальными PNG. Screenshot review collapsed-detail, expanded RU960/EN1280 PASS. Evidence .local/checks/hf172.
Acceptance: основания сигарет перекрыты передней частью, задняя чёрная часть видна за кончиками; в покое три кончика слегка выступают, при наведении сигареты выходят через проём без отрыва от пачки.
Read-set exception: публичная папка Drive прочитана через Invoke-WebRequest после отказа web-просмотрщика; source/ не читался. Для поиска доступа прочитан plugin-management skill; подключение не понадобилось.
Risks/blocker: нет новых; commit/push не выполнялись.
Next: пользовательская визуальная проверка пачки.
```
### HF-171 — публикация жёлтой рамки, ручной пачки и pan3000

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-169/HF-170 DONE, пользователь разрешил commit/push; активных пересечений нет.

CP-HF-171 read-set: текущий фронт; Git refs/status/diff; готовые изменения и evidence HF169/HF170; обе пользовательские сцены только hash. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md; публикация готовых docs/GAMEPLAY.md, localization/en.tres, scenes/UI/FirstTradeTutorial.tscn, scripts/Presentation/MonochromePresentation.cs, scripts/UI/{FirstTradeTutorial,ProgressPanel}.cs, shaders/monochrome.gdshader, tests/Integration/ProjectTest.{Presentation,Tutorial}.cs; .local/checks/hf171. Out of scope: source/, пользовательские main_menu.tscn/trade_scene.tscn, новая функциональность/баланс/schema/force push.

Приёмка: готовые проверенные изменения опубликованы обычным push, HEAD=origin/main; пользовательские сцены побайтно сохранены локально. Проверки: совпадение runtime/test/scene/shader/localization с tested isolated copy; прежние build/tutorial/progress-toggle/presentation PASS; staged diff --check, push/ref/status/hash readback.

```text
Task: HF-171
Recommended status: REVIEW → принято координатором, DONE
Changed: HF169/HF170 опубликованы коммитом f87a7ae в origin/main: ручное наведение на пачку, жёлтая рамка с узким shader exception, pan3000 и regression tests. Пользовательские основные сцены вне коммита; запись завершения публикуется отдельным documentation commit.
Checks: fetch0/0; SHA256 публикуемых runtime/test/scene/shader/localization совпадают с tested isolated copy. Прежние build/tutorial/progress-toggle/presentation PASS. Staged diff --check PASS; обычный push origin main PASS; HEAD=origin/main0/0; SHA256 пользовательских сцен совпадает со snapshot HF171.
Acceptance: проверенные изменения доступны на GitHub в main, обе пользовательские сцены сохранены локально.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых.
Next: пользовательская проверка опубликованной версии.
```
### HF-170 — порог передвижения3000px

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-169 DONE, активных пересечений нет.

CP-HF-170 read-set: текущий фронт; scripts/UI/FirstTradeTutorial.cs; tests/Integration/ProjectTest.Tutorial.cs; docs/GAMEPLAY.md; Git status/hash для сохранности прежних изменений. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/FirstTradeTutorial.cs (только PanGoal); tests/Integration/ProjectTest.Tutorial.cs (проверки3000px); docs/GAMEPLAY.md (число); .local/checks/hf170 и изолированная копия .local/checks/git-review-20261118/project. Out of scope: source/, остальные предыдущие изменения и пользовательские сцены, zoom/тексты/сохранения/тайминги/commit/push.

Приёмка: шкала движения заполняется за3000 принятых экранных пикселей;300px=10%, partial reload сохраняет10%,2975px ещё не завершает этап,3000px завершает. Остальной tutorial работает. Проверки: build, rendered --tutorial-checks, git diff --check, SHA256 файлов вне write-set.

```text
Task: HF-170
Recommended status: REVIEW → принято координатором, DONE
Changed: PanGoal512→3000; docs/GAMEPLAY.md обновлено; regression проверяет300px=10%, reload10%,2975px<100%,3000px=100%. Остальные настройки/предыдущие изменения сохранены.
Checks: local/isolated dotnet build PASS (0 warnings/errors); rendered --tutorial-checks PASS: новая точная граница/сохранение partial, zoom16/instant transition, полный walkthrough/продажа/skip/replay/new run/reload, ручное наведение на пачку и жёлтая рамка. stderr пуст; git diff --check PASS; SHA256 прежних изменённых файлов вне write-set совпадают с before.json. Evidence .local/checks/hf170.
Acceptance: шкала передвижения требует3000 реальных принятых экранных пикселей.
Read-set exception: нет, source/ не читался; test user:// изолирован.
Risks/blocker: нет новых.
Next: пользовательская проверка; commit/push не выполнялись.
```
### HF-169 — ручное наведение на пачку, жёлтая рамка и более длинный pan

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-168 DONE, активных пересечений нет.

CP-HF-169 read-set: текущий фронт; scripts/UI/{ProgressPanel,FirstTradeTutorial}.cs; scripts/Presentation/MonochromePresentation.cs; shaders/monochrome.gdshader; scenes/UI/{FirstTradeTutorial,CigaretteProgress}.tscn; scenes/trade_scene.tscn только HUD Progress/Toggle read-only; tests/Integration/ProjectTest.{Tutorial,Ui,Presentation,Helpers}.cs; localization/en.tres; docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/ProgressPanel.cs (убрать tutorial auto-expand); scripts/UI/FirstTradeTutorial.cs (pan512 и Summary hint/target); scenes/UI/FirstTradeTutorial.tscn (жёлтый Outline/group); scripts/Presentation/MonochromePresentation.cs и shaders/monochrome.gdshader (сохранить цвет только рамки); tests/Integration/ProjectTest.{Tutorial,Presentation}.cs (в Presentation только Start в clock fixture и ожидание menu warmup перед shutdown для shader regression); localization/en.tres; docs/GAMEPLAY.md; .local/checks/hf169 и изолированная копия .local/checks/git-review-20261118/project. Out of scope: source/, обе основные пользовательские сцены, домен/баланс/schema/WorldCamera, zoom16 и отсутствие итоговой задержки, прочие локальные изменения/commit/push.

Приёмка: Summary не раскрывает пачку автоматически, подсказка просит навести мышь и выделяет её кнопку; настоящее наведение раскрывает, уход закрывает как раньше. Рамка жёлтая в финальном monochrome render, её центр и остальной мир не получают цвет от исключения; pause/modal скрывают её. Pan512px вместо256; zoom/сохранения/остальной tutorial работают. Проверки: build; rendered --tutorial-checks/--progress-toggle-checks/--presentation-checks; pixel yellow и screenshots; git diff --check, SHA256 основных сцен.

```text
Task: HF-169
Recommended status: REVIEW → принято координатором, DONE
Changed: ProgressPanel больше не раскрывает пачку из-за Summary; настоящее наведение/клавиатура/закрепление работают как прежде. Summary просит навести мышь и выделяет ProgressToggleButton вместо всей панели. PanGoal256→512px, zoom16 и немедленный handoff прежние. Outline жёлтый (1;0,85;0), помечен группой; monochrome shader сохраняет только3px границу, её середина не получает новое цветовое исключение. При modal/hidden рамка не сохраняется. Tests/Tutorial —512px, отсутствие auto-expand/reload, hover/leave и pixel yellow/gray-center probe. Tests/Presentation — явный Start перед clock expiry и bounded wait фонового warmup меню перед shutdown; прежний fixture не запускал день и прерывал background load при Quit.
Checks: local/isolated dotnet build PASS (0 warnings/errors). Rendered --tutorial-checks PASS:300px теперь58,59%, partial reload/full512, zoom16/instant handoff, весь walkthrough; жёлтая рамка Start/pack и grayscale red probe в центре pack; Summary остаётся свёрнутым до настоящего mouse motion, раскрывается и закрывается по hover/leave, reload не раскрывает. Rendered --progress-toggle-checks PASS: анимации/языки/клавиатура/победа/проигрыш/reload/recovery. Rendered --presentation-checks PASS после актуализации fixture: grayscale/menu/world/clock alpha/table colors/pause/craft/cards/resize/cleanup; stderr финальных трёх групп пуст. Просмотрены tutorial-yellow-start и tutorial-yellow-pack. git diff --check PASS; SHA256 пользовательских основных сцен совпадают со snapshot. Evidence .local/checks/hf169.
Acceptance: игрок сам наводится на пачку в обучении, указатель жёлтый в финальном рендере, перемещение заполняет шкалу вдвое медленнее.
Read-set exception: нет; write-set Presentation fixture расширен координатором для валидной общей проверки shader после диагностики устаревшего теста. source/ не читался, test user:// изолирован.
Risks/blocker: нет новых. Прежние явное закрепление/клавиатура и раскрытие при терминальном исходе вне обучения сохранены.
Next: пользовательская визуальная приёмка; commit/push не выполнялись.
```
### HF-168 — публикация светлых мини-игр и обновлённого обучения

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-166/HF-167 DONE, пользователь разрешил commit/push; активных пересечений нет.

CP-HF-168 read-set: текущий фронт; Git status/diff/refs; готовый write-set/evidence HF166/HF167; обе пользовательские сцены только hash. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md; публикация готовых docs/GAMEPLAY.md, localization/en.tres, scenes/UI/FirstTradeTutorial.tscn, scripts/UI/EmbroideryGame.cs, scripts/UI/FirstTradeTutorial.cs, tests/Integration/ProjectTest.Crafting.cs, tests/Integration/ProjectTest.Tutorial.cs, tests/ProjectTest.cs; .local/checks/hf168. Out of scope: source/, пользовательские main_menu.tscn/trade_scene.tscn, новая функциональность/баланс/schema/force push.

Приёмка: подготовленный проверенный код опубликован обычным push, HEAD=origin/main; только две пользовательские сцены остаются незакоммиченными с прежними SHA256. Проверки: прежние build/craft-contrast/embroidery-target/tutorial/intro-transition PASS; совпадение публикуемых runtime/test файлов с проверенной isolated копией; staged diff --check, refs/status/hash readback.

```text
Task: HF-168
Recommended status: REVIEW → принято координатором, DONE
Changed: готовые HF166/HF167 опубликованы коммитом7b87ecd в origin/main; светлые мини-игры/чёрные подписи и шкала, сложность учебных шкал×4, короткие RU/EN подсказки, немедленный переход после zoom. Две пользовательские основные сцены вне коммита.
Checks: fetch0/0; публикуемые runtime/test/scene/localization SHA256 совпадают с проверенной isolated копией HF166/HF167; прежние build и rendered craft-contrast/embroidery-target/tutorial/intro-transition PASS. Staged diff --check PASS; обычный push PASS; HEAD=origin/main0/0; пользовательские сцены сохранены по SHA256. Новых behavioral изменений при публикации нет. Запись завершения публикуется отдельным documentation commit.
Acceptance: проверенные изменения и тесты доступны в main, пользовательские сцены сохранены локально.
Read-set exception: нет; source/ не читался.
Risks/blocker: нет новых.
Next: пользовательская проверка опубликованной игры.
```
### HF-167 — сложнее шкалы, короче подсказки, без ожидания после zoom

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-164/HF-166 DONE, активных пересечений нет.

CP-HF-167 read-set: текущий фронт; scripts/UI/FirstTradeTutorial.cs, scenes/UI/FirstTradeTutorial.tscn; scripts/Camera/WorldCamera.cs read-only (события); tests/Integration/ProjectTest.{Tutorial,Menu,Helpers,Presentation}.cs; localization/en.tres; docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/FirstTradeTutorial.cs; scenes/UI/FirstTradeTutorial.tscn; tests/Integration/ProjectTest.Tutorial.cs; localization/en.tres; docs/GAMEPLAY.md; .local/checks/hf167 и изолированная копия .local/checks/git-review-20261118/project. Out of scope: source/, пользовательские основные сцены, незакоммиченный HF166 и прочие файлы, WorldCamera/TradeScene/домен/баланс/schema, commit/push.

Приёмка: pan256px вместо64, zoom16 принятых шагов вместо4 (6,25% каждый); раннее колесо не засчитывается, предельное колесо не даёт прогресс. Подсказки короче RU/EN. После последнего zoom без таймера начинается прежний плавный переход; pause/modal/held drag не прерываются, завершённые legacy cfg и partial reload работают. Проверки: build; rendered --tutorial-checks/--intro-transition-checks, fitting RU/EN960×540/1280×720, git diff --check и SHA256 всех чужих правок.

```text
Task: HF-167
Recommended status: REVIEW → принято координатором, DONE
Changed: PanGoal64→256px, ZoomGoal4→16 событий (6,25% каждый); ProgressBar step0,01 для точных долей. Все основные подсказки tutorial сокращены RU/EN, сохранены риск реролла/цена+энергия/сброс после обучения. Удалены CameraCompletionDelaySeconds и счётчик2с; при выполненном zoom сразу на ближайшем свободном кадре начинается прежний плавный перелёт. Защиты pause/modal/удерживаемого drag остаются, промежуточный этап pan100→zoom0 с0,6с после release сохранён. Старые done-флаги/частичные счётчики совместимы, сохранённая старая фаза ожидания больше не ждёт. Docs обновлены.
Checks: local/isolated build PASS (0 warnings/errors). Rendered --tutorial-checks PASS: прежнее движение100px теперь частичное, pan256/full/reload, zoom1=6,25%/reload и16 событий в обе стороны, немедленный Start+camera tween после16-го события без timer; RU/EN960×540/1280×720 fitting; восстановление старого pending cfg сразу к актуальному шагу; legacy cfg безhandoff; полный walkthrough/продажа/finish/skip/replay/new run/reload и сброс дробного прогресса. Rendered --intro-transition-checks PASS; stderr обеих групп пуст. Просмотрен zoom-zero-ru-960: короткая фраза и шкала помещаются. git diff --check PASS; SHA256 всех прежних изменённых файлов вне write-set совпадает с before.json, включая HF166 и обе пользовательские сцены. Evidence .local/checks/hf167.
Acceptance: обе шкалы требуют в4раза больше действий, тексты короче, дополнительное ожидание после масштабирования убрано.
Read-set exception: нет, source/ не читался; test user:// изолирован.
Risks/blocker: нет новых. На пределах камеры нужно менять направление колеса, поскольку учитываются только принятые шаги.
Next: пользовательская визуальная приёмка; commit/push не выполнялись.
```
### HF-166 — светлые мини-игры с чёрным текстом и контрастной шкалой

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-165 DONE, активных пересечений нет.

CP-HF-166 read-set: текущий фронт; scripts/UI/EmbroideryGame.cs, scripts/UI/PolishingSurface.cs; art/minigames/rank_empty.png и rank_full.png; src/ChikagoShop.Domain/Crafting/{EmbroiderySession,PolishingSession}.cs (контракт качества read-only); tests/Integration/ProjectTest.{Crafting,EmbroideryTarget,Presentation,Helpers}.cs; tests/ProjectTest.cs; Git status/hash обеих пользовательских сцен. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/EmbroideryGame.cs (только палитра общего интерфейса/шкалы); tests/Integration/ProjectTest.Crafting.cs (контраст); tests/ProjectTest.cs (отдельная группа); .local/checks/hf166 и изолированная копия .local/checks/git-review-20261118/project. Out of scope: source/, обе пользовательские сцены, исходные изображения/доменные правила/баланс/schema/тайминги, прочие локальные изменения/commit/push.

Приёмка: общий интерфейс вышивки/полировки светлый; заголовки/счётчики/проценты/кнопки чёрные, rank S–D и чёрная заливка различимы на светлом фоне. Состояния Ready/Playing/timeout/success, RU/EN960×540/1280×720, button hover/focus/pressed; шкала отличает пустую и заполненную часть. Проверки: build; rendered --craft-contrast-checks, --embroidery-target-checks; screenshots/пиксельный контраст/реальные cancel/collect, git diff --check и SHA256 сохранности сцен.

```text
Task: HF-166
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/UI/EmbroideryGame.cs — общий светлый фон вышивки/полировки и белые карточки статистики, чёрные заголовки/значения/подсказки/проценты, светлые кнопки с чёрной надписью для normal/hover/pressed/hover_pressed/focus/disabled. Focus сохраняет контур без заливки. Исходная rank-текстура/буквы S–D теперь лежат на светлом фоне; пустая часть и чёрная заливка различимы. Fallback-шкала приведена к тем же white/black цветам. Только presentation, механика и изображения не менялись. Tests/Integration/ProjectTest.Crafting.cs + tests/ProjectTest.cs — --craft-contrast-checks.
Checks: workspace/isolated dotnet build PASS (0 warnings/errors). Rendered --craft-contrast-checks PASS:2 мини-игры ×2 языка ×2 окна ×4 состояния (ready/playing/timeout/success), реальные pixels светлого фона/карточек/чёрных букв и заполнения шкалы, все font states кнопок black, реальные cancel/collect без дублирования предметов. Rendered --embroidery-target-checks PASS: прежний контраст целей, mouse/touch стежки, все варианты SFX/ready/miss/finished. stderr обеих групп пуст. Просмотрены timeout-снимки вышивки54%/полировки0% в960×540: буквы рангов, пустая/полная часть шкалы и проценты отчётливо видны. git diff --check PASS; SHA256 обеих пользовательских сцен совпадают со snapshot. Evidence .local/checks/hf166.
Acceptance: весь текст мини-игр чёрный на светлом фоне; шкала больше не сливается с панелью, общий стиль двух мини-игр согласован.
Read-set exception: нет; source/ не читался; тесты с изолированным user://.
Risks/blocker: нет новых. Полный default ProjectTest не запускался; проверены профильные rendered группы.
Next: пользовательская визуальная приёмка; commit/push не выполнялись.
```
### HF-165 — публикация исправлений инвентаря, прилавка и обучения

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-152/HF-153/HF-162/HF-163/HF-164 DONE, пользователь разрешил commit/push, активных пересечений нет.

CP-HF-165 read-set: текущий фронт, Git refs/status/diff, готовые изменения и evidence HF-152/153/162/163/164; scenes/main_menu.tscn и scenes/trade_scene.tscn только diff/сохранность. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md; публикация готовых docs/GAMEPLAY.md, localization/en.tres, scenes/UI/FirstTradeTutorial.tscn, scenes/UI/SlotUI.tscn, scripts/Audio/IntroMusicDirector.cs, scripts/Camera/WorldCamera.cs, scripts/CounterWorld.cs, scripts/MainMenu.cs, scripts/UI/FirstTradeTutorial.cs, scripts/Presentation/SceneResourceWarmup.cs/.uid, tests/Integration/ProjectTest.{IntroMusic,Inventory,Menu,Trading,Tutorial}.cs, tests/ProjectTest.cs; .local/checks/hf165 и изолированная копия .local/checks/git-review-20261118/project. Out of scope: source/, пользовательские основные сцены (сохраняются локально вне коммита), новая функциональность/баланс/schema, force push.

Приёмка: полный подготовленный результат опубликован обычным push, HEAD=origin/main, пользовательские обе сцены побайтно сохранены; staged diff --check и rendered inventory-aspect/counter-item/tutorial/intro-transition на точных сценах HEAD без локального пользовательского diff; готовые build/evidence предыдущих задач PASS.

```text
Task: HF-165
Recommended status: REVIEW → принято координатором, DONE
Changed: готовые HF-152/153/162/163/164 опубликованы в origin/main коммитом a8c9f31 (18 файлов). Пользовательские main_menu.tscn/trade_scene.tscn остались локально вне коммита; их SHA256 совпадают со snapshot HF165. Запись завершения публикации оформляется отдельным documentation commit.
Checks: fetch без расхождений; staged diff --check PASS; rendered inventory-aspect/counter-item/tutorial/intro-transition точного публикуемого кода с обеими сценами HEAD PASS, stderr пуст. Прежние workspace/isolated build и профильные проверки PASS. Обычный push origin main PASS, HEAD=origin/main (0/0); рабочих изменений кроме двух пользовательских сцен нет до записи этого отчёта. Evidence .local/checks/hf165; прежние backup/stash сохранены.
Acceptance: исправлены пропорции иконок, положение предметов на прилавке, последовательные процентные шкалы камеры и быстрый пропуск пролога; тесты опубликованы, пользовательские сцены сохранены.
Read-set exception: нет, source/ не читался.
Risks/blocker: нет новых. Две пользовательские сцены намеренно остаются незакоммиченными локально.
Next: пользовательская проверка опубликованной версии.
```
### HF-164 — раздельные этапы камеры с процентными шкалами

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-163 DONE, активных пересечений нет.

CP-HF-164 read-set: текущий фронт; scripts/UI/FirstTradeTutorial.cs; scenes/UI/FirstTradeTutorial.tscn; scripts/Camera/WorldCamera.cs (события read-only); scripts/Presentation/TutorialCameraCheckpoint.cs; tests/Integration/ProjectTest.{Tutorial,Menu,Helpers,Presentation}.cs; tests/ProjectTest.cs; localization/en.tres; docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/FirstTradeTutorial.cs; scenes/UI/FirstTradeTutorial.tscn; tests/Integration/ProjectTest.Tutorial.cs; localization/en.tres; docs/GAMEPLAY.md; .local/checks/hf164 и изолированная копия .local/checks/git-review-20261118/project. Out of scope: source/, обе основные пользовательские сцены, WorldCamera/TradeScene/домен/баланс/игровая save schema, другие незакоммиченные правки/commit/push.

Приёмка: сначала только передвижение, затем только масштабирование; текущая шкала0–100%, без прочерков/галочек. Реальное перемещение даёт частичный прогресс; принятые wheel шаги дают25/50/75/100%; ранний zoom не засчитывается. Передвижение100% видно до отпускания drag и короткой паузы, затем zoom0% без нового перелёта. Частичный прогресс/этап/старые cfg/reload/replay сохраняют корректность; итоговая2с пауза свободной камеры и остальной tutorial прежние. Проверки: dotnet build; rendered --tutorial-checks и --intro-transition-checks, RU/EN960×540/1280×720 screenshots и fitting; git diff --check/SHA256 чужих правок.

```text
Task: HF-164
Recommended status: REVIEW → принято координатором, DONE
Changed: FirstTradeTutorial теперь имеет последовательные Camera (передвижение) и CameraZoom, отдельные заголовки/инструкции и ProgressBar0–100% вместо checklist. Реальный pan постепенно заполняет64px шкалу; при100% этап ждёт release и0,6с, затем открывает zoom0 без нового перелёта. Только на zoom этапе принятые события колеса дают25/50/75/100%; раннее колесо не засчитывается. Панорамирование/масштаб свободны в обоих этапах. Частичные pan/zoom/фаза сохраняются optional полями tutorial cfg с debounce0,25с и flush при выходе; старые done-флаги сохранены. Restart сбрасывает проценты; прежняя2с handoff-пауза/модалы/skip/legacy поведение сохранены. Новые RU/EN строки, сцена шкалы и docs/GAMEPLAY обновлены. WorldCamera/игровая save schema/основные сцены не менялись.
Checks: workspace/isolated dotnet build PASS (0 warnings/errors). Rendered --tutorial-checks PASS: pan0/частичный39/reload/100, удержание drag при100, следующий zoom0 без перелёта, раннее колесо0, zoom25/reload/50/75/100; заголовки и шкала RU/EN960×540/1280×720, свободная камера в итоговой паузе, pause/skip-confirmation не расходуют задержку, legacy handoff, полный walkthrough/продажа/finish/skip/replay/new run/reload. Rendered --intro-transition-checks PASS. stderr обеих групп пуст. Просмотрены movement-zero-ru-960, zoom-zero-en-960 и movement-partial39%; всё помещается. git diff --check PASS; SHA256 прежних локальных файлов вне write-set совпадает с before.json. Evidence .local/checks/hf164 (логи/снимки/manifest).
Acceptance: камера изучается последовательно; на каждом этапе отображаются текущие проценты без прочерков/галочек, прогресс накапливается по реальным действиям и сохраняется.
Read-set exception: нет; source/ не читался, тестовые сохранения изолированы.
Risks/blocker: нет новых. Один принятый шаг wheel равен25%; при достижении zoom limit событие не поступает и шкала не меняется, нужно прокрутить обратно.
Next: пользовательская визуальная приёмка; commit/push не выполнялись.
```
### HF-163 — предмет сделки на поверхности прилавка

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; HF-162 DONE, активных пересечений нет.

CP-HF-163 read-set: текущий фронт; scripts/CounterWorld.cs, scripts/UI/ZoomResponsiveUi.cs; scenes/trade_scene.tscn (TradingRoom/Item/TradePanel/background read-only); art/rooms/cashier_closed.jpg; tests/Integration/ProjectTest.{Trading,Resources,Economy,Presentation,Helpers}.cs, tests/ProjectTest.cs; каталог art/items через точные ArtPath runtime. Write-set: ORCHESTRATION_PLAN.md; scripts/CounterWorld.cs (только положение существующих визуальных вариантов); tests/Integration/ProjectTest.Trading.cs; tests/ProjectTest.cs (отдельная группа); .local/checks/hf163 и изолированная копия .local/checks/git-review-20261118/project. Out of scope: source/, новые изображения/домен/баланс/save schema, TradeScene.cs, обе пользовательские основные сцены, предыдущие локальные правки/commit/push.

Приёмка: у всех16 предметов нижний край непрозрачного изображения касается одной точки на столешнице справа от рук продавца; прозрачные поля не заставляют предмет висеть. Прежние размеры/пропорции/панель сделки и переключение видимого предмета сохранены. Проверки: dotnet build; rendered --counter-item-checks (16 вариантов, zoom/resize, снимки); real sale/sound-effects regression; git diff --check, SHA256 прежних правок.

```text
Task: HF-163
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/CounterWorld.cs — редакторский CounterItemContact=(930,744) в координатах фона кассы. При Ready каждый существующий вариант выравнивается по нижнему центру alpha used-rect с учётом прозрачных полей, transform фона/Item и прежнего масштаба. Root Item/TradePanel не перемещаются, пользовательская trade_scene не меняется. Tests/Integration/ProjectTest.Trading.cs и tests/ProjectTest.cs — отдельная --counter-item-checks группа.
Checks: workspace/isolated dotnet build PASS (0 warnings/errors); rendered --counter-item-checks PASS:16 вариантов ×2 zoom ×2 window, нижний край на столешнице, прежние размеры, единственный видимый предмет, отсутствие пересечения с панелью, скрытие Item. Снимки Hat/Guitar (исторический kind для maracas)/Cello сохранены; Hat и Cello просмотрены, предмет на поверхности справа от рук продавца. Rendered --sound-effects-checks PASS включая настоящий приход покупателя/отказ/продажу/reload; stderr обеих групп пуст. SHA256 прежних локальных файлов вне write-set совпадают с before.json. git diff --check PASS. Evidence .local/checks/hf163.
Acceptance: все предметы опираются нижним краем изображения на одну точку столешницы, без зависания на уровне груди продавца; панель/сделка сохранены.
Read-set exception: нет; source/ не читался; user:// тестов изолирован. Использованы точные art/rooms/cashier_closed.jpg и art/items/cello.png для визуальной проверки.
Risks/blocker: нет новых. Изменён только presentation; существующие масштабы предметов оставлены прежними.
Next: пользовательская визуальная приёмка; commit/push не выполнялись.
```
### HF-162 — пропорции предметов в инвентаре

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18; AUDIT-20261118 DONE, активных пересечений нет.

CP-HF-162 read-set: текущий фронт; scenes/UI/{SlotUI,InventoryUI,ShowcaseSlot}.tscn; scripts/UI/{SlotUI,InventoryUI}.cs; scripts/Presentation/MonochromePresentation.cs; tests/Integration/ProjectTest.{Inventory,Ui,Helpers,Presentation}.cs, tests/ProjectTest.cs; ChikagoShop.csproj, .vscode/settings.json; Git status/diff для сохранности пользовательских правок. Write-set: ORCHESTRATION_PLAN.md; scenes/UI/SlotUI.tscn (режим вписывания Icon); tests/Integration/ProjectTest.Inventory.cs; tests/ProjectTest.cs (отдельная группа); .local/checks/hf162 и изолированная копия .local/checks/git-review-20261118/project. Out of scope: source/, исходные изображения, обе пользовательские основные сцены, домен/баланс/save schema, прочие локальные изменения, commit/push.

Приёмка: широкие/высокие/квадратные изображения полностью вписаны по центру ячейки без растягивания/обрезки; размер ячейки не зависит от размера исходной текстуры. Rendered regression измеряет реальное изображение для разных пропорций текстуры/ячейки на 960×540 и 1280×720; прежние выбор/drag/quick transfer работают. Проверки: dotnet build; rendered --inventory-aspect-checks (новая проверка и TestInventoryTransfers); git diff --check; сохранность прежних локальных файлов.

```text
Task: HF-162
Recommended status: REVIEW → принято координатором, DONE
Changed: scenes/UI/SlotUI.tscn Icon stretch_mode=5 (KeepAspectCentered); tests/Integration/ProjectTest.Inventory.cs — rendered pixel regression для wide/tall/square, двух пропорций ячейки, 960×540/1280×720; tests/ProjectTest.cs — группа --inventory-aspect-checks. Прежний transfer fixture приведён к текущим предусловиям: запас ≥8 предметов независимо от items-per-craft, открытая вторая мастерская, явный Start перед ожиданием покупателя и каждым переходом; terminal fixture не предполагает ровно3 предмета за craft. Product-поведение кроме режима Icon не менялось.
Checks: новый rendered regression до fix FAIL (800×200 растягивался в81×81 вместо81×20,25); после fix все12 комбинаций PASS, TestInventoryTransfers PASS (mouse drag/drop/cancel/stale payload/Shift/paused/terminal). Isolated/workspace dotnet build PASS, 0 warnings/errors; stderr финальной rendered группы пуст; git diff --check PASS; SHA256 прежних локальных файлов вне write-set совпадают с before.json. Evidence .local/checks/hf162/{before,after}.{out,err}, before.json.
Acceptance: предмет полностью вписан с исходным соотношением сторон по центру, текстура не расширяет слот; выбор/перенос и ограничения сохранены.
Read-set exception: tests/Integration/ProjectTest.Crafting.cs (CraftAtelier/FinishEmbroidery) и scripts/TradeScene.cs (AcquireItem/ItemsPerSpin/EnergyRemaining/AdvanceToNextDay) для диагностики устаревшего transfer fixture; scenes/trade_scene.tscn только rg связей SlotUI/ShowcaseSlot для проверки использования сцены. source/ не читался; основные сцены/пользовательские изменения не менялись.
Risks/blocker: нет новых; реальные сохранения не использовались, тесты в изолированной копии проекта.
Next: пользовательская визуальная приёмка в игре; commit/push не выполнялись.
```
### AUDIT-20261118 — разбор входящих коммитов и обновление main

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-18. Пользователь разрешил обновление после проверки. Зависимости: нет; активных исполнителей и пересечений нет.

CP-AUDIT-20261118 read-set: текущий фронт и Git refs/status/diff для HEAD 8c78d49 → origin/main 806ef2d; точные 54 пути входящего diff, 16 путей локальных изменений из backup-manifest.json; tools/verify.ps1, .vscode/settings.json, ChikagoShop.csproj/ChikagoShop.sln, scripts/Audio/MusicSettings.cs, src/ChikagoShop.Domain/Days/DaySession.cs, tests/Integration/ProjectTest.Helpers.cs. Write-set: ORCHESTRATION_PLAN.md (эта запись); .local/checks/git-review-20261118; Git refs/index/stash и обновление отслеживаемых файлов строго входящим diff, восстановление прежних локальных правок. Out of scope: source/, новая функциональность/баланс/save schema, commit/push, реальные user:// сохранения.

Приёмка: разбор runtime diff, build/domain tests/import и rendered day-expiry/day-start/sound-effects/intro-music/game-music/music-settings/intro-transition; main=origin/main обычным fast-forward, локальные правки сохранены и совместимы; повторная сборка и проверки пересечений после восстановления. Команды/evidence: логи .local/checks/git-review-20261118; git diff --check, rev-list, status; SHA256 backup-manifest. Копия origin/main имеет отдельное config/name для изоляции user://.
```text
Task: AUDIT-20261118
Recommended status: REVIEW → принято координатором, DONE
Changed: main fast-forward 8c78d49 → 806ef2d (10 incoming commits, HEAD=origin/main). Прежние 14 modified + 2 untracked восстановлены; docs/GAMEPLAY.md и localization/en.tres содержат обе добавленные части, main_menu сохраняет пользовательскую разметку/UID/отсутствие лишних overrides и получает SoundVolume. Восемь файлов без пересечения побайтно совпадают с backup-manifest; проверены diff остальных восьми. Backup: stash 8c4b2803c055d785ea8f4222537e39cb2559e176 и .local/checks/git-review-20261118/backup + local-changes.patch. Новых commit/push нет.
Checks: origin snapshot build PASS (0 warnings/errors); dotnet test PASS 153/153; повторный import PASS (четыре прежних duplicate UID warnings); rendered day-expiry/day-start/sound-effects/intro-music/game-music/music-settings/intro-transition PASS, stderr пуст. После восстановления локальных изменений workspace/isolated build PASS; workspace/isolated import PASS (те же UID warnings); rendered intro-latency/intro-transition/intro-music/tutorial/music-settings/day-expiry/sound-effects PASS, stderr пуст. git diff --check PASS, no unmerged paths, rev-list HEAD...origin/main=0/0. Evidence .local/checks/git-review-20261118/*.out/*.err и review.md.
Acceptance: блокирующих дефектов/опасных операций во входящем runtime diff не найдено; сохранения/schema/DialoguePlayer не менялись. Таймаут/ручное закрытие автоматически открывают следующий день после продажи текущему покупателю/закрытия крафта; отдельная шина SFX и громкость работают в паузе. Локальные быстрый skip и camera handoff сохранились и проверены вместе с incoming.
Read-set exception: нет; source/ не читался, user:// тестов изолирован отдельным config/name. Первый cold import кратковременно жаловался на ещё не импортированный font; повторный import и runtime группы без ошибок.
Risks/blocker: четыре прежних duplicate UID warnings UI; полная default ProjectTest-группа не запускалась, выполнены целевые группы. Два пересекающихся файла сцены/переводов объединены вручную; сохранён stash для восстановления.
Next: пользовательская проверка обновлённой игры.
```
### HF-153 — пауза перед передачей камеры обучению

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-152 DONE, активных пересечений нет.

CP-HF-153 read-set: текущий фронт; scripts/UI/FirstTradeTutorial.cs, scenes/UI/FirstTradeTutorial.tscn, scripts/Camera/WorldCamera.cs, scripts/Presentation/TutorialCameraCheckpoint.cs; tests/Integration/ProjectTest.{Tutorial,Helpers,Menu}.cs, tests/ProjectTest.cs; localization/en.tres, docs/GAMEPLAY.md; main_menu.tscn/trade_scene.tscn только для сохранности. Write-set: ORCHESTRATION_PLAN.md; scripts/UI/FirstTradeTutorial.cs; scenes/UI/FirstTradeTutorial.tscn (длительность паузы в инспекторе); scripts/Camera/WorldCamera.cs (только readonly IsDragging); tests/Integration/ProjectTest.Tutorial.cs; localization/en.tres; docs/GAMEPLAY.md; .local/checks/hf153 и изолированный project HF139. Out of scope: source/, обе пользовательские сцены, незакоммиченный HF-152/другие скрипты/арт, TradeScene.cs/домен/игровая save schema/баланс/commit/push.

Приёмка: после выполнения обеих задач камеры не происходит мгновенного захвата управления.2с спокойного времени камера ещё свободна, оба действия отмечены выполненными и показано объяснение предстоящего автоматического ведения. Продолжение pan/zoom продлевает паузу; удержание drag не прерывается перелётом. Пауза/skip-confirmation/модальные окна не расходуют задержку; resize/смена языка не вызывают ранний захват. Затем единственный плавный перелёт к обычному следующему этапу с прежней блокировкой камеры. Reload во время ожидания сохраняет фазу и даёт полную паузу; старые tutorial cfg без нового optional флага и более поздние этапы не заставляют повторять практику. Новая партия/replay/подтверждённый skip очищают ожидание без поздних переходов. Проверки: dotnet build; rendered --tutorial-checks и --intro-transition-checks; RU/EN960×540/1280×720 pending-state screenshot; git diff-check и сохранность остального рабочего дерева.

```text
Task: HF-153
Recommended status: REVIEW → принято координатором, DONE
Changed: после выполнения pan и zoom камера остаётся свободной на2с спокойного времени. Показываются обе галочки и локализованное объяснение, что дальше обучение само направит камеру. Новые принятые pan/zoom сбрасывают ожидание; удерживаемый drag, pause, skip-confirmation, mini-game/card/failure/success не расходуют его. Затем прежняя плавная передача следующему шагу и NavigationLocked. Export CameraCompletionDelaySeconds=2 задан в tutorial scene. WorldCamera получил только readonly IsDragging. В tutorial cfg добавлен optional camera_handoff_done: reload ожидания показывает success и полную паузу без нового перелёта, более поздние этапы и старые cfg с обеими выполненными задачами пропускают повторный review; restart сбрасывает фазу. Игровая save schema/TradeScene и незакоммиченный HF152 не менялись.
Checks: local/isolated dotnet build PASS (0 warnings/errors). Rendered --tutorial-checks PASS: реальные drag/wheel, обе галочки без мгновенного lock; reload pending; RU/EN960×540/1280×720 fitting; zoom/pan продолжают работать после выполнения; pause дольше задержки не передаёт управление; skip-confirmation дольше задержки не передаёт управление, cancel возвращает свободную камеру без refocus; удержание drag дольше задержки не прерывается, после release/оставшейся паузы единственный переход Start и обычная блокировка остальных этапов. Legacy cfg без нового флага восстанавливает Select без повторного ожидания; весь прежний walkthrough/продажа/finish/skip/replay/new run/reload PASS. Rendered --intro-transition-checks PASS: natural/skip/null fallback/Continue и новая практика. stderr обеих групп пуст. Просмотрен tutorial-camera-complete-ru-960.png, UI помещается; снимки всех4 комбинаций сохранены в TEMP. git diff-check PASS; SHA256 всех прежних изменённых файлов вне write-set совпадает с snapshot HF153, включая обе пользовательские сцены и код/тесты ускорения пролога. Evidence .local/checks/hf153.
Acceptance: игрок видит успех и предупреждение, успевает закончить жест перед автоматическим ведением камеры; пауза/модалы/загрузка/старый прогресс работают.
Read-set exception: нет; source/ не читался. Проверки в изолированном user:// HF139, реальные сохранения не затрагивались.
Risks/blocker: нет новых.
Next: пользовательская приёмка; commit/push не выполнялись.
```

### HF-152 — быстрый переход после пропуска пролога

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-151 DONE, активных пересечений нет.

CP-HF-152 read-set: текущий фронт; scripts/MainMenu.cs, scripts/Audio/IntroMusicDirector.cs, scripts/Dialogue/DialoguePlayer.cs; scenes/main_menu.tscn (GameScene/MusicDirector read-only, пользовательские изменения сохраняются), scenes/trade_scene.tscn (только сохранность); tests/Integration/ProjectTest.{Menu,IntroMusic,Helpers}.cs, tests/ProjectTest.cs; ChikagoShop.csproj, project.godot, docs/GAMEPLAY.md, локальная GodotSharp API reference (ResourceLoader). Write-set: ORCHESTRATION_PLAN.md, scripts/MainMenu.cs (preload/вход после intro/continue, без изменения разметки), scripts/Audio/IntroMusicDirector.cs (отдельный короткий fade пропуска), новый scripts/Presentation/SceneResourceWarmup.cs/.uid при необходимости; tests/Integration/ProjectTest.{Menu,IntroMusic}.cs, tests/ProjectTest.cs (отдельная latency-группа), docs/GAMEPLAY.md; .local/checks/hf152 и изолированный project HF139. Out of scope: source/, обе пользовательские сцены, TradeScene.cs/новая доменная логика, баланс/save schema/арт/прочие музыкальные переходы/commit/push.

Диагностика: skip сейчас ждёт1,5с ExitFadeSeconds, после чего EnterGame синхронно GD.Load при сериализованном GameScene=null. Приёмка: короткое мягкое затухание по skip, естественное завершение сохраняет прежнее затухание; игровая сцена готовится фоном, fade/loading идут совместно, ожидание не блокирует UI. Нет двойного входа/повторного сброса сохранения при спаме; Continue и пустой override работают; удаление меню/отмена не дают позднего входа; прежние уровни/crossfade/RU-EN/обучение сохраняются. Наблюдаемая проверка: замер от настоящего skip до первого игрового кадра до/после, холодный и подготовленный путь; музыка завершает fade до запуска игры. Команды: dotnet build, rendered --intro-latency-checks, --intro-transition-checks, --intro-music-checks, --music-settings-checks; git diff-check и hash пользовательских сцен.

```text
Task: HF-152
Recommended status: REVIEW → принято координатором, DONE
Changed: пропуск пролога использует отдельный плавный fade0,18с вместо обязательных1,5с; естественное завершение сохраняет1,5с. MainMenu больше не загружает trade_scene синхронно в инициализаторе/после skip: SceneResourceWarmup запускает ResourceLoader.LoadThreadedRequest в Ready меню и проверяет status по кадрам, забирая PackedScene только после Loaded. Ресурс готовится одновременно с меню/диалогом/fade, инстанцирование остаётся на основном потоке. EnterGame асинхронно дожидается готовности и ставит _startingGame до await, чтобы не было повторного входа/сброса save. Прекращение жизни меню отменяет вход; фоновый request завершается без обращения к удалённому меню. Явный PackedScene override и Continue сохранены. Интерфейс/сцены/доменные правила не менялись.
Checks: local/isolated build PASS (0 warnings/errors); local import PASS (прежние4 duplicate UI UID warnings). Rendered --intro-latency-checks PASS: настоящий skip → первый игровой кадр, немедленный пропуск и после0,8с чтения; готовность в фоне, отсутствие второго входа, новый день со стартовыми ресурсами и ожидающим стартом, удаление меню во время fade не вызывает поздний вход. До:1,886с/1,624с; после:0,852с/0,310с на этом ПК (первый/подготовленный путь). Rendered --intro-transition-checks PASS: natural/skip с реальным и пустым override, Continue сохранённой партии и новое обучение. Rendered --intro-music-checks PASS: прежние уровни/cues/crossfades, плавность обеих половин короткого skip fade из3 стадий, спам skip/Play, natural fade, cancel/выход. Rendered --music-settings-checks PASS: menu/pause/громкость/языки/раскладка. stderr всех4 групп пуст. SHA256 обеих пользовательских сцен совпадает с snapshot HF152; git diff-check PASS. Evidence .local/checks/hf152.
Acceptance: задержка заметно уменьшена в замере, подготовленная сцена открывается после короткого затухания; обычный конец пролога/Continue/сохранения/обучение и музыкальные переходы работают.
Read-set exception: нет; использована локальная GodotSharp.xml reference из SDK4.7.2 для threaded API. source/ и пользовательские сохранения не читались; тесты в изолированном user:// HF139.
Risks/blocker: время холодного чтения/первого кадра зависит от ПК, полностью устранить его нельзя; новых блокеров нет. Прежние4 duplicate UID warnings чужих кнопок.
Next: пользовательская приёмка; ускорение не коммитилось и не пушилось. Предыдущая задача HF150 опубликована в HF151 (771f173; report8c78d49).
```

### HF-161 — коммиты и публикация текущей версии в main

Статус: `DONE`; координатор/исполнитель /root; READY 2026-11-18; HF154/155/157–160 DONE. CP-HF-161: .local/checks/hf161/CP-HF-161.md (точные scope/приёмка/проверки). Пользователь разрешил commit текущих фиксов и push всей main. Аудио и day/clock изменения сохраняются отдельными коммитами; main обновляется с сохранением обеих линий истории. ZIP/EXE/source/ не входят в публикацию.


Task: HF-161
Recommended status: REVIEW → принято координатором, DONE
Changed: новые коммиты26bfe68 (зловещая тема90BPM, reroll и3 stitch SFX) иa10ca34 (автопереход дней, подпись/отступы часов, физический клик после focus fix, регрессии). main fast-forward8c78d49→a10ca34 через существующий merge2193924, сохранены обе линии истории. Обычный git push origin main выполнен успешно, удалённая вершинаa10ca340907522a188be00d58f00c0b5b1941c16 подтверждена ls-remote. Все текущие runtime изменения включены; ZIP/EXE/source/ игнорируются и не опубликованы.
Checks: fetch origin/main PASS, расхождение0/0 до объединения; diff-check/staged check PASS; сохранённые HF154 intro-music, HF155 embroidery-target, HF160 day-start/day-expiry/sound-effects PASS и build0warnings/errors подтверждены перед commit; ancestry main→feature/music и ff-only PASS; push/readback HEAD=origin/main=ls-remote PASS; рабочее дерево чистое после функциональной публикации. Повторные runtime тесты без новых правок не запускались. Прежний устаревший day-timer regression HF157 не заявляется прошедшим.
Acceptance: пользовательские изменения сохранены логическими коммитами, вся локальная main опубликована без force. feature/music содержит оба новых функциональных коммита локально; отдельный push этой ветки не запрошен и не выполнен. Открытая ветка main.
Read-set exception: нет; source/ не читался.
Risks/blocker: блокеров публикации нет. EXE/ZIP в Downloads остались прежними по просьбе пользователя и ещё не включают последующие day/clock фиксы.
Next: зафиксировать этот итоговый протокол в main, обычный push и финальная сверка refs/status.

### HF-160 — физический клик по часам между кадрами

Статус: `DONE`; координатор/исполнитель /root; READY 2026-11-18; HF-159 DONE. CP-HF-160: .local/checks/hf160/CP-HF-160.md (точные scope/приёмка/проверки). Исследуется сброс focus на mouse-down до release; регрессия с удержанием ЛКМ между кадрами и проверкой звука. ZIP/EXE не меняются.


Task: HF-160
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/Days/DayCycleController.cs; tests/Integration/ProjectTest.DayStart.cs; ORCHESTRATION_PLAN.md.
Checks: regression с реальным down/hold120ms/up между кадрами до исправления FAIL (clockClicks!=1) в baseline-errors.log; после исправления dotnet build PASS (0 warnings/errors), rendered --day-start-checks PASS: ровно1 Cue.Click, день закрыт, следующим тиком day2/cards без второго нажатия. --day-expiry-checks PASS: timeout/manual/trade/craft/pause/reload/no duplicate; --sound-effects-checks PASS. git diff-check PASS. Evidence .local/checks/hf160/{baseline-errors,build,day-start,day-expiry,sound-effects}.log.
Acceptance: корневая причина — GuiInput(mouse-down) вызывал deferred ReleaseFocus раньше mouse-up, BaseButton отменял pending Pressed. Старые синтетические тесты down/up в одном кадре обходили defer и не воспроизводили физический ввод пользователя. ReleaseFocus теперь откладывается только после Pressed: фокус очищается после завершённого клика, звук и EndDay срабатывают один раз. Label/отступы/переход HF159 сохранены, current buyer не теряется.
Read-set exception: read-only input WorldCamera/FirstTradeTutorial и SoundEffects для исключения перехвата, source/ не читался.
Risks/blocker: новых блокеров нет; прежний sandbox root certificate warning. Пользовательский ZIP/EXE/Downloads не менялись, commit/push не выполнялись.
Next: пользователь проверяет обычный клик в проекте.

### HF-159 — переход после одного клика часов и отступы надписи

Статус: `DONE`; координатор/исполнитель /root; READY 2026-11-18; HF-158 DONE. CP-HF-159: .local/checks/hf159/CP-HF-159.md, точные scope/приёмка/проверки в пакете. Ручное закрытие должно запускать тот же отложенный переход, что таймаут; подпись «Завершить день» с внутренними отступами. ZIP/EXE не меняются.


Task: HF-159
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/Days/DayCycleController.cs; tests/Integration/ProjectTest.{DayStart,DayExpiry}.cs; ORCHESTRATION_PLAN.md.
Checks: dotnet build ChikagoShop.sln --no-restore PASS (0 warnings/errors); isolated Godot import PASS; rendered --day-start-checks PASS: один реальный клик закрывает день и следующий тик открывает day2/cards без второго EndDay, надпись RU «Завершить день»/EN End day, hover/focus/restore seconds, font width<=46%clock and margins24%. --day-expiry-checks PASS: timeout и реальный manual clock click с активным покупателем, последний buyer сохраняется до продажи в т.ч. после reload, затем автоматический переход; craft/pause/save/no duplicate regression. --closed-reroll-checks PASS. Визуально просмотрен и сохранён .local/checks/hf159/clock-end-ru.png: две строки внутри циферблата с заметным расстоянием до ободка. git diff-check PASS. Evidence .local/checks/hf159/{build,import,day-start,day-expiry,closed-reroll}.log.
Acceptance: причина — ручное RequestEnd возвращало Closed, но только timeout устанавливал маркер автоперехода, поэтому ручное завершение требовало второго клика. Теперь ручное Closed также устанавливает общий _advanceWhenClosed, ожидание сделки/крафта сохраняется. Текст использует существующий перевод Завершить день, ограничен внутренними52%clock с24%внешними отступами, фактическая ширина слова<=46%clock; рамка часов и текст не пересекаются. Другие звуки/изменения не затронуты.
Read-set exception: нет, source/ не читался.
Risks/blocker: новых блокеров нет; прежний Godot root certificate warning в sandbox. ZIP/EXE/Downloads не менялись. Commit/push не выполнялись.
Next: пользовательская проверка одного клика и отступов в проекте.

### HF-158 — надпись завершения дня при наведении на часы

Статус: `DONE`; координатор/исполнитель /root; READY 2026-11-18; HF-157 DONE. CP-HF-158: .local/checks/hf158/CP-HF-158.md. Точные scope/приёмка/проверки в пакете: надпись в стиле Start только на активных часах с положительным временем, сохранение секунд после ухода мыши и прежнего EndDay; RU/EN. ZIP/EXE не меняются.


Task: HF-158
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/Days/DayCycleController.cs; localization/en.tres; tests/Integration/ProjectTest.DayStart.cs; ORCHESTRATION_PLAN.md.
Checks: dotnet build ChikagoShop.sln --no-restore PASS (0 warnings/errors); isolated Godot import PASS; rendered --day-start-checks PASS: hover/caption/no extra tooltip, same font/black style, word fits painted face, mouse exit restores seconds, keyboard focus, English, real early-close click removes caption, fresh/reloaded/new-day/start checks. --day-expiry-checks PASS: timeout/trade/craft/pause/save regression. Визуально проверен TEMP/chikago-hf67-clock-end-ru.png; первый вариант тем же размером Start выходил за нарисованный круг из-за прозрачных полей sprite, финальный размер подогнан под внутренние60%TextureRect и надпись разбита на2 строки. git diff-check PASS. Evidence .local/checks/hf158/{build,import,day-start,day-expiry}.log.
Acceptance: наведение на активные часы с положительным временем заменяет цифры на «Закончить день», уход возвращает счётчик; Start сохранён до начала, закрытый день не показывает надпись. Шрифт/чёрный цвет/центрирование без рамки соответствуют Start, размер адаптируется по ширине слова внутри круга. Label MouseFilter Ignore не перехватывает клик; EndDay маршрут и автопереход HF157 не менялись. Английский End day.
Read-set exception: read-only CapturePresentation helper для пути визуального evidence. source/ не читался.
Risks/blocker: новых блокеров нет; прежний Godot certificate warning в sandbox. ZIP/EXE/Downloads не менялись, существующие музыкальные изменения сохранены; commit/push не выполнялись.
Next: пользовательская приёмка надписи в проекте.

### HF-157 — автоматический переход после истечения дня

Статус: `DONE`; координатор/исполнитель /root; READY 2026-11-18; HF-156 DONE. CP-HF-157: .local/checks/hf157/CP-HF-157.md. Точные read/write-set и out of scope в пакете. Исправляется отсутствие вызова перехода при закрытии дня таймером, с ожиданием активной сделки/крафта. Досрочное завершение только исследуется, UI/ручной маршрут не меняются. Приёмка и команды — в CP.


Task: HF-157
Recommended status: REVIEW → принято координатором, DONE
Changed: scripts/Days/DayCycleController.cs; tests/Integration/ProjectTest.DayExpiry.cs/.uid; tests/ProjectTest.cs; ORCHESTRATION_PLAN.md. Текущие HF154/155 audio изменения сохранены.
Checks: новый --day-expiry-checks до исправления FAIL: день1 закрыт, day2/cards не открываются. После исправления build PASS (0 warnings/errors), isolated import PASS, rendered --day-expiry-checks PASS: автоматический переход ровно один раз, ожидание текущей продажи/крафта, pause, восстановление expired save (с покупателем и без), полный таймер дня2 до Start. Rendered --day-start-checks PASS; --closed-reroll-checks PASS (таймаут/ручное закрытие запрещают reroll, финальная продажа не потеряна). git diff-check PASS. Старый --day-timer-checks FAIL на устаревшем ожидании90 в новой игре до Start; пробная адаптация fixture выявила также устаревшие отступы layout, её изменения полностью отменены. Days.cs совпадает с HEAD, общего PASS этого старого теста не заявляется. Evidence .local/checks/hf157/{baseline-errors,build,import,day-expiry,day-start,closed-reroll,day-timer-errors}.log.
Acceptance: таймаут помечает отложенный автоматический переход; следующий игровой тик после освобождения от сделки/мини-игры вызывает существующий EndDay/Advance/cards/проверку квоты. Reset сбрасывает маркер, Restore восстанавливает возможность перехода старого closed save без смены schema. Крафт/покупатель/пауза не прерываются, новый день после карточек ждёт Start. Domain и TradeScene не менялись. Досрочный EndDay/UI не восстанавливались и не менялись: функция DayCycleController.EndDay существует, ClockActionButton.Pressed уже связан с game.EndDay; старая EndDayButton находится в скрытом Content, скрываемом ProgressPanel.
Read-set exception: read-only scripts/UI/ProgressPanel.cs для выяснения скрытия старой кнопки. source/ не читался. Изначально расширен scope для новой EXE-сборки вслед за предыдущей просьбой о ZIP; пользователь затем явно уточнил не менять EXE/ZIP. Сборка остановлена по точному command line hf157/release-project, упаковка и запись Downloads не выполнялись.
Risks/blocker: исправление готово в проекте; архив Downloads остаётся прежним с SHA256=7B1824DB076557DEC4DD85E8D1B4339DD51A2AF4A38A35B6073BB453440DF5DC (readback PASS), исправления в нём пока нет по просьбе пользователя. Прежнее Godot root certificate warning в sandbox; устаревший day-timer regression требует отдельной актуализации. Новых функциональных блокеров нет.
Next: пользователь проверяет проект; восстановление отдельной кнопки/новая EXE-сборка только по дальнейшему запросу. Commit/push не выполнялись.

### HF-156 — Windows EXE и ZIP текущей музыкальной версии

Статус: `DONE`; координатор/исполнитель /root; READY 2026-11-18; HF-155 DONE. CP-HF-156: .local/checks/hf156/CP-HF-156.md. Read/write-set, out of scope и проверки в пакете. Собирается текущая feature/music вместе с незакоммиченными HF-154/155; оригинальные файлы проекта и прошлые архивы сохраняются. Приёмка: self-contained release Windows x64, JSON и все runtime-файлы, запуск распакованной сборки, ZIP в Downloads; без commit/push.


Task: HF-156
Recommended status: REVIEW → принято координатором, DONE
Changed: изолированный снимок текущего рабочего проекта feature/music2193924 + HF154/155; builds/ChikagoShop-2026-11-18-music-Windows; C:/Users/artem/Downloads/ChikagoShop-2026-11-18-music-Windows.zip; ORCHESTRATION_PLAN.md. Исходный export preset/csproj/игровые файлы не менялись.
Checks: isolated dotnet build PASS (0 warnings/errors); Godot4.7.2 .NET import/export-release PASS. Временный preset с ранее загруженным официальным mono Windows release template, JSON include, исключение dev/originals, debug symbols off. Экспорт содержит новые sinister/reroll/stitch ресурсы и config/economy.v1.json/dialogue/intro.ru.json. Встроен self-contained .NET8.0.31, coreclr/hostfxr/ChikagoShop assemblies. Standalone headless menu exit0; распакованный ZIP rendered menu exit0, rendered gameplay через временный override.cfg exit0 и успешная инициализация TradeScene. Override удалён из тестовой копии; в ZIP его нет. Inventory190 entries: EXE/PCK/runtime присутствуют, нет saves/settings/tests/PDB. ZIP121328816bytes; Downloads readback SHA256=7B1824DB076557DEC4DD85E8D1B4339DD51A2AF4A38A35B6073BB453440DF5DC совпадает с исходным архивом. git diff-check PASS. Evidence .local/checks/hf156/{build,import,export,smoke-menu,zip-menu,zip-gameplay}.log,zip-inventory.txt,delivery.json.
Acceptance: готовый архив в Downloads включает всю папку игры, получателю не нужны Godot/отдельный .NET; текущие незакоммиченные звуки включены, предыдущие архивы/пользовательские сохранения не тронуты; commit/push не выполнялись.
Read-set exception: прочитан точный прежний HF139 export handoff/preset/templates для повторного использования имеющихся официальных шаблонов; source/ не читался. Подпись JSON source snapshot фиксируется текущими файлами, Git publish не требовался.
Risks/blocker: блокеров нет. В sandbox smoke прежнее Failed to read root certificate store, других runtime ошибок нет. Release не поддерживает внешний --script для диагностики: проба остановлена по точному пути принадлежащего нам smoke-процесса, проверка заменена реальным запуском меню/игровой сцены распакованного EXE. Первый inventory сопоставлял slash с Windows ZIP backslash — исправлен только проверяющий код, повторная проверка PASS.
Next: пользователь распаковывает весь архив и запускает ChikagoShop.exe.

### HF-155 — случайные звуки стежков в вышивке

Статус: `DONE`; координатор/исполнитель /root; READY 2026-11-17; HF-154 DONE. CP-HF-155: .local/checks/hf155/CP-HF-155.md (точные read/write-set, out of scope и проверки). Приёмка: каждый успешный стежок выбирает один из трёх WAV, общий SFX bus/редакторская громкость, без дублирующего click и звука промаха; прежние изменения сохранены.


Task: HF-155
Recommended status: REVIEW → принято координатором, DONE
Changed: audio/sfx/stitch_{1,2,3}.wav/.import; scenes/Audio/SoundEffects.tscn (Stitch1/2/3); scripts/Audio/SoundEffects.cs (отдельный RNG и PlayStitch); scripts/UI/EmbroideryGame.cs (звук после успешного Hit); tests/Integration/ProjectTest.{EmbroideryTarget,SoundEffects}.cs; ORCHESTRATION_PLAN.md.
Checks: dotnet build ChikagoShop.sln --no-restore PASS (0 warnings/errors). Isolated Godot import PASS. Rendered --embroidery-target-checks PASS на1280x720/960x540: реальные mouse/touch, ровно один вариант на успешный стежок включая последний, все3 варианта наблюдались; ready/miss/finished/release без лишнего звука; проигрыватель Playing при Paused через SFX bus, контраст/переход целей сохранены. Rendered --sound-effects-checks PASS: все10 scene-owned players сохраняют уровни/bus, прежние кнопки/Skip/продажа/bell/reroll/pause regression PASS. SHA256 всех3 WAV совпадает с Downloads; import без loop/normalize/compression. git diff-check PASS. Evidence .local/checks/hf155/{build.log,import.log,embroidery-target.log,sound-effects.log,audio-hashes.txt}.
Acceptance: каждое попадание в активную чёрную точку случайно выбирает один из трёх оригинальных звуков. Отдельный RNG не изменяет случайную последовательность игровой логики. Громкость общая SFX, базовая редакторская -12dB у каждого Stitch player, max_polyphony4 для быстрых стежков. Generic click не добавляется, т.к. нарисованные точки обрабатывает Surface. Исходники и прежние незакоммиченные HF-154 изменения сохранены.
Read-set exception: для диагностики существующего ввода прочитаны только указанные crafting/target/helpers тесты. source/ не читался.
Risks/blocker: функциональных блокеров нет; прежнее предупреждение Godot Failed to read root certificate store в среде проверки. Пользовательские сохранения не затронуты, тесты в отдельном user://.
Next: пользовательская проверка громкости; commit/push не выполнялись.

### HF-154 — обновление зловещей темы и звуки смены покупателя

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-17; HF-153 DONE, пересечений нет. CP-HF-154: .local/checks/hf154/CP-HF-154.md. Точный read/write-set и out of scope в пакете. Приёмка: новая зловещая тема 90 BPM без паузы/щелчка в loop и с прежними fade; смена покупателя звучит по знаку нового предложения, без bell/дублирующего click; сохранены исходники/остальной микс. Проверки: audio analysis, build, isolated intro-music и rendered sound-effects.
Task: HF-154
Recommended status: REVIEW → принято координатором, DONE
Changed: audio/music/sinister_loop.wav/.import; audio/sfx/{reroll_success,reroll_failure}.wav/.import; scenes/Audio/SoundEffects.tscn; scripts/Audio/SoundEffects.cs; scripts/TradeScene.cs (только presentation event); tests/Integration/ProjectTest.{SoundEffects,IntroMusic}.cs; ORCHESTRATION_PLAN.md.
Checks: dotnet build ChikagoShop.sln --no-restore PASS, 0 warnings/errors. Isolated Godot import PASS. --intro-music-checks PASS: sinister 1411200 stereo frames/44100Hz/32s =12 тактов90BPM; waveform seam <0.001, RMS окна вокруг декодированного стыка без тишины/скачка; thoughtful/current mix/cues/fade/skip/exit regression PASS. Rendered --sound-effects-checks PASS: оба результата на реальных кликах, ровно один SFX по новому OfferedPrice, без click/bell; blocked/neutral/load без result; original hashes всех7 SFX совпадают, сохранены индивидуальные scene volume и отдельный SFX bus. git diff-check PASS. Evidence: .local/checks/hf154/{audio-analysis.txt,input-hashes.txt,build.log,import.log,intro-music.log,sound-effects.log}.
Acceptance: обновлённая зловещая тема только в прологе, без изменения fade/mix. Из исходника32.171315s сохранён музыкальный период32s, оставшийся render tail исключён, первые220frames (4.989ms) плавно соединены с естественным продолжением конца; PCM24→PCM16 без нормализации/смены темпа. Исходники Downloads не менялись. Удача/неудача подключены без изменения WAV, import без сжатия, редакторские уровни обоих -12dB. BuyerOfferChanged возникает после успешного reroll/RefreshOffer/SaveProgress; аудио выбирает результат по сравнению нового предложения с обычной ценой. Смена не звонит колокольчиком и не дублирует click, предыдущий result останавливается. Нулевая наценка без результата.
Read-set exception: локальная диагностика доступных audio tools и RIFF, без source/. Домен/правила/сохранения не менялись. Промежуточный SFX regression выявил ошибку fixture: предмет с единственной сменой отключал кнопку до тестового увеличения лимита. Fixture исправлен (лимит до первого reroll, проверка реального расхода попытки и отсутствия результата при пустом pool), финальный PASS.
Risks/blocker: функциональных блокеров нет. В изолированных Godot запусках прежнее Failed to read root certificate store; функциональные проверки проходят. Финальный intro-music без resource leak warnings. Проверка стыка инструментальная через samples/RMS и Godot decoder, прослушивание пользователем остаётся финальной субъективной оценкой.
Next: пользовательская проверка в feature/music; commit/push не выполнялись.
### HF-153 — локальная интеграция актуальной main с feature/music

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-17; CP-HF-153 в .local/checks/hf153/CP-HF-153.md. Пользователь просит загрузить main и проверить совместимость с музыкальными изменениями.
Read/write-set: точные пути main-paths.txt и восемь конфликтных файлов merge; новое поведение ограничено адаптацией UI/audio integration, без новой доменной логики. Резервная ветка backup/music-before-main-2026-11-17=7bd7ebc, локальная main обновлена fast-forward до8c78d49. Приёмка: обе линии истории сохранены, конфликты разрешены вручную, build/domain/адресные audio/settings/tutorial/HUD/balance проверки, никакого push. Проверочные артефакты — .local/checks/hf153.

Task: HF-153
Recommended status: REVIEW → принято координатором, DONE
Changed: локальная main fast-forward до origin/main 8c78d49; feature/music объединена с этой версией main. Вручную разрешены восемь конфликтов, сохранены обе линии истории. Новые сценовые настройки main совмещены с независимыми Music/SFX, компактной Back, звуком Skip и прежними уровнями музыки; FullscreenToggle сам обрабатывает переключение. В тесте устранена коллизия имени back. Записи параллельных задач main сохранены под заголовками MAIN-HF-*.
Checks: dotnet build ChikagoShop.sln --no-restore PASS (0 warnings/errors); dotnet test tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj --no-build --no-restore с DOTNET_ROLL_FORWARD=Major PASS 153/153. Isolated Godot import PASS; rendered --music-settings-checks, --sound-effects-checks, --progress-toggle-checks, --debug-balance-checks, --tutorial-checks PASS. Headless --menu-music-checks, --intro-music-checks, --game-music-checks PASS. SHA Git исходных двух music WAV/import, всех audio/sfx и SoundEffects.tscn совпадают с backup. git diff-check PASS. Evidence: .local/checks/hf153.
Acceptance: скачана актуальная main, совместимость с музыкальной веткой проверена локально; резервная backup/music-before-main-2026-11-17 сохранена. Пользовательские сохранения не затронуты, remote push не выполнялся.
Read-set exception: необходимая диагностика автоматического merge интеграционного MusicSettings-теста; точный scope расширен до tests/Integration/ProjectTest.MusicSettings.cs для устранения дубликата локальной переменной. source/ не читался.
Risks/blocker: музыкальные headless проверки проходят; intro-music при завершении сообщает 5 ObjectDB instances/1 resource still in use. Godot в среде проверки сообщает Failed to read root certificate store; import также сохраняет прежние duplicate UID warnings. Это не блокирует функциональные проверки, отсутствие этих предупреждений не заявляется.
Next: пользовательская проверка объединённой feature/music в Godot; публикация только по отдельному запросу.
Ниже записи `MAIN-HF-*` пришли из origin/main. Внутри сохранены их исходные HF-/CP-HF номера; префикс заголовка отличает их от параллельных музыкальных задач с теми же номерами.



### MAIN-HF-151 — публикация пачки сигарет HF-150

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-150 DONE. Пользователь разрешил commit/push.

CP-HF-151 read-set: текущий фронт, Git status/diff/remote/refs, готовый write-set/evidence HF-150, backup trade-scene-before.tscn. Write-set: ORCHESTRATION_PLAN.md; готовые docs/GAMEPLAY.md, localization/en.tres, scripts/UI/{ProgressPanel,FirstTradeTutorial}.cs, scripts/Days/DayCycleController.cs, tests/Integration/ProjectTest.{Ui,Helpers}.cs, tests/ProjectTest.cs, scenes/UI/CigaretteProgress.tscn, art/ui/cigarette_{pack_front,pack_back,stick}.png/.import; scenes/trade_scene.tscn (только индексная версия Progress/ClockActionButton/resources поверх HEAD, рабочий пользовательский файл сохраняется); Git index/commits/main/origin/main; .local/checks/hf151 и изолированная trade_scene.tscn HF139 для проверки точной staged-сцены. Out of scope: source/, main_menu.tscn, пользовательские UID/курсор/прочие узлы trade_scene, новые изменения поведения/баланса/save schema. Приёмка: проверенный HF-150 опубликован обычным push без force; HEAD=origin/main; пользовательские изменения обеих сцен сохранены локально вне коммита. Проверки: сохранённые build/import/progress/tutorial/cards/day-start HF-150 PASS, rendered progress точной индексной сцены, staged diff-check/точный scene diff, push и refs/status/hash readback.

Task: HF-151
Recommended status: REVIEW → принято координатором, DONE
Changed: готовый HF-150 опубликован коммитом771f173 в origin/main. Индексная trade_scene включает только progress/clock/resources; пользовательские UID/курсор/прочие секции и main_menu остались локально.
Checks: fetch main без расхождения; staged diff-check PASS; точная индексная scene rendered --progress-toggle-checks PASS, stderr пуст; ранее build/import/tutorial/cards/day-start PASS; обычный push PASS. Рабочие main_menu/trade_scene побайтно совпадают с snapshot HF151.
Acceptance: опубликованы пачка/3 сигареты/исходный Drive арт/управление днём на часах/тесты, пользовательские изменения сохранены.
Read-set exception: нет.
Risks/blocker: нет новых.
Next: ускорить переход после пропуска пролога по следующему запросу пользователя.

### MAIN-HF-150 — пачка сигарет вместо панели прогресса

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-149 DONE, активных пересечений нет.

CP-HF-150 read-set: текущий фронт; scenes/trade_scene.tscn (HUD Progress/clock), scripts/UI/ProgressPanel.cs, scripts/TradeScene.cs (публичное состояние/RefreshProgress/кнопки read-only), scripts/Days/DayCycleController.cs, scripts/UI/{FirstTradeTutorial,DayAnnouncementOverlay,RarityPanel,PauseMenu}.cs, scripts/Presentation/{DayCardSceneTransition,ButtonFeedbackController}.cs, scripts/UI/ZoomResponsiveUi.cs, scripts/Camera/WorldCamera.cs; tests/Integration/ProjectTest.{Ui,Tutorial,Camera,Days,DayStart,Helpers,Presentation,ButtonFeedback}.cs, tests/ProjectTest.cs, localization/en.tres, docs/GAMEPLAY.md, src/ChikagoShop.Domain/Configuration/EconomyConfig.cs; Drive UI/Готовое/UI в самой игре/Сигареты (три PNG). Write-set: ORCHESTRATION_PLAN.md; scenes/trade_scene.tscn (только HUD Progress/ClockActionButton и их ресурсы, остальной пользовательский diff сохраняется); scripts/UI/ProgressPanel.cs; scenes/UI/CigaretteProgress.tscn; art/ui/cigarette_{pack_front,pack_back,stick}.png/.import (оригиналы Drive), удаление временных cigarette_{pack,stick}.svg/.import; scripts/Days/DayCycleController.cs (клик по часам завершает/переводит день вместо удалённой кнопки); scripts/UI/FirstTradeTutorial.cs (замена ссылок на скрытую кнопку на часы); tests/Integration/ProjectTest.{Ui,Helpers}.cs; tests/ProjectTest.cs; localization/en.tres; docs/GAMEPLAY.md; .local/checks/hf150 и изолированный project .local/checks/hf139/project. Out of scope: source/, main_menu.tscn/прочие пользовательские узлы, TradeScene.cs/новые доменные правила, экономика/save schema, commit/push.

Уточнения пользователя: только3 сигареты с информацией, никаких кнопок/подписей/tooltip в пачке. Арт исключительно из указанной Drive-папки. Скрытые legacy labels/buttons остаются только для существующих TradeScene bindings. Действия восстановления уже доступны в паузе; завершение/переход дня переносятся на часы, обучение использует их. Приёмка: узкий край пачки слева; hover поочерёдно выдвигает3 сигареты с днём/суммой следующей квоты/countdown включая текущий день; уход убирает; переход на сигареты не закрывает; pin click/keyboard. Scene-authored layout и исходные PNG/AtlasTexture без редактирования пикселей. Live день/квота/язык/debug/reload, корректные финальные состояния; только3 подписи. RU/EN960×540 и1280×720; камера/pause/card/tutorial работают, часы сохраняют полный day cycle. Проверки: build/import, rendered --progress-toggle-checks, --tutorial-checks, --card-dealing-checks, --day-start-checks, screenshots RU/EN, git diff-check/сохранность пользовательских узлов.

```text
Task: HF-150
Recommended status: REVIEW → принято координатором, DONE
Changed: старый HUD заменён сценовой пачкой и3 горизонтальными сигаретами с плавным выдвижением/убиранием и задержкой между ними. Hover удерживает раскрытие при переходе к сигаретам; клик/Space закрепляет. Только3 информационные подписи: день, следующая квота, дни до срока включая текущий; прочие подписи/кнопки/tooltip пачки скрыты. Исходные3 PNG взяты из Drive/Сигареты, пиксели не менялись, прозрачные поля исключены через AtlasTexture. Имена локальных файлов соответствуют содержимому: Drive ПачкаПеред.PNG → cigarette_pack_front.png; ПачкаСпина.PNG (изображение сигареты) → cigarette_stick.png; Сигарета.PNG (чёрная задняя часть пачки) → cigarette_pack_back.png. Временные SVG удалены. Часы через новый заранее размещённый ClockActionButton вызывают существующий EndDay для досрочного закрытия/перехода, сохраняя прежнюю кнопку старта на часах. Восстановление/новая игра доступны через уже существующее меню паузы. Обучение указывает на часы. TradeScene.cs и доменные правила не менялись.
Checks: local/isolated dotnet build PASS (0 warnings/errors); local/isolated Godot import PASS (прежние4 duplicate UI UID warnings). Rendered --progress-toggle-checks PASS: настоящий hover, промежуточная позиция и stagger, сохранение tween на паузе, отсутствие drag камеры по сигарете, уход/выдвижение, click/keyboard pin, RU/EN960×540/1280×720 и fitting, скрытый Content, Start/EndDay/NextDay реальными кликами часов и возврат из карт, live стоимость/день дедлайна/смена квоты, победа/поражение, checkpoint/NewGame через реальные кнопки паузы, перезагрузка дня3 с верным countdown. Rendered --tutorial-checks, --card-dealing-checks, --day-start-checks PASS; stderr всех4 runtime-групп пуст. Просмотрены cigarette-en-1280 и cigarette-ru-960, исходные Drive PNG и свернутая пачка. git diff-check PASS; SHA256 main_menu совпадает с исходным snapshot, trade_scene вне согласованного progress/clock/resource блока совпадает с backup. Evidence .local/checks/hf150; screenshots TEMP/chikago-hf67-cigarette-*.png.
Acceptance: пачка использует указанный пользователем арт и показывает исключительно3 требуемые строки; остальные элементы прежней панели не отображаются. Цикл дня и обучение продолжают работать через часы, восстановление через паузу.
Read-set exception: для необходимой диагностики использованы ButtonFeedbackController (локальная группа), WorldCamera (ввод) и EconomyConfig (deadline schema), затем включены в CP; прочитан запрошенный Google Drive skill. source/ не читался. Пользовательские сохранения не затронуты: тесты в изолированном user:// HF139.
Risks/blocker: новых нет; прежние4 duplicate UID warnings других кнопок. Пользовательские изменения main_menu и прочих узлов trade_scene сохранены.
Next: пользовательская приёмка; commit/push не выполнялись.
```

### MAIN-HF-149 — публикация оформления настроек HF-147/HF-148

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-147/HF-148 DONE. Пользователь разрешил commit/push.

CP-HF-149 read-set: текущий фронт, Git status/diff/remote, готовый write-set/evidence HF-147/HF-148 и backup main-menu-before.tscn. Write-set: ORCHESTRATION_PLAN.md, docs/GAMEPLAY.md, scenes/UI/MusicVolumeControl.tscn, scenes/UI/FullscreenToggle.tscn, scripts/UI/FullscreenToggle.cs/.uid, scripts/MainMenu.cs, scripts/UI/{MusicVolumeControl,PauseMenu}.cs, tests/Integration/ProjectTest.MusicSettings.cs; scenes/main_menu.tscn (только индексная версия с изменением подписей Settings, пользовательский рабочий файл сохраняется); Git index/commits/main/origin/main; .local/checks/hf149. Out of scope: source/, пользовательский diff main_menu.tscn вне подписей Settings, trade_scene.tscn, новые изменения поведения, ветка feature/music. Приёмка: готовые проверенные настройки опубликованы обычным push без force, HEAD=origin/main, пользовательские изменения обеих сцен сохранены локально вне коммита. Проверки: evidence build/music-settings/pause-localization PASS, staged diff-check, проверка точного staged main_menu diff, push и сравнение refs/status.

Task: HF-149
Recommended status: REVIEW → принято координатором, DONE
Changed: готовые изменения HF-147/HF-148 опубликованы обычным push. Индексная main_menu.tscn включает только Label/внутреннюю бумагу Fullscreen и Label/пустую текстуру Back; пользовательские координаты/музыка/метаданные вне этих правок сохранены в рабочем дереве.
Checks: fetch main без расхождения; сохранённые build/music-settings/pause-localization HF-148 PASS; staged diff-check PASS; точный staged main_menu diff проверен; feature commit и push PASS.
Acceptance: оформление переключателя паузы и единый24px текст опубликованы; локальные пользовательские изменения main_menu/trade_scene вне коммитов.
Read-set exception: нет.
Risks/blocker: нет новых; origin/feature/music обновилась при fetch, в этой задаче не сливалась и не менялась.
Next: следующий запрос пользователя.

### MAIN-HF-148 — единый масштаб подписей настроек

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-147 DONE, активных пересечений нет.

CP-HF-148 read-set/write-set: ORCHESTRATION_PLAN.md, docs/GAMEPLAY.md, scenes/main_menu.tscn (только Settings subtree/ресурсы, с сохранением пользовательских координат основного меню и музыки), scenes/UI/{FullscreenToggle,MusicVolumeControl}.tscn, scripts/{MainMenu,UI/PauseMenu,UI/MusicVolumeControl}.cs (только подписи/типографика), tests/Integration/ProjectTest.MusicSettings.cs. Read-only scripts/UI/FullscreenToggle.cs и art/ui/{panel_small.png,en/fullscreen_off.png,en/back.png}, tests/Integration/ProjectTest.Localization.cs, tests/ProjectTest.cs. Write-set также .local/checks/hf148 и изолированный project .local/checks/hf139/project. Out of scope: source/, заголовки настроек, пользовательские изменения вне Settings subtree/main_menu ресурсов, trade_scene.tscn, gameplay/баланс/музыка, commit/push. Приёмка: в настройках меню и паузы подписи Fullscreen/Music volume/проценты/Language/языки/Back используют24px при прежних заголовках; рисованные края и переключатель сохранены, текст не масштабируется вместе с растровой кнопкой. Back/Fullscreen mouse/keyboard/RU-EN и layout960×540/1280×720 сохраняют работу. Проверки: build/import, rendered --music-settings-checks и --pause-localization-checks, screenshots, diff-check/сохранность пользовательских секций.

```text
Task: HF-148
Recommended status: REVIEW (принято координатором → DONE)
Changed: подписи полного экрана, громкости, процентов, языка/языковых кнопок и Назад в меню/паузе используют24px. Main-menu fullscreen и общая pause fullscreen scene содержат Label поверх внутренней белой области растровой надписи, не закрывающей бумажные края и рисунок toggle; исходные текстуры не редактировались. Back в настройках меню использует существующую пустую бумажную текстуру и Label; отдельная ChoiceBack основного меню сохраняет прежний арт. Разметка/типографика заданы в сценах; код не строит новый UI. Заголовки40px меню/32px паузы прежние. MainMenu перестал подставлять baked back-текстуру только в настройках; вся остальная локализация/управление прежние.
Checks: local/isolated build PASS (0 warnings/errors); local import PASS (прежние4 duplicate UI UID warnings). Rendered --music-settings-checks PASS: размер24 у всех7 категорий подписей в обоих меню/RU-EN/960×540 и1280×720, fitting fullscreen label, заголовок меню40, реальные клики Назад/Fullscreen и клавиатура, громкость и сохранение, сохранение паузы. Rendered --pause-localization-checks PASS; runtime stderr обеих групп пуст. Просмотрены menu-volume-en-1280.png и pause-volume-ru-960.png. git diff-check PASS. Prefix main_menu.tscn до FullscreenButton полностью совпадает с пользовательским backup (ресурсы/музыка/координаты основного меню/заголовок); trade_scene.tscn SHA256 сохранён. Evidence .local/checks/hf148.
Acceptance: подписи одного масштаба при прежних заголовках и рабочем рисованном UI.
Read-set exception: нет; source/ не читался, изолированный user:// HF139.
Risks/blocker: новых нет; пользовательские изменения вне настройки main_menu и вся trade_scene сохранены.
Next: пользовательская приёмка; commit/push не выполнялись.
```

### MAIN-HF-147 — рисованный переключатель полного экрана в паузе

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-146 DONE, активных пересечений нет.

CP-HF-147 read-set: текущий фронт; scripts/UI/PauseMenu.cs, scripts/MainMenu.cs (fullscreen localization read-only), tests/Integration/ProjectTest.{MusicSettings,Localization,Helpers}.cs, tests/ProjectTest.cs; art/ui/ru/fullscreen_{on,off}.png, art/ui/en/fullscreen_{on,off}.png; docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, scripts/UI/PauseMenu.cs, новые scripts/UI/FullscreenToggle.cs/.uid и scenes/UI/FullscreenToggle.tscn, tests/Integration/ProjectTest.MusicSettings.cs, docs/GAMEPLAY.md; .local/checks/hf147 и изолированный project .local/checks/hf139/project. Out of scope: source/, пользовательские main_menu.tscn/trade_scene.tscn, прочие стили паузы/сохранения/баланс/музыка, commit/push. Приёмка: переключатель в паузе использует те же4 рисованные локализованные текстуры, что главное меню; состояния отражают фактический fullscreen, реальное переключение работает на паузе мышью/клавиатурой; reopen/смена языка/оконного режима синхронизируют состояние без повторного сигнала. При960×540 и1280×720 кнопка вписывается в панель и совпадает по ширине с громкостью, рисованная геометрия хранится в отдельной сцене. Проверки: build, import, rendered --music-settings-checks, --pause-localization-checks; git diff-check/сохранность пользовательских сцен.

```text
Task: HF-147
Recommended status: REVIEW (принято координатором → DONE)
Changed: стандартный CheckButton полного экрана в паузе заменён сценой FullscreenToggle.tscn с теми же4 рисованными текстурами RU/EN on/off, что главное меню. Размер360×54 и растяжение по ширине заданы в сцене. Скрипт переключает реальный режим окна и без сигналов синхронизирует состояние/текстуру при открытии, смене языка и внешней смене режима; работает при активной паузе. Прочие элементы настроек и пользовательские сцены не менялись. Docs и regression-проверки обновлены.
Checks: local/isolated dotnet build PASS (0 warnings/errors); local/isolated import PASS (прежние4 duplicate UI UID warnings). Rendered --music-settings-checks PASS: RU/EN960×540 и1280×720, совпадение texture path с главным меню, соответствие actual fullscreen и ButtonPressed, реальный mouse enable/Space disable в обоих языках без снятия паузы, reopen/external window mode sync, громкость/сохранение/выравнивание/Назад. Rendered --pause-localization-checks PASS, игровой прогресс и время не изменены; runtime stderr обеих групп пуст. Скриншот pause-volume-ru-960.png просмотрен. git diff-check PASS; SHA256 обеих пользовательских сцен сохранён. Evidence: .local/checks/hf147; preview .local/checks/hf139/project/pause-volume-*.png.
Acceptance: переключатель полного экрана в паузе оформлен как в главном меню и сохраняет управление мышью/клавиатурой, локализацию и паузу.
Read-set exception: нет; source/ не читался, тесты запускались только с изолированным user:// HF139.
Risks/blocker: новых нет; прежние duplicate UID warnings.
Next: пользовательская приёмка; commit/push не выполнялись.
```

### MAIN-HF-146 — сверка GitHub и завершение публикации дебаг-панели

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-145 DONE. Пользователь ранее запросил push, сейчас запросил сверку актуальной версии; публикация ранее не выполнена.

CP-HF-146 read-set: текущий фронт, git status/diff/log/remote; готовые файлы/evidence HF-145 и сравнение с изолированной проверенной копией. Write-set: ORCHESTRATION_PLAN.md, Git index/commits/main/origin/main; точные готовые файлы HF-145: docs/GAMEPLAY.md, project.godot, scenes/UI/DebugBalanceNumberRow.tscn, scenes/UI/DebugBalancePanel.tscn, scripts/Debug/DebugBalancePanel.cs/.uid, scripts/Debug/DebugBalanceSettings.cs/.uid, scripts/Configuration/EconomyConfigProvider.cs, scripts/Configuration/SceneConfigurationDefaults.cs, scripts/Crafting/WorkshopItemCatalog.cs, scripts/Days/DayCycleController.cs, scripts/TradeScene.cs, scripts/UI/DayTransitionCard.cs, scripts/UI/EmbroideryGame.cs, scripts/UI/PolishingSurface.cs, scripts/UI/RarityPanel.cs, src/ChikagoShop.Domain/Configuration/BalanceProfile.cs, src/ChikagoShop.Domain/Days/DayUpgradeRules.cs, src/ChikagoShop.Domain/Days/DayUpgradeSession.cs, src/ChikagoShop.Domain/Days/DaySession.cs, src/ChikagoShop.Domain/Crafting/EmbroiderySession.cs, src/ChikagoShop.Domain/Crafting/PolishingSession.cs, src/ChikagoShop.Domain/Crafting/RarityItemTable.cs, tests/ChikagoShop.Domain.Tests/BalanceProfileTests.cs, tests/Integration/ProjectTest.DebugBalance.cs/.uid, tests/ProjectTest.cs. Out of scope: source/, пользовательские scenes/main_menu.tscn и scenes/trade_scene.tscn, новые изменения поведения, force push. Приёмка: fetch и сравнение истории; готовая панель опубликована обычным push; HEAD=origin/main; обе локальные сцены сохранены вне коммита. Проверки: существующие HF-145 build/domain153/rendered checks PASS, сравнение с проверенной копией, staged diff-check и итоговые refs/status.

Task: HF-146
Recommended status: REVIEW → принято координатором, DONE
Changed: fetch подтвердил HEAD=origin/main=512b755 без новых коммитов коллег; завершена ранее запрошенная публикация готовой панели HF-145, feature commit1cbe8e2 отправлен в origin/main обычным push. Удалена только лишняя пустая строка EOF нового шаблона для staged diff-check.
Checks: сравнение с проверенной изолированной копией PASS (TradeScene различался только окончаниями строк, документация содержит позднее описание); сохранённые build/domain153/153/debug-balance/tutorial/card-dealing/craft-localization HF-145 PASS; staged diff-check PASS; push PASS.
Acceptance: готовая панель опубликована; пользовательские main_menu.tscn (в том числе изменение размещения меню) и trade_scene.tscn сохранены локально вне коммита.
Read-set exception: нет.
Risks/blocker: нет новых; два пользовательских diff сцен остаются незакоммиченными.
Next: продолжить работу с синхронизированной main.

### MAIN-HF-145 — сохраняемая отладочная панель баланса

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; HF-144 DONE, активных пересечений нет.
Расширение координатором: write-set включает src/ChikagoShop.Domain/Days/DaySession.cs — конструктор с явными пределами duration для runtime debug-профиля (5–3630 секунд), прежние defaults90–120 и существующие domain-тесты не меняются. Без этого прежний clamp скрыто игнорировал бы настроенную длительность дня.
Расширение координатором: write-set/read-set включает scripts/UI/RarityPanel.cs (актуальные нормированные шансы, средние цены и попытки после изменения профиля), scenes/UI/DebugBalanceNumberRow.tscn (сценовый шаблон строки редактирования). Диагностика дополнительно открыла ChikagoShop.csproj и src/ChikagoShop.Domain/Trading/TradeOfferRules.cs, торговые правила не меняются.
Расширение read-set для проверки F10 из главного меню: scenes/main_menu.tscn, scripts/MainMenu.cs (без правок). Диагностика загрузки дня прочитала только LoadProgress в scripts/Persistence/TradeScene.Persistence.cs, без правок; путь сохранения и schema прежние.

CP-HF-145 read-set: текущий фронт; project.godot, ChikagoShop.csproj, config/economy.v1.json; scripts/Configuration/{EconomyConfigProvider,SceneConfigurationDefaults}.cs, scripts/Crafting/WorkshopItemCatalog.cs, scripts/Days/DayCycleController.cs, scripts/Debug/DebugHotkeysController.cs, scripts/TradeScene.cs, scripts/UI/{EmbroideryGame,PolishingSurface,DayTransitionCard,PauseMenu,FirstTradeTutorial}.cs, scripts/Presentation/{CraftButtonPresentation,DayCardSceneTransition}.cs; src/ChikagoShop.Domain/Configuration/{EconomyConfig,EconomyConfigLoader,EconomyConfigValidator}.cs, src/ChikagoShop.Domain/Crafting/{WeightedItemTable,RarityItemTable,EmbroiderySession,PolishingSession}.cs, src/ChikagoShop.Domain/Days/{DayUpgradeSession,DaySession}.cs; tests/ProjectTest.cs, tests/Integration/ProjectTest.{Debug,Crafting,Economy,Days,Helpers,Tutorial}.cs, tests/ChikagoShop.Domain.Tests/{DayUpgradeSessionTests,RarityItemTableTests,DebugSupportTests}.cs, docs/GAMEPLAY.md; scenes/trade_scene.tscn (только сохранность пользовательского diff). Write-set: ORCHESTRATION_PLAN.md, docs/GAMEPLAY.md, project.godot; новые src/ChikagoShop.Domain/Configuration/BalanceProfile.cs и src/ChikagoShop.Domain/Days/DayUpgradeRules.cs; src/ChikagoShop.Domain/Days/DayUpgradeSession.cs, src/ChikagoShop.Domain/Crafting/{RarityItemTable,EmbroiderySession,PolishingSession}.cs; новые scripts/Debug/{DebugBalanceSettings,DebugBalancePanel}.cs/.uid и scenes/UI/DebugBalancePanel.tscn; scripts/Configuration/{EconomyConfigProvider,SceneConfigurationDefaults}.cs, scripts/Crafting/WorkshopItemCatalog.cs, scripts/Days/DayCycleController.cs, scripts/UI/{EmbroideryGame,PolishingSurface,DayTransitionCard}.cs; scripts/TradeScene.cs (только замена закэшированных config-ссылок на актуальные getters и тонкий refresh adapter/передача актуального размера loot table, без новой доменной логики); tests/ProjectTest.cs, новые tests/Integration/ProjectTest.DebugBalance.cs/.uid, tests/ChikagoShop.Domain.Tests/BalanceProfileTests.cs; .local/checks/hf145 и изолированный test project .local/checks/hf139/project. Out of scope: source/, изменение пользовательской trade_scene.tscn, игровые save schema/реальные сохранения, арт/музыка/публикация. Приёмка: F10 открывает/закрывает scene-authored debug modal с прокруткой; редактируются стартовые деньги/энергия/квоты, длительности дня/вышивки/полировки, число предметов/стоимость крафта/открытия мастерской, веса редкостей/попытки, цена/редкость каждого предмета, цены/веса карт. Применить сейчас обновляет будущие действия и UI без повторной оплаты/сброса партии; новая игра применяет стартовые значения; профиль user://debug_balance.json переживает перезапуск debug build, production build игнорирует; сброс к исходному профилю и отмена изменений. Проверяется атомарная запись/валидация диапазонов/весов и повреждённого файла, отсутствие утечек паузы/прохода ввода, RU-подписи/960×540 и1280×720, reload/покупка карт/реальный крафт. Команды: dotnet build ChikagoShop.sln --no-restore; dotnet test tests/ChikagoShop.Domain.Tests --no-restore; Godot rendered --debug-balance-checks, --tutorial-checks, --card-dealing-checks, --craft-localization-checks; git diff --check.

```text
Task: HF-145
Recommended status: REVIEW (принято координатором → DONE)
Changed: сценовая F10-панель с5 прокручиваемыми вкладками и шаблоном строки: стартовые деньги/энергия/квоты/ожидание покупателя, длительность дня и двух мини-игр, количество и стоимость крафта/открытия музыки, веса5 редкостей/попытки, цена и редкость16 предметов, цены/веса7 карт и воспроизводимый seed. Отдельный BalanceProfile валидируется и атомарно сохраняется в user://debug_balance.json до публикации активного профиля. Debug-only provider загружает его при старте, runtime getters/catalog cache используют актуальную revision. Применение сохраняет прошедшее время дня и не пополняет деньги/энергию; новая игра использует новые стартовые значения. Устаревшие tooltip-числа заменены нормированными шансами и средними ценами актуальных предметов. Карты по умолчанию сохраняют расписание; режим1 использует веса без повторов и стабильное восстановление по seed/дню. Release игнорирует профиль и удаляет панель. TradeScene получил только config getters/refresh adapter, новые доменные правила вынесены из него. Документация обновлена.
Checks: local/isolated build PASS (0 warnings/errors); domain153/153 PASS. Local/isolated Godot import PASS (прежние4 duplicate UI UID warnings). Rendered --debug-balance-checks PASS: F10 из меню/игры, Esc/echo/блокировка F6/F7,5 вкладок960×540/1280×720, настоящий ввод числа и apply, совместное применение скрытых вкладок, реальные цены/3 предмета/редкость/время вышивки и полировки, цена продажи, карточка за17$ с правильным списанием/наградой, новый run и чтение профиля/игры с диска, пауза поверх паузы/мини-игры с заморозкой обоих таймеров, invalid draft/нулевое распределение/ошибка записи не меняют активный профиль, reset/defaults/cancel, повреждённый JSON не перезаписывается и даёт baseline. Последняя группа содержит только ожидаемый warning от намеренно повреждённого JSON, без ERROR. Rendered --tutorial-checks, --card-dealing-checks, --craft-localization-checks PASS; их stderr пуст. Просмотрены960×540 предметы/карты и1280×720 партия. git diff-check PASS; SHA256 пользовательской trade_scene.tscn совпадает с backup HF139. Evidence: .local/checks/hf145; screenshots TEMP/chikago-hf67-debug-balance-*.png.
Acceptance: перечисленные пользователем настройки реально участвуют в игровом процессе и сохраняются между запусками debug-билда; есть live apply, новая игра и возврат исходных значений. F10/закрытие без apply не сохраняют draft. Профиль не меняет shipped config или игровую save schema.
Read-set exception: диагностические чтения LoadProgress, TradeOfferRules и MainMenu перечислены в расширениях CP; исходники source/ и реальные пользовательские сохранения не читались. Тесты использовали прежний изолированный user:// HF139.
Risks/blocker: новых блокеров нет. Применение внутри мини-игры/подтверждения отключено (осмотр доступен), во время выбора карт панель не открывается. Стартовые деньги/энергия выдаются только при NewGame; время активной мини-игры не переписывается. Эти границы указаны в панели и документации.
Next: пользовательская приёмка через F10; commit/push не выполнялись.
```

### MAIN-HF-144 — публикация настроек и исправлений обучения

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-143 DONE. Пользователь разрешил commit/push.

CP-HF-144 read-set: текущий фронт, Git status/diff/remote, evidence .local/checks/hf143. Write-set: Git index/commits/main/origin/main; готовый diff HF-143 в ORCHESTRATION_PLAN.md, docs/GAMEPLAY.md, localization/en.tres, scenes/UI/FirstTradeTutorial.tscn, scenes/main_menu.tscn, scripts/Camera/WorldCamera.cs, scripts/MainMenu.cs, scripts/UI/FirstTradeTutorial.cs, tests/Integration/ProjectTest.MusicSettings.cs, tests/Integration/ProjectTest.Tutorial.cs. Out of scope: source/, прежний пользовательский diff scenes/trade_scene.tscn, новые изменения поведения. Приёмка: обычный push без force; HEAD=origin/main; локальная пользовательская сцена сохранена. Проверки: staged diff-check, существующие build/tutorial/music-settings/camera-limits/card-dealing PASS HF-143, push и сравнение refs/status.

Task: HF-144
Recommended status: REVIEW → принято координатором, DONE
Changed: готовые настройки и исправления обучения опубликованы коммитом b055d8c в origin/main.
Checks: staged diff-check PASS; обычный push PASS; сохранённые build/tutorial/music-settings/camera-limits/card-dealing HF-143 PASS.
Acceptance: опубликован только write-set HF-143; прежняя пользовательская scenes/trade_scene.tscn осталась локально без изменений.
Read-set exception: нет.
Risks/blocker: нет новых.
Next: следующий запрос пользователя.

### MAIN-HF-143 — настройки и завершение учебной партии

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-142 DONE, активных пересечений нет.

CP-HF-143 read-set: текущий фронт; scripts/MainMenu.cs, scenes/main_menu.tscn, scripts/UI/{FirstTradeTutorial,MusicVolumeControl,PauseMenu}.cs, scripts/Camera/WorldCamera.cs, scripts/TradeScene.cs (существующий NewGame), scripts/Presentation/{TutorialCameraCheckpoint,DayCardSceneTransition,MonochromePresentation}.cs, scenes/UI/{FirstTradeTutorial,MusicVolumeControl}.tscn, themes/game_theme.tres, art/ui/{button_back,panel_small}.png, art/ui/music_slider_grip.svg; tests/Integration/ProjectTest.{Tutorial,MusicSettings,CameraLimits,Helpers}.cs, tests/ProjectTest.cs, docs/GAMEPLAY.md, localization/en.tres; .local/checks/hf139/project/override.cfg. Write-set: ORCHESTRATION_PLAN.md, scripts/MainMenu.cs (привязки настроек), scenes/main_menu.tscn (сценовая разметка и текстурные стили настроек), scripts/UI/FirstTradeTutorial.cs, scenes/UI/FirstTradeTutorial.tscn, scripts/Camera/WorldCamera.cs, tests/Integration/ProjectTest.{Tutorial,MusicSettings}.cs, docs/GAMEPLAY.md, localization/en.tres; .local/checks/hf143 и изолированный test project .local/checks/hf139/project. Out of scope: source/, пользовательский diff scenes/trade_scene.tscn, новая доменная логика/изменение TradeScene.cs, музыка/save schema, commit/push. Приёмка: настройки в бумажном стиле, видимая рабочая Назад в RU/EN960×540 и1280×720; разметка хранится в сцене. Finish/подтверждённый skip через существующий NewGame восстанавливает исходный день/деньги/энергию/товары и сохраняет completed tutorial без повторного запуска; отмена skip не сбрасывает день. Камера принимает пользовательский ввод только на практике, после неё закреплена на checkpoint, в обычной игре свободна; повторная подсказка ЛКМ/колесо не показывается. Проверки: build, rendered tutorial/music-settings/camera-limits/card-dealing, diff-check, сохранность пользовательской сцены.

```text
Task: HF-143
Recommended status: REVIEW (принято координатором → DONE)
Changed: настройки главного меню хранятся в main_menu.tscn (громкость, язык, текстурные стили и компактная Назад). Убрано ограничение максимальной высоты300 и runtime-построение этой разметки; MainMenu связывает существующие кнопки с обработчиками. Бумажные текстуры задают неровные края переключателей языка; выбранный язык светлее. Finish и подтверждённый skip вызывают существующий NewGame без повторного запуска tutorial, сохраняют finished и плавно возвращают камеру к исходному обзору. Тексты итога/подтверждения сообщают о сбросе первого дня. WorldCamera.NavigationLocked блокирует drag/wheel/остаточное масштабирование после практики; checkpoint-перелёты продолжают работать. Повторная строка Navigation удалена из tutorial scene. TradeScene.cs не менялся.
Checks: local/isolated build PASS (0 warnings/errors); isolated import PASS (только прежние4 duplicate UI UID warnings); rendered tutorial PASS (реальные drag/wheel блокируются на Start/Craft/Select/Place/WaitBuyer/Offer/Summary; практика доступна; finish/skip восстанавливают стартовые ресурсы/товары/состояние дня и сохраняются при reload; отмена skip сохраняет оплаченный крафт). Rendered music-settings PASS (RU/EN960×540/1280×720, видимая текстурная Назад, реальный клик возвращает меню, громкость/пауза/сохранение работают). Rendered camera-limits и card-dealing PASS. stderr всех4 runtime-групп пуст; git diff-check PASS; SHA256 пользовательской trade_scene.tscn совпадает с hf139 backup. Снимок menu-volume-ru-960.png просмотрен. Evidence: .local/checks/hf143, screenshots в .local/checks/hf139/project.
Acceptance: все требования подтверждены; в обычной игре после обучения камера свободна, следующий первый день ожидает нажатия Начать со стартовыми ресурсами; finished не вызывает цикл повторного обучения.
Read-set exception: нет; source/ и реальные пользовательские сохранения не читались. Рендер выполнялся в прежнем изолированном user:// HF139.
Risks/blocker: прежние duplicate UID warnings; повтор обучения через паузу также заканчивается сбросом учебной партии, что явно указано в документации и подтверждении skip.
Next: пользовательская приёмка; commit/push не выполнялись.
```

### MAIN-HF-142 — публикация обучения HF-140/HF-141

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-140/HF-141 DONE. Пользователь разрешил commit/push.

CP-HF-142 read-set: текущий фронт, diff/status/log/remote Git, evidence `.local/checks/hf140` и `.local/checks/hf141`. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md, docs/GAMEPLAY.md, localization/en.tres, scenes/UI/FirstTradeTutorial.tscn, scripts/Camera/WorldCamera.cs, scripts/TradeScene.cs, scripts/UI/FirstTradeTutorial.cs, scripts/Presentation/TutorialCameraCheckpoint.cs и .uid, tests/Integration/ProjectTest.CameraLimits.cs, tests/Integration/ProjectTest.Menu.cs, tests/Integration/ProjectTest.Tutorial.cs, tests/ProjectTest.cs (публикация готового diff). Out of scope: source/, пользовательский diff scenes/trade_scene.tscn, новые изменения поведения. Приёмка: готовые изменения обучения отправлены обычным push без force, HEAD совпадает с origin/main; пользовательская локальная сцена сохранена вне коммита. Проверки: существующие build/tutorial/camera-limits/card-dealing/intro-transition PASS, staged diff-check, push и сравнение remote HEAD.

Task: HF-142
Recommended status: REVIEW → принято координатором, DONE
Changed: опубликован готовый write-set CP-HF-142, feature commit `48e87ff` в origin/main.
Checks: staged diff-check PASS; обычный push origin main PASS; build/tutorial/camera-limits/card-dealing/intro-transition PASS по evidence HF-140/HF-141.
Acceptance: камера по этапам, практика управления, обучение каждой новой партии и подтверждение пропуска опубликованы; пользовательский diff scenes/trade_scene.tscn сохранён локально вне коммита.
Read-set exception: нет.
Risks/blocker: нет новых; прежние предупреждения UID и общий tooltip baseline не относятся к этой публикации.
Next: продолжить работу по следующему запросу пользователя.

### MAIN-HF-141 — обучение каждой новой партии и подтверждение пропуска

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-140 DONE.
Расширение координатором: write-set включает tests/Integration/ProjectTest.Menu.cs. Startup fixture до _Ready требовала ненулевой GameScene, хотя текущая опубликованная main_menu.tscn явно хранит null и использует fallback EnterGame; разрешить shipped null и явно проверить оба варианта (PackedScene/null), добавить stale tutorial cfg и проверку новой партии после настоящего перехода меню. Runtime MainMenu не меняется.
CP-HF-141 read-set: текущий фронт, scripts/UI/FirstTradeTutorial.cs, scenes/UI/FirstTradeTutorial.tscn, scripts/TradeScene.cs (_Ready LoadProgress-result, NewGame lifecycle), scripts/MainMenu.cs (EnterGame/newGame read-only), scripts/UI/PauseMenu.cs (_Input/newGame read-only), tests/Integration/ProjectTest.{Tutorial,Menu,Helpers}.cs, tests/ProjectTest.cs, localization/en.tres, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, scripts/UI/FirstTradeTutorial.cs, scenes/UI/FirstTradeTutorial.tscn, scripts/TradeScene.cs (только публичный результат загрузки и событие завершённого NewGame, без доменной логики), tests/Integration/ProjectTest.Tutorial.cs, tests/ProjectTest.cs (описание группы), localization/en.tres, docs/GAMEPLAY.md; .local/checks/hf141. Out of scope: source/, пользовательская trade_scene.tscn, save schema/экономика/музыка, изменения MainMenu/PauseMenu, commit/push. Приёмка: новая партия из меню (game save отсутствует, даже если tutorial cfg старый) или NewGame в паузе начинает обучение заново с практики камеры; Continue сохраняет finished/skip/partial прогресс своей партии. Skip открывает сценовый modal с Да/Продолжить, до подтверждения finished не записывается; отмена/Esc возвращает тот же этап, подтверждение завершает и возвращает обзор. Modal блокирует мир и останавливает таймер, безопасно освобождает собственную паузу при confirm/cancel/restart/exit; RU/EN и две формы окна. Проверки: build/import, rendered tutorial и intro-transition (новая/продолжить), реальное confirm/cancel/Esc, повторная новая игра после finish/skip, stale cfg/fresh run; diff-check/сохранность локальной сцены.
```text
Task: HF-141
Recommended status: REVIEW (принято координатором → DONE)
Changed: новая партия снова запускает обучение. TradeScene только публикует результат LoadProgress через LoadedSavedGame и событие NewGameStarted после завершения прежнего reset/save; доменные правила и save schema не менялись. Tutorial Initialize игнорирует старый tutorial cfg при отсутствии загруженного game save; начало обучения фиксирует новую партию через существующий PersistProgress до запуска дня, чтобы сохранить частичную практику камеры при Continue. NewGameStarted сбрасывает tutorial/оба camera-флага, Continue восстанавливает текущее решение. Skip теперь открывает scene-authored modal: Продолжить обучение/Esc отменяют, Да, пропустить завершает/сохраняет и возвращает overview. Overlay блокирует мир; собственная пауза освобождается при cancel/confirm/reset/exit, Cancel получает исходный focus. Добавлены RU/EN тексты и docs.
Checks: local/isolated build PASS (0 warnings/errors); local import PASS (прежние duplicate UI UID warnings). Rendered tutorial PASS — подтверждение до save finished, блокировка клика Start за modal, реальный cancel/confirm/Esc без открытия PauseMenu, RU/EN960×540/1280×720, возврат overview, Continue после skip/finish, новая партия после finish/skip, настоящий NewGameButton в паузе, reset/exit с открытым modal без утечки pause, stale finished cfg без game save, прежняя partial practice/первый торговый цикл/камера. Intro-transition PASS —4 настоящих меню NewGame путей (полный пролог/skip × PackedScene/null fallback) со старым finished cfg начинают обучение, настоящий Continue сохраняет завершённое обучение и деньги. Старые fixture-ожидания shipped GameScene!=null и автоматического clock90 актуализированы под опубликованный null fallback и ручной Start; runtime MainMenu не менялся. Preview подтверждения просмотрен. Обе rendered группы exit0/stderr пуст; diff-check PASS; пользовательская trade_scene.tscn побайтно сохранена. Evidence: .local/checks/hf141.
Acceptance: каждый новый run позволяет пройти обучение заново; только подтверждённый skip отключает его в текущем run. Загрузка текущей партии не сбрасывает обучение; отмена сохраняет этап и игровой процесс.
Read-set exception: нет; write-set расширен координатором для устаревшей startup fixture. Изолированный user:// HF-139, source/ не читался, пользовательские сохранения тестами не затрагивались.
Risks/blocker: прежние UID warnings; новый UI хранится в сцене, продуктовых изменений MainMenu/PauseMenu нет.
Next: пользовательская приёмка. Commit/push не выполнялись.
```
### MAIN-HF-140 — камера по этапам обучения и практика управления

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-139 DONE, активных пересечений нет.
CP-HF-140 read-set: текущий фронт, scripts/UI/FirstTradeTutorial.cs, scripts/Camera/WorldCamera.cs, scenes/UI/FirstTradeTutorial.tscn, scenes/trade_scene.tscn (геометрия read-only), scripts/Presentation/DayCardSceneTransition.cs, tests/Integration/ProjectTest.{Tutorial,CameraLimits,CardDealing,Helpers}.cs, tests/ProjectTest.cs, localization/en.tres, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, scripts/UI/FirstTradeTutorial.cs, scripts/Camera/WorldCamera.cs, scenes/UI/FirstTradeTutorial.tscn, новый scripts/Presentation/TutorialCameraCheckpoint.cs/.uid, tests/Integration/ProjectTest.{Tutorial,CameraLimits}.cs, tests/ProjectTest.cs (описание группы), localization/en.tres, docs/GAMEPLAY.md; .local/checks/hf140. Out of scope: source/, пользовательская trade_scene.tscn, арт, музыка, экономика/TradeScene.cs/save schema, commit/push. Приёмка: scene-authored checkpoint каждого этапа, плавные ограниченные перелёты позиции и масштаба с возвратом к начальному overview в конце/skip; начальный этап требует фактического перемещения за ЛКМ по фону и масштабирования колесом, автоматика/клики по UI не засчитываются; частичный прогресс хранится в отдельном tutorial cfg. Перелёт не повторяется каждый кадр, отменяется при modal/pause/resize/reset и не конфликтует с показом карт, replay/reload/exit безопасны. Проверки: build, import, rendered tutorial/camera-limits/card-dealing, реальный ввод/промежуточный кадр/конечные checkpoints/обе формы окна/RU-EN/reload/skip/завершение/пауза; diff-check и сохранность локальной сцены.
```text
Task: HF-140
Recommended status: REVIEW (принято координатором → DONE)
Changed: в FirstTradeTutorial.tscn заданы8 Marker2D checkpoints с TutorialCameraCheckpoint (FocusTarget/ReferenceViewSize/offset/Overview), длительность0,8с экспортирована. WorldCamera выполняет отменяемый ограниченный FocusOn, блокирует навигацию на время перелёта, сообщает только реальный pan/zoom; resize/reset/exit сбрасывают фокус. FirstTradeTutorial переносит камеру при смене этапа, отменяет активный учебный фокус при pause/modal, восстанавливает после закрытия, не вмешивается в обзор для карт. Начальный Camera-step требует64 экранных пикселя фактического pan заЛКМ и колесо, показывает два индикатора; автоматические движения/GUI drag не засчитываются. Оба флага хранятся в отдельном tutorial.cfg, partial reload сохраняется, replay сбрасывает практику. Finish/Skip возвращают стартовый overview. Добавлены RU/EN тексты, документация и regression.
Checks: local/isolated build PASS (0 warnings/errors); local/isolated import PASS (прежние duplicate UI UID warnings). Rendered tutorial PASS — настоящий GUI drag не засчитан, реальный background drag и wheel засчитаны отдельно, partial reload, промежуточный кадр перелёта без auto-completion, последующие checkpoint/видимость цели, первая продажа, пауза/resize/RU-EN960×540/1280×720/replay/reset/skip/finish с возвратом к виду NewGame. Camera-limits PASS — полный viewport внутри bounds на каждом кадре перелёта к заведомо внешней точке, ручной ввод блокируется, resize отменяет фокус, прежние zoom/drag/reset limits сохранены. Card-dealing PASS — прежний возврат к overview/скрытие UI/раздача/отмена/purchase/skip/resize. Preview camera/craft просмотрены. git diff-check PASS; SHA256 локальной пользовательской trade_scene.tscn совпадает с backup HF-139. Evidence: .local/checks/hf140.
Acceptance: камера плавно показывает нужный участок каждого этапа; обучение управлению камерой требует действий игрока и не расходует ресурсы само. После завершения и пропуска возвращается исходный обзор; прежнее обучение/показ карт совместимы.
Read-set exception: нет; для проверки использована готовая изолированная копия HF-139 с отдельным user://. source/ не читался.
Risks/blocker: прежние UID warnings вне задачи. Параметры checkpoint видны в инспекторе; завершённые/пропущенные tutorial cfg не запускаются повторно автоматически. Игровые сохранения и пользовательская сцена не менялись.
Next: визуальная приёмка пользователем. Commit/push не выполнялись.
```
### MAIN-HF-139 — аудит последних коммитов и безопасное обновление

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-134–138 DONE. Пользователь разрешил загрузить совместимые изменения после проверки.
CP-HF-139 read-set: текущие фронты, git diff/log/ancestry4bcbb27..900e9b1; точные21 изменённых пути музыкальных коммитов (ORCHESTRATION_PLAN.md, art/ui/music_slider_grip.svg/.import, docs/GAMEPLAY.md, localization/en.tres, project.godot, scenes/UI/MusicVolumeControl.tscn, scenes/main_menu.tscn, scripts/Audio/{GameplayMusicDirector,IntroMusicDirector,MusicSettings}.cs/.uid, scripts/MainMenu.cs, scripts/UI/{MusicVolumeControl,PauseMenu}.cs/.uid, tests/Integration/ProjectTest.{GameMusic,IntroMusic,MusicSettings}.cs/.uid, tests/ProjectTest.cs); локальный diff scenes/trade_scene.tscn; сравнение деревьев art/items, scenes/UI/{CraftButton,DialoguePlayer}.tscn, scripts/Crafting/WorkshopItemCatalog.cs, scripts/UI/{EmbroideryGame,PolishingSurface}.cs, shaders/dialogue_portrait.gdshader, src/ChikagoShop.Domain/Crafting. Write-set: fast-forward main к origin/main (перечисленные21 пути), ORCHESTRATION_PLAN.md после обновления; .local/checks/hf139. Out of scope: source/, новые правки поведения, пользовательские сохранения, reset/stash/drop, commit/push. Приёмка: наши изменения присутствуют, музыкальный diff совместим, main обновлена; локальная сцена сохранена побайтно; build/domain/rendered music/asset/dialogue/craft checks проходят. Проверки: ancestry/protected diff, backup+SHA256, ff-only, изолированная сборка и рендер, итоговый git status/revisions. Протокол: CP сначала записан в .local/checks/hf139/context.md, чтобы не блокировать fast-forward входящего плана; сразу после обновления внесён сюда.
```text
Task: HF-139
Recommended status: REVIEW (принято координатором → DONE)
Changed: main обновлена fast-forward4bcbb27→900e9b1. Проверены четыре коммита коллеги:1fbc31f музыка пролога/согласованные уровни трёх тем;162bcfe сохранение громкости на отдельной Music-шине и общий ползунок меню/паузы;366cb02 размеры и оформление нового регулятора;900e9b1 отчёт о слиянии. Итого21 путь; нет правок предметных артов, каталога/редкостей, игровой сцены, портретов/шейдера, кнопки крафта, выбора мини-игр. Локальный trade_scene.tscn сохранён побайтно, backup в evidence. Его diff: UID ресурсов/узлов, порядок ресурсов, удаление двух повторяющих сцену курсорных overrides; координаты/текстуры16 предметов прежние. Подготовлены сборка и локальный Godot import; новых продуктовых правок нет.
Checks: наш4bcbb27 — ancestor origin/main PASS; protected feature diff=0 PASS. local/isolated build PASS (0 warnings/errors), domain141/141 PASS. Свежая изолированная копия: rendered music-settings/intro-music/game-music/asset-replacement/dialogue-portraits/craft-localization PASS (все exit0, stderr пуст). Первичный import сообщил об ещё не готовом font/theme cache; после завершения импорта повторный запуск PASS с прежними duplicate UI UID warnings, local import также только с прежними UID warnings. Локальная сцена SHA256 до/после merge и import совпадает. HEAD=origin/main=900e9b1; git diff-check PASS; status содержит только прежнюю локальную сцену и новый отчёт плана. Evidence: .local/checks/hf139.
Acceptance: отката наших опубликованных изменений нет; все4 новых коммита совместимы, загружены и проверены. Новое оформление ограничено элементом громкости в настройках; прежние арты/фон/разметка крафта не менялись. Громкость и игровой прогресс пользователя не затронуты тестами.
Read-set exception: нет; source/ не читался. CP до ff хранился локально, затем сразу внесён в обновлённый план; import меняет только производный Godot cache.
Risks/blocker: прежние duplicate UI UID warnings; существующая незакоммиченная сцена оставлена пользователю. Новых препятствий к работе нет.
Next: продолжать на актуальной main; commit/push в этой задаче не выполнялись.
```


### HF-152 — финальная проверка, коммиты и push только feature/music

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-149…151 DONE. Пользователь явно запросил перепроверку всех изменений, тематические коммиты и push музыкальной ветки, без main.

Расширение координатором: read/write-set включает tests/Integration/ProjectTest.ButtonFeedback.cs — вместо устаревшей фиксированной границы70% для EN Back проверить фактическое aspect-fit изображения и пиксели пустых полей; scripts/Presentation/ButtonFeedbackController.cs прочитан только для диагностики. Итоговый контрольный коммит плана также включает сохранённый исторический отчёт HF-139, чтобы после публикации не оставлять локальный хвост документации; сам ZIP не публикуется.
CP-HF-152 read-set: текущий фронт, git status/diff/log/refs/remotes, файлы и проверки HF-149…151, project.godot, tests/ProjectTest.cs, ChikagoShop.sln, tests/ChikagoShop.Domain.Tests/ChikagoShop.Domain.Tests.csproj. Write-set: файлы HF-149…151 для необходимых исправлений, ORCHESTRATION_PLAN.md (HF-149…152), Git index/objects и локальная/удалённая feature/music; .local/checks/hf152 — снимок/проверки/сообщения/индексные версии. Out of scope: main (локальная и origin/main), force-push, source/, личные настройки/исходные WAV, ZIP, несвязанный HF-139. Приёмка: build/domain и адресные rendered/audio regression PASS или подтверждённая baseline-проблема вне scope; финальное дерево проверено; логичные коммиты; обычный push feature/music; remote SHA совпадает с локальной веткой, main не изменилась. Проверки: diff-check, build/domain, isolated rendered SFX/settings/buttons, menu/intro/game music, редакторский -18dB fixture, ancestry/remote refs/staged diff/итоговый status.


```text
Task: HF-152
Recommended status: REVIEW (принято координатором → DONE)
Changed: финальный пакет перепроверен; сохранён авторский mix Hover/CustomerBell -10dB, Click/Settings/Sale -12dB. Устаревший визуальный тест EN Back обновлён на реальный aspect-fit/свободные поля, без изменений production-подсветки. Созданы6c2bad3 (плашка SFX/Skip),55139f9 (компактная Back/актуальный visual regression),0c52951 (scene-owned SFX/авторский mix). Все ранее накопленные коммиты и новые функциональные коммиты отправлены обычным push a35e98c→0c52951 только в feature/music. Контрольный коммит плана сохраняет итог проверки/публикации и исторический отчёт HF-139.
Checks: diff/staged diff-check PASS; build PASS (0 warnings/errors); domain141/141 PASS (на машине нет runtime.NET8, тестовый хост запущен на установленном.NET10 через DOTNET_ROLL_FORWARD=Major); isolated import PASS; rendered sound-effects/music-settings/button-feedback PASS; menu-music/intro-music/game-music PASS; отдельный editor Click=-18dB fixture PASS; runtime/code source checksums совпали с проверенной копией. Push feature/music PASS, remote SHA0c52951 совпал с локальным после функциональной публикации.
Acceptance: пользовательские уровни и все правки интерфейса/аудио находятся в музыкальной ветке; main не получала push/merge. Локальная main осталась900e9b1. Remote main параллельно изменилась512b755→3190512 независимыми коммитами balance editor; raw-SHA fetch/ancestry подтвердил, что0c52951 не входит в неё. Финальный отчёт публикуется дополнительным обычным push той же feature/music.
Read-set exception: tests/Integration/ProjectTest.ButtonFeedback.cs и scripts/Presentation/ButtonFeedbackController.cs диагностированы по расширению выше; сырая remote-main история прочитана для проверки отсутствия музыкального merge. Оригиналы WAV/сохранения/source/ не затронуты.
Risks/blocker: прежние предупреждения duplicate UI UID на импорте и certificate-store в ограниченной среде; адресные проверки завершились PASS. Remote main менялась параллельно внешними операциями, с нашей стороны отправлялась только refs/heads/feature/music. Evidence: .local/checks/hf152/{build,domain,import,sound-effects*,music-settings*,button-feedback*,menu-music,intro-music,game-music,editor-volume*}.log, main-before.txt/remote-main-before.txt и файлы сообщений/индексных версий.
Next: дальнейшие изменения или отдельный запрос на объединение с main.
```

### HF-151 — постоянная настройка базовой громкости каждого SFX в инспекторе

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-150 DONE, активных пересечений нет. Пользователь хочет разработческую настройку конкретного эффекта в Godot, применяемую при100% игрового ползунка.
CP-HF-151 read-set: текущий фронт, scripts/Audio/SoundEffects.cs, project.godot, tests/Integration/ProjectTest.SoundEffects.cs, docs/GAMEPLAY.md; read-only audio/sfx/*.wav/.import (пять точных ассетов HF-145), готовая копия HF-150 для проверки. Write-set: ORCHESTRATION_PLAN.md, новый scenes/Audio/SoundEffects.tscn, scripts/Audio/SoundEffects.cs, project.godot (только autoload SoundEffects), tests/Integration/ProjectTest.SoundEffects.cs, docs/GAMEPLAY.md; .local/checks/hf151 — изолированная проверка/evidence. Out of scope: source/, WAV/музыка/поведение UI, пользовательские настройки, commit/push/ZIP, несвязанные правки. Приёмка: пять AudioStreamPlayer заранее заданы в сцене, Volume dB каждого сохраняется редактором и не перезаписывается кодом; стартовые -6dB/нажатия/пауза/ползунок сохранены. Проверки: build, rendered SFX/settings, отдельный fixture с изменённой базовой громкостью Click, diff-check.


```text
Task: HF-151
Recommended status: REVIEW (принято координатором → DONE)
Changed: пять AudioStreamPlayer вынесены в scenes/Audio/SoundEffects.tscn с прежними streams/-6dB/polyphony. Autoload загружает сцену, код связывает её узлы и назначает SFX bus, не создаёт плееры и не перезаписывает VolumeDb. Старый общий VolumeDb property удалён, чтобы инспектор каждого узла был единственным источником базового микса; игровой ползунок продолжает множить bus gain. Добавлены инструкции и проверки scene-owned узлов/сохранения developer gain.
Checks: build PASS (0 warnings/errors); rendered --sound-effects-checks PASS; rendered --music-settings-checks PASS; отдельный изолированный Click=-18dB fixture PASS (stdout SFX_EDITOR_VOLUME_CHECK Click=-18dB, runtime сохранил именно значение сцены, пользовательский gain0.25 не перезаписал его); git diff-check PASS.
Acceptance: разработчик открывает SoundEffects.tscn, выбирает Hover/Click/Settings/Sale/CustomerBell, меняет Volume dB и сохраняет сцену; при100% в игре звучит заданный baseline. Стартовый микс и поведение звуков/паузы/пропуска не изменены. Evidence: .local/checks/hf151/{build.log,sound-effects*.log,music-settings*.log,editor-volume-fixture*.log}.
Read-set exception: ChikagoShop.csproj и готовая изолированная копия HF-150 использованы для проверок без source/; fixture менял только изолированную сцену и затем восстановил её.
Risks/blocker: прежнее certificate-store warning окружения при exit0; WAV и личные настройки не менялись. Commit/push/ZIP не выполнялись.
Next: пользователь настраивает нужный эффект через Volume dB в обычном дереве сцены.
```

### HF-150 — уменьшить Back в главных настройках

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-149 DONE, активных пересечений нет.
CP-HF-150 read-set/write-set: ORCHESTRATION_PLAN.md, scripts/MainMenu.cs (только ConfigureBackButton), scenes/main_menu.tscn (только минимум Settings/BackButton); read-only tests/Integration/ProjectTest.MusicSettings.cs и готовая проверочная копия HF-149. .local/checks/hf150 — evidence/изолированный рендер. Out of scope: source/, аудио, остальная геометрия/кнопки, commit/push/ZIP, пользовательские правки. Приёмка: Back уменьшена с480×144до260×78 с сохранением пропорций, по центру под языком, RU/EN без наложений. Проверки: build, rendered --music-settings-checks (960×540/1280×720), просмотр screenshot, diff-check.


```text
Task: HF-150
Recommended status: REVIEW (принято координатором → DONE)
Changed: ConfigureBackButton и scene минимум Back уменьшены до260×78 вместо480×144, исходные пропорции и центрирование сохранены; другие кнопки не менялись.
Checks: build PASS (0 warnings/errors); isolated rendered --music-settings-checks PASS (RU/EN960×540/1280×720, доступность Back, отступы/плашки); menu-volume-ru-1280.png просмотрен; git diff-check PASS.
Acceptance: Back компактная, примерно вдвое меньше по ширине, по центру под языком, не выходит за экран. Evidence: .local/checks/hf150/{build.log,settings.log,settings-errors.log}, project/*-volume-*.png.
Read-set exception: ChikagoShop.csproj и готовая изолированная копия HF-149 использованы для проверки без source/.
Risks/blocker: прежнее certificate-store warning окружения при exit0. Commit/push/ZIP не выполнялись.
Next: пользовательская приёмка размера.
```

### HF-149 — отдельная громкость эффектов и звуки пропуска пролога

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-148 DONE, активных пересечений нет.
CP-HF-149 read-set: текущий фронт, scripts/{MainMenu,UI/PauseMenu,UI/MusicVolumeControl,Audio/MusicSettings,Audio/SoundEffects,Dialogue/DialoguePlayer}.cs, scenes/{main_menu,UI/MusicVolumeControl,UI/DialoguePlayer}.tscn, project.godot, localization/en.tres, tests/Integration/ProjectTest.{MusicSettings,SoundEffects,IntroMusic,MenuMusic}.cs, tests/ProjectTest.cs, docs/GAMEPLAY.md, art/ui/button_back.png (только размеры). Write-set: ORCHESTRATION_PLAN.md, scripts/{MainMenu,UI/PauseMenu,UI/MusicVolumeControl,Audio/SoundEffects}.cs, scenes/main_menu.tscn (layout настроек для второй плашки/снятие ошибочных max bounds), localization/en.tres, tests/Integration/ProjectTest.{MusicSettings,SoundEffects}.cs, docs/GAMEPLAY.md; .local/checks/hf149 — изолированная проверка/evidence. Out of scope: source/, WAV/петли/уровни музыкального микса, доменная логика, commit/push/ZIP, пользовательские изменения. Приёмка: SoundVolume отдельная копия той же плашки в menu/pause (такой же стиль/ширина/отступы), audio/effects_volume сохраняется независимо от музыки/языка,0глушит все SFX без музыки; Skip реальный long-hover/click/fast-click с прежними правилами и однократным пропуском; весь экран помещается в960×540/1280×720. Проверки: build, rendered settings/SFX, intro/menu music regressions, diff-check.


```text
Task: HF-149
Recommended status: REVIEW (принято координатором → DONE)
Changed: SoundEffects управляет пользовательской громкостью SFX через bus gain/mute и audio/effects_volume в settings.cfg, default100%, независимо от music volume/языка. MusicVolumeControl получил ControlsSoundEffects и использован повторно для SoundVolume в главном меню/паузе: те же размеры/оформление/бегунок/PlainStyle, новая RU/EN подпись. Удалены ошибочные maximum bounds Settings/BackButton, чтобы обе плашки и Back помещались на экране. Оба ручных пути Skip теперь отправляют единый Pressed signal, поэтому общий audio observer играет один click перед скрытием кнопки; hover150мс сохранён.
Checks: build PASS (0 warnings/errors); rendered --music-settings-checks PASS (два независимых bus/mute, немедленная реакция, сохранение/загрузка, обе плашки/Back, RU/EN960×540/1280×720, mouse/keyboard); rendered --sound-effects-checks PASS (long hover Skip, real click Skip, fast Skip без отложенного hover и дубля, реальный Back, прежние SFX/lifecycle); --intro-music-checks PASS; --menu-music-checks PASS; git diff-check PASS. Визуально просмотрены menu-volume-ru-1280.png и pause-volume-en-960.png.
Acceptance: отдельная плашка звуков оформлена точно тем же компонентом,0глушит SFX без музыки; значения переживают пересоздание меню и сохраняют язык. Skip звучит на наведении/клике и по-прежнему запускает один fade-out; кнопка Back теперь доступна мышью. Evidence: .local/checks/hf149/{build.log,music-settings*.log,sound-effects*.log,intro-music.log,menu-music.log}, project/*-volume-*.png.
Read-set exception: ChikagoShop.csproj и изолированная копия HF-147 использованы для проверок без source/; PNG header button_settings прочитан вместе с button_back для диагностики прежней геометрии. Рабочие WAV/музыкальный код не менялись.
Risks/blocker: прежнее сообщение root certificate store окружения при exit0; полный игровой проход не выполнялся. Commit/push/ZIP не выполнялись.
Next: пользовательская проверка настроек и пропуска пролога.
```

### HF-148 — коммит звуков интерфейса в feature/music

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-16; HF-145…147 DONE, пользователь подтвердил результат и запросил коммит в музыкальную ветку.
CP-HF-148 read-set: текущий фронт, git status/diff/refs, evidence HF-145…147. Write-set: ORCHESTRATION_PLAN.md (HF-145…148), Git index/objects и локальная feature/music; .local/checks/hf148 — сообщение/индексная версия плана. Out of scope: main, push, source/, исходные WAV Downloads, ZIP, несвязанный HF-139. Приёмка: один тематический коммит с пятью SFX, отсроченным hover150мс/отменой быстрого клика, special actions/arrival bell, тестами/документацией; main неизменна; HF-139 остаётся вне коммита. Проверки: staged diff-check/stat, сохранённые build/rendered SFX checks HF-147 PASS, итоговые refs/status.


```text
Task: HF-148
Recommended status: REVIEW (принято координатором → DONE)
Changed: в feature/music создан один тематический коммит «Добавить звуки интерфейса и задержку наведения перед кликом»: пять WAV/imports, SoundEffects, presentation event прихода покупателя, документация/интеграционные проверки. Итоговый отчёт включён amend только этого нового неопубликованного коммита.
Checks: staged diff-check PASS; состав19files проверен; сохранённые build/rendered SFX HF-147 PASS; main=900e9b1 без изменения; вне коммита оставлен только локальный отчёт ZIP HF-139.
Acceptance: пользовательский результат сохранён в музыкальной ветке одним коммитом, main/remote не затронуты.
Read-set exception: нет.
Risks/blocker: HF-139 намеренно остаётся незакоммиченным; push/merge не выполнялись.
Next: дальнейшие правки или публикация по запросу пользователя.
```

### HF-147 — вернуть hover/click с отменяемой задержкой наведения

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; HF-146 DONE, активных пересечений нет. Пользователь хочет исходные звуки при условии отсутствия пары hover→click при быстром нажатии.
CP-HF-147 read-set/write-set: ORCHESTRATION_PLAN.md, scripts/Audio/SoundEffects.cs, tests/Integration/ProjectTest.SoundEffects.cs, tests/ProjectTest.cs, docs/GAMEPLAY.md; read-only audio/sfx/{hover,click}.wav, evidence HF-146. .local/checks/hf147 — изолированная копия/evidence. Out of scope: исходники/обработка WAV, source/, музыка, sale/settings/bell semantics, Back geometry, commit/push/ZIP. Приёмка: hover.wav после150мс реального наведения, быстрый click отменяет ожидание и играет только click.wav; начавшийся hover останавливается на mouse-down/activation; уход/скрытие/disabled/удаление отменяют ожидание; no focus/layout hover; pause и спецзвуки сохранены. Проверки: build, rendered SFX fast-click/dwell/leave/hide/pause/lifecycle checks, diff-check.


```text
Task: HF-147
Recommended status: REVIEW (принято координатором → DONE)
Changed: исходные hover.wav/click.wav снова назначены своим событиям. HoverDelaySeconds экспортирован, default.15; ожидание ведётся в Always Process с повторной проверкой фактической кнопки под указателем. Реальное движение создаёт одно ожидание; mouse-down/keyboard activation, уход/скрытие/disabled/removal отменяют ожидание и останавливают hover voice. Навигация/смена меню без движения ничего не планируют. Поздних Task/Timer callbacks нет. Специальные эффекты settings/sale/bell сохранены. Документация и интеграционные проверки обновлены.
Checks: build PASS (0 warnings/errors); isolated rendered --sound-effects-checks PASS: fast enter+press толькоClick; ожидание после fast-click не даёт позднийHover; dwell даёт одинHover; движение внутри не повторяет; mouse-down останавливает играющийHover до release; hide/quick leave/focus не создают звук; основной menu/pause/sale/bell/reload regression PASS; git diff-check PASS.
Acceptance: при быстром клике до150мс hover не начинается, звучит только отдельный click; при осознанном наведении остаётся прежний hover; исходные WAV не менялись. Evidence: .local/checks/hf147/{build.log,sfx.log,sfx-errors.log}.
Read-set exception: ChikagoShop.csproj и изолированная копия HF-146 использованы для проверок без source/.
Risks/blocker: если пользователь уже задержался дольше150мс, начало hover уже прозвучало и его нельзя отменить задним числом; нажатие немедленно прекращает оставшуюся часть. Известная Back geometry вне задачи, прежний certificate-store warning окружения при exit0. Commit/push/ZIP не выполнялись.
Next: пользовательское прослушивание; задержка доступна в SoundEffects.HoverDelaySeconds при необходимости настройки.
```

### HF-146 — тихое наведение, прежний hover-файл на обычное нажатие

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; HF-145 DONE, активных пересечений нет.
CP-HF-146 read-set/write-set: ORCHESTRATION_PLAN.md, scripts/Audio/SoundEffects.cs, tests/Integration/ProjectTest.SoundEffects.cs, tests/ProjectTest.cs, docs/GAMEPLAY.md; read-only audio/sfx/hover.wav и evidence HF-145. .local/checks/hf146 — изолированная копия/evidence. Out of scope: исходные WAV/музыка, source/, продажа/settings/bell-семантика, MainMenu Back geometry, commit/push/ZIP, пользовательские правки. Приёмка: любое наведение/фокус без звука, обычное нажатие играет именно hover.wav; settings/sale/bell остаются отдельными, disabled молчат, пауза работает. Проверки: build, rendered --sound-effects-checks, diff-check.


```text
Task: HF-146
Recommended status: REVIEW (принято координатором → DONE)
Changed: из SoundEffects удалены Hover cue/voice, MouseMotion/Input/Process наблюдение; generic Click теперь загружает hover.wav. Settings/Sale/CustomerBell и правила доступности/паузы не менялись. Старый click.wav сохранён как неиспользуемый ассет. Тест проверяет тишину при реальном наведении и фокусе, точный путь hover.wav у Click, фактическое проигрывание по нажатию и отсутствие двойных событий; документация и PASS-маркер актуализированы.
Checks: build PASS (0 warnings/errors); isolated rendered --sound-effects-checks PASS (silent hover, correct WAV on click, main/pause, sale/settings/bell exceptions, disabled/reload/cleanup); git diff-check PASS.
Acceptance: наведение полностью без звука, обычное нажатие играет именно бывшую запись наведения; отдельные звуки специальных действий сохранены. Evidence: .local/checks/hf146/{build.log,sfx.log,sfx-errors.log}.
Read-set exception: ChikagoShop.csproj/готовая изолированная копия HF-145 использованы для проверок без source/.
Risks/blocker: прежняя геометрия BackButton вне задачи; root certificate store warning окружения при exit0. Commit/push/ZIP не выполнялись.
Next: проверить на слух новое звучание нажатий.
```

### HF-145 — звуковые эффекты интерфейса, продажи и прихода покупателя

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; зависимостей/пересечений нет. Уточнение пользователя: колокольчик только при самостоятельном приходе, без смены кнопкой отказа.
CP-HF-145 read-set: текущий фронт, пять точных файлов Downloads/{продажа,наведение на иконку,колокольчик,шестерёнка,клик по иконке}.wav, scripts/{TradeScene,CounterWorld,ModularCustomer,MainMenu}.cs, scripts/{Audio/MusicSettings,UI/PauseMenu,UI/SlotUI,UI/InventoryUI,UI/ProgressPanel,Days/DayCycleController,Presentation/ButtonFeedbackController}.cs, scripts/Persistence/TradeScene.Persistence.cs, scenes/trade_scene.tscn, project.godot, tests/ProjectTest.cs, tests/Integration/ProjectTest.{Helpers,Trading,DayStart,ButtonFeedback}.cs, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, новые audio/sfx/{sale,hover,customer_bell,settings,click}.wav/.import, новый scripts/Audio/SoundEffects.cs/.uid, project.godot, scripts/TradeScene.cs (только presentation event и его вызов в StartBuyerOffer), tests/ProjectTest.cs, новый tests/Integration/ProjectTest.SoundEffects.cs/.uid, docs/GAMEPLAY.md; .local/checks/hf145 — evidence/изолированный запуск. Out of scope: source/, доменная логика TradeScene, исходники Downloads/обработка WAV, музыка/петли, новый ползунок, commit/push/ZIP, несвязанные правки. Приёмка: hover только при движении мыши на активный элемент, не от клика/фокуса/смены меню; обычный клик по всем активным кнопкам/иконкам меню/игры/паузы, SettingsButton — шестерёнка вместо обычного клика, AcceptButton — продажа вместо обычного клика; bell только StartBuyerOffer, не reroll/load. Эффекты работают при паузе независимо от Music bus. Проверки: hashes, build, isolated sfx real-input/event/lifecycle tests + music/button regression, diff-check.


```text
Task: HF-145
Recommended status: REVIEW (принято координатором → DONE)
Changed: пять WAV побайтно скопированы в audio/sfx, one-shot импорт без нормализации/сжатия. SoundEffects autoload наблюдает все существующие/динамические BaseButton (включая TextureButton, инвентарь, часы и паузу), различает hover/обычный click/settings/sale, не потребляет input и очищает подписки. Hover только после реального MouseMotion на новый доступный элемент; активация гасит прошлый hover и не запускает его снова. Общая громкость эффектов -6dB, отдельная SFX bus, проигрывание Always/MaxPolyphony. В TradeScene добавлены только presentation event BuyerArrived и один вызов после ShowTrade в StartBuyerOffer; смена/загрузка не вызывают bell. Документировано поведение и добавлен --sound-effects-checks.
Checks: пять source/asset SHA256 совпали PASS; build PASS (0 warnings/errors); import PASS; headless SFX checks PASS; final rendered --sound-effects-checks PASS (мышь/focus/disabled, меню/пауза, продажа без generic click, самостоятельные приходы/отказ/reload, Music mute не глушит SFX, cleanup); --intro-music-checks PASS; --game-music-checks PASS; git diff-check PASS. --button-feedback-checks FAIL на Texture feedback bounds кнопки Back; без SoundEffects autoload тот же FAIL, baseline подтверждён.
Acceptance: все пять событий звучат в нужных местах, hover не вызывается кликом/фокусом/сменой меню; bell отсутствует при reroll и restore. Исходные WAV и музыкальная система сохранены. Evidence: .local/checks/hf145/{hashes.txt,build.log,import.log,sound-effects.log,sound-effects-rendered*.log,button-feedback-rendered*.log,button-baseline*.log,intro-music.log,game-music.log}.
Read-set exception: tests/Integration/ProjectTest.Crafting.cs прочитан только для существующих CraftAtelier/FinishEmbroidery fixtures; ChikagoShop.csproj и runtime-ресурсы скопированы для изолированных проверок без source/. Временное отключение SoundEffects выполнено только в изолированном project.godot и восстановлено.
Risks/blocker: обнаружена прежняя проблема MainMenu Settings BackButton: global rect(440,910),(400,0), нулевая высота/за экраном; пользователю сообщено во время работы. Она не исправлялась в звуковой задаче, generic click главного меню проверен на языковой кнопке; Back в паузе проверен реальным кликом. Общий visual button suite не объявляется зелёным. Прежнее сообщение root certificate store среды не влияет на SFX/music exit0. Commit/push/ZIP не выполнялись.
Next: пользовательское прослушивание; отдельно исправить геометрию Back в главных настройках при запросе.
```

### HF-144 — коммит обновлённых главных мелодий в feature/music

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; HF-140…143 DONE; пользователь просит пока только коммит в музыкальной ветке.
CP-HF-144 read-set: текущий фронт, git status/diff/branches, evidence HF-140…143. Write-set: Git index/objects и локальная feature/music, ORCHESTRATION_PLAN.md (записи HF-140…144); .local/checks/hf144 — индексная версия плана/сообщение коммита. Out of scope: main, push, source/, ZIP, несвязанный локальный отчёт HF-139. Приёмка: один коммит «Изменены главные мелодии» с заменой двух WAV, единым уровнем и финальными петлями90/70BPM, тестами/документацией; main остаётся900e9b1; HF-139 остаётся незакоммиченным. Проверки: staged diff-check/stat, текущие music regression PASS HF-143, commit refs/status.


```text
Task: HF-144
Recommended status: REVIEW (принято координатором → DONE)
Changed: в feature/music создан один коммит «Изменены главные мелодии» с подробным описанием замены WAV, подготовки петель90/70BPM, единой громкости, тестов и документации. Итоговый отчёт включён amend только этого нового неопубликованного коммита; функциональные файлы не менялись.
Checks: staged diff-check PASS; staged stat12files подтверждён; сохранённые build/menu-music/intro-music/game-music HF-143 PASS; main осталась900e9b1. Вне индекса оставлена только прежняя запись HF-139 о сборке ZIP.
Acceptance: запрошенные музыкальные изменения сохранены отдельным коммитом в музыкальной ветке; main/remote не изменены.
Read-set exception: нет.
Risks/blocker: несвязанный локальный отчёт ZIP HF-139 намеренно остаётся незакоммиченным; origin/feature/music ещё указывает на прежнюю опубликованную историю. Push/merge не выполнялись.
Next: по дальнейшему запросу опубликовать ветку или объединить с main.
```

### HF-143 — убрать микропаузу задумчивой темы с учётом70BPM

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; HF-142 DONE. Пользователь уточнил ориентир70BPM и слышимую паузу на полном исходном fade-out/in.
CP-HF-143 read-set: текущий фронт, C:/Users/artem/Downloads/задумчиво2.wav (read-only), audio/music/{thoughtful_loop,menu_theme_loop}.wav/.import, .local/checks/hf142/audio-loop и reports, tests/Integration/ProjectTest.{IntroMusic,GameMusic}.cs, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, audio/music/thoughtful_loop.wav/.import, tests/Integration/ProjectTest.{IntroMusic,GameMusic}.cs, docs/GAMEPLAY.md, .local/checks/hf143 — обработка/evidence/изолированная проверка. Out of scope: главная тема/её импорт, source/, оригиналы Downloads, громкость/межтрековые переходы, commit/push/ZIP. Приёмка: период70BPM сохраняет финальную фразу после53⅓с, убрана экспортная добавка и падение громкости на стыке; нет импульса/паузы в native decoder; главная тема побайтно прежняя. Проверки: музыкальный период/sample seam/энергия в окнах у стыка, hashes main, build, isolated intro/game/menu checks, diff-check.


```text
Task: HF-143
Recommended status: REVIEW (принято координатором → DONE)
Changed: thoughtful period2419200frames/54.857142857с=64доли при70BPM; удалены6414frames/145.442мс экспортного хвоста. Fade-out/in стыка заменён raised-cosine bridge по400мс из непрерывного, совпадающего по музыкальной фазе фрагмента (48долей; корреляция head .927/tail .991; точный reference1814437). От0.4до54.457143с waveform побайтно эквивалентен исходному PCM (maxDifference0), в том числе прежняя обрезанная последняя фраза. Импорт и тесты длительности обновлены. Проверка seam требует непрерывной энергии и проверяет каждый20мс блок реального decoder-wrap, вместо исключения для исходного fade.
Checks: build PASS (0 warnings/errors); isolated import PASS; --intro-music-checks PASS (включая энергию всех20мс блоков вокруг повторения, скачок обоих каналов<.001, последняя фраза53⅓→54.334694с без повторения); --game-music-checks PASS; --menu-music-checks PASS; main WAV/import hashes unchanged PASS; source thoughtful hash unchanged PASS; unchanged-body maxDifference0 PASS; git diff-check PASS.
Acceptance: нет fade-to-silence на стыке; L/R seam step .00000155/.00000119, средний RMS .04530 вместо .04520 (нормализации нет); поздняя фраза сохранена, главная тема не менялась. Evidence: .local/checks/hf143/{bridge-candidates.jsonl,thoughtful-preparation.json,unchanged-body.json,main-before.txt,build.log,import.log,intro-music.log,game-music.log,menu-music.log}, воспроизводимый локальный audio-loop/Program.cs.
Read-set exception: ChikagoShop.csproj и готовая изолированная копия HF-142 использованы для сборки/проверок без source/. Неприменимый для WAV GetLoopCount в новой проверке заменён фактическим переходом playback position от конца к началу.
Risks/blocker:70BPM принят по уточнению пользователя и подтверждён хорошо совпадающими внутренними фазовыми фрагментами; воспроизведение проверено инструментально, музыкальная оценка прослушиванием остаётся за пользователем. Прежнее сообщение root certificate store не влияет на PASS/exit0. Commit/push/ZIP не выполнялись.
Next: прослушать повтор задумчивой темы в запущенной заново игре.
```

### HF-142 — восстановить полное окончание восьмитактовой задумчивой темы

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; HF-141 DONE. Пользователь подтвердил главную тему и уточнил восьмитактовую длину задумчивой; прежняя оценка90BPM/20тактов для неё ошибочна.
CP-HF-142 read-set: текущий фронт, C:/Users/artem/Downloads/задумчиво2.wav (read-only), audio/music/{thoughtful_loop,menu_theme_loop}.wav/.import, .local/checks/hf141/audio-loop и analysis/preparation reports, tests/Integration/ProjectTest.{IntroMusic,GameMusic}.cs, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, audio/music/thoughtful_loop.wav/.import, tests/Integration/ProjectTest.{IntroMusic,GameMusic}.cs, docs/GAMEPLAY.md, .local/checks/hf142 — анализ/обработка/изолированная проверка. Out of scope: главная тема и её импорт/код/тест, source/, оригиналы Downloads, громкость/межтрековые переходы, commit/push/ZIP. Приёмка: окончание оригинальной восьмитактовой фразы не обрезано; стык остаётся непрерывным; главный трек побайтно прежний. Проверки: длительность/сохранность окончательной части, seam/decoder checks, main hashes, build, isolated intro/game checks, diff-check. BPM уточнён у пользователя асинхронно, независимый анализ оригинала продолжается.


```text
Task: HF-142
Recommended status: REVIEW (принято координатором → DONE)
Changed: задумчивая восстановлена полностью из оригинала55.002585с/2425614frames, включая последние1.669252с вместо ошибочной границы53⅓с. Полный исходный WAV имеет собственные fade-in/out и стык L/R≈.00000262/.00000250 (без импульса), поэтому дополнительная подмена его краёв не нужна; сохранена естественная концовка. Import loop_end2425614; тесты проверяют SHA256 полного оригинала, воспроизведение последней фразы после53⅓с без loop и переход через фактический конец. Проверка непрерывной громкости остаётся у подготовленной главной темы; к естественному fade задумчивой применяется проверка отсутствия импульса, а не требование постоянного RMS. Документация исправлена.
Checks: build PASS (0 warnings/errors); isolated import PASS; --intro-music-checks PASS: length55.002585, decoding from53.333333 to54.334694, loops0, слышимые ненулевые samples финальной фразы; actual end decoder-wrap PASS; --game-music-checks PASS; --menu-music-checks PASS; SHA256 главной и её.import до/после совпадают; thoughtful source=runtime D3BC6A7557C0243B01B752AE970E435B767CB34E5735612FBAA8FF9B2C875716; git diff-check PASS.
Acceptance: преждевременного обрыва исходной восьмитактовой фразы нет; повтор происходит по полному оригиналу, образцы волны на стыке близки к нулю; главная тема и её импорт неизменны. Evidence: .local/checks/hf142/{original-edges.jsonl,main-before.txt,build.log,import.log,intro-music.log,game-music.log,menu-music.log}.
Read-set exception: ChikagoShop.csproj и готовая изолированная копия HF-141 использованы для проверок; source/ не читался. Предыдущие пользовательские изменения сохранены.
Risks/blocker: точный BPM в WAV не указан (REAPER bext без tempo); вопрос о BPM задан, ответа к завершению не получено. Новая музыкальная длина не выдумывалась: сохранён полный пользовательский оригинал с естественным окончанием, без привязки к90BPM. Прежнее сообщение root certificate store окружения не влияет на PASS/exit0. Субъективное прослушивание пользователем остаётся музыкальной оценкой; commit/push/ZIP не выполнялись.
Next: прослушать полный повтор задумчивой темы; если пользователь сообщит BPM, сверить его сетку без нового обрезания нот.
```

### HF-141 — ровные петли двух обновлённых тем без щелчка

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; HF-140 DONE. Новый запрос явно разрешает подгонку аудио, заменяя прежнее ограничение побайтной неизменности runtime-копий; оригиналы Downloads сохраняются.
CP-HF-141 read-set: текущий фронт, audio/music/{menu_theme_loop,thoughtful_loop}.wav/.import, C:/Users/artem/Downloads/{кйф2,задумчиво2}.wav (read-only), tests/Integration/ProjectTest.{MenuMusic,IntroMusic,GameMusic}.cs, docs/GAMEPLAY.md, scripts/Audio/{IntroMusicDirector,GameplayMusicDirector}.cs (read-only). Write-set: ORCHESTRATION_PLAN.md, audio/music/{menu_theme_loop,thoughtful_loop}.wav/.import, tests/Integration/ProjectTest.{MenuMusic,IntroMusic,GameMusic}.cs, docs/GAMEPLAY.md, .local/checks/hf141 — анализ/обработка/изолированная проверка. Out of scope: source/, исходные файлы Downloads, уровни громкости/межтрековые переходы, зловещая тема, пользовательские изменения, commit/push/ZIP. Приёмка: выбраны музыкально согласованные границы, стык сглажен без паузы и выброса; исходники сохранены; Godot loop_end соответствует подготовленным файлам; проверки реального decoder-wrap, стыкового скачка и громкости. Проверки: signal analysis/seam metrics, build, isolated menu/intro/game checks, diff-check.


```text
Task: HF-141
Recommended status: REVIEW (принято координатором → DONE)
Changed: игровые WAV подогнаны к целым музыкальным периодам при90BPM: main1881600frames/16тактов/42.666667с, thoughtful2352000frames/20тактов/53.333333с. Удалены хвосты138/73614frames; исходные24bit stereo44100Hz сохранены. Период main подтверждён корреляцией0.994 на21⅓с. Raised-cosine bridge использует по80мс вокруг одного непрерывного участка оригинала (main reference940735, thoughtful2352028); нет fade-to-zero/нормализации. Import loop_end обновлён, тесты проверяют стерео seam jump<.001, RMS у стыка в0.5…1.5от body и реальный decoder-wrap. Документация обновлена; аудиодиректоры/громкость/переходы не менялись.
Checks: build PASS (0 warnings/errors); isolated import PASS; --menu-music-checks PASS; --intro-music-checks PASS; --game-music-checks PASS; git diff-check PASS. Main L/R seam step .00006068/.00000310 вместо .039705/.039520; thoughtful .00012445/.00009084, fade-хвост исключён. Общий RMS изменился на -0.00233%/+0.45489% (без нормализации). Оригиналы Downloads сохранили SHA256 HF-140.
Acceptance: обе подготовленные петли имеют непрерывную звуковую волну, не проваливаются в тишину на стыке, декодер Godot повторяет их нативно, прежние музыкальные переходы проходят regression. Evidence: .local/checks/hf141/{analysis.jsonl,main-preparation.json,thoughtful-preparation.json,original-hashes.txt,build.log,import.log,menu-music.log,intro-music.log,game-music.log}; воспроизводимый локальный обработчик audio-loop/Program.cs.
Read-set exception: ChikagoShop.csproj и runtime-ресурсы скопированы для изолированных проверок без source/. Временный анализатор собран на установленном.NET10 без сетевых зависимостей; первоначальная net8 restore-попытка не прошла ограничение сети и заменена офлайн-сборкой. Все пользовательские изменения сохранены.
Risks/blocker: инструментального/декодерного контроля достаточно для критерия отсутствия импульсного стыка; субъективное прослушивание пользователем остаётся финальной музыкальной оценкой. Прежнее сообщение root certificate store окружения не мешает проверкам exit0. Commit/push и пересборка ZIP не выполнялись.
Next: прослушать обновлённые петли в запущенной заново игре.
```

### HF-140 — замена главной и задумчивой музыки оригинальными WAV

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-15; активных пересечений нет. Пользователь запрещает обработку аудиофайлов и просит одинаковую громкость во всех местах.
CP-HF-140 read-set: текущий фронт; C:/Users/artem/Downloads/{кйф2,задумчиво2}.wav (только заголовки/копирование/SHA256), audio/music/{menu_theme_loop,thoughtful_loop}.wav/.import, scripts/Audio/{IntroMusicDirector,GameplayMusicDirector}.cs, scenes/{main_menu,trade_scene}.tscn, tests/Integration/ProjectTest.{MenuMusic,IntroMusic,GameMusic,MusicSettings}.cs, tests/ProjectTest.cs, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md; audio/music/{menu_theme_loop,thoughtful_loop}.wav/.import; scripts/Audio/{IntroMusicDirector,GameplayMusicDirector}.cs; scenes/main_menu.tscn (только уровень меню); tests/Integration/ProjectTest.{MenuMusic,IntroMusic,GameMusic}.cs; docs/GAMEPLAY.md; .local/checks/hf140 — evidence/изолированная копия. Out of scope: source/, редактирование/обрезка/нормализация исходных WAV, новые композиции, музыка зловещая (файл), несвязанные пользовательские изменения сцены/плана, commit/push, пересборка ZIP. Приёмка: кйф2 используется везде вместо главной, задумчиво2 в прологе/картах; SHA256 копий равен исходникам; обе записи зациклены целиком через метаданные Godot; единый gain -12dB без прежних компенсаций; переходы/ползунок сохранены. Проверки: WAV metadata/SHA256, build, isolated menu/intro/game/music-settings checks, diff-check.


```text
Task: HF-140
Recommended status: REVIEW (принято координатором → DONE)
Changed: кйф2.wav и задумчиво2.wav скопированы побайтно под существующие runtime-пути главной/задумчивой тем; все текущие сцены автоматически используют новые записи. Import loop_end обновлены на полную длину1881738/2425614 frames (42.669796/55.002585с), trim/normalize/resample/compress остаются выключены. Общий gain меню/пролога/дня/карт -12dB, compensation1; ползунок и переходы сохранены. Тесты обновлены с прежних подготовленных коротких петель/RMS-нормализации на новые SHA256, полную длительность, одинаковый gain и lifecycle. Документация актуализирована.
Checks: hashes source=destination PASS: main3989653FD7C4CD1E10D9B3EA70F1614F78D5B67DD06F9DAA0D8E4169CD687E97, thoughtfulD3BC6A7557C0243B01B752AE970E435B767CB34E5735612FBAA8FF9B2C875716; build PASS (0 warnings/errors); isolated import PASS; --menu-music-checks PASS; --intro-music-checks PASS; --game-music-checks PASS; --music-settings-checks PASS; git diff-check PASS.
Acceptance: новые исходные WAV не обрабатывались и совпадают с Downloads по байтам; меню/пролог/торговля/карты используют замену, записи повторяются полностью, gain одинаковый. Evidence: .local/checks/hf140/{audio-hashes.txt,build,import,menu-music,intro-music,game-music,music-settings}.log (hashes — txt).
Read-set exception: ChikagoShop.csproj и runtime-ресурсы скопированы для изолированного запуска, без source/. Прежние пользовательские удаления лишних экспортированных полей и ext_resource в main_menu.tscn сохранены; добавлен только явный MenuVolumeDb -12. Локальный отчёт HF-139 сохранён.
Risks/blocker: исходные WAV 24-bit PCM; Godot декодирует импортированный sample в свой16-bit формат, исходные файлы остаются24-bit и неизменными. Одинаковое усиление сохраняет естественные различия исходного микса, нормализации нет. Повтор полной записи не гарантирует прежние границы по90BPM; подрезка/обработка запрещены пользователем. В среде тестов прежнее сообщение root certificate store. Commit/push и пересборка ZIP не выполнялись.
Next: прослушать обновлённые композиции в игре.
```

### HF-139 — ZIP актуальной Windows-сборки в Downloads

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-138 DONE; пользователь запросил отдельный архив готовой игры для отправки.
CP-HF-139 read-set: текущий фронт, git status/HEAD/remote main, project.godot, export_presets.cfg, ChikagoShop.csproj, docs/TESTING.md, .gitignore, локальные .NET/Godot executables и export templates; tracked runtime-файлы снимка HEAD900e9b1 для экспорта (без source/). Write-set: ORCHESTRATION_PLAN.md, .local/checks/hf139 (изолированный снимок/проверки), builds/ChikagoShop-900e9b1-Windows (готовая сборка), C:/Users/artem/Downloads/ChikagoShop-900e9b1-Windows.zip. Out of scope: source/, личные сохранения/настройки, изменение игрового поведения, commit/push, перезапись существующих пользовательских архивов. Приёмка: последняя main проверена, release Windows x86_64 .NET экспорт, в ZIP все необходимые runtime-файлы без личных данных; запуск отдельно от проекта подтверждён, архив помещён в Downloads. Проверки: release export, изолированный smoke-run с логом, ZIP inventory/hash/size, git diff-check.


```text
Task: HF-139
Recommended status: REVIEW (принято координатором → DONE)
Changed: из git archive HEAD900e9b1 создан изолированный проект; из официального mono_export_templates.tpz4.7.2 скачаны HTTP Range только Windows x64 release EXE/console и извлечены Deflate-потоки с проверкой размеров. Временный export preset использует .NET-шаблон, включает config/dialogue JSON, исключает dev-ресурсы и debug symbols. Релиз содержит EXE/PCK и self-contained .NET runtime; добавлена короткая инструкция. ZIP109473514байт,190 entries скопирован в C:/Users/artem/Downloads/ChikagoShop-900e9b1-Windows.zip.
Checks: remote main=HEAD900e9b1 PASS; release publish MSBuild завершён, экспорт PCK содержит economy.v1.json/intro.ru.json; self-contained coreclr/hostfxr/assemblies присутствуют; standalone rendered menu smoke PASS; распаковка готового ZIP и повторный запуск EXE отдельно от проекта PASS; ZIP required files/no personal saves PASS; SHA256 копии совпал:8AC91C3ACC165D34F32360A40BA7C94DC7F2C16C431A2420283ECE8E673B1783; git diff-check PASS.
Acceptance: готовый архив для Windows x86_64 находится в Downloads, Godot/отдельный .NET получателю не нужны; EXE запускается из полностью распакованной папки, ресурсы/runtime находятся рядом. Evidence: .local/checks/hf139/{import,export,publish,smoke-menu,zip-smoke}.log,zip-inventory.txt; сборка builds/ChikagoShop-900e9b1-Windows.
Read-set exception: scripts/Configuration/EconomyConfigProvider.cs и scripts/MainMenu.cs прочитаны для диагностики упаковки JSON/запуска; source/ не читался. Сетевые NuGet/template-загрузки и запись Downloads выполнены с разрешённым повышением прав.
Risks/blocker: полный игровой проход экспортированной сборки не выполнялся; release бинарник не поддерживает --scene, поэтому отдельный CLI-smoke торговой сцены неприменим. Первый export без доступа NuGet не собрал .NET, повторный с доступом собрал runtime; оболочка повторного экспорта задержала завершение после готовой упаковки и была отменена, затем итоговый ZIP успешно запущен. В изолированной среде остаётся сообщение root certificate store; игрового кода ошибок при запуске нет. Исходники/сохранения пользователя и tracked export preset не менялись; commit/push не выполнялись.
Next: отправить ZIP получателю и полностью распаковать перед запуском.
```

### HF-138 — коммиты музыки и объединение с main

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-135…137 DONE. Пользователь запросил коммиты в ветке музыки, затем перенос в main.
CP-HF-138 read-set: текущий фронт, git status/diff/log/branches, файлы/evidence HF-135…137. Write-set: ORCHESTRATION_PLAN.md, Git index/objects и локальные refs feature/music/main; .local/checks/hf138 — временные индексные версии. Out of scope: source/, игровые сохранения, несвязанные изменения, remote push, переписывание опубликованных коммитов. Приёмка: существующая feature/music продвинута fast-forward к актуальной main, три тематических коммита (пролог/громкость/оформление), main получает их fast-forward; итоговое дерево соответствует проверенному результату, status чистый. Проверки: ancestry, staged diff-check каждого коммита, сохранённые build/music checks, итоговые refs/status/tree.


```text
Task: HF-138
Recommended status: REVIEW (принято координатором → DONE)
Changed: существующая feature/music продвинута от a35e98c к текущей main4bcbb27 без потери истории; созданы1fbc31f (пролог),162bcfe (громкость),366cb02 (оформление), у каждого подробное описание. main получила все три fast-forward. Отдельный коммит плана фиксирует результат в обеих ветках.
Checks: ancestry feature/music→main PASS; staged diff-check каждого коммита PASS; итоговые рабочие файлы совпали с проверенной реализацией; main=feature/music=366cb02 после первого merge, status чистый. Сохранённые build/intro-music/game-music/music-settings/rendered checks HF-135…137 PASS; игровой код после проверок не менялся.
Acceptance: все функциональные изменения закоммичены в музыкальной ветке и перенесены в main; опубликованная история не переписана. GitHub не обновлялся.
Read-set exception: нет; временные индексные версии и файлы сообщений находятся только в игнорируемой .local/checks/hf138.
Risks/blocker: remote refs остаются на прежних коммитах до отдельного push. Ограничение записи .git пройдено штатным повышением разрешений для явно запрошенных операций.
Next: при запросе пользователя опубликовать feature/music и main обычным push.
```

### HF-137 — ширина громкости в меню и простое оформление в паузе

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-136 DONE, активных пересечений нет.
CP-HF-137 read-set/write-set: ORCHESTRATION_PLAN.md, scripts/{MainMenu.cs,UI/PauseMenu.cs,UI/MusicVolumeControl.cs}, tests/Integration/ProjectTest.MusicSettings.cs; read-only scenes/UI/MusicVolumeControl.tscn, themes/game_theme.tres. .local/checks/hf137 — изолированная копия/evidence. Out of scope: source/, аудиологика/сохранения/переходы, прежние пользовательские изменения, commit/push. Приёмка: в главном меню громкость той же ширины и выравнивания, что FullscreenButton; в паузе простой серый фон как у полного экрана, без бумажной рамки/фигурного бегунка; RU/EN и отступы сохраняются. Проверки: build, rendered --music-settings-checks (960×540/1280×720, ширина/стили/ввод/сохранение), diff-check.


```text
Task: HF-137
Recommended status: REVIEW (принято координатором → DONE)
Changed: MainMenu задаёт громкости ту же ширину500, что FullscreenButton; PauseMenu включает PlainStyle общего MusicVolumeControl: ровный фон как FullscreenButton, стандартный круглый бегунок, обычный размер шрифта и компактные отступы. Добавлены regression assertions ширины/выравнивания и plain-стиля.
Checks: dotnet build ChikagoShop.sln --no-restore → PASS (0 warnings/errors); isolated rendered --music-settings-checks → PASS (exit0, RU/EN,960×540/1280×720, мышь/клавиатура/сохранение); git diff --check → PASS.
Acceptance: ширина и положение обоих блоков совпадают с полным экраном; бумажная рамка и фигурный бегунок остались только в главном меню. Визуально просмотрены menu-volume-ru-1280.png и pause-volume-ru-960.png. Evidence: .local/checks/hf137/settings*.log и project/*-volume-*.png.
Read-set exception: готовая изолированная копия HF-136 и новая сборка использованы для рендера; source/ и игровые сохранения не читались.
Risks/blocker: прежнее сообщение Failed to read the root certificate store тестового окружения; проверки PASS. Commit/push не выполнялись.
Next: пользовательская приёмка оформления.
```

### HF-136 — ползунок громкости музыки в настройках

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-135 DONE, других активных пересечений нет.

Расширение координатором: write-set включает tests/Integration/ProjectTest.GameMusic.cs — прежняя фикстура пропускает новый ручной Start, поэтому не открывает карты; привести её к текущему циклу Start → Close → Advance. Дополнительный read-set для диагностики: scripts/Days/DayCycleController.cs, scripts/UI/DayTransitionCard.cs, scripts/Presentation/DayCardSceneTransition.cs, tests/Integration/ProjectTest.{CardDealing,DayStart}.cs.
CP-HF-136 read-set: текущий фронт, scripts/{MainMenu.cs,UI/PauseMenu.cs,Audio/IntroMusicDirector.cs,Audio/GameplayMusicDirector.cs,Localization/GameLocalization.cs,Presentation/MonochromePresentation.cs}, scenes/main_menu.tscn, themes/game_theme.tres, localization/en.tres, project.godot, tests/ProjectTest.cs, tests/Integration/ProjectTest.{Helpers,Localization,IntroMusic,GameMusic,MenuMusic}.cs, docs/TESTING.md. Write-set: ORCHESTRATION_PLAN.md, project.godot, scripts/{MainMenu.cs,UI/PauseMenu.cs,Audio/IntroMusicDirector.cs,Audio/GameplayMusicDirector.cs}, новые scripts/Audio/MusicSettings.cs и scripts/UI/MusicVolumeControl.cs (.uid), новые scenes/UI/MusicVolumeControl.tscn и art/ui/music_slider_grip.svg (.import), localization/en.tres, tests/ProjectTest.cs, новые tests/Integration/ProjectTest.MusicSettings.cs (.uid), docs/GAMEPLAY.md; .local/checks/hf136 — evidence/изолированная копия. Out of scope: source/, commit/push, базовые уровни треков и монтаж петель, игровые сохранения, несвязанные пользовательские изменения. Приёмка: аккуратный общий ползунок музыки 0–100% в главном меню и паузе, шрифт/бумага/монохром проекта, RU/EN, немедленная реакция без нарушения переходов, 0 выключает звук, настройка сохраняется между запусками и не стирает язык. Проверки: build, isolated music-settings/intro-music/game-music checks, rendered RU/EN 960×540 и1280×720, diff-check.


```text
Task: HF-136
Recommended status: REVIEW (принято координатором → DONE)
Changed: MusicSettings autoload — отдельная шина Music, мгновенная громкость/полный mute и сохранение audio/volume без потери языка; все голоса IntroMusicDirector/GameplayMusicDirector направлены в Music. Общая сцена MusicVolumeControl с игровой бумажной текстурой, шрифтом, тонкой дорожкой и SVG-бегунком подключена в главное меню и паузу; RU/EN и процент. Добавлены integration music-settings checks; GameMusic fixture обновлена для ручного старта дня. Документирована настройка в GAMEPLAY.md. Изменения HF-135 сохранены.
Checks: dotnet build ChikagoShop.sln --no-restore → PASS (0 warnings/errors); rendered --music-settings-checks → PASS (мышь/клавиатура, menu/pause, RU/EN 960×540 и1280×720); headless --music-settings-checks → PASS; --intro-music-checks → PASS; --game-music-checks → PASS; git diff --check → PASS.
Acceptance: настройка применяется сразу, 0 полностью отключает звук без перезапуска плееров, язык/громкость сохраняются независимо, повторно созданные меню читают значение; все меню/пролог/день/карты используют общий Music bus, плавные переходы сохранены. Визуально просмотрены menu-volume-ru-1280.png и pause-volume-en-960.png; всего сняты8 вариантов. Evidence: .local/checks/hf136/{build,import,settings,music-settings,intro-music,game-music}*.log и project/*-volume-*.png.
Read-set exception: ChikagoShop.csproj и runtime-ресурсы скопированы в изолированную копию для проверок, без source/; точечная диагностика устаревшей GameMusic fixture согласована координатором выше. Новые UID/import перенесены из изолированного импорта.
Risks/blocker: Godot пишет Failed to read the root certificate store в ограниченном окружении; проверки завершаются exit0. Первую зависшую headless-съёмку отменили, условие съёмки исправлено на DisplayServer.GetName(); окончательные headless/rendered проверки прошли. Commit/push не выполнялись.
Next: пользовательская проверка громкости в игре.
```

### HF-135 — восстановление музыки пролога после настройки громкости

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; зависимостей и пересечений нет.
CP-HF-135 read-set: текущий фронт, scenes/main_menu.tscn, scripts/Audio/IntroMusicDirector.cs, tests/Integration/ProjectTest.IntroMusic.cs, tests/ProjectTest.cs, docs/TESTING.md, tools/verify.ps1, .vscode/settings.json, .gitignore. Write-set: ORCHESTRATION_PLAN.md, scenes/main_menu.tscn, tests/Integration/ProjectTest.IntroMusic.cs; .local/checks/hf135 — изолированная копия/evidence. Out of scope: source/, другие пользовательские изменения, музыка игрового дня, commit/push. Приёмка: восстановлены ссылки двух треков и корректные параметры вместо null; пользовательские уровни всех тем -12 dB сохранены; все три темы звучат на одинаковом нормализованном уровне, переходы/skip работают. Проверки: build, isolated --intro-music-checks, git diff --check.

```text
Task: HF-135
Recommended status: REVIEW (принято координатором → DONE)
Changed: scenes/main_menu.tscn — восстановлены две AudioStream-ссылки, компенсация и длительности переходов, меню -6 dB; пользовательские три уровня -12 dB и editor UID сохранены. tests/Integration/ProjectTest.IntroMusic.cs — regression null-параметров и одинаковой нормализованной громкости вместо прежнего crescendo.
Checks: dotnet build ChikagoShop.sln --no-restore → PASS (0 warnings/errors); isolated Godot --headless --script res://tests/ProjectTest.cs -- --intro-music-checks → PASS; git diff --check → PASS.
Acceptance: музыкальные петли, переключение по репликам, равные RMS уровни (допуск 1%), плавные/быстрые переходы, завершение, skip и отмена подтверждены интеграционным тестом. Evidence: .local/checks/hf135/intro-music.log.
Read-set exception: ChikagoShop.csproj и перечни локальных исполняемых файлов прочитаны для сборки/запуска; runtime-ресурсы и themes скопированы в изолированную копию без чтения source/. Старое локальное evidence HF-104/HF-133 отсутствует на этом компьютере, создана новая копия.
Risks/blocker: Godot сообщает Failed to read the root certificate store в ограниченном окружении; музыкальные проверки завершились PASS, exit 0. Фактическое прослушивание пользователем остаётся оценкой восприятия громкости.
Next: прослушать пролог в редакторе.
```


### HF-134 — публикация нового комплекта предметов

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-133 DONE; пользователь явно запросил push во время реализации.
CP-HF-134 read-set: текущий фронт, git status/diff/log/remotes, evidence HF-133. Write-set: ORCHESTRATION_PLAN.md, Git index/commit и origin/main для точного write-set HF-133. Out of scope: source/, .local/, сохранения, новые изменения поведения, force-push. Приёмка: обычный push в origin/main, HEAD=origin/main, status чистый. Проверки: fetch до публикации0/0; сохранённые build/domain/rendered checks HF-133; staged diff-check/stat; post-push revision/status.
```text
Task: HF-134
Recommended status: REVIEW (принято координатором → DONE)
Changed: HF-133 опубликована в origin/main коммитом cb8b5d0:16 новых артов, каталог/редкости и нормализованные вероятности,16 готовых торговых вариантов сцены,8 вариантов полировки, удаление старого арта и обновлённые тесты/документы.
Checks: fetch0/0; build PASS, domain141/141 PASS, rendered asset-replacement и resource-checks PASS (evidence HF-133); staged diff-check PASS; обычный push PASS; после push HEAD=origin/main, status пустой.
Acceptance: новый комплект доступен в удалённой main; локальные тестовые данные/сохранения не опубликованы.
Read-set exception: нет.
Risks/blocker: прежние предупреждения UID и legacy aliases описаны в HF-133.
Next: основной фронт S1-04.
```
### HF-133 — новый комплект предметов и удаление старого арта

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-132 DONE, пользователь разрешил замену/удаление старого арта и самостоятельное назначение редкостей.
CP-HF-133 read-set: текущий фронт; точные 16 PNG в new/: Белая кепка с чёрными мазками(1), Бубен, Виолончель, Грамофон, маракасы, Метроном, обувь, перчатки, пиджак, платье, рубашка, Саксафон, скрипка, Труба, шляпка женская, шубка (просмотрены); scripts/Crafting/WorkshopItemCatalog.cs, scripts/{CounterWorld,TradeScene}.cs (последний read-only), scripts/UI/{PolishingSurface,EmbroideryGame,RarityPanel}.cs, scenes/trade_scene.tscn, config/economy.v1.json, src/ChikagoShop.Domain/Crafting/WeightedItemTable.cs, tests/Integration/ProjectTest.{Resources,Economy,Days,Crafting,Trading}.cs, tests/ProjectTest.cs, localization/en.tres, docs/{GAMEPLAY,ASSET_AUDIT}.md; поиск ссылок на старые файлы. Write-set: ORCHESTRATION_PLAN.md; перенос перечисленных новых PNG в art/items/{cap,tambourine,cello,gramophone,maracas,metronome,shoes,gloves,jacket,dress,shirt,saxophone,violin,trumpet,cloche_hat,fur_stole}.png и их .import; удаление прежних art/items/{coat,guitar,hat,trumpet,placeholder_clothing_exclusive,placeholder_clothing_first,placeholder_clothing_unique,placeholder_music_exclusive,placeholder_music_first,placeholder_music_unique}.svg/.import, {drums,guitar,hat_cloche}.png/.import, {gloves,hat_cloche}.jpg/.import; удаление art/minigames/violin.png/.import и polishing_{guitar,sax,trumpet,violin}{,_mask}.svg/.import; scripts/Crafting/WorkshopItemCatalog.cs, scripts/UI/{PolishingSurface,EmbroideryGame}.cs, scenes/trade_scene.tscn, новый src/ChikagoShop.Domain/Crafting/RarityItemTable.cs и tests/ChikagoShop.Domain.Tests/RarityItemTableTests.cs, tests/Integration/ProjectTest.{Resources,Economy,Days,Crafting,Trading}.cs, localization/en.tres, docs/{GAMEPLAY,ASSET_AUDIT}.md; .local/checks/hf133. Out of scope: source/, новые генерации/редактирование PNG, save schema/сброс сохранений, торговая доменная логика TradeScene.cs, стоимость крафта/квоты/общие шансы редкостей, commit/push. Приёмка: все16 новых артов доступны по8 в мастерской, редкость отражает сложность изготовления; первые10 индексов сохраняют workshop/rarity/kind для старых сейвов, новые6 дописаны. Вероятность категории не зависит от количества её предметов; внутри категории равные шансы. Все16 торговых вариантов размещены в сцене, мини-игра полировки использует8 новых музыкальных артов и их alpha; progressive repeat picker адаптирован под8 вариантов. Старые арты/заглушки/маски удалены после проверки ссылок, new/ убрана переносом без потери байтов. Проверки: build/domain, import, rendered asset-replacement/economy/resource checks, все16 inventory/showcase/trade/reload,8 polishing silhouettes/реальный mouse stroke, preview/hash/diff-check.
```text
Task: HF-133
Recommended status: REVIEW (принято координатором → DONE)
Changed:16 PNG перенесены из new/ в art/items/ с проверкой SHA256, прежние предметные арты/JPEG-дубликаты/цветные заглушки и SVG/маски полировки удалены. Новая папка пуста и удалена после переноса. Каталог содержит8 предметов каждой мастерской; первые10 kind/индексов/workshop/rarity сохранены,6 позиций дописаны. Редкости и основания описаны в GAMEPLAY.md: кепка/маракасы/бубен простые; сложный крой/акустическая настройка/мех/механический привод повышают редкость. Новый чистый RarityItemTable распределяет целые билеты через НОК количества предметов: суммарный шанс категории прежний, её предметы равновероятны, включая 1% unique на двоих. Улучшения используют тот же механизм. Все16 торговых Sprite2D размещены в сцене. Полировка получает8 новых музыкальных PNG и использует их alpha; repeat picker имеет8 вариантов против4 у вышивки. Добавлены EN имена, актуализированы проверки/документы. TradeScene.cs и save schema не менялись.
Checks: local/isolated build PASS (0 warnings/errors), domain141/141 PASS. Изолированная проверка в свежей копии без старых файлов: import PASS (прежние duplicate UI UID warnings); asset-replacement PASS —16 before-Ready sprites/fit/PNG alpha/EN,8 рабочих полировок/реальные mouse strokes/оплата, все16 inventory→showcase→trade→reroll→reload→sale; baseline и все6 уровней улучшений обеих мастерских сохраняют отображаемые проценты и равные доли внутри категории. Resource-checks PASS. Local import повторно PASS (первый завершил импорт с ошибкой GodotTools HotReloadAssemblyWatcher timer при выходе; повторный запуск только с прежними UID warnings). Polishing preview просмотрен. SHA25616/16 совпадают; поиск runtime/test ссылок на удалённые арты пустой; diff-check PASS. Evidence: .local/checks/hf133/ (свежий project/ и отдельный user://).
Acceptance: весь новый набор используется, старый удалён; общий баланс редкостей сохранён при расширении каталога. Сейвы сохраняют индексы и ценовые уровни, прежние товары получают новый рисунок/название своего уровня. Локальный пользовательский прогресс не затронут.
Read-set exception: src/ChikagoShop.Domain/Days/DayUpgradeSession.cs — диагностика API для regression всех6 уровней; .local/checks/hf104/hf104-check-dir.txt и override.cfg — шаблон изоляции. source/ не читался.
Risks/blocker: внутренние legacy kind (например guitar) сохранены ради совместимости и больше не совпадают с новым отображаемым названием. Качество/выбранный рисунок мини-игры по-прежнему не определяет выданный товар; это прежнее правило. Общий baseline tooltip FAIL вне задачи.
Next: HF-134 — публикация по последнему запросу пользователя.
```
### HF-132 — публикация мини-игр, портретов и кнопок

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-128–HF-131 DONE; пользователь явно запросил push.
CP-HF-132 read-set: текущий фронт, git status/diff/log/remotes, evidence .local/checks/hf{128,129,131}; write-set: ORCHESTRATION_PLAN.md, Git index/commit и origin/main для готовых файлов HF-128–HF-131 (art/dialogue/, scenes/UI/{DialoguePlayer,CraftButton}.tscn, scenes/trade_scene.tscn, scripts/Dialogue/DialoguePlayer.cs, scripts/MainMenu.cs, scripts/Presentation/CraftButton{Presentation,Visual}.cs/.uid, scripts/UI/EmbroideryGame.cs, shaders/dialogue_portrait.gdshader/.uid, src/ChikagoShop.Domain/Crafting/MiniGameVariantPicker.cs, tests/ChikagoShop.Domain.Tests/MiniGameVariantPickerTests.cs, tests/Integration/ProjectTest.{DialogueLocalization,Localization,Resources,DialoguePortraits}.cs/.uid, tests/ProjectTest.cs, docs/{ASSET_AUDIT,GAMEPLAY}.md). Out of scope: source/, .local/, игровые сохранения, новые изменения поведения, force-push. Приёмка: обычный push в origin/main, HEAD=origin/main, рабочее дерево чистое. Проверки: fetch/divergence, сохранённые результаты build/domain/rendered checks, staged diff-check/stat, post-push rev/status.
```text
Task: HF-132
Recommended status: REVIEW (принято координатором → DONE)
Changed: готовые HF-128–HF-131 опубликованы коммитом 8a29144 в origin/main: взвешенные варианты мини-игр, сцена диалога с тремя портретами и исправленными полутонами, общая сцена кнопки с готовой текстурной рамкой и единым оттенком значка. Локальные архивы/проверки не включены.
Checks: fetch до публикации HEAD...origin/main=0/0; сохранённые local/isolated build PASS, domain 139/139 PASS, dialogue-portraits и craft-localization PASS; staged diff-check PASS; обычный push PASS, после публикации HEAD=origin/main, status пуст.
Acceptance: изменения доступны в удалённой main, новых поведенческих правок в задаче нет.
Read-set exception: нет.
Risks/blocker: прежние предупреждения UID и baseline полного suite описаны в feature-handoff; локальные оригиналы находятся в .local/originals/dialogue/, игровые копии опубликованы.
Next: основной фронт S1-04.
```
### HF-131 — текстурная рамка крафта и общий оттенок значка

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-130 DONE, активных пересечений нет.
CP-HF-131 read-set: текущий фронт, scenes/trade_scene.tscn, scripts/Presentation/{CraftButtonPresentation,ButtonFeedbackController}.cs, art/ui/{camera_frame,create_button,panel_small}.png/.import, tests/Integration/ProjectTest.{Resources,Ui,Trading}.cs, tests/ProjectTest.cs, docs/ASSET_AUDIT.md, shaders/paper_edge.gdshader/.uid. Write-set: ORCHESTRATION_PLAN.md, scenes/trade_scene.tscn, новая scenes/UI/CraftButton.tscn, новый scripts/Presentation/CraftButtonVisual.cs/.uid, scripts/Presentation/CraftButtonPresentation.cs, tests/Integration/ProjectTest.Resources.cs, docs/ASSET_AUDIT.md, удаление shaders/paper_edge.gdshader/.uid; .local/checks/hf131. Out of scope: source/, PNG-редактирование/генерация, сохранения/экономика, остальные кнопки, commit/push. Приёмка: неровность двух кнопок задаёт готовая прозрачная camera_frame.png из проекта; процедурный edge shader удалён. Разметка кнопки/подписей/значка задана в общей сцене; RU/EN, hover/pressed/disabled окрашивают бумагу и значок одинаково, нет белого прямоугольника. Сохранены размеры, hotspot и реальный крафт/cancel. Проверки: build/import, rendered craft-localization с проверкой сцены и GPU hover/disabled, preview/diff-check.
```text
Task: HF-131
Recommended status: REVIEW (принято координатором → DONE)
Changed: две мастерские инстанцируют общую CraftButton.tscn. NinePatchRect берёт неровность из прозрачного camera_frame.png; бумага, готовая русская надпись (AtlasTexture), английская надпись и готовый значок размещены в сцене. PNG не редактировались. CraftButtonPresentation только переключает подписи; CraftButtonVisual применяет один оттенок к бумаге/рамке/надписям/значку по draw mode. Disabled остаётся непрозрачным, чтобы перекрытие двух слоёв бумаги не давало светлую плашку. Старый процедурный paper_edge.gdshader/.uid удалён. Аудит актуализирован.
Checks: local/isolated build PASS (0 warnings/errors); local/isolated import PASS (прежние duplicate UI UID warnings); rendered craft-localization PASS: scene-before-Ready, исходная прозрачная текстура без material, RU/EN/RU в обеих мастерских, GPU прозрачный неровный край/непрозрачная бумага/совпадение цвета бумаги и значка, disabled/reset/pressed, реальный hover и click/cancel без списаний. Превью EN normal/hover и RU просмотрены; diff-check PASS. Evidence: .local/checks/hf131.
Acceptance: внешний контур задаёт готовая текстура, шейдерная неровность отсутствует. Белый квадрат около ? не выделяется при затемнении, значок получает тот же оттенок. Размеры кнопок и hotspot сохранены; текст и декоративные узлы не перехватывают клики.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt — изолированная проверка; read-only проверка alpha локальных готовых кнопок/rarity_panel/settings_panel для поиска подходящего прозрачного оригинала. source/ не читался. Подходящая рамка найдена локально, Drive не требовался.
Risks/blocker: прежние duplicate UI UID warnings вне задачи. Исходная camera_frame.png используется без изменения байтов.
Next: визуальная приёмка пользователем. Commit/push не выполнялись.
```
### HF-130 — мягкий рисованный край кнопки и порядок в корне

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-129 DONE, активных пересечений нет.
CP-HF-130 read-set: текущий фронт, scenes/trade_scene.tscn, scripts/Presentation/CraftButtonPresentation.cs, art/ui/{create_button,panel_small}.png/.import, tests/Integration/ProjectTest.Resources.cs, tests/ProjectTest.cs, .gitignore, docs/ASSET_AUDIT.md, список корня и точные корневые ггшка.png, мафия.png, покупательница.png/.import (hash/reference check). Write-set: ORCHESTRATION_PLAN.md, scenes/trade_scene.tscn, новый shaders/paper_edge.gdshader/.uid, tests/Integration/ProjectTest.Resources.cs, docs/ASSET_AUDIT.md; перенос трёх корневых оригиналов и их .import в .local/originals/dialogue/ с проверкой SHA256, .local/checks/hf130. Out of scope: source/, сохранения, игровой баланс, остальные пользовательские изменения, commit/push, редактирование PNG. Приёмка: край кнопок создания предметов мягкий, слегка неровный в RU/EN, середина/иконка/текст и клики сохранены; оригиналы не потеряны, корень без импортируемых дубликатов портретов. Проверки: build, import, rendered craft-localization с GPU alpha fixture, preview, hash verification, diff-check.
```text
Task: HF-130
Recommended status: REVIEW (принято координатором → DONE)
Changed: у двух кнопок создания в сцене задан общий paper_edge ShaderMaterial. Узкая полупрозрачная кромка с небольшой статичной неровностью вычисляется в экранных пикселях, сохраняет середину рисунка и не затрагивает области клика. Три оригинала портретов и их import-sidecars перенесены из корня в .local/originals/dialogue/; SHA256 до/после совпадают. Runtime art/dialogue/ сохранён, аудит уточняет местоположение оригиналов. Посторонние изменения не затронуты.
Checks: local/isolated build PASS (0 warnings/errors), isolated import PASS (прежние duplicate UI UID warnings), rendered craft-localization PASS (RU/EN/RU, две мастерские, материал, реальный click/cancel; GPU — полупрозрачный неровный контур, непрозрачная неизменная середина, прежние bounds), preview просмотрен; hash verification 6/6 PASS; diff-check PASS. Evidence: .local/checks/hf130.
Acceptance: кромка мягче и слегка неровная при прежнем содержимом кнопки; исходники сохранены без изменений, импортируемые дубликаты удалены из корня переносом в локальный архив.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt — изолированная проверка. source/ не читался.
Risks/blocker: исходники в .local/ не входят в Git; игровые PNG находятся в art/dialogue/. Прежние UID warnings вне задачи.
Next: визуальная приёмка пользователем. Commit/push не выполнялись.
```
### HF-129 — сохранить шляпу и полутона лиц при осветлении

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-128 DONE, активных пересечений нет.
CP-HF-129 read-set: текущий фронт, shaders/dialogue_portrait.gdshader, art/dialogue/{frankie,castello}.png (гистограммы/read-only), scenes/UI/DialoguePlayer.tscn, tests/Integration/ProjectTest.DialoguePortraits.cs, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, shaders/dialogue_portrait.gdshader, tests/Integration/ProjectTest.DialoguePortraits.cs, docs/GAMEPLAY.md. Evidence: .local/checks/hf129 и изолированная копия. Out of scope: исходные/runtime PNG, source/, геометрия/текст диалога, выбор мини-игр HF-128, commit/push. Приёмка: осветляется тёмный штрих, средние тона шляпы сохраняют контраст с фоном, серые пятна под глазами не становятся белыми; говорящий без изменений, alpha сохранён. Проверки: build, rendered dialogue-portraits с реальными цветами шляпы/лица в GPU fixture, preview, diff-check.

```text
Task: HF-129
Recommended status: REVIEW (принято координатором → DONE)
Changed: shader осветляет только тёмный штрих, ограничивая целевой серый уровнем .4; активный портрет сохраняет исходный RGB. Средние и светлые тона больше не смешиваются с белым. GPU fixture дополнен цветами шляпы Френки (96/255) и участка под глазами Кастелло (144/255), docs актуализированы. PNG не изменялись.
Checks: local/isolated build PASS (0 warnings/errors); rendered dialogue-portraits PASS, включая исходный тон активного портрета, осветление чёрного, сохранность полутонов шляпы/лица и alpha; preview просмотрен; diff-check PASS. Evidence: .local/checks/hf129/{build,local-build,dialogue-portraits}*.log, preview.png.
Acceptance: шляпа неговорящего Френки сохраняет контраст; участки под глазами Кастелло не высветляются до белого. Плавное осветление штрихов неговорящих персонажей сохранено.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt для изолированной проверки. source/ не читался.
Risks/blocker: нет новых; прежние ограничения полного suite вне задачи.
Next: визуальная приёмка пользователем. Commit/push не выполнялись.
```
### HF-128 — меньше повторов мини-игр и персонажи пролога

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-127 DONE, активных пересечений нет.
CP-HF-128 read-set: текущий фронт, scripts/UI/{EmbroideryGame,PolishingSurface}.cs, src/ChikagoShop.Domain/Crafting/EmbroiderySession.cs, tests/ChikagoShop.Domain.Tests/{EmbroiderySessionTests,ChikagoShop.Domain.Tests.csproj}, scripts/Dialogue/DialoguePlayer.cs, src/ChikagoShop.Domain/Dialogue/DialogueSession.cs, scripts/MainMenu.cs (создание диалога), dialogue/intro.ru.json, tests/Integration/ProjectTest.{DialogueLocalization,Localization,EmbroideryTarget}.cs, tests/ProjectTest.cs; точные локальные файлы ггшка.png, мафия.png, покупательница.png (просмотрены), docs/{GAMEPLAY,ASSET_AUDIT}.md. Write-set: ORCHESTRATION_PLAN.md, новый src/ChikagoShop.Domain/Crafting/MiniGameVariantPicker.cs и tests/ChikagoShop.Domain.Tests/MiniGameVariantPickerTests.cs, scripts/UI/EmbroideryGame.cs, scripts/Dialogue/DialoguePlayer.cs, scripts/MainMenu.cs, новые scenes/UI/DialoguePlayer.tscn, shaders/dialogue_portrait.gdshader/.uid, art/dialogue/{frankie,castello,customer}.png/.import (неизменённые копии указанных PNG), tests/Integration/ProjectTest.{DialogueLocalization,Localization}.cs (инстанцирование сцены), новые tests/Integration/ProjectTest.DialoguePortraits.cs/.uid, tests/ProjectTest.cs (группа), docs/{GAMEPLAY,ASSET_AUDIT}.md. Evidence: .local/checks/hf128 и изолированная копия. Out of scope: source/, редкости/экономика/save schema, оригиналы PNG, тексты/порядок реплик, commit/push. Приёмка: 4 варианта каждой мини-игры выбираются взвешенно; вес повторившегося варианта падает 1→1/2→1/4…до1/64, разные истории вышивки/полировки; повторы возможны, новый вариант сбрасывает серию. Пролог использует сцену с тремя портретами; говорящий из Speaker имеет исходный цвет, остальные светлеют смешением с белым при сохранении alpha; рассказчик осветляет всех, RU/EN не меняет идентичность говорящего; переход плавный, две формы окна, сохранены пагинация/музыка/skip. Проверки: build/domain unit tests; import; rendered dialogue-portraits/dialogue-localization/embroidery-target/tutorial checks; preview/diff-check.

```text
Task: HF-128
Recommended status: REVIEW (принято координатором → DONE)
Changed: MiniGameVariantPicker — независимый взвешенный выбор косметического варианта по внешнему random roll; история в каждом EmbroideryGame, вес последнего варианта .5^длина_серии до1/64. Next вызывается только после прежних проверок допустимости Open; стоимость/редкость не менялись. Новые domain unit tests покрывают равномерное первое выпадение, прогрессивное уменьшение, ненулевой нижний предел, сброс/независимость и invalid rolls. Вся разметка DialoguePlayer перенесена из создания в C# в scenes/UI/DialoguePlayer.tscn, MainMenu и fixtures инстанцируют сцену. Frankie/Castello/Customer размещены готовыми TextureRect, PNG — побайтные копии локальных оригиналов. Шейдер смешивает RGB с белым на 32%, сохраняя alpha/tint; говорящий из source Speaker плавно возвращается к исходному цвету за0,2с, рассказчик осветляет всех. Shade фона задан в сцене .45 для светлой рисованной композиции по референсу. DialoguePath экспортирован в инспектор. Пагинация/музыка/текст прежние; runtime только связывает узлы, адаптирует текст и меняет tone. Docs/аудит актуализированы.
Checks: local/isolated build PASS (0 warnings/errors); domain PASS 139/139; local/isolated import PASS (exit0, прежние duplicate UI UID warnings). Rendered dialogue-portraits PASS (до-ready узлы, покупательница/Кастелло/рассказчик/Френки, RU/EN и1280×720/960×540, GPU: active black0 / inactive black≈.32 / alpha holes preserved); dialogue-localization PASS (все24 реплики/имена/пагинация/живой язык/порядок music cues/реальные настройки/skip); embroidery-target и tutorial PASS. Preview покупательницы/Френки просмотрены, SHA256 всех трёх runtime PNG совпадает с оригиналами, diff-check PASS.
Acceptance: повтор одного варианта подряд становится менее вероятным с каждым повтором (для4 вариантов около14,3%→7,7%→4%…), но возможен; смена варианта сбрасывает серию. Две мастерские не влияют на историю друг друга. Все три персонажа находятся в редактируемой сцене; текущий Speaker исходного JSON определяет обычный тон, остальные светлее независимо от локализации.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt — изолированная копия. source/ не читался. Корневые пользовательские PNG сохранены незакоммиченными; локальный импорт создал их производные .import, они не входят в результат задачи.
Risks/blocker: история выбора живёт в текущей игровой сцене и сбрасывается при её загрузке; повторы учитываются при показе мини-игры, включая отменённый preview. Это намеренный косметический эффект без save-schema. Прежние UID warnings и baseline tooltip FAIL полного integration suite вне задачи.
Next: пользовательская приёмка; основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf128/{build,local-build,domain,import,local-import,dialogue-portraits,dialogue-localization,embroidery-target,tutorial}*.log, asset-hashes.log, {customer,frankie}-preview.png.
### HF-127 — публикация обучения первой продаже

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-126 DONE, пользователь разрешил commit/push.
CP-HF-127 read-set: текущий фронт, Git status/diff/refs/remotes, evidence .local/checks/hf126. Write-set: ORCHESTRATION_PLAN.md; Git index/commits/main и origin/main для docs/GAMEPLAY.md, localization/en.tres, scenes/trade_scene.tscn, scenes/UI/FirstTradeTutorial.tscn, scripts/UI/FirstTradeTutorial.cs/.uid, tests/ProjectTest.cs, tests/Integration/ProjectTest.Tutorial.cs/.uid. Out of scope: .local/, source/, builds/, новые игровые правки, force push. Приёмка: обычный push main, remote SHA совпадает с HEAD, дерево чистое. Проверки: fetch/divergence, diff-check, сохранённые build/import и rendered tutorial/day-start/card-dealing checks HF-126, ls-remote.

Handoff HF-127: REVIEW → DONE после приёмки координатором. Changed: обучение HF-126 опубликовано коммитом 847b0ff. Checks: fetch/divergence PASS (0/0), diff-check PASS; сохранённые local/isolated build PASS (0 warnings/errors), import PASS с прежними UID warnings; rendered tutorial/day-start/card-dealing checks PASS из HF-126; push origin main PASS (ad9067b..847b0ff); ls-remote подтвердил SHA 847b0ff1eebc02e923b80017c8f0ceb01331611d; дерево перед фиксацией отчёта чистое. Acceptance: обычная публикация main выполнена. Read-set exception: нет. Risks/blocker: прежний tooltip baseline полного suite вне задачи; новых нет. Next: основной фронт S1-04; отчёт фиксируется отдельным commit/push.
### HF-126 — обучение первому торговому циклу

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-122…HF-124 DONE, активных пересечений нет.
CP-HF-126 read-set: текущий фронт, scripts/TradeScene.cs (публичное состояние/ready/торговля), scripts/UI/{InventoryUI,EmbroideryGame,PauseMenu}.cs, scripts/Days/DayCycleController.cs (Started), scripts/Camera/WorldCamera.cs, scripts/Presentation/DayCardSceneTransition.cs, scripts/Localization/GameLocalization.cs, scenes/trade_scene.tscn, scenes/UI/SlotUI.tscn, tests/ProjectTest.cs, tests/Integration/ProjectTest.Helpers.cs, docs/GAMEPLAY.md, localization/en.tres. Write-set: ORCHESTRATION_PLAN.md, новые scenes/UI/FirstTradeTutorial.tscn, scripts/UI/FirstTradeTutorial.cs/.uid, tests/Integration/ProjectTest.Tutorial.cs/.uid; scenes/trade_scene.tscn (инстанс обучения), tests/ProjectTest.cs (группа), localization/en.tres, docs/GAMEPLAY.md. Evidence: .local/checks/hf126 и изолированная копия. Out of scope: TradeScene/домен/экономика/таймер, существующая save schema, source/, bitmap, commit/push. Приёмка: первое прохождение последовательно объясняет Start, производство/энергию, выбор предмета, витрину/ожидание покупателя, оффер/рероллы, продажу и квоты; подсветка следует цели при pan/zoom/resize; пропуск в любой момент, повтор из паузы; completion/skip сохраняется отдельно от игрового save, незавершённый шаг восстанавливается по текущему состоянию. Подсказки не блокируют игру/покупки, скрыты в модальных окнах/паузе; разметка в .tscn. Проверки: build/import, rendered --tutorial-checks (реальные клики/lifecycle/reload/skip/RU/EN/resize), day-start/card-dealing, diff-check.

```text
Task: HF-126
Recommended status: REVIEW (принято координатором → DONE)
Changed: новая FirstTradeTutorial.tscn содержит панель, подсветку, текст навигации, Skip/Done и готовую Replay-кнопку; инстанс включён в trade_scene.tscn. FirstTradeTutorial наблюдает публичное состояние без изменений TradeScene/домена: Start → Craft → Select → Place → WaitBuyer → Offer → Summary. Пояснения энергии/рероллов/квоты, ✓/× вместо зависимости от цвета. Подсветка преобразует фактическую геометрию цели в экранные координаты, обрезается viewport и пропускает input. Панель адаптируется/уходит от цели. При pause/craft/cards/endstate подсказки скрыты. Готовая кнопка Replay переносится в существующее меню паузы без создания UI в коде и запускает обучение без сброса игры. Отдельный <SavePath>.tutorial.cfg хранит завершение/пропуск и базовый счётчик продаж; незавершённый шаг выводится из game state, продажа перед reload не теряется. Пустой SavePath не автозапускает обучение в transient preview, ручной Restart доступен. Добавлены 14 EN-строк, GPU regression и docs.
Checks: local/isolated build PASS (0 warnings/errors), import PASS (exit 0, прежние duplicate UI UID warnings); rendered --tutorial-checks PASS (реальные Start/craft/select/place/sale/Done, reload после craft и sale, pause/skip/replay/reset, сохранность денег/предметов, screen-space highlight при pan/zoom, RU/EN live switch и 960×540/1280×720); --day-start-checks и --card-dealing-checks PASS (exit 0). Preview просмотрен, diff-check PASS.
Acceptance: первое прохождение получает обучение одному полному торговому циклу с одним следующим действием за раз, без автоматической выдачи/покупок/паузы таймера. Обучение можно пропустить и повторить из паузы. Разметка доступна в редакторе, completion хранится отдельно от игровой save schema.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt для изолированной копии. Пользовательские сохранения/настройки не затронуты.
Risks/blocker: новых нет; прежние import UID warnings и baseline tooltip FAIL полного integration suite вне задачи. Обучение намеренно не даёт дополнительного времени и не меняет баланс.
Next: пользовательская приёмка первого запуска; основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf126/{build,local-build,import,tutorial,day-start,card-dealing}*.log и preview.png.
### HF-125 — публикация сцен, анимаций и уведомлений

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-122…HF-124 DONE, пользователь разрешил commit/push.
CP-HF-125 read-set: текущий фронт, Git status/diff/refs/remotes, evidence .local/checks/hf122…hf124. Write-set: ORCHESTRATION_PLAN.md; Git index/commits/main и origin/main для текущего набора HF-122…HF-124: docs/GAMEPLAY.md, localization/en.tres, scenes/trade_scene.tscn, scenes/UI/{InventoryUI,DayTransitionCard,DayUpgradeCard}.tscn, scripts/Camera/WorldCamera.cs, scripts/Days/{DayCycleController,DayNightBackground}.cs, scripts/Presentation/{DayCardDealAnimator,RoomDoorwayPresentation,DayCardSceneTransition,InventoryExpansionAnimator}.cs и новые .uid, удаление RoomFramePresentation.cs/.uid, scripts/TradeScene.cs, scripts/UI/{DayAnnouncementOverlay,DayTransitionCard,InventoryUI,PokerCardBack}.cs, tests/ProjectTest.cs, tests/Integration/ProjectTest.{CardDealing,DayAnnouncements,Resources,RoomFrames,InventoryExpansion}.cs и новый .uid. Out of scope: .local/, source/, builds/, новые игровые правки, force push. Приёмка: обычный push main, HEAD совпадает с remote SHA, дерево чистое. Проверки: fetch/divergence, diff-check, ранее прошедшие build/import и профильные rendered checks, ls-remote после публикации.

Handoff HF-125: REVIEW → DONE после приёмки координатором. Changed: HF-122…HF-124 опубликованы коммитом 8f4caa9 — фон/оформление в сцене, анимации инвентаря и показа колоды, короткое уведомление с отсчётом до промежуточной квоты. Checks: fetch/divergence PASS (0/0), diff-check PASS; сохранённые local/isolated build PASS (0 warnings/errors), import PASS с прежними UID warnings, rendered room-frame/room-doorway/camera-limits/day-start/inventory-expansion/card-dealing/day-announcement PASS из задач; push origin main PASS (b922282..8f4caa9); ls-remote подтвердил SHA 8f4caa97520c069c71616803096a90f55503e529; дерево перед фиксацией отчёта чистое. Acceptance: обычный push main выполнен. Read-set exception: нет. Risks/blocker: прежний tooltip baseline полного suite вне задачи, новых нет. Next: основной фронт S1-04; отчёт фиксируется отдельным commit/push.
### HF-124 — остаток дней и срок промежуточной квоты в уведомлении

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-123/HF-116 DONE, активных пересечений нет.
CP-HF-124 read-set: текущий фронт, scripts/UI/DayAnnouncementOverlay.cs, scripts/Days/DayCycleController.cs, scripts/TradeScene.cs (FirstDeadline), scripts/Configuration/EconomyConfigProvider.cs, src/ChikagoShop.Domain/Configuration/EconomyConfig.cs (тип Quotas), localization/en.tres, tests/Integration/ProjectTest.DayAnnouncements.cs, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, scripts/UI/DayAnnouncementOverlay.cs, scripts/Days/DayCycleController.cs (передача срока из конфигурации), localization/en.tres, tests/Integration/ProjectTest.DayAnnouncements.cs, docs/GAMEPLAY.md. Evidence .local/checks/hf124 и изолированная копия. Out of scope: экономика/сроки/сохранения, source/, commit/push. Приёмка: нет «День N из M»; короткое «Осталось 2 дня», второй строкой срок промежуточной квоты до её дедлайна включительно; текущий день входит в остаток, после первого дедлайна строка квоты исчезает, RU/EN/plural/live switch. Проверки: build, rendered day-announcement и day-start checks, preview, diff-check.

```text
Task: HF-124
Recommended status: REVIEW (принято координатором → DONE)
Changed: DayAnnouncementOverlay показывает короткое «Осталось N дней» без номера/общего числа. До первого дедлайна включительно добавляется «До промежуточной квоты: N дней», далее строка исчезает; высота блока учитывает одну/две строки. DayCycleController передаёт срок из economy config; добавлены RU/EN plural-ключи и regression на дни 1/3/4/5/6/7. Docs актуализированы.
Checks: local/isolated build PASS (0 warnings/errors); rendered day-announcement PASS (текст, дедлайн 4→2→1→скрыт, общий остаток, RU/EN/live translation, viewport, lifecycle/pause/reload), day-start PASS; preview с «До промежуточной квоты: 2 дня» просмотрен; diff-check PASS. Финальная визуальная доводка HF-123 сохранила normal-style пустых ячеек в анимационных копиях; повторный inventory-expansion PASS, предметы/иконки сохранены.
Acceptance: уведомление сразу сообщает остаток и срок промежуточной квоты, без «День N из M»; текущий день включён в оба счётчика. Стоимости и дедлайны не менялись.
Read-set exception: нет.
Risks/blocker: новых нет; прежние import UID warnings и baseline tooltip FAIL полного suite вне задачи.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf124/{build,local-build,day-announcement,day-start}*.log и preview.png; анимации — evidence HF-123.
### HF-123 — анимация расширения инвентаря и вступление к выбору карт

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-122 DONE, активных пересечений нет.
CP-HF-123 read-set: текущий фронт, scripts/UI/{InventoryUI,SlotUI,DayTransitionCard,PokerCardBack,ZoomResponsiveUi,DayAnnouncementOverlay}.cs, scripts/Presentation/{DayCardDealAnimator,MonochromePresentation}.cs, scripts/Camera/WorldCamera.cs, scripts/Days/DayCycleController.cs, scripts/TradeScene.cs (capacity/ready/unlock), scenes/UI/{InventoryUI,SlotUI}.tscn, tests/Integration/ProjectTest.{Inventory,CardDealing,Helpers,DayStart}.cs, tests/ProjectTest.cs, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, scripts/UI/{InventoryUI,DayTransitionCard,PokerCardBack}.cs, scripts/Presentation/DayCardDealAnimator.cs, новые scripts/Presentation/{InventoryExpansionAnimator,DayCardSceneTransition}.cs/.uid, scripts/Camera/WorldCamera.cs, scripts/Days/DayCycleController.cs (инстанцирование готовой сцены), scenes/UI/InventoryUI.tscn, новые scenes/UI/{DayTransitionCard,DayUpgradeCard}.tscn, tests/Integration/ProjectTest.CardDealing.cs, новые tests/Integration/ProjectTest.InventoryExpansion.cs/.uid, tests/ProjectTest.cs (группа), docs/GAMEPLAY.md. Evidence: .local/checks/hf123 и изолированная копия. Out of scope: bitmap/source/, фон HF-122, домен/стоимости/сохранения, commit/push. Приёмка: рост 3→6 сдвигает старые ячейки влево, новые последовательно влетают из-под экрана; загрузка сразу показывает финальное состояние; reset/resize/removal отменяют анимацию. Карты: камера плавно возвращается к начальному разрешённому обзору, затем UI исчезает, колода поднимается снизу, начинается прежняя раздача; пауза/input сохраняются, закрытие/сброс/удаление восстанавливают UI и отменяют callbacks без списаний. Постоянные элементы и шаблон карты заданы в .tscn. Проверки: build/import; rendered inventory-expansion/card-dealing/camera-limits/day-start; resize/cancel/reload; preview; diff-check.

```text
Task: HF-123
Recommended status: REVIEW (принято координатором → DONE)
Changed: InventoryExpansionAnimator — временные input-ignore копии кадров/иконок, старые слоты сдвигаются влево за 0,45 с, новые прилетают снизу за 0,48 с со сдвигом 0,18 с. InventoryUI блокирует взаимодействие на переходе, отменяет при resize/reset/exit и пропускает первичную загрузку. Постоянный фон/счётчик/animation host находятся в InventoryUI.tscn. WorldCamera поддерживает отменяемый ReturnToOverview. DayCardSceneTransition — камера 0,7 с → fade UI 0,25 с → скрытие HUD/world UI; оформление комнат сохраняется; close/reset/exit восстанавливает видимость/opacity и отменяет tweens. DayCardDealAnimator поднимает колоду снизу перед раздачей. DayTransitionCard/DayUpgradeCard вынесены в сцены, PokerCardBack.IsDeck экспортирован; DayCycleController инстанцирует сцену. Обновлены regression и docs.
Checks: local/isolated build PASS (0 warnings/errors); import PASS (прежние duplicate UI UID warnings); rendered inventory-expansion PASS (сдвиг/порядок/иконка/сохранность предметов/reset/resize/reload), card-dealing PASS (RU/EN, 960×540/1280×720, плавность/порядок, скрытие/возврат UI, deck/flip/purchase/skip/cancel/reload/resize), camera-limits и day-start PASS. Preview инвентаря/карт просмотрены. Старая card fixture актуализирована под явный Start из HF-106.
Acceptance: расширение и показ карт проходят по запрошенной последовательности; анимации не изменяют экономику/сохранения/доменные правила. Постоянная разметка редактируется в сценах.
Read-set exception: нет; изолированная копия определена через .local/checks/hf104/hf104-check-dir.txt.
Risks/blocker: прежний tooltip baseline полного suite вне задачи.
Next: HF-124 — уточнение уведомления по последнему сообщению пользователя. Commit/push не выполнялись.
```

Evidence: .local/checks/hf123/{build,local-build,import,inventory-expansion,card-dealing,camera-limits,day-start}*.log и {inventory,cards}-preview.png.
### HF-122 — фон и оформление комнат заданы в сцене

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-120 DONE, активных пересечений нет.
CP-HF-122 read-set: текущий фронт, scenes/trade_scene.tscn, scripts/Presentation/{RoomFramePresentation,RoomDoorwayPresentation}.cs/.uid, scripts/Days/DayNightBackground.cs, scripts/TradeScene.cs (overlay lifecycle), tests/Integration/ProjectTest.{RoomFrames,Resources,CameraLimits}.cs, tests/ProjectTest.cs (доступные проверки), docs/GAMEPLAY.md, ChikagoShop.csproj. Write-set: ORCHESTRATION_PLAN.md, scenes/trade_scene.tscn, удаление scripts/Presentation/RoomFramePresentation.cs/.uid, scripts/Presentation/RoomDoorwayPresentation.cs, scripts/Days/DayNightBackground.cs, scripts/TradeScene.cs (только получение готовых overlay/button вместо создания), tests/Integration/ProjectTest.{RoomFrames,Resources}.cs, docs/GAMEPLAY.md. .local/checks/hf122 и изолированная копия — инструменты переноса/evidence. Out of scope: source/, bitmap-файлы, новая графика/дизайн, игровые правила, остальной UI, commit/push. Приёмка: до _Ready в PackedScene уже есть шесть фонов с конечными трансформами, шесть рамок со сплошным сердечником, семь перегородок, два фона день/ночь, два занавеса и кнопка открытия; редактор показывает ту же компоновку, runtime не создаёт/не двигает их. Код только переключает текстуры/видимость/alpha по состоянию. Внешний вид, прежние камеры/локализация/покупка сохранены. Проверки: before-tree→runtime regression, build/import, rendered room-frame/room-doorway/camera-limits/day-start, визуальное сравнение, diff-check.

```text
Task: HF-122
Recommended status: REVIEW (принято координатором → DONE)
Changed: trade_scene.tscn содержит конечные трансформы шести фонов, AtlasTexture и общий StyleBoxFlat рамок, шесть RoomFrame/SeamSeal, семь RoomPartitions, TimeOfDayNight/Day и два LockedOverlay/LockedCurtain с UnlockButton. Все узлы принадлежат PackedScene и доступны в редакторе до запуска. RoomFramePresentation.cs/.uid удалены; RoomDoorwayPresentation больше не создаёт занавесы, TradeScene только связывает ссылки на готовые overlay/button. DayNightBackground только получает готовый дневной слой и обновляет alpha. Фиксированные offsets занавесов заданы явно, не зависят от runtime-разрешения anchors. Тесты и gameplay docs актуализированы.
Checks: local/isolated build PASS (0 warnings/errors); import PASS (exit 0, прежние duplicate UI UID warnings); rendered room-frame PASS (before-tree ownership/геометрия 32 узлов, неизменность после _Ready, прежние пиксели рамок, day/night alpha/reset); room-doorway PASS (initial editor geometry, unlock/reload/checkpoint/NewGame); camera-limits и day-start PASS. Preview до/после совпадает побайтно: SHA256 E5FEFB6F15C5AD0B821F09ABE25385181BC8605ED07F4C28165AA95C2939954F. diff-check PASS.
Acceptance: компоновка фона полностью задана в сцене, код не создаёт и не подгоняет оформление при старте; визуальный результат прежний, состояние по-прежнему переключается кодом. Кнопка открытия, камера и ручной запуск дня работают.
Read-set exception: scripts/CounterWorld.cs и ChikagoShop.csproj для диагностики порядка _Ready и изолированного инструмента переноса; .local/checks/hf104/hf104-check-dir.txt — изолированная копия. Одноразовый BakeBackground/migrate-background остался в изолированной копии/.local, в runtime-проект не добавлен.
Risks/blocker: прежние duplicate UI UID warnings и baseline tooltip FAIL вне задачи; новых блокеров нет.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf122/{bake-build,bake,build,local-build,import,room-frame,room-doorway,camera-limits,day-start}*.log, baked-background.tscn, migrate-background.ps1, preview.png.
### HF-121 — публикация перегородок и удаления заголовков

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-120 DONE, пользователь разрешил commit/push.
CP-HF-121 read-set: текущий фронт, Git status/diff/refs/remotes, evidence .local/checks/hf120. Write-set: ORCHESTRATION_PLAN.md; Git index/commits/main и origin/main для scripts/Presentation/{RoomFramePresentation,RoomDoorwayPresentation}.cs, scripts/TradeScene.cs, tests/Integration/ProjectTest.{RoomFrames,Resources}.cs; текущая пользовательская правка отступа scripts/Days/DayCycleController.cs включается без изменения содержимого. Out of scope: .local/, source/, builds/, новые игровые изменения, force push. Приёмка: push main, remote SHA совпадает с HEAD, дерево чистое. Проверки: fetch/divergence, diff-check, сохранённые local/isolated build и rendered room-frame/room-doorway checks HF-120, ls-remote.

Handoff HF-121: REVIEW → DONE после приёмки координатором. Changed: HF-120 и пользовательский отступ DayCycleController опубликованы коммитом e9e75ca. Checks: fetch/divergence PASS (0/0), diff-check PASS; сохранённые local/isolated build (0 warnings/errors) и rendered room-frame/room-doorway checks HF-120 PASS; push origin main PASS (2aa7b29..e9e75ca); ls-remote подтвердил SHA e9e75cae5ddce5b133be02cc8da13bde10f6f644; дерево перед фиксацией отчёта чистое. Acceptance: обычная публикация main выполнена. Read-set exception: нет. Risks/blocker: новых нет; прежний tooltip baseline вне задачи. Next: основной фронт S1-04; отчёт фиксируется отдельным commit/push.
### HF-120 — тонкие светлые перегородки и занавесы без заголовков

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-14; HF-117 DONE, активных пересечений нет.
CP-HF-120 read-set: текущий фронт, scripts/Presentation/{RoomFramePresentation,RoomDoorwayPresentation}.cs, scripts/TradeScene.cs (заголовки/локализация), scenes/trade_scene.tscn (геометрия), tests/Integration/ProjectTest.{RoomFrames,Resources}.cs. Write-set: ORCHESTRATION_PLAN.md, scripts/Presentation/{RoomFramePresentation,RoomDoorwayPresentation}.cs, scripts/TradeScene.cs (удаление двух LockTitle и их refresh), tests/Integration/ProjectTest.{RoomFrames,Resources}.cs. .local/checks/hf120 и изолированная копия — evidence. Out of scope: source/, bitmap-файлы, стиль/толщина рамок, мебель/интерактивные области, пользовательская правка scripts/Days/DayCycleController.cs, экономика, commit/push. Приёмка: межэтажный зазор уже ещё на 6 world px, межкомнатный на 4 world px; перегородки тёмно-серые вместо чёрного при прежних чёрных рамках; нет надписей о закрытой комнате/витринах на обоих языках, кнопка открытия/цена и занавесы сохранены. Проверки: build, rendered --room-frame-checks и --room-doorway-checks, визуальный крупный план, diff-check.

```text
Task: HF-120
Recommended status: REVIEW (принято координатором → DONE)
Changed: RoomFramePresentation продлевает фоны в межэтажный зазор на 13 вместо 10 world px, боковые края во внутренние перегородки на 2 world px; 7 отдельных тёмно-серых перегородок (RGB 0,16) рисуются под прежними чёрными рамками. RoomDoorwayPresentation подгоняет край занавеса под фон без затемнения. Из TradeScene удалены оба LockTitle и refresh заголовка витрин, неиспользуемый StyleLockedTitle удалён. Regression проверяет прежние margin/scale/цвет рамок, новые ширины/цвет 7 перегородок, пиксель реального рендера, отсутствие заголовков и согласование занавесов.
Checks: local/isolated build PASS (0 warnings/errors); rendered --room-frame-checks PASS (exit 0: 24 стороны/углы, прежняя толщина рамок, перегородки, day/night, close-up); --room-doorway-checks PASS (exit 0: failed/successful unlock, reload, checkpoint, NewGame); preview просмотрен; diff-check PASS. LockTitle/StyleLockedTitle больше не встречаются в scripts.
Acceptance: межэтажная перегородка около 13 вместо 19 world px, вертикальные около 10 вместо 14 world px, цвет немного светлее; стиль и толщина рамки сохранены. Нет ROOM LOCKED/DISPLAYS LOCKED и их русских вариантов; занавесы и прежняя кнопка открытия за 100$ сохранены.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt — повторное использование изолированной копии. Пользовательская правка scripts/Days/DayCycleController.cs сохранена без изменений.
Risks/blocker: новых нет; полный integration suite с прежним tooltip baseline FAIL не объявляется зелёным.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf120/{build,local-build,room-frame,room-doorway}*.log и preview.png.
### HF-119 — публикация интерфейса и стартового вида

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; HF-112…HF-118 DONE, пользователь разрешил commit/push.
CP-HF-119 read-set: текущий фронт, Git status/diff/refs/remotes, evidence .local/checks/hf112…hf118, изменённые файлы HF-112…HF-118. Write-set: ORCHESTRATION_PLAN.md; Git index/commits/main и origin/main для текущих docs/{ASSET_AUDIT,GAMEPLAY}.md, localization/en.tres, scripts/Camera/WorldCamera.cs, scripts/Days/DayCycleController.cs, scripts/Presentation/{ButtonFeedbackController,RoomDoorwayPresentation,RoomFramePresentation,CraftButtonPresentation}.cs и CraftButtonPresentation.cs.uid, scripts/TradeScene.cs, scripts/UI/DayAnnouncementOverlay.cs, tests/Integration/ProjectTest.{CameraLimits,DayAnnouncements,DayStart,Resources,RoomFrames}.cs, tests/ProjectTest.cs. Out of scope: .local/, source/, builds/, новые игровые правки, force push. Приёмка: обычный push main, remote SHA совпадает с HEAD, дерево чистое. Проверки: fetch/divergence, diff-check, сохранённые build и профильные rendered tests HF-112…HF-118, проверка удалённой ветки после публикации.

Handoff HF-119: REVIEW → DONE после приёмки координатором. Changed: проверенные изменения HF-112…HF-118 опубликованы коммитом a372bd6 (рамки/стыки и контраст надписей, hover часов, английская кнопка крафта, расширенное объявление дня, старт камеры). Checks: fetch PASS (HEAD и origin/main до публикации совпадали, 0/0); diff-check PASS; local/isolated build PASS (0 warnings/errors), профильные rendered tests из HF-112…HF-118 PASS; push origin main PASS (8b1ac1d..a372bd6); ls-remote подтвердил SHA a372bd62905d20154b81e7fd45b6c5bca058974b. Acceptance: обычный push main выполнен, рабочее дерево перед фиксацией этого отчёта чистое. Read-set exception: нет. Risks/blocker: общий integration baseline tooltip FAIL остаётся вне публикации, полная suite не объявляется зелёной. Next: основной фронт S1-04; отчёт фиксируется отдельным commit/push.
### HF-118 — старт камеры с максимального отдаления по центру сцены

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимости HF-109 DONE, активных пересечений нет.
CP-HF-118 read-set: текущий фронт, scripts/Camera/WorldCamera.cs, scripts/TradeScene.cs (NewGame), scripts/Persistence/TradeScene.Persistence.cs (камера не сохраняется), scenes/trade_scene.tscn (параметры камеры), tests/Integration/ProjectTest.CameraLimits.cs, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, scripts/Camera/WorldCamera.cs, scripts/TradeScene.cs (один вызов сброса вида в NewGame), tests/Integration/ProjectTest.CameraLimits.cs, docs/GAMEPLAY.md (стартовый вид). Evidence: .local/checks/hf118 и изолированная копия. Out of scope: ограничения/управление камеры, сохранения, игровая логика, source/, commit/push. Приёмка: вход в сцену и NewGame устанавливают максимальное разрешённое отдаление для текущего окна, центр мировой сцены с соблюдением границ; прежняя цель зума/drag сброшены; движение/зум/границы сохранены. Проверки: build, rendered --camera-limits-checks с initial/NewGame, два размера окна, diff-check.

```text
Task: HF-118
Recommended status: REVIEW (принято координатором → DONE)
Changed: WorldCamera.ResetToOverview рассчитывает минимальный разрешённый Zoom для текущего viewport, центрирует мировую сцену с ClampToBounds, сбрасывает drag, цель зума и anchor. Вызывается из _Ready и NewGame (один presentation-вызов в TradeScene). Расширен camera regression, уточнены gameplay docs.
Checks: local/isolated build PASS (0 warnings/errors); rendered --camera-limits-checks PASS (exit 0: initial entry, NewGame на 1280×720/1000×800, отсутствие отскока к старой цели/drag, максимальный обзор, 4 края, resize, курсорный anchor, GUI/pause lifecycle); preview просмотрен; diff-check PASS.
Acceptance: начало игры сразу показывает максимально разрешённый обзор по центру сцены; повторная новая игра возвращает этот вид после навигации. Ограничения остаются прежними и учитывают размеры окна.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt — изолированная копия; пользовательские сохранения не затронуты.
Risks/blocker: новых нет.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf118/{build,local-build,camera-limits}*.log и preview.png.
### HF-117 — тонкие стыки рамок без белых просветов

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимости HF-113 DONE, активных пересечений нет.
CP-HF-117 read-set: текущий фронт, scripts/Presentation/RoomFramePresentation.cs, scripts/Presentation/RoomDoorwayPresentation.cs, scripts/TradeScene.cs (overlay), art/ui/camera_frame.png/.import, shaders/monochrome.gdshader, tests/Integration/ProjectTest.RoomFrames.cs, tests/Integration/ProjectTest.Resources.cs, scenes/trade_scene.tscn (геометрия), .local/checks/hf113/preview.png. Write-set (расширен координатором для сужения самого межэтажного зазора): ORCHESTRATION_PLAN.md, scripts/Presentation/RoomFramePresentation.cs, scripts/Presentation/RoomDoorwayPresentation.cs (край занавеса), tests/Integration/ProjectTest.RoomFrames.cs, tests/Integration/ProjectTest.Resources.cs (геометрия фонов/занавесов). .local/checks/hf117 и изолированная копия — evidence. Out of scope: source/, исходные фото, геометрия интерактивных объектов/мебели, камера, затемнение занавеса, экономика, commit/push. Приёмка: фон каждой комнаты заходит на 10 world px в межэтажный зазор (остаток около 19 вместо 39 px), наружный силуэт здания сохранён; горизонтальный штрих рамки вдвое тоньше; непрерывный чёрный сердечник закрывает пиксельные просветы стыка, без белых вкраплений самой рамки; оси рамок остаются на границах, центр прозрачен, закрытые/открытые комнаты работают. Проверки: build, rendered --room-frame-checks (пиксели, толщина, непрерывность), --room-doorway-checks, preview, diff-check.

```text
Task: HF-117
Recommended status: REVIEW (принято координатором → DONE)
Changed: RoomFramePresentation — горизонтальный масштаб штриха 0,2 вместо 0,4, сплошной чёрный сердечник по периметру и чёрная модуляция PNG; фон верхних комнат продлён вниз на 10 world px, нижних вверх на 10 px. Межэтажный зазор уменьшен примерно с 39 до 19 world px. RoomDoorwayPresentation продлевает занавесы до новых краёв. Regression-тесты проверяют фактические пиксели рамок, толщину, непрерывность четырёх сторон и углов, совпадение с новыми границами, геометрию фонов/занавесов после unlock/reload.
Checks: isolated/local build PASS (0 warnings/errors); rendered --room-frame-checks PASS (exit 0); --room-doorway-checks PASS (exit 0: initial/failed/successful unlock/reload/checkpoint/new game); close-up preview просмотрен; diff-check PASS. Субпиксельная непрерывность измеряется с учётом уменьшения до 20%: alpha >0,5 в каждом пикселе оси, непрозрачные части рамки строго чёрные; горизонтальная alpha-толщина <4 px.
Acceptance: межэтажная полоса заметно тоньше, белые вкрапления и просветы вдоль рамок закрыты; рамки совпадают с обновлёнными границами фонов. Фото не потребовалось редактировать. Мебель, интерактивные области, внешний силуэт здания и прозрачность/яркость занавесов сохранены.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt — повторное использование изолированной копии. Write-set расширен координатором на край занавеса и соответствующий regression; без этого фоновые полосы оставались широкими после уменьшения рамки.
Risks/blocker: новых нет; полная integration suite с прежним tooltip baseline FAIL не объявляется зелёной.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf117/{build,local-build,room-frame,room-doorway}*.log и preview.png.
### HF-116 — расширенное уведомление нового дня ниже центра

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; активных пересечений/зависимостей нет.
CP-HF-116 read-set: текущий фронт; scripts/UI/DayAnnouncementOverlay.cs, scripts/Days/DayCycleController.cs, scripts/Configuration/EconomyConfigProvider.cs, scripts/Crafting/WorkshopItemCatalog.cs (число дней); scripts/TradeScene.cs (CurrentDay/lifecycle, без изменений); tests/Integration/ProjectTest.DayAnnouncements.cs, localization/en.tres, docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, scripts/UI/DayAnnouncementOverlay.cs, scripts/Days/DayCycleController.cs (только передача текущего/общего дней), tests/Integration/ProjectTest.DayAnnouncements.cs, localization/en.tres (новые строки), docs/GAMEPLAY.md (уведомление). .local/checks/hf116 и изолированная копия — evidence. Out of scope: длительность/правила дня, сохранения, экономика, source/, commit/push. Приёмка: при открытии показаны номер дня и сколько осталось, включая текущий день (7→1); грамматика RU/EN и live language switch; текстовый блок на 60% высоты, середина/закрытие без счётчика; fade/pause/input/reload сохранены. Проверки: build, rendered --day-announcement-checks (актуализация фикстур под ручной Start из HF-106, RU/EN, размеры, lifecycle, числа), --day-start-checks, diff-check.

```text
Task: HF-116
Recommended status: REVIEW (принято координатором → DONE)
Changed: DayAnnouncementOverlay — дополнительная строка с номером дня/остатком и грамматикой, live перевод составного текста; адаптивная компоновка блока на 60% высоты. DayCycleController передаёт CurrentDay и TotalDays из economy config (однократная загрузка). EN-ключи, gameplay docs; существующая day-announcement fixture актуализирована для явного Start из HF-106 и расширена проверками countdown.
Checks: build PASS (0 warnings/errors); rendered --day-announcement-checks и --day-start-checks PASS (exit 0). Проверены остатки 7/4/2/1, RU/EN plural/live switch, положение ниже центра, размеры, input-ignore, отсутствие details у midday, fade/pause/reload/checkpoint/следующий день/увеличенная длительность; реальный дневной Start/save lifecycle сохранён. EN preview просмотрен; diff-check PASS.
Acceptance: открытие нового дня показывает «Магазин открыт», «День N из 7» и сколько осталось, включая текущий день; остальные объявления ниже центра без счётчика. Длительность анимации и дни/баланс не менялись.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt — изолированная копия; пользовательские сохранения не затронуты.
Risks/blocker: новых нет; общий integration baseline tooltip FAIL вне задачи.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf116/{build,isolated-build,day-announcement,day-start}*.log и preview.png.
### HF-115 — английская кнопка создания предметов

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; активных пересечений/зависимостей нет.
CP-HF-115 read-set: текущий фронт; публичная Drive 17ntZUHFM_HplXZsQJTUuufh4bYPu5M4q → UI/Готовое/{UI в самой игре/Кнопка созданя,Общий UI}; art/ui/{create_button,panel_small}.png, art/fonts/game.ttf; scripts/TradeScene.cs (RefreshCraftLabels), scripts/Localization/GameLocalization.cs, scripts/MainMenu.cs (переключение языка), scenes/trade_scene.tscn (стили/кнопки), scripts/UI/EmbroideryGame.cs (тест открытия/отмены); tests/Integration/ProjectTest.{Resources,Localization}.cs; tests/ProjectTest.cs; docs/ASSET_AUDIT.md. Write-set: ORCHESTRATION_PLAN.md, scripts/Presentation/CraftButtonPresentation.cs/.uid; scripts/TradeScene.cs (только вызовы оформления в RefreshCraftLabels), tests/Integration/ProjectTest.Resources.cs, tests/ProjectTest.cs (новая группа), docs/ASSET_AUDIT.md. .local/checks/hf115, изолированная копия — evidence. Out of scope: source/, билды Drive, замена русской графики, экономика/крафт/сохранения, commit/push. Приёмка: готовой EN-версии в разделе создания нет (только русская PNG и три примера подсказки); английская надпись CREATE ITEMS добавлена средствами UI поверх готовой пустой панели в прежнем стиле; значок ? сохранён crop из исходника; обе мастерские меняются RU/EN без изменения состояний/размеров/кликов. Проверки: build, rendered --craft-localization-checks (RU/EN/RU, состояния, реальные кнопки, preview), diff-check.

```text
Task: HF-115
Recommended status: REVIEW (принято координатором → DONE)
Changed: новый CraftButtonPresentation.cs/.uid собирает английскую кнопку из готовой blank panel, нативного CREATE ITEMS и AtlasTexture значка вопроса; две строки подключения в RefreshCraftLabels; RU/EN regression-группа; аудит и план. Русская PNG и scene-геометрия не менялись.
Checks: Drive root/UI/Готовое/UI в самой игре/Кнопка созданя и Общий UI прочитаны; actual download единственной кнопки совпадает с русской runtime PNG по SHA256 EA951785AF6E1510E665F4A09612A420A7A4874A187745F12F08961C574E6023. Build PASS (0 warnings/errors); isolated/local import PASS (exit 0); rendered --craft-localization-checks PASS (exit 0: RU/EN/RU/EN, оба workshop, normal/hover/pressed/disabled, сохранённые размеры/availability, overlay input-ignore, настоящий клик открывает прежнюю мини-игру без раннего списания, Escape отменяет). EN preview просмотрен; diff-check PASS.
Acceptance: английская кнопка есть в обеих мастерских при English, исходная русская картинка возвращается при Russian. Готовая английская bitmap-кнопка не найдена в профильном разделе; новая версия собрана кодом UI из существующей графики и шрифта, без AI/bitmap-редактирования. Прежняя экономика и область подсказки сохранены.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt для изолированной копии; source/ и пользовательские настройки/сохранения не затронуты.
Risks/blocker: наличие EN-версии в иных непрофильных разделах Drive не утверждается; прежние duplicate UI UID warnings вне задачи.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf115/drive-*.html, drive-create-button.png, {build,isolated-build,import,local-import,checks}*.log и preview.png.
### HF-114 — лёгкое затемнение часов при наведении

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; активных зависимостей/пересечений нет.
CP-HF-114 read-set: текущий фронт; scripts/Days/DayCycleController.cs, scripts/Presentation/ButtonFeedbackController.cs, shaders/monochrome.gdshader; tests/Integration/ProjectTest.DayStart.cs. Write-set: ORCHESTRATION_PLAN.md; scripts/Days/DayCycleController.cs (только feedback часов); scripts/Presentation/ButtonFeedbackController.cs (opt-out для локального feedback); tests/Integration/ProjectTest.DayStart.cs (rendered hover). .local/checks/hf114 и изолированная копия — evidence. Out of scope: поведение запуска дня, прочие кнопки, экономика, source/, commit/push. Приёмка: на часах/«Начать» нет прямоугольной hover/focus-рамки; лёгкое затемнение самой текстуры на 10% по силуэту, без прямоугольной плашки; уход мыши/начало дня возвращает яркость, настоящий клик работает, фокус клавиатуры заметен тем же затемнением. Проверки: build, rendered --day-start-checks с пиксельным сравнением hover/normal, diff-check.

Handoff HF-114: REVIEW → DONE после приёмки координатором. Changed: кнопка старта часов использует локальный feedback (group opt-out из общего rectangular outline), StyleBoxEmpty для пяти состояний, hover/keyboard focus затемняют только текстуру часов до 0,9; exit/скрытие старта возвращают белую модуляцию. Checks: build PASS (0 warnings/errors); rendered --day-start-checks PASS (exit 0), >100 светлых пикселей часов стали темнее на 5–15%, отсутствуют global/theme borders, проверены уход мыши, фокус, очистка после настоящего клика и прежний lifecycle/save/reload; preview просмотрен; diff-check PASS. Acceptance: лёгкое затемнение по alpha-силуэту без рамки/прямоугольной плашки, прочие кнопки сохраняют прежний feedback. Read-set exception: .local/checks/hf104/hf104-check-dir.txt для изолированной копии. Risks/blocker: новых нет. Next: основной фронт S1-04. Evidence: .local/checks/hf114/{build,isolated-build,checks}*.log и preview.png. Commit/push не выполнялись.
### HF-113 — рамки точно по границам комнат

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимости/активные пересечения отсутствуют.
CP-HF-113 read-set: текущий фронт, scripts/Presentation/RoomFramePresentation.cs, art/ui/camera_frame.png, tests/Integration/ProjectTest.RoomFrames.cs, scenes/trade_scene.tscn (трансформы/фоны комнат). Write-set: ORCHESTRATION_PLAN.md, scripts/Presentation/RoomFramePresentation.cs, tests/Integration/ProjectTest.RoomFrames.cs. .local/checks/hf113/ и изолированная копия — evidence. Out of scope: размеры/положение комнат, камера, фон, надписи HF-112, экономика, source/, commit/push. Приёмка: центр реального штриха PNG совпадает с четырьмя краями каждой комнаты, нет прежнего внутреннего отступа; прозрачный центр/мышь/занавесы сохраняются. Проверки: build, rendered --room-frame-checks с проверкой всех шести границ и снимком при близком zoom, --room-doorway-checks, diff-check.

```text
Task: HF-113
Recommended status: REVIEW (принято координатором → DONE)
Changed: RoomFramePresentation теперь один раз измеряет середину alpha-штриха на четырёх сторонах исходного PNG и выносит соответствующую часть NinePatch за границы комнаты, совмещая штрих с периметром; atlas переиспользуется шестью рамками. Tests/ProjectTest.RoomFrames — измерение центра реального отрисованного alpha-штриха в отдельном прозрачном SubViewport для всех 24 сторон, крупный preview; план.
Checks: build PASS (0 warnings/errors); rendered --room-frame-checks PASS (exit 0), пиксельная погрешность центра штриха <0,8 px в контрольном рендере; --room-doorway-checks PASS (exit 0); крупный план просмотрен; diff-check PASS.
Acceptance: устранён внутренний отступ визуальной рамки — ось штриха проходит по периметру комнаты. Размеры/положения комнат, прозрачность центра, игнорирование мыши и z-index поверх занавесов сохранены; правки контраста HF-112 сохранены.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt для повторного использования изолированной копии. source/ и пользовательские сохранения не затронуты.
Risks/blocker: естественная неровность рисунка PNG сохранена; новых блокеров нет.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf113/{build,isolated-build,room-frame,room-doorway}*.log и preview.png.
### HF-112 — читаемая надпись закрытой комнаты

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимостей/активных пересечений нет. По скриншоту ROOM LOCKED теряется на светлом занавесе.
CP-HF-112 read-set: текущий фронт, scripts/TradeScene.cs (создание LockTitle), scripts/Presentation/RoomDoorwayPresentation.cs, tests/Integration/ProjectTest.Resources.cs. Write-set: ORCHESTRATION_PLAN.md; scripts/TradeScene.cs (только стиль двух LockTitle); scripts/Presentation/RoomDoorwayPresentation.cs (общий стиль); tests/Integration/ProjectTest.Resources.cs (проверка контраста). .local/checks/hf112 и изолированная копия — evidence/preview. Out of scope: затемнение занавеса, экономика, геометрия, source/, commit/push. Приёмка: чёрный текст с белой обводкой читаем поверх светлых и чёрных полос, одинаковый стиль у ROOM LOCKED/DISPLAYS LOCKED, прежняя локализация/покупка. Проверки: build, --room-doorway-checks, рендер, diff-check.

Handoff HF-112: REVIEW → DONE после приёмки координатором. Changed: общий StyleLockedTitle в RoomDoorwayPresentation, два вызова оформления в TradeScene, regression-проверка обоих заголовков и план. Checks: build PASS (0 warnings/errors), isolated --room-doorway-checks PASS (exit 0), rendered EN preview PASS (exit 0), снимок просмотрен, diff-check PASS. Acceptance: чёрный текст с белой обводкой 6 читается на занавесе; надпись витрин оформлена так же, яркость PNG и покупка не менялись. Read-set exception: .local/checks/hf104/hf104-check-dir.txt для изолированной копии. Risks/blocker: новых нет. Next: основной фронт S1-04. Evidence: .local/checks/hf112/{build,checks,preview}*.log и preview.png. Commit/push не выполнялись.
### HF-111 — публикация изменений интерфейса и камеры

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13. HF-105…HF-110 DONE; пользователь явно разрешил commit и push.
CP-HF-111 read-set: текущий фронт, Git status/diff/refs/remotes, evidence HF-110 в .local/checks/hf110/. Write-set: ORCHESTRATION_PLAN.md; Git index/commit/main и origin/main для текущего проверенного набора .gitignore/README/docs, art/ui/camera_frame.png/.import, scenes/trade_scene.tscn, scripts/Camera/WorldCamera.cs, scripts/Days/DayCycleController.cs, scripts/Persistence/TradeScene.Persistence.cs, scripts/TradeScene.cs, scripts/Presentation/RoomFramePresentation.cs/.uid, src/ChikagoShop.Domain/Persistence/ProgressData.cs, tests/ChikagoShop.Domain.Tests/ProgressMigrationTests.cs, tests/ProjectTest.cs, tests/Integration/ProjectTest.{CameraLimits,DayStart,RoomFrames}.cs/.uid. Out of scope: .local/, source/, builds/, новые игровые изменения, force push. Приёмка: обычный push main, remote SHA совпадает с HEAD, рабочее дерево чистое.

Handoff HF-111: REVIEW → DONE после приёмки координатором; изменения HF-105…HF-110 опубликованы коммитом 55da86b. git push origin main PASS (6f0e8e4..55da86b), ls-remote подтвердил SHA 55da86b3f5f3cda6fa1b25751f916e6070cc6144, дерево чистое после публикации. Checks: fetch PASS (HEAD и origin/main = 6f0e8e4, 0/0); diff-check PASS; сохранены build PASS (0 warnings/errors), domain PASS (132/132), rendered room-frame/camera-limits/day-start PASS из HF-110; импорт PASS. Полный integration suite не объявляется зелёным: прежнее baseline tooltip-падение задокументировано. Read-set exception: нет. Risks/blocker: прежние диагностические предупреждения вне задачи. Next: основной фронт S1-04.
### HF-110 — порядок в папках проекта

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; HF-105…HF-109 DONE, активных пересечений нет.
CP-HF-110 read-set: текущий фронт, AGENTS.md, .gitignore, корневые файлы и имена .godot/hf102…hf109; scripts/Presentation/{ViewportFrame,RoomFramePresentation}.cs/.uid; tests/Integration/ProjectTest.ViewportFrame.cs/.uid, tests/ProjectTest.cs; scenes/trade_scene.tscn; README.md, docs/{ASSET_AUDIT,TESTING,GAMEPLAY}.md, docs/archive/ORCHESTRATION_HISTORY_2026-11-13.md; РамкаПустая.PNG/.import, art/ui/camera_frame.png. Write-set: ORCHESTRATION_PLAN.md, docs/archive/ORCHESTRATION_HISTORY_2026-11-13.md, .gitignore, README.md, docs/{ASSET_AUDIT,TESTING}.md; scripts/Presentation/RoomFramePresentation.cs; удаление неиспользуемого scripts/Presentation/ViewportFrame.cs/.uid после локальной резервной копии; переименование tests/Integration/ProjectTest.ViewportFrame.cs/.uid → ProjectTest.RoomFrames.cs/.uid, tests/ProjectTest.cs (имя метода и alias группы); .local/ (игнорируемые оригиналы, references, evidence, archive, .gdignore); перемещение только root РамкаПустая.PNG/.import и .godot/hf102…hf109 туда. Out of scope: source/, builds/, пользовательские сохранения, игровой баланс/поведение, остальные кэши Godot, commit/push. Приёмка: корень без дубликата фото; dev-материалы вне кэша Godot и Git; нет мёртвого экранного CanvasLayer-класса, room factory сохранён; архивированные завершённые отчёты и актуальная таблица проверок. Проверки: build/domain, import, rendered room-frame/camera-limits/day-start checks, local Markdown links, сохранность основной спецификации, diff-check.

```text
Task: HF-110
Recommended status: REVIEW (принято координатором → DONE)
Changed: .local/{originals,references,checks,archive} с .gdignore, игнорируется Git; пользовательский оригинал рамки и sidecar перемещены из корня, все HF-102…HF-109 evidence сохранены и сгруппированы; удалён неиспользуемый ViewportFrame.cs/.uid с резервной копией .local/archive/*.txt, неизменённая room factory теперь принадлежит RoomFramePresentation; тест переименован в ProjectTest.RoomFrames с сохранением UID, основной флаг --room-frame-checks и совместимый alias --camera-frame-checks; README/docs/план упорядочены, завершённые HF-104…HF-109 перенесены в архив за 13 ноября.
Checks: build PASS (0 warnings/errors); domain PASS (132/132); Godot import PASS (exit 0); rendered --room-frame-checks, --camera-limits-checks, --day-start-checks PASS (exit 0). Markdown links PASS, основная спецификация после «Как пользоваться планом» совпадает с HEAD, оригинал рамки и runtime-копия имеют одинаковый SHA256, .local игнорируется Git, ссылок на удалённый класс/метод нет, diff-check PASS.
Acceptance: корень без дубликата изображения; локальные материалы отделены от кэша движка, имена evidence сохранены; шесть комнатных рамок и прежние незакоммиченные изменения сохранены, игровое поведение не менялось. source/, builds/ и пользовательские сохранения не затронуты.
Read-set exception: .local/checks/hf104/hf104-check-dir.txt прочитан для повторного использования изолированной копии; в ней удалены только четыре известные устаревшие script/test-файла после проверки абсолютных путей.
Risks/blocker: прежние duplicate UI UID warnings и baseline tooltip FAIL общего suite вне задачи; материалы .local остаются только на этом компьютере.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .local/checks/hf110/{build,domain,isolated-build,import,room-frame,camera-limits,day-start}*.log.
Основная стадия: **S1 — домен, конфигурация и воспроизводимость**. `G-S0 = PASSED`, `G-S1 = OPEN`.

| Состояние | Задача | Следующее действие |
| --- | --- | --- |
| `DONE` | `S1-01`, `S1-02`, `S1-03` | Контекст-пакеты сохранены ниже. |
| `NEXT` | `S1-04` — именованные потоки RNG | Подготовить CP по актуальному коду перед назначением. |
| Требует сверки | `HF-64`, `VF-02…VF-05` | Сопоставить старые незакрытые статусы с evidence поздних задач. |

Последняя публикация изменений: `55da86b` — рамки только у комнат, часы +15% и ручной старт дня, привязка комнат к day/night-схемам, ограничения камеры и порядок в папках. HF-105…HF-111 завершены и опубликованы; последующая запись закрывает отчёт публикации.

Отчёты и контекст-пакеты: [до 12 ноября](docs/archive/ORCHESTRATION_HISTORY_2026-11-12.md), [за 13 ноября](docs/archive/ORCHESTRATION_HISTORY_2026-11-13.md). Два HF-100 различаются названиями; номера не менялись. Внеочередные задачи не закрывают ворота стадий автоматически.

Перед релизом остаются прежние диагностики: duplicate UID четырёх пар кнопок art/ui и art/ui/ru; resource/ObjectDB warning при завершении отдельных Godot-проверок. Их наличие не означает прохождение release gate.

## Навигация

- [Как пользоваться планом](#как-пользоваться-планом)
- [Быстрый контекст и требования](#быстрый-контекст)
- [Исторический baseline](#baseline-на-31-октября-2026)
- [Карта стадий и ворота](#карта-стадий)
- [Открытые решения](#открытые-решения)
- [Реестр рисков](#реестр-рисков)
- [Контекст-пакеты выполненных задач](#контекст-пакеты-выполненных-задач)
- [Definition of Done MVP](#definition-of-done-mvp)
- [Журнал решений](#журнал-решений)
- [История HF и visual pass до 12 ноября](docs/archive/ORCHESTRATION_HISTORY_2026-11-12.md)
- [История задач за 13 ноября](docs/archive/ORCHESTRATION_HISTORY_2026-11-13.md)

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

## Baseline на 31 октября 2026

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

## Контекст-пакеты выполненных задач

Пакеты ниже сохранены для воспроизводимости S0-04, S0-06 и S1-01…S1-03. Они не являются новым READY-фронтом; для S1-04 нужен отдельный актуальный CP.

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
