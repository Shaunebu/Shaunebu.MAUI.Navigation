# Live Navigation Monitoring

> **Status:** Implemented (feature/NavigationInspector)
> **Requires:** Shaunebu.MAUI.Navigation.Debugger ≥ 1.0.0-preview.2, Shaunebu.MAUI.Navigation.Vsix ≥ 1.0.0-preview.2

---

## Overview

Live Navigation Monitoring streams navigation diagnostic events from a running MAUI
application to the Visual Studio Navigation Session Inspector in real time.  No JSON
export/import step is required; events appear in the VSIX timeline within milliseconds
of occurring in the app.

The feature coexists with the existing offline JSON replay path:

| Mode | Flow |
|---|---|
| **Offline (Mode A)** | MAUI App → Export JSON → VSIX Import → Replay |
| **Live (Mode B)**   | MAUI App → TCP Stream → VSIX Live Session |

---

## Architecture

### Transport

**TCP with newline-delimited JSON (NDJSON).**

| Candidate | Windows host | Android emulator | Android device | iOS | Notes |
|---|---|---|---|---|---|
| Named Pipes | ✅ | ❌ | ❌ | ❌ | OS-boundary; breaks outside host |
| Memory-mapped files | ✅ | ❌ | ❌ | ❌ | Same restriction |
| WebSocket | ✅ | ✅ | ✅ | ✅ | Requires HTTP upgrade; extra lib |
| **TCP + NDJSON** | ✅ | ✅ | ✅ | ✅ | Chosen |

Rationale:
- Works for all targets. For Android emulators, `AdbTunnelService` automatically runs `adb forward tcp:17382 tcp:17382` before connecting — no manual setup required.
- For physical Android devices, connect the device via USB and either let adb forward run automatically when an emulator session is detected, or connect using the device's LAN IP.
- Reuses the existing `System.Text.Json` source-gen infrastructure; no new serializer.
- No HTTP negotiation overhead; minimal latency.
- Default port **17382** (configurable via `NavigationDebuggerOptions.LivePort`).

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│  MAUI Process  (net9.0 / net10.0)                                   │
│                                                                     │
│  NavigationDiagnosticsBus ──Subscribe()──► LiveNavigationPublisher  │
│                                                  │                  │
│                                           TcpListenerServer         │
│                                           LiveEventSerializer        │
│                                           HeartbeatLoop             │
└─────────────────────────────────────────────────────────────────────┘
			│  TCP 127.0.0.1:17382  (NDJSON, one message per line)
			▼
