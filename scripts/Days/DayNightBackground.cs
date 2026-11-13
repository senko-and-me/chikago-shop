using System;
using Godot;

// The scene owns both background layers and their layout; runtime only blends daylight.
public sealed class DayNightBackground
{
	private readonly Sprite2D _dayLayer;

	public DayNightBackground(TradeScene game)
	{
		_dayLayer = game.GetNode<Sprite2D>("World/TimeOfDayDay");
	}

	public void Update(double remainingSeconds, double durationSeconds)
	{
		float alpha = durationSeconds > 0
			? (float)Math.Clamp(remainingSeconds / durationSeconds, 0, 1)
			: 0f;
		_dayLayer.Modulate = new Color(1f, 1f, 1f, alpha);
	}
}
