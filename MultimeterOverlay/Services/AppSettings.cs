
using System.IO;
using System.Text.Json;

namespace MultimeterOverlay.Services;

public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "MultimeterOverlay",
        "settings.json");

    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
    public double OverlayWidth { get; set; }
    public double OverlayHeight { get; set; }
    public bool ShowOverlay { get; set; } = true;


    public double? MainLeft { get; set; }
    public double? MainTop { get; set; }
    public double MainWidth { get; set; }
    public double MainHeight { get; set; }
    public bool MainMaximized { get; set; }

    public string Language { get; set; } = "pl";

    public string OverlayBackgroundColor { get; set; } = "#181818";
    public string OverlayTextColor { get; set; } = "#FFFFFF";


    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            string json = File.ReadAllText(SettingsPath);

            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }
        catch (Exception ex) when (
            ex is IOException
            or UnauthorizedAccessException
            or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        string? directory = Path.GetDirectoryName(SettingsPath);

        if (directory is not null)
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(SettingsPath, json);
    }

    public static void Reset()
    {
        if (File.Exists(SettingsPath))
            File.Delete(SettingsPath);
    }
}
