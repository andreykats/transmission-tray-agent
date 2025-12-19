namespace TransmissionTrayAgent.Models;

public enum GameDetectionBehavior
{
    NotifyOnly = 0,    // Show notification only, user must manually pause
    AutoPause = 1      // Automatically pause torrents + show notification
}
