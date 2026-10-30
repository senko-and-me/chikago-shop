using ChikagoShop.Domain.Configuration;
using Godot;

public static class EconomyConfigProvider
{
    public const string ResourcePath = "res://config/economy.v1.json";

    public static EconomyConfig Load()
    {
        string json = FileAccess.GetFileAsString(ResourcePath);
        if (string.IsNullOrWhiteSpace(json))
            throw new EconomyConfigException($"Unable to read economy config: {ResourcePath}");
        return EconomyConfigLoader.Load(json);
    }
}
