using AircraftExplorer.Audio;
using AircraftExplorer.Input;
using AircraftExplorer.Modes;

namespace AircraftExplorer;

public sealed class MainForm : Form
{
    private readonly ISpeechService _speech;
    private readonly KeyboardInputProvider _keyboard;
    private readonly InputManager _inputManager;
    private readonly AppModeManager _modeManager;
    private readonly ModeContext _context;
    private readonly Label _statusLabel;
    private readonly System.Windows.Forms.Timer _tickTimer;

    public MainForm(
        ISpeechService speech,
        KeyboardInputProvider keyboard,
        InputManager inputManager,
        AppModeManager modeManager,
        ModeContext context)
    {
        _speech = speech;
        _keyboard = keyboard;
        _inputManager = inputManager;
        _modeManager = modeManager;
        _context = context;

        // Form setup
        Text = "Aircraft Explorer";
        Size = new Size(600, 400);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 30);
        KeyPreview = true;

        // Status label — visual mirror of speech output for sighted helpers
        _statusLabel = new Label
        {
            Text = "Aircraft Explorer",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14f),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            AccessibleName = "Status",
            AccessibleRole = AccessibleRole.StaticText
        };
        Controls.Add(_statusLabel);

        // 60Hz tick timer for input polling and axis updates
        _tickTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _tickTimer.Tick += OnTick;

        KeyDown += OnKeyDown;
        FormClosing += OnFormClosing;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // Restore saved tone volume
        float savedVolume = _context.Settings.ToneVolume;
        _context.SpatialAudio.AdjustToneVolume(savedVolume - _context.SpatialAudio.ToneVolume);

        _speech.Speak("Welcome to Aircraft Explorer.", true);
        UpdateStatus("Welcome to Aircraft Explorer");
        _modeManager.Push(new MainMenuMode(), _context);

        _tickTimer.Start();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (KeyboardInputProvider.IsMappedKey(e.KeyData))
        {
            _keyboard.HandleKeyDown(e.KeyData);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_modeManager.HasModes)
        {
            RequestClose();
            return;
        }

        // Give current mode a tick for continuous input (e.g. flight axes)
        _modeManager.CurrentMode?.OnTick();

        var action = _inputManager.Poll();

        if (action is null || action == InputAction.None)
            return;

        // Global volume control — works in all modes
        if (action == InputAction.VolumeUp || action == InputAction.VolumeDown)
        {
            float delta = action == InputAction.VolumeUp ? 0.1f : -0.1f;
            _context.SpatialAudio.AdjustToneVolume(delta);
            _context.Settings.ToneVolume = _context.SpatialAudio.ToneVolume;
            int percent = (int)(_context.SpatialAudio.ToneVolume * 100);
            _speech.Speak($"Tone volume {percent} percent.", true);
            return;
        }

        var currentMode = _modeManager.CurrentMode;
        if (currentMode is null)
            return;

        var result = currentMode.HandleInput(action.Value);

        if (result.Transition == ModeTransition.Quit)
        {
            _speech.Speak("Goodbye. Thanks for exploring.", true);
            UpdateStatus("Goodbye");
            _tickTimer.Stop();

            // Brief delay so speech finishes before the window closes
            var closeTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            closeTimer.Tick += (_, _) =>
            {
                closeTimer.Stop();
                closeTimer.Dispose();
                Close();
            };
            closeTimer.Start();
            return;
        }

        _modeManager.ProcessResult(result, _context);

        // Update the visual status label with the current mode name
        if (_modeManager.CurrentMode is not null)
            UpdateStatus(_modeManager.CurrentMode.ModeName);
    }

    private void UpdateStatus(string text)
    {
        _statusLabel.Text = text;
    }

    private void RequestClose()
    {
        _tickTimer.Stop();
        Close();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _tickTimer.Stop();

        try { _context.Settings.Save(_context.SettingsFilePath); }
        catch { /* best-effort save */ }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tickTimer.Dispose();
            _statusLabel.Dispose();
        }
        base.Dispose(disposing);
    }
}
