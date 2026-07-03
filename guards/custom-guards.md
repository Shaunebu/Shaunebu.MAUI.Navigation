# Custom Guards

## Overview

Any class that implements `INavigationGuard` can be registered as a guard. This page covers patterns for building custom guards beyond the built-in base classes.

---

## Minimal Custom Guard

```csharp
public sealed class RoleBasedGuard : INavigationGuard
{
	private readonly IAuthService _auth;

	public RoleBasedGuard(IAuthService auth) => _auth = auth;

	public async Task<NavigationGuardResult> CanNavigateAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken = default)
	{
		// Allow non-admin routes
		if (context.TargetRoute?.StartsWith("admin/", StringComparison.Ordinal) != true)
			return NavigationGuardResult.Allow();

		var user = await _auth.GetCurrentUserAsync(cancellationToken);
		if (user?.IsAdmin == true)
			return NavigationGuardResult.Allow();

		return NavigationGuardResult.Reject("Administrator role required.");
	}
}
```

---

## NavigationGuardResult Factory Methods

```csharp
// Allow the navigation to proceed
NavigationGuardResult.Allow()

// Reject and return NavigationFailureReason.GuardRejected
NavigationGuardResult.Reject("reason string")

// Redirect to a different page (can chain further guards)
NavigationGuardResult.RedirectTo<LoginPage>("reason string")
NavigationGuardResult.RedirectTo<LoginPage>()  // reason is optional
```

---

## Guard Registration Order

Guards are evaluated in **registration order**. The first `Reject` or `RedirectTo` result stops the pipeline. Register high-priority guards first:

```csharp
// 1. Maintenance mode — highest priority
builder.Services.AddSingleton<INavigationGuard, MaintenanceGuard>();
// 2. Authentication — second
builder.Services.AddSingleton<INavigationGuard, AuthGuard>();
// 3. Role checks — after auth is confirmed
builder.Services.AddSingleton<INavigationGuard, RoleBasedGuard>();
// 4. Unsaved changes — last, lowest priority
builder.Services.AddSingleton<INavigationGuard, EditProfileUnsavedChangesGuard>();
```

---

## Stateless vs Stateful Guards

| Type | DI Lifetime | Use Case |
|---|---|---|
| Stateless | `AddSingleton` | Guards that only depend on injected services |
| Stateful | `AddTransient` | Guards that carry per-navigation state |
| Scoped | `AddScoped` | Guards that share state within a navigation scope |

Most guards should be stateless singletons.

---

## Parameterized Guard

Pass guard-specific configuration via the DI container:

```csharp
public sealed class RouteAllowListGuard : INavigationGuard
{
	private readonly IReadOnlySet<string> _allowedRoutes;

	public RouteAllowListGuard(IReadOnlySet<string> allowedRoutes)
		=> _allowedRoutes = allowedRoutes;

	public Task<NavigationGuardResult> CanNavigateAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken = default)
	{
		if (context.TargetRoute is null || _allowedRoutes.Contains(context.TargetRoute))
			return Task.FromResult(NavigationGuardResult.Allow());

		return Task.FromResult(NavigationGuardResult.Reject(
			$"Route '{context.TargetRoute}' is not in the allow list."));
	}
}

// Registration with configuration
var allowedRoutes = new HashSet<string>(StringComparer.Ordinal)
{
	"auth/login", "main/home", "shared/privacy"
};
builder.Services.AddSingleton<IReadOnlySet<string>>(allowedRoutes);
builder.Services.AddSingleton<INavigationGuard, RouteAllowListGuard>();
```

---

## Guard Context Reference

```csharp
public sealed class NavigationGuardContext
{
	public Type?   TargetPageType  { get; init; }  // e.g., typeof(HomePage)
	public string? TargetRoute     { get; init; }  // e.g., "main/home"
	public NavigationPresentationMode PresentationMode { get; init; }
	public IReadOnlyDictionary<string, object?> Parameters { get; init; }
	public INavigationFlow? CurrentFlow { get; init; }
}
```

---

## Unit Testing a Guard

```csharp
[Fact]
public async Task RoleBasedGuard_RejectsNonAdminUserOnAdminRoute()
{
	var auth  = new FakeAuthService { IsAdmin = false };
	var guard = new RoleBasedGuard(auth);

	var context = new NavigationGuardContext
	{
		TargetRoute   = "admin/users",
		TargetPageType = typeof(AdminUsersPage)
	};

	var result = await guard.CanNavigateAsync(context);

	Assert.False(result.Allowed);
	Assert.Contains("Administrator role required", result.Reason);
}
```

---

## Related Pages

- [Guards Overview](overview.md)
- [Authentication Guard](authentication-guard.md)
- [Maintenance Guard](maintenance-guard.md)
- [Internet Required Guard](internet-required-guard.md)
- [Unsaved Changes Guard](unsaved-changes-guard.md)
- [Testing Navigation](../advanced/testing-navigation.md)
