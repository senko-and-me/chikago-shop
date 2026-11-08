using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestLocalizedMenuAssets()
    {
        string locale = TranslationServer.GetLocale();
        var menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
        Root.AddChild(menu);
        try
        {
            foreach (string language in new[] { "ru", "en" })
            {
                menu.GetNode<Button>("SettingsMenuContainer/Settings/LanguageRow/" +
                    (language == "en" ? "EnglishButton" : "RussianButton"))
                    .EmitSignal(BaseButton.SignalName.Pressed);
                await ToSignal(this, SignalName.ProcessFrame);
                string prefix = $"res://art/ui/{language}/";
                foreach (var entry in new[] { ("NewGameButton", "new_game.png"), ("ContinueButton", "continue.png") })
                    Check(menu.GetNode<TextureButton>("MainMenuContainer/Menu/" + entry.Item1).TextureNormal?.ResourcePath == prefix + entry.Item2,
                        "Choice buttons must use renamed localized textures");
                var fullscreen = menu.GetNode<TextureButton>("SettingsMenuContainer/Settings/FullscreenButton");
                Check(fullscreen.TextureNormal?.ResourcePath is string path &&
                    (path == prefix + "fullscreen_on.png" || path == prefix + "fullscreen_off.png"),
                    "Fullscreen button must use renamed localized art");
                Check(menu.GetNode<TextureButton>("MainMenuContainer/Menu/PlayButton").TextureNormal != null,
                    "Dynamically constructed play texture path must load");
            }
        }
        finally
        {
            menu.QueueFree();
            TranslationServer.SetLocale(locale);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private static void TestResourceOrganization()
    {
        var font = GD.Load<FontFile>("res://art/fonts/game.ttf");
        Check(font != null, "The canonical game font must load");
        string supported = font.GetSupportedChars();
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        foreach (char character in alphabet)
            Check(supported.Contains(character), $"Game font must contain its own glyph for {character}");
        Check(!FileAccess.FileExists("res://MyFont_Completed.ttf"), "The root duplicate font must be removed");
        CheckArtDirectory("res://art");
        foreach (string scene in new[] { "main_menu", "trade_scene", "customer" })
            Check(GD.Load<PackedScene>($"res://scenes/{scene}.tscn") != null, $"Scene must load: {scene}");
    }

    private static void CheckArtDirectory(string path)
    {
        using var directory = DirAccess.Open(path);
        Check(directory != null, $"Art directory must exist: {path}");
        foreach (string folder in directory.GetDirectories())
            CheckArtDirectory(path + "/" + folder);
        foreach (string file in directory.GetFiles().Where(file => !file.EndsWith(".import")))
        {
            Check(file.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '.'),
                $"Resource names must use lowercase ASCII snake_case: {path}/{file}");
            Check(GD.Load<Resource>(path + "/" + file) != null, $"Resource must load after rename: {path}/{file}");
        }
    }
}
