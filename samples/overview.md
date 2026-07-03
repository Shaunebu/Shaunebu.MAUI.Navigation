# Sample Application

## Overview

The `Shaunebu.MAUI.Navigation.Sample` project is a fully-wired MAUI application that demonstrates every framework feature in a realistic scenario — two flows (Auth and Main), guards, overlays, typed parameters, diagnostics, and debugger integration.

Use the sample as a reference implementation when onboarding to the framework.

---

## Project Structure

```
Shaunebu.MAUI.Navigation.Sample/
├── MauiProgram.cs              ← startup wiring
├── App.xaml / App.xaml.cs      ← MAUI app entry
├── AppShell.xaml / .cs         ← Shell definition
│
├── Flows/
│   ├── AuthFlow.cs             ← [NavigationFlow("Auth")]
│   └── MainFlow.cs             ← [NavigationFlow("Main")]
│
├── Guards/
│   ├── SampleAuthGuard.cs      ← AuthenticationGuard<LoginPage>
│   ├── SampleMaintenanceGuard.cs ← MaintenanceGuard<MaintenancePage>
│   └── EditProfileUnsavedChangesGuard.cs ← UnsavedChangesGuard
│
├── Pages/
│   ├── Auth/     LoginPage, RegisterPage
│   ├── Main/     HomePage, ProfilePage, EditProfilePage, SettingsPage, ProductDetailsPage
│   ├── Shared/   PrivacyPage, TermsPage
│   ├── Overlays/ LoadingOverlayPage, NoInternetOverlayPage
│   └── Debug/    DebuggerShellPage, DiagnosticsPage
│
├── ViewModels/
│   ├── Auth/     LoginViewModel, RegisterViewModel
│   ├── Main/     HomeViewModel, ProfileViewModel, EditProfileViewModel,
│   │             SettingsViewModel, ProductDetailsViewModel
│   └── Shared/   MaintenanceViewModel, PrivacyViewModel, TermsViewModel
│
├── Parameters/
│   └── ProductDetailsParameters.cs  ← [NavigationParameters] typed record
│
├── Services/
│   ├── IAuthService / InMemoryAuthService
│   ├── IMaintenanceService
│   └── OverlayHost.cs
│
└── AnalyzerValidation/
	└── AnalyzerValidationSandbox.cs ← intentional analyzer violations (suppressed)
```

---

## Startup Wiring (MauiProgram.cs)

```csharp
var builder = MauiApp.CreateBuilder();

builder.UseMauiApp<App>()
	   .UseShaunebuNavigation(opts =>
	   {
		   // Register all routes discovered by the source generator
		   GeneratedNavigationRegistration.RegisterGeneratedRoutes(opts);

		   // Global option overrides
		   opts.EnableDiagnostics = true;
		   opts.EnableNavigationGuards = true;
		   opts.EnableOverlaySystem = true;
		   opts.EnableBackButtonHandling = true;
		   opts.RegisterShellRoutesAutomatically = true;
	   });

// Guards (evaluated in registration order)
builder.Services.AddTransient<INavigationGuard, SampleMaintenanceGuard>();
builder.Services.AddTransient<INavigationGuard, SampleAuthGuard>();
builder.Services.AddTransient<INavigationGuard, EditProfileUnsavedChangesGuard>();

// Flows
builder.Services.AddTransient<INavigationFlow, AuthFlow>();
builder.Services.AddTransient<INavigationFlow, MainFlow>();

// Services
builder.Services.AddSingleton<IAuthService, InMemoryAuthService>();
builder.Services.AddSingleton<IMaintenanceService, StubMaintenanceService>();
builder.Services.AddSingleton<OverlayHost>();

// Pages and ViewModels
builder.Services.AddTransient<LoginPage>();
builder.Services.AddTransient<LoginViewModel>();
// ... (all pages and ViewModels registered as transient)

// Debugger (DEBUG only)
#if DEBUG
builder.Services.UseNavigationDebugger(opts =>
{
	opts.EnableSessionRecording = true;
	opts.EnableRuntimeWarnings  = true;
	opts.EnableStackDiffing     = true;
});
#endif
```

---

## Flows

### AuthFlow

