# Diagnostics Overview

## Overview

`Shaunebu.MAUI.Navigation` exposes three structured diagnostic interfaces that the navigation pipeline calls at key lifecycle points. By default, the implementations log via `ILogger`. You can replace them with custom sinks — analytics services, crash reporters, or test harnesses — by registering your own implementations with the DI container.

---

## The Three Diagnostic Interfaces

| Interface | Events |
|---|---|
| `INavigationDiagnostics` | Forward navigation started, succeeded, failed, guard rejected, flow entered/exited |
| `IBackNavigationDiagnostics` | Back navigation requested, succeeded, failed, blocked by guard, blocked by overlay, handled by custom handler |
| `IOverlayDiagnostics` | Overlay showing, shown, hiding, hidden, operation failed |

---

## Enabling Diagnostics

```csharp
builder.UseShaunebuNavigation(options =>
{
	options.EnableDiagnostics = true;   // registers ILogger-backed implementations
});
```

When `EnableDiagnostics = false` (the default), no-op implementations are registered and the pipeline has zero overhead.

---

## Replacing With a Custom Sink

Register your custom implementation **after** `UseShaunebuNavigation`:

```csharp
builder.UseShaunebuNavigation(options =>
{
	options.EnableDiagnostics = true;
});

// Override with analytics-forwarding implementation
builder.Services.AddSingleton<INavigationDiagnostics, AnalyticsDiagnostics>();
```

Example analytics implementation:

```csharp
public sealed class AnalyticsDiagnostics : INavigationDiagnostics
{
	private readonly IAnalyticsService _analytics;

	public AnalyticsDiagnostics(IAnalyticsService analytics) => _analytics = analytics;

	public void NavigationStarted(NavigationOperation operation)
		=> _analytics.Track("navigation_started", new { operation.Id, operation.Route });

	public void NavigationSucceeded(NavigationOperation operation)
		=> _analytics.Track("navigation_succeeded", new
		{
			operation.Id,
			DurationMs = operation.Elapsed?.TotalMilliseconds
		});

	public void NavigationFailed(NavigationOperation operation, NavigationResult result)
		=> _analytics.Track("navigation_failed", new
		{
			operation.Id,
			result.FailureReason,
			result.Message
		});

	public void NavigationGuardRejected(NavigationOperation operation, NavigationGuardResult guardResult)
		=> _analytics.Track("guard_rejected", new { operation.Id, guardResult.Reason });

	public void BackNavigationRequested(BackNavigationOptions options)    { }
	public void FlowEntered(string flowName, NavigationOperation op)      { }
	public void FlowExited(string flowName, NavigationOperation op)       { }
}
```

---

## Privacy and Parameter Safety

Navigation parameters are **never logged directly** by the default implementation. Only parameter *keys* appear in log output. Implement `INavigationParameterSanitizer` if you need to control which parameter values are forwarded to external sinks.

---

## Correlation with NavigationOperation.Id

Every diagnostic event carries the `NavigationOperation.Id` (`Guid`). Use this to correlate all log entries for a single navigation in structured logging systems such as Seq or Application Insights:

```
[nav] {Id=abc123} Navigation started: main/home
[nav] {Id=abc123} Guard evaluated: AppAuthGuard → Allowed
[nav] {Id=abc123} Navigation succeeded in 42ms
```

---

## Debugger Integration

When `UseNavigationDebugger` is called, the debugger replaces the default `INavigationDiagnostics`, `IBackNavigationDiagnostics`, and `IOverlayDiagnostics` registrations with the `NavigationDiagnosticsBridge`. The bridge forwards all events to the `NavigationDiagnosticsBus`, the session recorder, and the runtime warning engine.

See [debugger/overview.md](../debugger/overview.md).

---

## Related Pages

- [Navigation Diagnostics](navigation-diagnostics.md)
- [Back Navigation Diagnostics](back-navigation-diagnostics.md)
- [Overlay Diagnostics](overlay-diagnostics.md)
- [Debugger Overview](../debugger/overview.md)
