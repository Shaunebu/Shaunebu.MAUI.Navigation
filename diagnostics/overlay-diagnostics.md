# Overlay Diagnostics

## Overview

`IOverlayDiagnostics` receives structured events for every overlay show/hide lifecycle event. Use it to track overlay usage in analytics, detect unusual overlay patterns, or assert overlay behavior in tests.

---

## Interface

```csharp
public interface IOverlayDiagnostics
{
	void OverlayShowing(string overlayKey, OverlayPriority priority);
	void OverlayShown(string overlayKey);
	void OverlayHiding(string overlayKey);
	void OverlayHidden(string overlayKey);
	void OverlayOperationFailed(string overlayKey, string reason);
}
```

---

## Event Reference

### OverlayShowing

Called when `ShowLoadingAsync`, `ShowNoInternetAsync`, or `ShowOverlayAsync<T>` is invoked, before the overlay is rendered:

```csharp
public void OverlayShowing(string overlayKey, OverlayPriority priority)
	=> _logger.LogDebug("[overlay] Showing: {Key} (priority: {Priority})",
		overlayKey, priority);
```

`overlayKey` is a string identifying the overlay type (e.g., `"Loading"`, `"NoInternet"`, or the full type name for custom overlays).

### OverlayShown

Called after the overlay has been rendered successfully:

```csharp
public void OverlayShown(string overlayKey)
	=> _logger.LogInformation("[overlay] Shown: {Key}", overlayKey);
```

### OverlayHiding

Called when `HideLoadingAsync`, `HideNoInternetAsync`, or `HideOverlayAsync<T>` is invoked:

```csharp
public void OverlayHiding(string overlayKey)
	=> _logger.LogDebug("[overlay] Hiding: {Key}", overlayKey);
```

### OverlayHidden

Called after the overlay has been hidden:

```csharp
public void OverlayHidden(string overlayKey)
	=> _logger.LogInformation("[overlay] Hidden: {Key}", overlayKey);
```

### OverlayOperationFailed

Called when an overlay show or hide operation failed or was suppressed:

```csharp
public void OverlayOperationFailed(string overlayKey, string reason)
	=> _logger.LogWarning("[overlay] Operation failed: {Key} — {Reason}",
		overlayKey, reason);
```

---

## OverlayPriority

`OverlayPriority` is an enum that controls which overlay is rendered on top when multiple are active. Higher values are rendered on top:

```csharp
public enum OverlayPriority
{
	Low,
	Normal,
	High,
	Critical
}
```

The loading overlay uses `Normal`; the no-internet overlay uses `High` by default.

---

## Registration

```csharp
builder.Services.AddSingleton<IOverlayDiagnostics, MyOverlayDiagnostics>();
```

The debugger replaces this registration with `NavigationDiagnosticsBridge` when `UseNavigationDebugger` is called.

---

## Testing With Overlay Diagnostics

```csharp
public sealed class TestOverlayDiagnostics : IOverlayDiagnostics
{
	public List<string> ShownOverlays  { get; } = [];
	public List<string> HiddenOverlays { get; } = [];

	public void OverlayShown(string overlayKey)  => ShownOverlays.Add(overlayKey);
	public void OverlayHidden(string overlayKey) => HiddenOverlays.Add(overlayKey);
	public void OverlayShowing(string overlayKey, OverlayPriority priority) { }
	public void OverlayHiding(string overlayKey)                            { }
	public void OverlayOperationFailed(string overlayKey, string reason)    { }
}

// In a unit test
var diagnostics = new TestOverlayDiagnostics();
services.AddSingleton<IOverlayDiagnostics>(diagnostics);

// ... exercise code ...

Assert.Contains("Loading", diagnostics.ShownOverlays);
Assert.Contains("Loading", diagnostics.HiddenOverlays);
```

---

## Related Pages

- [Diagnostics Overview](overview.md)
- [Navigation Diagnostics](navigation-diagnostics.md)
- [Back Navigation Diagnostics](back-navigation-diagnostics.md)
- [Overlays Overview](../overlays/overview.md)
