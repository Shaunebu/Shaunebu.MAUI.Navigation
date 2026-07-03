# Runtime Warnings

## Overview

The `NavigationRuntimeWarningEngine` evaluates every incoming diagnostic event against a set of named warning rules and emits structured `NavigationRuntimeWarning` objects. Warnings appear in the VSIX **Warnings** panel and are stored in the recorded session.

Warnings are evaluated automatically when `EnableRuntimeWarnings = true` (the default). No code changes are needed in your app.

---

## INavigationRuntimeWarningEngine

```csharp
public interface INavigationRuntimeWarningEngine
{
	Task<IReadOnlyList<NavigationRuntimeWarning>> EvaluateAsync(
		INavigationDiagnosticEvent @event,
		CancellationToken cancellationToken = default);
}
```

---

## NavigationRuntimeWarning

```csharp
public sealed class NavigationRuntimeWarning
{
	public string             Code        { get; init; }   // e.g. "NAV-W001"
	public DebuggerWarningSeverity Severity { get; init; }
	public string             Message     { get; init; }
	public Guid?              OperationId { get; init; }
	public string?            PageTypeName { get; init; }
	public string?            Route       { get; init; }
	public DateTimeOffset     OccurredAt  { get; init; }
}
```

---

## DebuggerWarningSeverity

| Value | Meaning |
|---|---|
| `Info` | Informational note; no action required |
| `Warning` | Potential design problem worth reviewing |
| `Error` | Likely incorrect behaviour or a broken pattern |

---

## Built-In Warning Rules

All built-in rules implement `INavigationWarningRule` and are registered automatically.

| Code | Default Severity | Trigger |
|---|---|---|
| `NAV-W001` | Error | Navigation to a route not in the route registry |
| `NAV-W002` | Warning | Duplicate page detected in the navigation stack |
| `NAV-W003` | Warning | Modal stack depth exceeds the configured threshold (default: 3) |
| `NAV-W004` | Warning | Navigation stack depth exceeds the configured threshold (default: 10) |
| `NAV-W008` | Warning | Navigation attempted while another operation is in-progress (lock contention) |
| `NAV-W009` | Warning | Back navigation attempted on an empty navigation stack |
| `NAV-W012` | Info | Navigation completed in more than the threshold duration (default: 1000 ms) |

---

## Configuring Warning Rules

Warning thresholds and severity overrides are set via `NavigationDebuggerOptions`:

```csharp
builder.Services.UseNavigationDebugger(opts =>
{
	opts.EnableRuntimeWarnings = true;

	// Override modal stack depth threshold (NAV-W003)
	opts.Warnings.Configure("NAV-W003", rule =>
	{
		rule.Threshold        = 5;
		rule.SeverityOverride = DebuggerWarningSeverity.Error;
	});

	// Disable slow-navigation info notice (NAV-W012)
	opts.Warnings.Configure("NAV-W012", rule => rule.Enabled = false);

	// Lower slow-navigation threshold to 500 ms
	opts.Warnings.Configure("NAV-W012", rule => rule.Threshold = 500);
});
```

---

## Custom Warning Rules

Implement `INavigationWarningRule` and register it in DI before calling `UseNavigationDebugger`:

```csharp
public sealed class GuardRedirectWarningRule : INavigationWarningRule
{
	public string                Code            => "NAV-W099";
	public DebuggerWarningSeverity DefaultSeverity => DebuggerWarningSeverity.Warning;

	public Task<NavigationRuntimeWarning?> EvaluateAsync(
		INavigationDiagnosticEvent @event,
		NavigationDebuggerContext context,
		CancellationToken cancellationToken = default)
	{
		if (@event is not NavigationFailedEvent failed)
			return Task.FromResult<NavigationRuntimeWarning?>(null);

		if (failed.FailureReason?.Contains("GuardRedirectLoop") is not true)
			return Task.FromResult<NavigationRuntimeWarning?>(null);

		return Task.FromResult<NavigationRuntimeWarning?>(new NavigationRuntimeWarning
		{
			Code       = Code,
			Severity   = DefaultSeverity,
			Message    = "Guard redirect loop detected.",
			OperationId = failed.OperationId,
			Route      = failed.Route
		});
	}
}

// Register before UseNavigationDebugger
builder.Services.AddSingleton<INavigationWarningRule, GuardRedirectWarningRule>();

#if DEBUG
builder.Services.UseNavigationDebugger();
#endif
```

---

## Accessing Warnings Programmatically

Resolve `INavigationRuntimeWarningEngine` to evaluate events manually:

```csharp
var warnings = await _warningEngine.EvaluateAsync(diagnosticEvent);

foreach (var warning in warnings)
	Console.WriteLine($"[{warning.Severity}] {warning.Code}: {warning.Message}");
```

Or inspect all recorded warnings from the current session:

```csharp
var session = _recorder.CurrentSession;
foreach (var warning in session.Warnings)
	Console.WriteLine($"{warning.Code} at {warning.OccurredAt}: {warning.Message}");
```

---

## Related Pages

- [Debugger Overview](overview.md)
- [Session Recording](session-recording.md)
- [Analyzers Overview](../analyzers/overview.md) — compile-time counterpart to runtime warnings
