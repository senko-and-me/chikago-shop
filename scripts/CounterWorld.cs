using Godot;
using System;
using System.Linq;

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
        foreach (CraftItem item in WorkshopItemCatalog.Items)
        {
            // Reuse scene-owned variants and fill only the existing catalogue's missing kinds.
            if (_item.GetChildren().OfType<Sprite2D>().Any(sprite =>
                sprite.Name.ToString().Equals(item.Kind, StringComparison.OrdinalIgnoreCase))) continue;
            Texture2D texture = GD.Load<Texture2D>(item.ArtPath);
            float scale = Math.Min(128f / texture.GetWidth(), 128f / texture.GetHeight());
            _item.AddChild(new Sprite2D
            {
                Name = item.Kind, Texture = texture, Scale = Vector2.One * scale,
                Position = new Vector2(64, -48), Visible = false
            });
        }
        var background = GetNode<Sprite2D>("TradingRoom/TradingRoomBackground");
        Vector2 contact = _item.ToLocal(background.ToGlobal(CounterItemContact));
        foreach (var sprite in _item.GetChildren().OfType<Sprite2D>())
        {
            if (sprite.Texture == null) continue;
            using var image = sprite.Texture.GetImage();
            Rect2I ink = image.GetUsedRect();
            if (ink.Size == Vector2I.Zero) continue;
            // Align the artwork itself: transparent padding differs between catalogue items.
            Vector2 bottomCenter = (Vector2)ink.Position + new Vector2(ink.Size.X / 2f, ink.Size.Y);
            Vector2 localContact = sprite.GetRect().Position + bottomCenter;
            sprite.Position = contact - sprite.Transform.BasisXform(localContact);
        }
        RefreshItem();
    }

    private void RefreshItem()
    {
        foreach (var child in _item.GetChildren())
        {
            if (child is Sprite2D variant)
                variant.Visible = variant.Name.ToString().ToLowerInvariant() == _itemKind;
        }
        _item.Visible = _itemVisible;
    }
}
