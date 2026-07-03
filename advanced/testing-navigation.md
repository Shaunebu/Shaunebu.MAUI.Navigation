# Testing Navigation

## Overview

`Shaunebu.MAUI.Navigation` is designed for testability. Guards, flows, ViewModels, and the full pipeline can be tested without a running MAUI process, an Android/iOS emulator, or any MAUI infrastructure.

This page covers unit testing guards and ViewModels, integration testing via `INavigationStackInspector` and `INavigationSessionRecorder`, and a reference `Fake`-based testing infrastructure pattern.

---

## Unit Testing Guards

Guards are plain classes with a single async method. Test them directly using any xUnit-compatible test framework:

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

[Fact]
public async Task SampleAuthGuard_Allows_WhenAuthenticated()
{
	var authService = new FakeAuthService(isAuthenticated: true);
	var guard       = new SampleAuthGuard(authService);

	var context = new NavigationGuardContext
	{
		TargetRoute    = "main/home",
		TargetPageType = typeof(HomePage)
	};

	var result = await guard.CanNavigateAsync(context);

	Assert.True(result.Allowed);
}
```

### FakeAuthService Pattern

```csharp
internal sealed class FakeAuthService : IAuthService
{
	private readonly bool _isAuthenticated;

	public FakeAuthService(bool isAuthenticated = false)
		=> _isAuthenticated = isAuthenticated;

	public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
		=> Task.FromResult(_isAuthenticated);

	public Task<bool> LoginAsync(string user, string password, CancellationToken ct = default)
		=> Task.FromResult(!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password));

	public Task LogoutAsync(CancellationToken ct = default)
		=> Task.CompletedTask;
}
```

---

## Unit Testing Flows

Flow lifecycle callbacks are plain async methods — test them without any pipeline involvement:

```csharp
[Fact]
public async Task AuthFlow_LogsEntryWithPreviousFlowName()
{
	var logger = new FakeLogger<AuthFlow>();
	var flow   = new AuthFlow(logger);

	var ctx = new NavigationFlowContext
	{
		Operation        = NavigationOperationFactory.CreateFlowTransition(typeof(AuthFlow)),
		FlowName         = "Auth",
		PreviousFlowName = "Main"
	};

	await flow.OnEnterAsync(ctx);

	Assert.Contains(logger.Messages, m => m.Contains("Main"));
}

[Fact]
public async Task AuthFlow_ExitLogsFlowName()
{
	var logger = new FakeLogger<AuthFlow>();
	var flow   = new AuthFlow(logger);

	var ctx = new NavigationFlowContext
	{
		Operation = NavigationOperationFactory.CreateFlowTransition(typeof(AuthFlow)),
		FlowName  = "Auth"
	};

	await flow.OnExitAsync(ctx);

	Assert.Contains(logger.Messages, m => m.Contains("Auth"));
}
```

---

## Unit Testing ViewModels

Inject fake implementations of `INavigationHandler` and `INavigationFlowManager` to test ViewModel commands without any MAUI infrastructure:

```csharp
internal sealed class FakeNavigationHandler : INavigationHandler
{
	public List<Type> GoToHistory { get; } = new();

	public Task<NavigationResult> GoToAsync<TPage>(
		Action<NavigationOptions>? configure   = null,
		CancellationToken          cancellation = default) where TPage : Page
	{
		GoToHistory.Add(typeof(TPage));
		return Task.FromResult(NavigationResult.Success(new NavigationOperation()));
	}

	// Implement remaining members as no-ops or throw NotImplementedException
	public Task<NavigationResult> SetRootAsync<TPage>(Action<NavigationOptions>? configure = null, CancellationToken ct = default) where TPage : Page
		=> Task.FromResult(NavigationResult.Success(new NavigationOperation()));

	public Task<NavigationResult> ShowModalAsync<TPage>(Action<NavigationOptions>? configure = null, CancellationToken ct = default) where TPage : Page
		=> Task.FromResult(NavigationResult.Success(new NavigationOperation()));

	public Task<NavigationResult> CloseModalAsync(Action<NavigationOptions>? configure = null, CancellationToken ct = default)
		=> Task.FromResult(NavigationResult.Success(new NavigationOperation()));

