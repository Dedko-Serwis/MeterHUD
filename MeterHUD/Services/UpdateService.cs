using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace MeterHUD.Services;

public sealed record UpdateRelease(Version Version, string Notes, string? InstallerUrl, string? Sha256);

public sealed class UpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/Dedko-Serwis/MeterHUD/releases/latest";
    private static readonly HttpClient Client = CreateClient();
    public static Version CurrentVersion
    {
        get
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
            return new Version(version.Major, version.Minor, Math.Max(0, version.Build));
        }
    }

    public static bool TryParseReleaseVersion(string? tag, out Version? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(tag)) return false;
        var normalized = tag.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase)) normalized = normalized[1..];
        if (!Version.TryParse(normalized, out var parsed) || parsed.Build < 0) return false;
        version = new Version(parsed.Major, parsed.Minor, parsed.Build);
        return true;
    }

    public static bool IsNewer(Version installed, Version latest) => latest > installed;

    public static bool ShouldCheck(int days, DateTimeOffset? last, DateTimeOffset now) =>
        days != 0 && (days < 0 || last is null || now - last.Value >= TimeSpan.FromDays(days));

    public static bool HasVerifiedInstaller(UpdateRelease? release) =>
        release is { InstallerUrl: not null, Sha256: not null } &&
        Uri.TryCreate(release.InstallerUrl, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
        release.Sha256.Length == 64 && release.Sha256.All(Uri.IsHexDigit);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MeterHUD", "0.2"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    public async Task<UpdateRelease?> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var response = await Client.GetAsync(ApiUrl, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null; // Brak publicznego wydania albo repozytorium nadal prywatne.
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
        if (!TryParseReleaseVersion(tag, out var version))
            throw new FormatException("Nieprawidłowa wersja GitHub Release.");
        var notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";
        string? url = null, hash = null;
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (name is null || !name.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase)) continue;
                url = asset.GetProperty("browser_download_url").GetString();
                var digest = asset.TryGetProperty("digest", out var d) ? d.GetString() : null;
                if (digest is not null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                    hash = digest["sha256:".Length..];
                break;
            }
        }
        return new UpdateRelease(version!, notes, url, hash);
    }

    public async Task<string> DownloadVerifiedAsync(UpdateRelease release, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (release.InstallerUrl is null || release.Sha256 is null || release.Sha256.Length != 64 ||
            !release.Sha256.All(Uri.IsHexDigit) ||
            !Uri.TryCreate(release.InstallerUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Brak zaufanego instalatora z sumą SHA-256.");

        var uri = new Uri(release.InstallerUrl!);\n        var directory = Path.Combine(Path.GetTempPath(), "MeterHUD-Updates");
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, Guid.NewGuid().ToString("N") + "-Setup.exe");
        try
        {
            using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength;
            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = File.Create(file))
            {
                var buffer = new byte[65536];
                long received = 0;
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                    received += count;
                    if (total is > 0)
                        progress?.Report((int)Math.Min(100, received * 100.0 / total.Value));
                }
            }
            await using var stream = File.OpenRead(file);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
            if (!actual.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Niezgodna suma SHA-256 instalatora.");
            return file;
        }
        catch
        {
            try { File.Delete(file); } catch (IOException) { }
            throw;
        }
    }

    public static void StartInstaller(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
