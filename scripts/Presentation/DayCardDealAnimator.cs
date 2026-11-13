using System;
using System.Collections.Generic;
using Godot;

public sealed class DayCardDealAnimator(Node owner)
{
    private readonly List<Tween> _tweens = new();

    private Tween Tween()
    {
        var tween = owner.CreateTween().SetPauseMode(Godot.Tween.TweenPauseMode.Process);
        _tweens.Add(tween);
        return tween;
    }

    public void Cancel()
    {
        foreach (var tween in _tweens) if (tween.IsValid()) tween.Kill();
        _tweens.Clear();
    }

    public void Deal(IReadOnlyList<PanelContainer> cards, Control deck, ColorRect shade, Control panel, Action finished)
    {
        Cancel();
        Tween().TweenProperty(shade, "modulate:a", 1f, 0.25);
        Tween().TweenProperty(panel, "modulate:a", 1f, 0.2);
        Vector2 deckDestination = deck.Position;
        deck.Position = new Vector2(deckDestination.X, deck.GetViewportRect().Size.Y + deck.Size.Y);
        var entrance = Tween();
        entrance.TweenProperty(deck, "position", deckDestination, 0.42)
            .SetTrans(Godot.Tween.TransitionType.Cubic).SetEase(Godot.Tween.EaseType.Out);
        entrance.Parallel().TweenProperty(deck, "modulate:a", 1f, 0.2);
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            var content = card.GetNode<Control>("Content");
            var back = card.GetNode<Control>("Back");
            Vector2 destination = card.Position;
            card.PivotOffset = card.Size / 2;
            card.Position = deck.GlobalPosition + deck.Size / 2 - card.GetParent<Control>().GlobalPosition - card.Size / 2;
            card.Scale = Vector2.One * 0.22f;
            card.Rotation = -0.24f + i * 0.05f;
            card.Modulate = new Color(1, 1, 1, 0);
            content.Hide();
            back.Show();
            double delay = 0.55 + i * 0.14;
            var tween = Tween();
            tween.TweenInterval(delay);
            tween.TweenProperty(card, "position", destination, 0.48).SetTrans(Godot.Tween.TransitionType.Cubic).SetEase(Godot.Tween.EaseType.Out);
            tween.Parallel().TweenProperty(card, "scale", Vector2.One, 0.48).SetTrans(Godot.Tween.TransitionType.Cubic).SetEase(Godot.Tween.EaseType.Out);
            tween.Parallel().TweenProperty(card, "rotation", 0f, 0.48);
            tween.Parallel().TweenProperty(card, "modulate:a", 1f, 0.12);
            tween.TweenProperty(card, "scale:x", 0f, 0.09).SetTrans(Godot.Tween.TransitionType.Sine);
            tween.TweenCallback(Callable.From(() => { back.Hide(); content.Show(); }));
            tween.TweenProperty(card, "scale:x", 1f, 0.12).SetTrans(Godot.Tween.TransitionType.Sine);
            if (i == cards.Count - 1) tween.TweenCallback(Callable.From(finished));
        }
        if (cards.Count == 0) finished();
    }

    public void Leave(IReadOnlyList<PanelContainer> cards, PanelContainer selected, Control deck,
        ColorRect shade, Control panel, Action finished)
    {
        Cancel();
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            bool chosen = card == selected;
            Vector2 destination = chosen ? card.Position + new Vector2(90, -520)
                : deck.GlobalPosition + deck.Size / 2 - card.GetParent<Control>().GlobalPosition - card.Size / 2;
            var tween = Tween();
            tween.TweenInterval(i * 0.045);
            tween.TweenProperty(card, "position", destination, 0.38).SetTrans(Godot.Tween.TransitionType.Cubic).SetEase(Godot.Tween.EaseType.In);
            tween.Parallel().TweenProperty(card, "scale", Vector2.One * (chosen ? 1.08f : 0.2f), 0.38);
            tween.Parallel().TweenProperty(card, "rotation", chosen ? 0.08f : -0.2f, 0.38);
            tween.Parallel().TweenProperty(card, "modulate:a", 0f, 0.38);
        }
        var exit = Tween();
        exit.TweenInterval(0.32);
        exit.TweenProperty(shade, "modulate:a", 0f, 0.23);
        exit.Parallel().TweenProperty(panel, "modulate:a", 0f, 0.23);
        exit.Parallel().TweenProperty(deck, "modulate:a", 0f, 0.23);
        exit.TweenCallback(Callable.From(finished));
    }
}
