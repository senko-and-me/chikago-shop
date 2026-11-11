namespace ChikagoShop.Domain.Crafting;

public enum EmbroideryState { Ready, Playing, Finished }

// Coordinates use the reference canvas (840 x 480), independent of display size.
public sealed class EmbroiderySession : ICraftSession
{
    private static readonly (double X, double Y)[][] Patterns =
    [
        [(150,180),(210,300),(270,180),(330,300),(390,180),(450,300),(510,180),(570,300),(630,180),(690,300)],
        [(300,340),(235,190),(300,340),(325,135),(300,340),(420,120),(300,340),(520,165),(300,340),(600,250)],
        [(420,240),(410,142),(420,240),(515,177),(420,240),(510,322),(420,240),(325,323),(420,240),(325,178)],
        [(420,96),(625,240),(420,384),(215,240),(420,125),(590,240),(420,355),(250,240),(420,154),(555,240),(420,326),(285,240)]
    ];

    public EmbroiderySession(int pattern = 0)
    {
        if (pattern < 0 || pattern >= Patterns.Length) throw new ArgumentOutOfRangeException(nameof(pattern));
        Points = Array.AsReadOnly(Patterns[pattern]);
    }

    public IReadOnlyList<(double X, double Y)> Points { get; }
    public EmbroideryState State { get; private set; }
    public int Stitches { get; private set; }
    public int Misses { get; private set; }
    public double RemainingSeconds { get; private set; } = 10;
    public bool Complete => Stitches == Points.Count;
    public int Quality => Math.Clamp((int)Math.Round(100 - Misses * 12 - (10 - RemainingSeconds) * 1.1
        - (State == EmbroideryState.Finished && !Complete ? (Points.Count - Stitches) * 7 : 0)), 0, 100);
    public string Rank => Quality >= 90 ? "S" : Quality >= 75 ? "A" : Quality >= 60 ? "B" : Quality >= 40 ? "C" : "D";

    public void Start()
    {
        if (State == EmbroideryState.Ready) State = EmbroideryState.Playing;
    }

    public void FinishForTesting(bool successful)
    {
        if (State != EmbroideryState.Playing) return;
        Stitches = successful ? Points.Count : 0;
        Misses = successful ? 0 : 10;
        RemainingSeconds = successful ? 10 : 0;
        State = EmbroideryState.Finished;
    }

    public void Tick(double delta)
    {
        if (State != EmbroideryState.Playing || !double.IsFinite(delta) || delta <= 0) return;
        RemainingSeconds = Math.Max(0, RemainingSeconds - delta);
        if (RemainingSeconds == 0) State = EmbroideryState.Finished;
    }

    public bool Hit(double x, double y)
    {
        if (State != EmbroideryState.Playing || !double.IsFinite(x) || !double.IsFinite(y)) return false;
        var target = Points[Stitches];
        if (Math.Pow(x - target.X, 2) + Math.Pow(y - target.Y, 2) >= 25 * 25)
        {
            Misses++;
            return false;
        }
        Stitches++;
        if (Complete) State = EmbroideryState.Finished;
        return true;
    }
}
