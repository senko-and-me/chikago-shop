using System;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestNotebookBuffs()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var notebook = game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
        var details = notebook.GetNode<Label>("Page/Notes/BuffDetails");
        var cards = game.GetNode<DayTransitionCard>("DayTransitionCard");
        notebook.SetProcess(false);
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        string Read() { notebook._Process(0); return details.Text; }
        try
        {
            foreach (string language in new[] { "ru", "en" })
            {
                TranslationServer.SetLocale(language);
                foreach (DayUpgrade selected in DayUpgradeRules.Available)
                {
                    // A restored selection uses the same display path as a newly purchased card.
                    game.DayUpgrades.Restore(false, selected, musicUnlocked: true);
                    string text = Read();
                    var termsForCard = game.DayUpgrades.Rules.For(selected);
                    string effect = selected == DayUpgrade.Cash
                        ? GameLocalization.Format("Получено {0}$ сразу.", termsForCard.Amount)
                        : DayTransitionCard.Describe(selected, termsForCard);
                    Check(text == "• " + effect,
                        $"Notebook must show only the marked {selected} effect in {language}");
                    if (selected == DayUpgrade.Cash)
                        Check(text.Contains(language == "en" ? "Received" : "Получено"), "Cash entry must describe an already received reward");
                    if (language == "en") Check(!System.Text.RegularExpressions.Regex.IsMatch(text, "[А-Яа-яЁё]"),
                        $"Notebook card {selected} must be fully translated");
                    int balance = game.Score;
                    Read(); Read();
                    Check(game.Score == balance, "Reading the notebook must never grant resources");
                    await Frame();
                    Check(details.Position.Y + details.Size.Y <= 301, "Full card effect must fit inside the authored notes area");
                }
                game.DayUpgrades.Reset();
                Check(Read() == (language == "en" ? "None" : "Нет"), "Reset must remove the previous day's card");
            }
            var terms = DayUpgradeRules.DefaultTerms();
            game.DayUpgrades.Rules = new DayUpgradeRules { Terms = terms };
            game.DayUpgrades.Restore(false, DayUpgrade.Materials);
            Read();
            terms[DayUpgrade.Materials] = new CardTerms(5, 17);
            Check(Read().Contains("17"), "Writing must follow actual runtime amounts even when selection is unchanged");

            // Exercise the real presenter -> controller -> paid purchase, then read its result.
            game.DayUpgrades.Rules = new DayUpgradeRules();
            for (int day = 2; ; day++)
            {
                Check(day < 100, "A deterministic Cash offer must exist");
                game.DayUpgrades.Offer(day);
                if (System.Linq.Enumerable.Contains(game.DayUpgrades.Options, DayUpgrade.Cash)) break;
            }
            Check(Read() == "None", "A pending offer must not display an unpurchased buff");
            game.Score = 1000;
            cards.Open(1, 2, game.DayUpgrades.Options, game.Score);
            await WaitForDayCardAnimation(cards);
            cards.Choose(DayUpgrade.Cash);
            ulong started = Time.GetTicksMsec();
            while (cards.IsOpen)
            {
                Check(Time.GetTicksMsec() - started < 7000, "Card purchase must complete");
                await Frame();
            }
            Check(game.Score == 1000 - game.DayUpgrades.CostFor(DayUpgrade.Cash) + game.DayUpgrades.CashReward,
                "Cash must be credited exactly once by the purchase");
            Check(Read().Contains("Received $15"), "A real purchase must appear in the notebook after the card screen closes");
            int receivedBalance = game.Score;
            game.DayUpgrades.Restore(false, DayUpgrade.Cash);
            Check(Read().Contains("Received") && game.Score == receivedBalance, "Restoring the Cash note must not pay a second reward");
            game.DayUpgrades.Offer(3);
            game.DayUpgrades.TrySkip();
            Check(Read() == "None", "Skipping the next day must clear the previous effect");
        }
        finally
        {
            cards.Close();
            Paused = false;
            game.QueueFree();
            await Frame();
        }
    }
}
