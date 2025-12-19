using TransmissionTrayAgent.Models;

namespace TransmissionTrayAgent;

public class SettingsForm : Form
{
    private Label labelHost;
    private TextBox textBoxHost;
    private Label labelPort;
    private NumericUpDown numericPort;
    private Label labelUsername;
    private TextBox textBoxUsername;
    private Label labelPassword;
    private TextBox textBoxPassword;
    private Label labelPollingInterval;
    private NumericUpDown numericPollingInterval;
    private CheckBox checkBoxAutoStart;
    private CheckBox checkBoxDisableNotifications;
    private Button buttonSave;
    private Button buttonCancel;
    private Button buttonTestConnection;
    private Button buttonTestNotification;

    // Game Detection UI Controls
    private GroupBox groupBoxGameDetection;
    private CheckBox checkBoxEnableGameDetection;
    private RadioButton radioNotifyOnly;
    private RadioButton radioAutoPause;
    private Label labelGameInstructions;
    private ListBox listBoxGames;
    private TextBox textBoxGameProcess;
    private Button buttonAddGame;
    private Button buttonRemoveGame;
    private CheckBox checkBoxUseHttps;

    private AppSettings _settings;
    private TransmissionClient? _transmissionClient;

    public AppSettings Settings => _settings;
    public bool SettingsSaved { get; private set; }

    public SettingsForm(AppSettings? settings = null)
    {
        _settings = settings ?? new AppSettings();
        SettingsSaved = false;

        InitializeComponent();
        LoadSettingsToForm();
    }

