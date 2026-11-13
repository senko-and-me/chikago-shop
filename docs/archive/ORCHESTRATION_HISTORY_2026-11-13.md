# История задач оркестрового плана за 13 ноября 2026

[Вернуться к основному плану](../../ORCHESTRATION_PLAN.md)

Завершённые отчёты сохранены из текущего фронта. Две разные задачи HF-100 различаются названиями: публикация перевода и сверка Drive-ассетов. Исторические номера сохранены. Затемнение из HF-102 отменено в HF-103; актуальный занавес имеет исходную яркость.

### HF-103 — убрать затемнение занавеса

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимость HF-102 DONE; пересечений с активными задачами нет.
CP-HF-103 read-set/write-set: ORCHESTRATION_PLAN.md, scripts/Presentation/RoomDoorwayPresentation.cs, tests/Integration/ProjectTest.Resources.cs. Out of scope: остальные изменения оформления, экономика, source/, commit/push. Приёмка: занавес обеих комнат отображается без цветового затемнения (Modulate = White). Проверки: dotnet build ChikagoShop.sln --no-restore; --room-doorway-checks; git diff --check. Evidence — .godot/hf103-*.log. Handoff: REVIEW → DONE после приёмки координатором; Changed: перечисленный write-set; build PASS (0 warnings/errors), room-doorway-checks PASS (exit 0), diff-check PASS. Acceptance: оба занавеса без затемнения. Read-set exception: нет. Risks: прежние shutdown resource warnings. Next: основной фронт S1-04.
### HF-102 — занавес закрытых комнат

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимостей нет, активных пересекающихся задач нет.
CP-HF-102 read-set: текущий фронт, AGENTS.md, IMG_2363.PNG, scripts/TradeScene.cs (только создание закрывающих панелей), scripts/Presentation/RoomDoorwayPresentation.cs, tests/Integration/ProjectTest.Resources.cs, tests/ProjectTest.cs, scenes/trade_scene.tscn, .vscode/settings.json, tools/verify.ps1. Write-set: ORCHESTRATION_PLAN.md, art/rooms/locked_curtain.png и .import, scripts/TradeScene.cs (только подключение оформления двух панелей), scripts/Presentation/RoomDoorwayPresentation.cs, tests/Integration/ProjectTest.Resources.cs. TEMP/.godot — evidence. Out of scope: экономика, сохранения, новые комнаты, source/, commit/push, редактирование исходного фото. Приёмка: указанное фото закрывает музыкальную комнату и правые витрины до покупки; кнопка и подписи читаемы, открытие/reload/checkpoint/New Game сохраняют прежнее поведение. Проверки: dotnet build, --room-doorway-checks, git diff --check, рендер.



Handoff HF-102 (2026-11-13): REVIEW → DONE после приёмки координатором. Changed: art/rooms/locked_curtain.png и .import, scripts/Presentation/RoomDoorwayPresentation.cs, две строки подключения в scripts/TradeScene.cs, tests/Integration/ProjectTest.Resources.cs, план. Исходное фото скопировано без изменения байтов. Панели сохраняют прежние размеры и обработку мыши; занавес затемнён до 0,32 для белых надписей, закрытые витрины скрываются до открытия. Checks: dotnet build ChikagoShop.sln --no-restore → PASS (0 warnings/errors); Godot import → PASS; --room-doorway-checks → PASS (exit 0: оба занавеса, неуспешная/успешная покупка за прежние 100$, reload, checkpoint, New Game); рендер GL compatibility → PASS, снимок просмотрен; git diff --check → PASS. Evidence: .godot/hf102-{import,checks,preview}*.log, .godot/hf102-preview.png. Read-set exception: scripts/Camera/WorldCamera.cs, scripts/Presentation/ButtonFeedbackController.cs, scripts/UI/InventoryUI.cs прочитаны для диагностики рендера при дальнем zoom. Risks: прежние duplicate UID warnings при импорте и shutdown resource/ObjectDB warning в headless-проверке остаются. Next: продолжить основной фронт S1-04. Commit/push не выполнялись.

