# Typed Navigation

## Overview

`INavigationHandler` is the central navigation service in `Shaunebu.MAUI.Navigation`. All application navigation must flow through this abstraction — never call `Shell.Current.GoToAsync`, `NavigationPage.PushAsync`, or `Application.Current.MainPage.Navigation` directly.

`INavigationHandler` is registered as a **singleton** by `UseShaunebuNavigation`. Inject it into any ViewModel via constructor injection.

---

## The Navigation Pipeline

Every navigation call passes through this deterministic 10-step pipeline:

1. Create `NavigationOperation`
2. Validate the target page or route
3. Check duplicate-navigation threshold
4. Acquire the navigation lock
5. Run all registered `INavigationGuard` implementations
6. Resolve the route via `INavigationRouteRegistry`
7. Resolve the page via `IPageResolver`
8. Execute the appropriate adapter (Shell or NavigationPage)
9. Invoke `INavigationAware` lifecycle callbacks on the ViewModel
10. Emit `INavigationDiagnostics` events and return `NavigationResult`

All failures are returned as `NavigationResult` values unless `ShaunebuNavigationOptions.ThrowOnNavigationFailure` is `true`.

---

## INavigationHandler — Full API

### GoToAsync\<TPage\>

Navigates to the page of type `TPage`. Route resolution is flow-aware: when the same page type is registered under multiple flows, the route for the currently active flow is selected automatically.

```csharp
// Simple push
await _navigation.GoToAsync<SettingsPage>();

// Push with options
var result = await _navigation.GoToAsync<SettingsPage>(options =>
{
	options.Animated  = true;
	options.Reason    = "User tapped settings";
});

if (!result.Succeeded)
	HandleFailure(result);
```

**Signature:**
```csharp
Task<NavigationResult> GoToAsync<TPage>(
	Action<NavigationOptions>? configure = null,
	CancellationToken cancellationToken  = default)
	where TPage : Page;
```

---

### GoToAsync(Type)

Navigate by runtime page type when the generic overload is not available (e.g., dynamic dispatch):

```csharp
Type pageType = GetNextPage();
var result = await _navigation.GoToAsync(pageType);
```

**Signature:**
```csharp
Task<NavigationResult> GoToAsync(
	Type pageType,
	Action<NavigationOptions>? configure = null,
	CancellationToken cancellationToken  = default);
```

---

### GoToAsync\<TRoute\>

Navigate using a strongly typed `INavigationRoute` instance. Routes encapsulate both destination and metadata:

```csharp
public sealed class ProductRoute : INavigationRoute
{
	public int ProductId { get; init; }
}

var result = await _navigation.GoToAsync(new ProductRoute { ProductId = 42 });
```

**Signature:**
```csharp
Task<NavigationResult> GoToAsync<TRoute>(
	TRoute route,
	Action<NavigationOptions>? configure = null,
	CancellationToken cancellationToken  = default)
	where TRoute : INavigationRoute;
```

---

### GoBackAsync

Navigates back in the current navigation stack:

```csharp
var result = await _navigation.GoBackAsync();

if (result.FailureReason == NavigationFailureReason.BackNavigationNotAvailable)
	// already at root
```

Back navigation respects this priority order:
1. Close any open overlays or popups
2. Dismiss any open modal pages
3. Pop the navigation stack

Guards and `IBackAware` ViewModels are evaluated before the pop executes.

**Signature:**
```csharp
Task<NavigationResult> GoBackAsync(
	Action<BackNavigationOptions>? configure = null,
	CancellationToken cancellationToken      = default);
```

---

### ShowModalAsync\<TPage\>

Presents a page modally (equivalent to `GoToAsync<TPage>` with `PresentationMode = Modal`):

```csharp
await _navigation.ShowModalAsync<TermsPage>();
```

**Signature:**
```csharp
Task<NavigationResult> ShowModalAsync<TPage>(
	Action<NavigationOptions>? configure = null,
	CancellationToken cancellationToken  = default)
	where TPage : Page;
```

