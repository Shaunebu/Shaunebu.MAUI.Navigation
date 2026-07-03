# Session Recording

## Overview

The `NavigationSessionRecorder` records all navigation activity into a bounded `NavigationDiagnosticsSession`. Sessions can be inspected live, exported to JSON, or loaded into the timeline replayer for post-mortem analysis.

---

## INavigationSessionRecorder

```csharp
public interface INavigationSessionRecorder : IAsyncDisposable
{
	NavigationDiagnosticsSession CurrentSession  { get; }
	bool                         IsRecording     { get; }

	Task StartSessionAsync(CancellationToken cancellationToken = default);
	Task EndSessionAsync(CancellationToken cancellationToken   = default);
	Task ResetSessionAsync(CancellationToken cancellationToken = default);
}
```

| Member | Description |
|---|---|
| `CurrentSession` | The active recording session |
| `IsRecording` | Whether a session is actively recording |
| `StartSessionAsync` | Begins recording (called automatically at startup) |
| `EndSessionAsync` | Ends the current session and marks it complete |
| `ResetSessionAsync` | Clears all recorded events and starts a fresh session |

---

## NavigationDiagnosticsSession

A session is a bounded record of all navigation operations that occurred during the recording window:

```csharp
var session = recorder.CurrentSession;

Console.WriteLine($"Session ID   : {session.SessionId}");
Console.WriteLine($"Started at   : {session.StartedAt}");
Console.WriteLine($"App version  : {session.AppVersion}");
Console.WriteLine($"Operations   : {session.Operations.Count}");
Console.WriteLine($"Warnings     : {session.Warnings.Count}");
```

Operations are evicted oldest-first when `MaxOperationRecords` is reached (default: 500).

---

## Accessing the Recorder

Resolve `INavigationSessionRecorder` from DI:

```csharp
// In a diagnostic ViewModel or service
private readonly INavigationSessionRecorder _recorder;

public DiagnosticsViewModel(INavigationSessionRecorder recorder)
	=> _recorder = recorder;

public void PrintSummary()
{
	var session = _recorder.CurrentSession;
	Console.WriteLine($"{session.Operations.Count} operations recorded");
}
```

---

## Resetting Between Test Scenarios

```csharp
// Before each test scenario
await recorder.ResetSessionAsync();

// ... execute scenario ...

// Inspect results
var session = recorder.CurrentSession;
Assert.Equal(2, session.Operations.Count);
```

---

## Enabling Recording

Recording is enabled via `NavigationDebuggerOptions`:

```csharp
builder.Services.UseNavigationDebugger(opts =>
{
	opts.EnableSessionRecording = true;
	opts.MaxOperationRecords    = 200;   // reduce for memory-sensitive scenarios
});
```

---

## Related Pages

- [Debugger Overview](overview.md)
- [Timeline Replay](timeline-replay.md)
- [Exporting Sessions](exporting-sessions.md)
