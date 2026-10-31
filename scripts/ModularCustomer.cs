using System;
using Godot;

public partial class ModularCustomer : Node2D
{
	private const int HairVariants = 3;
	private const int ClothesVariants = 3;
	private const int LegacyAppearanceCount = 6;
	private Node2D[] _variants = null!;

	public override void _Ready() => _variants = [GetNode<Node2D>("Male"), GetNode<Node2D>("Female")];

	public int GetAppearanceCount() => _variants.Length * HairVariants * ClothesVariants;

	public void SetAppearance(int appearanceIndex)
	{
		if (appearanceIndex < 0 || appearanceIndex >= GetAppearanceCount())
			throw new ArgumentOutOfRangeException(nameof(appearanceIndex));

		int bodyAndHairIndex = appearanceIndex % LegacyAppearanceCount;
		int bodyIndex = bodyAndHairIndex / HairVariants;
		int hairIndex = bodyAndHairIndex % HairVariants;
		int clothesIndex = appearanceIndex / LegacyAppearanceCount;
		for (int index = 0; index < _variants.Length; index++)
		{
			Node2D body = _variants[index];
			body.Visible = index == bodyIndex;
			Node2D clothes = body.GetNode<Node2D>("Clothes");
			for (int styleIndex = 0; styleIndex < clothes.GetChildCount(); styleIndex++)
				clothes.GetChild<CanvasItem>(styleIndex).Visible = styleIndex == clothesIndex;
			Node2D hair = body.GetNode<Node2D>("Hair");
			for (int styleIndex = 0; styleIndex < hair.GetChildCount(); styleIndex++)
				hair.GetChild<CanvasItem>(styleIndex).Visible = styleIndex == hairIndex;
		}
	}
}
