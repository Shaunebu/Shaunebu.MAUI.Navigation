# Navigation Exceptions

## Overview

By default, `Shaunebu.MAUI.Navigation` returns all failures as structured `NavigationResult` values. This is the recommended approach for production code because it forces explicit error handling and prevents uncaught exceptions from crashing the app.

However, exception-based failure mode can be enabled for development and testing scenarios where you want immediate visibility into navigation problems.

---

## Result-Based Mode (Default)

`ShaunebuNavigationOptions.ThrowOnNavigationFailure` defaults to `false`:

```csharp
builder.UseShaunebuNavigation(options =>
{
	options.ThrowOnNavigationFailure = false; // default — return NavigationResult
});
```

In this mode, every navigation method returns a `NavigationResult`. Check `result.Succeeded` and `result.FailureReason` to handle errors:

```csharp
var result = await _navigation.GoToAsync<SettingsPage>();

if (!result.Succeeded)
{
	// result.FailureReason — structured enum
	// result.Message       — human-readable description
	// result.Exception     — underlying exception, if any
	_logger.LogWarning("Navigation failed: {Reason}", result.FailureReason);
}
```

---

## Exception Mode (Development / Testing Only)

Enable exception mode to surface navigation failures as thrown exceptions:

```csharp
#if DEBUG
builder.UseShaunebuNavigation(options =>
{
	options.ThrowOnNavigationFailure = true;
});
#endif
```

When a navigation fails in exception mode, a `NavigationException` is thrown. Never enable this in production — unhandled exceptions crash the app.

---

## NavigationException

`NavigationException` wraps the failure reason and carries the original `NavigationResult`:

```csharp
try
{
	await _navigation.GoToAsync<SettingsPage>();
}
catch (NavigationException ex)
{
	Console.WriteLine(ex.FailureReason); // NavigationFailureReason enum value
	Console.WriteLine(ex.Message);       // human-readable description
	Console.WriteLine(ex.Result);        // the full NavigationResult
}
```

`NavigationException` inherits from `Exception`. The `InnerException` property carries any underlying platform exception.

---

## Guard Redirect Loops

If a guard repeatedly redirects navigation in a cycle, the pipeline aborts after `ShaunebuNavigationOptions.MaxGuardRedirectDepth` consecutive redirects (default: 5) and returns:

```
NavigationFailureReason.GuardRedirectLoop
```

Example: Guard A redirects to LoginPage, Guard A then redirects away from LoginPage, creating a loop. After 5 redirects, the operation fails with `GuardRedirectLoop`.

To fix: ensure at least one guard exempts the redirect target page from its check.

---

## Common Failure Scenarios

### Route Not Registered

```
NavigationFailureReason.RouteNotRegistered
```

The page type has no registered route. Fix: add `[NavigationRoute("my/route")]` to the page and call `GeneratedNavigationRegistration.RegisterGeneratedRoutes(options)`, or call `options.RegisterPage<MyPage>("my/route")` manually.

### Guard Rejected

```
NavigationFailureReason.GuardRejected
```

A navigation guard returned `NavigationGuardResult.Reject(reason)`. Inspect `result.Message` for the guard's reason string. See [guards/overview.md](../guards/overview.md).

### Back Navigation Not Available

```
NavigationFailureReason.BackNavigationNotAvailable
```

`GoBackAsync()` was called when the navigation stack is already at the root. Check the stack before calling:

```csharp
var snapshot = await _inspector.GetSnapshotAsync();
if (snapshot.NavigationStack.Count > 1)
	await _navigation.GoBackAsync();
```

### Ambiguous Route

```
NavigationFailureReason.AmbiguousRoute
```

The same page type is registered under multiple flows and no flow is currently active. Fix: start a flow with `INavigationFlowManager.ResetToFlowAsync<TFlow>()` before navigating.

---

## Related Pages

- [Navigation Results](navigation-results.md)
- [Navigation Options](navigation-options.md)
- [Guards Overview](../guards/overview.md)
