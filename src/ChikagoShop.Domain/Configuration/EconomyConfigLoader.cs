using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChikagoShop.Domain.Configuration;

public static class EconomyConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static EconomyConfig Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new EconomyConfigException("Economy config is empty.");

        try
        {
            EconomyConfig config = JsonSerializer.Deserialize<EconomyConfig>(json, JsonOptions)
                ?? throw new EconomyConfigException("Economy config deserialized to null.");
            EconomyConfigValidator.Validate(config);
            return config;
        }
        catch (JsonException error)
        {
            throw new EconomyConfigException("Economy config is not valid JSON.", error);
        }
    }

    public static EconomyConfig Load(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        return Load(reader.ReadToEnd());
    }
}
