using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestBuyerAvailability()
    {
        const string save = "user://pol02_buyer_supply.json";
        const string customerPath = "World/TradingRoom/Customer";
        const string thoughtPath = "World/TradingRoom/BuyerWaitingThought/ThoughtBubble";
        TradeScene game = null;
        async Task Load(string json)
        {
            if (game != null) { game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); }
            using (var file = FileAccess.Open(save, FileAccess.ModeFlags.Write)) file.StoreString(json);
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        void CheckEmptyCounter(string reason) => Check(!game.TradeActive &&
            !game.GetNode<ModularCustomer>(customerPath).Visible &&
            !game.GetNode<Control>(thoughtPath).Visible, reason);
        try
        {
            await Load("""{"save_version":16,"day_started":true,"energy_remaining":0}""");
            Check(game.EnergyRemaining == 0 && game.StoredItems.Length == 0, "Fixture must have no energy or stock");
            CheckEmptyCounter("No customer should appear when no goods or energy remain");
            int arrivals = 0;
            game.BuyerArrived += () => arrivals++;
            game._Process(0); game._Process(1); game._Process(20);
            CheckEmptyCounter("Idle frames must not create an impossible customer");
            Check(arrivals == 0, "An empty exhausted shop must not emit an arrival or its bell");

            game.NewGame(); game.EndDay();
            Check(game.GetNode<ModularCustomer>(customerPath).Visible, "A productive fresh day must allow a waiting customer");
            game.EnergyRemaining = 0;
            game._Process(0);
            CheckEmptyCounter("A customer already waiting when supply becomes impossible must leave without reloading");

            await Load("""{"save_version":16,"day_started":true,"energy_remaining":0,"customer_index":0,"waiting_buyer_present":true,"buyer_wait_remaining":12}""");
            CheckEmptyCounter("An obsolete waiting customer must disappear immediately when loading an exhausted empty shop");
            using (var json = System.Text.Json.JsonDocument.Parse(FileAccess.GetFileAsString(save)))
                Check(!json.RootElement.GetProperty("waiting_buyer_present").GetBoolean(),
                    "Removing an impossible waiting customer must also normalize the saved state");

            await Load("""{"save_version":16,"day_started":true,"energy_remaining":0,"stored_items":[0]}""");
            Check(game.GetNode<ModularCustomer>(customerPath).Visible && !game.TradeActive,
                "An item in inventory must still attract a buyer at zero energy");
            game.SelectInventoryItem(0); game.PlaceItemInShowcase(0); game._Process(0);
            Check(game.TradeActive && game.EnergyRemaining == 0, "The final inventory item must remain sellable");
            game._Process(0.5);
            Check(game.TradeActive, "An active final deal must not disappear when all other stock is empty");
            game.AcceptTrade();
            arrivals = 0;
            game.BuyerArrived += () => arrivals++;
            game._Process(0); game._Process(1);
            CheckEmptyCounter("After the final sale no new waiting customer may appear");
            Check(arrivals == 0, "The final sale must not trigger another arrival sound");

            await Load("""{"save_version":16,"day_started":true,"energy_remaining":0,"showcase_items":[0,-1,-1,-1,-1,-1]}""");
            game._Process(0);
            Check(game.TradeActive && game.EnergyRemaining == 0, "Displayed goods must also be sellable at zero energy");
            game.AcceptTrade(); game._Process(0);
            CheckEmptyCounter("Selling the last displayed item must leave the counter empty");
            game.EndDay(); game._Process(0);
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
            await WaitForDayCardAnimation(game);
            game.EndDay();
            Check(game.CurrentDay == 2 && game.EnergyRemaining > 0 && game.GetNode<ModularCustomer>(customerPath).Visible,
                "The new day's replenished energy must restore normal arrivals");
            GD.Print("BUYER_SUPPLY_CHECKS_PASS: exhausted shop, saved waiting buyer, inventory/showcase stock, final sale and next day");
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
