using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChikagoShop.Domain.Dialogue;
using Godot;

public partial class ProjectTest
{
    private async Task TestDialogueLocalization()
    {
        Check(OS.GetUserDataDir().Contains("DialogueLocalizationHF97"),
            "Dialogue language checks require isolated settings and saves");
        string originalLocale = TranslationServer.GetLocale();
        Vector2I originalSize = Root.Size;
        byte[] settingsBefore = FileAccess.FileExists(GameLocalization.SettingsPath)
            ? FileAccess.GetFileAsBytes(GameLocalization.SettingsPath) : null;
        var original = DialogueSession.FromJson(FileAccess.GetFileAsString("res://dialogue/intro.ru.json"));
        var catalog = GD.Load<Translation>("res://localization/en.tres");
        var keys = catalog.GetMessageList().Select(key => key.ToString()).ToHashSet();
        var source = new List<DialogueLine>();
        while (!original.IsComplete) { source.Add(original.Current); original.Advance(); }
        Check(source.Count == 24, "All original speeches and actions must remain in the dialogue");
        foreach (var line in source)
        {
            Check(keys.Contains(line.Text) && !string.IsNullOrWhiteSpace(catalog.GetMessage(line.Text)) &&
                  !Regex.IsMatch(catalog.GetMessage(line.Text), "[А-Яа-яЁё]"),
                "Every complete speech and action must have a nonempty English translation");
            if (line.Speaker.Length > 0)
                Check(keys.Contains(line.Speaker), "Every character name must be localized");
        }

        MainMenu menu = null;
        DialoguePlayer player = null;
        try
        {
            TranslationServer.SetLocale("ru");
            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            await ToSignal(this, SignalName.ProcessFrame);
            menu.OnSettingsPressed();
            await ClickControl(menu, "SettingsMenuContainer/Settings/LanguageRow/EnglishButton");
            Check(GameLocalization.Language == "en", "The actual English settings button must select English");
            var settings = new ConfigFile();
            Check(settings.Load(GameLocalization.SettingsPath) == Error.Ok &&
                  settings.GetValue("interface", "language").AsString() == "en",
                "The menu's English choice must be saved");
            menu.OnBackPressed();
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(menu.ActiveDialogue.GetNode<Label>("Panel/Content/Speaker").Text == "Customer" &&
                  menu.ActiveDialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text.StartsWith("...Oh, you're quite right.") &&
                  menu.ActiveDialogue.GetNode<Label>("Heading").Text.Contains("Frankie's Fashion House"),
                "New Game after selecting English must open an English dialogue and title");
            while (menu.ActiveDialogue.Session.Index < 6) AdvanceMusicLine(menu.ActiveDialogue);
            Check(menu.GetNode<IntroMusicDirector>("MusicDirector").CurrentCue == "thoughtful" &&
                  !Regex.IsMatch(menu.ActiveDialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text, "[А-Яа-яЁё]"),
                "The English business speech must still trigger the thoughtful soundtrack");
            while (menu.ActiveDialogue.Session.Index < 14) AdvanceMusicLine(menu.ActiveDialogue);
            Check(menu.GetNode<IntroMusicDirector>("MusicDirector").CurrentCue == "sinister",
                "The English refusal speech must still trigger the sinister soundtrack");
            menu.ActiveDialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            menu.OnSettingsPressed();
            await ClickControl(menu, "SettingsMenuContainer/Settings/LanguageRow/RussianButton");
            Check(GameLocalization.Language == "ru", "The Russian settings button must restore Russian");
            menu.QueueFree(); menu = null;
            await ToSignal(this, SignalName.ProcessFrame);

            foreach (string language in new[] { "ru", "en" })
            foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                TranslationServer.SetLocale(language);
                Root.Size = size;
                player = new DialoguePlayer();
                var cues = new List<string>();
                player.MusicCueRequested += cues.Add;
                Root.AddChild(player);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                int completions = 0;
                player.Finished += () => completions++;
                for (int index = 0; index < source.Count; index++)
                {
                    Check(player.Session.Index == index && player.Session.Current == source[index],
                        "Localization must preserve the original line identity, order and musical cue");
                    string expected = GameLocalization.Text(source[index].Text);
                    var speaker = player.GetNode<Label>("Panel/Content/Speaker");
                    Check(speaker.Text == GameLocalization.Text(source[index].Speaker) &&
                          speaker.Visible == (source[index].Speaker.Length > 0),
                        "Names must translate and narration must keep no heading");
                    string reconstructed = "";
                    int pages = 0;
                    while (!player.Session.IsComplete && player.Session.Index == index)
                    {
                        Check(++pages < 100, "Localized pagination must make progress");
                        if (player.IsRevealing) player.Advance();
                        var text = player.GetNode<RichTextLabel>("Panel/Content/Text");
                        if (language == "en")
                            Check(!Regex.IsMatch(text.Text, "[А-Яа-яЁё]"), "English pages must contain no Russian fragments");
                        Check(text.GetThemeFontSize("normal_font_size") == 20 &&
                              (text.ScrollActive || text.GetContentHeight() <= text.Size.Y),
                            "Translated pages must fit or scroll without shrinking the project font");
                        var panel = player.GetNode<Control>("Panel");
                        Check(text.GlobalPosition.X >= panel.GlobalPosition.X + 112 &&
                              text.GetGlobalRect().End.X <= panel.GetGlobalRect().End.X - 112 &&
                              text.GetGlobalRect().End.Y <= panel.GetGlobalRect().End.Y - 64,
                            "Translation must preserve the cloud's internal padding");
                        reconstructed += text.Text;
                        player.Advance();
                    }
                    Check(reconstructed == expected, "All pages must reconstruct the full translated line without gaps or duplication");
                }
                Check(completions == 1 && cues.SequenceEqual(new[] { "thoughtful", "sinister" }),
                    "Each locale must finish once and trigger the same musical sequence once");
                player.QueueFree(); player = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }

            TranslationServer.SetLocale("ru");
            player = new DialoguePlayer();
            var liveCues = new List<string>();
            player.MusicCueRequested += liveCues.Add;
            Root.AddChild(player);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            while (player.Session.Index < 6) AdvanceMusicLine(player);
            player.Advance(); player.Advance();
            int liveIndex = player.Session.Index;
            int cuesBefore = liveCues.Count;
            TranslationServer.SetLocale("en");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(player.Session.Index == liveIndex && liveCues.Count == cuesBefore &&
                  player.GetNode<Label>("Panel/Content/Speaker").Text == "Castello" &&
                  player.GetNode<Label>("Heading").Text.Contains("Frankie's Fashion House") &&
                  player.GetNode<RichTextLabel>("Panel/Content/Text").Text.StartsWith("...Yes... well, I came on business, Frank."),
                "Live language change must translate the current line without advancing or repeating its music cue");
            Check(player.IsRevealing, "The translated line must restart its normal typewriter reveal");
            TranslationServer.SetLocale("ru");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(player.Session.Index == liveIndex && liveCues.Count == cuesBefore &&
                  player.GetNode<Label>("Panel/Content/Speaker").Text == "Кастелло" &&
                  player.GetNode<RichTextLabel>("Panel/Content/Text").Text.StartsWith("...да...это все так"),
                "Switching back must restore the original Russian line without changing progression");
            Check(GameLocalization.SelectLanguage("en") == Error.Ok, "English preference must save");
            TranslationServer.SetLocale("ru");
            GameLocalization.LoadPreference();
            Check(GameLocalization.Language == "en", "A saved English choice must apply after loading settings again");
        }
        finally
        {
            if (player != null && GodotObject.IsInstanceValid(player)) player.QueueFree();
            if (menu != null && GodotObject.IsInstanceValid(menu)) menu.QueueFree();
            Root.Size = originalSize;
            if (settingsBefore == null) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GameLocalization.SettingsPath));
            else
            {
                using var file = FileAccess.Open(GameLocalization.SettingsPath, FileAccess.ModeFlags.Write);
                file.StoreBuffer(settingsBefore);
            }
            TranslationServer.SetLocale(originalLocale);
        }
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        GD.Print("DIALOGUE_LOCALIZATION_CHECKS_PASS: all 24 lines/names/title, real settings buttons/persistence, RU/EN pages at two sizes, live switch without cue replay");
    }
}
