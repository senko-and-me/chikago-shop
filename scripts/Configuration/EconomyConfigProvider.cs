using ChikagoShop.Domain.Configuration;
using Godot;

public static class EconomyConfigProvider
{
    public const string ResourcePath = "res://config/economy.v1.json";
    private static readonly System.Lazy<EconomyConfig> BaselineConfig = new(LoadBaseline);
    public static EconomyConfig Baseline => BaselineConfig.Value;

    public static EconomyConfig Load() => OS.IsDebugBuild() ? DebugBalanceSettings.Economy : Baseline;

    private static EconomyConfig LoadBaseline()
    {
        string json = FileAccess.GetFileAsString(ResourcePath);
        if (string.IsNullOrWhiteSpace(json))
            throw new EconomyConfigException($"Unable to read economy config: {ResourcePath}");
        return EconomyConfigLoader.Load(json);
    }
}
