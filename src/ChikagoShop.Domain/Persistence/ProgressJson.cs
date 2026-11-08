using System.Text.Json;

namespace ChikagoShop.Domain.Persistence;

public static class ProgressJson
{
    public const int CurrentVersion = 13;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static string Serialize(ProgressData data) => JsonSerializer.Serialize(data, Options);

    public static ProgressData? Deserialize(string json, int defaultEnergy)
    {
        var data = JsonSerializer.Deserialize<ProgressData>(json, Options);
        if (data is null) return null;
        // Historical saves can omit energy fields. Their defaults follow the current economy.
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("energy_remaining", out _))
            data.EnergyRemaining = defaultEnergy;
        if (!document.RootElement.TryGetProperty("checkpoint_energy", out _))
            data.CheckpointEnergy = defaultEnergy;
        return data;
    }
}
