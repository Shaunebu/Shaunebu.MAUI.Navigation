# Navigation Diagnostics

## Overview

`INavigationDiagnostics` receives structured events for every forward navigation operation. The default implementation logs via `ILogger<T>`. Replace it to forward events to external services.

---

## Interface

```csharp
public interface INavigationDiagnostics
{
	void NavigationStarted(NavigationOperation operation);
	void NavigationSucceeded(NavigationOperation operation);
	void NavigationFailed(NavigationOperation operation, NavigationResult result);
	void NavigationGuardRejected(NavigationOperation operation, NavigationGuardResult guardResult);
	void BackNavigationRequested(BackNavigationOptions options);
	void FlowEntered(string flowName, NavigationOperation operation);
	void FlowExited(string flowName, NavigationOperation operation);
}
```

---

## Event Reference

### NavigationStarted

Called when a navigation operation has been created and is about to execute (before guards run):

```csharp
public void NavigationStarted(NavigationOperation operation)
{
	_logger.LogDebug("[nav] Started: {Route} ({Id})", operation.Route, operation.Id);
}
```

### NavigationSucceeded

Called when the navigation completed successfully:

```csharp
public void NavigationSucceeded(NavigationOperation operation)
{
	_logger.LogInformation("[nav] Succeeded: {Route} in {Ms}ms ({Id})",
		operation.Route,
		operation.Elapsed?.TotalMilliseconds,
		operation.Id);
}
```

### NavigationFailed

Called when the navigation failed for any reason:

```csharp
public void NavigationFailed(NavigationOperation operation, NavigationResult result)
{
	_logger.LogWarning("[nav] Failed: {Route} — {Reason} ({Id})",
		operation.Route,
		result.FailureReason,
		operation.Id);
}
```

### NavigationGuardRejected

Called when a guard returned `Reject` or the guard pipeline was aborted:

```csharp
public void NavigationGuardRejected(NavigationOperation operation, NavigationGuardResult guardResult)
{
	_logger.LogWarning("[nav] Guard rejected: {Route} — {Reason} ({Id})",
		operation.Route,
		guardResult.Reason,
		operation.Id);
}
```

### FlowEntered / FlowExited

Called when a flow transition occurs:

```csharp
public void FlowEntered(string flowName, NavigationOperation operation)
	=> _logger.LogInformation("[nav] Flow entered: {Flow} ({Id})", flowName, operation.Id);

public void FlowExited(string flowName, NavigationOperation operation)
	=> _logger.LogInformation("[nav] Flow exited: {Flow} ({Id})", flowName, operation.Id);
```

---

## NavigationOperation Properties Available in Events

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Unique identifier for correlation |
| `StartedAt` | `DateTimeOffset` | UTC start time |
| `CompletedAt` | `DateTimeOffset?` | UTC completion time (set on succeeded/failed events) |
| `TargetPageType` | `Type?` | The page being navigated to |
| `Route` | `string?` | The resolved route string |
| `PresentationMode` | `NavigationPresentationMode` | How the page was presented |
| `Source` | `string?` | The caller that initiated navigation |
| `Reason` | `string?` | Human-readable navigation reason |
| `Elapsed` | `TimeSpan?` | Duration from start to completion |

---

## Registration

```csharp
// Option 1: Enable default ILogger-backed implementation
builder.UseShaunebuNavigation(options =>
{
	options.EnableDiagnostics = true;
});

// Option 2: Custom implementation
builder.Services.AddSingleton<INavigationDiagnostics, MyDiagnostics>();
```

---

## Related Pages

- [Diagnostics Overview](overview.md)
- [Back Navigation Diagnostics](back-navigation-diagnostics.md)
- [Overlay Diagnostics](overlay-diagnostics.md)
- [Debugger Overview](../debugger/overview.md)
