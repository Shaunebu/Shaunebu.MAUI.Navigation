# Maintenance Guard

## Overview

`MaintenanceGuard<TMaintenancePage>` is an abstract base class that blocks all navigation when the application is in maintenance mode, redirecting users to a dedicated maintenance page.

---

## Class Declaration

```csharp
public abstract class MaintenanceGuard<TMaintenancePage> : INavigationGuard
	where TMaintenancePage : Microsoft.Maui.Controls.Page
```

---

## Abstract Members to Implement

### IsMaintenanceModeActiveAsync

Returns whether the application is currently in maintenance mode:

```csharp
protected abstract Task<bool> IsMaintenanceModeActiveAsync(CancellationToken cancellationToken);
```

---

## Virtual Members to Override

### IsExemptFromMaintenance *(optional)*

Returns whether the target page type is exempt from the maintenance gate. The default implementation exempts only `TMaintenancePage` itself, preventing an infinite redirect loop:

```csharp
protected virtual bool IsExemptFromMaintenance(Type? targetPageType)
	=> targetPageType == typeof(TMaintenancePage);
```

Override to also exempt other pages (e.g., an error page or a static informational page):

```csharp
protected override bool IsExemptFromMaintenance(Type? targetPageType)
	=> targetPageType == typeof(MaintenancePage)
	|| targetPageType == typeof(StatusPage);
```

---

## Pipeline Behavior

When maintenance mode is active and the target page is not exempt, the guard returns:

```csharp
NavigationGuardResult.RedirectTo<TMaintenancePage>("Application is in maintenance mode.")
```

---

## Example Implementation

```csharp
// Guards/SampleMaintenanceGuard.cs
public sealed class SampleMaintenanceGuard : MaintenanceGuard<MaintenancePage>
{
	private readonly IMaintenanceService _maintenance;

	public SampleMaintenanceGuard(IMaintenanceService maintenance)
		=> _maintenance = maintenance;

	protected override Task<bool> IsMaintenanceModeActiveAsync(CancellationToken cancellationToken)
		=> _maintenance.IsMaintenanceModeActiveAsync(cancellationToken);
}
```

Register **before** the authentication guard so maintenance takes priority:

```csharp
// MauiProgram.cs — registration order matters
builder.Services.AddSingleton<INavigationGuard, SampleMaintenanceGuard>();
builder.Services.AddSingleton<INavigationGuard, SampleAuthGuard>();
```

---

## IMaintenanceService Pattern

```csharp
public interface IMaintenanceService
{
	Task<bool> IsMaintenanceModeActiveAsync(CancellationToken cancellationToken);
}

public sealed class InMemoryMaintenanceService : IMaintenanceService
{
	public bool IsInMaintenance { get; set; } = false;

	public Task<bool> IsMaintenanceModeActiveAsync(CancellationToken cancellationToken)
		=> Task.FromResult(IsInMaintenance);
}
```

In production, implement this against a remote configuration endpoint (e.g., Azure App Configuration).

---

## Related Pages

- [Guards Overview](overview.md)
- [Authentication Guard](authentication-guard.md)
- [Custom Guards](custom-guards.md)
