
using System.IO;
using System.Text.Json;

namespace MeterHUD.Services;

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
    public bool OverlayTopmost { get; set; } = true;


    public double? MainLeft { get; set; }
    public double? MainTop { get; set; }
    public double MainWidth { get; set; }
    public double MainHeight { get; set; }
    public bool MainMaximized { get; set; }

    public int UpdateCheckDays { get; set; } = 7;
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }
    public string Language { get; set; } = "pl";
    public string CsvDirectory { get; set; } = "";

    public string GetCsvDirectory() => string.IsNullOrWhiteSpace(CsvDirectory)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultimeterOverlay", "Logs")
        : CsvDirectory;

    public string OverlayBackgroundColor { get; set; } = AppColors.OverlayBackground;
    public string OverlayTextColor { get; set; } = AppColors.OverlayText;
    public string OverlayConnectedColor { get; set; } = AppColors.OverlayConnected;
    public string OverlayWaitingColor { get; set; } = AppColors.OverlayWaiting;
    public string OverlayDisconnectedColor { get; set; } = AppColors.OverlayDisconnected;
    public string OverlayRangeTextColor { get; set; } = AppColors.OverlayRangeText;
    public string OverlayMinMaxColor { get; set; } = AppColors.OverlayMinMax;
    public string OverlayAcColor { get; set; } = AppColors.OverlayAc;
    public string OverlayDcColor { get; set; } = AppColors.OverlayDc;
    public string OverlayDiodeColor { get; set; } = AppColors.OverlayDiode;
    public string OverlayHoldColor { get; set; } = AppColors.OverlayHold;


    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

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
        {
            Directory.CreateDirectory(directory);
        }

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
        {
            File.Delete(SettingsPath);
        }
    }
}
