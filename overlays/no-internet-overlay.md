# No-Internet Overlay

## Overview

The no-internet overlay displays a banner or screen informing the user that the device is offline. Unlike the loading overlay, the no-internet overlay typically includes a **retry action** that the user can tap to re-attempt connectivity.

---

## ShowNoInternetAsync

```csharp
Task ShowNoInternetAsync(
	NoInternetOverlayOptions? options = null,
	CancellationToken cancellationToken = default);
```

Displays the no-internet overlay.

---

## HideNoInternetAsync

```csharp
Task HideNoInternetAsync(CancellationToken cancellationToken = default);
```

Hides the no-internet overlay. Safe to call even if the overlay is not visible.

---

## NoInternetOverlayOptions

```csharp
public sealed class NoInternetOverlayOptions
{
	public string?     Message          { get; set; }
	public string      RetryButtonLabel { get; set; } = "Retry";
	public Func<Task>? OnRetry          { get; set; }
}
```

| Property | Default | Description |
|---|---|---|
| `Message` | `null` | Text displayed on the overlay |
| `RetryButtonLabel` | `"Retry"` | Label for the retry action button |
| `OnRetry` | `null` | Async callback invoked when the user taps the retry button |

---

## Basic Usage

```csharp
await _overlay.ShowNoInternetAsync(new NoInternetOverlayOptions
{
	Message          = "No internet connection.",
	RetryButtonLabel = "Try Again",
	OnRetry          = async () =>
	{
		await _overlay.HideNoInternetAsync();
		await _dataService.RefreshAsync();
	}
});
```

---

## Connectivity-Driven Show/Hide

The recommended pattern is to react to the MAUI `Connectivity.ConnectivityChanged` event:

```csharp
public sealed class ConnectivityMonitor
{
	private readonly IOverlayNavigationService _overlay;

	public ConnectivityMonitor(IOverlayNavigationService overlay)
	{
		_overlay = overlay;
		Connectivity.ConnectivityChanged += OnConnectivityChanged;
	}

	private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
	{
		if (e.NetworkAccess != NetworkAccess.Internet)
		{
			await _overlay.ShowNoInternetAsync(new NoInternetOverlayOptions
			{
				Message  = "You are offline.",
				OnRetry  = async () => await _overlay.HideNoInternetAsync()
			});
		}
		else
		{
			await _overlay.HideNoInternetAsync();
		}
	}
}
```

---

## Overlay vs Navigation-Based Offline State

| Approach | When to Use |
|---|---|
| `ShowNoInternetAsync` | Transient offline state mid-session. User is already authenticated and in the main flow. |
| `InternetRequiredGuard` | Gate specific routes from being navigated to when offline. |
| Flow transition | When the entire app must switch to an offline mode with a dedicated page hierarchy. |

> **Do not** call `GoToAsync<NoInternetPage>()`. This pushes the page onto the stack and makes it reachable via the back button. Analyzer rule **SHAUNAV005** flags this pattern.

---

## Related Pages

- [Overlays Overview](overview.md)
- [Loading Overlay](loading-overlay.md)
- [Custom Overlays](custom-overlays.md)
- [Internet Required Guard](../guards/internet-required-guard.md)
