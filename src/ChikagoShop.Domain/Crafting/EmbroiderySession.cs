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

    // Item contours remain fixed across launches; the integer API retains the legacy exercises.
    private static (double X, double Y)[] ItemPattern(string kind) => kind switch
    {
        "hat" => [(225,310),(285,280),(315,165),(365,135),(475,135),(525,165),(555,280),
            (615,310),(570,345),(420,360),(270,345),(225,310)],
        "vest" => [(290,135),(355,115),(420,195),(485,115),(550,135),(520,225),(550,355),
            (455,375),(420,335),(385,375),(290,355),(320,225),(290,135)],
        "gloves" => [(325,375),(310,285),(265,230),(280,205),(335,250),(315,155),(340,140),
            (370,235),(370,110),(400,110),(405,225),(430,115),(460,125),(440,240),
            (485,155),(510,175),(475,285),(470,375),(325,375)],
        "matteo_hat" => [(210,330),(290,295),(310,145),(355,115),(485,115),(530,145),
            (550,295),(630,330),(575,365),(420,380),(265,365),(210,330),(290,295),
            (550,295),(540,250),(300,250),(290,295)],
        "lorgnette" => [(225,200),(250,155),(305,145),(350,175),(360,230),(330,270),
            (275,280),(235,250),(225,200),(360,200),(450,200),(460,155),(515,145),
            (560,175),(570,230),(540,270),(485,280),(450,250),(450,200),(570,230),
            (620,375)],
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown embroidery item kind.")
    };

    public EmbroiderySession(string kind, double duration = 10) : this(ItemPattern(kind), duration) { }

    public EmbroiderySession(int pattern = 0, double duration = 10)
        : this(pattern >= 0 && pattern < Patterns.Length ? Patterns[pattern]
            : throw new ArgumentOutOfRangeException(nameof(pattern)), duration) { }

    private EmbroiderySession((double X, double Y)[] points, double duration)
    {
        if (!double.IsFinite(duration) || duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration));
        Duration = RemainingSeconds = duration;
        Points = Array.AsReadOnly(points);
    }

    public IReadOnlyList<(double X, double Y)> Points { get; }
    public EmbroideryState State { get; private set; }
    public int Stitches { get; private set; }
    public int Misses { get; private set; }
    public double Duration { get; }
    public double RemainingSeconds { get; private set; }
    public bool Complete => Stitches == Points.Count;
    public int Quality => Math.Clamp((int)Math.Round(100 - Misses * 12 - (Duration - RemainingSeconds) / Duration * 11
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
        RemainingSeconds = successful ? Duration : 0;
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
