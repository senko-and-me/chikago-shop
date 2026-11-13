using System;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestInventoryExpansion()
    {
        const string save = "user://inventory_expansion_test.json";
        Vector2I originalSize = Root.Size;
        TradeScene game = null;
        TradeScene Create()
        {
            var instance = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            instance.SavePath = save;
            Root.AddChild(instance);
            instance.SetProcess(false);
            instance.GetNode<Camera2D>("Camera2D").SetProcess(false);
            return instance;
        }
        async Task Wait(double seconds) => await ToSignal(CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        try
        {
            Root.Size = new Vector2I(1280, 720);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            game = Create();
            await Wait(0.1);
            game.GetItemFromAtelier();
            FinishEmbroidery(game);
            int itemCount = game.StoredItems.Length;
            game.Score = 500;
            var inventory = game.GetNode<InventoryUI>("HUD/InventoryUI");
            var layer = inventory.GetNode<Control>("ExpansionAnimation");
            var background = inventory.GetNode<TextureRect>("InventorySlotBackground");
            float startX = background.Position.X;
            game.TryUnlockMusicWorkshop();
            Check(inventory.IsExpanding && layer.GetChildCount() == 6 && inventory.Container.GetChildCount() == 6,
                "An in-game capacity increase must animate six visual slots while the real inventory already has its final capacity");
            Check(layer.GetNode<Control>("Arrival0").Position.X == startX &&
                layer.GetNode<Control>("Arrival3").Position.Y > Root.GetVisibleRect().Size.Y,
                "Existing slots must begin at their old positions; new slots must begin below the screen");
            Check(layer.GetNode<TextureRect>("Arrival0/Icon").Texture != null &&
                layer.GetNode<TextureRect>("Arrival0/Icon").Texture == inventory.Container.GetChild(0).GetNode<TextureRect>("Icon").Texture,
                "Existing item artwork must move together with its animated slot");
            await Wait(0.18);
            float movingX = layer.GetNode<Control>("Arrival0").Position.X;
            Check(movingX < startX && movingX > background.Position.X,
                "Existing slots must smoothly slide left instead of jumping to the expanded layout");
            double deadline = Time.GetTicksMsec() + 2000;
            while (layer.GetNode<Control>("Arrival3").Position.Y == layer.GetNode<Control>("Arrival4").Position.Y)
            {
                Check(Time.GetTicksMsec() < deadline, "The first arrival must start within the animation deadline");
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Check(layer.GetNode<Control>("Arrival3").Position.Y < layer.GetNode<Control>("Arrival4").Position.Y &&
                layer.GetNode<Control>("Arrival4").Position.Y > Root.GetVisibleRect().Size.Y,
                "The first arrival must start moving before the second slot leaves the bottom of the screen");
            while (layer.GetNode<Control>("Arrival4").Position.Y == layer.GetNode<Control>("Arrival5").Position.Y)
            {
                Check(Time.GetTicksMsec() < deadline, "The second arrival must follow within the animation deadline");
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Check(layer.GetNode<Control>("Arrival4").Position.Y < layer.GetNode<Control>("Arrival5").Position.Y,
                "The second arrival must precede the third");
            using (var image = await CapturePresentation("inventory-expansion")) { }
            await Wait(0.7);
            Check(!inventory.IsExpanding && layer.GetChildCount() == 0 && inventory.Scroller.Modulate == Colors.White &&
                background.Modulate == Colors.White && game.StoredItems.Length == itemCount && game.Score == 400,
                "Finishing the expansion must reveal the actual inventory without losing items or charging extra");
            game.NewGame();
            game.Score = 500;
            game.TryUnlockMusicWorkshop();
            game.NewGame();
            await Wait(0.1);
            Check(!inventory.IsExpanding && inventory.Container.GetChildCount() == 3 && layer.GetChildCount() == 0 &&
                background.Modulate == Colors.White, "NewGame must cancel the expansion and restore the three-slot layout");
            game.Score = 500;
            game.TryUnlockMusicWorkshop();
            Root.Size = new Vector2I(1000, 800);
            await Wait(0.1);
            Check(!inventory.IsExpanding && inventory.Scroller.Modulate == Colors.White,
                "Resizing during arrival must settle the final layout and remove stale animation copies");
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = Create();
            inventory = game.GetNode<InventoryUI>("HUD/InventoryUI");
            await Wait(0.1);
            Check(game.MusicWorkshopUnlocked && !inventory.IsExpanding && inventory.Container.GetChildCount() == 6,
                "Loading an expanded inventory must show six settled slots without replaying an unlock animation");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            Root.Size = originalSize;
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
