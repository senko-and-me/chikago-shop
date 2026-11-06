namespace ChikagoShop.Domain.Days;

public enum DayUpgrade { None, Cash, Energy, Time, Atelier, Music, AtelierPrice, MusicPrice }

// One temporary upgrade per new day; cash is an immediate, one-off reward.
public sealed class DayUpgradeSession
{
    public bool Pending { get; private set; }
    public DayUpgrade Selected { get; private set; }
    public int Day { get; private set; } = 2;
    public int AtelierLevel { get; private set; }
    public int MusicLevel { get; private set; }
    public int AtelierPriceLevel { get; private set; }
    public int MusicPriceLevel { get; private set; }
    public IReadOnlyList<DayUpgrade> Options => Enumerable.Range(0, 3)
        .Select(offset => (DayUpgrade)(1 + (Day - 2 + offset) % 7)).ToArray();
    public int EnergyCapacity => Selected == DayUpgrade.Energy ? 3 : 2;
    public double Duration => DaySession.DurationSeconds + (Selected == DayUpgrade.Time ? 30 : 0);
    public int CashReward => 50;

    public void Offer(int day = 2) { Day = Math.Clamp(day, 2, 7); Pending = true; Selected = DayUpgrade.None; }
    public void Reset(bool preserveCategories = false)
    {
        Pending = false; Selected = DayUpgrade.None;
        if (!preserveCategories) { AtelierLevel = 0; MusicLevel = 0; AtelierPriceLevel = 0; MusicPriceLevel = 0; }
    }

    public bool TryChoose(DayUpgrade upgrade)
    {
        if (!Pending || !Options.Contains(upgrade)) return false;
        Selected = upgrade;
        Pending = false;
        if (upgrade == DayUpgrade.Atelier) AtelierLevel = Math.Min(6, AtelierLevel + 1);
        if (upgrade == DayUpgrade.Music) MusicLevel = Math.Min(6, MusicLevel + 1);
        if (upgrade == DayUpgrade.AtelierPrice) AtelierPriceLevel = Math.Min(6, AtelierPriceLevel + 1);
        if (upgrade == DayUpgrade.MusicPrice) MusicPriceLevel = Math.Min(6, MusicPriceLevel + 1);
        return true;
    }

    public void Restore(bool pending, DayUpgrade selected, int day = 2, int atelierLevel = 0, int musicLevel = 0,
        int atelierPriceLevel = 0, int musicPriceLevel = 0)
    {
        Day = Math.Clamp(day, 2, 7);
        AtelierLevel = Math.Clamp(atelierLevel, 0, 6);
        MusicLevel = Math.Clamp(musicLevel, 0, 6);
        AtelierPriceLevel = Math.Clamp(atelierPriceLevel, 0, 6);
        MusicPriceLevel = Math.Clamp(musicPriceLevel, 0, 6);
        Selected = Enum.IsDefined(selected) ? selected : DayUpgrade.None;
        Pending = pending && Selected == DayUpgrade.None;
    }

    public int Price(string workshop, int baseline)
    {
        if (baseline < 0) throw new ArgumentOutOfRangeException(nameof(baseline));
        int level = workshop == "atelier" ? AtelierPriceLevel : workshop == "music" ? MusicPriceLevel : 0;
        return checked((int)decimal.Round((decimal)baseline * (100 + level * 20) / 100, 0, MidpointRounding.AwayFromZero));
    }

    public int Weight(string workshop, string rarity, int baseline, int commonBaseline)
    {
        int level = workshop == "atelier" ? AtelierLevel : workshop == "music" ? MusicLevel : 0;
        int transfer = Math.Min(Math.Max(0, commonBaseline - 1), level * 15);
        int first = transfer * 8 / 15;
        int exclusive = transfer * 5 / 15;
        return baseline + (rarity switch
        {
            "common" => -transfer,
            "first_class" => first,
            "exclusive" => exclusive,
            "unique" => transfer - first - exclusive,
            _ => 0
        });
    }
}
