# Getting Started

## Overview

This guide walks you through integrating `Shaunebu.MAUI.Navigation` into a new or existing .NET MAUI application from scratch. After completing this guide you will have:

- The navigation library registered in your app
- At least two pages annotated with `[NavigationRoute]`
- A ViewModel performing typed, result-checked navigation
- A guard that protects authenticated routes

> **Prerequisites:** .NET 9 or later, a working .NET MAUI project, and Visual Studio 2022 17.8+ or VS 2026.

---

## Step 1 — Install Packages

```xml
<!-- Your MAUI application .csproj -->
<PackageReference Include="Shaunebu.MAUI.Navigation" Version="1.0.0-preview.1" />
<PackageReference Include="Shaunebu.MAUI.Navigation.Generators" Version="1.0.0-preview.1" />

<!-- Optional: analyzers enforce best practices at compile time -->
<PackageReference Include="Shaunebu.MAUI.Navigation.Analyzers"  Version="1.0.0-preview.1" PrivateAssets="all" />
<PackageReference Include="Shaunebu.MAUI.Navigation.CodeFixes"  Version="1.0.0-preview.1" PrivateAssets="all" />

<!-- Debug only: runtime diagnostics -->
```

See [installation.md](installation.md) for full NuGet and VSIX instructions.

---

## Step 2 — Register the Library

Call `UseShaunebuNavigation` in `MauiProgram.cs` before building the app.

```csharp
// MauiProgram.cs
using Shaunebu.MAUI.Navigation.Extensions;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.UseShaunebuNavigation(options =>
		{
			options.DefaultNavigationMode     = NavigationPresentationMode.Shell;
			options.PreventDoubleNavigation   = true;
			options.EnableDiagnostics         = true;
			options.EnableNavigationGuards    = true;
			options.EnableOverlaySystem       = true;
			options.ThrowOnNavigationFailure  = false;   // always false in production

			// Let the source generator register all [NavigationRoute]-decorated pages
			GeneratedNavigationRegistration.RegisterGeneratedRoutes(options);
		});

#if DEBUG
		// Runtime debugger — never ship in release
		builder.Services.UseNavigationDebugger(opts =>
		{
			opts.EnableSessionRecording = true;
			opts.EnableRuntimeWarnings  = true;
			opts.EnableStackDiffing     = true;
		});
#endif

		// Guards
		builder.Services.AddSingleton<INavigationGuard, AppAuthGuard>();

		// Flows
		builder.Services.AddSingleton<AuthFlow>();
		builder.Services.AddSingleton<MainFlow>();
		builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<AuthFlow>());
		builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<MainFlow>());

		// Pages and ViewModels as transient
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<HomeViewModel>();

		return builder.Build();
	}
}
```

> **Why transient pages and ViewModels?** Each navigation call creates a fresh page instance. This avoids stale state on the back stack and ensures `OnNavigatedToAsync` fires with fresh data every time.

---

## Step 3 — Annotate Pages

The `[NavigationRoute]` attribute tells the source generator to emit route constants and typed registration helpers. The decorated class **must be `partial`**.

```csharp
// Pages/Auth/LoginPage.xaml.cs
[NavigationRoute("auth/login", Flow = "Auth")]
public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel vm) => BindingContext = vm;
}

// Pages/Main/HomePage.xaml.cs
[NavigationRoute("main/home", Flow = "Main")]
public partial class HomePage : ContentPage
{
	public HomePage(HomeViewModel vm) => BindingContext = vm;
}

// Pages/Shared/PrivacyPage.xaml.cs
[NavigationRoute("shared/privacy", IsShared = true)]
public partial class PrivacyPage : ContentPage
{
	public PrivacyPage(PrivacyViewModel vm) => BindingContext = vm;
}
```

At build time the generator emits `GeneratedNavigationRegistration.RegisterGeneratedRoutes(options)` which calls `options.RegisterPage<TPage>(route, flow, ...)` for every annotated page.

