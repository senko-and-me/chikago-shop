# Сборка и проверки

[К README](../README.md)

Команды выполняются из корня проекта. `godot` должен указывать на .NET-версию Godot 4.7.2. Если он отсутствует в PATH, используйте полный путь к исполняемому файлу.

## Сборка и тесты домена

```powershell
dotnet build ChikagoShop.sln
dotnet test tests/ChikagoShop.Domain.Tests --no-build
```

## Проверки Godot

Сначала импортируйте ресурсы:

```powershell
godot --headless --editor --path . --import --quit
```

Интеграционные проверки создают игровые сцены и могут записывать сохранения и настройки. Запускайте их в отдельной копии проекта с изолированным `user://`. В этой копии создайте `override.cfg` (если его нет) со следующим содержимым:

```ini
[application]
config/use_custom_user_dir=true
config/custom_user_dir_name="ChikagoShopStartupSmokeMenuMusicCleanupDialogueLocalizationHF97"
```

Имя содержит ожидаемые тестами префиксы `ChikagoShopStartupSmoke` и `ChikagoShopStartupSmokeMenuMusic`. После проверки удалите только созданный для неё `override.cfg`; существующий пользовательский файл не перезаписывайте.

```powershell
godot --headless --path . --script res://tests/ProjectTest.cs
```

Для отдельной группы добавьте флаг после `--`:

```powershell
godot --headless --path . --script res://tests/ProjectTest.cs -- --resource-checks
godot --headless --path . --script res://tests/ProjectTest.cs -- --game-music-checks
```

| Группа | Флаг |
| --- | --- |
| Ресурсы, локализованные картинки и шрифт | `--resource-checks` |
| Музыкальные PNG, маски полировки, оплата, торговля и загрузка | `--asset-replacement-checks` |
| Занавесы и двери комнат: покупка, загрузка, checkpoint и новая игра | `--room-doorway-checks` |
| Сохранения и миграции | `--persistence-checks` |
| Баланс | `--balance-checks` |
| Таймер дня | `--day-timer-checks` |
| Покупки улучшений | `--upgrade-purchase-checks` |
| Полный перевод диалога RU/EN, переключение языка и музыкальные cues | `--dialogue-localization-checks` |
| Переход из вступления | `--intro-transition-checks` |
| Музыка меню / вступления / игры | `--menu-music-checks`, `--intro-music-checks`, `--game-music-checks` |
| Карточки / объявления дня | `--card-dealing-checks`, `--day-announcement-checks` |
| Палитра / кнопки | `--presentation-checks`, `--button-feedback-checks` |
| Язык в паузе / мишень вышивки | `--pause-localization-checks`, `--embroidery-target-checks` |

Проверки пикселей, палитры, кнопок, мишени вышивки и замены музыкальных ассетов требуют рендеринга: запускайте их без `--headless`. Снимки сохраняются в системный TEMP; `--asset-replacement-checks --asset-preview` сохраняет барабан в `user://drum-polishing.png` изолированной копии.

`tools/verify.ps1` запускает сборку, тесты домена, импорт и интеграционные проверки. Его также следует запускать в изолированной копии проекта:

```powershell
./tools/verify.ps1 -GodotPath "C:/path/to/godot.exe"
```

## Симуляция баланса

```powershell
dotnet run --project tools/ChikagoShop.Balance -- --runs 10000 --seed 20261107 --output docs/BALANCE_REPORT.md
```

Результаты: [BALANCE_REPORT.md](BALANCE_REPORT.md). Симуляция оценивает модель экономики и не заменяет ручное прохождение.

## Известное ограничение общего прогона (13.11.2026)

На текущем окружении общий ProjectTest останавливается в TestUiLayoutInvalidation на проверке активации подсказки инвентаря (ProjectTest.Ui.cs:61). Падение воспроизведено и на исходном main 86f0019, и с новыми ассетами, при headless и rendered запуске. Целевые группы asset-replacement, room-doorway, resource и dialogue-localization проходят отдельно; это не подтверждает прохождение полного release gate.
