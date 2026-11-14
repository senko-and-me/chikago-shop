namespace ChikagoShop.Domain.Crafting;

// Cosmetic variety only: callers supply an independent random roll, never an economy stream.
public sealed class MiniGameVariantPicker
{
    public int Count { get; }
    public int LastIndex { get; private set; } = -1;
    public int ConsecutiveCount { get; private set; }
    public double RepeatWeight => LastIndex < 0 ? 1 : Math.Pow(0.5, Math.Min(ConsecutiveCount, 6));
    public double RepeatChance => LastIndex < 0 ? 1.0 / Count : RepeatWeight / (Count - 1 + RepeatWeight);

    public MiniGameVariantPicker(int count)
    {
        if (count < 2) throw new ArgumentOutOfRangeException(nameof(count));
        Count = count;
    }

    public int Next(double roll)
    {
        if (!double.IsFinite(roll) || roll < 0 || roll >= 1) throw new ArgumentOutOfRangeException(nameof(roll));
        double remaining = roll * (LastIndex < 0 ? Count : Count - 1 + RepeatWeight);
        int selected = Count - 1;
        for (int index = 0; index < Count; index++)
        {
            remaining -= index == LastIndex ? RepeatWeight : 1;
            if (remaining < 0) { selected = index; break; }
        }
        ConsecutiveCount = selected == LastIndex ? Math.Min(ConsecutiveCount + 1, 6) : 1;
        LastIndex = selected;
        return selected;
    }
}
