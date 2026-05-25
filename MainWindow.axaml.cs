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
        StatusText.Text = "CONECTANDO...";
        ChangelogText.Text = "Carregando patches do servidor...";

        string rawUrl = _config.ServerIp.Trim();

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            ChangelogText.Text = "Configuração de IP do servidor vazia. Vá em SETTINGS e configure o IP.";
            StatusText.Text = "ERRO DE CONFIGURAÇÃO";
            ServerStatusText.Text = "[ STATUS: ERRO ]";
            UpdateButton.Content = "Abrir SPT Launcher";
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
            ChangelogText.Text = "A URL do Servidor inserida é inválida.";
            StatusText.Text = "URL INVÁLIDA";
            ServerStatusText.Text = "[ STATUS: ERRO ]";
            UpdateButton.Content = "Abrir SPT Launcher";
            return;
        }

        bool webOnline = await _api.PingServer(manifestUrl);
        bool sptOnline = await _api.IsPortOpen(safeHost, 6969);

        ServerStatusText.Text = $"[ WEB SERVER: {(webOnline ? "ONLINE" : "OFFLINE")} | SPT SERVER: {(sptOnline ? "ONLINE" : "OFFLINE")} ]";

        if (!webOnline)
        {
            ChangelogText.Text = "O servidor Web de atualizações está offline. Verifique o host ou tente novamente mais tarde.";
            StatusText.Text = "SERVER OFFLINE";
            UpdateButton.Content = "Abrir SPT Launcher";
            return;
        }

        _serverManifest = await _api.GetVersionManifest(manifestUrl);

        if (_serverManifest == null || !_serverManifest.Any())
        {
            ChangelogText.Text = "Falha crítica ao ler o arquivo remoto versions.json.";
            StatusText.Text = "ERRO MANIFESTO";
            UpdateButton.Content = "Abrir SPT Launcher";
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

            ChangelogText.Text = $"Nova versão final disponível: v{ultimaVersao.Version}\n" +
                                 $"Total de {updatesPendentes.Count} patch(es) pendente(s).\n\n" +
                                 $"{notasCompiladas}";

            StatusText.Text = "ATUALIZAÇÃO DISPONÍVEL";
            UpdateButton.Content = "Atualizar";
        }
        else
        {
            var ultimaVersaoServidor = _serverManifest.OrderBy(v => Version.Parse(v.Version)).LastOrDefault();
            string notasUltimaVersao = ultimaVersaoServidor != null
                ? $"\n\n--- HISTÓRICO DE ALTERAÇÕES DA v{ultimaVersaoServidor.Version} ---\n{ultimaVersaoServidor.Changelog}"
                : "";

            ChangelogText.Text = $"Você está rodando a versão estável mais recente!\n" +
                                 $"Versão instalada: v{localVersionStr}" +
                                 $"{notasUltimaVersao}";

            StatusText.Text = "SISTEMA ATUALIZADO";
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

            foreach (var update in updatesPendentes)
            {
                var progress = new Progress<double>(p =>
                {
                    StatusText.Text = $"BAIXANDO v{update.Version} ({p:F0}%)";
                    DownloadProgressBar.Value = p;
                });

                string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"patch_{update.Version}.zip");

                await _download.DownloadFile(update.DownloadUrl, zipPath, progress);

                DownloadProgressBar.Value = 100;
                StatusText.Text = "REMOVENDO ARQUIVOS OBSOLETOS...";

                await Task.Run(() =>
                {
                    _delete.ExecuteDeletion(_config.SptPath, update.FilesToDelete);
                });

                StatusText.Text = $"EXTRAINDO v{update.Version}... (AGUARDE)";
                await Task.Run(() =>
                {
                    _extract.Extract(zipPath, _config.SptPath);
                });

                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                SaveLocalVersion(update.Version);
                DownloadProgressBar.Value = 0;
            }

            StatusText.Text = "CONCLUÍDO COM SUCESSO!";
            CheckServer();
        }
        catch (Exception ex)
        {
            ChangelogText.Text = $"ERRO CRÍTICO NA ATUALIZAÇÃO:\n{ex.Message}\n\nVerifique se o jogo não está aberto ou se há permissão de escrita na pasta.";
            StatusText.Text = "FALHA NO PROCESSO";
            DownloadProgressBar.Value = 0;
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
        ChangelogText.Text = "Configurações gravadas com sucesso no banco de dados local. Clique em VOLTAR para aplicar.";
        StatusText.Text = "CONFIG SALVA";
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
            ChangelogText.Text = $"Erro ao abrir o launcher do SPT: {ex.Message}\nVerifique se o caminho da pasta raiz do SPT está correto nas configurações.";
            StatusText.Text = "ERRO AO ABRIR JOGO";
        }
    }
}
