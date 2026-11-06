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
    private readonly byte[] _mask;
    private readonly int _width, _height;
    private readonly PolishingCell[] _cells;
    private readonly int _insideCount;
    private bool _drag, _farOutside, _hasPoint, _hasDirection;
    private double _lastX, _lastY, _dirX, _dirY, _elapsed, _lastPenalty = -1;
    public IReadOnlyList<PolishingCell> Cells { get; }
    public int Columns { get; }
    public int Rows { get; }
    public EmbroideryState State { get; private set; }
    public double RemainingSeconds { get; private set; } = 10;
    public int Misses { get; private set; }
    public double Progress { get; private set; }
    public double DirtProgress { get; private set; }
    public bool Complete => Progress >= 0.85;
    public int Quality => Math.Clamp((int)Math.Round(Math.Min(100, Progress / .85 * 96)
        + RemainingSeconds / 10 * 4 - Misses * 4.5), 0, 100);
    public string Rank => Quality >= 92 ? "S" : Quality >= 78 ? "A" : Quality >= 63 ? "B" : Quality >= 42 ? "C" : "D";

    public PolishingSession(byte[] alphaMask, int width = 600, int height = 480)
    {
        if (width <= 0 || height <= 0 || alphaMask.Length != checked(width * height))
            throw new ArgumentException("Mask dimensions must match its pixel data.");
        _width = width; _height = height;
        _mask = (byte[])alphaMask.Clone();
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
        _hasPoint = Inside(x,y); _lastX=x; _lastY=y;
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
        if (_hasPoint) Wipe(x,y,elapsedMilliseconds);
        _lastX=x; _lastY=y; _hasPoint=true;
    }

    private void RecordMiss()
    {
        if (_elapsed - _lastPenalty < .24) return;
        _lastPenalty = _elapsed; Misses++;
    }

    private void Wipe(double x, double y, double milliseconds)
    {
        double dx=x-_lastX, dy=y-_lastY, distance=Math.Sqrt(dx*dx+dy*dy);
        if (distance < 1.9) return;
        double energy=Math.Clamp((distance/Math.Max(7,milliseconds)-.22)/1.65,0,1);
        bool flip=_hasDirection && dx/distance*_dirX+dy/distance*_dirY < -.43;
        _dirX=dx/distance; _dirY=dy/distance; _hasDirection=true;
        double strength=Math.Min(.071,(.010+.051*energy+(flip?.024:0))*.616);
        double ratio=Math.Min(1,34/distance), fromX=x-dx*ratio, fromY=y-dy*ratio;
        // Layer one must be removed locally before the final satin film starts clearing.
        int atX=Math.Clamp((int)(x/CellSize),0,Columns-1), atY=Math.Clamp((int)(y/CellSize),0,Rows-1);
        foreach(var cell in _cells)
        {
            if (!cell.Inside) continue;
            double d=SegmentDistanceSquared(cell.X,cell.Y,fromX,fromY,x,y);
            if(d>BrushRadius*BrushRadius) continue;
            cell.Dirt *= 1-strength;
            if(Squared(cell.X-x,cell.Y-y)<=BrushRadius*BrushRadius) cell.Dirt *= 1-strength;
        }
        if (_cells[atY*Columns+atX].Dirt < 125.0/209)
        foreach(var cell in _cells)
        {
            if(!cell.Inside) continue;
            double radius=BrushRadius*.89, filmStrength=Math.Max(.006,strength*.85);
            if(SegmentDistanceSquared(cell.X,cell.Y,fromX,fromY,x,y)>radius*radius) continue;
            cell.Film *= 1-filmStrength;
            if(Squared(cell.X-x,cell.Y-y)<=radius*radius) cell.Film *= 1-filmStrength;
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
