# Navigation Routes

## Overview

Routes map page types to string identifiers used by the MAUI Shell router and the internal `INavigationRouteRegistry`. Routes are registered at startup either manually via `ShaunebuNavigationOptions.RegisterPage<TPage>()` or automatically via the `[NavigationRoute]` source generator attribute.

---

## Manual Route Registration

Routes can be registered imperatively inside the `UseShaunebuNavigation` delegate:

```csharp
builder.UseShaunebuNavigation(options =>
{
	options.RegisterPage<LoginPage>(
		route:                  "auth/login",
		flow:                   "Auth",
		requiresAuthentication: false,
		allowAnonymous:         true);

	options.RegisterPage<HomePage>(
		route:                  "main/home",
		flow:                   "Main",
		requiresAuthentication: true);

	options.RegisterPage<PrivacyPage>(
		route:    "shared/privacy",
		flow:     null,          // global — accessible from any flow
		allowAnonymous: true);
});
```

### RegisterPage\<TPage\> parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `route` | `string` | required | The route string (e.g. `"main/home"`) |
| `flow` | `string?` | `null` | The flow name, or `null` for global routes |
| `requiresAuthentication` | `bool` | `false` | Hint for `AuthenticationGuard` base classes |
| `allowAnonymous` | `bool` | `false` | Explicitly permits unauthenticated access |
| `defaultPresentationMode` | `NavigationPresentationMode` | `Push` | Default mode for this route |

---

## Generator-Based Route Registration

The recommended approach is to annotate each page with `[NavigationRoute]` and call the generated helper:

```csharp
// Page declaration
[NavigationRoute("main/home", Flow = "Main")]
public partial class HomePage : ContentPage { }

[NavigationRoute("auth/login", Flow = "Auth")]
public partial class LoginPage : ContentPage { }

[NavigationRoute("shared/privacy", IsShared = true)]
public partial class PrivacyPage : ContentPage { }

// MauiProgram.cs — single registration call
builder.UseShaunebuNavigation(options =>
{
	GeneratedNavigationRegistration.RegisterGeneratedRoutes(options);
});
```

The generator emits `GeneratedNavigationRegistration.RegisterGeneratedRoutes(options)` which calls `options.RegisterPage<TPage>` for every annotated page. It also emits a `NavigationRoutes` static class with route constant strings.

See [generators/generated-routes.md](../generators/generated-routes.md) for the full list of generated artifacts.

---

## NavigationRouteAttribute

```csharp
[NavigationRoute("main/home", Flow = "Main")]
public partial class HomePage : ContentPage { }
```

| Property | Type | Description |
|---|---|---|
| `Route` | `string` | The navigation route string. Must contain only alphanumeric characters, slashes, hyphens, and underscores. |
| `Flow` | `string?` | The flow name this page belongs to (`"Auth"`, `"Main"`, etc.). `null` = global. |
| `IsShared` | `bool` | When `true`, the page is a shared route accessible across all flows. |

---

## Flow-Aware Route Resolution

When multiple flows register the same page type (e.g. `PrivacyPage` in both `Auth` and `Main`), the `INavigationRouteRegistry` selects the route belonging to the currently active flow.

```csharp
// Same page type, two flows
options.RegisterPage<PrivacyPage>("auth/privacy",  flow: "Auth");
options.RegisterPage<PrivacyPage>("main/privacy",  flow: "Main");

// Navigation — route selected based on INavigationFlowManager.CurrentFlow
await _navigation.GoToAsync<PrivacyPage>();
// If Auth flow is active → navigates to "auth/privacy"
// If Main flow is active → navigates to "main/privacy"
```

If the route is ambiguous and no flow is active, the result carries `NavigationFailureReason.AmbiguousRoute`.

---

## Shell Route Auto-Registration

When `ShaunebuNavigationOptions.RegisterShellRoutesAutomatically` is `true` (the default), routes are also registered in the MAUI Shell route table (`Routing.RegisterRoute`) automatically during DI setup, so `GoToAsync` using `Shell` presentation mode works without manual `Routing.RegisterRoute` calls.

---

## INavigationRoute — Typed Route Objects

For advanced scenarios, implement `INavigationRoute` to create strongly typed route objects that carry both destination and metadata:

```csharp
public sealed class ProductDetailsRoute : INavigationRoute
{
	public int    ProductId   { get; init; }
	public string ProductName { get; init; } = string.Empty;
}

// Usage
await _navigation.GoToAsync(new ProductDetailsRoute
{
	ProductId   = 42,
	ProductName = "Widget Pro"
});
```

---

## NavigationRouteDescriptor

Routes stored in the registry are described by `NavigationRouteDescriptor`:

| Property | Type | Description |
|---|---|---|
| `Route` | `string` | The route string |
| `PageType` | `Type` | The page type |
| `FlowName` | `string?` | The owning flow, or `null` for global routes |
| `RequiresAuthentication` | `bool` | Auth hint |
| `AllowAnonymous` | `bool` | Anonymous access flag |
| `DefaultPresentationMode` | `NavigationPresentationMode` | Default mode |
| `IsSharedRoute` | `bool` | `true` when the same page is registered across flows |

---

## Related Pages

- [Typed Navigation](typed-navigation.md)
- [Navigation Options](navigation-options.md)
- [Generators — Generated Routes](../generators/generated-routes.md)
- [Flows Overview](../flows/overview.md)
