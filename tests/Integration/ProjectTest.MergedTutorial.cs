using System;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestMergedTutorial()
    {
        const string save = "user://merged_tutorial.json", profile = save + ".tutorial.cfg";
        TradeScene game = null;
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        async Task Wait(Func<bool> predicate, string message)
        {
            ulong deadline = Time.GetTicksMsec() + 7000;
            while (!predicate()) { Check(Time.GetTicksMsec() < deadline, message); await Frame(); }
        }
        try
        {
            var cfg = new ConfigFile();
            foreach (string key in new[] { "camera_pan_done", "camera_zoom_done", "room_focus_done", "camera_handoff_done", "two_sales" })
                cfg.SetValue("tutorial", key, true);
            cfg.SetValue("tutorial", "finished", false);
            cfg.Save(profile);
            ProgressSaveStore.Save(save, new ProgressData { SaveVersion = ProgressJson.CurrentVersion, Day = 1, DayStarted = true, Score = 220 });
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            var tutorial = game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
            tutorial.ProgressPath = profile;
            tutorial.CameraTravelSeconds = 0;
            Root.AddChild(game);
            game.SetProcess(false);
            var mini = game.GetNode<EmbroideryGame>("EmbroideryGame");
            mini.SetProcess(false);
            await Frame();
            await Wait(() => tutorial.CurrentStep == FirstTradeTutorial.Step.Craft, "Restored navigation practice must lead to the catalogue lesson");
            var next = tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done");
            async Task Hat()
            {
                game.GetItemFromAtelier();
                await Frame();
                Check(game.Crafting.IsCatalogOpen, "Tutorial must open the real catalogue");
                Check(game.Crafting.Select("hat"), "Tutorial must select the hat by its stable kind");
                await Frame();
                mini.Activate(); mini.FinishForTesting(true); mini.Activate();
                game._Process(0);
                await Frame();
            }
            await Hat();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Select && game.ShowcaseItems[0] == 0,
                "The completed hat must lead to inspecting its automatic showcase");
            game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").EmitSignal(Control.SignalName.MouseEntered);
            await Frame();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Place, "Inspection must lead to manual item selection");
            game.OnShowcasePressed(0);
            await Frame();
            for (int i = 0; i < 4; i++) { next.EmitSignal(Button.SignalName.Pressed); await Frame(); }
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.TimedSale && game.TradeActive,
                "The existing buyer and offer lessons must still introduce the timed sale");
            game._Process(100);
            Check(game.BuyerWaitRemaining == 1, "The first training sale must preserve the speed bonus opportunity");
            game.AcceptTrade(); await Frame();
            Check(game.Accepted == 1 && game.ShowcaseItems[0] < 0, "The first training sale must free the showcase");
            await Hat();
            game.OnShowcasePressed(0);
            await Frame();
            game._Process(100);
            Check(game.TradeActive && game.BuyerWaitRemaining == 0, "The second lesson must allow selling after the bonus expires");
            game.AcceptTrade(); await Frame();
            Check(game.Accepted == 2 && tutorial.CurrentStep == FirstTradeTutorial.Step.Summary,
                "Both practice sales must reach the quota lesson");
            Vector2 pack = ScreenBounds(game.GetNode<Control>("HUD/Root/ProgressToggleButton")).GetCenter();
            MoveButtonPointer(pack);
            await Wait(() => game.DayTransitionOpen, "Hovering the quota pack must open the practice upgrade choice");
            void CheckTrainingDisplays()
            {
                foreach (Control slot in game.GetNode("World/ShowroomLeft/Showcases").GetChildren())
                    Check(slot.GetNode<Control>("DisplayTable").IsVisibleInTree() && slot.Modulate.A == 1,
                        "Training must retain its display tables during dealing and card explanations");
            }
            ulong cardDeadline = Time.GetTicksMsec() + 6000;
            while (game.GetNode<DayTransitionCard>("DayTransitionCard").IsAnimating)
            {
                CheckTrainingDisplays();
                Check(Time.GetTicksMsec() < cardDeadline, "The training cards must finish dealing");
                await Frame();
            }
            await Frame();
            CheckTrainingDisplays();
            using (await CapturePresentation("training-card-showcases")) { }
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.CardExplanation, "The remaining upgrades need their existing explanation");
            next.EmitSignal(Button.SignalName.Pressed); await Frame();
            next.EmitSignal(Button.SignalName.Pressed); await Frame();
            var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Check(card.Options.Count == 1 && card.Options[0] == DayUpgrade.AtelierPrice,
                "Training must offer only the remaining applicable price card");
            card.Choose(DayUpgrade.AtelierPrice);
            await WaitForDayCardAnimation(game);
            await Frame();
            CheckTrainingDisplays();
            Check(game.DayUpgrades.Selected == DayUpgrade.AtelierPrice && tutorial.CurrentStep == FirstTradeTutorial.Step.Notebook,
                "The real purchase must continue to the notebook lesson");
            var notebook = game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
            Rect2 tab = notebook.GetGlobalTransformWithCanvas() * notebook.HoverBounds;
            MoveButtonPointer(new Vector2(tab.End.X - 1, tab.GetCenter().Y));
            await Wait(() => tutorial.CurrentStep == FirstTradeTutorial.Step.NotebookContents, "Right-edge hover must reveal the notebook during training");
            next.EmitSignal(Button.SignalName.Pressed); await Frame();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Summary, "Notebook must continue to pause/save information");
            next.EmitSignal(Button.SignalName.Pressed); await Frame();
            Check(!tutorial.IsActive && game.CurrentDay == 1 && !game.DayStarted && game.Score == 220 &&
                Array.TrueForAll(game.ShowcaseItems, item => item < 0), "Completing practice must reset the new showcase state for the real game");
            var stored = new ConfigFile(); stored.Load(profile);
            Check(stored.GetValue("tutorial", "finished", false).AsBool(), "Completion must persist");
            GD.Print("MERGED_TUTORIAL_PASS: catalogue, automatic display, inspection, both sales, price card, edge notebook and reset");
        }
        finally
        {
            Paused = false;
            game?.QueueFree();
            foreach (string file in new[] { save, profile }) if (FileAccess.FileExists(file)) DirAccess.RemoveAbsolute(file);
        }
        await Frame();
    }
}
