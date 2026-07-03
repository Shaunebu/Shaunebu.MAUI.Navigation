# Back Navigation Diagnostics

## Overview

`IBackNavigationDiagnostics` receives structured events for every back navigation operation. It covers all ways back navigation can succeed, fail, or be blocked — including blocks by `IBackAware` ViewModels, guards, and the overlay system.

---

## Interface

```csharp
public interface IBackNavigationDiagnostics
{
	void BackNavigationRequested(BackNavigationOptions options);
	void BackNavigationSucceeded(NavigationOperation operation);
	void BackNavigationFailed(NavigationOperation operation, NavigationResult result);
	void BackNavigationGuardRejected(NavigationOperation operation, string? reason);
	void BackNavigationPreventedByViewModel(NavigationOperation operation);
	void BackNavigationHandledByCustomHandler(NavigationOperation operation);
	void BackNavigationBlockedByOverlay(NavigationOperation operation);
}
```

---

## Event Reference

### BackNavigationRequested

Called when `GoBackAsync()` or the hardware back button initiates a back navigation. Fired before any guard or ViewModel evaluation:

```csharp
public void BackNavigationRequested(BackNavigationOptions options)
	=> _logger.LogDebug("[back] Back navigation requested");
```

### BackNavigationSucceeded

Called when back navigation completed successfully:

```csharp
public void BackNavigationSucceeded(NavigationOperation operation)
	=> _logger.LogInformation("[back] Succeeded from {Route} ({Id})",
		operation.Route, operation.Id);
```

### BackNavigationFailed

Called when back navigation failed (e.g., empty stack):

```csharp
public void BackNavigationFailed(NavigationOperation operation, NavigationResult result)
	=> _logger.LogWarning("[back] Failed: {Reason} ({Id})",
		result.FailureReason, operation.Id);
```

### BackNavigationGuardRejected

Called when a guard returned `Reject` for a back navigation:

```csharp
public void BackNavigationGuardRejected(NavigationOperation operation, string? reason)
	=> _logger.LogWarning("[back] Guard rejected: {Reason} ({Id})",
		reason, operation.Id);
```

### BackNavigationPreventedByViewModel

Called when an `IBackAware` ViewModel's `CanGoBackAsync()` returned `false`:

```csharp
public void BackNavigationPreventedByViewModel(NavigationOperation operation)
	=> _logger.LogInformation("[back] Prevented by ViewModel ({Id})", operation.Id);
```

This event fires when the user has unsaved changes and tapped "Stay" in the confirmation dialog.

### BackNavigationHandledByCustomHandler

Called when a registered custom back handler intercepted and handled the back navigation:

```csharp
public void BackNavigationHandledByCustomHandler(NavigationOperation operation)
	=> _logger.LogDebug("[back] Handled by custom handler ({Id})", operation.Id);
```

### BackNavigationBlockedByOverlay

Called when back navigation was blocked because an overlay is currently active (e.g., the loading overlay is visible):

```csharp
public void BackNavigationBlockedByOverlay(NavigationOperation operation)
	=> _logger.LogDebug("[back] Blocked by overlay ({Id})", operation.Id);
```

---

## Registration

```csharp
builder.Services.AddSingleton<IBackNavigationDiagnostics, MyBackNavDiagnostics>();
```

---

## Related Pages

- [Diagnostics Overview](overview.md)
- [Navigation Diagnostics](navigation-diagnostics.md)
- [Overlay Diagnostics](overlay-diagnostics.md)
- [Typed Navigation — IBackAware](../navigation/typed-navigation.md#ibackaware--intercepting-back-navigation)
