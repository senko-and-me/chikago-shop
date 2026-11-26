using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestCounterItemPlacement()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        var item = game.GetNode<Node2D>("World/TradingRoom/Item");
        Vector2I originalWindow = Root.Size;
        Root.AddChild(game);
        game.SetProcess(false);
        var sprites = item.GetChildren().OfType<Sprite2D>().ToArray();
        Check(sprites.Length == 10, "Each catalogue item must have a counter sprite");
        var camera = game.GetNode<WorldCamera>("Camera2D");
        camera.SetProcess(false);
        var world = game.GetNode<CounterWorld>("World");
        var background = game.GetNode<Sprite2D>("World/TradingRoom/TradingRoomBackground");
        var panel = item.GetNode<Control>("TradePanel");
        Vector2 panelPosition = panel.Position;
        try
        {
            world.ItemVisible = true;
            panel.Show();
            foreach (var window in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (float zoom in new[] { 0.7f, 1.4f })
            {
                Root.Size = window;
                camera.Zoom = Vector2.One * zoom;
                camera.Position = background.ToGlobal(new Vector2(700, 590));
                camera.ForceUpdateScroll();
                foreach (var sprite in sprites)
                {
                    world.ItemKind = sprite.Name.ToString().ToLowerInvariant();
                    Check(sprite.Visible && item.GetChildren().OfType<Sprite2D>().Count(value => value.Visible) == 1,
                        "Changing the trade item must show exactly one catalogue variant");
                    using var image = sprite.Texture.GetImage();
                    Rect2I ink = image.GetUsedRect();
                    Vector2 localBottom = sprite.GetRect().Position + (Vector2)ink.Position +
                        new Vector2(ink.Size.X / 2f, ink.Size.Y);
                    Vector2 actual = sprite.ToGlobal(localBottom);
                    Vector2 expected = background.ToGlobal(new Vector2(930, 744));
                    Check(actual.DistanceTo(expected) < 0.1f,
                        $"{sprite.Name}: the visible bottom edge must touch the cashier countertop, actual={actual}, expected={expected}");
                    float expectedScale = Math.Min(232f / ink.Size.X, 224f / ink.Size.Y);
                    expectedScale *= sprite.Name.ToString() switch
                    {
                        "hat" or "vest" or "gloves" => 0.75f,
                        "drum" => 0.65f,
                        _ => 1f
                    };
                    bool fitted = Mathf.IsEqualApprox(sprite.Scale.X, expectedScale) && sprite.Scale.X == sprite.Scale.Y;
                    Check(fitted && panel.Position == panelPosition,
                        "Counter alignment must preserve authored scales, fit new art and keep the independent trade panel position");
                    Rect2 visibleBounds = sprite.GlobalTransform * new Rect2(sprite.GetRect().Position + (Vector2)ink.Position, ink.Size);
                    Check(!panel.GetGlobalRect().Intersects(visibleBounds), "The trade panel must leave the counter item visible");
                    if (window.X == 1280 && zoom > 1)
                    {
                        await ToSignal(CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
                        Check(sprite.IsVisibleInTree(), "The sampled counter item must stay visible while rendering");
                        using var frame = await CapturePresentation();
                        frame.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chikago-hf163-{sprite.Name}.png"));
                    }
                }
            }
            world.ItemVisible = false;
            Check(!item.Visible, "Ending item presentation must still hide the item and its trade panel together");
        }
        finally
        {
            game.QueueFree();
            Root.Size = originalWindow;
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }


}
