using ChikagoShop.Domain.Days;

namespace ChikagoShop.Domain.Persistence;

// Legacy interpretation is independent of Godot, disk paths and scene state.
public static class ProgressMigration
{
    // Unavailable marks historical saves from before the visitor event existed.
    // A pending or interrupted conversation must never be bypassed by loading.
    public static bool StoryWorkshopUnlocked(bool alreadyUnlocked, int day, int quotaStage,
        QuotaVisitStage visit, int firstDeadline) => alreadyUnlocked ||
        day > firstDeadline && quotaStage >= 1 &&
        visit is QuotaVisitStage.Rewarded or QuotaVisitStage.Unavailable;

    public static ProgressData Prepare(ProgressData data)
    {
        bool music = data.SaveVersion >= 7 && data.MusicWorkshopUnlocked;
        double? remaining = data.SaveVersion >= 8 ? data.DayRemainingSeconds : null;
        if (!data.DayClosed)
            remaining = DaySession.MigrateRemaining(remaining, data.SaveVersion, music,
                data.SaveVersion >= 9 && data.SelectedUpgrade == (int)DayUpgrade.Time);
        // Old seven-day saves that had already paid the first quota resume
        // at the equivalent point in the second week (old day 5 -> new day 8).
        int day = data.SaveVersion < 14 && data.QuotaStage == 1 && !data.Completed
            ? data.Day + 3 : data.Day;
        return data with
        {
            Day = day,
            StoredItems = MigrateItems(data.StoredItems, data.SaveVersion),
            CheckpointItems = MigrateItems(data.CheckpointItems, data.SaveVersion),
            ShowcaseItems = MigrateItems(data.ShowcaseItems, data.SaveVersion),
            CheckpointShowcaseItems = MigrateItems(data.CheckpointShowcaseItems, data.SaveVersion),
            ItemIndex = MigrateItem(data.ItemIndex, data.SaveVersion),
            MusicWorkshopUnlocked = music,
            CheckpointMusicWorkshopUnlocked = data.SaveVersion >= 7 && data.CheckpointMusicWorkshopUnlocked,
            DayRemainingSeconds = remaining,
            DayClosed = data.SaveVersion >= 8 && data.DayClosed,
            UpgradePending = data.SaveVersion >= 9 && data.UpgradePending,
            SelectedUpgrade = data.SaveVersion >= 9 ? data.SelectedUpgrade : (int)DayUpgrade.None
        };
    }

    private static int[] MigrateItems(int[]? items, int version) =>
        items?.Select(index => MigrateItem(index, version)).ToArray() ?? [];

    private static int MigrateItem(int index, int version) => version >= 19 ? index : index switch
    {
        10 => 1, // cloche_hat -> vest
        11 => 4, // shoes -> gloves
        12 => 5, // dress -> matteo_hat
        13 => 2, // tambourine -> guitar
        14 => 8, // saxophone -> violin
        15 => 9, // cello -> tuba
        _ => index
    };

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
