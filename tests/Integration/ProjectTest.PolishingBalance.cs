using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ChikagoShop.Domain.Crafting;

public partial class ProjectTest
{
    private readonly record struct Scrub(Vector2 Start, Vector2 End);

    private static Scrub[] BuildScrubRoute(PolishingSession session, bool verticalFirst)
    {
        Vector2 Point(PolishingCell cell) => new((float)cell.X, (float)cell.Y);
        var cells = session.Cells.Where(cell => cell.Inside).Select(Point).ToArray();
        var directions = verticalFirst
            ? new[] { Vector2.Down, Vector2.Right, new Vector2(0.6f, 0.8f) }
            : new[] { Vector2.Right, Vector2.Down, new Vector2(0.8f, 0.6f) };
        var candidates = new List<(Scrub Stroke, int[] Covered)>();
        foreach (Vector2 cell in cells)
        {
            Vector2 end = Vector2.Zero;
            bool found = false;
            foreach (var direction in directions)
            {
                end = cell + direction * 14;
                if (!Enumerable.Range(1, 7).All(sample =>
                    session.Inside(cell.X + direction.X * sample * 2, cell.Y + direction.Y * sample * 2)))
                    continue;
                found = true;
                break;
            }
            if (!found) continue;
            Vector2 center = (cell + end) / 2;
            var covered = Enumerable.Range(0, cells.Length)
                .Where(index => cells[index].DistanceSquaredTo(center) <= 30 * 30).ToArray();
            candidates.Add((new Scrub(cell, end), covered));
        }

        var route = new List<Scrub>();
        var uncovered = Enumerable.Repeat(true, cells.Length).ToArray();
        while (candidates.Count > 0)
        {
            var best = candidates.MaxBy(candidate => candidate.Covered.Count(index => uncovered[index]));
            int coverage = best.Covered.Count(index => uncovered[index]);
            if (coverage == 0) break;
            route.Add(best.Stroke);
            foreach (int index in best.Covered) uncovered[index] = false;
        }

        var ordered = new List<Scrub>();
        Vector2 cursor = cells[0];
        while (route.Count > 0)
        {
            var next = route.MinBy(stroke => stroke.Start.DistanceSquaredTo(cursor));
            ordered.Add(next);
            route.Remove(next);
            cursor = next.Start;
        }
        return ordered.ToArray();
    }

    private static (double Time, int Moves, double Distance) SimulatePolishing(
        PolishingSession session, Scrub[] route, double speed)
    {
        double time = 0, distance = 0;
        int moves = 0, index = 0;
        Vector2 cursor = route[0].Start;
        void Travel(Vector2 target, bool rub)
        {
            double step = cursor.DistanceTo(target);
            double seconds = step / speed;
            time += seconds;
            distance += step;
            cursor = target;
            session.Tick(seconds);
            if (rub)
            {
                moves++;
                session.MoveRub(target.X, target.Y, seconds * 1000);
            }
        }

        session.Start();
        while (session.State == EmbroideryState.Playing)
        {
            var stroke = route[index++ % route.Length];
            Travel(stroke.Start, false);
            session.BeginRub(cursor.X, cursor.Y);
            PolishingCell CellAt(Vector2 point) =>
                session.Cells[(int)(point.Y / PolishingSession.CellSize) * session.Columns +
                    (int)(point.X / PolishingSession.CellSize)];
            PolishingCell start = CellAt(stroke.Start), end = CellAt(stroke.End);
            for (int move = 0; move < 32 && session.State == EmbroideryState.Playing; move++)
            {
                if (start.Film <= .14 && (!end.Inside || end.Film <= .14)) break;
                Travel(move % 2 == 0 ? stroke.End : stroke.Start, true);
            }
            session.EndRub();
        }
        return (time, moves, distance);
    }

    private void CheckPolishingInputCadence()
    {
        var mask = Enumerable.Repeat((byte)255, 144 * 144).ToArray();
        PolishingSession Run(int packets)
        {
            var session = new PolishingSession(mask, 144, 144);
            session.Start();
            session.BeginRub(48, 72);
            for (int stroke = 0; stroke < 12; stroke++)
            {
                int start = stroke % 2 == 0 ? 48 : 76;
                int direction = stroke % 2 == 0 ? 1 : -1;
                for (int index = 1; index <= packets; index++)
                {
                    session.Tick(.020 / packets);
                    session.MoveRub(start + direction * 28d * index / packets, 72, 20d / packets);
                }
            }
            return session;
        }

        var coarse = Run(1);
        foreach (int packets in new[] { 2, 4, 8 })
        {
            var fine = Run(packets);
            Check(coarse.Cells.Zip(fine.Cells).All(pair =>
                    Math.Abs(pair.First.Dirt - pair.Second.Dirt) < 1e-8 &&
                    Math.Abs(pair.First.Film - pair.Second.Film) < 1e-8) &&
                coarse.Rank == fine.Rank && coarse.Misses == fine.Misses,
                $"The same path and speed must remove the same dirt and film with {packets} motion packets");
        }
    }

    private void TestPolishingBalance()
    {
        CheckPolishingInputCadence();
        var board = new PolishingSurface();
        Root.AddChild(board);
        try
        {
            var means = new List<(double Time, double Moves, double Distance)>();
            foreach (string kind in PolishingSurface.InstrumentKinds)
            {
                var reference = board.Begin(kind);
                var routes = new[] { BuildScrubRoute(reference, false), BuildScrubRoute(reference, true) };
                var results = (from route in routes
                    from speed in new[] { 1650d, 1950d, 2250d }
                    let session = board.Begin(kind)
                    let result = SimulatePolishing(session, route, speed)
                    select (result.Time, result.Moves, result.Distance, session.Progress, session.Rank, session.Misses))
                    .ToArray();
                Check(results.All(result => result.Progress >= .85 && result.Rank == "S" && result.Misses == 0),
                    $"{kind} must reach the existing completion threshold at rank S across all six routes and speeds");
                means.Add((results.Average(r => r.Time), results.Average(r => r.Moves), results.Average(r => r.Distance)));
                GD.Print(FormattableString.Invariant(
                    $"POLISH_BENCH {kind} area={reference.MaskArea} radius={reference.Radius:F2} strength={reference.CleaningStrength:F3} time={results.Average(r => r.Time):F3} moves={results.Average(r => r.Moves):F1} distance={results.Average(r => r.Distance):F1} min={results.Min(r => r.Progress):F3} ranks={string.Join(',', results.Select(r => r.Rank))} misses={results.Sum(r => r.Misses)}"));
            }
            foreach (var metric in new Func<(double Time, double Moves, double Distance), double>[]
                { result => result.Time, result => result.Moves, result => result.Distance })
                Check(means.Max(metric) / means.Min(metric) <= 1.15, "Mean effort must stay within 15% across instruments");
        }
        finally { board.QueueFree(); }
    }
}
