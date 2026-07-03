# Navigation Options

## Overview

Two option types control navigation behavior: `ShaunebuNavigationOptions` configures global defaults at startup, while `NavigationOptions` configures individual navigation calls at runtime.

---

## ShaunebuNavigationOptions — Global Configuration

Pass a delegate to `UseShaunebuNavigation` to configure the library:

```csharp
builder.UseShaunebuNavigation(options =>
{
	options.DefaultNavigationMode      = NavigationPresentationMode.Shell;
	options.PreventDoubleNavigation    = true;
	options.DoubleNavigationThreshold  = TimeSpan.FromMilliseconds(750);
	options.EnableDiagnostics          = true;
	options.EnableNavigationGuards     = true;
	options.EnableBackButtonHandling   = true;
	options.EnableOverlaySystem        = true;
	options.ThrowOnNavigationFailure   = false;
	options.MaxGuardRedirectDepth      = 5;
	options.DefaultAnimated            = true;
});
```

### All Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultNavigationMode` | `NavigationPresentationMode` | `Push` | Presentation mode when none is specified per call |
| `EnableDiagnostics` | `bool` | `false` | Emit structured events via `INavigationDiagnostics` |
| `PreventDoubleNavigation` | `bool` | `true` | Block rapid duplicate navigation calls |
| `DoubleNavigationThreshold` | `TimeSpan` | 500 ms | Window within which duplicate calls are blocked |
| `ThrowOnNavigationFailure` | `bool` | `false` | Throw `NavigationException` on failure instead of returning `NavigationResult` |
| `RegisterShellRoutesAutomatically` | `bool` | `true` | Auto-register routes in MAUI Shell `Routing` table |
| `EnableOverlaySystem` | `bool` | `true` | Enable the overlay system (`IOverlayNavigationService`) |
| `EnableBackButtonHandling` | `bool` | `true` | Enable system/hardware back button interception |
| `EnableNavigationGuards` | `bool` | `true` | Enable the navigation guard pipeline |
| `MaxGuardRedirectDepth` | `int` | `5` | Maximum consecutive guard-redirects before aborting with `GuardRedirectLoop` |
| `DefaultAnimated` | `bool` | `true` | Default animation state for navigations |
| `DefaultStackBehavior` | `NavigationStackBehavior` | `Default` | Default stack behavior when none is specified per call |

### Recommended Production Configuration

```csharp
builder.UseShaunebuNavigation(options =>
{
	options.DefaultNavigationMode      = NavigationPresentationMode.Push;
	options.PreventDoubleNavigation    = true;
	options.DoubleNavigationThreshold  = TimeSpan.FromMilliseconds(750);
	options.EnableDiagnostics          = false;   // disable in production if not needed
	options.EnableNavigationGuards     = true;
	options.EnableBackButtonHandling   = true;
	options.EnableOverlaySystem        = true;
	options.ThrowOnNavigationFailure   = false;   // always false in production
	options.MaxGuardRedirectDepth      = 5;
});
```

---

## NavigationOptions — Per-Call Configuration

Pass a configure delegate to any `INavigationHandler` method to override defaults for that specific call:

```csharp
await _navigation.GoToAsync<ProductDetailsPage>(options =>
{
	options.Animated                = true;
	options.PresentationMode        = NavigationPresentationMode.Push;
	options.StackBehavior           = NavigationStackBehavior.ReplaceCurrent;
	options.Parameters["ProductId"] = productId;
	options.Source                  = nameof(ProductListViewModel);
	options.Reason                  = "User tapped product row";
});
```

### All Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `PresentationMode` | `NavigationPresentationMode` | `Push` | How the page is presented |
| `Animated` | `bool` | `true` | Whether the transition is animated |
| `IsRoot` | `bool` | `false` | Whether the target becomes the new root |
| `ClearBackStack` | `bool` | `false` | Whether the back stack is cleared after navigating |
| `PreventDuplicates` | `bool` | `true` | Prevent duplicate consecutive navigation to the same page |
| `AllowConcurrentNavigation` | `bool` | `false` | When `false`, the navigation lock is enforced |
| `Parameters` | `Dictionary<string, object?>` | empty | Parameters passed to the target page or ViewModel |
| `Transition` | `NavigationTransition` | `Default` | Visual transition applied to the navigation |
| `StackBehavior` | `NavigationStackBehavior` | `Default` | How the navigation stack is managed |
| `Source` | `string?` | `null` | Caller or feature that initiated this navigation |
| `Reason` | `string?` | `null` | Human-readable reason for the navigation |

---

## NavigationPresentationMode

| Value | Description |
|---|---|
| `Push` | Pushes onto the current navigation stack (default) |
| `Modal` | Opens the page modally |
| `Root` | Replaces the application root, clearing the entire back stack |
| `Replace` | Replaces the current page without a back-stack entry |
| `Shell` | Uses Shell navigation (route auto-selected as relative or absolute) |
| `ShellAbsolute` | Uses an absolute Shell route (e.g. `//main/home`) |
| `ShellRelative` | Uses a relative Shell route (e.g. `details`) |

---

## NavigationStackBehavior

| Value | Description |
|---|---|
| `Default` | Preserve the stack as-is |
| `Preserve` | Explicitly preserve the existing stack |
| `Clear` | Clear the entire stack before navigating |
| `ReplaceCurrent` | Replace the top-most page in the stack |
| `RemoveDuplicates` | Remove existing entries for the target page before pushing |
| `ResetToRoot` | Reset the stack to contain only the root page |

---

## NavigationTransition

| Value | Description |
|---|---|
| `Default` | Platform default transition |
| `None` | No transition animation |
| `SlideFromRight` | Slide in from the right (forward) |
| `SlideFromLeft` | Slide in from the left (backward) |
| `SlideFromBottom` | Slide up from the bottom (modal-style) |
| `Fade` | Fade the incoming page in |

---

## Common Configuration Scenarios

### Standard Push
```csharp
await _navigation.GoToAsync<DetailsPage>();
// PresentationMode = Push (default)
```

### Modal Sheet
```csharp
await _navigation.ShowModalAsync<TermsPage>(options =>
{
	options.Transition = NavigationTransition.SlideFromBottom;
});
```

### Replace Root After Login
```csharp
await _navigation.SetRootAsync<HomePage>(options =>
{
	options.ClearBackStack = true;
	options.Reason         = "User authenticated";
});
```

### Replace Current Page
```csharp
await _navigation.GoToAsync<SplashPage>(options =>
{
	options.StackBehavior = NavigationStackBehavior.ReplaceCurrent;
});
```

### Passing Parameters
```csharp
await _navigation.GoToAsync<OrderDetailsPage>(options =>
{
	options.Parameters["OrderId"]   = orderId;
	options.Parameters["IsReadOnly"] = true;
});
```

In the receiving ViewModel via `INavigationAware`:
```csharp
public Task OnNavigatedToAsync(NavigationContext context)
{
	var orderId   = (int)context.Parameters["OrderId"]!;
	var isReadOnly = (bool)context.Parameters["IsReadOnly"]!;
	// ...
	return Task.CompletedTask;
}
```

---

## Related Pages

- [Typed Navigation](typed-navigation.md)
- [Navigation Routes](navigation-routes.md)
- [Navigation Results](navigation-results.md)
