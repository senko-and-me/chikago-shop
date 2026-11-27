namespace ChikagoShop.Domain.Crafting;

public sealed class PolishingCell(double x, double y, bool inside)
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public bool Inside { get; } = inside;
    public double Dirt { get; internal set; } = 1;
    public double Film { get; internal set; } = 1;
}

// A sampled coating on the exact silhouette, independent of the renderer and input device.
public sealed class PolishingSession : ICraftSession
{
    public const int CellSize = 7;
    public const double BrushRadius = 34;
    private const double ReferenceArea = 42000;
    private const double Step = 7;
    private const double MaximumSweep = 34;
    public int MaskArea { get; }
    public double Radius { get; }
    public double CleaningStrength { get; }
    private readonly byte[] _mask;
    private readonly int _width, _height;
    private readonly PolishingCell[] _cells;
    private readonly int _insideCount;
    private bool _drag, _farOutside, _hasPoint, _hasDirection;
    private double _lastX, _lastY, _dirX, _dirY, _elapsed, _lastPenalty = -1;
    private double _distanceSinceWipe, _wipeX, _wipeY, _timeSinceWipe, _reversalReach;
    public IReadOnlyList<PolishingCell> Cells { get; }
    public int Columns { get; }
    public int Rows { get; }
    public EmbroideryState State { get; private set; }
    public double Duration { get; }
    public double RemainingSeconds { get; private set; }
    public int Misses { get; private set; }
    public double Progress { get; private set; }
    public double DirtProgress { get; private set; }
    public bool Complete => Progress >= 0.85;
    public int Quality => Math.Clamp((int)Math.Round(Math.Min(100, Progress / .85 * 96)
        + RemainingSeconds / Duration * 4 - Misses * 4.5), 0, 100);
    public string Rank => Quality >= 92 ? "S" : Quality >= 78 ? "A" : Quality >= 63 ? "B" : Quality >= 42 ? "C" : "D";

