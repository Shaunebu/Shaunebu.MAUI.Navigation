# Loading Overlay

## Overview

The loading overlay displays a blocking spinner with an optional message during asynchronous operations such as network requests, authentication, or data loading. It prevents user interaction while the operation is in progress.

---

## ShowLoadingAsync

```csharp
Task ShowLoadingAsync(
	LoadingOverlayOptions? options = null,
	CancellationToken cancellationToken = default);
```

Displays the loading overlay. Calling `ShowLoadingAsync` while the overlay is already visible updates the message if a new one is provided.

---

## HideLoadingAsync

```csharp
Task HideLoadingAsync(CancellationToken cancellationToken = default);
```

Hides the loading overlay. Safe to call even if the overlay is not visible.

---

## LoadingOverlayOptions

```csharp
public sealed class LoadingOverlayOptions
{
	public string? Message       { get; set; }
	public bool    IsCancellable { get; set; } = false;
}
```

| Property | Default | Description |
|---|---|---|
| `Message` | `null` | Text displayed beneath the spinner. Pass `null` to show a spinner without text. |
| `IsCancellable` | `false` | When `true`, tapping outside the overlay dismisses it. |

---

## Basic Usage

```csharp
await _overlay.ShowLoadingAsync(new LoadingOverlayOptions { Message = "Signing in…" });
try
{
	await _authService.LoginAsync(username, password);
	await _flowManager.ResetToFlowAsync<MainFlow>();
}
finally
{
	await _overlay.HideLoadingAsync();
}
```

---

## Without a Message

```csharp
await _overlay.ShowLoadingAsync();
try
{
	await _dataService.SyncAsync();
}
finally
{
	await _overlay.HideLoadingAsync();
}
```

---

## Multiple Async Operations

For a series of operations, update the message between steps while keeping the overlay visible:

```csharp
await _overlay.ShowLoadingAsync(new LoadingOverlayOptions { Message = "Step 1 of 3: Loading profile…" });
try
{
	await _profileService.LoadAsync();

	// Update message for step 2
	await _overlay.ShowLoadingAsync(new LoadingOverlayOptions { Message = "Step 2 of 3: Loading orders…" });
	await _orderService.LoadAsync();

	await _overlay.ShowLoadingAsync(new LoadingOverlayOptions { Message = "Step 3 of 3: Finalizing…" });
	await _settingsService.LoadAsync();
}
finally
{
	await _overlay.HideLoadingAsync();
}
```

---

## Checking Visibility

```csharp
var snapshot = _overlay.GetSnapshot();
if (snapshot.IsLoadingVisible)
{
	// Loading is already showing — avoid double-show
	return;
}
```

---

## Related Pages

- [Overlays Overview](overview.md)
- [No-Internet Overlay](no-internet-overlay.md)
- [Custom Overlays](custom-overlays.md)
