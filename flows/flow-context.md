# Flow Context

## Overview

`NavigationFlowContext` is passed to `INavigationFlow.OnEnterAsync` and `OnExitAsync` lifecycle callbacks. It carries information about the flow transition that triggered the callback.

---

## NavigationFlowContext

```csharp
public sealed class NavigationFlowContext
{
	public required NavigationOperation                Operation       { get; init; }
	public required string                             FlowName        { get; init; }
	public          string?                            PreviousFlowName { get; init; }
	public          IReadOnlyDictionary<string, object?> Parameters    { get; init; }
}
```

| Property | Description |
|---|---|
| `Operation` | The `NavigationOperation` that triggered the flow transition |
| `FlowName` | The name of the flow being entered or exited |
| `PreviousFlowName` | The name of the flow that was active before this transition, or `null` if this is the first flow |
| `Parameters` | Additional parameters provided to the flow via `configure` delegate on `StartFlowAsync`/`ResetToFlowAsync` |

---

## Using the Context in OnEnterAsync

```csharp
public async Task OnEnterAsync(NavigationFlowContext context)
{
	// Log the transition
	_logger.LogInformation(
		"Entering flow '{Flow}' (from '{Prev}')",
		context.FlowName,
		context.PreviousFlowName ?? "none");

	// Access parameters passed at flow start time
	if (context.Parameters.TryGetValue("deepLinkRoute", out var route) && route is string target)
		await _navigation.GoToAsync(target);
	else
		// No deep link — load default state
		await LoadDefaultStateAsync();
}
```

---

## Using the Context in OnExitAsync

```csharp
public async Task OnExitAsync(NavigationFlowContext context)
{
	_logger.LogInformation(
		"Exiting flow '{Flow}' (heading to '{Next}')",
		context.FlowName,
		context.PreviousFlowName ?? "unknown");   // PreviousFlowName on exit = incoming flow name

	// Flush any pending analytics
	await _analytics.FlushAsync();
}
```

---

## Passing Parameters to a Flow

Flow entry parameters are set when calling `ResetToFlowAsync` or `StartFlowAsync` via the `NavigationOptions` delegate. Consumers access them in `OnEnterAsync` via `context.Parameters`:

```csharp
// Caller: pass a deep-link route to the new flow
await _flowManager.ResetToFlowAsync<MainFlow>(options =>
{
	options.Parameters["deepLinkRoute"] = "main/orders/1234";
	options.Reason                      = "Deep link launch";
});

// Receiver: access in MainFlow.OnEnterAsync
public async Task OnEnterAsync(NavigationFlowContext context)
{
	if (context.Parameters.TryGetValue("deepLinkRoute", out var route) && route is string target)
		await _navigation.GoToAsync(target);
}
```

---

## NavigationOperation in Context

`context.Operation` carries the full `NavigationOperation` record for the root navigation that initiated the flow entry. This includes:

- `Operation.Id` — unique identifier for the transition
- `Operation.StartedAt` — UTC timestamp of the transition
- `Operation.TargetPageType` — the root page type
- `Operation.Route` — the resolved route string
- `Operation.Elapsed` — available after the operation completes

---

## Related Pages

- [Flows Overview](overview.md)
- [Creating Flows](creating-flows.md)
- [Flow Manager](flow-manager.md)
- [Navigation Options](../navigation/navigation-options.md)
