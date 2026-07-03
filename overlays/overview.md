# Overlays Overview

## Overview

The overlay system provides a way to present transient UI states — loading spinners, offline banners, and custom overlays — **without pushing pages onto the navigation stack**. Overlays are rendered as visual layers above the current content.

> **Rule**: Do not navigate to a `LoadingPage` or `NoInternetPage`. Use overlays instead. Pushing a loading or offline page onto the navigation stack makes that state reachable via the back button and corrupts the navigation history. The analyzer rule **SHAUNAV004** and **SHAUNAV005** enforce this at compile time.

---

## IOverlayNavigationService

`IOverlayNavigationService` is registered as a singleton by `UseShaunebuNavigation` when `EnableOverlaySystem = true`.

### Full API Surface

```csharp
public interface IOverlayNavigationService
{
	event EventHandler? StateChanged;

	// Loading overlay
	Task ShowLoadingAsync(LoadingOverlayOptions? options = null, CancellationToken ct = default);
	Task HideLoadingAsync(CancellationToken ct = default);

	// No-internet overlay
	Task ShowNoInternetAsync(NoInternetOverlayOptions? options = null, CancellationToken ct = default);
	Task HideNoInternetAsync(CancellationToken ct = default);

	// Custom overlays
	Task ShowOverlayAsync<TOverlay>(object? parameter = null, CancellationToken ct = default)
		where TOverlay : View;
	Task HideOverlayAsync<TOverlay>(CancellationToken ct = default)
		where TOverlay : View;

	// State inspection
	OverlaySnapshot      GetSnapshot();
	Task<OverlaySnapshot> GetSnapshotAsync(CancellationToken ct = default);
	object?              GetParameter(string overlayKey);
	bool                 IsOverlayVisible(string overlayKey);
}
```

---

## Enabling the Overlay System

```csharp
builder.UseShaunebuNavigation(options =>
{
	options.EnableOverlaySystem = true; // default: true
});
```

---

## Core Principle: try/finally

Always pair `ShowXxx` with `HideXxx` in a `try/finally` block to guarantee cleanup even if an exception occurs:

```csharp
await _overlay.ShowLoadingAsync(new LoadingOverlayOptions { Message = "Loading…" });
try
{
	await _dataService.LoadAsync();
}
finally
{
	await _overlay.HideLoadingAsync();
}
```

---

## Overlay Prioritization

When multiple overlays are active, they are stacked by `OverlayPriority`. The highest-priority visible overlay is rendered on top. Call `GetSnapshot()` to inspect active overlays:

```csharp
var snapshot = _overlay.GetSnapshot();
Console.WriteLine($"Loading: {snapshot.IsLoadingVisible}");
Console.WriteLine($"No-internet: {snapshot.IsNoInternetVisible}");
Console.WriteLine($"All active: {string.Join(", ", snapshot.VisibleOverlays)}");
```

### OverlaySnapshot Properties

| Property | Type | Description |
|---|---|---|
| `VisibleOverlays` | `IReadOnlyList<string>` | All active overlay keys, ordered by descending priority |
| `IsLoadingVisible` | `bool` | Whether the loading overlay is visible |
| `IsNoInternetVisible` | `bool` | Whether the no-internet overlay is visible |

---

## StateChanged Event

Subscribe to `StateChanged` to react to overlay visibility changes (e.g., to update a ViewModel's `IsBusy` property):

```csharp
_overlay.StateChanged += (_, _) =>
{
	var snapshot = _overlay.GetSnapshot();
	IsBusy = snapshot.IsLoadingVisible;
};
```

---

## Sample App OverlayHost

The sample app uses a singleton `OverlayHost` service that subscribes to `StateChanged` and presents real overlay pages above Shell content:

```csharp
// Registration
builder.Services.AddSingleton<OverlayHost>();

// OverlayHost subscribes in AppShell so it is active before any navigation occurs
public partial class AppShell : Shell
{
	public AppShell(OverlayHost overlayHost, ...)
	{
		InitializeComponent();
		overlayHost.Initialize(this); // subscribes to StateChanged
	}
}
```

---

## Related Pages

- [Loading Overlay](loading-overlay.md)
- [No-Internet Overlay](no-internet-overlay.md)
- [Custom Overlays](custom-overlays.md)
- [Diagnostics — Overlay Diagnostics](../diagnostics/overlay-diagnostics.md)
- [Analyzers — SHAUNAV004/SHAUNAV005](../analyzers/analyzer-rules.md)
