
# Shaunebu.MAUI.Navigation

> **A commercial-grade, type-safe navigation platform for .NET MAUI.**  
> Built around structured results, guard pipelines, named flows, overlay management, compile-time code generation, and a fully integrated Visual Studio debugger extension.

![NuGet](https://img.shields.io/nuget/v/Shaunebu.MAUI.Navigation?label=NuGet&color=blue)
![Downloads](https://img.shields.io/nuget/dt/Shaunebu.MAUI.Navigation?label=Downloads)
![Platform](https://img.shields.io/badge/Platform-.NET%20MAUI-512BD4?logo=dotnet)
![Status](https://img.shields.io/badge/Status-Preview-orange)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2026-5C2D91?logo=visualstudio)

![Source Generators](https://img.shields.io/badge/Source%20Generators-Enabled-8A2BE2)
![Roslyn](https://img.shields.io/badge/Roslyn-Analyzers%20%2B%20CodeFixes-512BD4)
![Live Debugger](https://img.shields.io/badge/Live-Debugger-red)
![Diagnostics](https://img.shields.io/badge/Diagnostics-JSON%20%7C%20Markdown-success)
![Architecture](https://img.shields.io/badge/Architecture-Type--Safe-success)

![Platforms](https://img.shields.io/badge/Platforms-Android%20%7C%20iOS%20%7C%20Windows%20%7C%20macOS-00BFFF)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)
![MAUI](https://img.shields.io/badge/.NET%20MAUI-8%2B-512BD4)

![Navigation](https://img.shields.io/nuget/v/Shaunebu.MAUI.Navigation?label=Navigation&logo=nuget)
![Debugger](https://img.shields.io/nuget/v/Shaunebu.MAUI.Navigation.Debugger?label=Debugger&logo=nuget)
![Generators](https://img.shields.io/nuget/v/Shaunebu.MAUI.Navigation.Generators?label=Generators&logo=nuget)
![Analyzers](https://img.shields.io/nuget/v/Shaunebu.MAUI.Navigation.Analyzers?label=Analyzers&logo=nuget)
![CodeFixes](https://img.shields.io/nuget/v/Shaunebu.MAUI.Navigation.CodeFixes?label=CodeFixes&logo=nuget)

[![VSIX](https://img.shields.io/badge/VSIX-Available-5C2D91?logo=visualstudio)](https://marketplace.visualstudio.com/items?itemName=IngJorgePeralesDiaz.ShaunebuMAUINavigation)
[![Live Monitoring](https://img.shields.io/badge/Live-Monitoring-red)](https://marketplace.visualstudio.com/items?itemName=IngJorgePeralesDiaz.ShaunebuMAUINavigation)

[![NuGet](https://img.shields.io/badge/NuGet-Packages-004880?logo=nuget)](https://www.nuget.org/packages/Shaunebu.MAUI.Navigation/)
[![Marketplace](https://img.shields.io/badge/Visual%20Studio-Marketplace-5C2D91?logo=visualstudio)](https://marketplace.visualstudio.com/items?itemName=IngJorgePeralesDiaz.ShaunebuMAUINavigation)
[![Documentation](https://img.shields.io/badge/Documentation-GitHub-181717?logo=github)](https://github.com/shaunebu/Shaunebu.MAUI.Navigation)
[![Support](https://img.shields.io/badge/Support-Buy%20me%20a%20Coffee-FFDD00?logo=buymeacoffee)](https://buymeacoffee.com/jcz65te)

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Architecture Overview](#architecture-overview)
- [NuGet Packages](#nuget-packages)
- [VSIX Extension](#vsix-extension)
- [Quick Start](#quick-start)
- [Installation](#installation)
- [Documentation Sections](#documentation-sections)
- [API Reference](#api-reference)

---

## Overview

`Shaunebu.MAUI.Navigation` replaces the fragmented MAUI/Shell navigation surface — `Shell.Current.GoToAsync`, `NavigationPage.PushAsync`, `Application.Current.MainPage.Navigation` — with a single, testable, observable abstraction: **`INavigationHandler`**.

Every navigation call passes through a deterministic 10-step pipeline:

1. Create `NavigationOperation`
2. Validate the target page or route
3. Check duplicate-navigation threshold
4. Acquire the navigation lock
5. Run all registered `INavigationGuard` implementations
6. Resolve the route via `INavigationRouteRegistry`
7. Resolve the page via `IPageResolver`
8. Execute the appropriate adapter (Shell or NavigationPage)
9. Invoke `INavigationAware` lifecycle callbacks on the ViewModel
10. Emit `INavigationDiagnostics` events and return `NavigationResult`

All failures are returned as structured `NavigationResult` values — never silent exceptions in production.

---

## Key Features

| Feature | Description |
|---|---|
| **Typed Navigation** | `GoToAsync<TPage>()`, `SetRootAsync<TPage>()`, `ShowModalAsync<TPage>()` |
| **Structured Results** | `NavigationResult` with `FailureReason`, `Message`, `Exception` |
| **Guard Pipeline** | `INavigationGuard` — allow, reject, or redirect per navigation |
| **Named Flows** | `INavigationFlow` + `INavigationFlowManager` — Auth/Main/Onboarding contexts |
| **Overlay System** | `IOverlayNavigationService` — loading and no-internet overlays |
| **Source Generators** | `[NavigationRoute]` attribute emits route constants + typed extension methods |
| **Roslyn Analyzers** | 10 diagnostic rules (SHAUNAV001–SHAUNAV010) enforcing navigation best practices |
| **Roslyn Code Fixes** | Automated fixes for select analyzer violations |
| **Navigation Debugger** | Session recording, timeline replay, stack diffing, runtime warnings, export/import |
| **VSIX Extension** | Visual Studio tool window for live inspection and session file analysis |
| **Back-Navigation Control** | `IBackAware` ViewModel interceptor + hardware back-button handling |
| **ViewModel Lifecycle** | `INavigationAware` — `OnNavigatedToAsync`, `OnNavigatingFromAsync`, `OnNavigatedFromAsync` |
| **Stack Inspector** | `INavigationStackInspector` — point-in-time stack snapshots for diagnostics and tests |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Your Application                            │
│  ViewModels inject INavigationHandler / INavigationFlowManager      │
└────────────────────────────┬────────────────────────────────────────┘
							 │
┌────────────────────────────▼────────────────────────────────────────┐
│                    Navigation Pipeline                              │
│  Duplicate check → Lock → Guards → Route resolution → Adapter      │
│  → INavigationAware callbacks → INavigationDiagnostics → Result     │
└────────┬──────────────┬──────────────┬───────────────┬─────────────┘
		 │              │              │               │
   Shell Adapter  NavigationPage  Overlay System   Flow Manager
		 │              │              │               │
┌────────▼──────────────▼──────────────▼───────────────▼─────────────┐
│                  MAUI Platform Layer                                │
│       Shell.Current / NavigationPage / Application.Current         │
└─────────────────────────────────────────────────────────────────────┘
		 │
┌────────▼─────────────────────────────────────────────────────────────┐
│                  Diagnostics + Debugger (DEBUG only)                 │
│   NavigationDiagnosticsBus → SessionRecorder → WarningEngine        │
│   → StackDiffEngine → Exporter → VSIX Tool Window                  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## NuGet Packages

| Package | Target | Purpose |
|---|---|---|
| `Shaunebu.MAUI.Navigation` | net9.0 / net10.0 | Core navigation library |
| `Shaunebu.MAUI.Navigation.Debugger` | net9.0 / net10.0 | Runtime diagnostics platform |
| `Shaunebu.MAUI.Navigation.Analyzers` | netstandard2.0 | Roslyn analyzers (SHAUNAV001–010) |
| `Shaunebu.MAUI.Navigation.CodeFixes` | netstandard2.0 | Roslyn code fixes |
| `Shaunebu.MAUI.Navigation.Generators` | netstandard2.0 | Incremental source generators |

Install the core package and generators in your MAUI app project. Add Analyzers + CodeFixes for compile-time enforcement. Add Debugger only in DEBUG configurations.

---

## VSIX Extension

The **Navigation Inspector** Visual Studio extension provides:

- **Live stack viewer** — real-time navigation and modal stack while debugging
- **Timeline panel** — chronological view of all navigation operations in a session
- **Warning viewer** — runtime warnings surfaced from `NavigationRuntimeWarningEngine`
- **Frame inspector** — per-operation detail (route, parameters, timing, stack diff)
- **Session loader** — open exported `.json` session files for post-mortem analysis

Install from the Visual Studio Marketplace or the `vsix/` build output.

---

## Quick Start

### 1. Register Navigation

```csharp
// MauiProgram.cs
builder.UseShaunebuNavigation(options =>
{
	options.DefaultNavigationMode      = NavigationPresentationMode.Shell;
	options.PreventDoubleNavigation    = true;
	options.DoubleNavigationThreshold  = TimeSpan.FromMilliseconds(750);
	options.EnableDiagnostics          = true;
	options.EnableNavigationGuards     = true;
	options.EnableOverlaySystem        = true;
	options.ThrowOnNavigationFailure   = false;

	// Route registration via source generator
	GeneratedNavigationRegistration.RegisterGeneratedRoutes(options);
});
```

### 2. Annotate Pages

```csharp
[NavigationRoute("main/home", Flow = "Main")]
public partial class HomePage : ContentPage { }

[NavigationRoute("auth/login", Flow = "Auth")]
public partial class LoginPage : ContentPage { }
```

### 3. Navigate in ViewModels

```csharp
public sealed class LoginViewModel
{
	private readonly INavigationHandler _navigation;
	private readonly INavigationFlowManager _flowManager;

	public LoginViewModel(INavigationHandler navigation, INavigationFlowManager flowManager)
	{
		_navigation  = navigation;
		_flowManager = flowManager;
	}

	public async Task LoginAsync()
	{
		// Perform login …

		var result = await _flowManager.ResetToFlowAsync<MainFlow>();
		if (!result.Succeeded)
			await ShowErrorAsync(result.Message);
	}
}
```

### 4. Handle Results

```csharp
var result = await _navigation.GoToAsync<SettingsPage>();

if (!result.Succeeded)
{
	switch (result.FailureReason)
	{
		case NavigationFailureReason.GuardRejected:
			await ShowAlertAsync("Access denied.");
			break;
		case NavigationFailureReason.DuplicateNavigationPrevented:
			break; // user tapped twice — safe to ignore
		default:
			_logger.LogWarning("Navigation failed: {Reason}", result.FailureReason);
			break;
	}
}
```

---

## Documentation Sections

| Section | Description |
|---|---|
| [Getting Started](getting-started.md) | First-time setup walkthrough |
| [Installation](installation.md) | NuGet and VSIX installation |
| [Typed Navigation](navigation/typed-navigation.md) | `INavigationHandler` API reference |
| [Navigation Routes](navigation/navigation-routes.md) | Route registry and `[NavigationRoute]` attribute |
| [Navigation Options](navigation/navigation-options.md) | `NavigationOptions` and `ShaunebuNavigationOptions` |
| [Navigation Results](navigation/navigation-results.md) | `NavigationResult` and `NavigationFailureReason` |
| [Navigation Exceptions](navigation/navigation-exceptions.md) | `ThrowOnNavigationFailure` and exception handling |
| [Guards Overview](guards/overview.md) | Navigation guard pipeline |
| [Authentication Guard](guards/authentication-guard.md) | `AuthenticationGuard<TLoginPage>` |
| [Maintenance Guard](guards/maintenance-guard.md) | `MaintenanceGuard<TMaintenancePage>` |
| [Internet Required Guard](guards/internet-required-guard.md) | `InternetRequiredGuard<TOfflinePage>` |
| [Unsaved Changes Guard](guards/unsaved-changes-guard.md) | `UnsavedChangesGuard` |
| [Custom Guards](guards/custom-guards.md) | Implementing `INavigationGuard` |
| [Flows Overview](flows/overview.md) | Named navigation flows |
| [Creating Flows](flows/creating-flows.md) | Implementing `INavigationFlow` |
| [Flow Manager](flows/flow-manager.md) | `INavigationFlowManager` API |
| [Flow Context](flows/flow-context.md) | `NavigationFlowContext` |
| [Overlays Overview](overlays/overview.md) | Overlay system concepts |
| [Loading Overlay](overlays/loading-overlay.md) | `ShowLoadingAsync` / `HideLoadingAsync` |
| [No-Internet Overlay](overlays/no-internet-overlay.md) | `ShowNoInternetAsync` / `HideNoInternetAsync` |
| [Custom Overlays](overlays/custom-overlays.md) | Extending the overlay system |
| [Diagnostics Overview](diagnostics/overview.md) | `INavigationDiagnostics` |
| [Debugger Overview](debugger/overview.md) | Navigation debugger platform |
| [Generators Overview](generators/overview.md) | Source generator system |
| [Analyzers Overview](analyzers/overview.md) | Roslyn analyzer rules |
| [VSIX Overview](vsix/overview.md) | Visual Studio extension |
| [Sample Application](samples/overview.md) | End-to-end walkthrough |
| [Advanced Topics](advanced/overview.md) | DI, extensibility, testing |
| [API Reference](reference/api-reference.md) | Full public API index |

---

## API Reference

See [reference/api-reference.md](reference/api-reference.md) for the complete index of all public interfaces, classes, records, enums, and extension methods.
