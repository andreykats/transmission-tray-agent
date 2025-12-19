using System.Reflection;
using TransmissionTrayAgent.Models;

namespace TransmissionTrayAgent;

public enum TrayIconState
{
    Active,
    Paused,
    Disconnected
}

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon _trayIcon;
    private ContextMenuStrip _contextMenu;
    private System.Threading.Timer? _pollingTimer;
    private readonly SynchronizationContext _syncContext;
    private readonly AppSettings _settings;
    private TransmissionClient _transmissionClient;
    private TrayIconState _currentState = TrayIconState.Disconnected;
    private bool _isToggling = false;

    // Game detection state
    private GameDetectionService? _gameDetectionService;
    private bool _wasAutoPausedByGame = false;
    private string? _gameNameThatTriggeredPause = null;

    // Icons for different states
    private Icon? _activeIcon;
    private Icon? _pausedIcon;
    private Icon? _disconnectedIcon;

    public TrayApplicationContext(AppSettings settings)
    {
        _settings = settings;
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _transmissionClient = new TransmissionClient(settings);

        // Initialize game detection if enabled
        if (_settings.EnableGameDetection && _settings.MonitoredGameProcesses.Count > 0)
        {
            _gameDetectionService = new GameDetectionService(_settings.MonitoredGameProcesses);
        }

        InitializeComponent();
        LoadIcons();
        StartPolling();
    }

    private void InitializeComponent()
    {
        // Create context menu
        _contextMenu = new ContextMenuStrip();

        // Add Settings menu item
        var settingsMenuItem = new ToolStripMenuItem("Settings");
        settingsMenuItem.Click += SettingsMenuItem_Click;
        _contextMenu.Items.Add(settingsMenuItem);

        // Add separator
        _contextMenu.Items.Add(new ToolStripSeparator());

        // Add Exit menu item
        var exitMenuItem = new ToolStripMenuItem("Exit");
        exitMenuItem.Click += ExitMenuItem_Click;
        _contextMenu.Items.Add(exitMenuItem);

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Transmission Tray Agent - Initializing...",
            ContextMenuStrip = _contextMenu
        };

        // Set initial icon to disconnected
        UpdateIcon(TrayIconState.Disconnected);

        // Wire up events
        _trayIcon.MouseClick += TrayIcon_Click;
    }

    private void SettingsMenuItem_Click(object? sender, EventArgs e)
    {
        ShowSettingsDialog();
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        ExitApplication();
    }

    private void LoadIcons()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Load active icon
            _activeIcon = LoadIconFromResource(assembly, "TransmissionTrayAgent.Resources.active.ico");

            // Load paused icon
            _pausedIcon = LoadIconFromResource(assembly, "TransmissionTrayAgent.Resources.paused.ico");

            // Load disconnected icon
            _disconnectedIcon = LoadIconFromResource(assembly, "TransmissionTrayAgent.Resources.disconnected.ico");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading icons: {ex.Message}");

            // Fallback: Use system icon if custom icons fail to load
            _activeIcon = _pausedIcon = _disconnectedIcon = SystemIcons.Application;
        }
    }

    private Icon? LoadIconFromResource(Assembly assembly, string resourceName)
    {
        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                return new Icon(stream);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load icon {resourceName}: {ex.Message}");
        }

        return null;
    }

    public void UpdateIcon(TrayIconState newState)
    {
        _currentState = newState;

        Icon? iconToUse = newState switch
        {
            TrayIconState.Active => _activeIcon,
            TrayIconState.Paused => _pausedIcon,
            TrayIconState.Disconnected => _disconnectedIcon,
            _ => _disconnectedIcon
        };

        if (iconToUse != null)
        {
            _trayIcon.Icon = iconToUse;
        }

        // Update tooltip
        string stateText = newState switch
        {
            TrayIconState.Active => "Active (Downloading)",
            TrayIconState.Paused => "Paused",
            TrayIconState.Disconnected => "Disconnected",
            _ => "Unknown"
        };

        _trayIcon.Text = $"Transmission Tray Agent - {stateText}";
    }

    private void StartPolling()
    {
        int intervalMs = _settings.PollingIntervalSeconds * 1000;

        _pollingTimer = new System.Threading.Timer(
            async _ => await PollTransmissionState(),
            null,
            TimeSpan.Zero, // Start immediately
            TimeSpan.FromMilliseconds(intervalMs)
        );
    }

    private async Task PollTransmissionState(bool skipToggleCheck = false)
    {
        if (!skipToggleCheck && _isToggling)
        {
            // Skip polling if we're in the middle of toggling
            return;
        }

        try
        {
            var stats = await _transmissionClient.GetSessionStatsAsync();

            // Check if any torrent is active using session stats (more efficient)
            bool anyTorrentActive = stats.ActiveTorrentCount > 0;

            TrayIconState newState = anyTorrentActive
                ? TrayIconState.Active
                : TrayIconState.Paused;

            // Update icon on UI thread
            _syncContext.Post(_ => UpdateIcon(newState), null);

            // Check for game detection
            if (_gameDetectionService != null && _settings.EnableGameDetection)
            {
                _syncContext.Post(_ => CheckGameDetection(), null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Polling error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            // Update to disconnected state on UI thread
            _syncContext.Post(_ => UpdateIcon(TrayIconState.Disconnected), null);
        }
    }

    private async void TrayIcon_Click(object? sender, EventArgs e)
    {
        // Only handle left-click for toggle (right-click shows context menu)
        if (e is MouseEventArgs mouseEvent && mouseEvent.Button == MouseButtons.Left)
        {
            await ToggleTransmissionState();
        }
    }

    private async Task ToggleTransmissionState()
    {
        if (_isToggling)
        {
            return; // Already toggling
        }

        if (_currentState == TrayIconState.Disconnected)
        {
            if (!_settings.DisableNotifications)
            {
                _trayIcon.ShowBalloonTip(
                    3000,
                    "Transmission Tray Agent",
                    "Cannot toggle: Not connected to Transmission server",
                    ToolTipIcon.Error
                );
            }
            return;
        }

        _isToggling = true;

        try
        {
            bool wasPausing = false;

            if (_currentState == TrayIconState.Active)
            {
                // Stop all torrents
                await _transmissionClient.StopAllTorrentsAsync();
                wasPausing = true;
            }
            else // Paused
            {
                // Start all torrents
                await _transmissionClient.StartAllTorrentsAsync();
            }

            // Poll server immediately to get actual state after toggle
            await PollTransmissionState(skipToggleCheck: true);

            // If user manually toggles while game running, reset auto-pause tracking
            if (wasPausing && _wasAutoPausedByGame)
            {
                // User manually paused after we auto-paused
                // Reset our flag so we don't auto-resume when game closes
                _wasAutoPausedByGame = false;
                _gameNameThatTriggeredPause = null;
            }
            else if (!wasPausing && _gameDetectionService?.CurrentGame != null)
            {
                // User manually resumed while game is running
                // Respect their choice - don't re-pause
                _wasAutoPausedByGame = false;
            }

            // Show balloon tip based on what we tried to do
            if (!_settings.DisableNotifications)
            {
                _trayIcon.ShowBalloonTip(
                    2000,
                    "Transmission Tray Agent",
                    wasPausing ? "All torrents paused" : "All torrents resumed",
                    ToolTipIcon.Info
                );
            }
        }
        catch (Exception ex)
        {
            if (!_settings.DisableNotifications)
            {
                _trayIcon.ShowBalloonTip(
                    3000,
                    "Transmission Tray Agent",
                    $"Error: {ex.Message}",
                    ToolTipIcon.Error
                );
            }

            UpdateIcon(TrayIconState.Disconnected);
        }
        finally
        {
            _isToggling = false;
        }
    }

    public void ShowSettingsDialog()
    {
        // Reload current settings
        var currentSettings = SettingsManager.LoadSettings();

        using var settingsForm = new SettingsForm(currentSettings);
        var result = settingsForm.ShowDialog();

        if (result == DialogResult.OK && settingsForm.SettingsSaved)
        {
            // Settings changed, restart application to apply
            if (!currentSettings.DisableNotifications)
            {
                _trayIcon.ShowBalloonTip(
                    3000,
                    "Transmission Tray Agent",
                    "Settings saved. Please restart the application for changes to take effect.",
                    ToolTipIcon.Info
                );
            }
        }
    }

    // Game Detection Methods

    private async void CheckGameDetection()
    {
        if (_gameDetectionService == null) return;

        try
        {
            var result = _gameDetectionService.PollGameState();

            if (result.ChangeType == Models.GameStateChange.GameStarted)
            {
                await HandleGameStarted(result.GameName);
            }
            else if (result.ChangeType == Models.GameStateChange.GameStopped)
            {
                await HandleGameStopped(result.GameName);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Game detection error: {ex.Message}");
            // Silent failure - don't disrupt main app functionality
        }
    }

    private async Task HandleGameStarted(string? gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return;

        // Don't trigger if already paused by game or if disconnected
        if (_wasAutoPausedByGame || _currentState == TrayIconState.Disconnected)
            return;

        if (_settings.GameDetectionBehavior == Models.GameDetectionBehavior.AutoPause)
        {
            // AUTO-PAUSE MODE
            try
            {
                await _transmissionClient.StopAllTorrentsAsync();
                _wasAutoPausedByGame = true;
                _gameNameThatTriggeredPause = gameName;

                // Update state immediately
                await PollTransmissionState(skipToggleCheck: true);

                // Show notification
                if (!_settings.DisableNotifications)
                {
                    _trayIcon.ShowBalloonTip(
                        3000,
                        "Game Detected",
                        $"{gameName} started. Torrents automatically paused.",
                        ToolTipIcon.Info
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-pause failed: {ex.Message}");

                if (!_settings.DisableNotifications)
                {
                    _trayIcon.ShowBalloonTip(
                        3000,
                        "Auto-Pause Failed",
                        $"Could not pause torrents: {ex.Message}",
                        ToolTipIcon.Error
                    );
                }
            }
        }
        else
        {
            // NOTIFY-ONLY MODE
            if (!_settings.DisableNotifications)
            {
                _trayIcon.ShowBalloonTip(
                    5000,
                    "Game Detected",
                    $"{gameName} is running. You may want to pause torrents to reduce lag.",
                    ToolTipIcon.Warning
                );
            }
        }
    }

    private async Task HandleGameStopped(string? gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return;

        // Only auto-resume if we were the ones who auto-paused
        if (_settings.GameDetectionBehavior == Models.GameDetectionBehavior.AutoPause && _wasAutoPausedByGame)
        {
            try
            {
                await _transmissionClient.StartAllTorrentsAsync();
                _wasAutoPausedByGame = false;

                string displayName = _gameNameThatTriggeredPause ?? gameName;
                _gameNameThatTriggeredPause = null;

                // Update state immediately
                await PollTransmissionState(skipToggleCheck: true);

                // Show notification
                if (!_settings.DisableNotifications)
                {
                    _trayIcon.ShowBalloonTip(
                        3000,
                        "Game Closed",
                        $"{displayName} has closed. Torrents automatically resumed.",
                        ToolTipIcon.Info
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-resume failed: {ex.Message}");
                _wasAutoPausedByGame = false; // Reset flag even on failure

                if (!_settings.DisableNotifications)
                {
                    _trayIcon.ShowBalloonTip(
                        3000,
                        "Auto-Resume Failed",
                        $"Could not resume torrents: {ex.Message}",
                        ToolTipIcon.Error
                    );
                }
            }
        }
        else if (_settings.GameDetectionBehavior == Models.GameDetectionBehavior.NotifyOnly)
        {
            // Notify-only mode: just inform user game closed
            if (!_settings.DisableNotifications)
            {
                _trayIcon.ShowBalloonTip(
                    3000,
                    "Game Closed",
                    $"{gameName} has closed.",
                    ToolTipIcon.Info
                );
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollingTimer?.Dispose();
            _transmissionClient?.Dispose();

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            _contextMenu?.Dispose();
            _activeIcon?.Dispose();
            _pausedIcon?.Dispose();
            _disconnectedIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    public void ExitApplication()
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }
}
