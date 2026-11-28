using System;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestBuyerPatience()
    {
        const string save = "user://pol03_buyer_patience.json";
        const string buttons = "World/TradingRoom/Item/TradePanel/Content/Buttons/";
        TradeScene game = null;
        async Task Reload(string state = null)
        {
            if (game != null) { game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); }
            if (state != null) using (var file = FileAccess.Open(save, FileAccess.ModeFlags.Write)) file.StoreString(state);
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game); game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        void CheckExpiredOffer()
        {
            int half = TradeScene.PriceForPercent(game.GetItemPrice(game.ItemIndex), -50);
            Check(game.TradeActive && game.BuyerWaitRemaining == 0 && game.Percentage == -50 && game.OfferedPrice == half,
                "Expired buyer must immediately offer half the normal price and display -50%");
            var content = game.GetNode("World/TradingRoom/Item/TradePanel/Content");
            int quality = TradeScene.QualityBonus(game.ActiveTradeQualityRank);
            string expected = GameLocalization.Format("Получим: {0}$", half);
            if (quality != 0) expected += " " + TradeScene.SignedAmount(quality);
            Check(content.GetNode<RichTextLabel>("OfferLabel").GetParsedText() == expected &&
                content.GetNode<RichTextLabel>("OfferLabel").TooltipText.Contains(GameLocalization.Format("Скорость: +{0}$", 0)),
                "Expired payout must retain quality and omit the speed bonus");
            Check(game.GetNode<Button>(buttons + "DeclineButton").Disabled && !game.GetNode<Button>(buttons + "AcceptButton").Disabled,
                "After timeout only accepting the offer remains available");
        }
        try
        {
            await Reload("""{"save_version":16,"day_started":true,"score":500,"energy_remaining":1,"trade_active":true,"item_index":0,"customer_index":0,"percentage":20,"rerolls_remaining":3,"buyer_wait_remaining":0.25,"active_trade_grade":"S","stored_items":[1]}""");
            game._Process(0.24);
            Check(game.Percentage == 20 && !game.GetNode<Button>(buttons + "DeclineButton").Disabled,
                "The original offer and rerolls must remain available while patience is positive");
            game._Process(0.02);
            CheckExpiredOffer();
            int customer = game.CustomerIndex, rerolls = game.RerollsRemaining, price = game.OfferedPrice;
            game.RerollCustomer();
            Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = true }, true);
            Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = false }, true);
            await ClickControl(game, buttons + "DeclineButton");
            Check(game.CustomerIndex == customer && game.RerollsRemaining == rerolls && game.OfferedPrice == price,
                "Mouse, keyboard and direct rerolls must not replace an expired buyer or spend attempts");
            game.PersistProgress(); await Reload();
            CheckExpiredOffer();
            int expected = game.Score + game.OfferedPrice + TradeScene.QualityBonus(game.ActiveTradeQualityRank);
            await ClickControl(game, buttons + "AcceptButton");
            Check(!game.TradeActive && game.Score == expected && game.Accepted == 1,
                "Accepting an expired S-quality item must pay half plus quality once");
            game.AcceptTrade();
            Check(game.Score == expected, "Repeated accept must not pay twice");
            game._Process(0);
            Check(game.BuyerWaitRemaining == game.BuyerWaitInitial && game.BuyerWaitInitial is >= 15 and <= 20, "The next customer must reset their patience within the shared range");
            if (!game.TradeActive) game.OnShowcasePressed(game.FirstDisplayedItem("atelier"));
            game._Process(0);
            Check(game.TradeActive && game.Percentage >= -30 && !game.GetNode<Button>(buttons + "DeclineButton").Disabled,
                "The next buyer must use a normal offer and available rerolls");
            int ordinaryAttempts = game.RerollsRemaining;
            double ordinaryPatience = game.BuyerWaitRemaining;
            game.RerollCustomer();
            Check(game.RerollsRemaining == ordinaryAttempts - 1 && game.BuyerWaitRemaining == ordinaryPatience,
                "Outside tutorial, a reroll must spend one attempt without refreshing the buyer's patience");

            await Reload("""{"save_version":16,"day_started":true,"energy_remaining":0,"waiting_buyer_present":true,"customer_index":0,"buyer_wait_remaining":0.01,"stored_items":[0]}""");
            game._Process(0.02);
            game.OnShowcasePressed(game.FirstDisplayedItem("atelier"));
            game._Process(0);
            CheckExpiredOffer();
            game.PersistProgress(); await Reload(); CheckExpiredOffer();
            int loyal = (int)DayUpgrade.Loyal;
            await Reload($$"""{"save_version":20,"day_started":true,"score":500,"trade_active":true,"item_index":0,"customer_index":0,"percentage":-27,"rerolls_remaining":3,"buyer_wait_remaining":0.5,"buyer_wait_initial":20,"active_trade_grade":"D","selected_upgrade":{{loyal}}}""");
            int expired = Math.Min(-27, -game.DayUpgrades.Rules.For(DayUpgrade.Loyal).Amount);
            game._Process(0.6);
            Check(game.Percentage == expired && game.GetNode<Button>(buttons + "DeclineButton").Disabled,
                "Even with the loyal card, expiry must never raise the existing offer or allow rerolls");
            int latePayout = game.OfferedPrice - 20;
            Check(game.GetNode<RichTextLabel>("World/TradingRoom/Item/TradePanel/Content/OfferLabel").Text.Contains("[color=#c32938]"),
                "A loss from D quality must be displayed in red after expiration");
            game.PersistProgress();
            await Reload();
            Check(game.DayUpgrades.Selected == DayUpgrade.Loyal && game.Percentage == expired, "Loyal expiry must be stable across reload");
            int balance = game.Score;
            game.AcceptTrade();
            Check(game.Score == balance + latePayout, "Loyal expiry must retain the D deduction in the real payout");

            int bargain = (int)DayUpgrade.Bargaining;
            await Reload($$"""{"save_version":20,"day_started":true,"score":500,"trade_active":true,"item_index":0,"customer_index":0,"percentage":30,"rerolls_remaining":1,"buyer_wait_remaining":17,"buyer_wait_initial":20,"selected_upgrade":{{bargain}}}""");
            Check(game.DayUpgrades.Selected == DayUpgrade.None, "A saved bargaining card must load as no active upgrade");
            game.RerollCustomer();
            Check(game.RerollsRemaining == 0 && game.Percentage < 30 && game.BuyerWaitRemaining == 17,
                "The third reroll must follow the normal offer rules and preserve buyer patience");

            int patient = (int)DayUpgrade.Patient;
            await Reload($$"""{"save_version":20,"day_started":true,"score":500,"selected_upgrade":{{patient}}}""");
            game._Process(0);
            int extension = game.DayUpgrades.WaitExtension;
            Check(game.BuyerWaitInitial >= 15 + extension && game.BuyerWaitInitial <= 20 + extension,
                "Patient customers must receive the full extension on new arrivals");
            GD.Print("BUYER_PATIENCE_CHECKS_PASS: exact expiry, -50%, no reroll via UI/key/API, exact payout, reload and late stock");
        }
        finally
        {
            Paused = false;
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
        }
    }
}
