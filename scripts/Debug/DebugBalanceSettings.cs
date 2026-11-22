using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Days;
using Godot;

// Debug-only configuration; disk publication succeeds before the active profile is replaced.
public static class DebugBalanceSettings
{
    public const string DefaultPath = "user://debug_balance.json";
    public static string StoragePath { get; set; } = DefaultPath;
    private static BalanceProfile _profile = new();
    private static EconomyConfig _economy;
    private static bool _loaded;
    private static int _revision;
    private static IReadOnlyList<BalanceField> _fields;
    public static IReadOnlyList<BalanceField> Fields => _fields ??= BuildFields();
    public static int Revision { get { EnsureLoaded(); return _revision; } }
    public static bool HasOverrides { get { EnsureLoaded(); return OS.IsDebugBuild() && (_profile.Numbers.Count > 0 || _profile.ItemRarities.Count > 0); } }
    public static BalanceProfile Profile { get { EnsureLoaded(); return _profile.Clone(); } }
    public static EconomyConfig Economy { get { EnsureLoaded(); return _economy ?? EconomyConfigProvider.Baseline; } }
    public static double Number(string key)
    {
        var field = Fields.Single(entry => entry.Key == key);
        EnsureLoaded();
        return OS.IsDebugBuild() ? _profile.Number(key, field.Default) : field.Default;
    }
    public static DayUpgradeRules CardRules => new()
    {
        Costs = Cards.ToDictionary(card => card, card => (int)Number($"card.{card}.cost")),
        Weights = Cards.ToDictionary(card => card, card => (int)Number($"card.{card}.weight")),
        WeightedSelection = Number("cards.weighted") == 1,
        Seed = (int)Number("cards.seed")
    };
    public static DayUpgrade[] Cards => Enum.GetValues<DayUpgrade>().Where(card => card is not (DayUpgrade.None or DayUpgrade.Time)).ToArray();
    public static string CardName(DayUpgrade card) => card switch
    {
        DayUpgrade.Cash => "Оборотные средства", DayUpgrade.Energy => "Второе дыхание",
        DayUpgrade.Atelier => "Мастерство портного", DayUpgrade.Music => "Мастерство мастера",
        DayUpgrade.AtelierPrice => "Имя на этикетке", _ => "Коллекционный звук"
    };
    private static void EnsureLoaded() { if (!_loaded) Reload(out _); }
    public static bool Reload(out string error)
    {
        _loaded = true;
        try
        {
            var profile = OS.IsDebugBuild() && Godot.FileAccess.FileExists(StoragePath)
                ? BalanceProfile.FromJson(Godot.FileAccess.GetFileAsString(StoragePath)) : new BalanceProfile();
            RemoveRetiredOverrides(profile);
            // One-time migration of the old shipped first-quota defaults in existing F10 profiles.
            // Later explicit changes (even back to 250 or 500) remain fully editable.
            bool migrated = MigrateFirstQuotaBaseline(profile) | MigrateEconomyBaseline(profile);
            Validate(profile);
            if (migrated && OS.IsDebugBuild() && Godot.FileAccess.FileExists(StoragePath))
            {
                try
                {
                    string path = ProjectSettings.GlobalizePath(StoragePath);
                    string temporary = path + ".economy1500.tmp";
                    System.IO.File.WriteAllText(temporary, profile.ToJson());
                    System.IO.File.Move(temporary, path, overwrite: true);
                }
                catch (Exception exception)
                {
                    GD.PushWarning("Не удалось сохранить обновлённый профиль квоты: " + exception.Message);
                }
            }
            Publish(profile);
            error = "";
            return true;
        }
        catch (Exception exception)
        {
            Publish(new BalanceProfile());
            error = exception.Message;
            GD.PushWarning("Профиль дебаг-баланса не загружен: " + error);
            return false;
        }
    }
    public static bool TrySave(BalanceProfile profile, out string error)
    {
        string temporary = null;
        try
        {
            if (!OS.IsDebugBuild()) throw new InvalidOperationException("Панель доступна только в debug-сборке.");
            RemoveRetiredOverrides(profile);
            // This is an explicit edit, so do not migrate this first-quota choice later.
            profile.FirstQuotaBaselineRevision = 1;
            profile.EconomyBaselineRevision = 1;
            Validate(profile);
            string path = ProjectSettings.GlobalizePath(StoragePath);
            temporary = path + ".tmp";
            System.IO.File.WriteAllText(temporary, profile.ToJson());
            System.IO.File.Move(temporary, path, overwrite: true);
            Publish(profile);
            error = "";
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
        finally { if (temporary != null && System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary); }
    }
    private static bool MigrateFirstQuotaBaseline(BalanceProfile profile)
    {
        if (profile.FirstQuotaBaselineRevision >= 1) return false;
        if (profile.Numbers != null && profile.Numbers.TryGetValue("quota.first", out double oldQuota) &&
            (oldQuota == 250 || oldQuota == 500))
            profile.Numbers.Remove("quota.first");
        profile.FirstQuotaBaselineRevision = 1;
        return true;
    }

    private static bool MigrateEconomyBaseline(BalanceProfile profile)
    {
        // Older F10 profiles can pin the old defaults even after the game's config changes.
        // Replace only known shipped values; preserve different deliberate user settings.
        if (profile.EconomyBaselineRevision >= 1) return false;
        if (profile.Numbers != null)
        {
            if (profile.Numbers.TryGetValue("quota.final", out double finalQuota) && finalQuota == 450)
                profile.Numbers.Remove("quota.final");
            if (profile.Numbers.TryGetValue("workshop.music.unlock", out double unlockCost) &&
                (unlockCost == 100 || unlockCost == 500))
                profile.Numbers.Remove("workshop.music.unlock");
        }
        profile.EconomyBaselineRevision = 1;
        return true;
    }

    private static void RemoveRetiredOverrides(BalanceProfile profile)
    {
        profile.Numbers?.Remove("card.Time.cost");
        profile.Numbers?.Remove("card.Time.weight");
        profile.Numbers?.Remove("buyer.min");
        profile.Numbers?.Remove("buyer.max");
        // A legacy profile may have disabled every other card and relied on Time.
        if (profile.Numbers != null && Cards.All(card => profile.Numbers.GetValueOrDefault($"card.{card}.weight", 1) == 0))
            profile.Numbers["card.Cash.weight"] = 1;
    }
    private static void Publish(BalanceProfile profile)
    {
        _profile = profile.Clone();
        _economy = BuildEconomy(_profile);
        _revision++;
    }
    public static void Validate(BalanceProfile profile)
    {
        var baseline = EconomyConfigProvider.Baseline;
        profile.Validate(Fields, WorkshopItemCatalog.BaselineItems.Select(item => item.Kind).ToHashSet(),
            baseline.Rarities.Select(rarity => rarity.Id).ToHashSet());
        double Get(string key) => profile.Number(key, Fields.Single(field => field.Key == key).Default);
        if (Get("energy.after") < Get("energy.before")) throw new ArgumentException("Энергия после открытия мастерской должна быть не ниже начальной.");
        if (Cards.Sum(card => Get($"card.{card}.weight")) <= 0) throw new ArgumentException("Хотя бы одна карта должна иметь положительный вес.");
        foreach (string workshop in new[] { "atelier", "music" })
        {
            bool available = WorkshopItemCatalog.BaselineItems.Where(item => item.WorkshopId == workshop).Any(item =>
                Get($"rarity.{profile.ItemRarities.GetValueOrDefault(item.Kind, item.RarityId)}.weight") > 0);
            if (!available) throw new ArgumentException($"В мастерской {workshop} нет предметов с положительным шансом создания.");
        }
    }
    private static EconomyConfig BuildEconomy(BalanceProfile profile)
    {
        var source = EconomyConfigProvider.Baseline;
        int Get(string key, int fallback) => (int)profile.Number(key, fallback);
        return new EconomyConfig
        {
            SchemaVersion = source.SchemaVersion, IsProvisional = source.IsProvisional,
            StartingMoney = Get("start.cash", source.StartingMoney),
            Days = new DayConfig
            {
                TotalDays = source.Days.TotalDays, TutorialDay = source.Days.TutorialDay,
                DurationSeconds = Get("day.seconds", source.Days.DurationSeconds),
                EnergyBeforeMusicUnlock = Get("energy.before", source.Days.EnergyBeforeMusicUnlock),
                EnergyAfterMusicUnlock = Get("energy.after", source.Days.EnergyAfterMusicUnlock),
                ItemsPerCraft = Get("craft.count", source.Days.ItemsPerCraft)
            },
            Workshops = source.Workshops.Select(workshop => new WorkshopConfig
            {
                Id = workshop.Id, SpinCost = Get($"workshop.{workshop.Id}.cost", workshop.SpinCost),
                UnlockCost = Get($"workshop.{workshop.Id}.unlock", workshop.UnlockCost)
            }).ToList(),
            Rarities = source.Rarities.Select(rarity => new RarityConfig
            {
                Id = rarity.Id, Name = rarity.Name, Weight = Get($"rarity.{rarity.Id}.weight", rarity.Weight),
                Rerolls = Get($"rarity.{rarity.Id}.rerolls", rarity.Rerolls), BasePrices = new(rarity.BasePrices)
            }).ToList(),
            OfferRanges = source.OfferRanges,
            Quotas = source.Quotas.Select(quota => new QuotaConfig
            { Id = quota.Id, DeadlineDay = quota.DeadlineDay, Amount = Get($"quota.{quota.Id}", quota.Amount) }).ToList()
        };
    }
    private static IReadOnlyList<BalanceField> BuildFields()
    {
        var config = EconomyConfigProvider.Baseline;
        var fields = new List<BalanceField>
        {
            new("start.cash", "Партия", "Стартовые деньги ($) · новая игра", config.StartingMoney, 0, 1000000),
            new("energy.before", "Партия", "Энергия до открытия музыки · новая игра", config.Days.EnergyBeforeMusicUnlock, 1, 99),
            new("energy.after", "Партия", "Энергия после открытия музыки", config.Days.EnergyAfterMusicUnlock, 1, 99),
            new("day.seconds", "Партия", "День до музыки (сек.; после +30)", config.Days.DurationSeconds, 5, 3600),
            new("craft.count", "Создание", "Предметов за один крафт", config.Days.ItemsPerCraft, 1, 50),
            new("hand.enabled", "Партия", "Невидимая рука: 0 — выкл., 1 — вкл.", 1, 0, 1),
            new("hand.rarity", "Редкости", "Невидимая рука: предел редкости (п.п.)", 5, 0, 5),
            new("hand.offer", "Партия", "Невидимая рука: предел предложения (п.п.)", 3, 0, 3),
            new("craft.atelier.seconds", "Создание", "Время вышивки (сек.)", 10, 1, 600),
            new("craft.music.seconds", "Создание", "Время полировки (сек.)", 10, 1, 600),
            new("cards.weighted", "Карты", "Выбор карт: 0 — расписание, 1 — по весам", 0, 0, 1),
            new("cards.seed", "Карты", "Seed выбора карт (повторяемый результат)", 1, 0, 1000000)
        };
        foreach (var quota in config.Quotas)
            fields.Add(new($"quota.{quota.Id}", "Партия", $"Квота к концу дня {quota.DeadlineDay} ($)", quota.Amount, 1, 1000000));
        foreach (var workshop in config.Workshops)
        {
            string name = workshop.Id == "atelier" ? "Ателье" : "Музыка";
            fields.Add(new($"workshop.{workshop.Id}.cost", "Создание", name + ": цена одного крафта ($)", workshop.SpinCost));
            if (workshop.UnlockCost > 0) fields.Add(new($"workshop.{workshop.Id}.unlock", "Создание", name + ": цена открытия ($)", workshop.UnlockCost));
        }
        foreach (var rarity in config.Rarities)
        {
            fields.Add(new($"rarity.{rarity.Id}.weight", "Редкости", rarity.Name + ": вес создания", rarity.Weight, 0, 1000));
            fields.Add(new($"rarity.{rarity.Id}.rerolls", "Редкости", rarity.Name + ": смен покупателя", rarity.Rerolls, 0, 50));
        }
        foreach (var card in Cards)
        {
            fields.Add(new($"card.{card}.cost", "Карты", CardName(card) + ": цена ($)", DayUpgradeSession.Cost(card)));
            fields.Add(new($"card.{card}.weight", "Карты", CardName(card) + ": вес выбора", 1, 0, 1000));
        }
        foreach (var item in WorkshopItemCatalog.BaselineItems)
            fields.Add(new($"item.{item.Kind}.price", "Предметы", item.Name + ": базовая цена ($)", item.BasePrice, 1, 100000));
        return fields;
    }
}
