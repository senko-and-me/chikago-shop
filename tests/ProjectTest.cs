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
