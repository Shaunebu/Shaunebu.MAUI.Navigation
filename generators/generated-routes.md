# Generated Routes

## Overview

When you annotate a `partial` page class with `[NavigationRoute]`, the `NavigationRouteGenerator` emits three compile-time artifacts into your project:

| Artifact | Description |
|---|---|
| `GeneratedRoutes` | Static class of route string constants |
| `GeneratedNavigationRegistration` | One-call registration helper for `MauiProgram.cs` |
| `GeneratedNavigationExtensions` | Typed `GoTo{PageName}Async` extension methods on `INavigationHandler` |

All output is placed in the `YourApp.Generated` namespace by default and is visible in the IDE via the **Analyzers / Source Files** node in Solution Explorer.

---

## Annotating Pages

```csharp
// Pages/Main/HomePage.cs
[NavigationRoute("main/home", Flow = "Main")]
public partial class HomePage : ContentPage { }

// Pages/Auth/LoginPage.cs
[NavigationRoute("auth/login", Flow = "Auth")]
public partial class LoginPage : ContentPage { }

// Pages/Auth/RegisterPage.cs
[NavigationRoute("auth/register", Flow = "Auth")]
public partial class RegisterPage : ContentPage { }

// Pages/Shared/PrivacyPage.cs
[NavigationRoute("shared/privacy", IsShared = true)]
public partial class PrivacyPage : ContentPage { }
```

---

## GeneratedRoutes

The generator emits a `GeneratedRoutes` static class containing a `string` constant for every annotated page:

```csharp
// Generated — do not edit
public static class GeneratedRoutes
{
	public const string HomePage     = "main/home";
	public const string LoginPage    = "auth/login";
	public const string RegisterPage = "auth/register";
	public const string PrivacyPage  = "shared/privacy";
}
```

Use these constants instead of string literals to avoid triggering **SHAUNAV003 — Magic route string**:

```csharp
// ❌ SHAUNAV003
await _navigation.GoToAsync("main/home");

// ✅ Use the generated constant
await _navigation.GoToAsync(GeneratedRoutes.HomePage);

// ✅ Or the typed overload (preferred)
await _navigation.GoToAsync<HomePage>();
```

---

## GeneratedNavigationRegistration

A single static method registers all annotated pages in one call inside `UseShaunebuNavigation`:

```csharp
// MauiProgram.cs
builder.UseShaunebuNavigation(options =>
{
	options.DefaultNavigationMode    = NavigationPresentationMode.Shell;
	options.PreventDoubleNavigation  = true;
	options.EnableDiagnostics        = true;
	options.EnableNavigationGuards   = true;

	// Registers every [NavigationRoute]-decorated page
	GeneratedNavigationRegistration.RegisterGeneratedRoutes(options);
});
```

This replaces all manual `options.RegisterPage<TPage>(route, flow, ...)` calls for annotated pages. Manual registration is still required for pages that cannot have the `partial` modifier or the attribute applied (e.g. third-party pages).

---

## GeneratedNavigationExtensions

The generator emits a typed extension method for every annotated page. Each method wraps `INavigationHandler.GoToAsync<TPage>`:

```csharp
// Generated — do not edit
public static class GeneratedNavigationExtensions
{
	public static Task<NavigationResult> GoToHomePageAsync(
		this INavigationHandler handler,
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken  = default)
		=> handler.GoToAsync<HomePage>(configure, cancellationToken);

	public static Task<NavigationResult> GoToLoginPageAsync(
		this INavigationHandler handler,
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken  = default)
		=> handler.GoToAsync<LoginPage>(configure, cancellationToken);

	// ... one method per [NavigationRoute]-decorated page
}
```

Usage in ViewModels:

```csharp
// Typed — no strings, no casts
await _navigation.GoToHomePageAsync();

await _navigation.GoToSettingsPageAsync(opts =>
{
	opts.Animated = true;
	opts.Reason   = "User tapped settings";
});

var result = await _navigation.GoToLoginPageAsync(cancellationToken: ct);
if (!result.Succeeded)
	HandleFailure(result);
```

---

## NavigationRoute Attribute Reference

```csharp
[NavigationRoute(route, Flow = null, IsShared = false)]
public partial class MyPage : ContentPage { }
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `route` | `string` | required | Route string, e.g. `"main/home"`. Only alphanumeric characters, `/`, `-`, `_` are valid. |
| `Flow` | `string?` | `null` | Flow name (`"Auth"`, `"Main"`, …). `null` = global route. |
| `IsShared` | `bool` | `false` | Marks the page as a shared route accessible from any flow. When `true`, `Flow` should be `null` or omitted. |

---

## Shared Pages

A page registered as `IsShared = true` is accessible from any active flow without ambiguity:

```csharp
[NavigationRoute("shared/privacy", IsShared = true)]
public partial class PrivacyPage : ContentPage { }
```

If the same page type is registered under **multiple flows** (not `IsShared`), the registry uses flow-aware resolution:

```csharp
// Same page type, two flows
[NavigationRoute("auth/privacy",  Flow = "Auth")]
[NavigationRoute("main/privacy",  Flow = "Main")]
public partial class PrivacyPage : ContentPage { }
```

In this case `GoToAsync<PrivacyPage>()` selects the route matching the currently active flow. If no flow is active and both routes are registered, `NavigationResult.FailureReason` will be `AmbiguousRoute`.

---

## Viewing Generated Output

To inspect the generated files in Visual Studio:

1. Open **Solution Explorer**.
2. Expand your MAUI app project.
3. Expand **Dependencies → Analyzers → Shaunebu.MAUI.Navigation.Generators**.
4. Open any of the `.g.cs` files shown.

In the terminal:

```powershell
# Show generated files from the last build
Get-ChildItem -Recurse obj\Debug\net10.0-android\generated\ -Filter "*.g.cs"
```

---

## Generator Diagnostics

The generator emits compile-time diagnostics when pages are incorrectly annotated. See [Generator Diagnostics](diagnostics.md) for the SHAUNGEN001–SHAUNGEN007 reference.

---

## Related Pages

- [Generators Overview](overview.md)
- [Generator Diagnostics](diagnostics.md)
- [Navigation Routes](../navigation/navigation-routes.md)
- [Typed Navigation](../navigation/typed-navigation.md)
- [Analyzers Overview](../analyzers/overview.md)
