
using System;

namespace ChikagoShop.Domain.Days;

public enum EndDayAction { Closed, WaitForTrade, Advance }

public sealed class DaySession {
  public const double DurationSeconds = 60;
  public const double TwoRoomDurationSeconds = 90;
  public const double MaxDurationSeconds = 120;

  public double Duration { get; private set; } = DurationSeconds;

  public double RemainingSeconds { get; private set; } = DurationSeconds;

  public bool IsClosed { get; private set; }

  public bool CanReceiveBuyers => !IsClosed;

  public int AvailableEnergy(int storedEnergy) {
    return IsClosed ? 0 : storedEnergy;
  }

  public bool Tick(double delta) {
    if (IsClosed || !double.IsFinite(delta) || delta <= 0)
      return false;

    RemainingSeconds = Math.Max(0, RemainingSeconds - delta);

    if (RemainingSeconds > 0)
      return false;

    Close();
    return true;
  }

  public EndDayAction RequestEnd(bool tradeActive) {
    if (!IsClosed) {
      Close();
      return EndDayAction.Closed;
    }

    return tradeActive ? EndDayAction.WaitForTrade : EndDayAction.Advance;
  }

  public void Close() {
    RemainingSeconds = 0;
    IsClosed = true;
  }

  public void ConfigureDuration(double durationSeconds) {
    double newDuration = ValidDuration(durationSeconds);

    if (IsClosed) {
      Duration = newDuration;
      return;
    }

    double elapsedSeconds = Math.Max(0, Duration - RemainingSeconds);

    Duration = newDuration;

    RemainingSeconds = Math.Max(0, Duration - elapsedSeconds);

    if (RemainingSeconds <= 0)
      Close();
  }

  public void Reset(double durationSeconds = DurationSeconds) {
    Duration = ValidDuration(durationSeconds);
    RemainingSeconds = Duration;
    IsClosed = false;
  }

  public void Restore(double? remainingSeconds, bool closed,
                      double durationSeconds = DurationSeconds) {
    Duration = ValidDuration(durationSeconds);

    RemainingSeconds = remainingSeconds is double remaining && double.IsFinite(remaining)
                           ? Math.Clamp(remaining, 0, Duration)
                           : Duration;

    IsClosed = closed || RemainingSeconds == 0;

    if (IsClosed)
      RemainingSeconds = 0;
  }

  private static double ValidDuration(double durationSeconds) {
    return double.IsFinite(durationSeconds)
               ? Math.Clamp(durationSeconds, DurationSeconds, MaxDurationSeconds)
               : DurationSeconds;
  }
}
