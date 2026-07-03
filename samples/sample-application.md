# Sample Application Walkthrough

## Overview

This page walks through the `Shaunebu.MAUI.Navigation.Sample` project end-to-end — from startup through each navigation scenario the sample demonstrates. Use it as a reference when integrating the library into a new application.

The sample project is located at:

```
samples/Shaunebu.MAUI.Navigation.Sample/
```

---

## Prerequisites

- .NET 9 SDK or later
- Visual Studio 2022 17.8+ or Visual Studio 2026
- Android SDK for emulator/device testing (or iOS SDK on macOS)

---

## Running the Sample

```powershell
# From the repository root
dotnet build samples/Shaunebu.MAUI.Navigation.Sample/Shaunebu.MAUI.Navigation.Sample.csproj

# Run on Android emulator
dotnet run --project samples/Shaunebu.MAUI.Navigation.Sample \
	-f net10.0-android
```

Or open `Shaunebu.MAUI.Navigation.slnx` in Visual Studio, set `Shaunebu.MAUI.Navigation.Sample` as the startup project, and press F5.

---

## Application Flows

The sample is built around two named flows:

| Flow | Root Page | Purpose |
|---|---|---|
| `Auth` | `LoginPage` | Unauthenticated users — login and registration |
| `Main` | `HomePage` | Authenticated users — home, profile, settings, products |

On startup, the app starts the `Auth` flow. Successful login resets to the `Main` flow.

---

## Startup Sequence

1. `MauiProgram.cs` calls `UseShaunebuNavigation` — registers routes, guards, flows, pages, and ViewModels.
2. `AppShell.xaml.cs` resolves `INavigationFlowManager` from DI and calls `StartFlowAsync<AuthFlow>()` after the first Shell render.
3. `AuthFlow.OnEnterAsync` logs the flow entry.
4. `LoginPage` is presented as the root.

---

## Scenario 1 — Login

**User story:** User enters credentials and is redirected to the authenticated home screen.

**Pages involved:** `LoginPage` → `HomePage`

**ViewModel:** `LoginViewModel`

```csharp
private async Task LoginAsync(CancellationToken cancellationToken)
{
	var success = await _authService.LoginAsync(Username, Password, cancellationToken);

	if (!success)
	{
		StatusMessage = "Invalid credentials.";
		return;
	}

	// Switch to the authenticated root — clears the auth back stack.
	var result = await _flowManager.ResetToFlowAsync<MainFlow>(
		options =>
		{
			options.ClearBackStack = true;
			options.Animated       = true;
			options.Reason         = "User authenticated";
		},
		cancellationToken);

	if (!result.Succeeded)
		StatusMessage = $"Navigation failed: {result.Message}";
}
```

**Key framework features demonstrated:**
- `INavigationFlowManager.ResetToFlowAsync<MainFlow>()` — clears the auth back stack and sets `HomePage` as the new root.
- `NavigationResult` structured error handling.
- `IOverlayNavigationService.ShowLoadingAsync` / `HideLoadingAsync` — loading overlay wrapping the auth call.

---

## Scenario 2 — Register

**User story:** User navigates from the login screen to create a new account.

**Pages involved:** `LoginPage` → `RegisterPage`

**ViewModel:** `LoginViewModel.GoToRegisterAsync`

```csharp
private Task GoToRegisterAsync(CancellationToken cancellationToken)
	=> _navigation.GoToAsync<RegisterPage>(
		options => options.Animated = true,
		cancellationToken);
```

**Key framework features demonstrated:**
- `INavigationHandler.GoToAsync<TPage>()` — typed push navigation without route strings.

---

## Scenario 3 — Privacy Policy (Modal)

**User story:** User views the privacy policy from the login screen. The page is shared between `Auth` and `Main` flows.

**Pages involved:** `LoginPage` → `PrivacyPage` (shared, modal)

**ViewModel:** `LoginViewModel.OpenPrivacyModalAsync`

```csharp
private Task OpenPrivacyModalAsync(CancellationToken cancellationToken)
	=> _navigation.GoToAsync<PrivacyPage>(
		options =>
		{
			options.PresentationMode = NavigationPresentationMode.Shell;
			options.Animated         = true;
			options.Reason           = "Privacy opened from Login";
		},
		cancellationToken);
```

**Key framework features demonstrated:**
- Shared page (`IsShared = true`) accessible from both `Auth` and `Main` flows.
- `NavigationPresentationMode.Shell` for Shell-based presentation.

---

## Scenario 4 — Edit Profile (Unsaved Changes Guard)

**User story:** User edits their profile. If they try to navigate away with unsaved changes, they see a confirmation dialog.

**Pages involved:** `ProfilePage` → `EditProfilePage`

**Guard:** `EditProfileUnsavedChangesGuard`

```csharp
public sealed class EditProfileUnsavedChangesGuard : UnsavedChangesGuard
{
	protected override bool HasUnsavedChanges(NavigationGuardContext context)
	{
		if (context.CurrentPage is not EditProfilePage editPage)
			return false;
		return editPage.BindingContext is EditProfileViewModel { HasChanges: true };
	}

	protected override string ConfirmationMessage
		=> "You have unsaved changes. Discard and leave?";
}
```