┌─────────────────────────────────────────────────────────────────────┐
│  VSIX Process  (net48 / Vsix.Desktop)                               │
│                                                                     │
│  AdbTunnelService (auto-forward on emulator connect)                │
│       │  adb forward tcp:17382 tcp:17382  (only when needed)        │
│       ▼                                                             │
│  LiveSessionClient ◄── TcpClient reconnect loop                     │
│       │  ReadLineAsync → LiveEventEnvelope                          │
│       │  StopAsync closes socket to unblock read loop               │
│       ▼                                                             │
│  LiveSessionManager  (re-entrancy guard; exception-safe sink)       │
│       │  SessionStart → InspectorShellViewModel.BeginLiveSession()  │
│       │  NavEvent     → TimelinePanelViewModel.AppendLiveFrame()    │
│       │               → StackViewerPanelViewModel.ShowLiveFrame()   │
│       │               → WarningViewerPanelViewModel.AppendLiveWarning() │
│       │  Heartbeat    → (connection-alive acknowledgement)          │
│       │  SessionEnd   → InspectorShellViewModel.EndLiveSession()    │
│       │                                                             │
│  InspectorShellViewModel                                            │
│       ConnectLiveCommand / DisconnectCommand                        │
│       LiveMode  / ConnectionStatus                                  │
└─────────────────────────────────────────────────────────────────────┘
```

### Wire Protocol

Each message is a single JSON object followed by `\n` (NDJSON).

#### Envelope schema

```json
{
  "type": "NavEvent | Heartbeat | SessionStart | SessionEnd",
  "ts":   "2025-01-01T00:00:00.0000000Z",
  "sessionId": "...",
  "appVersion": "...",
  "platform":  "...",
  "os":        "...",
  "eventType": "NavigationStartedEvent | ...",
  "payload":   { ... }
}
```

| `type` | Required extra fields | Direction |
|---|---|---|
| `SessionStart` | `sessionId`, `appVersion`, `platform`, `os` | App → VSIX |
| `NavEvent` | `eventType`, `payload` | App → VSIX |
| `Heartbeat` | — | App → VSIX (every 5 s) |
| `SessionEnd` | — | App → VSIX |

#### Concrete `eventType` values

`NavigationStartedEvent`, `NavigationSucceededEvent`, `NavigationFailedEvent`,
`GuardRejectedEvent`, `GuardRedirectedEvent`, `BackNavigationRequestedEvent`,
`BackNavigationSucceededEvent`, `BackNavigationFailedEvent`,
`OverlayShownEvent`, `OverlayHiddenEvent`,
`FlowEnteredEvent`, `FlowExitedEvent`,
`StackCorruptionEvent`, `RuntimeWarningEvent`

### Sequence Diagram — Connect and Stream

```
VS User        VSIX Inspector        LiveSessionManager     MAUI App
   |--Connect-->|                          |                    |
   |            |--ConnectLiveAsync()-->   |                    |
   |            |                    TcpClient.ConnectAsync()   |
   |            |                          |<----TCP accept-----|
   |            |                          |<--SessionStart-----|
   |            |<--IsLive=true, status----|                    |
   |            |                          |<--NavEvent---------|
   |            |               AppendLiveFrame(frame)          |
   |            |<--AllFrames row added----|                    |
   |            |                          |<--Heartbeat--------|
   |            |                          |   (reset timeout)  |
   |--Pause---> |                          |                    |
   |            |--PauseLive()------------>|  (buffer events)   |
   |--Resume--> |                          |                    |
   |            |--ResumeLive()----------->|  (flush buffer)    |
   |--Disconnect|                          |                    |
   |            |--DisconnectAsync()------>|                    |
   |            |                    socket.Close()             |
   |            |<--IsLive=false, status---|                    |
```

### Sequence Diagram — Reconnect

```
LiveSessionClient              MAUI App
	  |                            |
	  |<--TCP disconnect-----------|
	  |   (IOException)            |
	  |--wait ReconnectDelay (2s)--|
	  |--TcpClient.ConnectAsync()-->
	  |<--TCP accept---------------|
	  |<--SessionStart-------------|
	  ... resumes normal streaming
```

### Data Flow Diagram

```
Bus.Publish(INavigationDiagnosticEvent)
		│
		▼
LiveNavigationPublisher.ReadLoopAsync()
		│
		├── Serialize to LiveEventEnvelope JSON line
		├── Write to NetworkStream (TcpClient)
		└── Backpressure: bounded queue (512), drop-oldest if full

NetworkStream (NDJSON over TCP)
		│
		▼
LiveSessionClient.ReadLoopAsync()
		│
		├── StreamReader.ReadLineAsync()
		├── Deserialize to LiveEventEnvelope
		└── Post to ILiveSessionManager

LiveSessionManager.HandleEnvelopeAsync()
		│
		├── SessionStart  → build NavigationDiagnosticsSession stub, call InspectorShellViewModel.BeginLiveSession()
		├── NavEvent      → build NavigationReplayFrame, append to session
		│                    InspectorShellViewModel._liveSession updates
		│                    TimelinePanelViewModel.AppendLiveFrame()
		│                    StackViewerPanelViewModel.ShowLiveFrame()
		│                    WarningViewerPanelViewModel.AppendLiveWarning()
		├── Heartbeat     → reset watchdog
		└── SessionEnd    → InspectorShellViewModel.EndLiveSession()
