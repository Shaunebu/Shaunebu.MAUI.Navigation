# Advanced Topics

## Overview

This section covers advanced scenarios that go beyond the standard setup: testing strategies, custom pipeline extensions, multi-platform considerations, and production-safety patterns.

---

## Testing Navigation

### Unit Testing Guards

Guards are plain classes with an async method. Test them directly without any MAUI infrastructure:

```csharp
[Fact]
public async Task SampleAuthGuard_Redirects_WhenNotAuthenticated()
{
	var authService = new FakeAuthService(isAuthenticated: false);
	var guard       = new SampleAuthGuard(authService);

	var context = new NavigationGuardContext
	{
		TargetRoute    = "main/home",
		TargetPageType = typeof(HomePage)
	};

	var result = await guard.CanNavigateAsync(context);

	Assert.True(result.IsRedirect);
	Assert.Equal(typeof(LoginPage), result.RedirectToPageType);
}
```

### Unit Testing Flows

```csharp
[Fact]
public async Task AuthFlow_LogsEntryWithPreviousFlow()
{
	var logger = new FakeLogger<AuthFlow>();
	var flow   = new AuthFlow(logger);

	var ctx = new NavigationFlowContext { PreviousFlowName = "Main" };
	await flow.OnEnterAsync(ctx);

	Assert.Contains(logger.Messages, m => m.Contains("Main"));
}
```

### Integration Testing with NavigationStackSnapshot

Use `INavigationStackInspector` to assert stack state after navigation:

```csharp
var snapshot = _stackInspector.GetSnapshot();

Assert.Equal("main/home", snapshot.CurrentRoute);
Assert.Equal("Main", snapshot.CurrentFlow);
Assert.Single(snapshot.NavigationStack);
Assert.Empty(snapshot.ModalStack);
```

### Integration Testing with the Session Recorder

```csharp
await _recorder.ResetSessionAsync();

await _navigation.GoToAsync<HomePage>();
await _navigation.GoToAsync<ProductDetailsPage>();

var session = _recorder.CurrentSession;
Assert.Equal(2, session.Operations.Count);
Assert.Empty(session.Warnings);
```

---

## Custom Navigation Pipeline Extensions

The navigation pipeline calls `INavigationGuard.CanNavigateAsync` for every registered guard in DI registration order. To insert custom cross-cutting logic, create a guard that acts as middleware:

```csharp
public sealed class AnalyticsGuard : INavigationGuard
{
	private readonly IAnalyticsService _analytics;

	public AnalyticsGuard(IAnalyticsService analytics)
		=> _analytics = analytics;

	public async Task<NavigationGuardResult> CanNavigateAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken = default)
	{
		await _analytics.TrackNavigationAttemptAsync(context.TargetRoute, cancellationToken);
		return NavigationGuardResult.Allow();
	}
}
```

Register it first so it runs before any blocking guard:

```csharp
builder.Services.AddTransient<INavigationGuard, AnalyticsGuard>();
builder.Services.AddTransient<INavigationGuard, SampleMaintenanceGuard>();
builder.Services.AddTransient<INavigationGuard, SampleAuthGuard>();
```

---

## Preventing Double Navigation

The framework prevents duplicate navigation by default when `PreventDoubleNavigation = true`. The threshold controls how long (in milliseconds) a second identical navigation is treated as a duplicate:

```csharp
builder.UseShaunebuNavigation(opts =>
{
	opts.PreventDoubleNavigation    = true;
	opts.DoubleNavigationThreshold  = TimeSpan.FromMilliseconds(500);
});
```

When a duplicate is detected, navigation returns `NavigationResult` with `FailureReason = NavigationFailureReason.DuplicateNavigationPrevented`.

---

## Back Button Handling

When `EnableBackButtonHandling = true`, the framework intercepts hardware back-button presses and routes them through the guard pipeline. ViewModels implementing `IBackAware` can intercept:

```csharp
public sealed class EditProfileViewModel : BaseViewModel, IBackAware
{
	public Task<bool> CanGoBackAsync()
		=> Task.FromResult(!_hasUnsavedChanges);

	public async Task OnBackAsync()
	{
		// Custom logic when back is allowed
		await CleanupAsync();
	}
}
```

If `CanGoBackAsync` returns `false`, back navigation is blocked. `UnsavedChangesGuard` provides a higher-level version of this pattern with user confirmation.

---

## Shell Route Auto-Registration

With `RegisterShellRoutesAutomatically = true`, all routes defined in the MAUI `Shell` hierarchy (via `ShellContent.Route`) are automatically registered in the navigation route registry. This enables gradual adoption — existing Shell routes can be used with `INavigationHandler` without re-annotating every page:

```csharp
builder.UseShaunebuNavigation(opts =>
{
	opts.RegisterShellRoutesAutomatically = true;
	// Add [NavigationRoute] pages on top
	GeneratedNavigationRegistration.RegisterGeneratedRoutes(opts);
});
```

---

## Production Configuration

```csharp
builder.UseShaunebuNavigation(opts =>
{
	// Disable diagnostics overhead in Release
	opts.EnableDiagnostics        = false;

	// Use result-based failure mode (never throws in production)
	opts.ThrowOnNavigationFailure = false;

	// Limit guard redirect depth to prevent loops
	opts.MaxGuardRedirectDepth    = 5;

	// Prevent race conditions from rapid taps
	opts.PreventDoubleNavigation       = true;
	opts.DoubleNavigationThreshold     = TimeSpan.FromMilliseconds(300);

	GeneratedNavigationRegistration.RegisterGeneratedRoutes(opts);
});

// Debugger is always DEBUG-only
#if DEBUG
builder.Services.UseNavigationDebugger();
#endif
```

---

## Multi-Flow Startup

When the correct starting flow depends on runtime state (e.g. whether the user is authenticated), resolve `INavigationFlowManager` from DI and start the appropriate flow:

```csharp
// App.xaml.cs
protected override async void OnStart()
{
	var flowManager = ServiceProvider.GetRequiredService<INavigationFlowManager>();
	var authService = ServiceProvider.GetRequiredService<IAuthService>();

	if (authService.IsAuthenticated)
		await flowManager.StartFlowAsync<MainFlow>();
	else
		await flowManager.StartFlowAsync<AuthFlow>();
}
```

After login succeeds:

```csharp
// LoginViewModel
await _flowManager.CompleteCurrentFlowAsync(); // exits Auth flow
await _flowManager.StartFlowAsync<MainFlow>(); // enters Main flow
```

---

## Related Pages

- [Navigation Options](../navigation/navigation-options.md)
- [Guards Overview](../guards/overview.md)
- [Flows Overview](../flows/overview.md)
- [Back Navigation Diagnostics](../diagnostics/back-navigation-diagnostics.md)
- [Debugger Overview](../debugger/overview.md)
