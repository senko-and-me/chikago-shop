using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestShowcaseCardVisibility()
    {
        const string path = "user://showcase_card_visibility.json";
        var packed = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        Vector2I oldSize = Root.Size;
        try
        {
            foreach (bool music in new[] { false, true })
            foreach (bool stocked in new[] { false, true })
            {
                Root.Size = music ? new Vector2I(960, 540) : new Vector2I(1280, 720);
                ProgressSaveStore.Save(path, new ProgressData {
                    SaveVersion = ProgressJson.CurrentVersion, Day = music ? 8 : 2, Score = 1000,
                    QuotaStage = music ? 1 : 0, QuotaVisit = music ? QuotaVisitStage.Rewarded : QuotaVisitStage.Unavailable,
                    MusicWorkshopUnlocked = music, UpgradePending = true,
                    ShowcaseItems = stocked ? new[] { 0, -1, -1, music ? 2 : -1, -1, -1 } : new[] { -1, -1, -1, -1, -1, -1 },
                    ShowcaseGrades = new[] { "S", "D", "D", "A", "D", "D" }
                });
                game = packed.Instantiate<TradeScene>();
                game.SavePath = path;
                game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
                Root.AddChild(game);
                game.SetProcess(false);
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.IsOpen && Paused, "The fixture must restore an actual pending card choice");
                int[] items = game.ShowcaseItems.ToArray();
                string grade = game.GetShowcaseQualityRank(0);
                void CheckDisplays()
                {
                    foreach (string room in new[] { "ShowroomLeft", "Showroom" })
                    {
                        bool visible = room == "ShowroomLeft" || music;
                        foreach (Control slot in game.GetNode("World/" + room + "/Showcases").GetChildren())
                        {
                            var table = slot.GetNode<TextureRect>("DisplayTable");
                            Check(table.IsVisibleInTree() == visible && slot.Modulate.A == 1 && table.Modulate.A == 1,
                                "Card transitions must retain tables and stock, while locked music displays remain hidden");
                            if (visible) Check(slot.GetNode<Label>("ItemLabel").IsVisibleInTree(), "Showcase labels must remain with their tables");
                        }
                    }
                    Check(game.ShowcaseItems.SequenceEqual(items) && game.GetShowcaseQualityRank(0) == grade,
                        "Displaying upgrade cards must not mutate stock or quality");
                }
                async Task WaitForCards()
                {
                    double deadline = Time.GetTicksMsec() + 6000;
                    while (card.IsAnimating)
                    {
                        Check(Time.GetTicksMsec() < deadline, "Card animation must finish");
                        CheckDisplays();
                        await ToSignal(this, SignalName.ProcessFrame);
                    }
                    CheckDisplays();
                }
                await WaitForCards();
                Check(!game.GetNode<Control>("HUD/Root").Visible && !game.GetNode<Control>("World/Atelier/GetItemButton").Visible,
                    "Other gameplay UI must still hide during the card choice");
                int money = game.Score;
                game.OnShowcasePressed(0);
                Check(!game.TradeActive && game.Score == money, "Visible showcases must remain inactive while the choice pauses gameplay");
                using (await CapturePresentation("showcases-with-cards-" + music + "-" + stocked)) { }
                if (stocked) card.Choose(card.Options.First());
                else card.Skip();
                await WaitForCards();
                Check(!card.IsOpen && !Paused && game.GetNode<Control>("HUD/Root").Visible,
                    "Skip and purchase must return to the normal gameplay UI");
                game.PersistProgress();
                var saved = ProgressSaveStore.Load(path, 3);
                Check(saved.ShowcaseItems.SequenceEqual(items), "Card choice must save the same displayed stock");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = packed.Instantiate<TradeScene>();
                game.SavePath = path;
                game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
                Root.AddChild(game);
                game.SetProcess(false);
                CheckDisplays();
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = null;
            }
        }
        finally
        {
            Paused = false;
            game?.QueueFree();
            Root.Size = oldSize;
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
