using Microsoft.Win32;
using System.Text.Json;
using TransmissionTrayAgent.Models;

namespace TransmissionTrayAgent;

public static class SettingsManager
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TransmissionTrayAgent"
    );

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryValueName = "TransmissionTrayAgent";

    /// <summary>
    /// Loads settings from %APPDATA%\TransmissionTrayAgent\settings.json
    /// Returns default settings if file doesn't exist
    /// </summary>
    public static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }

        return new AppSettings();
    }

    /// <summary>
    /// Saves settings to %APPDATA%\TransmissionTrayAgent\settings.json
    /// Creates directory if it doesn't exist
    /// </summary>
    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save settings: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if settings file exists
    /// </summary>
    public static bool SettingsExist()
    {
        return File.Exists(SettingsFilePath);
    }

    /// <summary>
    /// Adds or removes the application from Windows startup
    /// </summary>
    public static void SetWindowsStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                throw new Exception("Could not open registry key");
            }

            if (enable)
            {
                // Add to startup - use the current executable path
                string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                key.SetValue(RegistryValueName, $"\"{exePath}\"");
            }
            else
            {
                // Remove from startup
                key.DeleteValue(RegistryValueName, false);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update Windows startup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if the application is set to start with Windows
    /// </summary>
    public static bool IsWindowsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            if (key == null) return false;

            var value = key.GetValue(RegistryValueName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }
}
