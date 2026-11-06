using System.Globalization;
using System.Text.RegularExpressions;
using Godot;

// Russian source messages remain stable keys, including in existing gameplay saves.
public partial class GameLocalization : Node
{
    public const string SettingsPath = "user://settings.cfg";
    private static readonly Regex MissingQuota = new(
        @"^Не хватает (\d+)\$ на квоту\. Вернитесь к контрольной точке\.$",
        RegexOptions.CultureInvariant);

    public static string Language => TranslationServer.GetLocale().Split('_')[0];

    public override void _Ready()
    {
        var english = GD.Load<Translation>("res://localization/en.tres");
        var russian = new Translation { Locale = "ru" };
        foreach (string source in english.GetMessageList())
        {
            russian.AddMessage(source, source);
        }
        TranslationServer.AddTranslation(english);
        TranslationServer.AddTranslation(russian);
        LoadPreference();
    }

    public static string Text(string source) => TranslationServer.Translate(source).ToString();

    public static string Format(string source, params object[] arguments) =>
        string.Format(CultureInfo.InvariantCulture, Text(source), arguments);

    public static string Status(string source)
    {
        var match = MissingQuota.Match(source ?? "");
        return match.Success
            ? Format("Не хватает {0}$ на квоту. Вернитесь к контрольной точке.", match.Groups[1].Value)
            : Text(source ?? "");
    }

    // ConfigFile preserves other settings and keeps locale separate from gameplay saves.
    public static Error SelectLanguage(string language, string path = SettingsPath)
    {
        if (language is not ("ru" or "en")) return Error.InvalidParameter;
        var settings = new ConfigFile();
        settings.Load(path);
        settings.SetValue("interface", "language", language);
        Error error = settings.Save(path);
        TranslationServer.SetLocale(language);
        return error;
    }

    public static void LoadPreference(string path = SettingsPath)
    {
        var settings = new ConfigFile();
        string language = settings.Load(path) == Error.Ok
            ? settings.GetValue("interface", "language", "ru").AsString() : "ru";
        TranslationServer.SetLocale(language is "ru" or "en" ? language : "ru");
    }
}
