using System.Windows;
using System.Windows.Controls;
using MeterHUD.Services;

namespace MeterHUD.Views;

public partial class UpdateWindow : Window
{
    private readonly UpdateService _service;
    private readonly AppSettings _settings;
    private UpdateRelease? _release;
    private bool _busy;
    public UpdateWindow(UpdateService service, AppSettings settings)
    {
        InitializeComponent();
        _service = service;
        _settings = settings;
        CurrentVersionText.Text = UpdateService.CurrentVersion.ToString(3);
        IntervalCombo.SelectedIndex = settings.UpdateCheckDays switch { -1 => 0, 30 => 2, 0 => 3, _ => 1 };
        RefreshLastCheck();
    }

    private void RefreshLastCheck() =>
        LastCheckText.Text = "Ostatnie sprawdzenie: " +
            (_settings.LastUpdateCheckUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "nigdy");

    private void IntervalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_settings is null || IntervalCombo.SelectedItem is not ComboBoxItem item) return;
        if (!int.TryParse(item.Tag?.ToString(), out var days)) return;
        _settings.UpdateCheckDays = days;
        try { _settings.Save(); }
        catch (Exception ex) { StatusText.Text = "Błąd zapisu ustawień: " + ex.Message; }
    }

    public async Task CheckAsync()
    {
        if (_busy) return;
        _busy = true;
        CheckButton.IsEnabled = false;
        InstallButton.IsEnabled = false;
        try
        {
            StatusText.Text = "Sprawdzam GitHub Releases...";
            _release = await _service.CheckAsync();
            _settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
            _settings.Save();
            RefreshLastCheck();
            LatestVersionText.Text = _release?.Version.ToString(3) ?? "—";
            NotesText.Text = _release?.Notes ?? "";
            StatusText.Text = _release is null ? "Brak publicznego wydania (repozytorium może być prywatne)." :
                _release.Version <= UpdateService.CurrentVersion ? "Masz aktualną wersję." :
                _release.InstallerUrl is null || _release.Sha256 is null
                    ? "Nowa wersja dostępna, ale brak zweryfikowanego instalatora."
                    : "Dostępna jest nowsza wersja.";
            InstallButton.IsEnabled = _release is { InstallerUrl: not null, Sha256: not null } &&
                                      _release.Version > UpdateService.CurrentVersion;
        }
        catch (Exception ex) { StatusText.Text = "Błąd sprawdzania: " + ex.Message; }
        finally { _busy = false; CheckButton.IsEnabled = true; }
    }

    private async void Check_Click(object sender, RoutedEventArgs e) => await CheckAsync();

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _release is null) return;
        if (MessageBox.Show(this, "Pobrać i uruchomić instalator? Zapisz wcześniej swoją pracę.",
                "MeterHUD", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _busy = true;
        CheckButton.IsEnabled = false;
        InstallButton.IsEnabled = false;
        DownloadProgress.Visibility = Visibility.Visible;
        try
        {
            StatusText.Text = "Pobieranie instalatora...";
            var progress = new Progress<int>(value => DownloadProgress.Value = value);
            var path = await _service.DownloadVerifiedAsync(_release, progress);
            StatusText.Text = "Pobrano i zweryfikowano SHA-256. Uruchamiam instalator...";
            UpdateService.StartInstaller(path);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Błąd aktualizacji: " + ex.Message;
            InstallButton.IsEnabled = true;
        }
        finally { _busy = false; CheckButton.IsEnabled = true; }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
