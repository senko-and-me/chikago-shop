using Godot;

public partial class CounterWorld : Node2D
{
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
            if (!item.ArtPath.Contains("placeholder_")) continue;
            _item.AddChild(new Sprite2D
            {
                Name = item.Kind, Texture = GD.Load<Texture2D>(item.ArtPath),
                Position = new Vector2(64, -48), Visible = false
            });
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
