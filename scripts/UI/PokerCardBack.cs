using Godot;

// The same hand-drawn card back is used for the deck and every dealt card.
public partial class PokerCardBack : Control
{
    [Export] public bool IsDeck { get; set; }
    private Texture2D _texture = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _texture = GD.Load<Texture2D>("res://art/ui/card_back.png");
        Resized += QueueRedraw;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Rect2 bounds = !IsDeck && GetParent() is PanelContainer card
            ? new Rect2(-Position, card.Size) : new Rect2(Vector2.Zero, Size);
        int layers = IsDeck ? 3 : 1;
        for (int layer = layers - 1; layer >= 0; layer--)
        {
            DrawTextureRect(_texture, new Rect2(bounds.Position + new Vector2(layer * 4, layer * -4), bounds.Size), false);
        }
    }
}