### HF-100 — объединить и опубликовать английский перевод

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13. Пользователь явно разрешил push перевода в main. Локальный перевод bf74adf объединяется с origin/main=68973e4 обычным merge без force/rebase.
CP-HF-100: read-set — текущий фронт и git status/diff/log; файлы HF-97 (английская локализация), новые README/docs/архив origin/main, tests/ProjectTest.cs, локальные test/toolchain evidence. Write-set — разрешение документальных конфликтов в ORCHESTRATION_PLAN.md/README.md; docs/DEVELOPMENT.md и docs/TESTING.md (перенос описания перевода); docs/archive/ORCHESTRATION_HISTORY_2026-11-12.md (сохранение исходного отчёта перевода); Git index/merge commit и origin/main. Out of scope: source/, удаление новых удалённых изменений, новые продуктовые изменения, force push. Приёмка: код перевода и все три новых удалённых коммита сохранены; новая структура документации сохранена, сведения о переводе перенесены в профильные разделы; build/domain и targeted localization/music suites проходят, обычный push main и remote ref comparison успешны.

```text
Task: HF-100
Recommended status: REVIEW
Changed: merge 71a72da объединяет локальный перевод bf74adf и origin/main=68973e4; README/план разрешены с сохранением новой структуры, описание перевода перенесено в docs/DEVELOPMENT.md и docs/TESTING.md, исходный отчёт локализации сохранён в архиве.
Checks: build -> PASS (0 warnings/errors); domain -> PASS (131 tests); --dialogue-localization-checks, --menu-music-checks, --intro-music-checks, --game-music-checks -> PASS; staged diff-check -> PASS. Runtime/scene remote changes match origin/main, translation runtime/catalog/tests match bf74adf. git push origin main -> PASS (68973e4..71a72da); git ls-remote confirmed 71a72da; working tree clean. Evidence: TEMP/chikago-hf100-{build,domain,import,dialogue-localization,menu-music,intro-music,game-music}.log.
Acceptance: перевод опубликован в main без перезаписи истории, все новые удалённые коммиты сохранены. Временные настройки и тестовые сохранения изолированы; override.cfg удалён.
Read-set exception: архив и профильные docs прочитаны для необходимого разрешения конфликтов после удалённой реорганизации; source/ не читался.
Risks/blocker: только ранее задокументированные import-предупреждения о дублях UID четырёх пар кнопок; музыкальные/языковые тесты завершились чисто. Новых блокеров публикации нет.
Next: продолжить от синхронизированного main.
```

Координатор подтвердил HF-100 2026-11-13 после успешного push и проверки удалённой ref: REVIEW → DONE.
Полный отчёт «HF-97 — английская локализация вступительного диалога» сохранён в [архиве](../archive/ORCHESTRATION_HISTORY_2026-11-12.md). Эта историческая HF-97 отличается от одноимённой задачи уборки проекта; её номер не изменён.

### HF-101 — готовые инструменты в существующей полировке

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13. Пользователь попросил проверить пригодность предметов для второй мини-игры; ограничение предыдущей задачи — не добавлять новые объекты.
CP-HF-101 read-set: текущий фронт; scripts/UI/PolishingSurface.cs, EmbroideryGame.cs; art/items/{guitar,violin,drums}.png; art/minigames/violin.png; tests/Integration/ProjectTest.Resources.cs; docs/ASSET_AUDIT.md. Write-set: эти два UI-скрипта, указанная regression-проверка, docs/ASSET_AUDIT.md, ORCHESTRATION_PLAN.md. TEMP/.godot — пробы и снимки.
Приёмка: готовые гитара и скрипка остаются в полировке; подходящий прозрачный барабан заменяет прежний случайный вариант SVG-саксофона; по-прежнему четыре варианта мини-игры, прежние оплата/выдача/таймер. Маска совпадает с PNG, очистка работает, alpha-фон не даёт ложных промахов. Fallback sax сохраняется для совместимости прямых вызовов. Out of scope: новые товары/комнаты/мини-игры, экономика/сохранения, TradeScene.cs, source/, commit/push. Проверки: build, asset-replacement-checks в изолированной копии, рендер барабана и настоящий mouse-drag.

Проверено HF-101: build решения PASS (0 warnings/errors); rendered asset-replacement-checks PASS, exit 0 в прежней изолированной TEMP-копии. Проверены четыре текущие маски и fallback sax, открытие барабана в реальной мини-игре, прежнее списание денег/энергии и реальные mouse-drag по силуэту; полный торговый/reload-сценарий HF-100 также PASS. Снимок drum-polishing.png из изолированного userdata просмотрен: поле/масштаб/прозрачность корректны. Evidence: .godot/drum-checks.log и drum-check-errors.log (пустой), diff-check PASS. Новых объектов каталога нет; фон/таймер/экономика не менялись, source/ не читался, commit/push не выполнялись.

