using System.Threading.Tasks;
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
            Check(content.GetNode<RichTextLabel>("OfferLabel").GetParsedText() == GameLocalization.Format("Получим: {0}$", half),
                "Expired displayed payout must be exactly half without speed/quality bonuses");
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
            int expected = game.Score + game.OfferedPrice;
            await ClickControl(game, buttons + "AcceptButton");
            Check(!game.TradeActive && game.Score == expected && game.Accepted == 1,
                "Accepting an expired S-quality item must pay exactly half once");
            game.AcceptTrade();
            Check(game.Score == expected, "Repeated accept must not pay twice");
            game._Process(0);
            Check(game.BuyerWaitRemaining == game.BuyerWaitInitial && game.BuyerWaitInitial is >= 10 and <= 20 && !game.TradeActive, "The next customer must reset their patience");
            game.OnShowcasePressed(game.FirstDisplayedItem("atelier"));
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
