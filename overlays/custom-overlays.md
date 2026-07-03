# Custom Overlays

## Overview

Beyond the built-in loading and no-internet overlays, you can show any `View`-derived type as a custom overlay using the generic `ShowOverlayAsync<TOverlay>` and `HideOverlayAsync<TOverlay>` methods.

---

## ShowOverlayAsync\<TOverlay\>

```csharp
Task ShowOverlayAsync<TOverlay>(
	object? parameter = null,
	CancellationToken cancellationToken = default)
	where TOverlay : View;
```

Shows the overlay of type `TOverlay`. An optional `parameter` object is stored and can be retrieved via `GetParameter(overlayKey)`.

---

## HideOverlayAsync\<TOverlay\>

```csharp
Task HideOverlayAsync<TOverlay>(
	CancellationToken cancellationToken = default)
	where TOverlay : View;
```

Hides the overlay of type `TOverlay`.

---

## Implementing a Custom Overlay

Create a MAUI `ContentView` for the overlay UI:

```xaml
<!-- Overlays/ToastOverlay.xaml -->
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
			 xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
			 x:Class="MyApp.Overlays.ToastOverlay">
	<Frame BackgroundColor="#323232"
		   CornerRadius="8"
		   Padding="16"
		   HorizontalOptions="Center"
		   VerticalOptions="End"
		   Margin="0,0,0,40">
		<Label x:Name="MessageLabel"
			   TextColor="White"
			   FontSize="14" />
	</Frame>
</ContentView>
```

```csharp
// Overlays/ToastOverlay.xaml.cs
public partial class ToastOverlay : ContentView
{
	public ToastOverlay() => InitializeComponent();

	public void SetMessage(string message) => MessageLabel.Text = message;
}
```

---

## Showing and Hiding a Custom Overlay

```csharp
// Show a toast message
await _overlay.ShowOverlayAsync<ToastOverlay>(parameter: "Item saved successfully");

// Retrieve the parameter later
var message = _overlay.GetParameter(typeof(ToastOverlay).FullName!) as string;

// Hide the toast
await _overlay.HideOverlayAsync<ToastOverlay>();
```

---

## Auto-Dismiss Pattern

```csharp
public async Task ShowToastAsync(string message, TimeSpan? duration = null)
{
	await _overlay.ShowOverlayAsync<ToastOverlay>(parameter: message);

	await Task.Delay(duration ?? TimeSpan.FromSeconds(3));

	// Only hide if still visible (another call may have hidden it)
	if (_overlay.IsOverlayVisible(typeof(ToastOverlay).FullName!))
		await _overlay.HideOverlayAsync<ToastOverlay>();
}
```

---

## Checking Overlay Visibility

```csharp
// Check by overlay key (type full name)
bool isToastVisible = _overlay.IsOverlayVisible(typeof(ToastOverlay).FullName!);

// Get all active overlays
var snapshot = _overlay.GetSnapshot();
foreach (var key in snapshot.VisibleOverlays)
	Console.WriteLine($"Active overlay: {key}");
```

---

## StateChanged Event

React to overlay state changes across the system:

```csharp
_overlay.StateChanged += (_, _) =>
{
	var snapshot = _overlay.GetSnapshot();
	// Update bound properties
	IsAnyOverlayVisible = snapshot.VisibleOverlays.Count > 0;
};
```

---

## Related Pages

- [Overlays Overview](overview.md)
- [Loading Overlay](loading-overlay.md)
- [No-Internet Overlay](no-internet-overlay.md)
- [Diagnostics — Overlay Diagnostics](../diagnostics/overlay-diagnostics.md)
