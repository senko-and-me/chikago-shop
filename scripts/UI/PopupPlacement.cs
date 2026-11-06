using System;
using Godot;

// Geometry only: callers supply current screen-space bounds, without scene lookups.
public static class PopupPlacement
{
    public static Vector2 FindPosition(Rect2 viewport, Rect2 anchor, Vector2 size,
        ReadOnlySpan<Rect2> obstacles, float gap = 12)
    {
        Vector2 center = anchor.GetCenter();
        ReadOnlySpan<Vector2> candidates = stackalloc Vector2[]
        {
            new(anchor.End.X + gap, center.Y - size.Y / 2),
            new(anchor.Position.X - size.X - gap, center.Y - size.Y / 2),
            new(center.X - size.X / 2, anchor.Position.Y - size.Y - gap),
            new(center.X - size.X / 2, anchor.End.Y + gap),
            new(anchor.End.X + gap, anchor.Position.Y - size.Y - gap),
            new(anchor.Position.X - size.X - gap, anchor.Position.Y - size.Y - gap)
        };
        Vector2 best = viewport.Position;
        float bestScore = float.PositiveInfinity;
        foreach (Vector2 candidate in candidates)
        {
            Vector2 position = candidate.Clamp(viewport.Position, viewport.End - size);
            Rect2 rect = new(position, size);
            float score = rect.Intersection(anchor).Area * 10 + candidate.DistanceSquaredTo(position);
            foreach (Rect2 obstacle in obstacles)
                score += rect.Intersection(obstacle).Area * 4;
            if (score >= bestScore) continue;
            bestScore = score;
            best = position;
        }
        return best;
    }
}
