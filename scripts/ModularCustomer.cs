using Godot;

public partial class ModularCustomer : Node2D
{
	private const int HairVariants = 3;
	private Node2D[] _variants = null!;

	public override void _Ready() => _variants = [GetNode<Node2D>("Male"), GetNode<Node2D>("Female")];

	public int GetAppearanceCount() => _variants.Length * HairVariants;

	public void SetAppearance(int appearanceIndex)
	{
		int bodyIndex = appearanceIndex / HairVariants;
		int hairIndex = appearanceIndex % HairVariants;
		for (int index = 0; index < _variants.Length; index++)
		{
			Node2D body = _variants[index];
			body.Visible = index == bodyIndex;
			Node2D hair = body.GetNode<Node2D>("Hair");
			for (int styleIndex = 0; styleIndex < hair.GetChildCount(); styleIndex++)
				hair.GetChild(styleIndex).Set("visible", styleIndex == hairIndex);
		}
	}
}
