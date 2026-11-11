using System.Diagnostics;
using System.Windows;

namespace MeterHUD.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Repository_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/Dedko-Serwis/MeterHUD");

    private void License_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://www.gnu.org/licenses/gpl-3.0.html");

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
