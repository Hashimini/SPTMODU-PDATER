using System;
using System.IO;
using Avalonia.Controls;
using MODUPDATER.Services;
using MODUPDATER.Models;
using MODUPDATER.Config;
using MODUPDATER.Lang;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MODUPDATER;

public partial class MainWindow : Window
{
    private readonly ApiService _api = new();
    private readonly DownloadService _download = new();
    private readonly ExtractService _extract = new();
    private readonly ConfigService _configService = new();
    private readonly DeleteService _delete = new();

    private AppConfig _config = new();
    private List<VersionInfo>? _serverManifest;

    //Initialize the basic logic
    public MainWindow()
    {
        InitializeComponent();
        LangService.Load();
        _config = _configService.Load();
        CheckServer();
    }

    private string GetLocalVersion()
    {
        if (string.IsNullOrWhiteSpace(_config.InstalledVersion))
        {
            _config.InstalledVersion = "0.0.0";
            _configService.Save(_config);
        }

        return _config.InstalledVersion;
    }

    private void SaveLocalVersion(string version)
    {
        _config.InstalledVersion = version;
        _configService.Save(_config);
    }

    private async void CheckServer()
    {
        ServerStatusText.Text = LangService.Get("Checking");
        StatusText.Text = LangService.Get("Connecting");
        ChangelogText.Text = LangService.Get("Loading");

        string rawUrl = _config.ServerIp.Trim();

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            ChangelogText.Text = LangService.Get("EmptyServerIP");
            StatusText.Text = LangService.Get("Error");
            ServerStatusText.Text = LangService.Get("StatusError");
            UpdateButton.Content = LangService.Get("OpenLauncher");
            return;
        }

