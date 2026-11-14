using Godot;

// Artwork and both captions are placed in CraftButton.tscn; localization only switches them.
public static class CraftButtonPresentation
{
    public static void Apply(Button button, bool english)
    {
        button.Text = "";
        button.GetNode<Control>("EnglishCaption").Visible = english;
        button.GetNode<Control>("RussianCaption").Visible = !english;
    }
}
