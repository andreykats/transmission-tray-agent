using TransmissionTrayAgent;

namespace TransmissionTrayAgent;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Configure application
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Set up unhandled exception handlers
        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            // Check if settings exist
            if (!SettingsManager.SettingsExist())
            {
                // First run - show settings dialog
                using var settingsForm = new SettingsForm();
                var result = settingsForm.ShowDialog();

                if (result != DialogResult.OK || !settingsForm.SettingsSaved)
                {
                    // User cancelled or didn't save settings
                    MessageBox.Show(
                        "Settings are required to run Transmission Tray Agent.\nThe application will now exit.",
                        "Configuration Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }
            }

            // Load settings
            var settings = SettingsManager.LoadSettings();

            // Create and run tray application
            using var trayContext = new TrayApplicationContext(settings);
            Application.Run(trayContext);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fatal error: {ex.Message}\n\n{ex.StackTrace}",
                "Transmission Tray Agent - Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        HandleException(e.Exception);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            HandleException(ex);
        }
    }

    private static void HandleException(Exception ex)
    {
        try
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.Message}",
                "Transmission Tray Agent - Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {ex}");
        }
        catch
        {
            // If we can't even show the error dialog, just exit
            Environment.Exit(1);
        }
    }
}
