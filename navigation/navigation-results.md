# Navigation Results

## Overview

Every `INavigationHandler` method returns a `NavigationResult`. Results are the primary mechanism for communicating navigation outcomes — failures are returned as structured values rather than thrown exceptions (unless `ShaunebuNavigationOptions.ThrowOnNavigationFailure` is `true`).

---

## NavigationResult

```csharp
public sealed class NavigationResult
{
	public bool                      Succeeded      { get; init; }
	public NavigationFailureReason?  FailureReason  { get; init; }
	public string?                   Message        { get; init; }
	public Exception?                Exception      { get; init; }
	public NavigationOperation       Operation      { get; init; }
}
```

| Property | Description |
|---|---|
| `Succeeded` | `true` when the navigation completed successfully |
| `FailureReason` | Structured enum identifying the failure; `null` on success |
| `Message` | Human-readable description of the result |
| `Exception` | The exception that caused the failure, if any; `null` for non-exceptional failures |
| `Operation` | The `NavigationOperation` this result is associated with |

---

## Handling Results

Always check `Succeeded` before consuming the result:

```csharp
var result = await _navigation.GoToAsync<DetailsPage>();

if (!result.Succeeded)
{
	switch (result.FailureReason)
	{
		case NavigationFailureReason.GuardRejected:
			await Shell.Current.DisplayAlert("Access Denied", result.Message, "OK");
			break;

		case NavigationFailureReason.DuplicateNavigationPrevented:
			// User tapped twice — safe to ignore
			break;

		case NavigationFailureReason.OperationCancelled:
			// Cancellation token was triggered — no action needed
			break;

		default:
			_logger.LogWarning("Navigation failed: {Reason} — {Message}",
				result.FailureReason, result.Message);
			break;
	}
}
```

---

## NavigationFailureReason

| Value | Description |
|---|---|
| `Unknown` | Unknown or unspecified failure |
| `RouteNotRegistered` | The requested route is not registered in the route registry |
| `PageNotRegistered` | The target page type is not registered with DI or the route registry |
| `NavigationInProgress` | A navigation operation is already in progress |
| `GuardRejected` | A navigation guard rejected the navigation request |
| `InvalidNavigationStack` | The navigation stack is in an invalid state for the requested operation |
| `ModalStackEmpty` | The modal stack is empty; no modal can be dismissed |
| `BackNavigationNotAvailable` | Back navigation is not available from the current state |
| `DuplicateNavigationPrevented` | Navigation was prevented because it duplicates a recent navigation |
| `InvalidRoot` | The specified root page type is invalid |
| `UnsupportedPresentationMode` | The requested presentation mode is not supported in the current context |
| `ParameterBindingFailed` | Parameter binding to the target ViewModel failed |
| `ShellUnavailable` | Shell is unavailable; Shell navigation cannot be performed |
| `NavigationPageUnavailable` | A `NavigationPage` is unavailable for stack-based navigation |
| `OperationCancelled` | The navigation operation was cancelled via a `CancellationToken` |
| `AmbiguousRoute` | The route matched more than one registered entry and is ambiguous |
| `GuardRedirectLoop` | A guard redirect caused a cycle that exceeded `MaxGuardRedirectDepth` |
| `FlowNotRegistered` | The requested flow type is not registered in the DI container |
| `NoActiveFlow` | No flow is currently active; the operation requires an active flow |

---

## NavigationOperation

Each `NavigationResult` carries the `NavigationOperation` that was executed:

```csharp
public sealed class NavigationOperation
{
	public Guid                             Id              { get; init; }
	public DateTimeOffset                   StartedAt       { get; init; }
	public DateTimeOffset?                  CompletedAt     { get; set; }
	public Type?                            TargetPageType  { get; init; }
	public string?                          Route           { get; init; }
	public NavigationPresentationMode       PresentationMode { get; init; }
	public NavigationStackBehavior          StackBehavior   { get; init; }
	public string?                          Source          { get; init; }
	public string?                          Reason          { get; init; }
	public IReadOnlyDictionary<string, object?> Parameters  { get; init; }
	public TimeSpan?                        Elapsed         { get; }
}
```

`Elapsed` is `null` until `CompletedAt` is set. The debugger platform records all operations for session replay.

---

## Result-Based vs Exception-Based Failures

By default `ThrowOnNavigationFailure` is `false` — all failures are returned as `NavigationResult` values.

Enable exception mode only during development to surface issues quickly:

```csharp
// Only during development/testing — never in production
options.ThrowOnNavigationFailure = true;
```

When enabled, a failed navigation throws `NavigationException`. See [navigation-exceptions.md](navigation-exceptions.md).

---

## Testing Navigation Results

Use `INavigationStackInspector` to verify navigation state in unit tests:

```csharp
var snapshot = await _inspector.GetSnapshotAsync();
Assert.Equal("main/home",  snapshot.CurrentRoute);
Assert.Equal("Main",       snapshot.CurrentFlow);
Assert.Equal(1,            snapshot.NavigationStack.Count);
Assert.Equal(0,            snapshot.ModalStack.Count);
```

---

## Related Pages

- [Typed Navigation](typed-navigation.md)
- [Navigation Exceptions](navigation-exceptions.md)
- [Navigation Options](navigation-options.md)
- [Diagnostics Overview](../diagnostics/overview.md)