### HF-100 — сверить Drive-ассеты с существующими объектами

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13.
CP-HF-100 read-set: AGENTS.md; текущий фронт; публичная Drive-папка 17ntZUHFM_HplXZsQJTUuufh4bYPu5M4q и её разделы ассетов (без билдов); art/ и import-sidecars; scripts/Crafting/WorkshopItemCatalog.cs, scripts/UI/, scripts/Presentation/, scripts/ModularCustomer.cs, scenes/, tests/Integration/ProjectTest.Resources.cs и проверки crafting/presentation; README/docs со ссылками на ресурсы. source/ не читается.
Write-set: ORCHESTRATION_PLAN.md; docs/ASSET_AUDIT.md; существующие runtime-изображения art/ (замена готовыми Drive-версиями, если назначение однозначно); точечные ссылки существующих предметов/UI в scripts/scenes и соответствующие regression-проверки после фиксации выбранного набора. Кэши, скачивания и отчёты сравнения — .godot/.
Out of scope: новые игровые предметы/комнаты/персонажи/механики, экономика, ID и индексы каталога, сохранения, source/, commit/push. Приёмка: понятный перечень задействованных/неиспользуемых ассетов и подходящих замен; однозначные замены применены к существующим объектам; состав объектов и правила не меняются. Проверки: build, импорт, ссылки/ресурсы и целевые проверки изменённых компонентов, визуальная сверка.

Основная стадия по реестру: **S1 — домен, конфигурация и воспроизводимость**. Ворота `G-S0 = PASSED`, `G-S1 = OPEN`. Выполненные внеочередные задачи не закрывают ворота стадий автоматически.

| Состояние | Задача | Следующее действие |
| --- | --- | --- |
| `DONE` | `S1-01`, `S1-02`, `S1-03` | Приёмка и контекст-пакеты сохранены ниже. |
| `NEXT` | `S1-04` — именованные потоки RNG | Перед назначением подготовить `CP-S1-04` по актуальному коду. |
| Требует сверки | `HF-64` — переход после диалога и Windows-билд | В старой записи остался `IN_PROGRESS`; проверить evidence перед сменой статуса. |
| Требует сверки | `VF-02…VF-05` — visual pass | В журнале остались `IN_PROGRESS`; сопоставить приёмку с более поздними HF-задачами. |

Последняя опубликованная работа: `68973e4` — уборка плана и архив истории. HF-97…HF-99 завершены; HF-100 выполнен локально по запросу сверки Drive-ассетов. Основной очередной фронт S1-04 этим не заменён.

Полные отчёты, контекст-пакеты и первоначальная таблица visual pass: [архив задач до 12.11.2026](../archive/ORCHESTRATION_HISTORY_2026-11-12.md). Исторические номера задач не перенумерованы; два HF-93 различаются названиями «мишень вышивки» и «музыка меню».

### Что проверить перед следующим релизом

- При импорте обнаружены повторяющиеся UID четырёх пар кнопок `art/ui/` и `art/ui/ru/`.
- `resource-checks` завершается с кодом 0, но Godot сообщает о незакрытых объектах/ресурсе при shutdown.
- Эти диагностики не были устранены уборкой документации и не означают прохождение полного release gate.

Выбранный набор HF-100: добавить только файлы текстур art/items/{guitar,violin,drums}.png (новых игровых сущностей нет); заменить ArtPath индексов 2/7/8 в scripts/Crafting/WorkshopItemCatalog.cs; существующую Guitar-текстуру и масштаб в scenes/trade_scene.tscn; scripts/CounterWorld.cs — создавать ровно недостающие прежние варианты по kind, а не по имени placeholder-файла; scripts/UI/PolishingSurface.cs — использовать готовую гитару в существующей полировке с маской по alpha. Точные тестовые write-set: tests/ProjectTest.cs, tests/Integration/ProjectTest.Resources.cs, tests/Integration/ProjectTest.Economy.cs. Остальные ассеты только классифицируются в отчёте; шрифт не заменяется из-за отсутствия латиницы.

