using Godot;

// Focus and framing are authored alongside the tutorial, independently of its state logic.
public partial class TutorialCameraCheckpoint : Marker2D
{
    [Export] public NodePath FocusTarget { get; set; }
    [Export] public Vector2 ReferenceViewSize { get; set; } = new(1600, 900);
    [Export] public bool Overview { get; set; }

    public void MoveCamera(Node scene, WorldCamera camera, double duration)
    {
        if (Overview) { camera.ReturnToOverview(duration); return; }
        var target = scene.GetNode<Node2D>(FocusTarget);
        Vector2 center = target is Sprite2D sprite
            ? sprite.GlobalTransform * sprite.GetRect().GetCenter() : target.GlobalPosition;
        camera.FocusOn(center + Position, ReferenceViewSize, duration);
    }
}
