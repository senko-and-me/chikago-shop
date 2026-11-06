namespace ChikagoShop.Domain.Days;

public enum EndDayAction { Closed, WaitForTrade, Advance }

// Closing shop and moving to the next calendar day are separate operations.
public sealed class DaySession
{
    public const double DurationSeconds = 120;
    public double RemainingSeconds { get; private set; } = DurationSeconds;
    public bool IsClosed { get; private set; }
    public bool CanReceiveBuyers => !IsClosed;
    public int AvailableEnergy(int storedEnergy) => IsClosed ? 0 : storedEnergy;

    public bool Tick(double delta)
    {
        if (IsClosed || !double.IsFinite(delta) || delta <= 0) return false;
        RemainingSeconds = Math.Max(0, RemainingSeconds - delta);
        if (RemainingSeconds > 0) return false;
        Close();
        return true;
    }

    public EndDayAction RequestEnd(bool tradeActive)
    {
        if (!IsClosed)
        {
            Close();
            return EndDayAction.Closed;
        }
        return tradeActive ? EndDayAction.WaitForTrade : EndDayAction.Advance;
    }

    public void Close()
    {
        RemainingSeconds = 0;
        IsClosed = true;
    }

    public void Reset()
    {
        RemainingSeconds = DurationSeconds;
        IsClosed = false;
    }

    public void Restore(double? remainingSeconds, bool closed)
    {
        RemainingSeconds = remainingSeconds is double remaining && double.IsFinite(remaining)
            ? Math.Clamp(remaining, 0, DurationSeconds) : DurationSeconds;
        IsClosed = closed || RemainingSeconds == 0;
        if (IsClosed) RemainingSeconds = 0;
    }
}
