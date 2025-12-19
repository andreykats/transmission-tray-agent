namespace TransmissionTrayAgent.Models;

public class GameDetectionResult
{
    public GameStateChange ChangeType { get; set; }
    public string? GameName { get; set; }
}

public enum GameStateChange
{
    NoChange,
    GameStarted,
    GameStopped
}
