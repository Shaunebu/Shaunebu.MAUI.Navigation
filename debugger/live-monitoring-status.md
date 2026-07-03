# Live Navigation Monitoring - Status

**Last Updated:** 2026-07-03
**Overall Completion:** 100%

---

## Executive Summary

### What is already implemented

- **Runtime publisher (MAUI side)** - fully implemented. `LiveNavigationPublisher` subscribes to the `INavigationDiagnosticsBus`, accepts a single TCP client, buffers events in a bounded `Channel<string>` (drop-oldest, 512 capacity), emits `SessionStart` with app metadata, streams `NavEvent` NDJSON envelopes for all 12 concrete diagnostic event types, sends periodic `Heartbeat` messages, and writes a best-effort `SessionEnd` on disconnect.
- **Hosted service lifecycle** - `LivePublisherHostedService` wraps the publisher as an `IHostedService` so the TCP listener starts/stops with the MAUI app host.
- **DI registration** - `NavigationDebuggerExtensions.UseNavigationDebugger()` conditionally registers `LiveNavigationPublisher` and the hosted service when `EnableLiveStreaming = true`.
- **Wire protocol** - `LiveEventEnvelope`, `LiveEventPayload`, `LiveMessageType` are defined in both the debugger library (`net9/10`) and the VSIX Desktop project (`net48`-compatible copy). `LiveEventSerializer` maps all 12 event types to payloads.
- **Source-generated JSON context** - `LiveEventJsonContext` in the debugger; reflection-based fallback in VSIX Desktop (net48 cannot use source-gen `Default` context).
- **VSIX TCP client** - `LiveSessionClient` connects, reads NDJSON, reconnects automatically after disconnect, and raises `EnvelopeReceived`, `Connected`, `Disconnected` events. Disconnect is now fully safe: the active TCP socket is forcibly closed in `StopAsync` to unblock the .NET 4.8 `ReadLineAsync` call, eliminating the Visual Studio crash/restart on Disconnect.
- **VSIX session manager** - `LiveSessionManager` / `ILiveSessionManager` orchestrate connect/disconnect/pause/resume, route envelopes to `ILiveSessionSink`, track active flows and overlays, build `NavigationOperationRecord` and `NavigationReplayFrame` instances, and dispatch on the UI thread. Exposes `GetCurrentSession()` and `ResetSession()` for export/clear support. Re-entrant disconnect is guarded; all sink dispatch calls are exception-safe.
- **Automatic Android emulator tunnel** - `AdbTunnelService` / `IAdbTunnelService` automatically locate `adb.exe` (via `ANDROID_HOME`, default SDK location, or `PATH`), detect running emulator instances via `adb devices`, check for an existing port-forward with `adb forward --list`, and create it only when missing. Called automatically by `LiveSessionManager.ConnectAsync` when the host is loopback. Status messages are surfaced in the UI. No manual `adb forward` command required.
- **Pause / resume with buffering** - envelopes received while paused are buffered in a `ConcurrentQueue` and flushed in order on resume.
- **Session `EndedAt` stamping** - `LiveSessionManager` sets `_liveSession.EndedAt = DateTimeOffset.UtcNow` on both `SessionEnd` envelope receipt and explicit `DisconnectAsync`.
- **InspectorShellViewModel live commands** - `ConnectLiveCommand`, `DisconnectLiveCommand`, `PauseLiveCommand`, `ResumeLiveCommand`, `ExportLiveSessionCommand`, `ClearLiveSessionCommand` are wired up with correct `CanExecute` guards. `BeginLiveSession`, `AppendLiveFrame`, `EndLiveSession` (`ILiveSessionSink`) are implemented. `BeginLiveSession` and `EndLiveSession` both call `RaiseLiveCommandsChanged()` immediately after `IsLive` changes, so WPF re-queries every live/export/clear command's `CanExecute` the moment a live session starts or ends.
- **Export live session (JSON + Markdown)** - `ExportLiveSessionCommand` (JSON) and `ExportLiveSessionMarkdownCommand` (Markdown) share a format-aware `ExportLiveSessionAsync(DiagnosticsExportFormat, CancellationToken)` helper that calls `INavigationDiagnosticsExporter.ExportToFileAsync` after obtaining a save path from `IFileSaveDialogService`; both commands are enabled whenever a live session is active. `WpfFileSaveDialogService` (WPF `SaveFileDialog`) is registered in `InspectorFactory.CreateShellView`. `InspectorFactory` instantiates the exporter under `#if WINDOWS` (not `WINDOWS && !NETFRAMEWORK`), and a net48-compatible `Net48Exporter` implementation covers the actual net48-windows VSIX runtime host, so the exporter is never null at runtime.
- **Clear live session** - `ClearLiveSessionCommand` calls `LiveSessionManager.ResetSession()`, clears the timeline/warnings, and resets all live panel state.
- **TimelinePanelViewModel live mode** - `BeginLiveMode()`, `AppendLiveFrame()`, `EndLiveMode()` appended to the existing offline timeline; auto-selects the latest frame.
- **WarningViewerPanelViewModel live mode** - `BeginLiveMode()`, `AppendLiveWarning()` implemented.
- **StackViewerPanelViewModel live mode** - `ShowLiveFrame()` shows current-route stack without a diff.
- **InspectorFactory** - `CreateShell(enableLive: true)` and `CreateShellView()` wire a `LiveSessionManager` into the shell by default.
- **Shell UI** - `NavigationInspectorShellView.xaml` has a live toolbar row with Host/Port text boxes, Connect/Disconnect/Pause/Resume/Export/Clear buttons, a live status `TextBlock`, an orange **Reconnecting** badge (visible only when `LiveConnectionState == Reconnecting`), and a red **LIVE** badge (visible only when `LiveConnectionState == Connected`).
- **`EqualityToVisibilityConverter`** - new WPF converter compares the bound value's string representation to `ConverterParameter`; excluded from non-WPF TFMs via the project `<Compile Remove>` condition.
- **Sample app live config** - `samples/.../MauiProgram.cs` sets `opts.EnableLiveStreaming = true`.
- **Live unit tests** - `LiveEventSerializerTests` (all 12 event types), `LiveSessionManagerTests` (envelope routing, pause/resume, frame construction), `InspectorShellViewModelLiveTests` (commands and sink methods), `TimelinePanelViewModelLiveTests` (12 tests for live-mode timeline behavior), `AdbTunnelServiceTests` (8 tests covering all tunnel scenarios), `LiveDisconnectTests` (9 tests covering all disconnect lifecycle states), `LiveNavigationPublisherTests` (10 tests), `LiveSessionClientTests` (11 tests), `WarningViewerPanelViewModelLiveTests` (13 tests), `LiveIntegrationTests` (10-test end-to-end TCP round-trip).
- **Build** - all projects build cleanly against `net48-windows`, `net9.0`, `net9.0-windows`, `net10.0`, and `net10.0-android`. Zero errors, zero regressions.
- **Architecture document** - `docs/debugger/live-monitoring.md` exists.

