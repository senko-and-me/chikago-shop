using System;
using System.Linq;
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

    private async Task TestTradeScene()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn")
            ?? throw new Exception("The trade scene must load");
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);

        var world = game.GetNode<CounterWorld>("World");
        var wall = game.GetNode<ColorRect>("World/Wall");
        Check(wall.Size == new Vector2(3840, 2160), "The world must retain all four rooms");
        var salesRoom = game.GetNode<Sprite2D>("World/TradingRoomBackground");
        var roomTexture = salesRoom.Texture ?? throw new Exception("The room background must load");
        Check(roomTexture.GetSize() == new Vector2(1920, 1080) && salesRoom.Position == new Vector2(0, 1080),
            "The sales room must be below the atelier");
        var showroom = game.GetNode<Sprite2D>("World/ShowroomBackground");
        Check(showroom.Texture != null && showroom.Position == new Vector2(1920, 1080),
            "The showroom must fill the lower-right room");
        Check(game.GetNode<Node2D>("World/Showcases").GetChildCount() == 6,
            "The showroom must have six showcases");
        var atelierButton = game.GetNode<Button>("World/Atelier/GetItemButton");
        var musicButton = game.GetNode<Button>("World/MusicWorkshop/GetItemButton");
        var placeButton = game.GetNode<Button>("World/PlaceItemButton");
        var trashButton = game.GetNode<Button>("World/TrashBin");
        foreach (string kind in new[] { "Hat", "Coat", "Guitar", "Trumpet" })
            Check(game.GetNode<Sprite2D>($"World/Item/{kind}").Texture != null,
                $"The {kind} item must have an illustration");
        Check(!atelierButton.Disabled && !musicButton.Disabled, "Both workshops must provide items");
        Check(placeButton.Disabled && !game.TradeActive && game.StoredItems.Length == 0,
            "A new game must start without a free item to sell");

        var customer = game.GetNode<ModularCustomer>("World/Customer");
        Check(customer.GetAppearanceCount() == 6, "The customer must offer six appearances");
        foreach (string bodyName in new[] { "Male", "Female" })
        {
            Check(customer.GetNode<Sprite2D>($"{bodyName}/Body").Texture != null, "Each body needs art");
            Check(customer.GetNode<Sprite2D>($"{bodyName}/Clothes").Texture != null, "Each body needs clothes");
            Check(customer.GetNode($"{bodyName}/Hair").GetChildCount() == 3, "Each body needs three hairstyles");
        }

        var rKey = new InputEventKey { Keycode = Key.R, Pressed = true };
        for (int i = 0; i < 20; i++) game._Input(rKey);
        Check(game.StoredItems.Length == 0 && !game.TradeActive, "R must never create an item or a trade");

        atelierButton.EmitSignal(Button.SignalName.Pressed);
        musicButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.StoredItems.Length == 2 && game.StoredItems[0] is >= 0 and <= 1 &&
              game.StoredItems[1] is >= 2 and <= 3,
            "Each workshop must only produce items from its category");
        int firstItem = game.StoredItems[0];
        int secondItem = game.StoredItems[1];
        Check(placeButton.Disabled, "Placing requires an inventory selection");
        SelectInventorySlot(game, 0);
        Check(!placeButton.Disabled, "Placing must become available after selecting an item");
        placeButton.EmitSignal(Button.SignalName.Pressed);
        Check(!game.TradeActive && game.ShowcaseItems[0] == firstItem &&
              game.StoredItems.SequenceEqual(new[] { secondItem }),
            "Placing must move the selected item into a showcase");
        Check(game.GetNode<TextureRect>("World/Showcases/Slot1/Icon").Texture != null,
            "The showcase must display the item's illustration");
        game._Process(game.BuyerWaitSeconds / 2d);
        Check(!game.TradeActive, "A buyer must wait before choosing a showcase item");
        game._Process(game.BuyerWaitSeconds / 2d + 0.1);
        Check(game.TradeActive && game.ItemIndex == firstItem && game.ShowcaseItems.All(item => item == -1),
            "A buyer must choose a displayed item after the wait");
        Check(world.ItemVisible && placeButton.Visible &&
              game.GetNode<Button>("World/Showcases/Slot1").Disabled &&
              game.GetNode<Label>("World/Showcases/Slot1/ItemLabel").Text == "НА СДЕЛКЕ",
            "The active item's showcase must stay reserved during the trade");
        game.GetItemFromAtelier();
        SelectInventorySlot(game, 1);
        game.PlaceItemInShowcase(0);
        Check(game.StoredItems.Length == 2 && game.ShowcaseItems[0] == -1,
            "The reserved showcase must reject a new item");
        Check(!placeButton.Disabled, "Another showcase must remain available during a trade");
        placeButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.TradeActive && game.ShowcaseItems[1] >= 0 && game.StoredItems.SequenceEqual(new[] { secondItem }),
            "The player must be able to stock another showcase during a trade");
        game.GetNode<Button>("World/Showcases/Slot2").EmitSignal(Button.SignalName.Pressed);
        Check(!trashButton.Disabled, "A displayed item must be selectable for disposal");
        trashButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.TradeActive && game.ShowcaseItems[1] == -1 &&
              game.GetNode<Button>("World/Showcases/Slot1").Disabled,
            "Trash must remove a displayed item without releasing the reserved slot");
        foreach (string kind in new[] { "hat", "coat", "guitar", "trumpet" })
            Check(game.GetNode<Sprite2D>($"World/Item/{char.ToUpperInvariant(kind[0])}{kind[1..]}").Visible ==
                  (kind == TradeScene.GetItemKind(firstItem)), "Only the selected item illustration must be visible");
        int[] prices = [4, 9, 18, 12];
        Check(game.Percentage is >= -30 and <= 30, "Offer percentage is out of range");
        Check(game.OfferedPrice == TradeScene.PriceForPercent(prices[firstItem], game.Percentage),
            "Offer price does not match the percentage");
        Check(game.GetNode<Label>("World/Item/TradePanel/Content/OfferLabel").Text == $"Получим: {game.OfferedPrice}$",
            "The panel must show the amount received");

        game.Percentage = -30;
        game.ShowTrade();
        for (int remaining = 2; remaining >= 0; remaining--)
        {
            int oldCustomer = game.CustomerIndex;
            int oldPrice = game.OfferedPrice;
            game.RerollCustomer();
            Check(game.ItemIndex == firstItem && game.CustomerIndex != oldCustomer, "Reroll must keep the item and replace the buyer");
            Check(game.OfferedPrice != oldPrice && game.RerollsRemaining == remaining,
                "Reroll must change the price and consume one attempt");
        }
        Check(game.GetNode<Button>("World/Item/TradePanel/Content/RerollButton").Disabled,
            "Reroll button must disable at zero");

        game.DeclineTrade();
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(new[] { secondItem }) &&
              game.ShowcaseItems[0] == firstItem,
            "A declined item must return to its showcase");
        Check(!world.ItemVisible && placeButton.Visible, "The sale controls must hide after declining");
        game._Process(game.BuyerWaitSeconds + 0.1);
        int acceptedOffer = game.OfferedPrice;
        game.AcceptTrade();
        Check(game.Accepted == 1 && game.Score == acceptedOffer, "Accepting must record the sale");
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(new[] { secondItem }) &&
              game.ShowcaseItems.All(item => item == -1), "A sold item must leave the showcase permanently");
        game.GetItemFromAtelier();
        SelectInventorySlot(game, 1);
        Check(!trashButton.Disabled, "An inventory item must be selectable for disposal");
        trashButton.EmitSignal(Button.SignalName.Pressed);
        Check(game.StoredItems.SequenceEqual(new[] { secondItem }) && trashButton.Disabled,
            "Trash must remove only the selected inventory item");
        game.AcceptTrade();
        Check(game.Accepted == 1, "An inactive trade must not be accepted twice");
        game._Input(rKey);
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(new[] { secondItem }),
            "R must not bypass the stored-item queue");

        SelectInventorySlot(game, 0);
        game.GetNode<Button>("World/Showcases/Slot3").EmitSignal(Button.SignalName.Pressed);
        game.GetItemFromAtelier();
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(4);
        int[] displayed = game.ShowcaseItems;
        game._Process(game.BuyerWaitSeconds + 0.1);
        int[] remainingShowcases = game.ShowcaseItems;
        Check(game.TradeActive && displayed[2] >= 0 && displayed[4] >= 0 &&
              remainingShowcases.Count(item => item >= 0) == 1 &&
              ((remainingShowcases[2] == -1 && game.ItemIndex == displayed[2]) ||
               (remainingShowcases[4] == -1 && game.ItemIndex == displayed[4])),
            "A buyer must choose one of the occupied showcases");
        game.DeclineTrade();
        Check(game.ShowcaseItems.SequenceEqual(displayed), "Declining must restore the chosen showcase");

        var camera = game.GetNode<Camera2D>("Camera2D");
        Check(camera.Position.Y > 1080, "The camera must start in the sales room");
        float oldZoom = camera.Zoom.X;
        game._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Position = new Vector2(640, 360), Pressed = true });
        Check(Mathf.IsEqualApprox(camera.Zoom.X, oldZoom), "Wheel input must not jump to the target zoom");
        game._Process(1d / 60d);
        Check(camera.Zoom.X > oldZoom && camera.Zoom.X < oldZoom * 1.12f,
            "Mouse wheel zoom must advance smoothly between frames");
        float zoomAfterStep = camera.Zoom.X;
        game._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Position = new Vector2(640, 360), Pressed = true });
        game._Process(1d / 60d);
        Check(camera.Zoom.X < zoomAfterStep && camera.Zoom.X > oldZoom,
            "Zooming back out must also advance smoothly");
        float startingX = camera.Position.X;
        game._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
        game._Input(new InputEventMouseMotion { Relative = new Vector2(100, 0) });
        Check(Mathf.IsEqualApprox(camera.Position.X, startingX - 100f / camera.Zoom.X), "Dragging must pan the camera");
        camera.Zoom = Vector2.One * 1.8f;
        camera.Position = new Vector2(-100, -100);
        game.ClampCamera();
        Vector2 halfView = game.GetViewportRect().Size / (2f * camera.Zoom);
        Check(camera.Position.X - halfView.X >= -240.01f && camera.Position.X - halfView.X < -200f &&
              camera.Position.Y - halfView.Y >= -240.01f && camera.Position.Y - halfView.Y < -200f,
            "The camera must allow a small view beyond the outer room edges");
        camera.Position = new Vector2(3000, 540);
        game.ClampCamera();
        Check(camera.Position.X > 1920 && camera.Position.Y < 1080, "The camera must reach the upper-right workshop");
        game.ClampCamera();
        camera.Position = new Vector2(3000, 1620);
        game.ClampCamera();
        Check(camera.Position.X > 1920 && camera.Position.Y > 1080,
            "The camera must reach the lower-right showroom");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Trade scene checks passed");
    }

    private async Task TestProgress()
    {
        string absolutePath = ProjectSettings.GlobalizePath(SavePath);
        if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(absolutePath);
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn")
            ?? throw new Exception("The trade scene must load");
        TradeScene game = await CreateGame(scene);
        game.GetItemFromAtelier();
        game.GetItemFromMusicWorkshop();
        int[] itemsBeforePlacement = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(3);
        Check(!game.TradeActive && game.StoredItems.Length == 1 &&
              game.ShowcaseItems[3] == itemsBeforePlacement[0],
            "Placing must move the selected item into a showcase without starting a trade");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(!game.TradeActive && game.ShowcaseItems[3] == itemsBeforePlacement[0] &&
              game.StoredItems[0] == itemsBeforePlacement[1],
            "Showcase contents and inventory must survive a reload");
        game._Process(game.BuyerWaitSeconds + 0.1);
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0],
            "A buyer must start a trade after waiting");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0] && game.ShowcaseItems[3] == -1 &&
              game.GetNode<Button>("World/Showcases/Slot4").Disabled &&
              game.GetNode<Label>("World/Showcases/Slot4/ItemLabel").Text == "НА СДЕЛКЕ",
            "An active showcase trade must survive a reload");
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(5);
        Check(game.TradeActive && game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.ShowcaseItems[3] == -1,
            "Other showcases must remain available during a restored trade");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.TradeActive && game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.GetNode<Button>("World/Showcases/Slot4").Disabled,
            "Stocking during a trade must survive another reload");
        game.DeclineTrade();
        Check(game.ShowcaseItems[3] == itemsBeforePlacement[0] &&
              game.ShowcaseItems[5] == itemsBeforePlacement[1] && game.StoredItems.Length == 0,
            "Declining after a reload must restore the reserved item without moving other goods");

        game.CurrentDay = 4;
        game.Score = game.FirstQuota - 1;
        game.EndDay();
        Check(game.Failed && !game.TradeActive && game.ShowcaseItems[3] == itemsBeforePlacement[0] &&
              game.ShowcaseItems[5] == itemsBeforePlacement[1] && game.StoredItems.Length == 0,
            "Failure at the deadline must preserve both unsold showcase items");
        game.GetItemFromAtelier();
        game._Input(new InputEventKey { Keycode = Key.R, Pressed = true });
        Check(game.StoredItems.Length == 0,
            "Failure and R must not create items");
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 1 && game.Score == 0 && game.StoredItems.Length == 0 &&
              game.ShowcaseItems.All(item => item == -1),
            "First failure must restore the empty day-one checkpoint");

        game.GetItemFromAtelier();
        game.GetItemFromMusicWorkshop();
        int[] checkpointItems = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(2);
        game.CurrentDay = 4;
        game.Score = game.FirstQuota + 12;
        game.EndDay();
        Check(game.QuotaStage == 1 && game.CurrentDay == 5 && game.Score == 12,
            "Paying the first quota must open day five with the remaining cash");
        Check(game.StoredItems.SequenceEqual(new[] { checkpointItems[1] }) &&
              game.ShowcaseItems[2] == checkpointItems[0] && !game.TradeActive,
            "The checkpoint must retain unsold showroom items");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.CurrentDay == 5 && game.Score == 12 && game.ShowcaseItems[2] == checkpointItems[0] &&
              game.StoredItems.SequenceEqual(new[] { checkpointItems[1] }),
            "The showroom, inventory and progress must survive a scene reload");
        game.GetNode<Button>("World/Showcases/Slot3").EmitSignal(Button.SignalName.Pressed);
        game.GetNode<Button>("World/TrashBin").EmitSignal(Button.SignalName.Pressed);
        Check(game.ShowcaseItems[2] == -1, "Trash must remove the selected showroom item");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.ShowcaseItems[2] == -1 && game.StoredItems.SequenceEqual(new[] { checkpointItems[1] }),
            "A discarded showroom item must stay discarded after reload");
        game.ReturnToCheckpoint();
        Check(game.ShowcaseItems[2] == checkpointItems[0],
            "Returning to the checkpoint must restore its showroom contents");
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(4);
        game.CurrentDay = 6;
        game.Score = 1000;
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 5 && game.Score == 12 && !game.TradeActive &&
              game.StoredItems.SequenceEqual(new[] { checkpointItems[1] }) &&
              game.ShowcaseItems[2] == checkpointItems[0] && game.ShowcaseItems[4] == -1,
            "Checkpoint must restore cash, inventory and showroom items");

        game.CurrentDay = 7;
        game.Score = game.SecondQuota;
        game.EndDay();
        Check(game.Completed && game.QuotaStage == 2 && game.Score == 0,
            "Paying the second quota must finish the game");
        game.NewGame();
        Check(game.CurrentDay == 1 && game.Score == 0 && game.StoredItems.Length == 0 && !game.TradeActive &&
              game.ShowcaseItems.All(item => item == -1),
            "New game must reset progress without giving a free item");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);

        using (var oldSave = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
            oldSave.StoreString("{\"trade_active\":true,\"item_index\":2,\"customer_index\":1,\"percentage\":0,\"rerolls_remaining\":1}");
        game = await CreateGame(scene);
        Check(game.TradeActive && game.StoredItems.Length == 0, "An older active trade must be restored without duplicating its item");
        Check(game.RerollsRemaining == 3 && game.CustomerIndex == 3,
            "Old reroll limits and customer appearance must migrate");
        game.RerollCustomer();
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.RerollsRemaining == 2, "A current save must preserve spent rerolls");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        DirAccess.RemoveAbsolute(absolutePath);
        GD.Print("Inventory, save and quota checks passed");
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

    private async Task<TradeScene> CreateGame(PackedScene scene)
    {
        var game = scene.Instantiate<TradeScene>();
        game.SavePath = SavePath;
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        return game;
    }

    private static void SelectInventorySlot(TradeScene game, int index)
    {
        var container = game.GetNode<HBoxContainer>("HUD/InventoryUI/HBoxContainer");
        ((Button)container.GetChild(index)).EmitSignal(Button.SignalName.Pressed);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
