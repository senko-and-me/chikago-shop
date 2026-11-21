namespace ChikagoShop.Domain.Economy;

/// <summary>
/// Small, hidden correction of the *next* random outcomes. It does not change
/// the player's existing items, rerolls, cards, expenses, or quota amounts.
/// Positive pressure helps a struggling run; negative pressure slightly
/// moderates a run far ahead of its quota trajectory.
/// </summary>
public sealed class InvisibleHandDirector
{
    public double Pressure { get; private set; }

    public void Reset() => Pressure = 0;

    public void Restore(double pressure) =>
        Pressure = double.IsFinite(pressure) ? Math.Clamp(pressure, -1, 1) : 0;

    /// <summary>Updates only on meaningful game actions, so feedback is gradual.</summary>
    public void Observe(int cash, double unsoldStockValue, int checkpointCash,
        int currentDay, int firstDay, int lastDay, double dayFraction,
        int quota, bool enabled)
    {
        if (!enabled) { Pressure = 0; return; }
        if (lastDay < firstDay || quota <= 0) return;

        double elapsed = Math.Clamp(currentDay - firstDay + Math.Clamp(dayFraction, 0, 1), 0,
            lastDay - firstDay + 1);
        double progress = elapsed / (lastDay - firstDay + 1);
        double targetCash = checkpointCash + Math.Max(0, quota - checkpointCash) * progress;
        // Unsold items count as assets, albeit discounted for uncertain buyer offers.
        double assets = cash + Math.Max(0, unsoldStockValue) * 0.8;
        double tolerance = Math.Max(75, quota * 0.25);
        double desired = Math.Clamp((targetCash - assets) / tolerance, -1, 1);
        // Avoid punishing a run immediately for beginning with its starting cash.
        if (progress < 0.1) desired *= progress / 0.1;
        Pressure += Math.Clamp(desired - Pressure, -0.12, 0.12);
    }

    public int RarityShift(int totalWeight, double maximumPercentagePoints) =>
        (int)Math.Round(totalWeight * Pressure * Math.Clamp(maximumPercentagePoints, 0, 5) / 100,
            MidpointRounding.AwayFromZero);

    public int AdjustInitialOffer(int originalPercent, double maximumPercentagePoints)
    {
        int offset = (int)Math.Round(Pressure * Math.Clamp(maximumPercentagePoints, 0, 3),
            MidpointRounding.AwayFromZero);
        return Math.Clamp(originalPercent + offset, -30, 30);
    }
}
