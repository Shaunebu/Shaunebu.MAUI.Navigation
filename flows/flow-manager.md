# Flow Manager

## Overview

`INavigationFlowManager` is the service for managing application-level navigation flows. Inject it into ViewModels or services that need to start, reset, or complete flows.

---

## Interface

```csharp
public interface INavigationFlowManager
{
	INavigationFlow?               CurrentFlow  { get; }
	IReadOnlyList<INavigationFlow> FlowHistory  { get; }

	Task<NavigationResult> StartFlowAsync<TFlow>(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken  = default)
		where TFlow : class, INavigationFlow;

	Task<NavigationResult> ResetToFlowAsync<TFlow>(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken  = default)
		where TFlow : class, INavigationFlow;

	Task<NavigationResult> CompleteCurrentFlowAsync(
		CancellationToken cancellationToken = default);
}
```

---

## Properties

### CurrentFlow

Returns the currently active `INavigationFlow`, or `null` if no flow is active:

```csharp
var flow = _flowManager.CurrentFlow;
Console.WriteLine(flow?.Name ?? "No flow active");
```

### FlowHistory

Returns the ordered list of flows entered during the current session. Useful for debugging and analytics:

```csharp
foreach (var f in _flowManager.FlowHistory)
	Console.WriteLine($"Visited flow: {f.Name}");
```

---

## Methods

### ResetToFlowAsync\<TFlow\>

The most common transition method. Resets the **entire** navigation state and starts `TFlow` from scratch:

1. Calls `OnExitAsync` on the current flow (if any)
2. Clears the back stack
3. Sets `TFlow.RootPageType` as the new root
4. Calls `OnEnterAsync` on the new flow
5. Updates `CurrentFlow`
6. Appends the flow to `FlowHistory`
7. Returns `NavigationResult`

```csharp
// After successful login
var result = await _flowManager.ResetToFlowAsync<MainFlow>();

if (!result.Succeeded)
	await Shell.Current.DisplayAlert("Error", result.Message, "OK");
```

Configure root navigation options:

```csharp
await _flowManager.ResetToFlowAsync<MainFlow>(options =>
{
	options.Animated = false;  // skip animation during app init
	options.Reason   = "User authenticated";
});
```

---

### StartFlowAsync\<TFlow\>

Starts `TFlow` **without** clearing the back stack. The current flow is exited first. Use this when you want to layer a flow on top of the existing stack (e.g., entering an in-app onboarding wizard mid-session):

```csharp
// Start onboarding without losing the current stack
await _flowManager.StartFlowAsync<OnboardingFlow>();
```

---

### CompleteCurrentFlowAsync

Marks the current flow as complete and transitions back to the **previous flow in history**, if any. If there is no previous flow, navigation returns to the stack's root.

```csharp
// In the last onboarding step ViewModel
public async Task FinishOnboardingAsync()
{
	var result = await _flowManager.CompleteCurrentFlowAsync();

	if (!result.Succeeded)
		_logger.LogWarning("Flow completion failed: {Reason}", result.FailureReason);
}
```

---

## App Startup Pattern

The recommended startup pattern is to determine the initial flow in the `App` constructor or `AppShell` loaded handler:

```csharp
// App.xaml.cs
public partial class App : Application
{
	private readonly INavigationFlowManager _flowManager;
	private readonly IAuthService           _auth;

	public App(
		AppShell               shell,
		INavigationFlowManager flowManager,
		IAuthService           auth)
	{
		InitializeComponent();
		MainPage    = shell;
		_flowManager = flowManager;
		_auth        = auth;
	}

	protected override async void OnStart()
	{
		base.OnStart();

		// Enter the correct flow based on current auth state
		if (_auth.IsAuthenticated)
			await _flowManager.ResetToFlowAsync<MainFlow>(o => o.Animated = false);
		else
			await _flowManager.ResetToFlowAsync<AuthFlow>(o => o.Animated = false);
	}
}
```

---

## Failure Cases

| Scenario | Failure Reason |
|---|---|
| `TFlow` is not registered with DI | `FlowNotRegistered` |
| `CompleteCurrentFlowAsync` with no active flow | `NoActiveFlow` |
| Guard rejects root navigation | `GuardRejected` |
| Root page type not registered | `PageNotRegistered` |

---

## Related Pages

- [Flows Overview](overview.md)
- [Creating Flows](creating-flows.md)
- [Flow Context](flow-context.md)
- [Navigation Results](../navigation/navigation-results.md)
