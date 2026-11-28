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
        "gloves" => [(340,337),(328,278),(290,219),(349,248),(332,169),(363,158),(385,230),
            (385,131),(413,222),(445,139),(435,239),(491,173),(470,272),(462,337),
            (340,337)],
        "matteo_hat" => [(256,305),(322,277),(337,163),(375,139),(465,139),(503,163),(518,277),
            (584,305),(542,331),(420,344),(298,331),(256,305),(322,277),(518,277)],
        "lorgnette" => [(360,218),(325,151),(262,151),(224,210),(257,272),(322,272),(360,218),
            (449,218),(477,151),(540,151),(576,218),(540,272),(472,272),(449,218),
            (595,350)],
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
