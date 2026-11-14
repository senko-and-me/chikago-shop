# Оркестровый план: «Бакалея для гангстеров»

Версия плана: 1.2
Обновлено: 2026-11-14
Стадия по реестру: `S1 — домен, конфигурация и воспроизводимость`

Этот файл хранит требования, решения, стадии и текущий фронт. Рабочий протокол — в [AGENTS.md](AGENTS.md), подробные завершённые отчёты — в [архиве](docs/archive/ORCHESTRATION_HISTORY_2026-11-12.md). `source/` остаётся локальным архивом и не входит в Git.

Требования и таблицы ниже сохраняют первоначальный план от 31.10.2026. Внеочередные HF-задачи развивали прототип дальше этого среза; старые числа и описания не следует принимать за актуальную конфигурацию runtime. Текущие игровые механики описаны в [docs/GAMEPLAY.md](docs/GAMEPLAY.md), числа — в `config/economy.v1.json`. Завершение функции в HF не заменяет приёмку соответствующей задачи стадии.

## Текущий фронт

### HF-142 — публикация обучения HF-140/HF-141

Статус: `IN_PROGRESS`; координатор/исполнитель `/root`; READY 2026-11-14; HF-140/HF-141 DONE. Пользователь разрешил commit/push.

CP-HF-142 read-set: текущий фронт, diff/status/log/remote Git, evidence `.local/checks/hf140` и `.local/checks/hf141`. Write-set: Git index/commits/main/origin/main; ORCHESTRATION_PLAN.md, docs/GAMEPLAY.md, localization/en.tres, scenes/UI/FirstTradeTutorial.tscn, scripts/Camera/WorldCamera.cs, scripts/TradeScene.cs, scripts/UI/FirstTradeTutorial.cs, scripts/Presentation/TutorialCameraCheckpoint.cs и .uid, tests/Integration/ProjectTest.CameraLimits.cs, tests/Integration/ProjectTest.Menu.cs, tests/Integration/ProjectTest.Tutorial.cs, tests/ProjectTest.cs (публикация готового diff). Out of scope: source/, пользовательский diff scenes/trade_scene.tscn, новые изменения поведения. Приёмка: готовые изменения обучения отправлены обычным push без force, HEAD совпадает с origin/main; пользовательская локальная сцена сохранена вне коммита. Проверки: существующие build/tutorial/camera-limits/card-dealing/intro-transition PASS, staged diff-check, push и сравнение remote HEAD.

### HF-141 — обучение каждой новой партии и подтверждение пропуска

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
### HF-140 — камера по этапам обучения и практика управления

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
### HF-139 — аудит последних коммитов и безопасное обновление

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
