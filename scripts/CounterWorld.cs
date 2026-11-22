using Godot;
using System;

public partial class CounterWorld : Node2D
{
    // Contact point in the cashier background's pixel coordinates, on the clear part of the counter.
    [Export] public Vector2 CounterItemContact { get; set; } = new(930, 744);
    private Node2D _item = null!;
    private string _itemKind = "hat";
    private bool _itemVisible = false;

    public string ItemKind
    {
        get => _itemKind;
        set
        {
            _itemKind = value;
            if (IsNodeReady()) RefreshItem();
        }
    }

    public bool ItemVisible
    {
        get => _itemVisible;
        set
        {
            _itemVisible = value;
            if (IsNodeReady()) _item.Visible = value;
        }
    }

    public override void _Ready()
    {
        _item = GetNode<Node2D>("TradingRoom/Item");
        var background = GetNode<Sprite2D>("TradingRoom/TradingRoomBackground");
        Vector2 contact = _item.ToLocal(background.ToGlobal(CounterItemContact));
        foreach (CraftItem item in WorkshopItemCatalog.Items)
        {
            Texture2D texture = GD.Load<Texture2D>(item.ArtPath);
            using var image = texture.GetImage();
            Rect2I ink = image.GetUsedRect();
            if (ink.Size == Vector2I.Zero) continue;
            float scale = Math.Min(232f / ink.Size.X, 224f / ink.Size.Y);
            scale *= item.Kind switch
            {
                "hat" or "vest" or "gloves" => 0.75f,
                "drum" => 0.65f,
                _ => 1f
            };
            var sprite = new Sprite2D { Name = item.Kind, Texture = texture, Scale = Vector2.One * scale, Visible = false };
            Vector2 bottomCenter = (Vector2)ink.Position + new Vector2(ink.Size.X / 2f, ink.Size.Y);
            Vector2 localContact = sprite.GetRect().Position + bottomCenter;
            sprite.Position = contact - localContact * scale;
            _item.AddChild(sprite);
        }
        RefreshItem();
    }

    private void RefreshItem()
    {
        foreach (var child in _item.GetChildren())
        {
            if (child is Sprite2D variant)
                variant.Visible = variant.Name == _itemKind;
        }
        _item.Visible = _itemVisible;
    }
}