```

---

## Configuration

```csharp
builder.Services.UseNavigationDebugger(opts =>
{
	opts.EnableLiveStreaming = true;   // Default: false
	opts.LivePort            = 17382; // Default: 17382
	opts.LiveHeartbeatIntervalMs = 5000; // Default: 5000
});
```

### Android emulator

The VSIX automatically forwards the port when it detects a running emulator. No
manual `adb forward` command is required:

1. Start the Android emulator (via Visual Studio or standalone `emulator.exe`).
2. Click **Connect** in the inspector toolbar using `127.0.0.1:17382`.
3. `AdbTunnelService` locates `adb.exe`, detects the emulator via `adb devices`,
   and runs `adb forward tcp:17382 tcp:17382` automatically if not already set up.
4. The status bar shows a diagnostic message if adb is not found or the forward fails.

**Discovery order for `adb.exe`:**
1. `%ANDROID_HOME%\platform-tools\adb.exe`
2. `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe`
3. First `adb.exe` found on `%PATH%`

**Physical Android devices** — adb forward is only created automatically when an
emulator is detected. For a physical device either:
- Ensure `adb forward tcp:17382 tcp:17382` has been run after connecting via USB, or
- Connect using the device's local IP address directly in the Host field.

---

## VSIX Usage

1. Open a .NET MAUI project in Visual Studio.
2. Open **View → Other Windows → Navigation Session Inspector**.
3. Run the MAUI app (debug build) with `EnableLiveStreaming = true`.
4. Click **Connect** in the inspector toolbar — the host and port are pre-filled with `127.0.0.1:17382`.
   - For Android emulator: the VSIX automatically creates the `adb forward` if needed.
   - For physical device: either ensure `adb forward` was run, or enter the device's LAN IP.
5. Navigate in the app — events stream into the timeline in real time.
6. Click **Pause** to freeze the live feed without disconnecting.
7. Click **Resume** to resume streaming.
8. Click **Disconnect** to end the live session cleanly (no Visual Studio restart required).
9. The captured live session can be exported as JSON via the normal **Export** button.

---

## Backward Compatibility

Live mode is additive and opt-in:

- `EnableLiveStreaming` defaults to `false` — zero runtime cost when disabled.
- Offline JSON import/export/replay is unchanged.
- Both modes can be active simultaneously (record to session AND stream live).
- The VSIX **Load Session** button is always available, even when live mode is active.

---

## Security

- The TCP server binds to `127.0.0.1` (loopback only) — not reachable from the network.
- Only intended for **DEBUG builds** (wired in the `#if DEBUG` block in `MauiProgram.cs`).
- Parameter values are never transmitted over the live stream (same sanitization as the offline recorder).

---

## Testing

See `tests/Shaunebu.MAUI.Navigation.Vsix.Tests/Live/` for:

- `LiveEventSerializerTests` — all 12 event type round-trip serializations
- `LiveSessionManagerTests` — envelope routing, pause/resume buffer, frame construction, all stack-sync scenarios
- `InspectorShellViewModelLiveTests` — live commands, CanExecute guards, and sink methods
- `TimelinePanelViewModelLiveTests` — 12 live-mode timeline behavior tests
- `StackViewerPanelViewModelTests` — 6 live-mode `ShowLiveFrame` tests (nav stack + modal stack)
- `AdbTunnelServiceTests` — 8 unit tests covering forward-create, forward-exists, adb-not-found, no-emulator, command-failure, idempotency, and cancellation
- `LiveDisconnectTests` — 9 regression tests covering all disconnect lifecycle states (idle, connected, re-entrant, repeated cycles, disposal)
- `LiveIntegrationTests` — 10 end-to-end loopback TCP tests: SessionStart, NavSucceeded, BackNav, GuardRedirect, Overlay, RuntimeWarning, multi-nav ordering

All 128 VSIX tests pass. Total across all projects: 1 136 / 1 136.
