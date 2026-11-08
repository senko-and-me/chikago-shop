
using System;
using Godot;

public sealed class DayNightBackground
{
	private const string DayTexturePath = "res://art/rooms/day_time.png";
	private const string NightTexturePath = "res://art/rooms/night_time.png";

	private readonly Sprite2D _dayLayer;

	public DayNightBackground(TradeScene game)
	{
		Node2D world = game.GetNode<Node2D>("World");
		Sprite2D layout = world.GetNode<Sprite2D>("RoomLayout");

		Texture2D dayTexture = GD.Load<Texture2D>(DayTexturePath);
		Texture2D nightTexture = GD.Load<Texture2D>(NightTexturePath);

		if (dayTexture == null || nightTexture == null || layout.Texture == null)
		{
			GD.PushWarning("day_time.png or night_time.png is missing.");
			return;
		}

		Vector2 backgroundSize = layout.Texture.GetSize() * layout.Scale;

		Sprite2D nightLayer = CreateLayer(
			"TimeOfDayNight",
			nightTexture,
			layout,
			backgroundSize
		);

		world.AddChild(nightLayer);
		world.MoveChild(nightLayer, layout.GetIndex() + 1);

		_dayLayer = CreateLayer(
			"TimeOfDayDay",
			dayTexture,
			layout,
			backgroundSize
		);

		world.AddChild(_dayLayer);
		world.MoveChild(_dayLayer, nightLayer.GetIndex() + 1);
	}

	private static Sprite2D CreateLayer(
		string name,
		Texture2D texture,
		Sprite2D layout,
		Vector2 size)
	{
		return new Sprite2D
		{
			Name = name,
			Texture = texture,
			Position = layout.Position,
			Rotation = layout.Rotation,
			Centered = layout.Centered,
			Offset = layout.Offset,
			Scale = new Vector2(
				size.X / texture.GetWidth(),
				size.Y / texture.GetHeight()
			),
			ZIndex = layout.ZIndex,
			Modulate = Colors.White
		};
	}

	public void Update(double remainingSeconds, double durationSeconds)
	{
		if (_dayLayer == null)
			return;

		float alpha = durationSeconds > 0
			? (float)Math.Clamp(
				remainingSeconds / durationSeconds,
				0,
				1
			)
			: 0f;

		_dayLayer.Modulate = new Color(1f, 1f, 1f, alpha);
	}
}