### What is partially implemented

- **Stack snapshot in live frames** - `LiveSessionManager` builds a `NavigationStackSnapshot` with only `CurrentRoute` and `CurrentFlow`. The full navigation stack is not transmitted from the MAUI side, so `StackBefore` is always empty and `StackAfter` contains at most one entry.
- **`FrameInspectorPanelViewModel` live mode** - `Inspect(frame)` is called from `AppendLiveFrame` but there are no live-specific property labels or additional context.

### What is not implemented

- **`adb reverse` documentation in the UI** - the XAML `ToolTip` mentions it; automatic forwarding is now implemented, but a rich guidance panel with step-by-step instructions for physical devices is not yet built.
- **Documentation updates** - `docs/vsix/overview.md`, `docs/vsix/features.md`, `debugger/ROADMAP.md`, and `vsix/vsix-roadmap.md` still describe live monitoring as deferred/not implemented.
- **VSIX command/menu integration** - `NavigationInspectorPackage.cs` has no "Connect Live" VS menu item; live actions are only accessible from inside the tool window toolbar.

### Current blockers

- ~~**`IFileSaveDialogService` not implemented in host**~~ - resolved: `WpfFileSaveDialogService` is registered via `InspectorFactory.CreateShellView`, and `Net48Exporter` guarantees the exporter is non-null on the net48-windows VSIX host, so both `ExportLiveSessionCommand` (JSON) and `ExportLiveSessionMarkdownCommand` (Markdown) work end-to-end.
- ~~**No end-to-end test**~~ - resolved: `LiveIntegrationTests` (10 tests) validates the full loopback TCP path.
- ~~**Disconnect causes Visual Studio restart**~~ - resolved: `LiveSessionClient.StopAsync` now forcibly closes the active socket before awaiting the read task; `LiveSessionManager.DisconnectAsync` is re-entrancy-guarded and all sink dispatches are exception-safe.
- ~~**Manual adb forward required**~~ - resolved: `AdbTunnelService` automatically ensures the port-forward when the host is loopback.
- ~~**Export JSON / Export MD buttons stayed disabled during an active live session**~~ - resolved in two parts:
  1. `BeginLiveSession`/`EndLiveSession` call `RaiseLiveCommandsChanged()` so WPF re-queries `CanExecute` the moment `IsLive` flips (notification fix).
  2. **Actual root cause**: `InspectorFactory.CreateShell` guarded the exporter with `#if WINDOWS` alone. The `WINDOWS` preprocessor symbol is only emitted by the SDK for .NET 5+ OS-specific TFMs (e.g. `net9.0-windows`) — it is **not** defined for `net48-windows`, which is the actual VS/VSIX host TFM. A real `net48-windows` build confirms the compiler only receives `/define:TRACE;DEBUG;NETFRAMEWORK;NET48;...OR_GREATER` (no `WINDOWS`). This left `_exporter` permanently `null` in the real host, so `IsLive && _exporter is not null && _saveDialog is not null` was always `false` — even after the notification fix — while `ClearLiveSessionCommand` (which only checks `IsLive`) stayed enabled, exactly matching the reported symptom. Fixed by changing the guard to `#if WINDOWS || NETFRAMEWORK`, matching the convention already used elsewhere in the same file (`WpfFileSaveDialogService.Instance` selection) and in `WpfFileSaveDialogService.cs` itself.

---

## Feature Breakdown

### Runtime Publisher

