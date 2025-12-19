using System.Diagnostics;
using TransmissionTrayAgent.Models;

namespace TransmissionTrayAgent;

public class GameDetectionService
{
    private readonly List<string> _monitoredProcesses;
    private string? _currentlyRunningGame = null;

    public GameDetectionService(List<string> monitoredProcesses)
    {
        _monitoredProcesses = monitoredProcesses ?? new List<string>();
    }

    public string? CurrentGame => _currentlyRunningGame;

    /// <summary>
    /// Checks if any of the monitored game processes are currently running
    /// </summary>
    /// <returns>The process name of the first detected game, or null if none found</returns>
    public string? CheckForRunningGames()
    {
        if (_monitoredProcesses == null || _monitoredProcesses.Count == 0)
            return null;

        try
        {
            var runningProcesses = Process.GetProcesses();

            foreach (var monitoredProcess in _monitoredProcesses)
            {
                // Remove .exe extension for comparison
                var processNameWithoutExt = monitoredProcess.Replace(".exe", "",
                    StringComparison.OrdinalIgnoreCase);

                foreach (var process in runningProcesses)
                {
                    if (process.ProcessName.Equals(processNameWithoutExt,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return monitoredProcess; // Return the configured name
                    }
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Access denied to process list: {ex.Message}");
            return null;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Win32 error accessing processes: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error in process detection: {ex.Message}");
            return null;
        }

        return null;
    }

    /// <summary>
    /// Polls the current game state and detects changes (game started/stopped)
    /// </summary>
    /// <returns>Result indicating if state changed and which game was affected</returns>
    public GameDetectionResult PollGameState()
    {
        var currentGame = CheckForRunningGames();

        // No change - same state as before
        if (currentGame == _currentlyRunningGame)
            return new GameDetectionResult { ChangeType = GameStateChange.NoChange };

        // Game started
        if (currentGame != null && _currentlyRunningGame == null)
        {
            _currentlyRunningGame = currentGame;
            return new GameDetectionResult
            {
                ChangeType = GameStateChange.GameStarted,
                GameName = currentGame
            };
        }

        // Game stopped
        if (currentGame == null && _currentlyRunningGame != null)
        {
            string stoppedGame = _currentlyRunningGame;
            _currentlyRunningGame = null;
            return new GameDetectionResult
            {
                ChangeType = GameStateChange.GameStopped,
                GameName = stoppedGame
            };
        }

        // Game switched (one stopped, another started)
        // For simplicity, treat as no change - could be enhanced in future
        _currentlyRunningGame = currentGame;
        return new GameDetectionResult { ChangeType = GameStateChange.NoChange };
    }
}
