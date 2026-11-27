using Godot;

// Scene-authored artwork shares one paper tint, including the help icon's paper.
public partial class CraftButtonVisual : Button
{
	private Control[] _artwork;
	private Color _tint = Colors.White;

	public override void _Ready()
	{
		_artwork = new[] { GetNode<Control>("Paper"), GetNode<Control>("Frame"),
			GetNode<Control>("RussianCaption"), GetNode<Control>("EnglishCaption"),
			GetNode<Control>("RarityHotspot/EnglishHelp") };
		// The artwork draws its own opaque face; the native button style is empty.
		ButtonDropShadow.Attach(this, solidFace: true);
	}

	public override void _Process(double delta)
	{
		Color tint = GetDrawMode() switch
		{
			DrawMode.Disabled => new Color(0.48f, 0.48f, 0.48f),
			DrawMode.Pressed or DrawMode.HoverPressed => new Color(0.78f, 0.78f, 0.78f),
			DrawMode.Hover => new Color(0.9f, 0.9f, 0.9f),
			_ => Colors.White
		};
		_tint = GetDrawMode() == DrawMode.Disabled ? tint : _tint.Lerp(tint, Mathf.Clamp((float)delta * 25, 0, 1));
		foreach (var item in _artwork) item.Modulate = _tint;
	}
}
