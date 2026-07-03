# Source Generators Overview

## Overview

The `Shaunebu.MAUI.Navigation.Generators` package ships three incremental Roslyn source generators that eliminate navigation boilerplate at compile time:

| Generator | Trigger | Output |
|---|---|---|
| `NavigationRouteGenerator` | `[NavigationRoute]` on a `partial` page class | Route constants, registration helper, typed extension methods |
| `NavigationParametersGenerator` | `[NavigationParameters]` on a `partial record` | Parameters type catalog |
| `NavigationFlowGenerator` | `[NavigationFlow]` on a `partial` class | Flow descriptor catalog |

All generators are incremental — they only run when their relevant syntax nodes change, so they do not affect build performance for unrelated changes.

---

## Installation

```xml
<PackageReference Include="Shaunebu.MAUI.Navigation.Generators"
				  Version="1.0.0-preview.1"
				  PrivateAssets="all" />
```

See [Installation](../installation.md) for complete guidance.

---

## NavigationRouteGenerator

### Purpose

Discovers every `partial` page class decorated with `[NavigationRoute]` and emits three generated files:

| Generated type | Description |
|---|---|
| `GeneratedRoutes` | String constants per route, e.g. `GeneratedRoutes.HomePage` |
| `GeneratedNavigationRegistration` | `RegisterGeneratedRoutes(ShaunebuNavigationOptions)` helper |
| `GeneratedNavigationExtensions` | Typed `GoTo{PageName}Async` extension methods on `INavigationHandler` |

### Annotation

```csharp
// Pages/HomePage.cs
[NavigationRoute("main/home")]
public partial class HomePage : ContentPage { }

// Pages/LoginPage.cs
[NavigationRoute("auth/login", Flow = "Auth")]
public partial class LoginPage : ContentPage { }

// Shared page — appears in multiple flows
[NavigationRoute("shared/settings", IsShared = true)]
public partial class SettingsPage : ContentPage { }
```

### Startup registration

```csharp
// MauiProgram.cs
builder.UseShaunebuNavigation(opts =>
{
	GeneratedNavigationRegistration.RegisterGeneratedRoutes(opts);
});
```

### Using generated extensions

```csharp
// Typed navigation — no string literals
await _navigation.GoToHomePageAsync();
await _navigation.GoToLoginPageAsync(opts => opts.PresentationMode = Modal);
await _navigation.GoToSettingsPageAsync(configure, cancellationToken);
```

### Using route constants

```csharp
// When you need the raw route string
Console.WriteLine(GeneratedRoutes.HomePage);   // "main/home"
Console.WriteLine(GeneratedRoutes.LoginPage);  // "auth/login"
```

---

## NavigationParametersGenerator

Discovers `partial record` types decorated with `[NavigationParameters]` and emits a compile-time catalog that enables the framework's typed parameter resolution.

```csharp
[NavigationParameters]
public sealed partial record ProductDetailParameters(
	int ProductId,
	bool ReadOnly = false) : INavigationParameters;
```

Generated output provides type registration so parameters can be resolved from `NavigationContext.Parameters` in `INavigationAware.OnNavigatedToAsync`.

---

## NavigationFlowGenerator

Discovers `partial` classes decorated with `[NavigationFlow]` and emits `GeneratedFlowDescriptors.g.cs` — a compile-time catalog that maps flows to their page sets for runtime validation.

```csharp
[NavigationFlow("Auth")]
public sealed partial class AuthFlow : INavigationFlow
{
	public string Name           => "Auth";
	public Type   RootPageType   => typeof(LoginPage);
	public bool   RequiresAuthentication => false;

	public Task OnEnterAsync(NavigationFlowContext ctx, CancellationToken ct = default)
		=> Task.CompletedTask;

	public Task OnExitAsync(NavigationFlowContext ctx, CancellationToken ct = default)
		=> Task.CompletedTask;
}
```

---

## Generator Diagnostics

See [Generator Diagnostics](diagnostics.md) for the full SHAUNGEN001–SHAUNGEN007 reference.

---

## Related Pages

- [Generator Diagnostics](diagnostics.md)
- [Navigation Routes](../navigation/navigation-routes.md)
- [Flows — Creating Flows](../flows/creating-flows.md)
- [Analyzers Overview](../analyzers/overview.md)
