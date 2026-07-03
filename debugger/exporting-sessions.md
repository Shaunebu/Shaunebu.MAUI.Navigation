# Exporting Sessions

## Overview

The `INavigationDiagnosticsExporter` and `INavigationDiagnosticsImporter` services let you serialize a recorded `NavigationDiagnosticsSession` to JSON or Markdown, persist it to a file, and re-import it later for replay or analysis.

Export/import is enabled when `NavigationDebuggerOptions.EnableExport = true` (the default).

---

## INavigationDiagnosticsExporter

```csharp
public interface INavigationDiagnosticsExporter
{
	Task<string> ExportToJsonAsync(
		NavigationDiagnosticsSession session,
		CancellationToken cancellationToken = default);

	Task ExportToFileAsync(
		NavigationDiagnosticsSession session,
		string filePath,
		DiagnosticsExportFormat format,
		CancellationToken cancellationToken = default);

	Task<string> ExportMarkdownReportAsync(
		NavigationDiagnosticsSession session,
		CancellationToken cancellationToken = default);
}
```

---

## INavigationDiagnosticsImporter

```csharp
public interface INavigationDiagnosticsImporter
{
	Task<NavigationDiagnosticsSession> ImportFromJsonAsync(
		string json,
		CancellationToken cancellationToken = default);

	Task<NavigationDiagnosticsSession> ImportFromFileAsync(
		string filePath,
		CancellationToken cancellationToken = default);
}
```

---

## DiagnosticsExportFormat

| Value | Output |
|---|---|
| `Json` | Structured JSON file (`.json`) |
| `Markdown` | Human-readable Markdown report (`.md`) |

---

## Exporting to JSON

```csharp
var session = _recorder.CurrentSession;
var json    = await _exporter.ExportToJsonAsync(session);

// Write to app data folder
var path = Path.Combine(FileSystem.AppDataDirectory, "nav-session.json");
await File.WriteAllTextAsync(path, json);
```

---

## Exporting to a File Directly

```csharp
var path = Path.Combine(FileSystem.AppDataDirectory, "nav-session.json");

await _exporter.ExportToFileAsync(
	session: _recorder.CurrentSession,
	filePath: path,
	format: DiagnosticsExportFormat.Json);
```

---

## Exporting a Markdown Report

```csharp
var markdown = await _exporter.ExportMarkdownReportAsync(_recorder.CurrentSession);

// Share via MAUI Share API
await Share.RequestAsync(new ShareTextRequest
{
	Title   = "Navigation Session Report",
	Text    = markdown,
	Subject = "nav-session-report.md"
});
```

---

## Importing a Session

```csharp
// Import from file
var session = await _importer.ImportFromFileAsync(path);

// Load into replayer for frame-by-frame analysis
await _replayer.LoadSessionAsync(session);
```

---

## Sharing Sessions With the VSIX Extension

The VSIX **Timeline** panel can open exported `.json` session files directly:

1. Export the session from your debug app run.
2. In Visual Studio: **View → Other Windows → Navigation Inspector**.
3. Click **Open Session** and select the `.json` file.
4. Use step forward/backward controls to replay each operation.

---

## Enabling Export

```csharp
builder.Services.UseNavigationDebugger(opts =>
{
	opts.EnableExport = true; // default: true
});
```

---

## Related Pages

- [Debugger Overview](overview.md)
- [Session Recording](session-recording.md)
- [Timeline Replay](timeline-replay.md)
- [VSIX Features](../vsix/features.md)
