using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;
using Godot;

// Debug-only configuration; disk publication succeeds before the active profile is replaced.
public static class DebugBalanceSettings
{
    internal const string FirstWeekDurationKey = "day.first.seconds";
    internal const string SecondWeekDurationKey = "day.second.seconds";
    internal const string BasePriceKey = "items.base_price";
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
        Terms = Economy.Cards, Seed = (int)Number("cards.seed")
    };
    public static DayUpgrade[] Cards => DayUpgradeRules.Available.ToArray();
    internal const string CraftCostKey = "items.craft_cost";
    public static string CardName(DayUpgrade card) => card switch
    {
        DayUpgrade.Cash => "Оборотные средства",
        DayUpgrade.Materials => "Дешёвые материалы",
        DayUpgrade.AtelierPrice => "Мастерство портного",
        DayUpgrade.MusicPrice => "Мастерство музыканта",
        DayUpgrade.Patient => "Терпеливые клиенты",
        DayUpgrade.QualityMark => "Знак качества",
        DayUpgrade.Loyal => "Лояльные покупатели",
        DayUpgrade.QuickService => "Быстрое обслуживание",
        _ => card.ToString()
    };
    private static void EnsureLoaded() { if (!_loaded) Reload(out _); }
    public static bool Reload(out string error)
    {
        _loaded = true;
        try
        {
            var profile = OS.IsDebugBuild() && Godot.FileAccess.FileExists(StoragePath)
                ? BalanceProfile.FromJson(Godot.FileAccess.GetFileAsString(StoragePath)) : new BalanceProfile();
            bool retired = RemoveRetiredOverrides(profile);
            // One-time migration of the old shipped first-quota defaults in existing F10 profiles.
            // Later explicit changes (even back to 250 or 500) remain fully editable.
            bool migrated = retired | MigrateFirstQuotaBaseline(profile) | MigrateEconomyBaseline(profile) | MigratePolishingStrength(profile) | CapDayCardCosts(profile);
            Validate(profile);
            if (migrated && OS.IsDebugBuild() && Godot.FileAccess.FileExists(StoragePath))
            {
                try
                {
                    string path = ProjectSettings.GlobalizePath(StoragePath);
                    string temporary = path + ".migration.tmp";
                    System.IO.File.WriteAllText(temporary, profile.ToJson());
                    System.IO.File.Move(temporary, path, overwrite: true);
                }
                catch (Exception exception)
                {
                    GD.PushWarning("Не удалось сохранить обновлённый профиль баланса: " + exception.Message);
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
            CapDayCardCosts(profile);
            // This is an explicit edit, so do not migrate this first-quota choice later.
            profile.FirstQuotaBaselineRevision = 1;
            profile.EconomyBaselineRevision = 2;
            profile.ItemCatalogueRevision = 1;
            profile.PolishingStrengthRevision = 1;
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
        if (profile.EconomyBaselineRevision >= 2) return false;
        if (profile.Numbers != null)
        {
            var numbers = profile.Numbers;
            if (numbers.Remove("day.seconds", out double oldDuration) && oldDuration != 60)
            {
                numbers.TryAdd(FirstWeekDurationKey, oldDuration);
                numbers.TryAdd(SecondWeekDurationKey, Math.Min(oldDuration + 30, 3600));
            }
            if (numbers.GetValueOrDefault("start.cash") == 220) numbers.Remove("start.cash");
            if (numbers.GetValueOrDefault("quota.first") == 750) numbers.Remove("quota.first");
            if (numbers.GetValueOrDefault("quota.final") is 450 or 1500) numbers.Remove("quota.final");
            var previousCards = new Dictionary<DayUpgrade, CardTerms>
            {
                [DayUpgrade.Cash] = new(0, 15), [DayUpgrade.Materials] = new(13, 7),
                [DayUpgrade.AtelierPrice] = new(20, 10), [DayUpgrade.MusicPrice] = new(20, 10),
                [DayUpgrade.Patient] = new(11, 3),
                [DayUpgrade.QualityMark] = new(8, 8), [DayUpgrade.Loyal] = new(12, 20),
                [DayUpgrade.QuickService] = new(5, 7)
            };
            foreach (var (card, terms) in previousCards)
            {
                string cost = $"card.{card}.cost", amount = $"card.{card}.amount";
                if (numbers.TryGetValue(cost, out double price) && price == terms.Cost) numbers.Remove(cost);
                if (numbers.TryGetValue(amount, out double effect) && effect == terms.Amount) numbers.Remove(amount);
            }
            if (profile.Numbers.TryGetValue("workshop.music.unlock", out double unlockCost) &&
                (unlockCost == 100 || unlockCost == 500))
                profile.Numbers.Remove("workshop.music.unlock");
        }
        profile.EconomyBaselineRevision = 2;
        return true;
    }

    internal static bool MigratePolishingStrength(BalanceProfile profile)
    {
        if (profile.PolishingStrengthRevision >= 1) return false;
        if (profile.Numbers != null)
        {
            foreach (var item in WorkshopItemCatalog.BaselineItems.Where(item => item.WorkshopId == "music"))
            {
                string key = $"polish.{item.Kind}.strength";
                if (profile.Numbers.TryGetValue(key, out double value))
                    profile.Numbers[key] = Math.Round(value * PolishingSession.CleaningEfficiencyScale, 4);
            }
        }
        profile.PolishingStrengthRevision = 1;
        return true;
    }

    private static bool RemoveRetiredOverrides(BalanceProfile profile)
    {
        int oldCount = (profile.Numbers?.Count ?? 0) + (profile.ItemRarities?.Count ?? 0);
        bool removedLegacy = false;
        profile.ItemRarities?.Clear();
        profile.Numbers?.Remove("card.Time.cost");
        profile.Numbers?.Remove("card.Bargaining.cost");
        profile.Numbers?.Remove("card.Bargaining.amount");
        if (profile.Numbers?.GetValueOrDefault("card.Cash.cost") == 0) profile.Numbers.Remove("card.Cash.cost");
        profile.Numbers?.Remove("items.rerolls");
        profile.Numbers?.Remove("card.Time.weight");
        profile.Numbers?.Remove("card.Energy.cost");
        profile.Numbers?.Remove("card.Energy.weight");
        profile.Numbers?.Remove("energy.before");
        profile.Numbers?.Remove("energy.after");
        profile.Numbers?.Remove("workshop.music.unlock");
        profile.Numbers?.Remove("card.Atelier.cost");
        profile.Numbers?.Remove("card.Atelier.weight");
        profile.Numbers?.Remove("card.Music.cost");
        profile.Numbers?.Remove("card.Music.weight");
        profile.Numbers?.Remove("craft.count");
        profile.Numbers?.Remove("craft.atelier.seconds");
        profile.Numbers?.Remove("craft.music.seconds");
        profile.Numbers?.Remove("hand.rarity");
        foreach (var key in profile.Numbers?.Keys.ToArray() ?? [])
        {
            bool oldCard = key == "cards.weighted" || key.StartsWith("card.", StringComparison.Ordinal) && (key.EndsWith(".weight", StringComparison.Ordinal) || key.EndsWith(".limit", StringComparison.Ordinal));
            bool oldCost = key.StartsWith("craft.", StringComparison.Ordinal) && key.EndsWith(".cost", StringComparison.Ordinal);
            bool oldPrice = key.StartsWith("item.", StringComparison.Ordinal) && key.EndsWith(".price", StringComparison.Ordinal);
            bool oldWait = key.StartsWith("buyer.", StringComparison.Ordinal) && key is not ("buyer.wait.min" or "buyer.wait.max");
            bool oldRerolls = key.StartsWith("item.", StringComparison.Ordinal) && key.EndsWith(".rerolls", StringComparison.Ordinal);
            if (oldCost || oldPrice || oldWait || oldRerolls || oldCard) removedLegacy |= profile.Numbers.Remove(key);
        }
        foreach (var workshop in EconomyConfigProvider.Baseline.Workshops)
            profile.Numbers?.Remove($"workshop.{workshop.Id}.cost");
        foreach (var rarity in EconomyConfigProvider.Baseline.Rarities)
        {
            profile.Numbers?.Remove($"rarity.{rarity.Id}.weight");
            profile.Numbers?.Remove($"rarity.{rarity.Id}.rerolls");
        }
        return removedLegacy || oldCount != (profile.Numbers?.Count ?? 0) + (profile.ItemRarities?.Count ?? 0);
    }
    private static bool CapDayCardCosts(BalanceProfile profile)
    {
        if (profile.Numbers == null) return false;
        bool changed = false;
        foreach (DayUpgrade card in Cards)
        {
            string key = $"card.{card}.cost";
            if (!profile.Numbers.TryGetValue(key, out double cost) || cost <= DayUpgradeRules.MaximumCost) continue;
            profile.Numbers[key] = DayUpgradeRules.MaximumCost;
            changed = true;
        }
        return changed;
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
        if (Get("card.Cash.amount") <= Get("card.Cash.cost"))
            throw new ArgumentException(GameLocalization.Text("Денежная карточка должна давать больше, чем стоит."));
        if (Get("buyer.wait.min") > Get("buyer.wait.max"))
            throw new ArgumentException(GameLocalization.Text("Минимум ожидания покупателя должен быть не больше максимума."));
    }
    private static EconomyConfig BuildEconomy(BalanceProfile profile)
    {
        var source = EconomyConfigProvider.Baseline;
        int Get(string key, int fallback) => (int)profile.Number(key, fallback);
        return new EconomyConfig
        {
            SchemaVersion = source.SchemaVersion, IsProvisional = source.IsProvisional,
            StartingMoney = Get("start.cash", source.StartingMoney),
            Items = source.Items,
            BasePrice = Get(BasePriceKey, source.BasePrice),
            CraftCost = Get(CraftCostKey, source.CraftCost),
            Cards = source.Cards.ToDictionary(pair => pair.Key, pair => new CardTerms(
                Get($"card.{pair.Key}.cost", pair.Value.Cost), Get($"card.{pair.Key}.amount", pair.Value.Amount))),
            BuyerWaitMinSeconds = Get("buyer.wait.min", source.BuyerWaitMinSeconds),
            BuyerWaitMaxSeconds = Get("buyer.wait.max", source.BuyerWaitMaxSeconds),
            Days = new DayConfig
            {
                TotalDays = source.Days.TotalDays, TutorialDay = source.Days.TutorialDay,
                DurationSeconds = Get(FirstWeekDurationKey, source.Days.DurationSeconds),
                SecondWeekDurationSeconds = Get(SecondWeekDurationKey, source.Days.SecondWeekDurationSeconds),
                EnergyBeforeMusicUnlock = source.Days.EnergyBeforeMusicUnlock,
                EnergyAfterMusicUnlock = source.Days.EnergyAfterMusicUnlock,
                ItemsPerCraft = 1
            },
            Workshops = source.Workshops.Select(workshop => new WorkshopConfig
            {
                Id = workshop.Id,
                UnlockCost = workshop.UnlockCost
            }).ToList(),
            Rarities = source.Rarities.Select(rarity => new RarityConfig
            {
                Id = rarity.Id, Name = rarity.Name, Weight = rarity.Weight,
                BasePrices = new(rarity.BasePrices)
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
            new(FirstWeekDurationKey, "Партия", "Длительность дня первой недели (сек.)", config.Days.DurationSeconds, 5, 3600),
            new(SecondWeekDurationKey, "Партия", "Длительность дня второй недели (сек.)", config.Days.SecondWeekDurationSeconds, 5, 3600),
            new("hand.enabled", "Партия", "Невидимая рука: 0 — выкл., 1 — вкл.", 1, 0, 1),
            new("hand.offer", "Партия", "Невидимая рука: предел предложения (п.п.)", 3, 0, 3),
            new("buyer.wait.min", "Партия", "Минимум ожидания покупателя (сек.)", config.BuyerWaitMinSeconds, 1, 600),
            new("buyer.wait.max", "Партия", "Максимум ожидания покупателя (сек.)", config.BuyerWaitMaxSeconds, 1, 600),
            new("cards.seed", "Карты", "Seed выбора карт (повторяемый результат)", 1, 0, 1000000)
        };
        foreach (var quota in config.Quotas)
            fields.Add(new($"quota.{quota.Id}", "Партия", $"Квота к концу дня {quota.DeadlineDay} ($)", quota.Amount, 1, 1000000));
        foreach (var workshop in config.Workshops)
        {
            string name = workshop.Id == "atelier" ? "Ателье" : "Музыка";
            if (workshop.UnlockCost > 0) fields.Add(new($"workshop.{workshop.Id}.unlock", "Создание", name + ": цена открытия ($)", workshop.UnlockCost));
        }
        fields.Add(new(CraftCostKey, "Создание", "Себестоимость любого товара ($)", config.CraftCost, 1, 1000000));
        foreach (var card in Cards)
        {
            var terms = config.Cards[card];
            fields.Add(new($"card.{card}.cost", "Карты", CardName(card) + ": цена ($)", terms.Cost, 1, DayUpgradeRules.MaximumCost));
            fields.Add(new($"card.{card}.amount", "Карты", CardName(card) + ": величина эффекта", terms.Amount, 0,
                    card == DayUpgrade.Patient ? 600 : card == DayUpgrade.Loyal ? 50 : 1000000));
        }
        foreach (var item in WorkshopItemCatalog.BaselineItems.Where(item => item.WorkshopId == "music"))
        {
            fields.Add(new($"polish.{item.Kind}.radius", "Создание", item.Name + ": поправка радиуса полировки", item.Kind == "banjo" ? .97 : 1, .65, 1.55, .01));
            double strength = item.Kind switch
            {
                "guitar" => .67,
                "violin" => .63,
                "banjo" => .64,
                "tuba" => .82,
                "drum" => 1.16,
                _ => 1
            };
            fields.Add(new($"polish.{item.Kind}.strength", "Создание", item.Name + ": поправка силы полировки", Math.Round(strength * PolishingSession.CleaningEfficiencyScale, 4), .5, 3.125, .0025));
        }
        fields.Add(new(BasePriceKey, "Предметы", "Базовая цена всех товаров ($)", config.BasePrice, 1, 100000));
        return fields;
    }
}
