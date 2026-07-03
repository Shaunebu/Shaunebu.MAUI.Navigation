# API Reference

Complete reference for all public types in `Shaunebu.MAUI.Navigation` and related packages.

---

## Core Interfaces

### INavigationHandler

Primary navigation service. Resolve from DI as `INavigationHandler`.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface INavigationHandler
{
	// Typed navigation
	Task<NavigationResult> GoToAsync<TPage>(
		CancellationToken cancellationToken = default) where TPage : Page;

	Task<NavigationResult> GoToAsync<TPage>(
		Action<NavigationOptions> configure,
		CancellationToken cancellationToken = default) where TPage : Page;

	// Runtime-type navigation
	Task<NavigationResult> GoToAsync(
		Type pageType,
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default);

	// Route-string navigation
	Task<NavigationResult> GoToAsync(
		string route,
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default);

	// Route-object navigation
	Task<NavigationResult> GoToAsync(
		INavigationRoute route,
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default);

	// Back navigation
	Task<NavigationResult> GoBackAsync(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default);

	// Modal operations
	Task<NavigationResult> ShowModalAsync<TPage>(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default) where TPage : Page;

	Task<NavigationResult> CloseModalAsync(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default);

	// Root and reset
	Task<NavigationResult> SetRootAsync<TPage>(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default) where TPage : Page;

	Task<NavigationResult> ResetAsync(
		CancellationToken cancellationToken = default);
}
```

---

### INavigationAware

ViewModel navigation lifecycle interface.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface INavigationAware
{
	Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellationToken = default);
	Task OnNavigatingFromAsync(NavigationContext context, CancellationToken cancellationToken = default);
	Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellationToken = default);
}
```

---

### IBackAware

ViewModel back-navigation interceptor.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface IBackAware
{
	Task<bool> CanGoBackAsync(CancellationToken cancellationToken = default);
	Task OnBackAsync(CancellationToken cancellationToken = default);
}
```

---

### INavigationGuard

Navigation guard contract.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface INavigationGuard
{
	Task<NavigationGuardResult> CanNavigateAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken = default);
}
```

---

### INavigationFlow

Named navigation flow contract.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface INavigationFlow
{
	string Name                  { get; }
	Type   RootPageType          { get; }
	bool   RequiresAuthentication { get; }

	Task OnEnterAsync(NavigationFlowContext context, CancellationToken cancellationToken = default);
	Task OnExitAsync(NavigationFlowContext context, CancellationToken cancellationToken = default);
}
```

---

### INavigationFlowManager

Flow transition manager.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface INavigationFlowManager
{
	INavigationFlow?             CurrentFlow  { get; }
	IReadOnlyList<INavigationFlow> FlowHistory { get; }

	Task<NavigationResult> StartFlowAsync<TFlow>(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default) where TFlow : INavigationFlow;

	Task<NavigationResult> ResetToFlowAsync<TFlow>(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default) where TFlow : INavigationFlow;

	Task<NavigationResult> CompleteCurrentFlowAsync(
		Action<NavigationOptions>? configure = null,
		CancellationToken cancellationToken = default);
}
```

---

### IOverlayNavigationService

Overlay control service.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface IOverlayNavigationService
{
	Task ShowLoadingAsync(LoadingOverlayOptions? options = null, CancellationToken ct = default);
	Task HideLoadingAsync(CancellationToken ct = default);

	Task ShowNoInternetAsync(NoInternetOverlayOptions? options = null, CancellationToken ct = default);
	Task HideNoInternetAsync(CancellationToken ct = default);

	Task ShowOverlayAsync<TOverlay>(object? parameter = null, CancellationToken ct = default)
		where TOverlay : ContentView;

	Task HideOverlayAsync<TOverlay>(CancellationToken ct = default)
		where TOverlay : ContentView;

	OverlaySnapshot GetSnapshot();
	Task<OverlaySnapshot> GetSnapshotAsync(CancellationToken ct = default);
	T? GetParameter<T>(string key);
	bool IsOverlayVisible<TOverlay>() where TOverlay : ContentView;
}
```

---

### INavigationStackInspector

Read-only stack inspector.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface INavigationStackInspector
{
	Task<NavigationStackSnapshot> GetSnapshotAsync();
}
```

---

### INavigationRoute

Marker interface for typed route objects.

```csharp
namespace Shaunebu.MAUI.Navigation.Abstractions;

public interface INavigationRoute { }
```

---

## Models

### NavigationResult

```csharp
public sealed class NavigationResult
{
	public bool                    Succeeded     { get; init; }
	public NavigationFailureReason? FailureReason { get; init; }
	public string?                 Message       { get; init; }
	public Exception?              Exception     { get; init; }
	public NavigationOperation?    Operation     { get; init; }
}
```

### NavigationOperation

```csharp
public sealed class NavigationOperation
{
	public Guid                   Id               { get; init; }
	public DateTimeOffset         StartedAt        { get; init; }
	public DateTimeOffset?        CompletedAt      { get; init; }
	public Type?                  TargetPageType   { get; init; }
	public string?                Route            { get; init; }
	public NavigationPresentationMode PresentationMode { get; init; }
	public NavigationStackBehavior StackBehavior   { get; init; }
	public string?                Source           { get; init; }
	public string?                Reason           { get; init; }
	public IReadOnlyDictionary<string, object>? Parameters { get; init; }
	public TimeSpan               Elapsed          { get; }   // computed
}
```

### NavigationContext

```csharp
public sealed class NavigationContext
{
	public NavigationOperation? Operation      { get; init; }
	public Type?                SourcePageType { get; init; }
	public Type?                TargetPageType { get; init; }
	public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
```

### NavigationOptions

