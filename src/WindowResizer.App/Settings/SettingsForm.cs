using WindowResizer.Core.Settings;

namespace WindowResizer.App.Settings;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _windowWidthInput;
    private readonly Button _saveButton;

    public SettingsForm(AppSettings currentSettings)
    {
        ArgumentNullException.ThrowIfNull(currentSettings);

        Text = "Window Resizer Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(320, 120);

        var widthLabel = new Label
        {
            AutoSize = true,
            Text = "Window width (px)",
            Left = 16,
            Top = 18
        };

        _windowWidthInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 20000,
            Value = currentSettings.WindowWidthPx,
            Left = 16,
            Top = 42,
            Width = 120
        };

        _saveButton = new Button
        {
            Text = "Save",
            DialogResult = DialogResult.OK,
            Left = 148,
            Top = 78,
            Width = 72
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Left = 232,
            Top = 78,
            Width = 72
        };

        Controls.Add(widthLabel);
        Controls.Add(_windowWidthInput);
        Controls.Add(_saveButton);
        Controls.Add(cancelButton);

        AcceptButton = _saveButton;
        CancelButton = cancelButton;

        _saveButton.Click += (_, _) => ApplyChanges();

        SavedSettings = currentSettings;
    }

    public AppSettings SavedSettings { get; private set; }

    public NumericUpDown WindowWidthInput => _windowWidthInput;

    public Button SaveButtonControl => _saveButton;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            ApplyChanges();
        }

        base.OnFormClosing(e);
    }

    public void ApplyChanges()
    {
        var updatedSettings = SavedSettings with
        {
            WindowWidthPx = decimal.ToInt32(_windowWidthInput.Value)
        };

        AppSettingsValidator.Validate(updatedSettings);
        SavedSettings = updatedSettings;
    }
}
