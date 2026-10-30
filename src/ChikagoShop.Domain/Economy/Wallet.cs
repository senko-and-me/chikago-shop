namespace ChikagoShop.Domain.Economy;

public sealed class Wallet
{
    public Wallet(int initialBalance = 0)
    {
        if (initialBalance < 0) throw new ArgumentOutOfRangeException(nameof(initialBalance));
        Balance = initialBalance;
    }

    public int Balance { get; private set; }

    public void Credit(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Balance = checked(Balance + amount);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Balance < amount) return false;
        Balance -= amount;
        return true;
    }
}
