using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Crafting;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{





    private void PushEmbroideryClick(Vector2 point)
    {
        Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
        Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = false }, true);
    }

    private static void CraftMusic(TradeScene game)
    {
        if (!game.DayStarted) game.EndDay();
        game.GetItemFromMusicWorkshop();
        FinishMusic(game);
    }

    private static void FinishMusic(TradeScene game)
    {
        var polishing = game.GetNode<EmbroideryGame>("PolishingGame");
        if (!polishing.IsOpen) return;
        polishing.Activate();
        polishing._Process(10);
        polishing.Activate();
    }

    private static void CraftAtelier(TradeScene game)
    {
        if (!game.DayStarted) game.EndDay();
        game.GetItemFromAtelier();
        FinishEmbroidery(game);
    }

    private static void FinishEmbroidery(TradeScene game)
    {
        var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
        if (!embroidery.IsOpen) return;
        embroidery.Activate();
        embroidery._Process(10);
        embroidery.Activate();
    }
}
