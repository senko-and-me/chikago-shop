namespace ChikagoShop.Domain.Economy;

public sealed class EnergyPool
{
    public EnergyPool(int capacity)
    {
        SetCapacity(capacity, restore: true);
    }

    public int Capacity { get; private set; }
    public int Available { get; private set; }

    public void Restore() => Available = Capacity;

    public void SetCapacity(int capacity, bool restore = false)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        Available = restore ? capacity : Math.Min(Available, capacity);
    }

    public bool TrySpend(int amount = 1)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Available < amount) return false;
        Available -= amount;
        return true;
    }
}