**Key framework features demonstrated:**
- `UnsavedChangesGuard` base class.
- Guard registered as the last guard in DI order (lowest priority).
- `EditProfileViewModel.HasChanges` property controls guard behavior.

---

## Scenario 5 — Maintenance Mode

**User story:** When the maintenance flag is set, all navigation to `Main` flow pages is blocked and the user is redirected to `MaintenancePage`.

**Guard:** `SampleMaintenanceGuard`

```csharp
public sealed class SampleMaintenanceGuard : MaintenanceGuard<MaintenancePage>
{
	private readonly IMaintenanceService _maintenance;

	public SampleMaintenanceGuard(IMaintenanceService maintenance)
		=> _maintenance = maintenance;

	protected override Task<bool> IsMaintenanceModeActiveAsync(
		NavigationGuardContext context,
		CancellationToken      cancellationToken = default)
		=> _maintenance.IsMaintenanceActiveAsync(cancellationToken);
}
```

**Key framework features demonstrated:**
- `MaintenanceGuard<TPage>` base class.
- Guard registered first (highest priority).
- `IMaintenanceService` is a swappable in-memory stub in the sample.

---

## Scenario 6 — Product Details (Typed Parameters)

**User story:** User taps a product card on the home screen. The `ProductDetailsPage` receives a typed `ProductDetailsParameters` record.

**Pages involved:** `HomePage` → `ProductDetailsPage`

**Parameters:**

```csharp
[NavigationParameters]
public sealed partial record ProductDetailsParameters(
	int  ProductId,
	bool ReadOnly = false) : INavigationParameters;
```

**ViewModel:**

```csharp
public async Task OnNavigatedToAsync(NavigationContext context, CancellationToken ct)
{
	var p = context.Parameters.Get<ProductDetailsParameters>();
	ProductId = p.ProductId;
	ReadOnly  = p.ReadOnly;
	await LoadProductAsync(ProductId, ct);
}
```

**Key framework features demonstrated:**
- `[NavigationParameters]` source generator.
- `INavigationAware.OnNavigatedToAsync` lifecycle callback.
- Typed parameter resolution from `NavigationContext.Parameters`.

---

## Scenario 7 — Logout

**User story:** User logs out from Settings. The app resets to the Auth flow.

**Pages involved:** `SettingsPage` → `LoginPage`

**ViewModel:** `SettingsViewModel.LogoutAsync`

```csharp
private async Task LogoutAsync(CancellationToken cancellationToken)
{
	await _authService.LogoutAsync(cancellationToken);

	var result = await _flowManager.ResetToFlowAsync<AuthFlow>(
		opts => opts.ClearBackStack = true,
		cancellationToken);

	if (!result.Succeeded)
		StatusMessage = $"Logout failed: {result.Message}";
}
```

**Key framework features demonstrated:**
- `ResetToFlowAsync<AuthFlow>()` with `ClearBackStack = true` — wipes the Main back stack.
- `AuthFlow.OnEnterAsync` / `MainFlow.OnExitAsync` lifecycle callbacks are invoked automatically.

---

## Scenario 8 — Diagnostics Page (DEBUG)

**User story:** In DEBUG builds, a diagnostics page is accessible from the flyout menu. It shows live navigation state.

**Page:** `DiagnosticsPage`  
**ViewModel:** `DiagnosticsViewModel`

```csharp
// MauiProgram.cs — registered only in DEBUG
#if DEBUG
builder.Services.AddTransient<DiagnosticsPage>();
builder.Services.AddTransient<DiagnosticsViewModel>();
#endif
```

**Key framework features demonstrated:**
- `INavigationStackInspector.GetSnapshot()` — real-time stack inspection.
- `INavigationSessionRecorder.CurrentSession` — live operation history.
- Conditional DI registration for debug-only pages.

---

## Guards in Registration Order

```
1. SampleMaintenanceGuard   (highest priority — block all traffic during maintenance)
2. SampleAuthGuard          (block unauthenticated access to Main flow pages)
3. EditProfileUnsavedChangesGuard  (lowest priority — only blocks back navigation from edit)
```

---

## Running the Tests

The sample has companion integration tests in the `UITests` projects:

```powershell
# Run all unit + integration tests
dotnet test tests/Shaunebu.MAUI.Navigation.Tests/Shaunebu.MAUI.Navigation.Tests.csproj

# Run UI tests on Android (requires emulator)
dotnet test Shaunebu.MAUI.Navigation.UITests/UITests.Android/UITests.Android.csproj
```

---

## Related Pages

- [Sample Application Overview](overview.md)
- [Getting Started](../getting-started.md)
- [Flows — Creating Flows](../flows/creating-flows.md)
- [Guards Overview](../guards/overview.md)
- [Typed Navigation](../navigation/typed-navigation.md)
- [Overlays Overview](../overlays/overview.md)
- [Advanced — Testing Navigation](../advanced/testing-navigation.md)