    private void InitializeComponent()
    {
        // Form configuration
        this.Text = "Transmission Tray Agent - Settings";
        this.Width = 450;
        this.Height = 670;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        int labelWidth = 120;
        int controlLeft = labelWidth + 20;
        int controlWidth = 280;
        int rowHeight = 35;
        int currentY = 20;

        // Host
        labelHost = new Label
        {
            Text = "Transmission Host:",
            Left = 10,
            Top = currentY + 3,
            Width = labelWidth
        };
        textBoxHost = new TextBox
        {
            Left = controlLeft,
            Top = currentY,
            Width = controlWidth
        };
        this.Controls.Add(labelHost);
        this.Controls.Add(textBoxHost);
        currentY += rowHeight;

        // Port
        labelPort = new Label
        {
            Text = "Port:",
            Left = 10,
            Top = currentY + 3,
            Width = labelWidth
        };
        numericPort = new NumericUpDown
        {
            Left = controlLeft,
            Top = currentY,
            Width = 100,
            Minimum = 1,
            Maximum = 65535,
            Value = 9091
        };
        this.Controls.Add(labelPort);
        this.Controls.Add(numericPort);
        currentY += rowHeight;

        // Username
        labelUsername = new Label
        {
            Text = "Username:",
            Left = 10,
            Top = currentY + 3,
            Width = labelWidth
        };
        textBoxUsername = new TextBox
        {
            Left = controlLeft,
            Top = currentY,
            Width = controlWidth
        };
        this.Controls.Add(labelUsername);
        this.Controls.Add(textBoxUsername);
        currentY += rowHeight;

        // Password
        labelPassword = new Label
        {
            Text = "Password:",
            Left = 10,
            Top = currentY + 3,
            Width = labelWidth
        };
        textBoxPassword = new TextBox
        {
            Left = controlLeft,
            Top = currentY,
            Width = controlWidth,
            UseSystemPasswordChar = true
        };
        this.Controls.Add(labelPassword);
        this.Controls.Add(textBoxPassword);
        currentY += rowHeight;

        // Polling Interval
        labelPollingInterval = new Label
        {
            Text = "Polling Interval (sec):",
            Left = 10,
            Top = currentY + 3,
            Width = labelWidth
        };
        numericPollingInterval = new NumericUpDown
        {
            Left = controlLeft,
            Top = currentY,
            Width = 100,
            Minimum = 1,
            Maximum = 60,
            Value = 5
        };
        this.Controls.Add(labelPollingInterval);
        this.Controls.Add(numericPollingInterval);
        currentY += rowHeight;

        // Auto-start checkbox
        checkBoxAutoStart = new CheckBox
        {
            Text = "Start with Windows",
            Left = controlLeft,
            Top = currentY,
            Width = controlWidth
        };
        this.Controls.Add(checkBoxAutoStart);
        currentY += rowHeight;

        // Use HTTPS checkbox
        checkBoxUseHttps = new CheckBox
        {
            Text = "Use HTTPS",
            Left = controlLeft,
            Top = currentY,
            Width = controlWidth
        };
        this.Controls.Add(checkBoxUseHttps);
        currentY += rowHeight;

        // Disable notifications checkbox
        checkBoxDisableNotifications = new CheckBox
        {
            Text = "Disable notifications",
            Left = controlLeft,
            Top = currentY,
            Width = controlWidth
        };
        this.Controls.Add(checkBoxDisableNotifications);
        currentY += rowHeight + 10;

        // Game Detection GroupBox
        groupBoxGameDetection = new GroupBox
        {
            Text = "Game Detection",
            Left = 10,
            Top = currentY,
            Width = 420,
            Height = 240
        };
        this.Controls.Add(groupBoxGameDetection);

        int groupBoxY = 20;

        // Enable Game Detection checkbox
        checkBoxEnableGameDetection = new CheckBox
        {
            Text = "Enable game detection",
            Left = 10,
            Top = groupBoxY,
            Width = 200
        };
        checkBoxEnableGameDetection.CheckedChanged += CheckBoxEnableGameDetection_CheckedChanged;
        groupBoxGameDetection.Controls.Add(checkBoxEnableGameDetection);
        groupBoxY += 25;

        // Behavior radio buttons
        radioNotifyOnly = new RadioButton
        {
            Text = "Notify only (show alert)",
            Left = 30,
            Top = groupBoxY,
            Width = 200,
            Checked = true
        };
        groupBoxGameDetection.Controls.Add(radioNotifyOnly);
        groupBoxY += 25;

        radioAutoPause = new RadioButton
        {
            Text = "Auto-pause torrents when game starts",
            Left = 30,
            Top = groupBoxY,
            Width = 250
        };
        groupBoxGameDetection.Controls.Add(radioAutoPause);
        groupBoxY += 30;

        // Instructions label
        labelGameInstructions = new Label
        {
            Text = "Enter process names (e.g., 'overwatch.exe', 'valorant.exe'):",
            Left = 10,
            Top = groupBoxY,
            Width = 400,
            Height = 35
        };
        groupBoxGameDetection.Controls.Add(labelGameInstructions);
        groupBoxY += 40;

        // Process list box
        listBoxGames = new ListBox
        {
            Left = 10,
            Top = groupBoxY,
            Width = 200,
            Height = 80
        };
        groupBoxGameDetection.Controls.Add(listBoxGames);

        // Process input textbox
        textBoxGameProcess = new TextBox
        {
            Left = 220,
            Top = groupBoxY,
            Width = 180
        };
        textBoxGameProcess.KeyPress += TextBoxGameProcess_KeyPress;
        groupBoxGameDetection.Controls.Add(textBoxGameProcess);

        // Add button
        buttonAddGame = new Button
        {
            Text = "Add",
            Left = 220,
            Top = groupBoxY + 30,
            Width = 85
        };
        buttonAddGame.Click += ButtonAddGame_Click;
        groupBoxGameDetection.Controls.Add(buttonAddGame);

        // Remove button
        buttonRemoveGame = new Button
        {
            Text = "Remove",
            Left = 315,
            Top = groupBoxY + 30,
            Width = 85
        };
        buttonRemoveGame.Click += ButtonRemoveGame_Click;
        groupBoxGameDetection.Controls.Add(buttonRemoveGame);

        currentY += 250;

        // Buttons
        buttonTestConnection = new Button
        {
            Text = "Test Connection",
            Left = 10,
            Top = currentY,
            Width = 130
        };
        buttonTestConnection.Click += ButtonTestConnection_Click;
        this.Controls.Add(buttonTestConnection);

        buttonTestNotification = new Button
        {
            Text = "Test Notification",
            Left = 145,
            Top = currentY,
            Width = 130
        };
        buttonTestNotification.Click += ButtonTestNotification_Click;
        this.Controls.Add(buttonTestNotification);

        buttonCancel = new Button
        {
            Text = "Cancel",
            Left = controlLeft + controlWidth - 160,
            Top = currentY,
            Width = 75
        };
        buttonCancel.Click += ButtonCancel_Click;
        this.Controls.Add(buttonCancel);

        buttonSave = new Button
        {
            Text = "Save",
            Left = controlLeft + controlWidth - 80,
            Top = currentY,
            Width = 75
        };
        buttonSave.Click += ButtonSave_Click;
        this.Controls.Add(buttonSave);
    }

