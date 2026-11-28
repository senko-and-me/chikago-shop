using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChikagoShop.Domain.Dialogue;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private static readonly Regex CyrillicUi = new("[А-Яа-яЁё]");

    private static void CheckEnglishUi(Node node)
    {
        if (node is Control control && control.IsVisibleInTree())
        {
            string source = node is Label label ? label.Text : node is RichTextLabel rich ? rich.Text
                : node is Button button ? button.Text : "";
            if (source != "Русский")
                Check(!CyrillicUi.IsMatch(control.Tr(source)), $"Untranslated visible text {node.GetPath()}: {source}");
            Check(!CyrillicUi.IsMatch(control.Tr(control.TooltipText)), $"Untranslated tooltip {node.GetPath()}");
        }
        foreach (Node child in node.GetChildren()) CheckEnglishUi(child);
    }

    private static void CheckSceneTranslations(string directory, Translation catalog)
    {
        foreach (string file in DirAccess.GetFilesAt(directory).Where(file => file.EndsWith(".tscn")))
        {
            string scene = FileAccess.GetFileAsString(directory + "/" + file);
            foreach (Match match in Regex.Matches(scene, "(?m)^(?:text|tooltip_text|placeholder_text) = (\"(?:[^\"\\\\]|\\\\.)*\")"))
            {
                string source = System.Text.Json.JsonSerializer.Deserialize<string>(match.Groups[1].Value);
                if (source == "Русский" || !CyrillicUi.IsMatch(source)) continue;
                Check(!string.IsNullOrEmpty(catalog.GetMessage(source)), $"Missing authored UI translation {directory}/{file}: {source}");
            }
        }
        foreach (string child in DirAccess.GetDirectoriesAt(directory)) CheckSceneTranslations(directory + "/" + child, catalog);
    }

    private async Task TestLocalizationCoverage()
    {
        Check(OS.GetUserDataDir().Contains("SC218"), "Localization checks require isolated saves/settings");
        string originalLocale = TranslationServer.GetLocale();
        Vector2I originalSize = Root.Size;
        TradeScene game = null;
        MainMenu menu = null;
        DayTransitionCard cards = null;
        try
        {
            var catalog = GD.Load<Translation>("res://localization/en.tres");
            foreach (string source in catalog.GetMessageList())
            {
                string translated = catalog.GetMessage(source);
                Check(translated.Length > 0 && !CyrillicUi.IsMatch(translated), "Incomplete English entry: " + source);
                string[] Tokens(string value) => Regex.Matches(value, @"\{(?:\d+|[a-z_]+)\}")
                    .Select(match => match.Value).OrderBy(value => value).ToArray();
                Check(Tokens(source).SequenceEqual(Tokens(translated)), "Translation changed arguments: " + source);
            }
            CheckSceneTranslations("res://scenes", catalog);
            foreach (string file in DirAccess.GetFilesAt("res://dialogue").Where(file => file.EndsWith(".ru.json") && file != "quota_notices.ru.json"))
            {
                var session = DialogueSession.FromJson(FileAccess.GetFileAsString("res://dialogue/" + file));
                while (!session.IsComplete)
                {
                    var line = session.Current;
                    Check(!string.IsNullOrEmpty(catalog.GetMessage(line.Text)), "Missing story translation: " + line.Text);
                    if (line.Speaker.Length > 0) Check(!string.IsNullOrEmpty(catalog.GetMessage(line.Speaker)), "Missing speaker translation");
                    session.Advance();
                }
            }
            TranslationServer.SetLocale("en");
            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            menu.OnSettingsPressed();
            await ToSignal(this, SignalName.ProcessFrame);
            CheckEnglishUi(menu);
            menu.QueueFree(); menu = null;
            await ToSignal(this, SignalName.ProcessFrame);

            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = "";
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            game.NewGame();
            game.Score = 10000;
            game.EndDay(); // The first clock action starts an unopened day.
            UnlockMusicForFixture(game);
            await ToSignal(this, SignalName.ProcessFrame);
            CheckEnglishUi(game);
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            CheckEnglishUi(game);
            pause.Close();

            foreach (string workshop in new[] { "atelier", "music" })
            {
                game.Crafting.OpenCatalog(workshop);
                Check(game.Crafting.IsCatalogOpen, "Catalog must open for localization checks");
                TranslationServer.SetLocale("ru");
                TranslationServer.SetLocale("en");
                await ToSignal(this, SignalName.ProcessFrame);
                CheckEnglishUi(game.Crafting.Catalog);
                string kind = WorkshopItemCatalog.Items.First(item => item.WorkshopId == workshop).Kind;
                Check(game.Crafting.Select(kind), "Selected preparation must open");
                var host = game.GetNode<EmbroideryGame>(workshop == "atelier" ? "EmbroideryGame" : "PolishingGame");
                await ToSignal(this, SignalName.ProcessFrame);
                CheckEnglishUi(host);
                host.Close();
                game.Crafting.Catalog.CloseForReset();
            }

            var notice = game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice");
            game.SecondQuota = 12345;
            notice.Open(game.FirstQuota, 0, true, false);
            int steps = 0;
            while (notice.ConversationIndex < 5 && steps++ < 100)
                notice._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
            Check(notice.ConversationIndex == 5, "Quota dialogue must reach its first amount reference");
            int index = notice.ConversationIndex;
            foreach (int amount in new[] { 12345, 8765 })
            foreach (string language in new[] { "ru", "en" })
            {
                game.SecondQuota = amount;
                TranslationServer.SetLocale(language);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(notice.ConversationIndex == index, "Changing language must not advance a quota story");
                Check(notice.GetNode<Label>("StoryRoot/Speech/Content/Message").Text.Contains(amount.ToString()),
                    "Actual quota story must display the runtime second quota in both languages");
                var session = DialogueSession.FromJson(FileAccess.GetFileAsString("res://dialogue/first_quota.ru.json"));
                int mentions = 0;
                while (!session.IsComplete)
                {
                    if (session.Current.Text.Contains("{second_quota}"))
                    {
                        string text = StoryDialogueLayout.LineText(session.Current.Text, game.SecondQuota);
                        Check(text.Contains(amount.ToString()) && !text.Contains("{second_quota}") && !text.Contains("60 000"), "Every mention must resolve its amount");
                        mentions++;
                    }
                    session.Advance();
                }
                Check(mentions == 3, "All three quota references must be parameterized");
                if (language == "en") CheckEnglishUi(notice);
            }
            notice.CloseSilently();
            TranslationServer.SetLocale("en");
            notice.Open(0, 0, false, true);
            Check(notice.GetNode<Label>("StoryRoot/Heading").Text == "DEFEAT", "Defeat heading must translate");
            notice.CloseSilently();

            cards = GD.Load<PackedScene>("res://scenes/UI/DayTransitionCard.tscn").Instantiate<DayTransitionCard>();
            game.AddChild(cards);
            int handIndex = 0;
            foreach (var hand in DayUpgradeRules.Available.Chunk(3))
            {
                var size = handIndex++ % 2 == 0 ? new Vector2I(1280, 720) : new Vector2I(960, 540);
                Root.Size = size;
                TranslationServer.SetLocale("ru");
                cards.Open(1, 2, new[] { DayUpgrade.Energy, hand[0] }.Concat(hand).ToArray(), 1000);
                Check(cards.IsOpen && cards.Options.SequenceEqual(hand),
                    "Even a stale hand must show only the three distinct active cards in order");
                foreach (var upgrade in hand)
                {
                    var face = cards.GetNode<Control>("Root/Center/Card/Content/Choices/" + upgrade + "/Content");
                    Check(face.GetNode<Label>("Description").Text.Length > 0 &&
                        face.GetNode<Button>("Choose").Text == GameLocalization.Format("Купить за {0}$", game.DayUpgrades.CostFor(upgrade)),
                        "Each card must display its configured effect and price");
                }
                TranslationServer.SetLocale("en");
                await WaitForDayCardAnimation(cards);
                await CapturePresentation("localization-cards-" + handIndex);
                CheckEnglishUi(cards);
                Check(cards.GetNode<Label>("Root/Center/Card/Content/Route").Text.Contains("Funds"), "Day route must refresh live");
                cards.Close();
            }
            var debug = Root.GetNode<DebugBalancePanel>("DebugBalance");
            debug.Open();
            TranslationServer.SetLocale("ru");
            TranslationServer.SetLocale("en");
            await ToSignal(this, SignalName.ProcessFrame);
            foreach (var field in DebugBalanceSettings.Fields)
                Check(!CyrillicUi.IsMatch(DebugBalancePanel.LocalizedFieldLabel(field)), "Debug field must translate: " + field.Key);
            var tabs = debug.GetNode<TabContainer>("Root/Inset/Panel/Content/Tabs");
            for (int tab = 0; tab < tabs.GetTabCount(); tab++)
            {
                tabs.CurrentTab = tab;
                await ToSignal(this, SignalName.ProcessFrame);
                CheckEnglishUi(debug);
                Check(!CyrillicUi.IsMatch(tabs.GetTabTitle(tab)), "Tab title must translate without changing node paths");
            }
            debug.Editor("start.cash").Text = "bad";
            debug.GetNode<Button>("Root/Inset/Panel/Content/Actions/Apply").EmitSignal(BaseButton.SignalName.Pressed);
            CheckEnglishUi(debug);
            debug.Editor("start.cash").Text = "-1";
            debug.GetNode<Button>("Root/Inset/Panel/Content/Actions/Apply").EmitSignal(BaseButton.SignalName.Pressed);
            CheckEnglishUi(debug);
            TranslationServer.SetLocale("ru");
            Check(debug.GetNode<Label>("Root/Inset/Panel/Content/Status").Text.StartsWith("Не применено:"), "Validation errors must refresh to Russian");
            TranslationServer.SetLocale("en");
            CheckEnglishUi(debug);
            debug.Close();
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").Restart();
            await ToSignal(this, SignalName.ProcessFrame);
            CheckEnglishUi(game);
        }
        finally
        {
            Root.GetNode<DebugBalancePanel>("DebugBalance").Close();
            cards?.Close();
            Paused = false;
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (GodotObject.IsInstanceValid(menu)) menu.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(originalLocale);
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }
}
