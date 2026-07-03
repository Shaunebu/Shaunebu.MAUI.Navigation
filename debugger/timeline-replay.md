# Timeline Replay

## Overview

`INavigationTimelineReplayer` provides frame-by-frame read-only replay of a recorded `NavigationDiagnosticsSession`. Replay does **not** re-execute navigation operations or interact with live MAUI state — it reconstructs the navigation history from recorded events.

The VSIX tool window's **Timeline** panel uses the replayer to let you step through a session file frame by frame.

---

## INavigationTimelineReplayer

```csharp
public interface INavigationTimelineReplayer
{
	NavigationReplayFrame?         CurrentFrame  { get; }
	int                            TotalFrames   { get; }
	int                            CurrentIndex  { get; }
	NavigationReplayCursor         Cursor        { get; }
	bool                           IsSessionLoaded { get; }
	IReadOnlyList<ReplayOperationGroup> Groups   { get; }

	Task LoadSessionAsync(NavigationDiagnosticsSession session, CancellationToken ct = default);
	Task<NavigationReplayFrame> StepForwardAsync(CancellationToken ct = default);
	Task<NavigationReplayFrame> StepBackwardAsync(CancellationToken ct = default);
	Task<NavigationReplayFrame> SeekToAsync(int operationIndex, CancellationToken ct = default);
}
```

---

## Loading a Session

```csharp
// Load the current live session
var session = _recorder.CurrentSession;
await _replayer.LoadSessionAsync(session);

Console.WriteLine($"Total frames: {_replayer.TotalFrames}");
```

Or load an imported session from a file:

```csharp
var session = await _importer.ImportFromFileAsync("session.json");
await _replayer.LoadSessionAsync(session);
```

---

## Stepping Through Frames

```csharp
// Step forward
var frame = await _replayer.StepForwardAsync();
Console.WriteLine($"[{_replayer.CurrentIndex}/{_replayer.TotalFrames}] {frame.OperationSummary}");

// Step backward
frame = await _replayer.StepBackwardAsync();

// Jump to a specific operation
frame = await _replayer.SeekToAsync(operationIndex: 5);
```

---

## NavigationReplayFrame

Each frame represents the state of the navigation system **after** a single recorded operation:

| Property | Description |
|---|---|
| `OperationSummary` | Human-readable description of the operation |
| `NavigationStack` | Stack state after this operation |
| `ModalStack` | Modal stack state after this operation |
| `FlowName` | Active flow at this point in the session |
| `Warnings` | Warnings triggered by this operation |
| `Diff` | Stack diff (before/after) computed by `NavigationStackDiffEngine` |

---

## NavigationReplayCursor

| Value | Description |
|---|---|
| `AtStart` | Cursor is at the first frame |
| `AtEnd` | Cursor is at the last frame |
| `InProgress` | Cursor is between the first and last frame |
| `NoSession` | No session is loaded |

---

## Operation Groups

Frames are grouped by flow for structured timeline display:

```csharp
foreach (var group in _replayer.Groups)
{
	Console.WriteLine($"Flow: {group.FlowName}");
	foreach (var op in group.Operations)
		Console.WriteLine($"  [{op.Index}] {op.Summary}");
}
```

---

## Related Pages

- [Debugger Overview](overview.md)
- [Session Recording](session-recording.md)
- [Stack Diffing](stack-diffing.md)
- [Exporting Sessions](exporting-sessions.md)
- [VSIX — Timeline Panel](../vsix/features.md)
