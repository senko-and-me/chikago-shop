using System;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest : SceneTree
{
    private const string SavePath = "user://trade_progress_test.json";

    public override void _Initialize() => Callable.From(Run).CallDeferred();

    private async void Run()
    {
        try
        {
            await TestTradeScene();
            await TestProgress();
            await TestMainMenu();
            GD.Print("All C# project checks passed");
            Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            Quit(1);
        }
    }

    private async Task TestMainMenu()
    {
        var scene = GD.Load<PackedScene>("res://scenes/main_menu.tscn")
            ?? throw new Exception("Main menu must load");
        var menu = scene.Instantiate<MainMenu>();
        Root.AddChild(menu);
        CurrentScene = menu;
        await ToSignal(this, SignalName.ProcessFrame);

        Check(menu.GameScene.ResourcePath == "res://scenes/trade_scene.tscn", "Play must open the trade scene");
        foreach (string path in new[]
        {
            "MainMenuContainer/Menu/PlayButton", "MainMenuContainer/Menu/SettingsButton",
            "MainMenuContainer/Menu/QuitButton", "SettingsMenuContainer/Settings/BackButton"
        })
            Check(menu.GetNode<TextureButton>(path).TextureNormal != null, $"Menu button art must load: {path}");

        var mainView = menu.GetNode<CenterContainer>("MainMenuContainer");
        var settingsView = menu.GetNode<CenterContainer>("SettingsMenuContainer");
        menu.OnSettingsPressed();
        Check(!mainView.Visible && settingsView.Visible, "Settings must open");
        menu.OnBackPressed();
        Check(mainView.Visible && !settingsView.Visible, "Back must return to the main menu");
        menu.OnPlayPressed();
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene is TradeScene, "Play must enter the game");
        GD.Print("Main menu checks passed");
    }

    private async Task TestTradeScene()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn")
            ?? throw new Exception("The trade scene must load");
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);

        var firstRoom = game.GetNode<Sprite2D>("World/TradingRoomBackground");
        var secondRoom = game.GetNode<Sprite2D>("World/EmptyRoomBackground");
        var roomTexture = firstRoom.Texture ?? throw new Exception("The room background must load");
        Check(roomTexture == secondRoom.Texture, "Both rooms must use the supplied background");
        Check(roomTexture.GetSize() == new Vector2(1920, 1080), "The room background must keep its original size");
        Check(secondRoom.Position == new Vector2(1920, 0), "The empty room must be directly to the right");

        var customer = game.GetNode<ModularCustomer>("World/Customer");
        Check(customer.GetAppearanceCount() == 6, "The customer must offer six appearances");
        foreach (string bodyName in new[] { "Male", "Female" })
        {
            Check(customer.GetNode<Sprite2D>($"{bodyName}/Body").Texture != null, "Each body needs its own art");
            Check(customer.GetNode<Sprite2D>($"{bodyName}/Clothes").Texture != null, "Each body needs a clothes layer");
            Check(customer.GetNode($"{bodyName}/Hair").GetChildCount() == 3, "Each body needs three hairstyles");
        }

        int[] prices = [4, 9, 18, 12];
        for (int i = 0; i < 100; i++)
        {
            game.RollOffer();
            int appearance = game.CustomerIndex;
            Check(appearance is >= 0 and < 6, "Every trade must select an appearance");
            Check(customer.GetNode<Node2D>("Male").Visible == (appearance < 3), "The chosen body must be visible");
            Check(customer.GetNode<Node2D>("Female").Visible == (appearance >= 3), "The other body must be hidden");
            string hairPath = appearance < 3 ? "Male/Hair" : "Female/Hair";
            var hair = customer.GetNode(hairPath);
            for (int style = 0; style < 3; style++)
                Check(((Sprite2D)hair.GetChild(style)).Visible == (style == appearance % 3), "Only the selected hairstyle must be visible");
            Check(game.Percentage is >= -30 and <= 30, "Offer percentage is out of range");
            Check(game.OfferedPrice == TradeScene.PriceForPercent(prices[game.ItemIndex], game.Percentage),
                "Offer price does not match the percentage");
        }

        int acceptedOffer = game.OfferedPrice;
        Check(game.GetNode<Label>("World/Item/TradePanel/Content/OfferLabel").Text == $"Получим: {acceptedOffer}$",
            "The panel must show the amount received");
        game.AcceptTrade();
        Check(game.Accepted == 1 && game.Score == acceptedOffer, "Accepting must record the sale");
        Check(!game.TradeActive && !game.GetNode<CounterWorld>("World").ItemVisible, "The item must disappear after acceptance");
        Check(!game.GetNode<PanelContainer>("World/Item/TradePanel").Visible, "The panel must disappear after acceptance");
        game.AcceptTrade();
        Check(game.Accepted == 1, "An inactive trade must not be accepted twice");
        var key = new InputEventKey { Keycode = Key.R, Pressed = true };
        for (int i = 0; i < 20; i++)
        {
            game._Input(key);
            Check(game.TradeActive && game.GetNode<CounterWorld>("World").ItemVisible, "R must create a trade without a limit");
        }
        int scoreBeforeDecline = game.Score;
        game.DeclineTrade();
        Check(game.Declined == 1 && game.Score == scoreBeforeDecline, "Declining must not change the score");
        game._Input(key);
        Check(game.TradeActive, "R must create a trade after declining");

        var camera = game.GetNode<Camera2D>("Camera2D");
        float oldZoom = camera.Zoom.X;
        game._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Position = new Vector2(640, 360), Pressed = true });
        Check(camera.Zoom.X > oldZoom, "Mouse wheel must zoom the camera");
        float startingX = camera.Position.X;
        game._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = new Vector2(800, 500), Pressed = true });
        game._Input(new InputEventMouseMotion { Relative = new Vector2(100, 0) });
        Check(Mathf.IsEqualApprox(camera.Position.X, startingX - 100f / camera.Zoom.X), "Dragging must pan the camera");
        camera.Position = new Vector2(-100, -100);
        game.ClampCamera();
        Check(camera.Position.X >= 0 && camera.Position.Y >= 0, "The camera must stay inside the scene");
        camera.Position = new Vector2(3000, 540);
        game.ClampCamera();
        Check(camera.Position.X > 1920, "The camera must be able to reach the empty room");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Trade scene checks passed");
    }

    private async Task TestProgress()
    {
        string absolutePath = ProjectSettings.GlobalizePath(SavePath);
        if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(absolutePath);
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = await CreateGame(scene);
        for (int i = 0; i < 40; i++)
        {
            game.RollOffer();
            Check(game.RerollsRemaining == 3, "Every item must start with three rerolls");
        }

        game.ItemIndex = 0;
        game.CustomerIndex = 0;
        game.Percentage = -30;
        game.RerollsRemaining = 3;
        game.TradeActive = true;
        game.ShowTrade();
        foreach (int remaining in new[] { 2, 1, 0 })
        {
            int oldCustomer = game.CustomerIndex;
            int oldPrice = game.OfferedPrice;
            game.RerollCustomer();
            Check(game.ItemIndex == 0 && game.CustomerIndex != oldCustomer, "Reroll must keep the item and replace the buyer");
            Check(game.OfferedPrice != oldPrice, "Reroll must change the actual price");
            Check(game.RerollsRemaining == remaining, "Reroll must consume one attempt");
        }
        game.RerollCustomer();
        Check(game.RerollsRemaining == 0, "Reroll limit must be enforced");
        Check(game.GetNode<Button>("World/Item/TradePanel/Content/RerollButton").Disabled, "Reroll button must disable at zero");

        game.CurrentDay = 4;
        game.Score = game.FirstQuota - 1;
        game.EndDay();
        Check(game.Failed && !game.TradeActive, "Missing the quota must stop trades");
        game.RollOffer();
        Check(!game.TradeActive, "R must not bypass quota failure");
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 1 && game.Score == 0, "First failure must restart from day one");

        game.CurrentDay = 4;
        game.Score = game.FirstQuota + 12;
        game.EndDay();
        Check(game.QuotaStage == 1 && game.CurrentDay == 5, "Paying the first quota must open day five");
        Check(game.Score == 12 && game.CheckpointScore == 12, "Quota must be deducted and remaining cash saved");
        int savedItem = game.ItemIndex;
        int savedRerolls = game.RerollsRemaining;
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.CurrentDay == 5 && game.Score == 12, "Progress must survive a scene reload");
        Check(game.ItemIndex == savedItem && game.RerollsRemaining == savedRerolls, "The current offer must survive a reload");
        game.CurrentDay = 6;
        game.Score = 1000;
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 5 && game.Score == 12, "Checkpoint must restore day and cash");

        game.CurrentDay = 7;
        game.Score = game.SecondQuota;
        game.EndDay();
        Check(game.Completed && game.QuotaStage == 2, "Paying the second quota must finish the game");
        Check(game.Score == 0 && !game.TradeActive, "Final quota must be deducted and trades stopped");
        game.NewGame();
        Check(game.CurrentDay == 1 && game.Score == 0 && game.TradeActive, "New game must reset progress");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);

        using (var oldSave = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
            oldSave.StoreString("{\"trade_active\":true,\"item_index\":2,\"customer_index\":1,\"percentage\":0,\"rerolls_remaining\":1}");
        game = await CreateGame(scene);
        Check(game.RerollsRemaining == 3, "An older expensive-item save must gain the new reroll limit");
        Check(game.CustomerIndex == 3, "An older female customer must keep the female body");
        game.RerollCustomer();
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.RerollsRemaining == 2, "A current save must preserve spent rerolls");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        DirAccess.RemoveAbsolute(absolutePath);
        GD.Print("Reroll and quota checks passed");
    }

    private async Task<TradeScene> CreateGame(PackedScene scene)
    {
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = SavePath;
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        return game;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
