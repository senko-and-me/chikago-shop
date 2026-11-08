using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestPauseLocalization()
    {
        const string testSave = "user://pause_localization_test.json";
        const string menuPath = "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/";
        const string settingsPath = "PauseMenu/Root/Overlay/Center/Panel/Content/Settings/";
        bool hadSettings = FileAccess.FileExists(GameLocalization.SettingsPath);
        byte[] settingsBackup = hadSettings ? FileAccess.GetFileAsBytes(GameLocalization.SettingsPath) : null;
        Vector2I originalSize = Root.Size;
        string originalLanguage = GameLocalization.Language;
        TradeScene game = null;
        MainMenu mainMenu = null;
        try
        {
            var preserved = new ConfigFile();
            preserved.Load(GameLocalization.SettingsPath);
            preserved.SetValue("audio", "volume", 0.375);
            preserved.Save(GameLocalization.SettingsPath);
            GameLocalization.SelectLanguage("ru");
            if (FileAccess.FileExists(testSave)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(testSave));
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = testSave;
            Root.AddChild(game);
            game.SetProcess(false);
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            await ClickControl(game, menuPath + "SettingsButton");
            string saveBefore = FileAccess.GetFileAsString(testSave);
            int scoreBefore = game.Score;
            double timeBefore = game.DayRemainingSeconds;

            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "en", "ru" })
            {
                Root.Size = size;
                string name = language == "en" ? "EnglishButton" : "RussianButton";
                await ClickControl(game, settingsPath + "LanguageRow/" + name);
                var russian = game.GetNode<Button>(settingsPath + "LanguageRow/RussianButton");
                var english = game.GetNode<Button>(settingsPath + "LanguageRow/EnglishButton");
                Check(Paused && GameLocalization.Language == language &&
                    russian.ButtonPressed == (language == "ru") && english.ButtonPressed == (language == "en"),
                    "Pause language buttons must switch immediately and select exactly the current language");
                Check(russian.Text == "Русский" && english.Text == "English",
                    "Language names must remain recognizable in their own languages");
                var label = game.GetNode<Label>(settingsPath + "LanguageRow/LanguageLabel");
                var back = game.GetNode<Button>(settingsPath + "BackButton");
                Check(label.Tr(label.Text) == (language == "en" ? "Language" : "Язык") &&
                    back.Tr(back.Text) == (language == "en" ? "Back" : "Назад"),
                    "Pause settings must translate while they remain open");
                Check(game.GetNode<Label>("HUD/Root/ScorePanel/ScoreLabel").Text ==
                    (language == "en" ? $"Cash: ${game.Score}" : $"Счёт: {game.Score}$"),
                    "The paused game's dynamic HUD must update to the chosen language");
                foreach (string path in new[] { "FullscreenButton", "LanguageRow/LanguageLabel", "LanguageRow/RussianButton",
                    "LanguageRow/EnglishButton", "BackButton" })
                    Check(Root.GetVisibleRect().Encloses(game.GetNode<Control>(settingsPath + path).GetGlobalRect()),
                        "Pause language controls must fit both supported viewport sizes");
                Check(!russian.GetGlobalRect().Intersects(english.GetGlobalRect()), "Language buttons must not overlap");
                Check(game.Score == scoreBefore && game.DayRemainingSeconds == timeBefore &&
                    FileAccess.GetFileAsString(testSave) == saveBefore,
                    "Language selection must not change the game state or rewrite gameplay progress");
                var stored = new ConfigFile();
                Check(stored.Load(GameLocalization.SettingsPath) == Error.Ok &&
                    stored.GetValue("interface", "language").AsString() == language &&
                    stored.GetValue("audio", "volume").AsDouble() == 0.375,
                    "Language preference must persist while preserving other settings");
                if (size.X == 1280)
                {
                    using var image = await CapturePresentation();
                    image.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chikago-hf70-pause-{language}.png"));
                }
            }
            await ClickControl(game, settingsPath + "LanguageRow/EnglishButton");
            pause.Close();
            TranslationServer.SetLocale("ru");
            GameLocalization.LoadPreference();
            pause.Open();
            await ClickControl(game, menuPath + "SettingsButton");
            Check(game.GetNode<Button>(settingsPath + "LanguageRow/EnglishButton").ButtonPressed,
                "Reopened pause settings must reflect the saved language");
            pause.Close();
            game.QueueFree();
            game = null;
            await ToSignal(this, SignalName.ProcessFrame);
            mainMenu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(mainMenu);
            Check(mainMenu.GetNode<Button>("SettingsMenuContainer/Settings/LanguageRow/EnglishButton").ButtonPressed,
                "The main menu must share the preference selected in pause settings");
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (mainMenu != null && GodotObject.IsInstanceValid(mainMenu)) mainMenu.QueueFree();
            if (hadSettings)
            {
                using var settings = FileAccess.Open(GameLocalization.SettingsPath, FileAccess.ModeFlags.Write);
                settings.StoreBuffer(settingsBackup);
            }
            else if (FileAccess.FileExists(GameLocalization.SettingsPath))
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GameLocalization.SettingsPath));
            if (FileAccess.FileExists(testSave)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(testSave));
            TranslationServer.SetLocale(originalLanguage);
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestLocalization()
    {
        const string preferenceTest = "user://language_preference_test.cfg";
        bool hadSettings = FileAccess.FileExists(GameLocalization.SettingsPath);
        byte[] settingsBackup = hadSettings ? FileAccess.GetFileAsBytes(GameLocalization.SettingsPath) : null;
        Vector2I windowSize = Root.Size;
        MainMenu menu = null;
        TradeScene game = null;
        try
        {
            if (FileAccess.FileExists(preferenceTest)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(preferenceTest));
            GameLocalization.LoadPreference(preferenceTest);
            Check(GameLocalization.Language == "ru", "Missing language settings must default to Russian");
            var preservedSettings = new ConfigFile();
            preservedSettings.SetValue("audio", "volume", 0.5);
            Check(preservedSettings.Save(preferenceTest) == Error.Ok, "Test settings must save");
            Check(GameLocalization.SelectLanguage("en", preferenceTest) == Error.Ok, "English preference must save");
            preservedSettings.Load(preferenceTest);
            Check(preservedSettings.GetValue("audio", "volume").AsDouble() == 0.5,
                "Changing language must preserve other preference sections");
            TranslationServer.SetLocale("ru");
            GameLocalization.LoadPreference(preferenceTest);
            Check(GameLocalization.Language == "en", "Loading settings must restore the saved language");
            Check(GameLocalization.SelectLanguage("fr", preferenceTest) == Error.InvalidParameter && GameLocalization.Language == "en",
                "Unsupported language selection must leave the language unchanged");
            using (var invalid = FileAccess.Open(preferenceTest, FileAccess.ModeFlags.Write))
                invalid.StoreString("[interface]\nlanguage=\"unsupported\"\n");
            GameLocalization.LoadPreference(preferenceTest);
            Check(GameLocalization.Language == "ru", "Unsupported stored language must fall back to Russian");

            var catalog = GD.Load<Translation>("res://localization/en.tres");
            foreach (string source in catalog.GetMessageList())
            {
                string translated = catalog.GetMessage(source).ToString();
                Check(translated.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(translated, "[А-Яа-яЁё]"),
                    $"English catalog entry must be complete: {source}");
                var sourceArgs = System.Text.RegularExpressions.Regex.Matches(source, @"\{\d+\}").Select(match => match.Value).OrderBy(value => value);
                var translatedArgs = System.Text.RegularExpressions.Regex.Matches(translated, @"\{\d+\}").Select(match => match.Value).OrderBy(value => value);
                Check(sourceArgs.SequenceEqual(translatedArgs), $"Format arguments must match: {source}");
            }

            var menuScene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
            menu = menuScene.Instantiate<MainMenu>();
            Root.AddChild(menu);
            menu.OnSettingsPressed();
            const string settings = "SettingsMenuContainer/Settings/";
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                Root.Size = size;
                foreach (string language in new[] { "en", "ru" })
                {
                    string button = language == "en" ? "EnglishButton" : "RussianButton";
                    await ClickControl(menu, settings + "LanguageRow/" + button);
                    Check(GameLocalization.Language == language && menu.GetNode<Button>(settings + "LanguageRow/" + button).ButtonPressed,
                        "Language buttons must switch immediately and mark the selected language");
                    var title = menu.GetNode<Label>(settings + "SettingsTitle");
                    Check(title.Tr(title.Text) == (language == "en" ? "SETTINGS" : "НАСТРОЙКИ"),
                        "The visible settings title must translate without reopening the menu");
                    foreach (string path in new[] { "LanguageRow/RussianButton", "LanguageRow/EnglishButton", "FullscreenButton", "BackButton" })
                    {
                        Rect2 bounds = menu.GetNode<Control>(settings + path).GetGlobalRect();
                        Check(new Rect2(Vector2.Zero, Root.GetVisibleRect().Size).Encloses(bounds),
                            $"Settings controls must fit both window sizes and languages: {path}");
                    }
                }
            }
            await ClickControl(menu, settings + "LanguageRow/EnglishButton");
            TranslationServer.SetLocale("ru");
            GameLocalization.LoadPreference();
            Check(GameLocalization.Language == "en", "The actual GUI selection must survive preference reload");
            menu.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            menu = menuScene.Instantiate<MainMenu>();
            Root.AddChild(menu);
            Check(menu.GetNode<Button>(settings + "LanguageRow/EnglishButton").ButtonPressed,
                "A recreated menu must retain the selected language");
            menu.Hide();

            var gameScene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
            game = await CreateGame(gameScene);
            game.NewGame();
            CraftAtelier(game);
            Check(game.GetNode<Label>("HUD/Root/ScorePanel/ScoreLabel").Text == "Cash: $130" &&
                  game.GetNode<Label>("HUD/Root/ProgressPanel/Content/DayLabel").Text == "Day: 1 / 7",
                "Dynamic cash and day labels must use English with their actual values");
            Check(game.GetNode<Label>("HUD/Root/ProgressPanel/Content/StatusLabel").Text == "New game.",
                "Saved Russian status messages must display in English");
            var slot = (SlotUI)game.GetNode<InventoryUI>("HUD/InventoryUI").Container.GetChild(0);
            Check(slot.ItemInfo.Contains("Rarity:") && slot.ItemInfo.Contains("Value: $") &&
                  !System.Text.RegularExpressions.Regex.IsMatch(slot.ItemInfo, "[А-Яа-яЁё]"),
                "Inventory tooltip names, rarity and value must all translate");
            var heading = game.GetNode<RarityPanel>("World/Atelier/RarityPanel").Values.GetNode<Label>("Heading0");
            Check(heading.Tr(heading.Text) == "Rarity", "Rarity table headings must translate");
            SelectInventorySlot(game, 0);
            game.PlaceItemInShowcase(0);
            Check(!System.Text.RegularExpressions.Regex.IsMatch(game.GetNode<Label>("World/ShowroomLeft/Showcases/Slot1/ItemLabel").Text, "[А-Яа-яЁё]"),
                "An occupied showcase must display the localized item name");
            game._Process(game.BuyerCountdown + 0.01);
            Check(game.GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/OfferLabel").Text == $"You get: ${game.OfferedPrice}",
                "Offer text must translate while retaining the actual offer value");
            string saveBefore = FileAccess.GetFileAsString(SavePath);
            int score = game.Score;
            int item = game.ItemIndex;
            TranslationServer.SetLocale("ru");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.GetNode<Label>("HUD/Root/ScorePanel/ScoreLabel").Text == $"Счёт: {score}$" &&
                  game.GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/OfferLabel").Text == $"Получим: {game.OfferedPrice}$",
                "Switching back to Russian must refresh dynamic HUD and trade text");
            TranslationServer.SetLocale("en");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.Score == score && game.ItemIndex == item && game.TradeActive && FileAccess.GetFileAsString(SavePath) == saveBefore,
                "Changing language must not mutate gameplay or rewrite the save");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            var resume = game.GetNode<Button>("PauseMenu/Root/Overlay/Center/Panel/Content/Menu/ResumeButton");
            Check(resume.Tr(resume.Text) == "Resume", "Pause controls must use the selected language");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = await CreateGame(gameScene);
            Check(game.TradeActive && game.ItemIndex == item &&
                  game.GetNode<Label>("HUD/Root/ProgressPanel/Content/StatusLabel").Text == "New game.",
                "Loading a gameplay save in English must preserve the trade and translate its Russian status");
            Check(GameLocalization.Status("Не хватает 17$ на квоту. Вернитесь к контрольной точке.") ==
                  "You need $17 more for the quota. Return to the checkpoint.",
                "Legacy interpolated quota messages must retain their saved amount in English");
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (menu != null && GodotObject.IsInstanceValid(menu)) menu.QueueFree();
            if (hadSettings)
            {
                using var settings = FileAccess.Open(GameLocalization.SettingsPath, FileAccess.ModeFlags.Write);
                settings.StoreBuffer(settingsBackup);
            }
            else if (FileAccess.FileExists(GameLocalization.SettingsPath))
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GameLocalization.SettingsPath));
            if (FileAccess.FileExists(preferenceTest)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(preferenceTest));
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            Root.Size = windowSize;
            TranslationServer.SetLocale("ru");
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Russian/English UI, settings persistence, catalog, layout and save compatibility checks passed");
    }

    private async Task TestLongSentenceScroll()
    {
        Vector2I originalViewport = Root.Size;
        const string sequencePath = "user://dialogue_scroll_sequence_test.json";
        var examples = new[]
        {
            new { speaker = "Кастелло", text = "Знаешь же, мы стали работать по всему миру, последняя поставка в Великобританию на практически 11 тонн прошла все проверки и мы купаемся в золоте." },
            new { speaker = "Френки", text = "Здесь 150 лет назад шили хлопком наших плантаций, а когда всех негров распустили, мы всей семьёй кредитами выбивали новые земли, скупали шёлк, лён, кожу животных." },
            new { speaker = "Кастелло", text = "Проиграешь - я вышибу твои мозги и выкуплю всю мастерскую по половинной стоимости рынка, а твои дети извинятся перед всей моей семьёй за личное оскорбление в мою сторону." }
        };
        using (var file = FileAccess.Open(sequencePath, FileAccess.ModeFlags.Write))
            file.StoreString(System.Text.Json.JsonSerializer.Serialize(examples));
        try
        {
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1096, 616), new Vector2I(1280, 720), new Vector2I(1600, 900) })
            {
                Root.Size = size;
                var sequence = new DialoguePlayer { DialoguePath = sequencePath };
                Root.AddChild(sequence);
                try
                {
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    foreach (var example in examples)
                    {
                        if (sequence.IsRevealing) sequence.Advance();
                        var label = sequence.GetNode<RichTextLabel>("Panel/Content/Text");
                        Check(label.Text == example.text, "Screenshot examples must remain whole");
                        if (label.GetLineCount() <= 3)
                            Check(!label.ScrollActive && label.GetContentHeight() <= label.Size.Y,
                                $"Screenshot dialogue must fit three lines at {size}: {label.GetContentHeight()}/{label.Size.Y}");
                        Root.Size = size + new Vector2I(8, 8);
                        await ToSignal(this, SignalName.ProcessFrame);
                        Root.Size = size;
                        await ToSignal(this, SignalName.ProcessFrame);
                        await ToSignal(this, SignalName.ProcessFrame);
                        if (sequence.IsRevealing) sequence.Advance();
                        if (label.GetLineCount() <= 3)
                            Check(!label.ScrollActive && !label.GetVScrollBar().IsVisibleInTree(),
                                "Resizing must not leave a scrollbar on a three-line page");
                        sequence.Advance();
                    }
                }
                finally { sequence.QueueFree(); }
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        finally
        {
            Root.Size = originalViewport;
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(sequencePath));
        }
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
        {
            Root.Size = size;
            const string fittingPath = "user://fitting_sentence_scroll_test.json";
            string fittingText = size.X == 1280
                ? "Знаешь же, мы стали работать по всему миру, последняя поставка в Великобританию на практически 11 тонн прошла все проверки и мы купаемся в золоте."
                : "Первая строка\nВторая строка\nТретья строка.";
            using (var file = FileAccess.Open(fittingPath, FileAccess.ModeFlags.Write))
                file.StoreString(System.Text.Json.JsonSerializer.Serialize(new[] { new { speaker = "Кастелло", text = fittingText } }));
            var fittingDialogue = new DialoguePlayer { DialoguePath = fittingPath };
            Root.AddChild(fittingDialogue);
            try
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                fittingDialogue.Advance();
                var fittingLabel = fittingDialogue.GetNode<RichTextLabel>("Panel/Content/Text");
                Check(fittingLabel.GetLineCount() == 3 && fittingLabel.GetContentHeight() <= fittingLabel.Size.Y &&
                      !fittingLabel.ScrollActive && !fittingLabel.GetVScrollBar().IsVisibleInTree(),
                    $"Three fitting lines must not enable scrolling or reserve scrollbar width: viewport={size}, lines={fittingLabel.GetLineCount()}, height={fittingLabel.GetContentHeight()}/{fittingLabel.Size.Y}, active={fittingLabel.ScrollActive}, visible={fittingLabel.GetVScrollBar().IsVisibleInTree()}");
            }
            finally
            {
                fittingDialogue.QueueFree();
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(fittingPath));
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
        {
            Root.Size = size;
            const string path = "user://long_sentence_scroll_test.json";
            string sentence = string.Join(" ", Enumerable.Repeat("Длинное предложение для проверки прокрутки", 20)) + ".";
            using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
                file.StoreString(System.Text.Json.JsonSerializer.Serialize(new[] { new { speaker = "Френки", text = sentence } }));
            var dialogue = new DialoguePlayer { DialoguePath = path };
            Root.AddChild(dialogue);
            try
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                dialogue.Advance();
                var text = dialogue.GetNode<RichTextLabel>("Panel/Content/Text");
                await ToSignal(this, SignalName.ProcessFrame);
                var bar = text.GetVScrollBar();
                Check(text.Text == sentence && text.ScrollActive && text.GetThemeFontSize("normal_font_size") == 20 &&
                      bar.IsVisibleInTree() && bar.MaxValue > bar.Page,
                    "A very long sentence must stay whole with the normal font and a usable scrollbar");
                dialogue._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left,
                    Position = bar.GetGlobalRect().GetCenter() });
                Check(!dialogue.Session.IsComplete, "Scrollbar interaction must not finish the dialogue");
                bar.Value = bar.MaxValue;
                Check(bar.Value > 0, "The end of the sentence must be reachable");
            }
            finally
            {
                dialogue.QueueFree();
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        Root.Size = originalViewport;
    }
}
