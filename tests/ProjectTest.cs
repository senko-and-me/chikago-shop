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
            if (OS.GetCmdlineUserArgs().Contains("--v131-revision-checks"))
            {
                await TestRevisionUi();
                GD.Print("V131_REVISION_CHECKS_PASS: localized cigarettes, sale and bubble, pause artwork at 720p and 1080p");
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
            if (OS.GetCmdlineUserArgs().Contains("--polishing-balance-checks"))
            {
                TestPolishingBalance();
                GD.Print("POLISHING_BALANCE_CHECKS_PASS");
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
            if (OS.GetCmdlineUserArgs().Contains("--counter-item-checks"))
            {
                await TestCounterItemPlacement();
                GD.Print("COUNTER_ITEM_CHECKS_PASS: all item silhouettes rest on the counter, zoom/resize and visibility");
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
            if (OS.GetCmdlineUserArgs().Contains("--room-doorway-checks"))
            {
                await TestRoomDoorways();
                GD.Print("ROOM_DOORWAY_CHECKS_PASS: initial, failed/successful unlock, reload, checkpoint and new game");
                Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--progress-toggle-checks"))
            {
                await TestCollapsibleProgress();
                GD.Print("PROGRESS_TOGGLE_CHECKS_PASS: cigarette pack hover, animation, live quotas, keyboard and recovery");
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
            if (OS.GetCmdlineUserArgs().Contains("--resource-checks"))
            {
                TestResourceOrganization();
                await TestLocalizedMenuAssets();
                GD.Print("RESOURCE_CHECKS_PASS: font glyphs, resources and localized menu");
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
            if (OS.GetCmdlineUserArgs().Contains("--showcase-campaign-checks") ||
                OS.GetCmdlineUserArgs().Contains("--tutorial-checks"))
            {
                await TestShowcaseCampaign();
                GD.Print("SHOWCASE_CAMPAIGN_CHECKS_PASS");
                Quit();
                return;
            }
            await TestShowcaseCampaign();
            TestResourceOrganization();
            TestPopupPlacement();
            await TestStandaloneCamera();
            await TestCollapsibleProgress();
            await TestGameMusic();
            await TestLongSentenceScroll();
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
