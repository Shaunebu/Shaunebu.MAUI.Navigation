# Performance Counters

## Overview

`DebuggerPerformanceCounters` is a **DEBUG-only** static class that provides lock-free, low-overhead performance counters across all debugger subsystems. All writes use `Interlocked` and all reads are plain volatile reads — no locking required from any thread.

> **Important:** `DebuggerPerformanceCounters` is for test/diagnostic sampling only. It is **not** compiled in Release builds. Never write production logic against these counters.

---

## Counter Reference

### Bus

| Counter | Description |
|---|---|
| `BusPublishCount` | Total events published to the diagnostics bus |
| `BusFanOutWrites` | Total subscriber fan-out writes (publish × subscriber count) |

### Recorder

| Counter | Description |
|---|---|
| `RecorderEventsProcessed` | Events received and processed by the session recorder |
| `RecorderListEvictions` | Evictions from the bounded operation list (oldest-first) |

### Overlay Host

| Counter | Description |
|---|---|
| `OverlayEventsReceived` | Events received by the overlay host read loop |
| `OverlayStackThrottleScheduled` | Stack throttle callbacks scheduled |
| `OverlayStackThrottleFired` | Stack throttle callbacks that actually fired |
| `OverlayStackInspectorCalls` | `INavigationStackInspector.GetSnapshotAsync` calls by the overlay |
| `OverlayWarningEvaluations` | Warning engine `EvaluateAsync` calls made by the overlay |

### LiveEventListViewModel

| Counter | Description |
|---|---|
| `LiveEventPropertyChangedCount` | `PropertyChanged` events raised |
| `LiveEventArrayAllocations` | Array allocations inside `AddEvent` |

### StackPanelViewModel

| Counter | Description |
|---|---|
| `StackPanelPropertyChangedCount` | `PropertyChanged` events raised by `ApplySnapshot` |

---

## Usage

### Sampling counters in a test

```csharp
// Reset between test scenarios
DebuggerPerformanceCounters.Reset();

// Execute scenario
await _navigationHandler.GoToAsync<MainPage>();
await _navigationHandler.GoToAsync<DetailPage>();

// Assert
Assert.True(DebuggerPerformanceCounters.BusPublishCount >= 2);
Assert.Equal(2, DebuggerPerformanceCounters.RecorderEventsProcessed);
```

### Logging a snapshot

```csharp
#if DEBUG
Debug.WriteLine($"Bus publishes : {DebuggerPerformanceCounters.BusPublishCount}");
Debug.WriteLine($"Fan-out writes: {DebuggerPerformanceCounters.BusFanOutWrites}");
Debug.WriteLine($"Evictions     : {DebuggerPerformanceCounters.RecorderListEvictions}");
#endif
```

---

## Reset

Call `DebuggerPerformanceCounters.Reset()` to zero all counters between test runs or scenario boundaries:

```csharp
DebuggerPerformanceCounters.Reset();
```

---

## Related Pages

- [Debugger Overview](overview.md)
- [Session Recording](session-recording.md)