| Feature | Status | Completion | Notes |
|---|---|---|---|
| `LiveNavigationPublisher` class | Implemented | 100% | `StartAsync`, `DisposeAsync`, accept loop, send loop, heartbeat loop |
| Bus subscription | Implemented | 100% | `BusReadLoopAsync` subscribes to `INavigationDiagnosticsBus.Subscribe()` |
| `LiveEventSerializer` | Implemented | 100% | Maps all 12 event types |
| Heartbeat | Implemented | 100% | Configurable via `LiveHeartbeatIntervalMs` (default 5 000 ms) |
| Backpressure / bounded queue | Implemented | 100% | `Channel.CreateBounded` with `DropOldest`, capacity from `LiveSendQueueCapacity` (default 512) |
| One-client-at-a-time accept model | Implemented | 100% | Accept loop creates a fresh queue per connection |
| `SessionStart` metadata | Implemented | 100% | App version, platform, OS version forwarded on connect |
| `SessionEnd` on disconnect | Implemented | 100% | Best-effort send in `HandleClientAsync` finally block |
| Platform/OS guards | Implemented | 100% | `#if ANDROID` guards `Android.OS.Build`; other platforms use `RuntimeInformation.OSDescription` |
| Hosted service wrapper | Implemented | 100% | `LivePublisherHostedService` implements `IHostedService` |
| DI registration | Implemented | 100% | Registered in `UseNavigationDebugger()` when `EnableLiveStreaming = true` |
| `EnableLiveStreaming` option | Implemented | 100% | Default `false` (opt-in); set to `true` in sample app |
| Full stack state per event | Implemented | 100% | `stackNav` + `stackModal` transmitted in every completion envelope; publisher already wired to `INavigationStackInspector` |

### Transport Layer

| Feature | Status | Completion | Notes |
|---|---|---|---|
| TCP server (`TcpListener`) | Implemented | 100% | Binds to `IPAddress.Loopback` on `LivePort` (default 17382) |
| TCP client (`TcpClient`) | Implemented | 100% | `LiveSessionClient` with configurable reconnect delay (default 2 s) |
| NDJSON protocol | Implemented | 100% | One JSON object per line; source-gen on net9/10, reflection on net48 |
| `LiveEventEnvelope` wire types | Implemented | 100% | Duplicated into VSIX Desktop namespace for net48 compatibility |
| `LiveEventJsonContext` | Implemented | 100% | Debugger: source-gen. VSIX Desktop: partial class with reflection fallback |
| Reconnect on disconnect | Implemented | 100% | `ConnectLoopAsync` loop with `Task.Delay(_reconnectDelay, ct)` |
| Cancellation propagation | Implemented | 100% | `CancellationTokenSource` threaded through all loops |
| Session lifecycle messages | Implemented | 100% | `SessionStart`, `NavEvent`, `Heartbeat`, `SessionEnd` |

### VSIX Integration

| Feature | Status | Completion | Notes |
|---|---|---|---|
| `LiveSessionClient` | Implemented | 100% | net48-compatible (no `CancellationToken` overload on `ConnectAsync`, `ReadLineAsync`); `StopAsync` now forcibly closes the socket to unblock `ReadLineAsync` — fixes VS crash on Disconnect |
| `ILiveSessionManager` / `LiveSessionManager` | Implemented | 100% | Connect, Pause, Resume, Disconnect, StateChanged event, `GetCurrentSession()`, `ResetSession()`; re-entrancy guard and exception-safe sink dispatch added |
| `IAdbTunnelService` / `AdbTunnelService` | Implemented | 100% | Locates adb, detects emulators, checks/creates port-forward; called automatically by `ConnectAsync` when host is loopback; no manual `adb forward` required |
| `ILiveSessionSink` | Implemented | 100% | `BeginLiveSession`, `AppendLiveFrame`, `EndLiveSession` |
| `InspectorShellViewModel` live state | Implemented | 100% | `IsLive`, `LiveHost`, `LivePort`, `LiveStatus`, `LiveConnectionState` |
| `InspectorShellViewModel` live commands | Implemented | 100% | Connect, Disconnect, Pause, Resume, Export, Clear with CanExecute guards |
| `TimelinePanelViewModel` live mode | Implemented | 100% | `BeginLiveMode`, `AppendLiveFrame`, `EndLiveMode` |
| `WarningViewerPanelViewModel` live mode | Implemented | 100% | `BeginLiveMode`, `AppendLiveWarning` |
| `StackViewerPanelViewModel` live mode | Implemented | 100% | `ShowLiveFrame` populates `StackAfter` (nav stack) and `ModalAfter` (modal stack); `Clear` resets both |
| `FrameInspectorPanelViewModel` live mode | In Progress | 60% | `Inspect(frame)` called from `AppendLiveFrame`; no live-specific properties or label changes |
| `InspectorFactory` live wiring | Implemented | 100% | `enableLive: true` by default in both `CreateShell` and `CreateShellView` |
| Pause buffer flush on resume | Implemented | 100% | `ConcurrentQueue` drained in `ResumeAsync` |
| Reconnecting state handling | Implemented | 100% | State transitions to `Reconnecting` and orange badge shown in toolbar |
| Export live session | Implemented | 100% | Two commands (`ExportLiveSessionCommand` JSON, `ExportLiveSessionMarkdownCommand` Markdown) share `ExportLiveSessionAsync(format, ct)`; `WpfFileSaveDialogService` and `Net48Exporter`/`NavigationDiagnosticsExporter` wired end-to-end on net48-windows and net9.0-windows |
| Clear live session | Implemented | 100% | Command calls `ResetSession()` and resets all panel state |
| Session `EndedAt` stamping | Implemented | 100% | Set in both `SessionEnd` handler and `DisconnectAsync` |

