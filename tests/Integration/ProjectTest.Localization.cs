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
                var sequence = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
                sequence.DialoguePath = sequencePath;
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
            var fittingDialogue = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
            fittingDialogue.DialoguePath = fittingPath;
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
            var dialogue = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();
            dialogue.DialoguePath = path;
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
