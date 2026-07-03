# Analyzers Overview

## Overview

The `Shaunebu.MAUI.Navigation.Analyzers` package contains 9 Roslyn diagnostic analyzers (SHAUNAV001–SHAUNAV010) that enforce correct usage of `INavigationHandler` and prevent common architectural, safety, and async-correctness mistakes.

Analyzers run entirely at compile time. No runtime overhead is incurred.

---

## Installation

```xml
<PackageReference Include="Shaunebu.MAUI.Navigation.Analyzers"
				  Version="1.0.0-preview.1"
				  PrivateAssets="all">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

For code fixes (the `await` inserter), also reference:

```xml
<PackageReference Include="Shaunebu.MAUI.Navigation.CodeFixes"
				  Version="1.0.0-preview.1"
				  PrivateAssets="all">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

---

## Diagnostic Reference

### SHAUNAV001 — Direct Shell.Current.GoToAsync usage

| Property | Value |
|---|---|
| Category | Architecture |
| Severity | Warning |
| Analyzer | `DirectShellNavigationAnalyzer` |

Detects `Shell.Current.GoToAsync(...)` calls that bypass `INavigationHandler`, losing guard evaluation, diagnostics, and typed routing.

```csharp
// ❌ Triggers SHAUNAV001
await Shell.Current.GoToAsync("main/home");

// ✅ Use INavigationHandler instead
await _navigation.GoToAsync<HomePage>();
```

---

### SHAUNAV002 — Direct NavigationPage navigation

| Property | Value |
|---|---|
| Category | Architecture |
| Severity | Warning |
| Analyzer | `DirectNavigationPageAnalyzer` |

Detects `NavigationPage.PushAsync` / `PopAsync` calls that bypass the navigation pipeline.

---

### SHAUNAV003 — Magic route string

| Property | Value |
|---|---|
| Category | Best Practices |
| Severity | Warning |
| Analyzer | `MagicRouteStringAnalyzer` |

Detects string literals passed directly to `INavigationHandler.GoToAsync(string route, ...)`. Route constants from `GeneratedRoutes` or `[NavigationRoute]` should be used instead.

```csharp
// ❌ Triggers SHAUNAV003
await _navigation.GoToAsync("main/home");

// ✅ Use generated constants
await _navigation.GoToAsync(GeneratedRoutes.HomePage);
// or typed navigation
await _navigation.GoToAsync<HomePage>();
```

---

### SHAUNAV004 — Navigation in constructor

| Property | Value |
|---|---|
| Category | Navigation Safety |
| Severity | Warning |
| Analyzer | `NavigationInConstructorAnalyzer` |

Detects `INavigationHandler` method calls inside constructors. Navigation cannot safely execute before the MAUI page lifecycle is complete.

```csharp
// ❌ Triggers SHAUNAV004
public HomeViewModel(INavigationHandler nav)
{
	nav.GoToAsync<LoginPage>(); // unsafe in constructor
}

// ✅ Use OnNavigatedToAsync
public async Task OnNavigatedToAsync(NavigationContext ctx, CancellationToken ct)
	=> await _navigation.GoToAsync<LoginPage>();
```

---

### SHAUNAV005 — Offline page used as navigation target

| Property | Value |
|---|---|
| Category | Navigation Safety |
| Severity | Warning |
| Analyzer | `LoadingPageNavigationAnalyzer` |

Detects navigation to pages that are designated as overlay hosts (loading/no-internet pages). These should only be controlled via `IOverlayNavigationService`.

---

### SHAUNAV006 — Unawaited navigation call

| Property | Value |
|---|---|
| Category | Async Correctness |
| Severity | Warning |
| Analyzer | `UnawaitedNavigationAnalyzer` |
| **Code Fix** | `AddAwaitCodeFix` — inserts `await` and makes the method `async` |

Detects fire-and-forget navigation calls that silently discard the `NavigationResult`.

```csharp
// ❌ Triggers SHAUNAV006
_navigation.GoToAsync<HomePage>(); // result discarded

// ✅ Await the result
var result = await _navigation.GoToAsync<HomePage>();
```

Apply the **Add 'await'** code fix (Ctrl+. in Visual Studio) to automatically insert `await` and add the `async` modifier to the containing method.

---

### SHAUNAV007 — ViewModel coupled to MAUI types

| Property | Value |
|---|---|
| Category | Architecture |
| Severity | Warning |
| Analyzer | `ViewModelCoupledToMauiAnalyzer` |

Detects direct references to MAUI page/shell types (`Page`, `Shell`, `ContentPage`, etc.) inside ViewModel classes. ViewModels should be platform-agnostic.

---

### SHAUNAV008 — Concurrent navigation

| Property | Value |
|---|---|
| Category | Navigation Safety |
| Severity | Warning |
| Analyzer | `ConcurrentNavigationAnalyzer` |

Detects patterns where multiple `GoToAsync` calls are triggered without `await` between them in the same method, risking concurrent navigation failures.

---

### SHAUNAV010 — Application.MainPage navigation

| Property | Value |
|---|---|
| Category | Architecture |
| Severity | Warning |
| Analyzer | `ApplicationMainPageNavigationAnalyzer` |

Detects direct assignment of `Application.Current.MainPage` for navigation purposes, which bypasses the entire navigation pipeline including guards and diagnostics.

---

## Code Fixes

| Fix | Diagnostic | Action |
|---|---|---|
| `AddAwaitCodeFix` | SHAUNAV006 | Inserts `await` before the unawaited call; adds `async` to the containing method if needed |

---

## Severity Configuration

Override default severities in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.SHAUNAV003.severity = error     # promote magic routes to error
dotnet_diagnostic.SHAUNAV007.severity = suggestion # demote ViewModel coupling
dotnet_diagnostic.SHAUNAV005.severity = none       # suppress in legacy code
```

---

## Related Pages

- [Generator Diagnostics](../generators/diagnostics.md)
- [Debugger — Runtime Warnings](../debugger/runtime-warnings.md)
- [Installation](../installation.md)