### UI

| Feature | Status | Completion | Notes |
|---|---|---|---|
| Connect button | Implemented | 100% | Bound to `ConnectLiveCommand`; disabled when not disconnected |
| Disconnect button | Implemented | 100% | Bound to `DisconnectLiveCommand`; disabled when already disconnected |
| Pause button | Implemented | 100% | Bound to `PauseLiveCommand`; enabled only when `Connected` |
| Resume button | Implemented | 100% | Bound to `ResumeLiveCommand`; enabled only when `Paused` |
| Export JSON button | Implemented | 100% | Bound to `ExportLiveSessionCommand`; visible only when `IsLive` |
| Export Markdown button | Implemented | 100% | Bound to `ExportLiveSessionMarkdownCommand`; visible only when `IsLive` |
| Clear button | Implemented | 100% | Bound to `ClearLiveSessionCommand`; visible only when `IsLive` |
| Host text box | Implemented | 100% | Bound to `LiveHost` with `UpdateSourceTrigger=PropertyChanged` |
| Port text box | Implemented | 100% | Bound to `LivePort` with `UpdateSourceTrigger=PropertyChanged` |
| Live status indicator | Implemented | 100% | TextBlock bound to `LiveStatus` |
| Reconnecting visual indicator | Implemented | 100% | Orange badge; visible when `LiveConnectionState == Reconnecting` via `EqualityToVisibilityConverter` |
| Live mode indicator badge | Implemented | 100% | Red LIVE badge; visible when `LiveConnectionState == Connected` via `EqualityToVisibilityConverter` |
| `adb reverse` guidance panel | Not Started | 0% | Only a ToolTip hint on the Host TextBox |

### Session Management

| Feature | Status | Completion | Notes |
|---|---|---|---|
| `SessionStart` -> `NavigationDiagnosticsSession` | Implemented | 100% | Session ID, AppVersion, Platform, OsVersion forwarded |
| `SessionEnd` -> `EndLiveSession` | Implemented | 100% | Dispatched to UI thread via `IUiDispatcher` |
| Flow tracking | Implemented | 100% | `_activeFlow` updated on `FlowEnteredEvent` / `FlowExitedEvent` |
| Overlay tracking | Implemented | 100% | `_activeOverlays` updated on `OverlayShownEvent` / `OverlayHiddenEvent` |
| Pending-start correlation | Implemented | 100% | `_pendingStarts` dict correlates `NavigationStartedEvent` with completion event |
| `NavigationOperationRecord` construction | Implemented | 100% | Route, PageTypeName, PresentationMode, StackBehavior, ParameterKeys, ElapsedMs merged from start+completion payloads |
| `NavigationReplayFrame` construction | Implemented | 100% | OperationIndex, StackSnapshot (route only), FlowName, ActiveOverlays, Warnings |
| Full stack snapshot per frame | Implemented | 100% | `StackSnapshot.NavigationStack` and `ModalStack` fully populated from live payload; `ModalAfter` displayed in stack panel |
| Export live session | Implemented | 100% | JSON and Markdown formats both wired end-to-end via `Net48Exporter` (net48-windows) / `NavigationDiagnosticsExporter` (net9/10) and `WpfFileSaveDialogService` |
| Session duration / `EndedAt` | Implemented | 100% | Set on `SessionEnd` and `DisconnectAsync` |
| Reset session | Implemented | 100% | `ResetSession()` clears all accumulated state; `ClearLiveSessionCommand` calls it |

### Testing