    private void LoadSettingsToForm()
    {
        textBoxHost.Text = _settings.TransmissionHost;
        numericPort.Value = _settings.TransmissionPort;
        textBoxUsername.Text = _settings.Username;
        textBoxPassword.Text = _settings.Password;
        numericPollingInterval.Value = _settings.PollingIntervalSeconds;
        checkBoxUseHttps.Checked = _settings.UseHttps;
        checkBoxAutoStart.Checked = _settings.AutoStartWithWindows;
        checkBoxDisableNotifications.Checked = _settings.DisableNotifications;

        // Load game detection settings
        checkBoxEnableGameDetection.Checked = _settings.EnableGameDetection;
        radioNotifyOnly.Checked = _settings.GameDetectionBehavior == Models.GameDetectionBehavior.NotifyOnly;
        radioAutoPause.Checked = _settings.GameDetectionBehavior == Models.GameDetectionBehavior.AutoPause;
        listBoxGames.Items.Clear();
        foreach (var process in _settings.MonitoredGameProcesses)
        {
            listBoxGames.Items.Add(process);
        }

        // Trigger the event handler to enable/disable controls
        CheckBoxEnableGameDetection_CheckedChanged(null, EventArgs.Empty);
    }

    private void SaveFormToSettings()
    {
        _settings.TransmissionHost = textBoxHost.Text.Trim();
        _settings.TransmissionPort = (int)numericPort.Value;
        _settings.Username = textBoxUsername.Text.Trim();
        _settings.Password = textBoxPassword.Text;
        _settings.PollingIntervalSeconds = (int)numericPollingInterval.Value;
        _settings.UseHttps = checkBoxUseHttps.Checked;
        _settings.AutoStartWithWindows = checkBoxAutoStart.Checked;
        _settings.DisableNotifications = checkBoxDisableNotifications.Checked;

        // Save game detection settings
        _settings.EnableGameDetection = checkBoxEnableGameDetection.Checked;
        _settings.MonitoredGameProcesses = listBoxGames.Items.Cast<string>().ToList();
        _settings.GameDetectionBehavior = radioAutoPause.Checked
            ? Models.GameDetectionBehavior.AutoPause
            : Models.GameDetectionBehavior.NotifyOnly;
    }