```csharp
[NavigationFlow("Auth")]
public sealed partial class AuthFlow : INavigationFlow
{
	public string Name             => "Auth";
	public Type   RootPageType     => typeof(LoginPage);
	public bool   RequiresAuthentication => false;

	public async Task OnEnterAsync(NavigationFlowContext context)
		=> _logger.LogInformation("[AuthFlow] Entered. Previous: {Prev}", context.PreviousFlowName);

	public async Task OnExitAsync(NavigationFlowContext context)
		=> _logger.LogInformation("[AuthFlow] Exited.");
}
```

### MainFlow

```csharp
[NavigationFlow("Main")]
public sealed partial class MainFlow : INavigationFlow
{
	public string Name             => "Main";
	public Type   RootPageType     => typeof(HomePage);
	public bool   RequiresAuthentication => true;
	// ... OnEnterAsync / OnExitAsync
}
```

---

## Guards

### SampleAuthGuard

```csharp
public sealed class SampleAuthGuard : AuthenticationGuard<LoginPage>
{
	protected override Task<bool> IsAuthenticatedAsync(CancellationToken ct)
		=> Task.FromResult(_authService.IsAuthenticated);

	protected override Task<bool> RequiresAuthenticationAsync(
		NavigationGuardContext context, CancellationToken ct)
	{
		// Any route inside the "main/" prefix requires auth
		var requiresAuth = context.TargetRoute?.StartsWith("main/",
			StringComparison.OrdinalIgnoreCase) ?? false;
		return Task.FromResult(requiresAuth);
	}
}
```

### SampleMaintenanceGuard

```csharp
public sealed class SampleMaintenanceGuard : MaintenanceGuard<MaintenancePage>
{
	protected override Task<bool> IsMaintenanceModeActiveAsync(CancellationToken ct)
		=> Task.FromResult(_maintenanceService.IsActive);
}
```

### EditProfileUnsavedChangesGuard

```csharp
public sealed class EditProfileUnsavedChangesGuard : UnsavedChangesGuard
{
	protected override Task<bool> HasUnsavedChangesAsync(
		NavigationGuardContext context, CancellationToken ct)
	{
		// Only check when navigating away from EditProfilePage
		if (context.SourcePageType != typeof(EditProfilePage))
			return Task.FromResult(false);

		return Task.FromResult(_editProfileState.HasChanges);
	}

	protected override Task<bool> ConfirmDiscardAsync(CancellationToken ct)
		=> Application.Current!.MainPage!.DisplayAlert(
			"Unsaved changes",
			"You have unsaved changes. Discard them?",
			"Discard", "Cancel");
}
```

---

## Typed Navigation Parameters

```csharp
// Parameters record
[NavigationParameters]
public sealed partial record ProductDetailsParameters(
	Guid   ProductId,
	string ProductName) : INavigationParameters;

// Navigate with parameters
await _navigation.GoToAsync<ProductDetailsPage>(opts =>
{
	opts.Parameters = new ProductDetailsParameters(product.Id, product.Name);
});

// Receive in ViewModel
public async Task OnNavigatedToAsync(NavigationContext ctx, CancellationToken ct)
{
	if (ctx.Parameters is ProductDetailsParameters p)
	{
		ProductId   = p.ProductId;
		ProductName = p.ProductName;
	}
}
```

---

## Overlay Integration

```csharp
// LoadingOverlayPage is a ContentView registered as the loading overlay
// NoInternetOverlayPage is a ContentView registered as the no-internet overlay

// Show loading during an async operation
await _overlays.ShowLoadingAsync(new LoadingOverlayOptions { Message = "Loading..." });
try
{
	await _productService.LoadAsync();
}
finally
{
	await _overlays.HideLoadingAsync();
}
```

---

## Analyzer Validation Sandbox

`AnalyzerValidation/AnalyzerValidationSandbox.cs` intentionally contains suppressed violations of every SHAUNAV rule. This file is used to verify that the analyzers fire correctly and that code fixes apply cleanly.

---

## Running the Sample

```powershell
# Android emulator
dotnet build samples\Shaunebu.MAUI.Navigation.Sample -f net9.0-android
dotnet run --project samples\Shaunebu.MAUI.Navigation.Sample -f net9.0-android

# Windows (unpackaged)
dotnet run --project samples\Shaunebu.MAUI.Navigation.Sample -f net9.0-windows10.0.19041.0
```

---

## Related Pages

- [Getting Started](../getting-started.md)
- [Flows Overview](../flows/overview.md)
- [Guards Overview](../guards/overview.md)
- [Overlays Overview](../overlays/overview.md)
- [Debugger Overview](../debugger/overview.md)
