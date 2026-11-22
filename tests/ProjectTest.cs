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
            if (OS.GetCmdlineUserArgs().Contains("--interface-fixes-checks"))
            {
                await TestInterfaceFixes();
                GD.Print("INTERFACE_FIXES_CHECKS_PASS: instruments, 10 pack cycles, stationary notebook and all pause buttons at 720p/1080p");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--canonical-migration-checks"))
            {
                await TestCanonicalMigration();
                GD.Print("CANONICAL_MIGRATION_CHECKS_PASS: catalogue10, v18/v19, grades, checkpoint, active trade, real-save copy and round trip");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--buyer-availability-checks"))
            {
                await TestBuyerAvailability();
                GD.Print("BUYER_AVAILABILITY_CHECKS_PASS");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--buyer-patience-checks"))
            {
                await TestBuyerPatience();
                GD.Print("BUYER_PATIENCE_CHECKS_PASS");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--campaign-balance-checks"))
            {
                await TestCampaignBalance();
                GD.Print("CAMPAIGN_BALANCE_CHECKS_PASS: fourteen days, exact final quota, win/loss, terminal reload and reset");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--save-compatibility-checks"))
            {
                await TestSaveCompatibility();
                GD.Print("SAVE_COMPATIBILITY_CHECKS_PASS: legacy versions, active trade, checkpoint, current rewrite and invalid JSON");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--quota-visit-checks"))
            {
                await TestQuotaVisit();
                GD.Print("QUOTA_VISIT_CHECKS_PASS: greeting, dialogue reload, gift, checkpoint, failure, notebook and unlock");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--rarity-color-checks"))
            {
                await TestRarityColors();
                GD.Print("RARITY_COLOR_CHECKS_PASS: both workshops, roll tickets, prices, both languages, reload and reset");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--buyer-arrival-checks"))
            {
                await TestBuyerArrival();
                GD.Print("BUYER_ARRIVAL_CHECKS_PASS: immediate buyers, 20-second wait, tips, modal pause, saves, unlock panel and atelier art");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--prologue-motion-checks"))
            {
                await TestPrologueMotion();
                GD.Print("PROLOGUE_MOTION_CHECKS_PASS: dialogue timing, arrival/departure, locale, resize, rapid input and restart");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--room-curtain-checks"))
            {
                await TestRoomCurtains();
                GD.Print("ROOM_CURTAIN_CHECKS_PASS: reveal, repeated refresh, pause, reset, loaded state and teardown");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--energy-bulbs-checks"))
            {
                await TestEnergyBulbs();
                GD.Print("ENERGY_BULBS_CHECKS_PASS: remaining, depleted, unlocked/upgraded capacity, localized tooltips, pause and three window sizes");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--polishing-start-checks"))
            {
                await TestPolishingStart();
                GD.Print("POLISHING_START_CHECKS_PASS: real held clicks, start, collect, resize and languages");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--polishing-reward-checks"))
            {
                await TestPolishingReward();
                GD.Print("POLISHING_REWARD_CHECKS_PASS: all instruments, weighted previews, free cancel, exact reward, payment, quality, timeout and reload");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--room-title-checks"))
            {
                await TestRoomTitles();
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--quota-countdown-checks"))
            {
                await TestQuotaCountdown();
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--day-start-gate-checks"))
            {
                await TestDayStartGate();
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--room-focus-checks"))
            {
                await TestRoomDoubleClickFocus();
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--craft-contrast-checks"))
            {
                await TestCraftContrast();
                GD.Print("CRAFT_CONTRAST_CHECKS_PASS: light panels, black text, visible quality fill, both minigames/states/locales/sizes");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--counter-item-checks"))
            {
                await TestCounterItemPlacement();
                GD.Print("COUNTER_ITEM_CHECKS_PASS: all item silhouettes rest on the counter, zoom/resize and visibility");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--inventory-aspect-checks"))
            {
                await TestInventoryIconAspect();
                await TestInventoryTransfers();
                GD.Print("INVENTORY_ASPECT_CHECKS_PASS: rendered wide/tall/square icons, centered fit, resize and item transfers");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--day-expiry-checks"))
            {
                await TestDayExpiry();
                GD.Print("DAY_EXPIRY_CHECKS_PASS: automatic cards, active trade/craft, pause, reload and no duplicate advance");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--sound-effects-checks"))
            {
                await TestSoundEffects();
                GD.Print("SOUND_EFFECTS_CHECKS_PASS: immediate trimmed hover/click, first-wins UI playback without queue/restart, actions/keyboard, special cue priority, natural buyer bell, pause and cleanup");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--debug-balance-checks"))
            {
                await TestDebugBalance();
                GD.Print("DEBUG_BALANCE_CHECKS_PASS: hotkey/modal, live apply, new run, disk/reload, loot/items/craft/card prices and weights, validation and defaults");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--music-settings-checks"))
            {
                await TestMusicSettings();
                GD.Print("MUSIC_SETTINGS_CHECKS_PASS: live bus, mute, persistence, menu/pause, RU/EN, mouse/keyboard and layout");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--dialogue-portraits-checks"))
            {
                await TestDialoguePortraits();
                await TestStoryPortraitExpressions();
                await TestStoryDialogueLayout();
                GD.Print("DIALOGUE_PORTRAITS_CHECKS_PASS: scene portraits, speaker tones, narration, RU/EN, alpha and all story scenes at 720p/1080p");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--tutorial-checks"))
            {
                await TestFirstTradeTutorial();
                GD.Print("TUTORIAL_CHECKS_PASS: camera practice/flights, first sale, real clicks, highlights, pause, localization, skip, replay and reload");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--inventory-expansion-checks"))
            {
                await TestInventoryExpansion();
                GD.Print("INVENTORY_EXPANSION_CHECKS_PASS: left shift, staggered arrivals, items, reset, resize and reload");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--craft-localization-checks"))
            {
                await TestLocalizedCraftButtons();
                GD.Print("CRAFT_LOCALIZATION_CHECKS_PASS: RU/EN/RU, both workshops, styles, help icon, real craft click and cancellation");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--camera-limits-checks"))
            {
                await TestCameraReferenceLimits();
                await TestStandaloneCamera();
                GD.Print("CAMERA_LIMITS_CHECKS_PASS: reference zoom limits, all pan edges, resize, cursor anchor, GUI and pause");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--day-start-checks"))
            {
                await TestDayStart();
                GD.Print("DAY_START_CHECKS_PASS: clock size/insets, real start click, timer/buyers, reload, legacy, next day, checkpoint and new game");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--room-frame-checks") ||
                OS.GetCmdlineUserArgs().Contains("--camera-frame-checks"))
            {
                await TestRoomFrames();
                GD.Print("CAMERA_FRAME_CHECKS_PASS: no screen border, six preserved room frames and aligned day/night backgrounds");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--dialogue-localization-checks"))
            {
                await TestDialogueLocalization();
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--game-music-checks"))
            {
                await TestGameMusic();
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--intro-music-checks"))
            {
                await TestIntroMusic();
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--menu-music-checks"))
            {
                await TestMenuMusic();
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--embroidery-target-checks"))
            {
                await TestEmbroideryTarget();
                GD.Print("EMBROIDERY_TARGET_CHECKS_PASS: rendered contrast, sizes, target movement and real clicks");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--card-dealing-checks"))
            {
                await TestCardDealing();
                GD.Print("CARD_DEALING_CHECKS_PASS: deck, stagger, flip, dimmed gameplay, purchase/skip, cancellation and resize");
                Quit();
                return;
            }
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
                GD.Print("PROGRESS_TOGGLE_CHECKS_PASS: cigarette pack hover, animation, live quotas, keyboard and reload");
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
            if (OS.GetCmdlineUserArgs().Contains("--button-palette-checks"))
            {
                await TestButtonFeedback();
                GD.Print("BUTTON_PALETTE_CHECKS_PASS: neutral accents, native grayscale button states, mouse/keyboard and input lifecycle");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--presentation-checks"))
            {
                await TestNativeColorPresentation();
                GD.Print("PRESENTATION_CHECKS_PASS: authored grayscale scenes/art/runtime, colored bulbs, absent filter, menu/craft/cards/pause/rarity and resize");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--asset-replacement-checks"))
            {
                await TestDriveAssetReplacements();
                GD.Print("ASSET_REPLACEMENT_CHECKS_PASS: identities, sprite counts, fitted art, polishing, trade and reload");
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
            if (OS.GetCmdlineUserArgs().Contains("--intro-latency-checks"))
            {
                await TestIntroLatency();
                GD.Print("INTRO_LATENCY_CHECKS_PASS: real skip to first game frame, immediate and after reading");
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
            await TestGameMusic();
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