```csharp
public sealed class NavigationOptions
{
	public NavigationPresentationMode PresentationMode    { get; set; }
	public bool                       Animated            { get; set; }
	public bool                       IsRoot              { get; set; }
	public bool                       ClearBackStack      { get; set; }
	public bool                       PreventDuplicates   { get; set; }
	public bool                       AllowConcurrentNavigation { get; set; }
	public object?                    Parameters          { get; set; }
	public NavigationTransition       Transition          { get; set; }
	public NavigationStackBehavior    StackBehavior       { get; set; }
	public string?                    Source              { get; set; }
	public string?                    Reason              { get; set; }
}
```

### NavigationStackSnapshot

```csharp
public sealed class NavigationStackSnapshot
{
	public IReadOnlyList<string> NavigationStack { get; init; }
	public IReadOnlyList<string> ModalStack      { get; init; }
	public string?               CurrentRoute    { get; init; }
	public string?               CurrentFlow     { get; init; }
}
```

### NavigationFlowContext

```csharp
public sealed class NavigationFlowContext
{
	public NavigationOperation? Operation        { get; init; }
	public string?              FlowName         { get; init; }
	public string?              PreviousFlowName { get; init; }
	public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
```

### NavigationGuardContext

```csharp
public sealed class NavigationGuardContext
{
	public Type?   SourcePageType { get; init; }
	public Type?   TargetPageType { get; init; }
	public string? TargetRoute    { get; init; }
	public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
```

### NavigationGuardResult

```csharp
public sealed class NavigationGuardResult
{
	public bool    IsAllowed  { get; }
	public bool    IsRejected { get; }
	public bool    IsRedirect { get; }
	public Type?   RedirectToPageType { get; }
	public string? Reason     { get; }

	public static NavigationGuardResult Allow();
	public static NavigationGuardResult Reject(string reason);
	public static NavigationGuardResult RedirectTo<TPage>(string? reason = null);
}
```

---

## Enums

### NavigationFailureReason

| Value | Description |
|---|---|
| `RouteNotRegistered` | No matching route in the registry |
| `PageNotRegistered` | Page type not registered in DI |
| `NavigationInProgress` | Concurrent navigation blocked |
| `GuardRejected` | A guard returned `Reject` |
| `DuplicateNavigationPrevented` | Same route within threshold window |
| `GuardRedirectLoop` | Guard redirect depth exceeded |
| `FlowNotRegistered` | Flow type not registered |
| `NoActiveFlow` | Operation requires an active flow |

### NavigationPresentationMode

| Value | Description |
|---|---|
| `Default` | Standard push navigation |
| `Modal` | Present modally |
| `Root` | Replace navigation root |
| `Reset` | Clear entire navigation state |

### NavigationStackBehavior

| Value | Description |
|---|---|
| `Default` | Standard push onto the stack |
| `ClearToRoot` | Pop to root before navigating |
| `Replace` | Replace the current page |

### NavigationTransition

| Value | Description |
|---|---|
| `Default` | Platform default transition |
| `None` | No animation |
| `SlideFromRight` | Slide in from the right |
| `SlideFromBottom` | Slide in from the bottom |
| `Fade` | Fade transition |

---

## Configuration

### ShaunebuNavigationOptions

| Property | Type | Default | Description |
|---|---|---|---|
| `EnableDiagnostics` | `bool` | `false` | Enable diagnostic event publishing |
| `EnableNavigationGuards` | `bool` | `true` | Enable guard pipeline |
| `EnableOverlaySystem` | `bool` | `true` | Enable overlay services |
| `EnableBackButtonHandling` | `bool` | `true` | Intercept hardware back button |
| `ThrowOnNavigationFailure` | `bool` | `false` | Throw `NavigationException` on failure |
| `PreventDoubleNavigation` | `bool` | `true` | Block duplicate navigation within threshold |
| `DoubleNavigationThreshold` | `TimeSpan` | 300 ms | Duplicate detection window |
| `MaxGuardRedirectDepth` | `int` | `10` | Maximum guard redirect chain depth |
| `DefaultAnimated` | `bool` | `true` | Default animation flag |
| `DefaultNavigationMode` | `NavigationPresentationMode` | `Default` | Default presentation mode |
| `DefaultStackBehavior` | `NavigationStackBehavior` | `Default` | Default stack behavior |
| `RegisterShellRoutesAutomatically` | `bool` | `false` | Auto-register Shell hierarchy routes |

---

## Extension Methods

### MauiAppBuilder

```csharp
// Microsoft.Extensions.DependencyInjection
MauiAppBuilder UseShaunebuNavigation(
	this MauiAppBuilder builder,
	Action<ShaunebuNavigationOptions>? configure = null);
```

### IServiceCollection (Debugger)

```csharp
IServiceCollection UseNavigationDebugger(
	this IServiceCollection services,
	Action<NavigationDebuggerOptions>? configure = null);
```

### INavigationHandler (Generated)

```csharp
// For each [NavigationRoute("route/path")]-annotated page 'FooPage':
Task<NavigationResult> GoToFooPageAsync(this INavigationHandler nav);
Task<NavigationResult> GoToFooPageAsync(this INavigationHandler nav, Action<NavigationOptions> configure);
Task<NavigationResult> GoToFooPageAsync(this INavigationHandler nav, Action<NavigationOptions> configure, CancellationToken ct);
```

---

## Related Pages

- [Navigation Handler](../navigation/typed-navigation.md)
- [Navigation Options](../navigation/navigation-options.md)
- [Navigation Results](../navigation/navigation-results.md)
- [Guards Overview](../guards/overview.md)
- [Flows Overview](../flows/overview.md)
- [Generators Overview](../generators/overview.md)
