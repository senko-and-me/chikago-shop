using System.Text.Json;
using ChikagoShop.Domain.Persistence;
using Godot;

// Godot user:// I/O only. The domain owns the versioned JSON contract.
public static class ProgressSaveStore
{
    public static void Save(string path, ProgressData data)
    {
        if (string.IsNullOrEmpty(path)) return;
        string destination = ProjectSettings.GlobalizePath(path);
        string temporary = destination + ".tmp";
        try
        {
            System.IO.File.WriteAllText(temporary, ProgressJson.Serialize(data));
            System.IO.File.Move(temporary, destination, overwrite: true);
        }
        catch (System.Exception exception) when (exception is System.IO.IOException or System.UnauthorizedAccessException)
        {
            GD.PushWarning($"Не удалось сохранить прогресс: {path} ({exception.Message})");
        }
    }

    public static ProgressData Load(string path)
    {
        if (string.IsNullOrEmpty(path) || !FileAccess.FileExists(path)) return null;
        try { return ProgressJson.Deserialize(FileAccess.GetFileAsString(path)); }
        catch (JsonException) { return null; }
    }
}
