namespace TransmissionTrayAgent.Models;

public class AppSettings
{
    public string TransmissionHost { get; set; } = "localhost";
    public int TransmissionPort { get; set; } = 9091;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int PollingIntervalSeconds { get; set; } = 5;
    public bool UseHttps { get; set; } = false;
    public bool AutoStartWithWindows { get; set; } = false;
    public bool DisableNotifications { get; set; } = false;

    // Game Detection Settings
    public bool EnableGameDetection { get; set; } = false;
    public List<string> MonitoredGameProcesses { get; set; } = new List<string>();
    public GameDetectionBehavior GameDetectionBehavior { get; set; } = GameDetectionBehavior.NotifyOnly;
}