---

### CloseModalAsync

Dismisses the topmost modal page:

```csharp
await _navigation.CloseModalAsync();
```

**Signature:**
```csharp
Task<NavigationResult> CloseModalAsync(
	Action<BackNavigationOptions>? configure = null,
	CancellationToken cancellationToken      = default);
```

---

### SetRootAsync\<TPage\>

Replaces the application root with a new page, clearing the entire navigation and modal stack. Use this after authentication to prevent the login page from being reachable via back navigation:

```csharp
await _navigation.SetRootAsync<HomePage>(options =>
{
	options.ClearBackStack = true;
	options.Reason         = "User authenticated";
});
```

**Signature:**
```csharp
Task<NavigationResult> SetRootAsync<TPage>(
	Action<NavigationOptions>? configure = null,
	CancellationToken cancellationToken  = default)
	where TPage : Page;
```

> **Prefer `INavigationFlowManager.ResetToFlowAsync<TFlow>()`** for major app-state transitions, as it also fires flow lifecycle hooks (`OnEnterAsync` / `OnExitAsync`).

---

### SetRootAsync(Type)

Runtime-type overload:

```csharp
await _navigation.SetRootAsync(typeof(HomePage));
```

**Signature:**
```csharp
Task<NavigationResult> SetRootAsync(
	Type pageType,
	Action<NavigationOptions>? configure = null,
	CancellationToken cancellationToken  = default);
```

---

### ResetAsync

Resets the entire navigation state to its initial condition:

```csharp
await _navigation.ResetAsync();
```

**Signature:**
```csharp
Task<NavigationResult> ResetAsync(CancellationToken cancellationToken = default);
```

---

## ViewModel Lifecycle — INavigationAware

ViewModels that need to react to navigation events implement `INavigationAware`:

```csharp
public sealed partial class ProductListViewModel : INavigationAware
{
	public async Task OnNavigatedToAsync(NavigationContext context)
	{
		// Called after this page is navigated to — refresh data
		await LoadProductsAsync();
	}

	public Task OnNavigatingFromAsync(NavigationContext context)
	{
		// Called before navigating away — validate or cancel
		return Task.CompletedTask;
	}

	public Task OnNavigatedFromAsync(NavigationContext context)
	{
		// Called after navigating away — clean up
		return Task.CompletedTask;
	}
}
```

**Callback order for forward navigation:**
1. `OnNavigatingFromAsync` — current page's ViewModel
2. *(adapter navigates)*
3. `OnNavigatedToAsync` — new page's ViewModel
4. `OnNavigatedFromAsync` — previous page's ViewModel

The `NavigationContext` provides:
- `Operation` — the `NavigationOperation` associated with this lifecycle event
- `SourcePageType` — the page navigated from
- `TargetPageType` — the page navigated to
- `Parameters` — the parameters passed with the navigation

---

## IBackAware — Intercepting Back Navigation

ViewModels that need to prevent back navigation (e.g., forms with unsaved changes) implement `IBackAware`:

```csharp
public sealed partial class EditProfileViewModel : IBackAware
{
	private bool _hasUnsavedChanges;

	public async Task<bool> CanGoBackAsync()
	{
		if (!_hasUnsavedChanges)
			return true;

		return await Shell.Current.DisplayAlert(
			"Unsaved Changes",
			"Discard changes and go back?",
			"Discard", "Stay");
	}

	public Task OnBackAsync() => Task.CompletedTask;
}
```

When `CanGoBackAsync()` returns `false`, `GoBackAsync()` returns `NavigationFailureReason.GuardRejected`.

---

## Related Pages

- [Navigation Routes](navigation-routes.md)
- [Navigation Options](navigation-options.md)
- [Navigation Results](navigation-results.md)
- [Navigation Exceptions](navigation-exceptions.md)
- [Guards Overview](../guards/overview.md)
- [Flows Overview](../flows/overview.md)
