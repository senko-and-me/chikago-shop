using ChikagoShop.Domain.Days;

namespace ChikagoShop.Domain.Persistence;

// Legacy interpretation is independent of Godot, disk paths and scene state.
public static class ProgressMigration
{
    public static ProgressData Prepare(ProgressData data)
    {
        bool music = data.SaveVersion >= 7 && data.MusicWorkshopUnlocked;
        double? remaining = data.SaveVersion >= 8 ? data.DayRemainingSeconds : null;
        if (!data.DayClosed)
            remaining = DaySession.MigrateRemaining(remaining, data.SaveVersion, music,
                data.SaveVersion >= 9 && data.SelectedUpgrade == (int)DayUpgrade.Time);
        return data with
        {
            MusicWorkshopUnlocked = music,
            CheckpointMusicWorkshopUnlocked = data.SaveVersion >= 7 && data.CheckpointMusicWorkshopUnlocked,
            DayRemainingSeconds = remaining,
            DayClosed = data.SaveVersion >= 8 && data.DayClosed,
            UpgradePending = data.SaveVersion >= 9 && data.UpgradePending,
            SelectedUpgrade = data.SaveVersion >= 9 ? data.SelectedUpgrade : (int)DayUpgrade.None
        };
    }

    public static int Energy(int version, int savedEnergy, int capacity, bool musicUnlocked) =>
        version < 6 ? capacity : version < 12
            ? Math.Clamp(capacity - Math.Max(0, (musicUnlocked ? 2 : 1) - savedEnergy), 0, capacity)
            : savedEnergy;

    public static int CustomerIndex(ProgressData data) =>
        data.SaveVersion < 3 && data.CustomerIndex == 1 ? 3 : data.CustomerIndex;

    public static int Rerolls(ProgressData data, int itemLimit) =>
        data.SaveVersion < 2 && data.ItemIndex >= 0
            ? (itemLimit >= 0 ? itemLimit : Math.Clamp(data.RerollsRemaining, 0, 3))
            : Math.Clamp(data.RerollsRemaining, 0, 3);
}
