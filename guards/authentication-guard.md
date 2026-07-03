# Authentication Guard

## Overview

`AuthenticationGuard<TLoginPage>` is an abstract base class that blocks navigation to protected pages and redirects unauthenticated users to a login page. Subclass it and inject your authentication service.

---

## Class Declaration

```csharp
public abstract class AuthenticationGuard<TLoginPage> : INavigationGuard
	where TLoginPage : Microsoft.Maui.Controls.Page
```

---

## Abstract Members to Implement

### IsAuthenticatedAsync

Returns whether the current user is authenticated:

```csharp
protected abstract Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken);
```

### RequiresAuthenticationAsync *(virtual — override to gate specific routes)*

Returns whether the target navigation requires authentication. The default implementation returns `false` (allows all navigation). Override to apply route-specific protection:

```csharp
protected virtual Task<bool> RequiresAuthenticationAsync(
	NavigationGuardContext context,
	CancellationToken cancellationToken)
	=> Task.FromResult(false);
```

---

## Pipeline Behavior

When both `RequiresAuthenticationAsync` returns `true` and `IsAuthenticatedAsync` returns `false`, the guard returns:

```csharp
NavigationGuardResult.RedirectTo<TLoginPage>("Authentication required. Redirecting to login.")
```

The `TLoginPage` itself is implicitly exempt from the redirect (the base class does not check `RequiresAuthenticationAsync` for the login page).

---

## Example Implementation

```csharp
// Guards/AppAuthGuard.cs
public sealed class AppAuthGuard : AuthenticationGuard<LoginPage>
{
	private readonly IAuthService _auth;

	public AppAuthGuard(IAuthService auth) => _auth = auth;

	protected override Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken)
		=> Task.FromResult(_auth.IsAuthenticated);

	protected override Task<bool> RequiresAuthenticationAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken)
	{
		// Gate all routes under "main/"
		var requiresAuth = context.TargetRoute?.StartsWith("main/", StringComparison.Ordinal) == true;
		return Task.FromResult(requiresAuth);
	}
}
```

Register in `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<INavigationGuard, AppAuthGuard>();
```

---

## Route-Based vs Attribute-Based Gating

The `RequiresAuthenticationAsync` override can gate routes by:

**Route prefix:**
```csharp
var requiresAuth = context.TargetRoute?.StartsWith("main/", StringComparison.Ordinal) == true;
```

**Page type:**
```csharp
var protectedPages = new HashSet<Type> { typeof(HomePage), typeof(SettingsPage) };
var requiresAuth   = context.TargetPageType is not null && protectedPages.Contains(context.TargetPageType);
```

**Route descriptor flag (via `NavigationRouteDescriptor.RequiresAuthentication`):**

When routes are registered with `requiresAuthentication: true`:
```csharp
options.RegisterPage<HomePage>("main/home", requiresAuthentication: true);
```

The guard context does not expose this flag directly; use route prefix checks or the `RegisterPage` metadata via your own route registry extension.

---

## Sample App Implementation

The sample app ships `SampleAuthGuard`:

```csharp
public sealed class SampleAuthGuard : AuthenticationGuard<LoginPage>
{
	private readonly IAuthService _auth;

	public SampleAuthGuard(IAuthService auth) => _auth = auth;

	protected override Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken)
		=> Task.FromResult(_auth.IsAuthenticated);

	protected override Task<bool> RequiresAuthenticationAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken)
		=> Task.FromResult(
			context.TargetRoute?.StartsWith("main/", StringComparison.Ordinal) == true);
}
```

---

## Related Pages

- [Guards Overview](overview.md)
- [Custom Guards](custom-guards.md)
- [Flows Overview](../flows/overview.md)
