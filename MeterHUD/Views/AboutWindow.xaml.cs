using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace MeterHUD.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        AppVersion = "v" + (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
            ?? "1.0.0").Split('+')[0];
        DataContext = this;
    }

    public string AppVersion { get; }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Repository_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/Dedko-Serwis/MeterHUD");

    private void License_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://www.gnu.org/licenses/gpl-3.0.html");

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