        if (!rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        { rawUrl = "http://" + rawUrl; }

        string manifestUrl = rawUrl;
        if (!manifestUrl.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        { manifestUrl = manifestUrl.TrimEnd('/') + "/versions.json"; }

        string safeHost = "127.0.0.1";
        try
        {
            var uri = new Uri(manifestUrl);
            safeHost = uri.Host;
        }
        catch
        {
            ChangelogText.Text = LangService.Get("InvalidServerURL");
            StatusText.Text = LangService.Get("InvalidURL");
            ServerStatusText.Text = LangService.Get("StatusError");
            UpdateButton.Content = LangService.Get("OpenLauncher");
            return;
        }

        bool webOnline = await _api.PingServer(manifestUrl);
        bool sptOnline = await _api.IsPortOpen(safeHost, 6969);

        ServerStatusText.Text = $"[ WEB SERVER: {(webOnline ? LangService.Get("Online") : LangService.Get("Offline"))} | SPT SERVER: {(sptOnline ? LangService.Get("Online") : LangService.Get("Offline"))} ]";

        if (!webOnline)
        {
            ChangelogText.Text = LangService.Get("UpdateServerOffline");
            StatusText.Text = LangService.Get("ServerOffline");
            UpdateButton.Content = LangService.Get("OpenLauncher");
            return;
        }

        _serverManifest = await _api.GetVersionManifest(manifestUrl);

        if (_serverManifest == null || !_serverManifest.Any())
        {
            ChangelogText.Text = LangService.Get("ManifestReadFailed");
            StatusText.Text = LangService.Get("Error");
            UpdateButton.Content = LangService.Get("OpenLauncher");
            return;
        }

        string localVersionStr = GetLocalVersion();

        if (!Version.TryParse(localVersionStr, out Version? localVersion))
        { localVersion = new Version(0, 0, 0); }

        var updatesPendentes = new List<VersionInfo>();

        foreach (var v in _serverManifest)
        {
            if (Version.TryParse(v.Version, out Version? serverVer) && serverVer > localVersion)
            { updatesPendentes.Add(v); }
        }

        updatesPendentes = updatesPendentes.OrderBy(v => Version.Parse(v.Version)).ToList();

        if (updatesPendentes.Any())
        {
            var ultimaVersao = updatesPendentes.Last();
            string notasCompiladas = string.Join("\n\n", updatesPendentes.Select(u => $"--- CHANGELOG v{u.Version} ---\n{u.Changelog}"));

            ChangelogText.Text = LangService.Get("NewVersionAvailable") + $"v{ultimaVersao.Version}\n" +
                                 $"{updatesPendentes.Count}" + LangService.Get("PendingPatches") + $"\n\n" +
                                 $"{notasCompiladas}";

            StatusText.Text = LangService.Get("UpdateAvaliable");
            UpdateButton.Content = LangService.Get("Update");
        } else
        {
            var ultimaVersaoServidor = _serverManifest.OrderBy(v => Version.Parse(v.Version)).LastOrDefault();
            string notasUltimaVersao = ultimaVersaoServidor != null
                ? $"\n\n" + LangService.Get("VersionHistory") + $"v{ultimaVersaoServidor.Version} \n{ultimaVersaoServidor.Changelog}"
                : "";

            ChangelogText.Text = LangService.Get("LatestVersionInstalled") + $"\n" +
                                 LangService.Get("InstalledVersion") + $"v{localVersionStr}" +
                                 $"{notasUltimaVersao}";

            StatusText.Text = LangService.Get("Updated");
            UpdateButton.Content = LangService.Get("OpenLauncher");
        }
    }

    private async void UpdateButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (UpdateButton.Content?.ToString() == LangService.Get("OpenLauncher"))
        {
            OpenSptLauncher();
            return;
        }

        UpdateButton.IsEnabled = false;

        try
        {
            string localVersionStr = GetLocalVersion();
            if (!Version.TryParse(localVersionStr, out Version? localVersion))
            { localVersion = new Version(0, 0, 0); }

            if (_serverManifest == null) return;

            var updatesPendentes = new List<VersionInfo>();
            foreach (var v in _serverManifest)
            {
                if (Version.TryParse(v.Version, out Version? serverVer) && serverVer > localVersion)
                { updatesPendentes.Add(v); }
            }

            updatesPendentes = updatesPendentes.OrderBy(v => Version.Parse(v.Version)).ToList();

            foreach (var update in updatesPendentes)
            {
                var progress = new Progress<double>(p =>
                {
                    StatusText.Text = LangService.Get("Downloading") + $"v{update.Version} ({p:F0}%)";
                    DownloadProgressBar.Value = p;
                });

                string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"patch_{update.Version}.zip");

                await _download.DownloadFile(update.DownloadUrl, zipPath, progress);

                DownloadProgressBar.Value = 100;
                StatusText.Text = LangService.Get("RemovingFiles");

                await Task.Run(() =>
                { _delete.ExecuteDeletion(_config.SptPath, update.FilesToDelete); });

                StatusText.Text = LangService.Get("Extracting") + $"v{update.Version}...";

                await Task.Run(() =>
                { _extract.Extract(zipPath, _config.SptPath); });

                if (File.Exists(zipPath))
                { File.Delete(zipPath); }

                SaveLocalVersion(update.Version);
                DownloadProgressBar.Value = 0;
            }

            StatusText.Text = LangService.Get("CompletedSuccessfully");
            CheckServer();
        }
        catch (Exception ex)
        {
            ChangelogText.Text = LangService.Get("CriticalUpdateError") + $"\n{ex.Message}\n\n" + LangService.Get("CheckGame");
            StatusText.Text = LangService.Get("UpdateFailed");
            DownloadProgressBar.Value = 0;
        }
        finally
        { UpdateButton.IsEnabled = true; }
    }

    private void SettingsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainMenu.IsVisible = false;
        SettingsMenu.IsVisible = true;

        ServerIpBox.Text = _config.ServerIp;
        SptPathBox.Text = _config.SptPath;
    }

    private void SaveSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _config.ServerIp = ServerIpBox.Text ?? "";
        _config.SptPath = SptPathBox.Text ?? "";

        _configService.Save(_config);
        ChangelogText.Text = LangService.Get("SettingsSaved");
        StatusText.Text = LangService.Get("ConfigSaved");
    }

    private void BackButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SettingsMenu.IsVisible = false;
        MainMenu.IsVisible = true;
        CheckServer();
    }

    private void OpenSptLauncher()
    {
        try
        {
            string launcherPath = Path.Combine(_config.SptPath, "SPT.Launcher.exe");

            Process.Start(new ProcessStartInfo
            { FileName = launcherPath, WorkingDirectory = _config.SptPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ChangelogText.Text = LangService.Get("LauncherOpenError") + $"{ex.Message}\n" + LangService.Get("CheckSptPath");
            StatusText.Text = LangService.Get("GameLaunchError");
        }
    }
}
