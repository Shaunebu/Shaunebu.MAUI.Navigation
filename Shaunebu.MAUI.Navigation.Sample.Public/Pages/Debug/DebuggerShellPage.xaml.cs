using Shaunebu.MAUI.Navigation.Debugger.Abstractions;
using Shaunebu.MAUI.Navigation.Debugger.Recording;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Debug;

/// <summary>
/// Minimal MAUI debugger overlay shell.
/// Demonstrates attach/detach, session recording, export/import, warning
/// visualization, stack inspection, and timeline replay. All debugger
/// interactions are passive — no navigation operations are triggered.
/// </summary>
/// <remarks>
/// This page is only compiled and registered in DEBUG builds.
/// The route <c>debug/shell</c> is automatically excluded from Release via csproj conditions.
/// Navigate to it with: <c>await navigation.NavigateToAsync&lt;DebuggerShellPage&gt;()</c>
/// or <c>await Shell.Current.GoToAsync("debug/shell")</c>.
/// </remarks>
[NavigationRoute("debug/shell")]
public partial class DebuggerShellPage : ContentPage
{
    private readonly IDebuggerOverlayHost _host;
    private readonly INavigationSessionRecorder _recorder;
    private readonly INavigationDiagnosticsExporter _exporter;
    private readonly INavigationDiagnosticsImporter _importer;
    private readonly INavigationTimelineReplayer _replayer;

    // Backing store for an imported session used by replay load.
    private NavigationDiagnosticsSession? _importedSession;
    // Backing store for the last exported JSON so Import can round-trip it.
    private string? _lastExportedJson;
    // Playback cancellation for the auto-advance loop.
    private CancellationTokenSource? _playCts;

    public DebuggerShellPage(
        IDebuggerOverlayHost host,
        INavigationSessionRecorder recorder,
        INavigationDiagnosticsExporter exporter,
        INavigationDiagnosticsImporter importer,
        INavigationTimelineReplayer replayer)
    {
        InitializeComponent();
        _host     = host;
        _recorder = recorder;
        _exporter = exporter;
        _importer = importer;
        _replayer = replayer;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAllAsync().ConfigureAwait(false);
    }

    // ── Attach / Detach ────────────────────────────────────────────────────

    private async void OnAttachClicked(object sender, EventArgs e)
    {
        try
        {
            await _host.AttachAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Attach failed", ex).ConfigureAwait(false);
        }
        RefreshAttachStatus();
    }

    private async void OnDetachClicked(object sender, EventArgs e)
    {
        try
        {
            await StopPlaybackAsync().ConfigureAwait(false);
            await _host.DetachAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Detach failed", ex).ConfigureAwait(false);
        }
        RefreshAttachStatus();
    }

    // ── Session Recorder ──────────────────────────────────────────────────

    private async void OnStartSessionClicked(object sender, EventArgs e)
    {
        try { await _recorder.StartSessionAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowErrorAsync("StartSession failed", ex).ConfigureAwait(false); }
        RefreshRecordingStatus();
    }

    private async void OnEndSessionClicked(object sender, EventArgs e)
    {
        try { await _recorder.EndSessionAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowErrorAsync("EndSession failed", ex).ConfigureAwait(false); }
        RefreshRecordingStatus();
    }

    private async void OnResetSessionClicked(object sender, EventArgs e)
    {
        try { await _recorder.ResetSessionAsync().ConfigureAwait(false); }
        catch (Exception ex) { await ShowErrorAsync("ResetSession failed", ex).ConfigureAwait(false); }
        RefreshRecordingStatus();
    }

    // ── Export / Import ───────────────────────────────────────────────────

