# Analyzer Rules Reference

## Overview

This page is a quick-reference index of all Roslyn diagnostic rules shipped with `Shaunebu.MAUI.Navigation.Analyzers`. For full descriptions, code examples, and fix guidance, see [Analyzers Overview](overview.md).

---

## Rule Index

| Rule | Category | Default Severity | Title |
|---|---|---|---|
| [SHAUNAV001](#shaunav001) | Architecture | Warning | Direct `Shell.Current.GoToAsync` usage |
| [SHAUNAV002](#shaunav002) | Architecture | Warning | Direct `NavigationPage.PushAsync` / `PopAsync` usage |
| [SHAUNAV003](#shaunav003) | Best Practices | Warning | Magic route string literal |
| [SHAUNAV004](#shaunav004) | Navigation Safety | Warning | Navigation called in constructor |
| [SHAUNAV005](#shaunav005) | Navigation Safety | Warning | Overlay page used as navigation target |
| [SHAUNAV006](#shaunav006) | Async Correctness | Warning | Unawaited navigation call |
| [SHAUNAV007](#shaunav007) | Architecture | Warning | ViewModel coupled to MAUI types |
| [SHAUNAV008](#shaunav008) | Navigation Safety | Warning | Concurrent navigation without await |
| [SHAUNAV010](#shaunav010) | Architecture | Warning | `Application.Current.MainPage` navigation |

> **Note:** SHAUNAV009 is reserved and not currently emitted.

---

## SHAUNAV001

**Title:** Direct `Shell.Current.GoToAsync` usage  
**Category:** Architecture  
**Default Severity:** Warning  
**Analyzer:** `DirectShellNavigationAnalyzer`  
**Code Fix:** None

Calling `Shell.Current.GoToAsync` directly bypasses the guard pipeline, diagnostics, duplicate-navigation prevention, and typed routing.

```csharp
// ❌ SHAUNAV001
await Shell.Current.GoToAsync("main/home");

// ✅ Use INavigationHandler
await _navigation.GoToAsync<HomePage>();
```

---

## SHAUNAV002

**Title:** Direct `NavigationPage` navigation  
**Category:** Architecture  
**Default Severity:** Warning  
**Analyzer:** `DirectNavigationPageAnalyzer`  
**Code Fix:** None

Using `NavigationPage.PushAsync` / `PopAsync` bypasses the navigation pipeline.

```csharp
// ❌ SHAUNAV002
await Navigation.PushAsync(new SettingsPage());

// ✅ Use INavigationHandler
await _navigation.GoToAsync<SettingsPage>();
```

---

## SHAUNAV003

**Title:** Magic route string literal  
**Category:** Best Practices  
**Default Severity:** Warning  
**Analyzer:** `MagicRouteStringAnalyzer`  
**Code Fix:** None

Passing a raw string literal to `INavigationHandler.GoToAsync(string route, ...)` prevents compile-time validation. Use generated route constants or typed navigation.

```csharp
// ❌ SHAUNAV003
await _navigation.GoToAsync("main/home");

// ✅ Use generated constant
await _navigation.GoToAsync(GeneratedRoutes.HomePage);

// ✅ Or typed navigation (preferred)
await _navigation.GoToAsync<HomePage>();
```

---

## SHAUNAV004

**Title:** Navigation called in constructor  
**Category:** Navigation Safety  
**Default Severity:** Warning  
**Analyzer:** `NavigationInConstructorAnalyzer`  
**Code Fix:** None

`INavigationHandler` cannot safely execute before the MAUI page lifecycle completes. Move navigation to `INavigationAware.OnNavigatedToAsync` or an `ICommand` handler.

```csharp
// ❌ SHAUNAV004
public HomeViewModel(INavigationHandler nav)
{
	nav.GoToAsync<LoginPage>(); // unsafe
}

// ✅ Use OnNavigatedToAsync
public async Task OnNavigatedToAsync(NavigationContext ctx, CancellationToken ct)
	=> await _navigation.GoToAsync<LoginPage>(cancellationToken: ct);
```

---

## SHAUNAV005

**Title:** Overlay page used as navigation target  
**Category:** Navigation Safety  
**Default Severity:** Warning  
**Analyzer:** `LoadingPageNavigationAnalyzer`  
**Code Fix:** None

Pages designated as overlay hosts (loading or no-internet pages) must only be controlled via `IOverlayNavigationService`. Navigating to them directly with `INavigationHandler` corrupts the navigation stack.

```csharp
// ❌ SHAUNAV005
await _navigation.GoToAsync<LoadingPage>();

// ✅ Use the overlay service
await _overlay.ShowLoadingAsync(new LoadingOverlayOptions { Message = "Loading…" });
```

---

## SHAUNAV006

**Title:** Unawaited navigation call  
**Category:** Async Correctness  
**Default Severity:** Warning  
**Analyzer:** `UnawaitedNavigationAnalyzer`  
**Code Fix:** `AddAwaitCodeFix` — inserts `await`, adds `async` to the containing method

Discarding the `Task<NavigationResult>` means navigation failures are silently swallowed and the navigation lock is held until the discarded task resolves.

```csharp
// ❌ SHAUNAV006
_navigation.GoToAsync<HomePage>(); // result discarded

// ✅ Await the result
var result = await _navigation.GoToAsync<HomePage>();
if (!result.Succeeded)
	HandleFailure(result);
```

**Applying the code fix:** Place the cursor on the flagged call and press `Ctrl+.` (or lightbulb) → **Add 'await'**.

---

## SHAUNAV007

**Title:** ViewModel coupled to MAUI types  
**Category:** Architecture  
**Default Severity:** Warning  
**Analyzer:** `ViewModelCoupledToMauiAnalyzer`  
**Code Fix:** None

Direct references to MAUI types (`Page`, `Shell`, `ContentPage`, `NavigationPage`, etc.) inside ViewModel classes break platform-agnostic testability.

```csharp
// ❌ SHAUNAV007 — ViewModel references a MAUI Page type directly
public sealed class HomeViewModel
{
	private readonly Shell _shell;
	public HomeViewModel(Shell shell) => _shell = shell;
}

// ✅ Use navigation abstractions
public sealed class HomeViewModel
{
	private readonly INavigationHandler _navigation;
	public HomeViewModel(INavigationHandler navigation) => _navigation = navigation;
}
```

---

## SHAUNAV008

**Title:** Concurrent navigation without await  
**Category:** Navigation Safety  
**Default Severity:** Warning  
**Analyzer:** `ConcurrentNavigationAnalyzer`  
**Code Fix:** None

Multiple `GoToAsync` calls without `await` between them in the same method cause concurrent navigation. The navigation lock rejects the second call and the `NavigationResult` is silently discarded.

```csharp
// ❌ SHAUNAV008
_navigation.GoToAsync<HomePage>();
_navigation.GoToAsync<SettingsPage>(); // races with the first call

// ✅ Await each call
await _navigation.GoToAsync<HomePage>();
await _navigation.GoToAsync<SettingsPage>();
```

---

## SHAUNAV010

**Title:** `Application.Current.MainPage` navigation  
**Category:** Architecture  
**Default Severity:** Warning  
**Analyzer:** `ApplicationMainPageNavigationAnalyzer`  
**Code Fix:** None

Assigning `Application.Current.MainPage` for navigation purposes bypasses the entire pipeline — guards, diagnostics, typed routing, and lifecycle callbacks.

```csharp
// ❌ SHAUNAV010
Application.Current!.MainPage = new NavigationPage(new LoginPage());

// ✅ Use flow manager to reset the root
await _flowManager.ResetToFlowAsync<AuthFlow>();
```

---

## Suppressing Rules

### Inline suppression

```csharp
#pragma warning disable SHAUNAV001 // Direct Shell navigation — legacy code path
await Shell.Current.GoToAsync("legacy/page");
#pragma warning restore SHAUNAV001
```

### .editorconfig

```ini
[*.cs]
# Promote magic routes to error
dotnet_diagnostic.SHAUNAV003.severity = error

# Downgrade ViewModel coupling to suggestion
dotnet_diagnostic.SHAUNAV007.severity = suggestion

# Suppress overlay-as-target in legacy code
dotnet_diagnostic.SHAUNAV005.severity = none
```

---

## Related Pages

- [Analyzers Overview](overview.md)
- [Generator Diagnostics](../generators/diagnostics.md)
- [Installation](../installation.md)
- [Overlays Overview](../overlays/overview.md)
