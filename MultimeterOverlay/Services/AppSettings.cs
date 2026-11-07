
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
    public bool OverlaySizeLocked { get; set; }


    public double? MainLeft { get; set; }
    public double? MainTop { get; set; }
    public double MainWidth { get; set; }
    public double MainHeight { get; set; }
    public bool MainMaximized { get; set; }

    public string Language { get; set; } = "pl";

    public string OverlayBackgroundColor { get; set; } = "#181818";
    public string OverlayTextColor { get; set; } = "#FFFFFF";
    public string OverlayConnectedColor { get; set; } = "#39C875";
    public string OverlayWaitingColor { get; set; } = "#F4D35E";
    public string OverlayDisconnectedColor { get; set; } = "#777777";
    public string OverlayRangeTextColor { get; set; } = "#D0D0D0";
    public string OverlayMinMaxColor { get; set; } = "#2563B5";
    public string OverlayAcColor { get; set; } = "#C49A16";
    public string OverlayDcColor { get; set; } = "#228B50";
    public string OverlayHoldColor { get; set; } = "#D97706";


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