Проверено HF-100 13.11.2026: 30 публичных папок, 95 файлов вне билдов; 66 готовых файлов/PNG-схем скачаны и сопоставлены, 45 имеют исходное SHA256-совпадение. Полный перечень и ограничения анализа — [docs/ASSET_AUDIT.md](../ASSET_AUDIT.md). Новый шрифт не применён: отсутствует 52 латинские буквы. Гитара и две прежние редкие заглушки используют готовые PNG; всего по-прежнему 10 товаров и 10 торговых вариантов. Байты PNG совпадают с Drive. Сборка решения PASS (0 warnings/errors), импорт рабочего проекта PASS, повторный импорт чистой TEMP-копии PASS, asset-replacement-checks и resource-checks PASS (exit 0), Godot-render трёх замен просмотрен; diff-check PASS. Evidence: .godot/asset-{checks,resources,preview,isolated-import-retry}.log. Первичный импорт TEMP-копии аварийно завершился при первоначальной загрузке темы без готового кэша шрифта; повторный прошёл. Прежние duplicate UID warnings сохранены. Локальный source/ и пользовательские сохранения не читались/не менялись; commit/push не выполнялись.

### HF-99 — упорядочить оркестровый план

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-12.

CP-HF-99: read-set — AGENTS.md, исходный ORCHESTRATION_PLAN.md, Git status, ссылки на план в README.md и docs/DEVELOPMENT.md. Write-set — ORCHESTRATION_PLAN.md и docs/archive/ORCHESTRATION_HISTORY_2026-11-12.md. Out of scope — игровой код, переоценка статусов задач/ворот, source/, commit/push.

Приёмка: основной план начинается с краткого фронта и навигации; прежний фронт сохранён целиком в архиве; незакрытые записи явно видны; требования, решения, таблицы стадий и CP сохранены. Проверки: сравнение исходного журнала, сохранность спецификации, Markdown-ссылки, git diff --check. Изменений поведения игры нет.

Проверено 2026-11-12: исходный журнал сохранён дословно; спецификация, таблицы стадий/ворот, решения и CP совпадают с исходными после двух переименований заголовков и пояснения к старым пакетам. Локальные ссылки и якоря Markdown проверены. Игровой код не менялся, тесты игры не перезапускались. Основной файл сокращён с 2 785 до примерно 650 строк. Публикация уборки плана разрешена последующим явным запросом пользователя; изменения входят в отдельный документационный коммит.

## Дополнение: рамки, часы, фоны и камера (HF-104…HF-109)

### HF-109 — ограничения камеры по трём схемам

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимость HF-107 DONE; активных пересечений нет.
CP-HF-109 read-set: текущий фронт; три Drive PNG пользователя (минимальная область 1csoJtefH4ZZIALFxMPGE_5g7mIDz4Glj, максимальный диапазон 1Kgt76OwRZ819p1ZswnuvWgyil3eF6gsr, максимальная область 1TLw13kwydk8oPXqeM93lit8s3Zh_1dk0); scripts/Camera/WorldCamera.cs; scenes/trade_scene.tscn; tests/Integration/ProjectTest.Camera.cs; tests/ProjectTest.cs; docs/GAMEPLAY.md. Write-set: ORCHESTRATION_PLAN.md, scripts/Camera/WorldCamera.cs, scenes/trade_scene.tscn (только camera exports/initial view), tests/ProjectTest.cs (новая группа), tests/Integration/ProjectTest.CameraLimits.cs/.uid, docs/GAMEPLAY.md. .godot/TEMP — скачивания/evidence/изолированная копия. Out of scope: экономика, комнаты/фоны/рамки, HUD, source/, commit/push. Приёмка: ограничение видимой области камеры в пределах red range, zoom-out не шире 2560x1440, zoom-in не уже 1036x583 в координатах исходной схемы 4560x2565; центр/начальный вид из максимальной области; плавный zoom к курсору/drag/GUI/pause сохраняются; ограничения действуют после resize. Проверки: build, --camera-limits-checks (границы, wheel, pan, два размера, исходная standalone camera suite), rendered endpoints, diff-check.

