using System;
using System.Threading.Tasks;
using Godot;

// Root-owned so the black cover survives removal of the outgoing scene.
public partial class SceneTransition : CanvasLayer
{
    public const double FadeOutSeconds = 0.22;
    public const double FadeInSeconds = 0.25;
    private ColorRect _cover;
    private Tween _tween;
    private TaskCompletionSource<bool> _fade;
    private bool _exiting;
    public float Opacity => _cover.Color.A;
    public static bool IsActive(SceneTree tree) => tree.Root.GetNodeOrNull<SceneTransition>("SceneTransition") != null;

    public override void _Ready()
    {
        Layer = 1000;
        ProcessMode = ProcessModeEnum.Always;
        _cover = new ColorRect
        {
            Name = "Black", Color = new Color(0, 0, 0, 0),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        AddChild(_cover);
        _cover.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public override void _Input(InputEvent input) => GetViewport().SetInputAsHandled();

    private Task<bool> FadeTo(float opacity, double seconds)
    {
        _fade = new TaskCompletionSource<bool>();
        _tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _tween.TweenProperty(_cover, "color:a", opacity, seconds);
        _tween.Finished += () => _fade?.TrySetResult(true);
        return _fade.Task;
    }

    public static async Task<bool> Run(Node owner, Func<Task<bool>> change, bool keepChangedPause = false)
    {
        var tree = owner.GetTree();
        if (tree == null || owner.IsQueuedForDeletion() || IsActive(tree)) return false;
        var transition = new SceneTransition { Name = "SceneTransition" };
        tree.Root.AddChild(transition);
        bool paused = tree.Paused;
        bool pauseReleased = false;
        tree.Paused = true;
        try
        {
            if (!await transition.FadeTo(1, FadeOutSeconds)) return false;
            if (!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree() || owner.IsQueuedForDeletion()) return false;
            bool changed = await change();
            // Initialize the destination with its normal pause state, so a restored
            // crafting/card modal can take ownership of its own pause.
            // In-place story modals instead explicitly set their destination pause.
            if (!keepChangedPause) tree.Paused = paused;
            pauseReleased = true;
            // ChangeSceneToNode installs the new scene at the end of the frame.
            // Let deferred initialization and layout settle behind the opaque cover.
            for (int frame = 0; frame < 3; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (!await transition.FadeTo(0, FadeInSeconds)) return false;
            return changed;
        }
        finally
        {
            if (!pauseReleased) tree.Paused = paused;
            if (GodotObject.IsInstanceValid(transition) && !transition._exiting && transition.IsInsideTree())
            {
                tree.Root.RemoveChild(transition);
                transition.QueueFree();
            }
        }
    }

    public override void _ExitTree()
    {
        _exiting = true;
        if (_tween?.IsValid() == true) _tween.Kill();
        _fade?.TrySetResult(false);
    }
}