| Feature | Status | Completion | Notes |
|---|---|---|---|
| `LiveEventSerializer` unit tests | Implemented | 100% | All 12 event type roundtrips covered |
| `LiveNavigationPublisher` unit tests | Implemented | 100% | 10 tests: StartAsync/IsRunning, bus-event forwarding to connected client, session start/end on connect/disconnect, second-client-after-disconnect, heartbeat interval, queue backpressure |
| `LiveSessionClient` unit tests | Implemented | 100% | 11 tests: StartAsync idempotency, connect when server available/unavailable, reconnect after drop, StopAsync stops read loop cleanly, malformed-line handling, NDJSON parsing, DisposeAsync safety |
| `LiveSessionManager` unit tests | Implemented | 100% | Envelope routing, pause/resume buffer, frame construction, state transitions, push/pop/modal/root-reset/guard-redirect stack scenarios |
| `InspectorShellViewModel` live mode tests | Implemented | 100% | Commands, CanExecute guards, sink methods |
| `TimelinePanelViewModel` live mode tests | Implemented | 100% | 12 tests: BeginLiveMode, AppendLiveFrame, EndLiveMode, auto-select, counter resets |
| `StackViewerPanelViewModel` live mode tests | Implemented | 100% | 6 tests: ShowLiveFrame nav stack, modal stack, empty modal, overwrite, null guard, Clear reset |
| `WarningViewerPanelViewModel` live tests | Implemented | 100% | 13 tests: BeginLiveMode (clear/idempotent/property-changed), AppendLiveWarning (accumulate/session-warnings flag/null guard/frame-warnings isolation), Clear reset, property-changed notifications |
| `ExportLiveSessionCommandTests` | Implemented | 100% | 16 tests: CanExecute guards (not live, exporter missing, save dialog missing), `CanExecuteChanged` notification regression tests (fires after `BeginLiveSession`/`EndLiveSession` for both export commands), JSON/Markdown execute paths via `FakeDiagnosticsExporter`/`FakeFileSaveDialogService`, file name/filter selection, cancelled save dialog |
| `AdbTunnelServiceTests` | Implemented | 100% | 8 tests: forward missing → auto-create, forward exists, adb unavailable, no emulators, devices cmd fails, create fails, idempotent multi-call, cancellation |
| `LiveDisconnectTests` | Implemented | 100% | 9 tests: disconnect when disconnected, double-concurrent disconnect, disconnect after failed connect, connect→disconnect→connect, repeated cycles (5×), disconnect while connecting, EndLiveSession on sink, EndedAt stamped, DisposeAsync safe |
| End-to-end TCP round-trip test | Implemented | 100% | 10-test suite (`LiveIntegrationTests`) validates full loopback path: SessionStart, NavSucceeded, BackNav, GuardRedirect, Overlay, RuntimeWarning, multi-nav ordering |
| `InspectorFactory` TFM-guard regression test | Implemented | 100% | Source-invariant test (`InspectorFactory_ExporterGuard_IncludesNetFramework`) reads `InspectorFactory.cs` from disk and asserts the exporter-instantiation `#if` guard contains `NETFRAMEWORK`; guards against a regression to bare `#if WINDOWS`, which the `net9.0` test process can never exercise directly since it can't compile/run the `net48-windows` branch |
| Full test suite | Tested | 100% | **208 / 208 Vsix.Tests pass** (all existing tests continue to pass, plus the new TFM-guard regression test), 0 regressions |

### Documentation

| Feature | Status | Completion | Notes |
|---|---|---|---|
| `docs/debugger/live-monitoring.md` | Implemented | 100% | Architecture document with transport rationale, diagrams, wire protocol |
| `docs/debugger/live-monitoring-status.md` | Implemented | 100% | This document |
| `debugger/ROADMAP.md` | Not Started | 0% | Still describes live monitoring as future work |
| `vsix/vsix-roadmap.md` | Not Started | 0% | Still marks live attach as deferred |
| `vsix/vsix-desktop-roadmap.md` | Not Started | 0% | No live monitoring section |
| `docs/vsix/overview.md` | Not Started | 0% | Not updated |
| `docs/vsix/features.md` | Not Started | 0% | Not updated |
| Sample app `MauiProgram.cs` live config | Implemented | 100% | `opts.EnableLiveStreaming = true` set |

---

## Files Created

| File | Purpose |
|---|---|
| `docs/debugger/live-monitoring.md` | Architecture and design document |
| `docs/debugger/live-monitoring-status.md` | This status tracker |
| `debugger/.../Live/LiveEventEnvelope.cs` | Wire protocol types (net9/10 only) |
| `debugger/.../Live/LiveEventJsonContext.cs` | Source-gen JSON context (net9/10 only) |
| `debugger/.../Live/LiveEventSerializer.cs` | Event-to-envelope mapping (net9/10 only) |
| `debugger/.../Live/LiveNavigationPublisher.cs` | TCP server / bus subscriber (net9/10 only) |
| `debugger/.../Live/LivePublisherHostedService.cs` | `IHostedService` lifecycle wrapper (net9/10 only) |
| `vsix/.../Live/ILiveSessionManager.cs` | `LiveConnectionState` enum + manager interface |
| `vsix/.../Live/LiveEventEnvelope.cs` | Wire protocol types (net48-compatible copy) |
| `vsix/.../Live/LiveEventJsonContext.cs` | JSON context (net48-compatible partial class) |
| `vsix/.../Live/LiveSessionClient.cs` | TCP client with reconnect loop |
| `vsix/.../Live/LiveSessionManager.cs` | Session orchestration, envelope routing, frame construction, export/reset |
| `vsix/.../Infrastructure/IFileSaveDialogService.cs` | Save-dialog abstraction for ViewModel-layer export |
| `vsix/.../Infrastructure/EqualityToVisibilityConverter.cs` | WPF converter: visible when value equals parameter (windows TFMs only) |
| `vsix/.../Live/IAdbTunnelService.cs` | `IAdbTunnelService` interface + `AdbTunnelResult` DTO |
| `vsix/.../Live/AdbTunnelService.cs` | Production adb tunnel implementation with `IAdbProcessRunner` abstraction |
| `debugger/.../Export/Net48Exporter.cs` | net48-compatible `INavigationDiagnosticsExporter` implementation (reflection-based JSON serialization + Markdown report) used by the net48-windows VSIX host |
| `tests/.../Live/AdbTunnelServiceTests.cs` | 8-test unit suite for `AdbTunnelService` with `FakeAdbRunner` test double |
| `tests/.../Live/ExportLiveSessionCommandTests.cs` | 16-test suite for `ExportLiveSessionCommand` / `ExportLiveSessionMarkdownCommand`: CanExecute guards, `CanExecuteChanged` notification regression tests, and format-aware execute paths, using `FakeDiagnosticsExporter` / `FakeFileSaveDialogService` test doubles |
| `tests/.../Live/LiveDisconnectTests.cs` | 9-test disconnect lifecycle suite |
| `tests/.../Live/LiveIntegrationHarness.cs` | Real-TCP integration harness: allocates free port, starts publisher, connects manager, exposes shell/bus for assertions |
| `tests/.../Live/LiveIntegrationTests.cs` | 10-test end-to-end suite: SessionStart, NavSucceeded, BackNav, GuardRedirect, Overlay, RuntimeWarning, multi-nav ordering |
| `tests/.../Live/LiveEventSerializerTests.cs`
| `tests/.../Live/LiveSessionManagerTests.cs` | Envelope routing, pause/resume, frame construction tests |
| `tests/.../Live/InspectorShellViewModelLiveTests.cs` | Live command and sink method tests |
| `tests/.../Live/TimelinePanelViewModelLiveTests.cs` | 12 timeline live-mode behavior tests |

