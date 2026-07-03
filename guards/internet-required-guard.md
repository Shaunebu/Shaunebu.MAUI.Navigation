# Internet Required Guard

## Overview

`InternetRequiredGuard<TOfflinePage>` is an abstract base class that blocks navigation to pages that require an internet connection when the device is offline, optionally redirecting to a no-internet page.

---

## Class Declaration

```csharp
public abstract class InternetRequiredGuard<TOfflinePage> : INavigationGuard
	where TOfflinePage : Microsoft.Maui.Controls.Page
```

---

## Abstract Members to Implement

### IsConnectedAsync

Returns whether the device currently has an active internet connection:

```csharp
protected abstract Task<bool> IsConnectedAsync(CancellationToken cancellationToken);
```

---

## Virtual Members to Override

### RequiresInternetAsync *(optional)*

Returns whether the target page requires an internet connection. The default returns `true` for **all** pages. Override to restrict the guard to specific routes or page types:

```csharp
protected virtual Task<bool> RequiresInternetAsync(
	NavigationGuardContext context,
	CancellationToken cancellationToken)
	=> Task.FromResult(true);
```

---

## Pipeline Behavior

When the device is offline and the target page requires internet:

- The guard redirects to `TOfflinePage`.
- `TOfflinePage` itself is exempt to prevent an infinite redirect loop.

---

## Example Implementation

```csharp
// Guards/ConnectivityGuard.cs
public sealed class ConnectivityGuard : InternetRequiredGuard<NoInternetPage>
{
	protected override Task<bool> IsConnectedAsync(CancellationToken cancellationToken)
	{
		var access = Microsoft.Maui.Networking.Connectivity.NetworkAccess;
		return Task.FromResult(access == Microsoft.Maui.Networking.NetworkAccess.Internet);
	}

	// Only gate routes under "online/"
	protected override Task<bool> RequiresInternetAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken)
	{
		var requires = context.TargetRoute?.StartsWith("online/", StringComparison.Ordinal) == true;
		return Task.FromResult(requires);
	}
}
```

Register:

```csharp
builder.Services.AddSingleton<INavigationGuard, ConnectivityGuard>();
```

---

## Overlay vs Guard

For transient connectivity states (e.g., a user loses connection mid-session), consider using `IOverlayNavigationService.ShowNoInternetAsync()` instead of a guard redirect. The overlay approach does not modify the navigation stack:

```csharp
Connectivity.ConnectivityChanged += async (_, _) =>
{
	if (Connectivity.NetworkAccess != NetworkAccess.Internet)
		await _overlay.ShowNoInternetAsync(new NoInternetOverlayOptions
		{
			Message          = "No internet connection.",
			RetryButtonLabel = "Retry",
			OnRetry          = async () => await _overlay.HideNoInternetAsync()
		});
	else
		await _overlay.HideNoInternetAsync();
};
```

See [overlays/no-internet-overlay.md](../overlays/no-internet-overlay.md).

---

## Related Pages

- [Guards Overview](overview.md)
- [Overlays — No-Internet Overlay](../overlays/no-internet-overlay.md)
- [Custom Guards](custom-guards.md)
