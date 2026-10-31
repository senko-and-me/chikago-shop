using Godot;

public partial class RarityPanel : PanelContainer
{
    [Export] public int Price0 { get; set; } = 30;
    [Export] public int Price1 { get; set; } = 50;
    [Export] public int Price2 { get; set; } = 80;
    [Export] public int Price3 { get; set; } = 120;
    [Export] public int Price4 { get; set; } = 200;

    public override void _Ready()
    {
        if (GetParent().Name == "MusicWorkshop")
        {
            Price0 = 36;
            Price1 = 60;
            Price2 = 96;
            Price3 = 144;
            Price4 = 240;
        }

        int[] prices = [Price0, Price1, Price2, Price3, Price4];
        for (int i = 0; i < prices.Length; i++)
            GetNode<Label>($"Values/Price{i}").Text = $"{prices[i]}$";
    }
}
