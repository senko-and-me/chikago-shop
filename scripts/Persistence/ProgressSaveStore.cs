using System.Text.Json;
using ChikagoShop.Domain.Persistence;
using Godot;

// Godot user:// I/O only. The domain owns the versioned JSON contract.
public static class ProgressSaveStore
{
    public static void Save(string path, ProgressData data)
    {
        if (string.IsNullOrEmpty(path)) return;
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushWarning($"Не удалось сохранить прогресс: {path}");
            return;
        }
        file.StoreString(ProgressJson.Serialize(data));
    }

    public static ProgressData Load(string path, int defaultEnergy)
    {
        if (string.IsNullOrEmpty(path) || !FileAccess.FileExists(path)) return null;
        try { return ProgressJson.Deserialize(FileAccess.GetFileAsString(path), defaultEnergy); }
        catch (JsonException) { return null; }
    }
}