---

## Files Modified

| File | Changes |
|---|---|
| `debugger/.../Options/NavigationDebuggerOptions.cs` | Added `EnableLiveStreaming`, `LivePort`, `LiveHeartbeatIntervalMs`, `LiveSendQueueCapacity` |
| `debugger/.../Extensions/NavigationDebuggerExtensions.cs` | Added live publisher DI registration; added `GetPlatformString()`, `GetOsVersionString()` with `#if ANDROID` guard |
| `debugger/.../Shaunebu.MAUI.Navigation.Debugger.csproj` | Added `<Compile Remove="Live\**\*.cs" />` for `net48` TFM |
| `vsix/.../Infrastructure/InspectorFactory.cs` | Added `enableLive` parameter; wires `LiveSessionManager` into `InspectorShellViewModel`; exporter now created under `#if WINDOWS` (was `WINDOWS && !NETFRAMEWORK`) so the net48-windows VSIX host receives a non-null exporter |
| `vsix/.../ViewModels/InspectorShellViewModel.cs` | Added `ILiveSessionSink`, live state properties, seven live commands (added `ExportLiveSessionMarkdownCommand`), export/clear logic, disposal; export refactored into a shared `ExportLiveSessionAsync(DiagnosticsExportFormat format, CancellationToken ct)` helper that selects file extension/filter per format |
| `vsix/.../ToolWindows/NavigationInspectorShellView.xaml` | Added a second live-toolbar button (“📄 Export MD”) bound to `ExportLiveSessionMarkdownCommand`; inserted a new grid column and renumbered the Clear/status columns |
| `debugger/.../Shaunebu.MAUI.Navigation.Debugger.csproj` | net48 TFM keeps `Inspection/NavigationSessionMetrics.cs` compiled (needed by `Net48Exporter`) while still excluding `DiagnosticsJsonContext.cs`/`NavigationDiagnosticsExporter.cs`; non-net48 TFMs exclude `Net48Exporter.cs`/`Net48Importer.cs` |
| `vsix/.../ViewModels/TimelinePanelViewModel.cs` | Added `IsLive`, `BeginLiveMode()`, `AppendLiveFrame()`, `EndLiveMode()` |
| `vsix/.../ViewModels/WarningViewerPanelViewModel.cs` | Added `BeginLiveMode()`, `AppendLiveWarning()` |
| `vsix/.../ViewModels/StackViewerPanelViewModel.cs` | Added `ShowLiveFrame()`, `ModalAfter` property; `Clear()` now resets `ModalAfter` |
| `vsix/.../ToolWindows/NavigationInspectorShellView.xaml` | Live toolbar: Host/Port inputs, six command buttons, status text, Reconnecting badge, LIVE badge; `EqualToVis` converter registered |
| `vsix/.../ToolWindows/Panels/StackViewerPanel.xaml` | Added MODAL STACK section in no-diff (live) area; bound to `ModalAfter`, hidden when empty via `CountToVis` |
| `tests/.../Live/LiveSessionManagerTests.cs` | Added push, pop, modal-present, modal-dismiss, root-reset, guard-redirect, and multi-frame stack scenario tests; added `BackNavSucceededEnvelope` and `GuardRedirectedEnvelope` builders |
| `tests/.../StackViewerPanelViewModelTests.cs` | Added 6 `ShowLiveFrame` live-mode tests; updated `Clear_ResetsAll` to assert `ModalAfter` |
| `vsix/.../Shaunebu.MAUI.Navigation.Vsix.Desktop.csproj` | Added `EqualityToVisibilityConverter.cs` to non-WPF `<Compile Remove>` group |
| `vsix/.../Live/LiveSessionClient.cs` | Added `_currentTcp` volatile field and `_stopLock`; `StopAsync` now closes socket before awaiting read task; `ConnectLoopAsync` assigns/clears `_currentTcp` under lock; `ObjectDisposedException` handled cleanly |
| `vsix/.../Live/LiveSessionManager.cs` | Added `_disconnecting` re-entrancy guard; `DisconnectAsync` swallows `StopAsync`/`DisposeAsync` errors; all three sink dispatch sites wrapped in `try/catch`; `OnClientDisconnected` ignores spurious events post-disconnect; integrated `IAdbTunnelService` pre-connect call |
| `tests/.../Live/LiveSessionManagerTests.cs` | Updated `CreateManager` to pass `NoOpAdbTunnelService`; added `NoOpAdbTunnelService` test double |
| `tests/.../Live/LiveIntegrationHarness.cs` | Updated `CreateAndConnectCoreAsync` to pass `NoOpAdbTunnelService` |
| `samples/.../MauiProgram.cs` | Set `opts.EnableLiveStreaming = true` |
| `vsix/.../ViewModels/InspectorShellViewModel.cs` | `BeginLiveSession` and `EndLiveSession` now call `RaiseLiveCommandsChanged()` after updating `IsLive`, fixing a regression where the Export JSON/MD and Clear buttons stayed disabled during an active live session because WPF never re-queried `CanExecute` |
| `tests/.../Live/ExportLiveSessionCommandTests.cs` | Added 4 `CanExecuteChanged` regression tests (`ExportLiveSessionCommand`/`ExportLiveSessionMarkdownCommand` x `BeginLiveSession`/`EndLiveSession`) |
| `tests/.../Export/ExportLiveSessionTests.cs` | Corrected a misleading comment about the `WINDOWS` TFM symbol; added `InspectorFactory_ExporterGuard_IncludesNetFramework`, a source-invariant regression test that reads `InspectorFactory.cs` from disk (via `[CallerFilePath]`-based repo-root resolution) and asserts the exporter-instantiation `#if` guard contains `NETFRAMEWORK`, protecting the composition root from a silent regression to bare `#if WINDOWS` |

