using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestNotebookRightEdgeHover()
    {
        Vector2I originalSize = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var notebook = game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
        notebook.SetProcess(false);
        async Task Move(Vector2 point)
        {
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            await ToSignal(this, SignalName.ProcessFrame);
            notebook._Process(0.3);
        }
        async Task Settle() => await ToSignal(CreateTimer(notebook.SlideSeconds + 0.05), SceneTreeTimer.SignalName.Timeout);
        Rect2 ScreenHover() => notebook.GetGlobalTransformWithCanvas() * notebook.HoverBounds;
        try
        {
            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            {
                Root.Size = size;
                await ToSignal(this, SignalName.ProcessFrame);
                notebook.Hide();
                notebook._Process(0);
                notebook.Show();
                await Move(new Vector2(-100, -100));
                Rect2 closed = ScreenHover();
                float right = Root.GetVisibleRect().End.X;
                Check(Mathf.IsEqualApprox(closed.End.X, right), "Closed notebook hover must reach the viewport edge after resizing");
                Check(Mathf.IsEqualApprox(notebook.HoverBounds.Position.X, notebook.ClosedX - 11.2f),
                    "Extending right must preserve the existing left margin");
                await Move(new Vector2(right - 1, closed.GetCenter().Y));
                Check(notebook.Expanded, "Hovering at the far right must open the notebook without clicking");
                await Settle();
                Rect2 open = ScreenHover();
                Rect2 page = notebook.GetGlobalTransformWithCanvas() * notebook.GetNode<TextureRect>("Page").GetRect();
                Check(Mathf.IsEqualApprox(open.End.X, right) && right - 1 > page.End.X,
                    "The open notebook trigger must cover the real gap after the paper ends");
                await Move(new Vector2(right - 1, open.GetCenter().Y));
                await Settle();
                notebook._Process(1);
                Check(notebook.Expanded, "Moving into the gap must keep an unpinned notebook open");
                await Move(new Vector2(right - 1, open.Position.Y - 20));
                Check(!notebook.Expanded, "Leaving above the trigger must still close the notebook");
                await Settle();
                await Move(new Vector2(right - 1, closed.GetCenter().Y));
                Check(notebook.Expanded, "Reentering at the edge must reopen the notebook");
                await Settle();
                notebook._Input(new InputEventMouseMotion { Position = new Vector2(-1, -1) });
                notebook._Process(1);
                Check(!notebook.Expanded, "Leaving the window must still close the unpinned notebook");
                await Settle();
            }
        }
        finally
        {
            game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
