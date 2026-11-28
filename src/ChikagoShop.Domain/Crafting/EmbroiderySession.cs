namespace ChikagoShop.Domain.Crafting;

public enum EmbroideryState { Ready, Playing, Finished }

// Coordinates use the reference canvas (840 x 480), independent of display size.
public sealed class EmbroiderySession : ICraftSession
{
    private const int RankAQuality = 75;
    private bool _finishedEarly;
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
        "hat" => [(215,314),(278,282),(310,161),(362,130),(478,130),(530,161),(562,282),
            (625,314),(578,350),(499,362),(420,366),(341,362),(262,350),(215,314)],
        "vest" => [(290,135),(355,115),(420,195),(485,115),(550,135),(520,225),(535,290),
            (550,355),(455,375),(420,335),(385,375),(290,355),(320,225),(290,135)],
        "gloves" => [(326,333),(320,266),(276,218),(325,224),(309,153),(361,138),(381,328),
            (459,328),(479,138),(531,153),(515,224),(564,218),(520,266),(514,333)],
        "matteo_hat" => [(202,331),(263,302),(340,278),(324,218),(384,175),(417,146),(520,153),
            (526,247),(572,258),(640,270),(573,318),(499,337),(340,353),(202,331)],
        "lorgnette" => [(262,130),(330,130),(410,130),(526,130),(552,105),(585,130),(585,200),
            (585,263),(585,343),(553,343),(553,266),(553,210),(394,210),(262,210),(262,130)],
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
        - (State == EmbroideryState.Finished && !Complete ? (Points.Count - Stitches) * 7 : 0)), 0,
        _finishedEarly ? RankAQuality - 1 : 100);
    public string Rank => Quality >= 90 ? "S" : Quality >= RankAQuality ? "A" : Quality >= 60 ? "B" : Quality >= 40 ? "C" : "D";

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

    public void FinishEarly()
    {
        if (State != EmbroideryState.Playing) return;
        _finishedEarly = true;
        Tick(RemainingSeconds);
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