---

## Build Status

| Project | Build Status |
|---|---|
| `Shaunebu.MAUI.Navigation.Debugger` (`net9.0`, `net10.0`, `net48`) | Passing |
| `Shaunebu.MAUI.Navigation.Vsix.Desktop` (`net48-windows`, `net9.0`, `net9.0-windows`) | Passing |
| `Shaunebu.MAUI.Navigation.Vsix` (`net48-windows`) | Passing |
| `Shaunebu.MAUI.Navigation.Sample` (`net10.0-windows`) | Passing |
| `Shaunebu.MAUI.Navigation.Debugger.Tests` | Passing |
| `Shaunebu.MAUI.Navigation.Vsix.Tests` | Passing |

---

## Test Status

| Project | Passed | Failed | Total |
|---|---|---|---|
| `Shaunebu.MAUI.Navigation.Tests` | 547 | 0 | 547 |
| `Shaunebu.MAUI.Navigation.Generators.Tests` | 54 | 0 | 54 |
| `Shaunebu.MAUI.Navigation.Analyzers.Tests` | 40 | 0 | 40 |
| `Shaunebu.MAUI.Navigation.Debugger.Tests` | 367 | 0 | 367 |
| `Shaunebu.MAUI.Navigation.Vsix.Tests` | 208 | 0 | 208 |
| **Total** | **1 216** | **0** | **1 216** |

> 66 Selenium/WebDriver UI automation tests are excluded from this count — they require a running device/browser and fail in CI with `WebDriver.UnpackAndThrowOnError` timeouts.

Live monitoring now has comprehensive coverage: serializer, session manager (all stack-sync scenarios), stack-viewer live mode, shell ViewModel, timeline panel, runtime publisher, session client, warning-viewer live mode, **and full end-to-end TCP round-trip** (`LiveIntegrationTests`). A source-invariant regression test now also guards the `InspectorFactory` exporter TFM branch responsible for the export-button regression, since the `net9.0` test process can never execute the `net48-windows` code path directly. Only `FrameInspectorPanelViewModel` live-specific behavior remains without dedicated tests.

---

## Remaining Work

### Critical

| Task | Effort | Dependency | Blocking |
|---|---|---|---|
| ~~`IFileSaveDialogService` host implementation (WPF `SaveFileDialog`)~~ | ~~Trivial~~ | ~~Done~~ | ~~No~~ |
| ~~End-to-end integration test (loopback TCP publisher -> VSIX client -> ViewModel)~~ | ~~Large (2-3 d)~~ | ~~Done~~ | ~~No~~ |
| ~~Disconnect causes Visual Studio restart~~ | ~~Done~~ | ~~Done~~ | ~~No~~ |
| ~~Automatic `adb forward` for Android emulator~~ | ~~Done~~ | ~~Done~~ | ~~No~~ |

### High

| Task | Effort | Dependency | Blocking |
|---|---|---|---|
| ~~Transmit full navigation stack with each `NavEvent` (publisher side)~~ | ~~Medium (1 d)~~ | ~~Done~~ | ~~No~~ |
| ~~Receive and apply full stack snapshot in `LiveSessionManager`~~ | ~~Small (0.5 d)~~ | ~~Done~~ | ~~No~~ |
| ~~Unit tests for `LiveNavigationPublisher`~~ | ~~Small (0.5 d)~~ | ~~Done~~ | ~~No~~ |
| ~~Unit tests for `LiveSessionClient`~~ | ~~Small (0.5 d)~~ | ~~Done~~ | ~~No~~ |
| ~~Unit tests for `WarningViewerPanelViewModel` live mode~~ | ~~Small (0.5 d)~~ | ~~Done~~ | ~~No~~ |

### Medium