See [generators/generated-routes.md](../generators/generated-routes.md) for details on what is generated.

---

## Step 4 — Define Flows

Flows represent major application contexts. Implement `INavigationFlow`:

```csharp
// Flows/AuthFlow.cs
public sealed class AuthFlow : INavigationFlow
{
	public string Name            => "Auth";
	public Type   RootPageType    => typeof(LoginPage);
	public bool   RequiresAuthentication => false;

	public Task OnEnterAsync(NavigationFlowContext context) => Task.CompletedTask;
	public Task OnExitAsync(NavigationFlowContext context)  => Task.CompletedTask;
}

// Flows/MainFlow.cs
public sealed class MainFlow : INavigationFlow
{
	public string Name            => "Main";
	public Type   RootPageType    => typeof(HomePage);
	public bool   RequiresAuthentication => true;

	public Task OnEnterAsync(NavigationFlowContext context)
	{
		// Start background services, e.g. data sync
		return Task.CompletedTask;
	}

	public Task OnExitAsync(NavigationFlowContext context)
	{
		// Tear down session state
		return Task.CompletedTask;
	}
}
```

---

## Step 5 — Implement a Guard

```csharp
// Guards/AppAuthGuard.cs
public sealed class AppAuthGuard : AuthenticationGuard<LoginPage>
{
	private readonly IAuthService _auth;

	public AppAuthGuard(IAuthService auth) => _auth = auth;

	protected override Task<bool> IsAuthenticatedAsync(CancellationToken ct)
		=> Task.FromResult(_auth.IsAuthenticated);

	protected override Task<bool> RequiresAuthenticationAsync(
		NavigationGuardContext context, CancellationToken ct)
	{
		// Protect all routes starting with "main/"
		var requiresAuth = context.TargetRoute?.StartsWith("main/", StringComparison.Ordinal) == true;
		return Task.FromResult(requiresAuth);
	}
}
```

---

## Step 6 — Navigate from a ViewModel

```csharp
// ViewModels/Auth/LoginViewModel.cs
public sealed partial class LoginViewModel
{
	private readonly INavigationHandler     _navigation;
	private readonly INavigationFlowManager _flowManager;
	private readonly IAuthService           _auth;

	public LoginViewModel(
		INavigationHandler     navigation,
		INavigationFlowManager flowManager,
		IAuthService           auth)
	{
		_navigation  = navigation;
		_flowManager = flowManager;
		_auth        = auth;
	}

	public async Task LoginAsync(string username, string password)
	{
		await _auth.LoginAsync(username, password);

		// Reset root to main flow — clears auth back stack
		var result = await _flowManager.ResetToFlowAsync<MainFlow>();

		if (!result.Succeeded)
			await Shell.Current.DisplayAlert("Error", result.Message, "OK");
	}
}
```

---

## Step 7 — Implement INavigationAware (optional)

ViewModels that need to react to navigation events implement `INavigationAware`:

```csharp
public sealed partial class HomeViewModel : INavigationAware
{
	public async Task OnNavigatedToAsync(NavigationContext context)
	{
		// Reload dashboard data every time this page becomes active
		await LoadDashboardAsync();
	}

	public Task OnNavigatingFromAsync(NavigationContext context) => Task.CompletedTask;
	public Task OnNavigatedFromAsync(NavigationContext context)  => Task.CompletedTask;
}
```

---

## Next Steps

| Topic | Link |
|---|---|
| All `INavigationHandler` methods | [navigation/typed-navigation.md](../navigation/typed-navigation.md) |
| Available guard base classes | [guards/overview.md](../guards/overview.md) |
| Flow lifecycle in detail | [flows/overview.md](../flows/overview.md) |
| Overlay system | [overlays/overview.md](../overlays/overview.md) |
| Source generators | [generators/overview.md](../generators/overview.md) |
| Analyzer rules | [analyzers/overview.md](../analyzers/overview.md) |
| Sample application walkthrough | [samples/sample-application.md](../samples/sample-application.md) |
