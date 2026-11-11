using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

public partial class ProjectTest : SceneTree
{
    private const string SavePath = "user://trade_progress_test.json";

    public override void _Initialize() => Callable.From(Run).CallDeferred();

    private async void Run()
    {
        string originalLocale = TranslationServer.GetLocale();
        TranslationServer.SetLocale("ru");
        try
        {
            if (OS.GetCmdlineUserArgs().Contains("--day-announcement-checks"))
            {
                await TestDayAnnouncements();
                GD.Print("DAY_ANNOUNCEMENT_CHECKS_PASS: lifecycle, smooth fade, midpoint, restore, localization and input");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--debug-hotkey-checks"))
            {
                await TestDebugHotkeys();
                GD.Print("DEBUG_HOTKEY_CHECKS_PASS: funds, time, craft outcomes and input guards");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--room-doorway-checks"))
            {
                await TestRoomDoorways();
                GD.Print("ROOM_DOORWAY_CHECKS_PASS: initial, failed/successful unlock, reload, checkpoint and new game");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--new-game-funds-checks"))
            {
                await TestNewGameFunds();
                GD.Print("NEW_GAME_FUNDS_CHECKS_PASS: fresh start, reset, checkpoint, save/reload and scene defaults");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--progress-toggle-checks"))
            {
                await TestCollapsibleProgress();
                GD.Print("PROGRESS_TOGGLE_CHECKS_PASS: no sticky mouse selection, hover and keyboard preserved");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--closed-reroll-checks"))
            {
                await TestClosedDayRerolls();
                GD.Print("CLOSED_REROLL_CHECKS_PASS: timeout/manual close block buyer changes, final sale preserved");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--pause-hotkey-checks"))
            {
                await TestPauseHotkey();
                GD.Print("PAUSE_HOTKEY_CHECKS_PASS: no screen button, Escape pause/settings/resume and repeat guard");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--pause-localization-checks"))
            {
                await TestPauseLocalization();
                GD.Print("PAUSE_LOCALIZATION_CHECKS_PASS: live language, persistence, layout and gameplay invariance");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--button-feedback-checks"))
            {
                await TestButtonFeedback();
                await TestButtonFeedbackInGame();
                await TestTextureButtonFeedbackBounds();
                await TestShowcaseFeedbackBounds();
                GD.Print("BUTTON_FEEDBACK_CHECKS_PASS: grayscale hover/focus, brief action colors, input and lifecycle");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--presentation-checks"))
            {
                await TestMonochromePresentation();
                GD.Print("PRESENTATION_CHECKS_PASS: grayscale scenes, clock/table colors, modal masking and resize");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--resource-checks"))
            {
                TestResourceOrganization();
                await TestLocalizedMenuAssets();
                GD.Print("RESOURCE_CHECKS_PASS: font glyphs, resources and localized menu");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--persistence-checks"))
            {
                await TestProgress();
                await TestSaveCompatibility();
                GD.Print("PERSISTENCE_CHECKS_PASS: legacy migrations, checkpoint and round trip");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--intro-transition-checks"))
            {
                await TestIntroTransition();
                GD.Print("INTRO_TRANSITION_SMOKE_PASS: finish, skip, null fallback and continue reached TradeScene");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--balance-checks"))
            {
                await TestLiveEconomyBalance();
                await TestClockHud();
                await TestDayCardPurchases();
                GD.Print("Live economy, clock and paid upgrade regression checks passed");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--day-timer-checks"))
            {
                await TestClockHud();
                GD.Print("Clock HUD seconds, assets, layout, expiry and reload checks passed");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--upgrade-purchase-checks"))
            {
                await TestDayCardPurchases();
                GD.Print("Paid upgrade and skip checks passed");
                Quit();
                return;
            }
            TestResourceOrganization();
            await TestSaveCompatibility();
            TestEconomyConfig();
            await TestPlaceholderItems();
            TestPopupPlacement();
            await TestStandaloneCamera();
            await TestUiLayoutInvalidation();
            await TestPauseAndZoomUi();
            await TestCollapsibleProgress();
            await TestRarityPopupGeometry();
            await TestEmbroidery();
            await TestPolishing();
            await TestLockedRoomDrag();
            await TestMouseControls();
            await TestInventoryTransfers();
            await TestTradeScene();
            await TestShowcaseReuse();
            await TestProgress();
            await TestDayCycle();
            await TestClockHud();
            await TestDayTransitionCards();
            await TestDayCardPurchases();
            await TestCategoryPriceUpgrades();
            await TestLocalization();
            await TestLongSentenceScroll();
            await TestMainMenu();
            GD.Print("All C# project checks passed");
            Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            Quit(1);
        }
        finally
        {
            TranslationServer.SetLocale(originalLocale);
        }
    }
}