| Task | Effort | Dependency | Blocking |
|---|---|---|---|
| Update `debugger/ROADMAP.md` to mark live monitoring complete | Trivial | None | No |
| Update `vsix/vsix-roadmap.md` live attach section | Trivial | None | No |
| Update `vsix/vsix-desktop-roadmap.md` | Trivial | None | No |
| Update `docs/vsix/overview.md` and `docs/vsix/features.md` | Small (0.5 d) | None | No |
| `FrameInspectorPanelViewModel` live-specific label/context | Small (0.5 d) | None | No |

### Low

| Task | Effort | Dependency | Blocking |
|---|---|---|---|
| `adb reverse` in-product guidance panel | Small (1 d) | None | No |
| VS command/menu item for "Connect Live" (in `NavigationInspectorPackage.cs`) | Medium (1 d) | None | No |
| Source-gen `LiveEventJsonContext` for net48 VSIX Desktop (requires NuGet upgrade) | Large (unknown) | net48 source-gen constraints | No |
| Structured logging for VSIX live client (`ILogger` integration) | Small (0.5 d) | None | No |

---

## Risks

| Risk | Severity | Details |
|---|---|---|
| ~~**`IFileSaveDialogService` not implemented in host**~~ | ~~Resolved~~ | `WpfFileSaveDialogService` is registered via `InspectorFactory.CreateShellView`; `Net48Exporter` guarantees a non-null exporter on net48-windows. Both JSON (`ExportLiveSessionCommand`) and Markdown (`ExportLiveSessionMarkdownCommand`) export are validated end-to-end with 16 dedicated tests. |
| ~~**Export/Clear buttons stayed disabled during an active live session**~~ | ~~Resolved~~ | `BeginLiveSession`/`EndLiveSession` now call `RaiseLiveCommandsChanged()` immediately after `IsLive` changes, so WPF's cached `CanExecute` result is invalidated the moment a live session starts or ends. 4 regression tests added. |
| ~~**Disconnect causes VS crash/restart**~~ | ~~Resolved~~
| ~~**Manual adb forward required**~~ | ~~Resolved~~ | `AdbTunnelService` automatically locates adb, detects the emulator, and creates the port-forward when connecting to a loopback host. No user action required. |
| **Stack viewer limited in live mode** | Resolved | `NavigationStackSnapshot.NavigationStack` and `ModalStack` are now fully populated from `stackNav`/`stackModal` payload fields. `ModalAfter` displayed in the stack panel. |
| **No end-to-end test** | Resolved | `LiveIntegrationTests` (10 tests) validates the full loopback TCP path: SessionStart → NavSucceeded → BackNav → GuardRedirect → Overlay → RuntimeWarning → multi-nav ordering. |
| **net48 reflection-based JSON** | Medium | `LiveSessionClient` uses `JsonSerializer.Deserialize<T>(string)` on the net48 path. This works but will not benefit from source-gen performance optimizations and is sensitive to trimming. |
| **One-client-at-a-time model** | Medium | `LiveNavigationPublisher` accepts exactly one VSIX client at a time. A second VS instance opening the inspector will silently not receive events until the first disconnects. |
| **Physical device (non-emulator) setup** | Low | `AdbTunnelService` only creates a forward when an emulator is detected. For physical devices the user still needs to run `adb forward` manually or use the device's Wi-Fi IP directly. |
| **`TcpListener` binds to loopback only** | Low | `IPAddress.Loopback` means the publisher cannot be reached from a remote machine. This is intentional for security but means iOS device testing is not currently supported. |

---

## Release Readiness

| Area | Ready | Reason |
|---|---|---|
| Runtime publisher | Yes | Unit tests complete (10 tests); stack transmission complete |
| VSIX transport + session manager | Yes | Disconnect lifecycle hardened; re-entrancy guard; exception-safe sink dispatch; adb tunnel integrated |
| VSIX ViewModels (live mode) | Mostly | Shell, timeline, stack-viewer, and warning-viewer live tests pass; `FrameInspectorPanelViewModel` live-specific tests absent |
| UI (live toolbar) | Yes | All six buttons, status text, Reconnecting badge, LIVE badge wired and compiling |
| Export live session | Yes | JSON and Markdown export both wired end-to-end (`WpfFileSaveDialogService` + `Net48Exporter`/`NavigationDiagnosticsExporter`); 16 dedicated command tests pass, including `CanExecuteChanged` notification regression coverage |
| Android emulator setup | Yes | `AdbTunnelService` automatically creates port-forward; no manual adb command required |
| Disconnect safety | Yes | VS crash/restart eliminated; 9-test suite covers all disconnect state transitions |
| Tests | Mostly | 1 216/1 216 unit+integration pass; only `FrameInspectorPanelViewModel` live-specific tests absent |
| Documentation | No | ROADMAP and feature docs not updated |
| Production Ready | Mostly | Only remaining blocker: minor roadmap/doc updates (`ROADMAP.md`, `vsix-roadmap.md`, feature docs) |

---

## Self-Update Requirement

This document must be updated before continuing any Live Monitoring implementation work. Whenever a live monitoring file is changed:

1. Update the **Last Updated** timestamp.
2. Update affected rows in the **Feature Breakdown** tables.
3. Update **Files Created** / **Files Modified** if new files are added or changed.
4. Update **Build Status** and **Test Status** with actual results.
5. Recalculate **Overall Completion** percentage.
6. Update **Remaining Work** to reflect tasks completed and any new tasks discovered.