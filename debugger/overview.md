# Debugger Overview

## Overview

The `Shaunebu.MAUI.Navigation.Debugger` package is a comprehensive runtime diagnostics platform for .NET MAUI navigation. It provides session recording, timeline replay, stack diffing, runtime warning evaluation, export/import, and a live in-app dashboard — all entirely absent from Release builds.

> **Important:** Only register the debugger inside `#if DEBUG` blocks. Never ship debugger infrastructure in production.

---

## Architecture

```
						 UseNavigationDebugger(services)
								  │
		 ┌────────────────────────┼────────────────────────┐
		 │                        │                        │
NavigationDiagnosticsBus  NavigationDiagnosticsBridge  NavigationDebuggerOptions
(channel-backed, bounded) (replaces INavigationDiagnostics,
						   IBackNavigationDiagnostics,
						   IOverlayDiagnostics)
		 │
		 ├── NavigationSessionRecorder   (records all ops into a session)
		 ├── NavigationRuntimeWarningEngine (live rule evaluation per event)
		 ├── NavigationStackDiffEngine   (before/after snapshot diff)
		 ├── NavigationDiagnosticsExporter (JSON / Markdown export)
		 ├── NavigationDiagnosticsImporter (JSON import)
		 ├── NavigationTimelineReplayer  (read-only frame-by-frame replay)
		 └── DebuggerOverlayHost         (feeds DebuggerDashboardViewModel)
											  │
								   DebuggerDashboardViewModel
								   TimelinePanelViewModel
```

---

## Registration

```csharp
// MauiProgram.cs
#if DEBUG
builder.Services.UseNavigationDebugger(opts =>
{
	opts.EnableSessionRecording = true;
	opts.EnableRuntimeWarnings  = true;
	opts.EnableStackDiffing     = true;
	opts.EnableExport           = true;
	opts.MaxOperationRecords    = 200;
});
#endif
```

`UseNavigationDebugger` is an extension on `IServiceCollection` (not `MauiAppBuilder`), enabling use from any DI context.

---

## Components

| Component | Description |
|---|---|
| `NavigationDiagnosticsBus` | Channel-backed event bus; distributes events to all subscribers |
| `NavigationDiagnosticsBridge` | Bridges the core `INavigationDiagnostics` interfaces to the bus |
| `NavigationSessionRecorder` | Records all navigation events into a bounded `NavigationDiagnosticsSession` |
| `NavigationRuntimeWarningEngine` | Evaluates each event against warning rules; emits `NavigationRuntimeWarning` |
| `NavigationStackDiffEngine` | Computes before/after snapshot diffs for each operation |
| `NavigationDiagnosticsExporter` | Exports sessions to JSON or Markdown |
| `NavigationDiagnosticsImporter` | Imports sessions from JSON |
| `NavigationTimelineReplayer` | Frame-by-frame read-only replay of a recorded session |
| `DebuggerOverlayHost` | Subscribes to bus events and feeds ViewModels for the live overlay |
| `DebuggerDashboardViewModel` | Bindable dashboard state (live events, stack, warnings, timeline) |

---

## NavigationDebuggerOptions

Configure via the `UseNavigationDebugger` delegate:

| Property | Default | Description |
|---|---|---|
| `EnableSessionRecording` | `true` | Record all operations into a session |
| `EnableRuntimeWarnings` | `true` | Evaluate warning rules per event |
| `EnableStackDiffing` | `true` | Compute stack diffs per operation |
| `EnableInAppOverlay` | `false` | Show an in-app overlay panel |
| `EnableExport` | `true` | Register exporter/importer services |
| `MaxSessionDurationMinutes` | `60` | Session auto-eviction threshold |
| `MaxOperationRecords` | `500` | Maximum operation records before oldest-first eviction |
| `MaxWarningRecords` | `200` | Maximum warning records to retain |
| `DiagnosticsChannelCapacity` | `1000` | Bounded bus channel capacity |
| `UiRefreshThrottleMs` | `250` | Minimum interval between UI refresh callbacks |

---

## Related Pages

- [Session Recording](session-recording.md)
- [Timeline Replay](timeline-replay.md)
- [Stack Diffing](stack-diffing.md)
- [Runtime Warnings](runtime-warnings.md)
- [Exporting Sessions](exporting-sessions.md)
- [Performance Counters](performance-counters.md)
- [VSIX Overview](../vsix/overview.md)
