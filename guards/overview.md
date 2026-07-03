# Guards Overview

## Overview

Navigation guards are the policy layer of the navigation pipeline. A guard evaluates whether a requested navigation should be allowed to proceed, rejected, or redirected to a different page.

Guards are evaluated synchronously (but async-capable) **after** the duplicate-navigation check and **before** the navigation adapter executes. Multiple guards may be registered; **all must allow** the navigation for it to succeed.

---

## The Guard Contract

```csharp
public interface INavigationGuard
{
	Task<NavigationGuardResult> CanNavigateAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken = default);
}
```

Each guard returns one of three outcomes:

| Outcome | Factory Method | Description |
|---|---|---|
| Allow | `NavigationGuardResult.Allow()` | Navigation proceeds normally |
| Reject | `NavigationGuardResult.Reject(string reason)` | Navigation is blocked; `NavigationFailureReason.GuardRejected` is returned |
| Redirect | `NavigationGuardResult.RedirectTo<TPage>(string? reason)` | Navigation is redirected to a different page |

---

## NavigationGuardContext

Guards receive a `NavigationGuardContext` describing the pending navigation:

```csharp
public sealed class NavigationGuardContext
{
	public Type?   TargetPageType { get; init; }
	public string? TargetRoute    { get; init; }
	public NavigationPresentationMode PresentationMode { get; init; }
	public IReadOnlyDictionary<string, object?> Parameters { get; init; }
	public INavigationFlow? CurrentFlow { get; init; }
}
```

---

## Registering Guards

Guards are registered with the DI container. They are resolved and evaluated in **registration order**:

```csharp
// MauiProgram.cs
// SampleMaintenanceGuard runs first — maintenance mode takes priority
builder.Services.AddSingleton<INavigationGuard, SampleMaintenanceGuard>();
builder.Services.AddSingleton<INavigationGuard, SampleAuthGuard>();
builder.Services.AddSingleton<INavigationGuard, EditProfileUnsavedChangesGuard>();
```

> Use `AddSingleton` for stateless guards and `AddTransient` for guards that carry per-navigation state.

---

## Built-in Base Guard Classes

The library provides abstract base classes for the most common guard scenarios:

| Class | Description |
|---|---|
| `AuthenticationGuard<TLoginPage>` | Redirects unauthenticated users to the login page |
| `MaintenanceGuard<TMaintenancePage>` | Blocks all navigation during maintenance mode |
| `InternetRequiredGuard<TOfflinePage>` | Blocks navigation to online-only pages when offline |
| `UnsavedChangesGuard` | Prevents navigation away from pages with unsaved changes |

---

## Guard Redirect Loop Protection

If a guard repeatedly redirects navigation in a cycle, the pipeline aborts after `ShaunebuNavigationOptions.MaxGuardRedirectDepth` consecutive redirects (default: 5) and returns `NavigationFailureReason.GuardRedirectLoop`.

To prevent redirect loops, ensure guards exempt their own redirect target:

```csharp
// Guard exempts LoginPage from triggering another auth redirect
if (context.TargetPageType == typeof(LoginPage))
	return NavigationGuardResult.Allow();
```

The `AuthenticationGuard` and `MaintenanceGuard` base classes handle this automatically.

---

## Writing a Custom Guard

```csharp
public sealed class FeatureFlagGuard : INavigationGuard
{
	private readonly IFeatureFlagService _flags;

	public FeatureFlagGuard(IFeatureFlagService flags) => _flags = flags;

	public async Task<NavigationGuardResult> CanNavigateAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken = default)
	{
		// Only gate "beta/" routes
		if (context.TargetRoute?.StartsWith("beta/", StringComparison.Ordinal) != true)
			return NavigationGuardResult.Allow();

		var enabled = await _flags.IsEnabledAsync("beta-features", cancellationToken);
		if (enabled)
			return NavigationGuardResult.Allow();

		return NavigationGuardResult.Reject("Beta feature flag is not enabled.");
	}
}
```

Register:
```csharp
builder.Services.AddSingleton<INavigationGuard, FeatureFlagGuard>();
```

---

## Disabling the Guard Pipeline

The guard pipeline can be disabled globally (not recommended for production):

```csharp
builder.UseShaunebuNavigation(options =>
{
	options.EnableNavigationGuards = false;
});
```

---

## Related Pages

- [Authentication Guard](authentication-guard.md)
- [Maintenance Guard](maintenance-guard.md)
- [Internet Required Guard](internet-required-guard.md)
- [Unsaved Changes Guard](unsaved-changes-guard.md)
- [Custom Guards](custom-guards.md)
- [Typed Navigation](../navigation/typed-navigation.md)
- [Navigation Results](../navigation/navigation-results.md)
