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
            foreach (string key in new[] { "camera_pan_done", "camera_zoom_done", "two_sales" })
                cfg.SetValue("tutorial", key, true);
            cfg.SetValue("tutorial", "finished", false);
            cfg.SetValue("tutorial", "camera_handoff_done", false);
            cfg.SetValue("tutorial", "room_focus_done", false);
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
            await Wait(() => tutorial.CurrentStep == FirstTradeTutorial.Step.RoomFocus, "Practice must ask for the atelier");
            var camera = game.GetNode<WorldCamera>("Camera2D");
            var atelier = game.GetNode<Control>("World/Atelier/RoomFrame");
            Check(tutorial.Target == atelier, "The spotlight must select the atelier frame");
            void DoubleClickRoom(Control frame)
            {
                Rect2 visible = ScreenBounds(frame).Intersection(Root.GetVisibleRect());
                Check(visible.HasArea(), "The room must have a visible practice target");
                Vector2 point = visible.Position + visible.Size * new Vector2(0.28f, 0.42f);
                MoveButtonPointer(point);
                Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                    ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true }, true);
                PressButtonPointer(point, false);
            }
            camera.ReturnToOverview(0);
            await Frame();
            DoubleClickRoom(game.GetNode<Control>("World/TradingRoom/RoomFrame"));
            await Wait(() => !camera.IsFocusing, "A different room focus must finish");
            Check(!tutorial.RoomFocusDone && tutorial.CurrentStep == FirstTradeTutorial.Step.RoomFocus,
                "Focusing another room must not complete training");
            camera.ReturnToOverview(0);
            await Frame();
            DoubleClickRoom(atelier);
            Check(tutorial.RoomFocusDone && camera.IsFocusing && tutorial.CurrentStep == FirstTradeTutorial.Step.RoomFocus,
                "The atelier double-click must be accepted while the lesson still waits for the flight");
            await Wait(() => tutorial.CurrentStep == FirstTradeTutorial.Step.Craft, "Crafting begins after the atelier flight");
            Vector2 atelierPosition = camera.Position;
            await Frame();
            Check(!camera.IsFocusing && camera.Position.IsEqualApprox(atelierPosition),
                "Entering crafting must not launch an additional camera flight");
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
            double fullWait = game.BuyerWaitInitial;
            game._Process(25);
            Check(game.BuyerWaitRemaining == fullWait, "Waiting near the first showcase must preserve the entire customer timer");
            var display = game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1");
            Vector2 displayPoint = ScreenBounds(display).GetCenter();
            Check(Root.GetVisibleRect().HasPoint(displayPoint), "The showcase checkpoint must bring the item into view");
            Vector2 inspectionCamera = camera.Position;
            MoveButtonPointer(displayPoint);
            await Frame();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Place && camera.Position.IsEqualApprox(inspectionCamera),
                "Hover must advance to item selection without moving the camera away");
            Check(game.ShowcasePopup.Visible && game.ShowcasePopup.ItemText.Contains("S"),
                "The real hover must reveal the quality tooltip over the spotlight");
            await ClickControl(game, "World/ShowroomLeft/Showcases/Slot1");
            await Frame();
            Check(!camera.Position.IsEqualApprox(inspectionCamera),
                "The camera must move to the buyer only after selecting the item.");
            game.Percentage = 30;
            game.RerollsRemaining = 1;
            game.ShowTrade();
            for (int i = 0; i < 3; i++)
            {
                game._Process(5);
                Check(game.BuyerWaitRemaining == fullWait, "Reading each sale instruction must preserve the first customer's timer");
                next.EmitSignal(Button.SignalName.Pressed);
                await Frame();
            }
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Reroll && game.TradeActive,
                "The first offer must wait for a real reroll");
            const string decline = "World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton";
            Check(tutorial.Target == game.GetNode<Button>(decline), "The reroll button must be highlighted");
            game._Process(15);
            Check(game.BuyerWaitRemaining == fullWait, "Reading the reroll instruction must hold the entire timer");
            game.AcceptTrade();
            Check(game.Accepted == 0, "The first sale must not bypass reroll practice");
            int initialOffer = game.OfferedPrice, initialCustomer = game.CustomerIndex;
            Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = true }, true);
            Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = false }, true);
            Check(game.RerollsRemaining == 1 && tutorial.CurrentStep == FirstTradeTutorial.Step.Reroll,
                "The old shortcut must not bypass the highlighted practice button");
            await ClickControl(game, decline);
            await Frame();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.TimedSale && game.TradeActive &&
                game.OfferedPrice > initialOffer && game.CustomerIndex != initialCustomer && game.RerollsRemaining == 0,
                "The only reroll must change the customer and improve the actual price");
            Check(game.BuyerWaitRemaining == fullWait, "The quick sale must start with full original patience");
            const string salePath = "World/TradingRoom/Item/TradePanel/Content/";
            var baseLabel = game.GetNode<Label>(salePath + "BasePriceLabel");
            var bonusLabel = game.GetNode<Label>(salePath + "PercentLabel");
            var totalLabel = game.GetNode<RichTextLabel>(salePath + "OfferLabel");
            string originalLanguage = GameLocalization.Language;
            int basePrice = game.GetItemPrice(game.ItemIndex);
            int speed = TradeScene.SpeedBonus(fullWait, fullWait);
            int quality = TradeScene.QualityBonus(game.ActiveTradeQualityRank);
            int bonus = speed + quality;
            foreach (string language in new[] { "ru", "en" })
            {
                TranslationServer.SetLocale(language);
                await Frame();
                bool en = language == "en";
                string percent = game.Percentage >= 0 ? $"+{game.Percentage}%" : $"{game.Percentage}%";
                string offered = en ? $"You get: ${game.OfferedPrice}" : $"Получим: {game.OfferedPrice}$";
                string tip = en ? $"+${bonus}" : $"+{bonus}$";
                Check(baseLabel.Text == (en ? $"Base price: ${basePrice}" : $"Обычно: {basePrice}$") &&
                    bonusLabel.Text == (en ? $"Price change: {percent}" : $"К цене: {percent}") &&
                    totalLabel.GetParsedText() == $"{offered} {tip}" &&
                    totalLabel.Text == $"{offered} [color=#268d48]{tip}[/color]",
                    "Only the separate localized speed and quality bonus must be green");
                Check(bonusLabel.TooltipText.Contains(en ? $"Speed: +${speed}" : $"Скорость: +{speed}$") &&
                    bonusLabel.TooltipText.Contains(en ? $"Quality: +${quality}" : $"Качество: +{quality}$"),
                    "The bonus details must show its speed and quality sources");
                using (await CapturePresentation("v144-sale-" + language)) { }
            }
            TranslationServer.SetLocale(originalLanguage);
            double dayTime = game.DayRemainingSeconds;
            game._Process(1);
            Check(game.BuyerWaitRemaining == fullWait - 1 && game.DayRemainingSeconds != dayTime,
                "The buyer and day must tick during the quick sale");
            double waitBeforeReload = game.BuyerWaitRemaining;
            game.PersistProgress();
            game.QueueFree();
            await Frame();
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            tutorial = game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
            tutorial.ProgressPath = profile;
            tutorial.CameraTravelSeconds = 0;
            Root.AddChild(game);
            game.SetProcess(false);
            mini = game.GetNode<EmbroideryGame>("EmbroideryGame");
            mini.SetProcess(false);
            next = tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done");
            await Frame();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.TimedSale && game.BuyerWaitRemaining == waitBeforeReload,
                "Reload must preserve the timed practice without resetting its initial duration");
            game._Process(100);
            Check(game.BuyerWaitRemaining == 1 && TradeScene.SpeedBonus(game.BuyerWaitRemaining, fullWait) > 0,
                "The first training sale must preserve a nonzero speed bonus");
            var lateBonusLine = game.GetNode<RichTextLabel>(salePath + "OfferLabel");
            Check(lateBonusLine.GetParsedText() == $"Получим: {game.OfferedPrice}$ +{quality + 1}$",
                "The green combined bonus must drop as the speed bonus changes");
            int balanceBeforeSale = game.Score;
            int expectedPayout = game.OfferedPrice + quality + 1;
            game.AcceptTrade(); await Frame();
            Check(game.Score == balanceBeforeSale + expectedPayout, "Formatting must leave the actual payout intact");
            Check(game.Accepted == 1 && game.ShowcaseItems[0] < 0, "The first training sale must free the showcase");
            await Hat();
            game.OnShowcasePressed(0);
            await Frame();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.LateSale && game.BuyerWaitRemaining is > 0 and <= 2,
                "Late-sale practice must start within two seconds of expiry");
            game._Process(2.1);
            Check(game.TradeActive && game.BuyerWaitRemaining == 0, "The second lesson must allow selling after the bonus expires");
            Check(game.GetNode<Label>(salePath + "PercentLabel").Text == "К цене: -50%" &&
                game.GetNode<RichTextLabel>(salePath + "OfferLabel").Text == $"Получим: {game.OfferedPrice}$",
                "Expired offers must show the original price change and omit the zero bonus");
            game.AcceptTrade(); await Frame();
            Check(game.Accepted == 2 && tutorial.CurrentStep == FirstTradeTutorial.Step.Summary,
                "Both practice sales must reach the quota lesson");
            var pack = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            Rect2 packTab = ScreenBounds(game.GetNode<Control>("HUD/Root/ProgressToggleButton")).Intersection(Root.GetVisibleRect());
            MoveButtonPointer(packTab.Position + new Vector2(2, packTab.Size.Y * 0.82f));
            MoveButtonPointer(packTab.Position + new Vector2(3, packTab.Size.Y * 0.83f));
            MoveButtonPointer(Root.Size / 2);
            await Wait(() => tutorial.CurrentStep == FirstTradeTutorial.Step.CloseTrainingDay,
                "A brief off-center hover must advance from the pack to the clock");
            Check(pack.Expanded && tutorial.Target.Name == "ClockActionButton", "Cigarettes remain readable while the clock is highlighted");
            game._Process(game.DayRemainingSeconds + 0.1);
            await Frame();
            Check(game.DayClosed && !game.DayTransitionOpen && tutorial.CurrentStep == FirstTradeTutorial.Step.EndDayExplanation,
                "Clock expiry must show the explanation before opening cards");
            next.EmitSignal(Button.SignalName.Pressed);
            await Wait(() => game.DayTransitionOpen, "Continuing from the day-end briefing must open the existing cards");
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
            MoveButtonPointer(new Vector2(tab.End.X - 2, tab.Position.Y + tab.Size.Y * 0.78f));
            MoveButtonPointer(Root.Size / 2);
            await Wait(() => tutorial.CurrentStep == FirstTradeTutorial.Step.NotebookContents, "A brief right-edge hover must reveal the notebook during training");
            Check(notebook.Expanded, "The notebook must stay open for the lesson after the cursor leaves");
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
