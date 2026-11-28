using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestTimedTradesAndShadows()
    {
        const string save = "user://timed_trades_and_shadows.json";
        const string barPath = "World/TradingRoom/BuyerWaitingThought/ThoughtBubble/Stack/RedWaitingBar";
        const string tradeButtons = "World/TradingRoom/Item/TradePanel/Content/Buttons/";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        var originalSize = Root.Size;
        TradeScene game = null;
        MainMenu menu = null;

        async Task Reload(ProgressData data = null)
        {
            if (game != null)
            {
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            if (data != null) ProgressSaveStore.Save(save, data);
            game = scene.Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }

        ProgressData Stock(bool music)
        {
            int[] items = [-1, -1, -1, -1, -1, -1];
            items[music ? 3 : 0] = music ? 2 : 0;
            return new ProgressData
            {
                SaveVersion = ProgressJson.CurrentVersion, Day = 8, QuotaStage = 1,
                QuotaVisit = QuotaVisitStage.Rewarded, DayStarted = true, DayRemainingSeconds = 59,
                Score = 2000, EnergyRemaining = 6, CheckpointEnergy = 6, MusicWorkshopUnlocked = true,
                CheckpointDay = 8, CheckpointQuotaVisit = QuotaVisitStage.Rewarded,
                CheckpointMusicWorkshopUnlocked = true, CheckpointScore = 2000, ShowcaseItems = items,
                CheckpointShowcaseItems = items, WaitingBuyerPresent = true, WaitingBuyerMusic = music,
                WaitingBuyerItemKind = music ? "guitar" : "hat",
                CustomerIndex = 0, BuyerWaitInitial = music ? 24 : 12, BuyerWaitRemaining = music ? 21 : 11
            };
        }

        try
        {
            foreach (double initial in new[] { 10d, 12d, 15d, 20d, 25d })
            {
                Check(TradeScene.SpeedBonus(initial, initial) == 5 &&
                    TradeScene.SpeedBonus(initial * 0.8, initial) == 4 &&
                    TradeScene.SpeedBonus(initial * 0.4, initial) == 2 &&
                    TradeScene.SpeedBonus(0.01, initial) == 1 &&
                    TradeScene.SpeedBonus(0, initial) == 0, "Speed bonus thresholds must be proportional for every timer");
            }

            var atelierTimes = new HashSet<double>();
            var musicTimes = new HashSet<double>();
            for (int i = 0; i < 32; i++)
            {
                var fixture = Stock(false);
                fixture.WaitingBuyerPresent = false;
                fixture.ShowcaseItems = [];
                await Reload(fixture);
                game._Process(0);
                var saved = ProgressSaveStore.Load(save, 6);
                Check(saved.WaitingBuyerPresent && saved.BuyerWaitRemaining == saved.BuyerWaitInitial,
                    "Every new buyer must save the initially rolled duration");
                double time = game.BuyerWaitInitial;
                Check(time is >= 15 and <= 20, "New buyers in both categories must use the same inclusive range");
                (saved.WaitingBuyerMusic ? musicTimes : atelierTimes).Add(time);
            }
            Check(atelierTimes.Count > 1 && musicTimes.Count > 1, "Both request categories must have varying initial wait times");

            foreach (var (size, music) in new[] { (new Vector2I(1280,720), false), (new Vector2I(1920,1080), true) })
            {
                Root.Size = size;
                var data = Stock(music);
                await Reload(data);
                game._Process(0);
                game.OnShowcasePressed(music ? 3 : 0);
                string slot = music ? "World/Showroom/Showcases/Slot4" : "World/ShowroomLeft/Showcases/Slot1";
                Rect2 ShadowGeometry()
                {
                    var icon = game.GetNode<TextureRect>(slot + "/Icon");
                    var shadow = game.GetNode<TextureRect>(slot + "/TradeShadow");
                    Check(game.TradeActive && icon.Texture == null && shadow.Visible && shadow.Texture != null &&
                        shadow.Material is ShaderMaterial && shadow.MouseFilter == Control.MouseFilterEnum.Ignore,
                        "Only the noninteractive ink shadow must occupy the source slot during a sale");
                    Check(shadow.GetRect().GetCenter().IsEqualApprox(icon.GetRect().GetCenter() + new Vector2(7, 6)) &&
                        Mathf.IsZeroApprox(shadow.Rotation) && ((ShaderMaterial)shadow.Material).Shader.ResourcePath ==
                        "res://shaders/showcase_item_shadow.gdshader",
                        "A sale must keep the previous item's shadow pose, offset and material");
                    if (music)
                    {
                        var atlas = (AtlasTexture)shadow.Texture;
                        float oldScale = 1.35f * Mathf.Min(209f / atlas.Atlas.GetWidth(), 115f / atlas.Atlas.GetHeight());
                        Check(shadow.Size.IsEqualApprox(atlas.GetSize() * oldScale),
                            "Changing the instrument pose must keep its sale shadow at the previous scale");
                    }
                    return shadow.GetRect();
                }

                Rect2 geometry = ShadowGeometry();
                var camera = game.GetNode<WorldCamera>("Camera2D");
                var stand = game.GetNode<Control>(slot);
                camera.FocusOn(stand.GetGlobalRect().GetCenter(), camera.CloseupSize, 0);
                using (await CapturePresentation($"showcase-trade-shadow-{size.X}")) { }

                var bar = game.GetNode<ProgressBar>(barPath);
                Check(bar.MaxValue == game.BuyerWaitInitial && bar.Value == game.BuyerWaitInitial - game.BuyerWaitRemaining,
                    "Thought bubble tracks the exact individual elapsed fraction");
                var pause = game.GetNode<PauseMenu>("PauseMenu");
                double time = game.BuyerWaitRemaining;
                await PressEscapeForTest();
                Check(Paused, "Escape must open the true pause");
                game._Process(2);
                game.TickCraftClocks(0);
                Check(game.BuyerWaitRemaining == time, "Actual pause must stop the buyer clock");
                pause.Close();

                var craft = game.GetNode<EmbroideryGame>(music ? "PolishingGame" : "EmbroideryGame");
                craft.SetProcess(false);
                game.Crafting.Catalog.SetProcess(false);
                craft.Open();
                Check(game.Crafting.Select(music ? "guitar" : "hat"), "Select a concrete item before preparing craft");
                Check(craft.IsOpen && Paused, "The pre-start crafting menu must be open");
                double dayTime = game.DayRemainingSeconds;
                void Tick(double delta)
                {
                    game.Crafting.Catalog._Process(delta);
                    craft._Process(delta);
                    game._Process(delta);
                }
                Tick(1);
                Check(game.BuyerWaitRemaining == time - 1 && game.DayRemainingSeconds == dayTime - 1,
                    "Preparation must tick both shop clocks");
                craft.Activate();
                double craftTime = craft.ActiveSession.RemainingSeconds;
                await PressEscapeForTest();
                Check(pause.IsOpen, "Escape during craft must open the pause menu");
                Tick(2);
                Check(game.DayRemainingSeconds == dayTime - 1 && craft.ActiveSession.RemainingSeconds == craftTime &&
                    game.BuyerWaitRemaining == time - 1, "Esc pause must freeze all three timers");
                pause.Close();
                Tick(2);
                Check(game.BuyerWaitRemaining == time - 3 && game.DayRemainingSeconds == dayTime - 3 &&
                    craft.ActiveSession.RemainingSeconds == craftTime - 2,
                    "Playing either minigame must tick all three clocks");
                craft.FinishForTesting(true);
                Tick(1);
                Check(game.DayRemainingSeconds == dayTime - 4 && game.BuyerWaitRemaining == time - 4 &&
                    bar.Value == game.BuyerWaitInitial - game.BuyerWaitRemaining,
                    "The result screen must tick both shop clocks and update the patience bar");
                craft.Activate();
                time -= 4;
                Check(!craft.IsOpen && !Paused && game.BuyerWaitRemaining == time,
                    "Closing the craft must preserve the current waiting duration");

                game.RerollsRemaining = 3;
                game.RerollCustomer();
                Check(game.RerollsRemaining == 2 && game.BuyerWaitInitial == data.BuyerWaitInitial &&
                    game.BuyerWaitRemaining == time && ShadowGeometry() == geometry,
                    "Reroll changes the offer while preserving the timer and the one reserved shadow");
                game.PersistProgress();
                await Reload();
                Check(game.BuyerWaitInitial == data.BuyerWaitInitial && game.BuyerWaitRemaining == time &&
                    ShadowGeometry() == geometry, "Active sale, both timer values and originating shadow must restore exactly");
                Check(game.ShowcaseItems.Count(item => item == (music ? 2 : 0)) == 2 && game.StoredItems.Length == 0,
                    "Loading a reserved trade must not duplicate the sold item");
                game._Process(time);
                Check(game.TradeActive && game.BuyerWaitRemaining == 0 &&
                    game.GetNode<ProgressBar>(barPath).Ratio == 1 &&
                    !game.GetNode<Button>(tradeButtons + "AcceptButton").Disabled,
                    "Expiry must fill the bar and keep the buyer available for the existing final offer");
                game.AcceptTrade();
                Check(!game.GetNode<TextureRect>(slot + "/TradeShadow").Visible &&
                    game.GetNode<TextureRect>(slot + "/Icon").Texture == null,
                    "Completed sale must remove the shadow and the sold item");

                await Reload(data);
                game._Process(0);
                game.OnShowcasePressed(music ? 3 : 0);
                ShadowGeometry();
                game.ReturnToCheckpoint();
                Check(!game.GetNode<TextureRect>(slot + "/TradeShadow").Visible &&
                    game.GetNode<TextureRect>(slot + "/Icon").Texture != null,
                    "Checkpoint cancellation must restore the original stock and remove the shadow");

                var accept = game.GetNode<Button>(tradeButtons + "AcceptButton");
                var decline = game.GetNode<Button>(tradeButtons + "DeclineButton");
                var craftButton = game.GetNode<Button>("World/Atelier/GetItemButton");
                var unlock = game.GetNode<MusicWorkshopUnlockPanel>("World/MusicWorkshop/UnlockPanel").UnlockButton;
                foreach (BaseButton button in new BaseButton[] { accept, decline, craftButton, unlock })
                    CheckButtonShadow(button);
                foreach (var button in new[] { craftButton, unlock })
                {
                    var material = (ShaderMaterial)button.GetNode<ButtonDropShadow>("DropShadow").Material;
                    Check(!material.GetShaderParameter("use_mask").AsBool() &&
                        material.GetShaderParameter("face_position").AsVector2() == new Vector2(1.5f, 3.5f),
                        "The workshop unlock and craft buttons must use the same solid shadow profile");
                }
                CheckButtonShadow(unlock.GetParent().GetParent<PanelContainer>());
                pause = game.GetNode<PauseMenu>("PauseMenu");
                pause.Open();
                Check(!game.GetNode<Button>("PauseMenu/Root/Overlay/Center/Panel/Content/Menu/SettingsButton").HasNode("DropShadow"),
                    "Pause controls must have no map shadows");
                pause.Close();

                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                card.Open(1, 2, new[] { DayUpgrade.Cash, DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice }, 2000);
                await WaitForDayCardAnimation(card);
                foreach (var purchase in card.FindChildren("Choose", "Button", true, false).OfType<Button>())
                {
                    Check(!purchase.HasNode("DropShadow") && !purchase.GetParent().GetParent<PanelContainer>().HasNode("DropShadow"),
                        "Between-day cards and their buttons must have no map shadows");
                }
                var purchaseButton = card.FindChildren("Choose", "Button", true, false).OfType<Button>().First();
                await CheckShadowButtonInput(purchaseButton, $"purchase-button-shadow-{size.X}");
                card.Close();

                game.QueueFree();
                game = null;
                await ToSignal(this, SignalName.ProcessFrame);

                menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
                Root.AddChild(menu);
                var play = menu.GetNode<TextureButton>("MainMenuContainer/Menu/PlayButton");
                CheckButtonShadow(play);
                CheckButtonShadow(menu.GetNode<TextureButton>("MainMenuContainer/Menu/SettingsButton"));
                CheckButtonShadow(menu.GetNode<TextureButton>("MainMenuContainer/Menu/QuitButton"));
                Check(menu.GetNode<Control>("SettingsMenuContainer").FindChildren("DropShadow", "", true, false).Count == 0,
                    "The settings screen must not have shadows");
                await CheckShadowButtonInput(play, $"menu-button-shadows-{size.X}");
                await ClickControl(menu, "MainMenuContainer/Menu/PlayButton");
                foreach (string name in new[] { "NewGameButton", "ContinueButton", "ChoiceBackButton" })
                    CheckButtonShadow(menu.GetNode<TextureButton>("MainMenuContainer/Menu/" + name));
                using (await CapturePresentation($"choice-button-shadows-{size.X}")) { }
                // Preview this decoration independently: New Game now enters training before prologue.
                menu.GetNode<CanvasLayer>("IntroSkipLayer").Visible = true;
                var skip = menu.GetNode<TextureButton>("IntroSkipLayer/SkipButton");
                skip.Show();
                CheckButtonShadow(skip);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                MoveButtonPointer(ScreenBounds(skip).GetCenter());
                using (await CapturePresentation($"intro-skip-shadow-{size.X}")) { }
                Check(skip.IsHovered(), "The skip shadow must not obstruct the dialogue's own mouse handling");
                menu.QueueFree();
                menu = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        finally
        {
            menu?.QueueFree();
            game?.QueueFree();
            Paused = false;
            Root.Size = originalSize;
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task CheckShadowButtonInput(BaseButton button, string capture)
    {
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        Vector2 center = ScreenBounds(button).GetCenter();
        MoveButtonPointer(center);
        using (await CapturePresentation(capture)) { }
        Check(button.IsHovered(), $"Hover must reach {button.Name}, actual {Root.GuiGetHoveredControl()?.GetPath()} at {center}");
        PressButtonPointer(center, true);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(button.IsPressed(), "The shadow must not intercept a press");
        var outside = new Vector2(3, 3);
        MoveButtonPointer(outside);
        PressButtonPointer(outside, false);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(!button.IsPressed() && !button.IsHovered(), "The shadow must not leave a stale press or hover");
    }

    private static void CheckButtonShadow(Control button)
    {
        var shadow = button.GetNode<ButtonDropShadow>("DropShadow");
        var material = shadow.Material as ShaderMaterial;
        Check(shadow.ShowBehindParent && !shadow.IsProcessingInput() && material?.Shader.ResourcePath ==
            "res://shaders/ui_soft_shadow.gdshader" && material.GetShaderParameter("blur_radius").AsSingle() >= 5,
            "A shared blurred visual must draw behind the UI without receiving pointer or focus");
    }
}
