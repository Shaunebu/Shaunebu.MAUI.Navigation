# Creating Flows

## Overview

A flow is created by implementing `INavigationFlow`. The interface declares the flow's identity, root page, and entry/exit lifecycle hooks.

---

## INavigationFlow Interface

```csharp
public interface INavigationFlow
{
	/// <summary>The unique name of this flow (e.g. "Auth", "Main").</summary>
	string Name { get; }

	/// <summary>The page type that serves as the root when this flow is entered.</summary>
	Type RootPageType { get; }

	/// <summary>Whether this flow requires authenticated users.</summary>
	bool RequiresAuthentication { get; }

	/// <summary>Called when this flow is entered via StartFlowAsync or ResetToFlowAsync.</summary>
	Task OnEnterAsync(NavigationFlowContext context);

	/// <summary>Called when this flow is exited via ResetToFlowAsync or CompleteCurrentFlowAsync.</summary>
	Task OnExitAsync(NavigationFlowContext context);
}
```

---

## Minimal Flow Implementation

```csharp
// Flows/AuthFlow.cs
public sealed class AuthFlow : INavigationFlow
{
	public string Name                   => "Auth";
	public Type   RootPageType           => typeof(LoginPage);
	public bool   RequiresAuthentication => false;

	public Task OnEnterAsync(NavigationFlowContext context) => Task.CompletedTask;
	public Task OnExitAsync(NavigationFlowContext context)  => Task.CompletedTask;
}
```

---

## Flow with Lifecycle Hooks

```csharp
// Flows/MainFlow.cs
public sealed class MainFlow : INavigationFlow
{
	private readonly IDataSyncService _sync;
	private readonly IAnalyticsService _analytics;

	public MainFlow(IDataSyncService sync, IAnalyticsService analytics)
	{
		_sync      = sync;
		_analytics = analytics;
	}

	public string Name                   => "Main";
	public Type   RootPageType           => typeof(HomePage);
	public bool   RequiresAuthentication => true;

	public async Task OnEnterAsync(NavigationFlowContext context)
	{
		// Start background sync when main flow begins
		await _sync.StartAsync();
		_analytics.TrackEvent("flow_entered", new { flow = Name });
	}

	public async Task OnExitAsync(NavigationFlowContext context)
	{
		// Flush analytics and stop sync on logout
		await _sync.StopAsync();
		_analytics.TrackEvent("flow_exited", new { flow = Name });
	}
}
```

---

## Onboarding Flow

A first-run flow that completes and hands off to `MainFlow`:

```csharp
// Flows/OnboardingFlow.cs
public sealed class OnboardingFlow : INavigationFlow
{
	private readonly IOnboardingService _onboarding;

	public OnboardingFlow(IOnboardingService onboarding) => _onboarding = onboarding;

	public string Name                   => "Onboarding";
	public Type   RootPageType           => typeof(WelcomePage);
	public bool   RequiresAuthentication => false;

	public Task OnEnterAsync(NavigationFlowContext context) => Task.CompletedTask;

	public async Task OnExitAsync(NavigationFlowContext context)
	{
		// Mark onboarding complete so the app doesn't re-enter it
		await _onboarding.MarkCompletedAsync();
	}
}
```

In the ViewModel that handles the final onboarding step:

```csharp
public async Task CompleteOnboardingAsync()
{
	// CompleteCurrentFlowAsync exits the onboarding flow,
	// fires OnExitAsync, and returns to the previous flow in history.
	await _flowManager.CompleteCurrentFlowAsync();
}
```

---

## Flow Route Registration

Routes belonging to a flow are registered with the flow name:

```csharp
// Option A: Generator attribute (recommended)
[NavigationRoute("auth/login",    Flow = "Auth")]
public partial class LoginPage : ContentPage { }

[NavigationRoute("auth/register", Flow = "Auth")]
public partial class RegisterPage : ContentPage { }

[NavigationRoute("main/home",     Flow = "Main")]
public partial class HomePage : ContentPage { }

// Option B: Manual
builder.UseShaunebuNavigation(options =>
{
	options.RegisterPage<LoginPage>("auth/login",       flow: "Auth");
	options.RegisterPage<RegisterPage>("auth/register", flow: "Auth");
	options.RegisterPage<HomePage>("main/home",         flow: "Main");
});
```

---

## DI Registration

Register each flow as both its concrete type and `INavigationFlow`:

```csharp
// MauiProgram.cs
builder.Services.AddSingleton<AuthFlow>();
builder.Services.AddSingleton<MainFlow>();
builder.Services.AddSingleton<OnboardingFlow>();

builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<AuthFlow>());
builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<MainFlow>());
builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<OnboardingFlow>());
```

The flow manager resolves by concrete type (`GetService(typeof(TFlow))`) for `ResetToFlowAsync<TFlow>()` and `StartFlowAsync<TFlow>()`.

---

## Related Pages

- [Flows Overview](overview.md)
- [Flow Manager](flow-manager.md)
- [Flow Context](flow-context.md)
- [Navigation Routes](../navigation/navigation-routes.md)