```text
Task: HF-109
Recommended status: REVIEW (принято координатором → DONE)
Changed: WorldCamera — опциональные ReferenceSize/NavigationRegion/OverviewSize/CloseupSize, единый пересчёт из reference в world; ограничение обоих измерений viewport при zoom/resize; camera exports и начальный overview сцены; новая regression-группа, docs/GAMEPLAY, план.
Checks: build PASS (0 warnings/errors); isolated/local import PASS (exit 0); rendered --camera-limits-checks PASS (exit 0), включая TestStandaloneCamera. Проверены wheel до обоих пределов, четыре угла pan с ограничением целого viewport, два размера окна, cursor anchor, настоящие GUI drag/world drag, pause/resume и lifecycle. Рендеры максимального обзора и приближения у кассы просмотрены; diff-check PASS.
Acceptance: по скачанным 4560x2565 PNG красные границы: диапазон Rect2(429,563,3662,1440), максимальный обзор 2560x1440 (центр 2280,1283), минимальный 1036x583 (пример вокруг кассы x1754,y1225). В 1280x720 runtime zoom примерно 0,395833…0,9777. Пределы масштабируются от размера мира и viewport, поэтому не зависят от абсолютного размера окна. Комнаты/рамки/фон/игровая экономика не менялись. Камера без заданных reference-полей сохраняет прежнее standalone-поведение.
Read-set exception: .godot/hf104-check-dir.txt — повторное использование изолированной копии; source/ и пользовательские сохранения не читались и не менялись.
Risks/blocker: красные растровые границы сняты с точностью около 1 пикселя; для иных пропорций окна выбран вписанный вид без выхода за обозначенный диапазон. Прежний baseline tooltip FAIL вне задачи.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .godot/hf109/{min,range,max}.png; .godot/hf109-{build,import,checks,local-import}*.log; .godot/hf109-overview.png и hf109-closeup.png.
### HF-108 — убрать экранную рамку

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимость HF-107 DONE; активных пересечений нет.
CP-HF-108 read-set: текущий фронт, scenes/trade_scene.tscn, scripts/Presentation/{ViewportFrame,RoomFramePresentation}.cs, tests/Integration/ProjectTest.ViewportFrame.cs, tests/ProjectTest.cs, docs/ASSET_AUDIT.md. Write-set: ORCHESTRATION_PLAN.md, scenes/trade_scene.tscn (только экранный CanvasLayer и ссылка на его script), tests/Integration/ProjectTest.ViewportFrame.cs, tests/ProjectTest.cs (только текст результата группы), docs/ASSET_AUDIT.md. Out of scope: рамки комнат, фоны, камера, экономика, source/, commit/push. Приёмка: экранного ViewportFrame нет, все шесть рамок комнат сохранены. Проверки: build, rendered --camera-frame-checks, diff-check; .godot/TEMP — evidence.

Handoff HF-108: REVIEW → DONE после приёмки координатором. Changed: только указанный write-set; экранный CanvasLayer удалён из сцены, room frame factory и шесть комнатных рамок сохранены. Checks: build PASS (0 warnings/errors); rendered --camera-frame-checks PASS (exit 0: отсутствие экранной рамки, шесть рамок и границы фонов); diff-check PASS. Acceptance: рамка только вокруг комнат. Read-set exception: .godot/hf104-check-dir.txt для изолированной копии. Risks/blocker: новых нет. Next: основной фронт S1-04. Evidence: .godot/hf108-{build,checks}*.log. Commit/push не выполнялись.
### HF-107 — соответствие фонам Drive и рамки комнат

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимости HF-105/HF-106 DONE, активных пересечений нет.
CP-HF-107 read-set: текущий фронт; два пользовательских Drive PNG (1sjSPaqW-r7SK6fTfjMtgECzW3yp-Aa0H и 1g0liMsb3C_KWP8y7p2euLR8v-SyK_kpa); art/rooms/{day_time.png,night_time.png,room_layout.jpg,atelier.jpg,music_room.jpg,showroom.jpg,cashier.jpg,upper_corridor.jpg}; РамкаПустая.PNG; scripts/Presentation/ViewportFrame.cs; scripts/Days/DayNightBackground.cs; scripts/UI/ZoomResponsiveUi.cs; scenes/trade_scene.tscn; tests/Integration/ProjectTest.ViewportFrame.cs; tests/ProjectTest.cs; docs/ASSET_AUDIT.md. Write-set: ORCHESTRATION_PLAN.md, docs/ASSET_AUDIT.md; scripts/Presentation/ViewportFrame.cs; scripts/Presentation/RoomFramePresentation.cs/.uid; scenes/trade_scene.tscn (только трансформы шести комнат и подключение рамок); tests/Integration/ProjectTest.ViewportFrame.cs. .godot/TEMP — скачивания, проверочная копия/evidence. Out of scope: исходный PNG, замена уже совпадающих day/night файлов, содержимое комнат/каталог/сохранения/экономика, source/, commit/push. Приёмка: SHA256 дневного/ночного фона равны Drive; фоны покрывают прежний размер мира, комнаты вписаны в шесть прямоугольников новой схемы вместо перекрытия фасада; экранная рамка тоньше вдвое, такие же прозрачные рамки на шести комнатах (включая закрытые), навигация и клики доступны. Проверки: SHA256, build, import, rendered --camera-frame-checks и --day-start-checks, визуальный обзор day/night и diff-check.

```text
Task: HF-107
Recommended status: REVIEW (принято координатором → DONE)
Changed: экранная рамка 0,2 вместо 0,4; общий border factory в ViewportFrame; RoomFramePresentation.cs/.uid — рамки шести комнат поверх занавесов, прозрачный центр и MouseFilter.Ignore; шесть scene-трансформов совпадают с проёмами новых day/night-схем; regression-проверки, аудит и план.
Checks: повторное скачивание двух Drive PNG и SHA256 comparison PASS; build PASS (0 warnings/errors); import изолированной копии и рабочего проекта PASS (exit 0); rendered --camera-frame-checks, --day-start-checks, --room-doorway-checks PASS (exit 0). Проверены все шесть границ комнат относительно пиксельных прямоугольников схемы, оба фоновых world bounds, прозрачный центр, half thickness, pan/zoom/resize/настоящий клик, покупка/reload/checkpoint/New Game, часы и ручной старт. Дневной и ночной панорамные снимки просмотрены; diff-check PASS.
Acceptance: day_time/night_time уже идентичны присланным файлам 4560x2565 и не менялись. Вместо старой привязки комнат к room_layout.jpg исправлены их position/scale под новую схему; камера сохраняет прежние bounds. Рамки экрана и всех комнат используют исходную РамкаПустая.PNG через runtime-копию без редактирования. Комнаты/персонажи остаются отдельным игровым передним планом, поэтому ночью содержимое комнат не заменяется чёрными прямоугольниками референса.
Read-set exception: .godot/hf104-check-dir.txt — повторное использование изолированной копии; source/ и пользовательские сохранения не читались и не менялись.
Risks/blocker: прежние duplicate UI UID warnings и baseline tooltip FAIL общего integration suite вне задачи.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .godot/hf107-{build,import,camera-frame,day-start,room-doorway,local-import}*.log; hf107-hashes.txt; hf107-day.png и hf107-night.png. Скачанные эталоны — .godot/hf107/.
### HF-106 — часы крупнее и ручной запуск дня

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимостей и активных пересечений нет.
CP-HF-106 read-set: текущий фронт, AGENTS.md; scripts/Days/{DayCycleController,DayNightBackground}.cs; scripts/UI/{DayAnnouncementOverlay,InventoryUI}.cs; scripts/TradeScene.cs (адаптеры/жизненный цикл), scripts/Persistence/TradeScene.Persistence.cs; src/ChikagoShop.Domain/{Days/DaySession,Persistence/ProgressData,Persistence/ProgressMigration,Persistence/ProgressJson}.cs; tests/ProjectTest.cs, tests/Integration/ProjectTest.{Days,Helpers,DayAnnouncements}.cs, tests/ChikagoShop.Domain.Tests/ProgressMigrationTests.cs; localization/en.tres, project.godot. Write-set: ORCHESTRATION_PLAN.md; scripts/Days/DayCycleController.cs; scripts/TradeScene.cs (только read-only forwarding свойства); scripts/Persistence/TradeScene.Persistence.cs; src/ChikagoShop.Domain/Persistence/ProgressData.cs; tests/ProjectTest.cs; tests/Integration/ProjectTest.DayStart.cs/.uid; tests/ChikagoShop.Domain.Tests/ProgressMigrationTests.cs; docs/GAMEPLAY.md (правило запуска). .godot/TEMP — evidence. Out of scope: новая доменная логика в TradeScene, прежние unrelated тестовые дефекты, баланс, source/, commit/push. Приёмка: часы +15% при обычном viewport, отступы 6 вместо 12, защита от пересечения инвентаря на узком экране; «Начать» на часах и в прежней дневной кнопке запускает день один раз; до старта время и новые покупатели стоят; каждый новый день/чекпоинт ждёт старта, старые сохранения продолжаются; состояние нового старта переживает reload; RU/EN. Проверки: build/domain, --day-start-checks (рендер/настоящий клик, reload, следующий день, новая игра), diff-check.