    private bool ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(textBoxHost.Text))
        {
            MessageBox.Show("Please enter a Transmission host.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            textBoxHost.Focus();
            return false;
        }

        if (numericPort.Value < 1 || numericPort.Value > 65535)
        {
            MessageBox.Show("Please enter a valid port number (1-65535).", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            numericPort.Focus();
            return false;
        }

        if (checkBoxEnableGameDetection.Checked && listBoxGames.Items.Count == 0)
        {
            MessageBox.Show("Please add at least one game process to monitor.",
                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private async void ButtonTestConnection_Click(object? sender, EventArgs e)
    {
        if (!ValidateSettings())
            return;

        SaveFormToSettings();

        buttonTestConnection.Enabled = false;
        buttonTestConnection.Text = "Testing...";

        try
        {
            _transmissionClient ??= new TransmissionClient(_settings);
            bool success = await _transmissionClient.TestConnectionAsync();

            if (success)
            {
                MessageBox.Show("Connection successful!", "Test Connection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Connection failed. Please check your settings.", "Test Connection",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Connection error: {ex.Message}", "Test Connection",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            buttonTestConnection.Enabled = true;
            buttonTestConnection.Text = "Test Connection";
        }
    }

    private void ButtonSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateSettings())
            return;

        SaveFormToSettings();

        try
        {
            // Save settings to file
            SettingsManager.SaveSettings(_settings);

            // Update Windows startup registry
            SettingsManager.SetWindowsStartup(_settings.AutoStartWithWindows);

            SettingsSaved = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ButtonCancel_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void ButtonTestNotification_Click(object? sender, EventArgs e)
    {
        // Create a temporary NotifyIcon to test notifications
        using var testIcon = new NotifyIcon();
        testIcon.Icon = SystemIcons.Application;
        testIcon.Visible = true;
        testIcon.Text = "Transmission Tray Agent - Test";

        // Show test notification
        testIcon.ShowBalloonTip(
            5000,
            "Test Notification",
            "If you can see this, notifications are working correctly!",
            ToolTipIcon.Info
        );

        // Keep the icon visible for a few seconds
        System.Threading.Thread.Sleep(100);

        MessageBox.Show(
            "Test notification sent!\n\n" +
            "If you didn't see a notification, check:\n" +
            "1. Windows Focus Assist settings (should be Off or Priority only)\n" +
            "2. Windows Notifications & actions settings\n" +
            "3. Make sure 'Get notifications from apps and other senders' is ON",
            "Test Notification",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // Game Detection Event Handlers

    private void CheckBoxEnableGameDetection_CheckedChanged(object? sender, EventArgs e)
    {
        bool isEnabled = checkBoxEnableGameDetection.Checked;
        radioNotifyOnly.Enabled = isEnabled;
        radioAutoPause.Enabled = isEnabled;
        labelGameInstructions.Enabled = isEnabled;
        listBoxGames.Enabled = isEnabled;
        textBoxGameProcess.Enabled = isEnabled;
        buttonAddGame.Enabled = isEnabled;
        buttonRemoveGame.Enabled = isEnabled;
    }

    private void ButtonAddGame_Click(object? sender, EventArgs e)
    {
        string processName = textBoxGameProcess.Text.Trim();

        if (ValidateProcessName(processName))
        {
            listBoxGames.Items.Add(processName);
            textBoxGameProcess.Clear();
            textBoxGameProcess.Focus();
        }
    }

    private void ButtonRemoveGame_Click(object? sender, EventArgs e)
    {
        if (listBoxGames.SelectedItem != null)
        {
            listBoxGames.Items.Remove(listBoxGames.SelectedItem);
        }
    }

    private void TextBoxGameProcess_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Enter)
        {
            e.Handled = true;
            ButtonAddGame_Click(sender, e);
        }
    }

    private bool ValidateProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            MessageBox.Show("Process name cannot be empty.", "Invalid Input",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (processName.IndexOfAny(invalidChars) >= 0)
        {
            MessageBox.Show("Process name contains invalid characters.", "Invalid Input",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // Auto-append .exe if missing (user convenience)
        if (!processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            var result = MessageBox.Show(
                $"Add '.exe' extension to '{processName}'?",
                "Missing Extension",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                textBoxGameProcess.Text = processName + ".exe";
                return false; // Return false to re-trigger add with updated text
            }
        }

        // Check for duplicates
        foreach (var item in listBoxGames.Items)
        {
            if (item.ToString()!.Equals(processName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("This process is already in the list.", "Duplicate Entry",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        return true;
    }
}