    private async void OnExportJsonClicked(object sender, EventArgs e)
    {
        try
        {
            var json = await _exporter.ExportToJsonAsync(_recorder.CurrentSession).ConfigureAwait(false);
            _lastExportedJson = json;

            // Defensive validation: ensure JSON is valid before displaying.
            // This catches any corruption introduced by the exporter.
            try
            {
                using var _ = System.Text.Json.JsonDocument.Parse(json);
            }
            catch (System.Text.Json.JsonException validationEx)
            {
                await ShowErrorAsync("Export produced invalid JSON", validationEx).ConfigureAwait(false);
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ExportPreviewEditor.Text = json;
                ImportStatusLabel.Text = string.Empty;
            });
        }
        catch (Exception ex) { await ShowErrorAsync("ExportJson failed", ex).ConfigureAwait(false); }
    }

    private async void OnExportMarkdownClicked(object sender, EventArgs e)
    {
        try
        {
            var md = await _exporter.ExportMarkdownReportAsync(_recorder.CurrentSession).ConfigureAwait(false);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ExportPreviewEditor.Text = md;
                ImportStatusLabel.Text = string.Empty;
            });
        }
        catch (Exception ex) { await ShowErrorAsync("ExportMarkdown failed", ex).ConfigureAwait(false); }
    }

    private async void OnSaveJsonFileClicked(object sender, EventArgs e)
    {
        try
        {
            var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"NavDiagnostics_{timestamp}.json";
            var filePath = Path.Combine(downloadsPath, fileName);

            await _exporter.ExportToFileAsync(
                _recorder.CurrentSession,
                filePath,
                Shaunebu.MAUI.Navigation.Debugger.Export.DiagnosticsExportFormat.Json)
                .ConfigureAwait(false);

            // Verify the saved file is valid JSON
            var savedJson = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            using var _ = System.Text.Json.JsonDocument.Parse(savedJson);

            MainThread.BeginInvokeOnMainThread(() =>
                ImportStatusLabel.Text = $"Saved to: {filePath}");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Save JSON file failed", ex).ConfigureAwait(false);
        }
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        var json = ExportPreviewEditor.Text;
        if (string.IsNullOrWhiteSpace(json))
        {
            MainThread.BeginInvokeOnMainThread(() => ImportStatusLabel.Text = "Nothing to import.");
            return;
        }

        // Defensive check: detect common corruption artifacts before attempting import.
        if (json.Contains("',") || json.Contains("[]'") || json.Contains("[],'" ))
        {
            MainThread.BeginInvokeOnMainThread(() =>
                ImportStatusLabel.Text = "ERROR: JSON contains stray quote character. Please re-export.");
            return;
        }

        try
        {
            var session = await _importer.ImportFromJsonAsync(json).ConfigureAwait(false);
            _importedSession = session;
            MainThread.BeginInvokeOnMainThread(() =>
                ImportStatusLabel.Text =
                    $"Imported: {session.Operations.Count} ops  |  id={session.SessionId}");
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                ImportStatusLabel.Text = $"Import failed: {ex.Message}");
        }
    }

    // ── Replay Session Viewer ─────────────────────────────────────────────

    private async void OnReplayLoadClicked(object sender, EventArgs e)
    {
        // Prefer an imported session; fall back to the live recorder session.
        var session = _importedSession ?? _recorder.CurrentSession;
        try
        {
            await StopPlaybackAsync().ConfigureAwait(false);
            await _replayer.LoadSessionAsync(session).ConfigureAwait(false);
            RefreshReplayStatus();
            RefreshReplayFrames();
        }
        catch (Exception ex) { await ShowErrorAsync("LoadSession failed", ex).ConfigureAwait(false); }
    }

    private void OnReplayPlayClicked(object sender, EventArgs e)
    {
        if (!_replayer.IsSessionLoaded) return;
        if (_playCts is not null) return; // already playing

        _playCts = new CancellationTokenSource();
        var token = _playCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested && _replayer.CurrentIndex < _replayer.TotalFrames - 1)
                {
                    await _replayer.StepForwardAsync(token).ConfigureAwait(false);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RefreshReplayStatus();
                        UpdateCurrentFrameLabel();
                    });
                    await Task.Delay(400, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                    await ShowErrorAsync("Playback error", ex).ConfigureAwait(false));
            }
            finally
            {
                _playCts?.Dispose();
                _playCts = null;
            }
        }, token);
    }

    private async void OnReplayPauseClicked(object sender, EventArgs e)
    {
        await StopPlaybackAsync().ConfigureAwait(false);
    }

    private async void OnReplayStopClicked(object sender, EventArgs e)
    {
        await StopPlaybackAsync().ConfigureAwait(false);
        if (_replayer.IsSessionLoaded)
        {
            await _replayer.SeekToAsync(0).ConfigureAwait(false);
            RefreshReplayStatus();
            UpdateCurrentFrameLabel();
        }
    }

    // ── Refresh all ───────────────────────────────────────────────────────

    private async void OnRefreshAllClicked(object sender, EventArgs e)
    {
        await RefreshAllAsync().ConfigureAwait(false);
    }

    private Task RefreshAllAsync()
    {
        RefreshAttachStatus();
        RefreshRecordingStatus();
        RefreshWarnings();
        RefreshStack();
        RefreshReplayStatus();
        RefreshReplayFrames();
        return Task.CompletedTask;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void RefreshAttachStatus()
    {
        MainThread.BeginInvokeOnMainThread(() =>
            AttachStatusLabel.Text = _host.IsAttached ? "Attached ✓" : "Detached");
    }

    private void RefreshRecordingStatus()
    {
        MainThread.BeginInvokeOnMainThread(() =>
            RecordingStatusLabel.Text = _recorder.IsRecording ? "Recording ●" : "Idle ○");
    }

    private void RefreshWarnings()
    {
        // Read warnings from the dashboard ViewModel that the host exposes.
        var warnings = _host.Dashboard.Warnings.Warnings;
        MainThread.BeginInvokeOnMainThread(() =>
            WarningsCollection.ItemsSource = warnings.Count > 0
                ? (System.Collections.IEnumerable)warnings
                : null);
    }

    private void RefreshStack()
    {
        var navStack = _host.Dashboard.Stack.NavigationStack;
        MainThread.BeginInvokeOnMainThread(() =>
            StackViewerLabel.Text = navStack.Count > 0
                ? string.Join(" → ", navStack)
                : "(empty)");
    }

    private void RefreshReplayStatus()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_replayer.IsSessionLoaded)
            {
                ReplayStatusLabel.Text = "No session loaded.";
                ReplayFrameLabel.Text  = "Frame: —";
                return;
            }
            var v = _replayer.ValidationResult;
            var valid = v is null || v.IsReplayable ? "valid" : $"CORRUPTED ({v.Issues.Count} issue(s))";
            ReplayStatusLabel.Text = $"{_replayer.TotalFrames} frames  |  session {valid}";
            UpdateCurrentFrameLabel();
        });
    }

    private void UpdateCurrentFrameLabel()
    {
        var frame = _replayer.CurrentFrame;
        ReplayFrameLabel.Text = frame is null
            ? "Frame: —"
            : $"Frame {_replayer.CurrentIndex + 1}/{_replayer.TotalFrames}  |  {frame.Operation.Route ?? frame.Operation.PageTypeName ?? "(unknown)"}";
    }

    private void RefreshReplayFrames()
    {
        var frames = _replayer.IsSessionLoaded ? _replayer.Frames : null;
        MainThread.BeginInvokeOnMainThread(() =>
            ReplayFramesCollection.ItemsSource = frames?.Count > 0
                ? (System.Collections.IEnumerable)frames
                : null);
    }

    private async Task StopPlaybackAsync()
    {
        if (_playCts is null) return;
        await _playCts.CancelAsync().ConfigureAwait(false);
        _playCts?.Dispose();
        _playCts = null;
    }

    private Task ShowErrorAsync(string title, Exception ex)
        => MainThread.InvokeOnMainThreadAsync(() =>
            DisplayAlert(title, ex.Message, "OK"));
}
