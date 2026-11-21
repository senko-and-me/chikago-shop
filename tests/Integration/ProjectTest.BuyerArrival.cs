using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestBuyerArrival()
    {
        const string save = "user://buyer_arrival_test.json";
        const string customerPath = "World/TradingRoom/Customer";
        const string barPath = "World/TradingRoom/BuyerWaitingThought/ThoughtBubble/Stack/RedWaitingBar";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        bool rendered = DisplayServer.GetName() != "headless";
        var windowSize = Root.Size;
        TradeScene game = null;

        async Task Reload()
        {
            if (game != null)
            {
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            game = scene.Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }

        try
        {
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
            if (rendered) Root.Size = new Vector2I(1280, 720);
            await Reload();
            var customer = game.GetNode<ModularCustomer>(customerPath);
            int arrivals = 0;
            game.BuyerArrived += () => arrivals++;
            game._Process(20);
            Check(!customer.Visible && arrivals == 0, "Before Start there must be no customer");
            game.EndDay();
            Check(customer.Visible && !game.TradeActive && arrivals == 1 && game.BuyerWaitRemaining == 20,
                "Start must bring exactly one customer even with empty displays");
            Check(game.GetNode<DayAnnouncementOverlay>("DayAnnouncements").IsShowing &&
                !game.HasNode("DayAnnouncements/BuyerArrival"), "Only the existing day announcement should show at Start");

            foreach (var band in new[] { (0d, 0), (0.01d, 1), (4d, 1), (4.01d, 2), (8d, 2),
                (8.01d, 3), (12d, 3), (12.01d, 4), (16d, 4), (16.01d, 5), (20d, 5), (21d, 5) })
                Check(TradeScene.SpeedBonus(band.Item1) == band.Item2, $"Unexpected reward at {band.Item1} seconds");

            await CheckMusicUnlockPanel(game, rendered);
            var bar = game.GetNode<ProgressBar>(barPath);
            game._Process(10);
            Check(arrivals == 1 && bar.Value == 10 && bar.MaxValue == 20 && bar.Size == new Vector2(73, 15),
                "The original short bar must be half-filled after ten seconds, with the same customer");
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            game._Process(5);
            Check(Paused && game.BuyerWaitRemaining == 10 && bar.Value == 10, "Pause must freeze the red bar");
            pause.Close();
            game.GetItemFromMusicWorkshop();
            var polish = game.GetNode<EmbroideryGame>("PolishingGame");
            Check(polish.IsOpen && Paused, "The real polishing modal must open after buying the workshop");
            polish._Process(1);
            game._Process(5);
            Check(game.BuyerWaitRemaining == 10 && bar.Value == 10, "Crafting must freeze the customer timer");
            await PressEscapeForTest();
            Check(!polish.IsOpen && !Paused, "Escape must return from the crafting modal");

            game.PersistProgress();
            await Reload();
            customer = game.GetNode<ModularCustomer>(customerPath);
            Check(customer.Visible && game.BuyerWaitRemaining == 10, "Reload must restore a waiting customer and the remaining time");
            CraftAtelier(game);
            game.SelectInventoryItem(0);
            game.PlaceItemInShowcase(0);
            game._Process(0);
            Check(game.TradeActive && game.BuyerWaitRemaining == 10, "Serving starts a deal with the existing timer");
            var offerLabel = game.GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/OfferLabel");
            int price = game.OfferedPrice;
            int bonus = TradeScene.QualityBonus(game.ActiveTradeQualityRank);
            Check(offerLabel.Text == GameLocalization.Format("Получим: {0}$", price + bonus + 3),
                "The live offer must include the current speed band and the item quality");
            game._Process(2);
            Check(offerLabel.Text == GameLocalization.Format("Получим: {0}$", price + bonus + 2),
                "Crossing a time band must update the live total");
            game.RerollsRemaining = 3;
            game.RerollCustomer();
            Check(game.BuyerWaitRemaining == 8 && game.RerollsRemaining == 2 &&
                offerLabel.Text == GameLocalization.Format("Получим: {0}$", game.OfferedPrice + bonus + 2),
                "Reroll must update the offer and preserve remaining time");
            await Reload();
            Check(game.TradeActive && game.BuyerWaitRemaining == 8, "Reloading an active deal must preserve the exact time");
            game._Process(7.75);
            bar = game.GetNode<ProgressBar>(barPath);
            Check(bar.Value == 19.75 && TradeScene.SpeedBonus(game.BuyerWaitRemaining) == 1,
                "The customer earns the last dollar immediately before the end of the twenty-second window");
            game._Process(0.25);
            Check(game.BuyerWaitRemaining == 0 && bar.Value == 20 && TradeScene.SpeedBonus(game.BuyerWaitRemaining) == 0,
                "At twenty seconds the bar must be full and the speed bonus zero");

            int payout = game.Score + game.OfferedPrice + bonus;
            game.AcceptTrade();
            Check(game.Score == payout && game.Accepted == 1, "The sale must apply the current offer and quality bonus exactly once");
            arrivals = 0;
            game.BuyerArrived += () => arrivals++;
            game._Process(0);
            game._Process(0);
            Check(game.GetNode<ModularCustomer>(customerPath).Visible && !game.TradeActive &&
                game.BuyerWaitRemaining == 20 && arrivals == 1, "The next idle frame must bring one new buyer at full time");
            game.EndDay();
            game._Process(0);
            Check(Paused && game.DayTransitionOpen && arrivals == 1, "The day-card transition must prevent buyers");
            game.QueueFree();
            game = null;
            await ToSignal(this, SignalName.ProcessFrame);
            Paused = false;

            using (var legacy = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                legacy.StoreString("""{"save_version":16,"day_started":true,"score":250,"energy_remaining":3,"buyer_countdown":8,"stored_items":[0,1,4,10,11]}""");
            await Reload();
            Check(game.GetNode<ModularCustomer>(customerPath).Visible && game.BuyerWaitRemaining == 20,
                "A retired arrival delay in a legacy save must not prevent immediate arrival");
            await CheckAtelierArtwork(game);
            using (var legacy = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                legacy.StoreString("""{"save_version":16,"day_started":true,"customer_index":0,"waiting_buyer_present":true,"buyer_wait_remaining":3.25}""");
            await Reload();
            Check(game.GetNode<ModularCustomer>(customerPath).Visible && game.BuyerWaitRemaining == 3.25,
                "An existing buyer in a legacy save must keep the exact remaining seconds");
        }
        finally
        {
            game?.QueueFree();
            Paused = false;
            if (rendered) Root.Size = windowSize;
            TranslationServer.SetLocale("ru");
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
        }
        await ToSignal(this, SignalName.ProcessFrame);
        await TestBuyerAvailability();
    }

    private async Task CheckMusicUnlockPanel(TradeScene game, bool rendered)
    {
        const string panelPath = "World/MusicWorkshop/LockedOverlay/UnlockPanel";
        var panel = game.GetNode<MusicWorkshopUnlockPanel>(panelPath);
        var curtain = panel.GetParent<RoomCurtainPresentation>();
        var button = panel.UnlockButton;
        var title = panel.GetNode<Label>("Content/Title");
        int price = EconomyConfigProvider.Load().Workshops.Single(workshop => workshop.Id == "music").UnlockCost;
        game.GetNode<DayAnnouncementOverlay>("DayAnnouncements").Dismiss();
        game.Score = price - 1;
        game.RefreshDayPresentation();
        Check(button.Disabled && button.Text == $"Открыть за {price}$" && title.Tr(title.Text) == "Музыкальная мастерская",
            "Insufficient funds must disable a readable, correctly priced Russian button");
        TranslationServer.SetLocale("en");
        Check(button.Text == $"Unlock for ${price}" && title.Tr(title.Text) == "Music workshop",
            "The title and configured price must update live in English");
        Check(button.GetThemeStylebox("normal") is StyleBoxTexture normal && normal.Texture == panel.ButtonTexture &&
            button.GetThemeStylebox("hover") is StyleBoxTexture hover && hover.ModulateColor.R > normal.ModulateColor.R &&
            panel.GetThemeStylebox("panel") is StyleBoxTexture paper && paper.Texture == panel.PanelTexture,
            "The separate artwork slots must be used for the panel and for hover feedback");

        var camera = game.GetNode<WorldCamera>("Camera2D");
        camera.SetProcess(false);
        foreach (float zoom in new[] { camera.OverviewZoom, 1.4f })
        {
            camera.Zoom = Vector2.One * zoom;
            camera.Position = curtain.GetGlobalRect().GetCenter();
            camera.ForceUpdateScroll();
            await ToSignal(this, SignalName.ProcessFrame);
            Check(ScreenBounds(panel).GetCenter().DistanceTo(ScreenBounds(curtain).GetCenter()) < 1 &&
                ScreenBounds(curtain).Encloses(ScreenBounds(panel)), "The unlock card must stay centered inside the curtain at any zoom");
        }
        if (rendered) using (await CapturePresentation("music-unlock-disabled")) { }
        TranslationServer.SetLocale("ru");
        game.Score = price + 125;
        game.RefreshDayPresentation();
        Check(!button.Disabled, "An affordable workshop must become actionable");
        if (rendered) using (await CapturePresentation("music-unlock-affordable")) { }
        if (rendered) await ClickControl(game, panelPath + "/Content/UnlockButton");
        else button.EmitSignal(BaseButton.SignalName.Pressed);
        Check(game.MusicWorkshopUnlocked && game.Score == 125 && !panel.Visible && curtain.IsRevealing,
            "One purchase must charge the configured price, remove the panel and start the unchanged curtain reveal");
    }

    private async Task CheckAtelierArtwork(TradeScene game)
    {
        var items = new[]
        {
            (0, "hat", "atelier_hat.png", "Шляпка", "Hat"),
            (1, "gloves", "atelier_vest.png", "Жилетка", "Vest"),
            (4, "clothing_first", "atelier_gloves.png", "Перчатки", "Gloves"),
            (10, "cloche_hat", "atelier_vest.png", "Жилетка", "Vest"),
            (11, "shoes", "atelier_gloves.png", "Перчатки", "Gloves")
        };
        var craftOutcomes = Enumerable.Range(0, WorkshopItemCatalog.TotalWeight("atelier"))
            .Select(ticket => WorkshopItemCatalog.Roll("atelier", ticket)).ToHashSet();
        foreach (var (index, kind, artwork, russian, english) in items)
        {
            string artPath = "res://art/items/" + artwork;
            Check(craftOutcomes.Contains(index) && game.StoredItems[0] == index && TradeScene.GetItemKind(index) == kind,
                "Crafting and legacy inventory must retain every original kind and index");
            foreach (var (locale, name) in new[] { ("ru", russian), ("en", english) })
            {
                TranslationServer.SetLocale(locale);
                await ToSignal(this, SignalName.ProcessFrame);
                var slot = game.GetNode<InventoryUI>("HUD/InventoryUI").Container.GetChild<SlotUI>(0);
                var icon = slot.GetNode<TextureRect>("Icon");
                Check(TradeScene.GetItemName(index) == name && slot.ItemInfo.Contains(name) &&
                    icon.Texture.ResourcePath == artPath && icon.StretchMode == TextureRect.StretchModeEnum.KeepAspectCentered,
                    "Inventory, tooltip and localized item name must agree with the new proportional artwork");
            }
            game.SelectInventoryItem(0);
            game.PlaceItemInShowcase(0);
            var showcase = game.GetNode<TextureRect>("World/ShowroomLeft/Showcases/Slot1/Icon");
            Check(showcase.Texture.ResourcePath == artPath && showcase.StretchMode == TextureRect.StretchModeEnum.KeepAspectCentered,
                "The display must preserve the item texture proportions");
            game._Process(0);
            var visible = game.GetNode<Node2D>("World/TradingRoom/Item").GetChildren().OfType<Sprite2D>().Single(sprite => sprite.Visible);
            Check(game.TradeActive && game.ItemIndex == index && visible.Texture.ResourcePath == artPath &&
                visible.Scale.X == visible.Scale.Y && (visible.Texture.GetSize() * visible.Scale).Length() <= 182,
                "The sale must show exactly one fitted sprite for the saved kind");
            game.AcceptTrade();
        }
        var vest = GD.Load<Texture2D>("res://art/items/atelier_vest.png");
        Check(vest.GetWidth() == 104 && vest.GetHeight() == 149, "Vest preparation must remove the large right-hand margin without stretching the art");
        Check(TradeScene.GetItemArtPath(5).EndsWith("/jacket.png") && TradeScene.GetItemArtPath(6).EndsWith("/fur_stole.png") &&
            WorkshopItemCatalog.Items.Where(item => item.WorkshopId == "music").All(item => !item.ArtPath.Contains("atelier_")),
            "Exclusive and unique clothing and all the instruments must keep their existing artwork");
    }
}