    public PolishingSession(byte[] alphaMask, int width = 600, int height = 480, double duration = 10,
        double radiusCorrection = 1, double strengthCorrection = 1)
    {
        if (!double.IsFinite(duration) || duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration));
        if (!double.IsFinite(radiusCorrection) || radiusCorrection <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusCorrection));
        if (!double.IsFinite(strengthCorrection) || strengthCorrection <= 0)
            throw new ArgumentOutOfRangeException(nameof(strengthCorrection));
        Duration = RemainingSeconds = duration;
        if (width <= 0 || height <= 0 || alphaMask.Length != checked(width * height))
            throw new ArgumentException("Mask dimensions must match its pixel data.");
        _width = width; _height = height;
        _mask = (byte[])alphaMask.Clone();
        MaskArea = _mask.Count(alpha => alpha > 128);
        // The mask is already fitted to the playfield. Let both the reach and the real
        // removal rate absorb part of the area difference.
        double relativeArea = MaskArea / ReferenceArea;
        Radius = BrushRadius * Math.Clamp(Math.Pow(relativeArea, .22) * radiusCorrection, .8, 1.2);
        CleaningStrength = Math.Clamp(Math.Pow(relativeArea, .45) * strengthCorrection, .5, 2.5);
        Columns = (width + CellSize - 1) / CellSize;
        Rows = (height + CellSize - 1) / CellSize;
        _cells = new PolishingCell[Columns * Rows];
        for (int y=0;y<Rows;y++)
        for (int x=0;x<Columns;x++)
        {
            var cell = new PolishingCell(x * CellSize + CellSize / 2.0, y * CellSize + CellSize / 2.0,
                Inside(x * CellSize + CellSize / 2.0, y * CellSize + CellSize / 2.0));
            _cells[y * Columns + x] = cell;
            if (cell.Inside) _insideCount++;
        }
        if (_insideCount == 0) throw new ArgumentException("The silhouette cannot be empty.");
        Cells = Array.AsReadOnly(_cells);
    }

    public void Start() { if (State == EmbroideryState.Ready) State = EmbroideryState.Playing; }
    public void FinishForTesting(bool successful)
    {
        if (State != EmbroideryState.Playing) return;
        EndRub();
        foreach (var cell in _cells)
        {
            cell.Dirt = successful ? 0 : 1;
            cell.Film = successful ? 0 : 1;
        }
        Progress = DirtProgress = successful ? 1 : 0;
        Misses = 0;
        RemainingSeconds = successful ? Duration : 0;
        State = EmbroideryState.Finished;
    }
    public void Tick(double delta)
    {
        if (State != EmbroideryState.Playing || !double.IsFinite(delta) || delta <= 0) return;
        _elapsed += delta;
        RemainingSeconds = Math.Max(0, RemainingSeconds - delta);
        if (RemainingSeconds == 0) { State = EmbroideryState.Finished; EndRub(); }
    }

    public bool Inside(double x, double y) => double.IsFinite(x) && double.IsFinite(y) &&
        x >= 0 && y >= 0 && x < _width && y < _height && _mask[(int)y * _width + (int)x] > 128;

    private bool Near(double x, double y)
    {
        if (Inside(x,y)) return true;
        for(int dy=-47;dy<=47;dy+=5)
        for(int dx=-47;dx<=47;dx+=5)
            if(dx*dx+dy*dy<=47*47 && Inside(x+dx,y+dy)) return true;
        return false;
    }

    public void BeginRub(double x, double y)
    {
        if (State != EmbroideryState.Playing || !double.IsFinite(x) || !double.IsFinite(y)) return;
        _drag = true; _hasDirection = false;
        _farOutside = !Near(x,y);
        if (_farOutside) RecordMiss();
        _hasPoint = Inside(x,y);
        ResetSweep(x, y);
    }

    public void EndRub() { _drag = false; _hasPoint = false; _hasDirection = false; _farOutside = false; }

    public void MoveRub(double x, double y, double elapsedMilliseconds)
    {
        if (!_drag || State != EmbroideryState.Playing || !double.IsFinite(x) || !double.IsFinite(y) ||
            !double.IsFinite(elapsedMilliseconds) || elapsedMilliseconds < 0) return;
        bool far = !Near(x,y);
        if (far && !_farOutside) RecordMiss();
        _farOutside = far;
        if (!Inside(x,y)) { _hasPoint = false; _hasDirection = false; return; }
        if (_hasPoint) SampleSweep(x, y, elapsedMilliseconds);
        else ResetSweep(x, y);
        _lastX=x; _lastY=y; _hasPoint=true;
    }

    private void RecordMiss()
    {
        if (_elapsed - _lastPenalty < .24) return;
        _lastPenalty = _elapsed; Misses++;
    }

    private void ResetSweep(double x, double y)
    {
        _lastX = _wipeX = x;
        _lastY = _wipeY = y;
        _distanceSinceWipe = _timeSinceWipe = _reversalReach = 0;
    }

    private void SampleSweep(double x, double y, double milliseconds)
    {
        double dx = x - _lastX, dy = y - _lastY;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < 0.000001)
        {
            _timeSinceWipe += milliseconds;
            return;
        }

        double directionX = dx / distance, directionY = dy / distance;
        double start = Math.Max(0, distance - MaximumSweep);
        // A large jump may affect only the end of the sweep, as before.
        if (start > 0) ResetSweep(x - directionX * MaximumSweep, y - directionY * MaximumSweep);
        if (_hasDirection && directionX * _dirX + directionY * _dirY < -.43)
            _reversalReach = 2 * Step;
        _dirX = directionX;
        _dirY = directionY;
        _hasDirection = true;

        double travelled = 0;
        double usable = distance - start;
        double timePerUnit = milliseconds / distance;
        double originX = _lastX, originY = _lastY;
        // Use spatial steps so input batching does not grant extra cleaning.
        while (usable - travelled + _distanceSinceWipe >= Step - 0.00001 &&
            State == EmbroideryState.Playing)
        {
            double next = Step - _distanceSinceWipe;
            travelled += next;
            _timeSinceWipe += next * timePerUnit;
            double px = originX + travelled * directionX, py = originY + travelled * directionY;
            if (Inside(px, py))
                Wipe(px, py, _timeSinceWipe, _reversalReach > 0);
            _wipeX = px;
            _wipeY = py;
            _reversalReach = Math.Max(0, _reversalReach - Step);
            _distanceSinceWipe = _timeSinceWipe = 0;
        }
        double tail = Math.Max(0, usable - travelled);
        _distanceSinceWipe += tail;
        _timeSinceWipe += tail * timePerUnit;
    }

    private void Wipe(double x, double y, double milliseconds, bool flip)
    {
        double energy = Math.Clamp((Step / Math.Max(Step / 2, milliseconds) - .22) / 1.65, 0, 1);
        double strength = Math.Min(.071, (.010 + .051 * energy + (flip ? .024 : 0)) * .616) * CleaningStrength;
        double dirtRemaining = Math.Sqrt(1 - strength);
        double fromX = _wipeX, fromY = _wipeY;
        // Layer one must be removed locally before the final satin film starts clearing.
        int atX=Math.Clamp((int)(x/CellSize),0,Columns-1), atY=Math.Clamp((int)(y/CellSize),0,Rows-1);
        foreach(var cell in _cells)
        {
            if (!cell.Inside) continue;
            double d=SegmentDistanceSquared(cell.X,cell.Y,fromX,fromY,x,y);
            if(d>Radius*Radius) continue;
            cell.Dirt *= dirtRemaining;
            if(Squared(cell.X-x,cell.Y-y)<=Radius*Radius) cell.Dirt *= dirtRemaining;
        }
        if (_cells[atY*Columns+atX].Dirt < 125.0/209)
        foreach(var cell in _cells)
        {
            if(!cell.Inside) continue;
            double radius=Radius*.89;
            double filmRemaining = Math.Sqrt(1 - Math.Max(.006 * CleaningStrength, strength * .85));
            if(SegmentDistanceSquared(cell.X,cell.Y,fromX,fromY,x,y)>radius*radius) continue;
            cell.Film *= filmRemaining;
            if(Squared(cell.X-x,cell.Y-y)<=radius*radius) cell.Film *= filmRemaining;
        }
        double dirt=0,film=0;
        foreach(var cell in _cells) if(cell.Inside) { dirt+=1-cell.Dirt; film+=1-cell.Film; }
        DirtProgress=dirt/_insideCount;
        Progress=Math.Clamp((dirt*.43+film*.57)/_insideCount,0,1);
        if(Complete) { State=EmbroideryState.Finished; EndRub(); }
    }

    private static double Squared(double x,double y) => x*x+y*y;
    private static double SegmentDistanceSquared(double x,double y,double ax,double ay,double bx,double by)
    {
        double length=Squared(bx-ax,by-ay);
        double t=length==0?0:Math.Clamp(((x-ax)*(bx-ax)+(y-ay)*(by-ay))/length,0,1);
        return Squared(x-ax-t*(bx-ax),y-ay-t*(by-ay));
    }
}
