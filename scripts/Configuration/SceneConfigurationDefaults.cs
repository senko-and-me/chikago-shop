using System.Linq;
using ChikagoShop.Domain.Configuration;

// Repair missing editor-export defaults after scene deserialization, before game initialization.
public static class SceneConfigurationDefaults
{
    public static void Apply(TradeScene scene, EconomyConfig config)
    {
        if (scene.StartingMoney <= 0) scene.StartingMoney = config.StartingMoney;
        if (scene.FirstQuota <= 0) scene.FirstQuota = config.Quotas.Single(quota => quota.Id == "first").Amount;
        if (scene.SecondQuota <= 0) scene.SecondQuota = config.Quotas.Single(quota => quota.Id == "final").Amount;
        // An empty path deliberately disables I/O in fixtures; only null means missing.
        scene.SavePath ??= "user://trade_progress.json";
    }
}