```text
Task: HF-106
Recommended status: REVIEW (принято координатором → DONE)
Changed: DayCycleController — часы 207 вместо 180, отступы 6 вместо 12, адаптация к узкому экрану и кнопка «Начать»; TradeScene — только forwarding DayStarted; persistence mapping/ProgressData — дополнительный day_started (default true для прежних сохранений), новые доменные и integration проверки, docs/GAMEPLAY и план.
Checks: build PASS (0 warnings/errors); domain PASS (132/132); isolated import PASS (exit 0); rendered --day-start-checks PASS (exit 0). Проверены настоящий клик, повторный старт без сброса, ожидание таймера/покупателей и приход покупателя после старта, reload обоих состояний, legacy save, следующий день после пропуска карточки, checkpoint, New Game, EN и narrow viewport с шестью слотами. Рендер до старта просмотрен; diff-check PASS.
Acceptance: до старта нового дня таймер и покупатели ждут; секунды заменяют «Начать» после клика; ручной старт есть и в прежней дневной кнопке; сохранения совместимы. Поле JSON добавлено совместимо без изменения версии 13, старые поля сохранены. Прежние незакоммиченные изменения рамки и исходное изображение сохранены.
Read-set exception: .godot/hf104-check-dir.txt для прежней изолированной копии, docs/GAMEPLAY.md для актуализации правила запуска. Пользовательские сохранения не затронуты.
Risks/blocker: ранее известный baseline tooltip FAIL общего suite не исправлялся; прежние интеграционные сценарии, предполагающие автоматический запуск, требуют перехода к явному Start при их следующей актуализации.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .godot/hf106-{build,import,checks}*.log; .godot/hf106-day-start.png.
### HF-105 — рамка по краям камеры

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13; зависимостей и пересечений с активными задачами нет.
CP-HF-105 read-set: текущий фронт, AGENTS.md, РамкаПустая.PNG, scripts/Camera/WorldCamera.cs, scripts/Presentation/MonochromePresentation.cs, scripts/UI/{PauseMenu,DayTransitionCard,EmbroideryGame,DayAnnouncementOverlay}.cs, scenes/trade_scene.tscn, tests/ProjectTest.cs, tests/Integration/ProjectTest.{Presentation,Camera}.cs, project.godot, .vscode/settings.json. Write-set: ORCHESTRATION_PLAN.md, art/ui/camera_frame.png и .import, scripts/Presentation/ViewportFrame.cs и .uid, scenes/trade_scene.tscn, tests/ProjectTest.cs, tests/Integration/ProjectTest.ViewportFrame.cs и .uid. TEMP/.godot — проверочные копии/evidence. Out of scope: исходное изображение, source/, экономика, сохранения игрока, движение/границы камеры, меню, commit/push. Приёмка: предоставленная рамка по краям игрового viewport, центр прозрачен, HUD/модальные окна сверху; бордюр неподвижен при pan/zoom и меняет размер с viewport, мышь не перехватывает. Проверки: build, импорт, rendered --camera-frame-checks с двумя размерами и настоящим кликом, снимок и diff-check.

```text
Task: HF-105
Recommended status: REVIEW (принято координатором → DONE)
Changed: art/ui/camera_frame.png/.import (копия исходника без изменения байтов), scripts/Presentation/ViewportFrame.cs/.uid, scenes/trade_scene.tscn, tests/ProjectTest.cs, tests/Integration/ProjectTest.ViewportFrame.cs/.uid, план.
Checks: build PASS (0 warnings/errors); import в изолированной копии и рабочем проекте PASS (exit 0); rendered --camera-frame-checks PASS (exit 0); git diff --check PASS. Снимки 1280x720 и 1000x800 просмотрены. Проверены видимые пиксели на четырёх краях, неизменность центра, неподвижность бордюра при pan/zoom, resize и настоящий клик сквозь рамку.
Acceptance: рамка привязана к viewport отдельным CanvasLayer ниже HUD и модальных окон; прозрачные поля исходника исключены AtlasTexture, центр не рисуется; nine-patch сохраняет углы и толщину при смене пропорций окна. Исходный РамкаПустая.PNG сохранён в корне без изменений.
Read-set exception: .godot/hf104-check-dir.txt прочитан для повторного использования уже изолированной проверочной копии; пользовательские сохранения не затронуты.
Risks/blocker: прежние duplicate UI UID warnings; общий integration suite имеет ранее подтверждённое baseline tooltip-падение и не перезапускался для этой локальной правки.
Next: основной фронт S1-04. Commit/push не выполнялись.
```

Evidence: .godot/hf105-{build,import,checks,local-import}*.log; предпросмотр .godot/hf105-camera-frame.png, оба исходных снимка в системном TEMP/chikago-hf67-camera-frame-*.png.
### HF-104 — уборка и публикация ассетов и занавеса

Статус: `DONE`; координатор/исполнитель `/root`; READY 2026-11-13. Зависимости HF-100 (ассеты), HF-101…HF-103 DONE; активных пересекающихся задач нет. Пользователь разрешил уборку, commit и push.
CP-HF-104 read-set: AGENTS.md, план, Git status/diff/refs; текущий набор изменённых scripts/scenes/tests/art, IMG_2363.PNG и .import, .gitignore, README.md, docs/ASSET_AUDIT.md, docs/TESTING.md, docs/archive/ORCHESTRATION_HISTORY_2026-11-12.md, tools/verify.ps1, project.godot, .vscode/settings.json. Write-set: ORCHESTRATION_PLAN.md, docs/archive/ORCHESTRATION_HISTORY_2026-11-13.md, README.md, docs/TESTING.md, docs/ASSET_AUDIT.md; перемещение только корневых IMG_2363.PNG и .import в .godot/hf104-original; Git index/commit/main и origin/main для проверенных изменений HF-100…HF-103. Изолированная проверочная копия в системном TEMP. Для baseline-диагностики дополнительно прочитаны tests/Integration/ProjectTest.Ui.cs и ProjectTest.Helpers.cs; их изменение не входит в задачу. Out of scope: source/, сохранения игрока, удаление сборок, новый gameplay, force push. Приёмка: один runtime-занавес в art/rooms, завершённые отчёты в архиве, актуальные ссылки и фронт; build/domain и targeted asset/room/resource/dialogue checks PASS; полный integration прогон проверен сравнением с baseline, прежнее падение явно задокументировано; обычный push, remote SHA совпадает с HEAD, рабочее дерево чистое.

```text
Task: HF-104
Recommended status: REVIEW (принято координатором → DONE после push)
Changed: завершённые отчёты перенесены в docs/archive/ORCHESTRATION_HISTORY_2026-11-13.md, основной фронт сокращён без изменения требований/ворот; README и docs/TESTING содержат ссылки на аудит и новые группы. Корневые IMG_2363.PNG/.import перемещены в .godot/hf104-original, runtime art/rooms/locked_curtain.png сохранён с тем же SHA256. Подготовлены к публикации прежние изменения HF-100 «ассеты» и HF-101…HF-103.
Checks: build PASS (0 warnings/errors); domain PASS (131/131); import PASS (exit 0; первичный импорт без кэша сообщил о ещё не импортированном шрифте); rendered --asset-replacement-checks, --room-doorway-checks, --resource-checks, --dialogue-localization-checks PASS (exit 0). Общий ProjectTest FAIL на TestUiLayoutInvalidation:61, Showing a tooltip must immediately activate its presentation updates. Тот же FAIL воспроизведён на исходных runtime/scenes/tests из HEAD=86f0019 в той же изолированной копии, при headless и rendered запуске текущих изменений. Полный suite не объявляется зелёным. Diff-check, локальные Markdown-ссылки и сохранность основной спецификации PASS. git push origin main PASS (86f0019..1353677); ls-remote подтвердил SHA 135367799d66c77b83120234a66dbeb5b7acc94a; рабочее дерево чистое после публикации.
Acceptance: единственный runtime-занавес без затемнения, готовые музыкальные PNG и прежние игровые identities; источники/сохранения игрока не читались и не менялись. Изменения опубликованы обычным push без переписывания истории.
Read-set exception: ProjectTest.Ui.cs/Helpers.cs прочитаны для необходимой диагностики существующего падения; для baseline в проверочной копии временно использованы версии изменённых scripts/scenes/tests из HEAD, затем восстановлены рабочие файлы.
Risks/blocker: существующее падение tooltip suite, прежние duplicate UI UID и shutdown resource warnings. Это не release gate; новые целевые проверки проходят.
Next: основной фронт S1-04; исправление прежнего tooltip FAIL требует отдельной задачи.
```

Evidence: .godot/hf104-verify.log, hf104-{full,assets,rooms,resources,dialogue,baseline}*.log; путь изолированной копии — .godot/hf104-check-dir.txt. Исходное фото сохранено в .godot/hf104-original/.

В HF-110 локальные evidence HF-102…HF-109 перенесены из .godot/ в .local/checks/<task>/ и .local/references/<task>/, оригиналы — в .local/originals/. Имена файлов сохранены; в исторических записях оставлены исходные пути.
