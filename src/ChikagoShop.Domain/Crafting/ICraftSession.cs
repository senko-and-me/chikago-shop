namespace ChikagoShop.Domain.Crafting;

public interface ICraftSession
{
    EmbroideryState State { get; }
    double RemainingSeconds { get; }
    int Misses { get; }
    bool Complete { get; }
    int Quality { get; }
    string Rank { get; }
    void Start();
    void Tick(double delta);
}
