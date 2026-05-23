using System;
using System.IO;
using Avalonia.Controls;
using MODUPDATER.Services;
using MODUPDATER.Models;
using MODUPDATER.Config;
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
    private const string VERSION_FILE = "LauncherVersion.txt";

    public MainWindow()
    {
        InitializeComponent();
        _config = _configService.Load();
        CheckServer();
    }

    private string GetLocalVersion()
    {
        if (!File.Exists(VERSION_FILE))
        {
            File.WriteAllText(VERSION_FILE, "0.0.0");
            return "0.0.0";
        }
        return File.ReadAllText(VERSION_FILE).Trim();
    }

    private void SaveLocalVersion(string version)
    {
        File.WriteAllText(VERSION_FILE, version);
    }

    private async void CheckServer()
    {
        ServerStatusText.Text = "[ STATUS: VERIFICANDO... ]";
        StatusText.Text = "Carregando patches...";

        // --- TRATAMENTO INTELIGENTE DA URL (CORREÇÃO) ---
        string rawUrl = _config.ServerIp.Trim();

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            StatusText.Text = "Configuração de IP do servidor vazia.";
            ServerStatusText.Text = "[ STATUS: ERRO ]";
            UpdateButton.Content = "Abrir SPT Launcher";
            return;
        }

        // 1. Garante que possui o protocolo http:// ou https:// para o HttpClient não quebrar
        if (!rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            rawUrl = "http://" + rawUrl;
        }

        // 2. Garante que a URL aponta explicitamente para o arquivo versions.json
        string manifestUrl = rawUrl;
        if (!manifestUrl.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            manifestUrl = manifestUrl.TrimEnd('/') + "/versions.json";
        }

        // 3. Extrai puramente o Host/IP de forma segura para o teste de porta do SPT Server
        string safeHost = "127.0.0.1";
        try
        {
            var uri = new Uri(manifestUrl);
            safeHost = uri.Host; // Extrai puramente o IP (ex: "100.83.95.32")
        }
        catch
        {
            StatusText.Text = "URL do Servidor inválida.";
            ServerStatusText.Text = "[ STATUS: ERRO ]";
            UpdateButton.Content = "Abrir SPT Launcher";
            return;
        }
        // --- FIM DO TRATAMENTO ---

        // Agora usamos a URL higienizada e completa para a Web
        bool webOnline = await _api.PingServer(manifestUrl);

        // E usamos o IP purificado para testar a porta 6969 do SPT
        bool sptOnline = await _api.IsPortOpen(safeHost, 6969);

        ServerStatusText.Text = $"[ WEB SERVER: {(webOnline ? "ONLINE" : "OFFLINE")} | SPT SERVER: {(sptOnline ? "ONLINE" : "OFFLINE")} ]";

        if (!webOnline)
        {
            StatusText.Text = "WEB server offline.";
            UpdateButton.Content = "Abrir SPT Launcher";
            return;
        }

        // Usamos a URL correta aqui também
        _serverManifest = await _api.GetVersionManifest(manifestUrl);

        if (_serverManifest == null || !_serverManifest.Any())
        {
            StatusText.Text = "Falha ao ler versions.json";
            UpdateButton.Content = "Abrir SPT Launcher";
            return;
        }

        string localVersionStr = GetLocalVersion();

        if (!Version.TryParse(localVersionStr, out Version? localVersion))
        {
            localVersion = new Version(0, 0, 0);
        }

        var updatesPendentes = new List<VersionInfo>();

        foreach (var v in _serverManifest)
        {
            if (Version.TryParse(v.Version, out Version? serverVer) && serverVer > localVersion)
            {
                updatesPendentes.Add(v);
            }
        }

        updatesPendentes = updatesPendentes
            .OrderBy(v => Version.Parse(v.Version))
            .ToList();

        if (updatesPendentes.Any())
        {
            var ultimaVersao = updatesPendentes.Last();
            string notasCompiladas = string.Join("\n\n", updatesPendentes.Select(u => $"--- CHANGELOG v{u.Version} ---\n{u.Changelog}"));

            StatusText.Text = $"[ ATUALIZAÇÃO DISPONIVEL ]\n" +
                              $"Nova versão final disponível: v{ultimaVersao.Version}\n" +
                              $"Total de {updatesPendentes.Count} patch(es) pendente(s).\n\n" +
                              $"{notasCompiladas}";

            UpdateButton.Content = "Atualizar";
        }
        else
        {
            var ultimaVersaoServidor = _serverManifest.OrderBy(v => Version.Parse(v.Version)).LastOrDefault();
            string notasUltimaVersao = ultimaVersaoServidor != null
                ? $"\n\n--- HISTÓRICO DE ALTERAÇÕES DA v{ultimaVersaoServidor.Version} ---\n{ultimaVersaoServidor.Changelog}"
                : "";

            StatusText.Text = $"[ ATUALIZADO ]\n" +
                              $"Versão atual instalada: v{localVersionStr}" +
                              $"{notasUltimaVersao}";

            UpdateButton.Content = "Abrir SPT Launcher";
        }
    }

    private async void UpdateButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (UpdateButton.Content?.ToString() == "Abrir SPT Launcher")
        {
            OpenSptLauncher();
            return;
        }

        UpdateButton.IsEnabled = false;

        try
        {
            string localVersionStr = GetLocalVersion();
            if (!Version.TryParse(localVersionStr, out Version? localVersion))
            {
                localVersion = new Version(0, 0, 0);
            }

            if (_serverManifest == null) return;

            var updatesPendentes = new List<VersionInfo>();
            foreach (var v in _serverManifest)
            {
                if (Version.TryParse(v.Version, out Version? serverVer) && serverVer > localVersion)
                {
                    updatesPendentes.Add(v);
                }
            }

            updatesPendentes = updatesPendentes.OrderBy(v => Version.Parse(v.Version)).ToList();

            var progress = new Progress<double>(p =>
            {

            });

            foreach (var update in updatesPendentes)
            {
                StatusText.Text = $"Baixando versão {update.Version}...";
                string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"patch_{update.Version}.zip");

                await _download.DownloadFile(update.DownloadUrl, zipPath, progress);

                StatusText.Text = $"Limpando arquivos obsoletos da v{update.Version}...";
                _delete.ExecuteDeletion(_config.SptPath, update.FilesToDelete);

                StatusText.Text = $"Instalando versão {update.Version}...";
                _extract.Extract(zipPath, _config.SptPath);

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                SaveLocalVersion(update.Version);
            }

            StatusText.Text = "Atualização concluída com sucesso!";
            CheckServer();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"ERRO: {ex.Message}";
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
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
        StatusText.Text = "Configurações gravadas com sucesso no banco de dados local.";
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
            {
                FileName = launcherPath,
                WorkingDirectory = _config.SptPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Erro ao abrir o launcher do SPT: {ex.Message}";
        }
    }
}
