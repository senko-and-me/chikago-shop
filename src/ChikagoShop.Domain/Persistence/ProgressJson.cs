using System.Text.Json;

namespace ChikagoShop.Domain.Persistence;

public static class ProgressJson
{
    public const int CurrentVersion = 20;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static string Serialize(ProgressData data) => JsonSerializer.Serialize(data, Options);

    public static ProgressData? Deserialize(string json) => JsonSerializer.Deserialize<ProgressData>(json, Options);
}