	public Task<NavigationResult> GoBackAsync(Action<NavigationBackOptions>? configure = null, CancellationToken ct = default)
		=> Task.FromResult(NavigationResult.Success(new NavigationOperation()));
}
```

```csharp
[Fact]
public async Task LoginViewModel_NavigatesToHomePage_OnSuccessfulLogin()
{
	var handler     = new FakeNavigationHandler();
	var flowManager = new FakeNavigationFlowManager();
	var overlay     = new FakeOverlayNavigationService();
	var authService = new FakeAuthService(isAuthenticated: true);

	var vm = new LoginViewModel(handler, flowManager, overlay, authService);
	vm.Username = "admin";
	vm.Password = "password";

	await vm.LoginCommand.ExecuteAsync(CancellationToken.None);

	Assert.True(flowManager.ResetToFlowCalled);
	Assert.Equal(typeof(MainFlow), flowManager.LastResetFlowType);
}
```

---

## Integration Testing with INavigationStackInspector

Use `INavigationStackInspector` in integration tests to assert stack state after navigation:

```csharp
[Fact]
public async Task PushSettings_AddsToNavigationStack()
{
	// Arrange — pipeline wired up with real in-memory adapters
	var (handler, inspector) = BuildTestPipeline();

	// Act
	await handler.GoToAsync<SettingsPage>();

	// Assert
	var snapshot = inspector.GetSnapshot();
	Assert.Equal("main/settings", snapshot.CurrentRoute);
	Assert.Equal(1, snapshot.NavigationStack.Count);
}
```

---

## Integration Testing with INavigationSessionRecorder

```csharp
[Fact]
public async Task MultiStepNavigation_RecordsAllOperations()
{
	var (handler, _, recorder) = BuildTestPipelineWithRecorder();

	await recorder.ResetSessionAsync();

	await handler.GoToAsync<HomePage>();
	await handler.GoToAsync<ProductDetailsPage>();
	await handler.GoBackAsync();

	var session = recorder.CurrentSession;

	Assert.Equal(3, session.Operations.Count);
	Assert.Empty(session.Warnings);
	Assert.Equal("main/home", session.Operations[0].Route);
}
```

---

## Integration Testing with NavigationExecutionPipeline

For lower-level pipeline tests, construct `NavigationExecutionPipeline` directly with fakes:

```csharp
private static NavigationExecutionPipeline BuildPipeline(
	INavigationRouteRegistry?  registry = null,
	IPageResolver?             resolver = null,
	INavigationPageAdapter?    adapter  = null,
	INavigationDiagnostics?    diag     = null)
{
	return new NavigationExecutionPipeline(
		routeRegistry:    registry  ?? new FakeNavigationRouteRegistry(),
		pageResolver:     resolver  ?? new FakePageResolver(),
		guardPipeline:    new NavigationGuardPipeline(Enumerable.Empty<INavigationGuard>()),
		navigationLock:   new NavigationLock(),
		pageAdapter:      adapter   ?? new FakeNavigationPageAdapter(),
		shellAdapter:     null,
		diagnostics:      diag      ?? new NullNavigationDiagnostics(),
		flowNameProvider: () => null,
		options:          new ShaunebuNavigationOptions());
}

[Fact]
public async Task ExecuteAsync_Succeeds_ForRegisteredRoute()
{
	var registry = new FakeNavigationRouteRegistry();
	registry.RegisterRoute<HomePage>("main/home", "Main");

	var pipeline = BuildPipeline(registry: registry);

	var result = await pipeline.ExecuteAsync(
		new NavigationRequest { TargetPageType = typeof(HomePage) },
		CancellationToken.None);

	Assert.True(result.Succeeded);
}
```

---

## Guard Pipeline Integration Tests

Test the full guard chain in sequence without MAUI infrastructure:

```csharp
[Fact]
public async Task GuardPipeline_StopsAtFirstReject()
{
	var guards = new INavigationGuard[]
	{
		new AlwaysAllowGuard(),
		new AlwaysRejectGuard("Blocked by policy"),
		new AlwaysAllowGuard()
	};

	var pipeline = new NavigationGuardPipeline(guards);
	var context  = new NavigationGuardContext { TargetRoute = "main/home" };

	var result = await pipeline.RunAsync(context);

	Assert.False(result.Allowed);
	Assert.Contains("Blocked by policy", result.Reason);
}
```

---

## Cancellation Testing

```csharp
[Fact]
public async Task GoToAsync_Cancelled_ReturnsFailureResult()
{
	var pipeline = BuildPipeline();
	var cts      = new CancellationTokenSource();
	cts.Cancel();

	var result = await pipeline.ExecuteAsync(
		new NavigationRequest { TargetPageType = typeof(HomePage) },
		cts.Token);

	Assert.False(result.Succeeded);
	Assert.Equal(NavigationFailureReason.Cancelled, result.FailureReason);
}
```

---

## FakeLogger\<T\>

A minimal logger implementation for guard/flow tests:

```csharp
internal sealed class FakeLogger<T> : ILogger<T>
{
	public List<string> Messages { get; } = new();

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(
		LogLevel                         logLevel,
		EventId                          eventId,
		TState                           state,
		Exception?                       exception,
		Func<TState, Exception?, string> formatter)
		=> Messages.Add(formatter(state, exception));
}
```

---

## Related Pages

- [Custom Guards](../guards/custom-guards.md)
- [Guards Overview](../guards/overview.md)
- [Flows — Creating Flows](../flows/creating-flows.md)
- [Advanced Topics](overview.md)
- [API Reference](../reference/api-reference.md)
