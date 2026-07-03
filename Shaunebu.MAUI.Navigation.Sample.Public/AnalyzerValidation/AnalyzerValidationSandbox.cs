// =============================================================================
// ANALYZER VALIDATION SANDBOX
// =============================================================================
//
// Purpose  : Regression-validation file for all implemented SHAUNAV analyzer rules.
//            Each section below contains intentionally non-compliant code that MUST
//            trigger the corresponding diagnostic when the violation is uncommented.
//
// Usage    : To validate a rule:
//              1. Uncomment the violation block for that rule.
//              2. Build the project.
//              3. Verify the expected diagnostic ID appears at the marked location.
//              4. Re-comment the block.
//
// Rules covered:
//   SHAUNAV001 — Direct Shell.Current.GoToAsync
//   SHAUNAV002 — Direct NavigationPage API (PushAsync / PopAsync / PushModalAsync / PopModalAsync)
//   SHAUNAV003 — Magic route string passed to GoToAsync
//   SHAUNAV004 — Loading/busy page used as navigation target (ERROR severity)
//   SHAUNAV005 — NoInternet/offline page used as navigation target (ERROR severity)
//   SHAUNAV006 — Navigation call not awaited
//   SHAUNAV007 — Navigation inside constructor
//   SHAUNAV008 — Concurrent unawaited navigation calls (multiple unawaited in same method)
//   SHAUNAV009 — Application.Current.MainPage.Navigation access
//   SHAUNAV010 — ViewModel field / constructor parameter referencing Shell, NavigationPage,
//                INavigation, or INavigationService
//
// IMPORTANT: This file must NEVER be compiled with violations active in CI/production
//            builds. All violation blocks are commented out by default.
// =============================================================================

#pragma warning disable CS8632  // nullable annotation context reminder — intentional in sandbox

using Microsoft.Maui.Controls;
using Shaunebu.MAUI.Navigation.Abstractions;

namespace Shaunebu.MAUI.Navigation.Sample.Public.AnalyzerValidation;

// ── Stub types used to construct realistic violation scenarios ────────────────
// These are minimal stand-ins. They mirror the real page names so name-pattern
// based analyzers (SHAUNAV004, SHAUNAV005) fire correctly.

internal sealed class StubLoadingPage : ContentPage { }       // name ends in "LoadingPage"
internal sealed class StubNoInternetPage : ContentPage { }    // name ends in "NoInternetPage"
internal sealed class StubHomePage : ContentPage { }          // safe target — no pattern match

// INavigation parameters in the SHAUNAV002 violation methods are required scaffolding;
// they intentionally reference INavigation to write a realistic PushAsync test.
// Suppress SHAUNAV010 on those parameters so only SHAUNAV002 fires during validation.
#pragma warning disable SHAUNAV010

// =============================================================================
// SHAUNAV001 — Direct Shell.Current.GoToAsync
// =============================================================================
// Expected: Warning at the Shell.Current.GoToAsync call site.
// Analyzer: DirectShellNavigationAnalyzer
// Detection: syntax — receiver is Shell.Current, method is GoToAsync (non-generic).
//
// To validate: uncomment the method body below, build, confirm SHAUNAV001 warning.
// -----------------------------------------------------------------------------
internal sealed class ShauNav001_DirectShellNavigation
{
    private readonly INavigationHandler _navigation;

    public ShauNav001_DirectShellNavigation(INavigationHandler navigation)
        => _navigation = navigation;

    public async Task TriggerAsync()
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV001 ↓↓↓
        // await Shell.Current.GoToAsync("main/home");   // SHAUNAV001 + SHAUNAV003
        await Task.CompletedTask;
    }
}

// =============================================================================
// SHAUNAV002 — Direct NavigationPage API (INavigation.PushAsync etc.)
// =============================================================================
// Expected: Warning at the Navigation.PushAsync call site.
// Analyzer: DirectNavigationPageAnalyzer
// Detection: semantic — receiver type must be INavigation or NavigationProxy;
//            method names: PushAsync, PopAsync, PushModalAsync, PopModalAsync.
//
// To validate: uncomment the method body below, build, confirm SHAUNAV002 warning.
// -----------------------------------------------------------------------------
internal sealed class ShauNav002_DirectNavigationPage
{
    public async Task TriggerAsync(INavigation navigation)
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV002 ↓↓↓
        // await navigation.PushAsync(new StubHomePage());   // SHAUNAV002
        await Task.CompletedTask;
    }

    public async Task TriggerPopAsync(INavigation navigation)
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV002 ↓↓↓
        // await navigation.PopAsync();   // SHAUNAV002
        await Task.CompletedTask;
    }

    public async Task TriggerPushModalAsync(INavigation navigation)
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV002 ↓↓↓
        // await navigation.PushModalAsync(new StubHomePage());   // SHAUNAV002
        await Task.CompletedTask;
    }

    public async Task TriggerPopModalAsync(INavigation navigation)
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV002 ↓↓↓
        // await navigation.PopModalAsync();   // SHAUNAV002
        await Task.CompletedTask;
    }
}

// =============================================================================
// SHAUNAV003 — Magic route string
// =============================================================================
// Expected: Warning at the GoToAsync call that receives a raw string literal.
// Analyzer: MagicRouteStringAnalyzer
// Detection: syntax — non-generic GoToAsync with a string-literal as first argument.
//            Generic GoToAsync<TPage>() is intentionally excluded.
//
// To validate: uncomment the method body below, build, confirm SHAUNAV003 warning.
// -----------------------------------------------------------------------------
internal sealed class ShauNav003_MagicRouteString
{
    private readonly INavigationHandler _navigation;

    public ShauNav003_MagicRouteString(INavigationHandler navigation)
        => _navigation = navigation;

    public async Task TriggerAsync()
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV003 ↓↓↓
        // await Shell.Current.GoToAsync("main/home");   // SHAUNAV003 (magic route string)
        await Task.CompletedTask;
    }
}

// =============================================================================
// SHAUNAV004 — Loading/busy page used as navigation target (ERROR severity)
// =============================================================================
// Expected: Error at the GoToAsync<StubLoadingPage>() call.
// Analyzer: LoadingPageNavigationAnalyzer
// Detection: syntax — type name argument / generic type arg ends with "LoadingPage",
//            "BusyPage", "SpinnerPage", or "LoaderPage" (case-insensitive).
//
// To validate: uncomment the method body below, build, confirm SHAUNAV004 error.
// -----------------------------------------------------------------------------
internal sealed class ShauNav004_LoadingPageAsNavTarget
{
    private readonly INavigationHandler _navigation;

    public ShauNav004_LoadingPageAsNavTarget(INavigationHandler navigation)
        => _navigation = navigation;

    public async Task TriggerGenericAsync()
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV004 ↓↓↓
        // await _navigation.GoToAsync<StubLoadingPage>();   // SHAUNAV004
        await Task.CompletedTask;
    }

    public async Task TriggerPushAsync(INavigation navigation)
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV004 ↓↓↓
        // await navigation.PushAsync(new StubLoadingPage());   // SHAUNAV004
        await Task.CompletedTask;
    }
}

// =============================================================================
// SHAUNAV005 — NoInternet/offline page used as navigation target (ERROR severity)
// =============================================================================
// Expected: Error at the GoToAsync<StubNoInternetPage>() call.
// Analyzer: LoadingPageNavigationAnalyzer (handles both SHAUNAV004 and SHAUNAV005)
// Detection: syntax — type name argument / generic type arg ends with "NoInternetPage",
//            "OfflinePage", "NoConnectionPage", or "NoNetworkPage" (case-insensitive).
//
// To validate: uncomment the method body below, build, confirm SHAUNAV005 error.
// -----------------------------------------------------------------------------
internal sealed class ShauNav005_NoInternetPageAsNavTarget
{
    private readonly INavigationHandler _navigation;

    public ShauNav005_NoInternetPageAsNavTarget(INavigationHandler navigation)
        => _navigation = navigation;

    public async Task TriggerGenericAsync()
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV005 ↓↓↓
        // await _navigation.GoToAsync<StubNoInternetPage>();   // SHAUNAV005
        await Task.CompletedTask;
    }

    public async Task TriggerPushAsync(INavigation navigation)
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV005 ↓↓↓
        // await navigation.PushAsync(new StubNoInternetPage());   // SHAUNAV005
        await Task.CompletedTask;
    }
}

// =============================================================================
// SHAUNAV006 — Navigation call not awaited
// =============================================================================
// Expected: Warning at the bare (non-awaited) invocation statement.
// Analyzer: UnawaitedNavigationAnalyzer
// Detection: semantic — ExpressionStatement containing an invocation whose return
//            type is Task or Task<T>; method name in the navigation set.
//
// Note: the method must be async so the await-less call is syntactically valid.
//
// To validate: uncomment the method body below, build, confirm SHAUNAV006 warning.
// -----------------------------------------------------------------------------
internal sealed class ShauNav006_UnawaitedNavigation
{
    private readonly INavigationHandler _navigation;

    public ShauNav006_UnawaitedNavigation(INavigationHandler navigation)
        => _navigation = navigation;

    public async Task TriggerAsync()
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV006 ↓↓↓
        // _navigation.GoToAsync<StubHomePage>();   // SHAUNAV006 — result discarded, not awaited
        await Task.CompletedTask;
    }
}

// =============================================================================
// SHAUNAV007 — Navigation inside constructor
// =============================================================================
// Expected: Warning at the GoToAsync invocation inside the constructor body.
// Analyzer: NavigationInConstructorAnalyzer
// Detection: syntax — navigation method name appears inside a
//            ConstructorDeclarationSyntax ancestor (before any method / lambda boundary).
//
// To validate: uncomment the constructor body below, build, confirm SHAUNAV007 warning.
// -----------------------------------------------------------------------------
internal sealed class ShauNav007_NavigationInConstructor
{
    private readonly INavigationHandler _navigation;

    public ShauNav007_NavigationInConstructor(INavigationHandler navigation)
    {
        _navigation = navigation;
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV007 ↓↓↓
        // _navigation.GoToAsync<StubHomePage>();   // SHAUNAV007 — navigation inside constructor
    }
}

// =============================================================================
// SHAUNAV008 — Concurrent unawaited navigation (two or more unawaited calls in one method)
// =============================================================================
// Expected: Warning reported on the second (and any subsequent) unawaited call.
// Analyzer: ConcurrentNavigationAnalyzer
// Detection: syntax — collects all bare navigation invocations per method body;
//            fires when count >= 2.
//
// Note: SHAUNAV008 requires at least TWO unawaited calls in the same method.
//       The first call is reported by SHAUNAV006; the second triggers SHAUNAV008.
//
// To validate: uncomment the method body below, build, confirm SHAUNAV008 warning
//              on the second GoToAsync call (and SHAUNAV006 on both).
// -----------------------------------------------------------------------------
internal sealed class ShauNav008_ConcurrentNavigation
{
    private readonly INavigationHandler _navigation;

    public ShauNav008_ConcurrentNavigation(INavigationHandler navigation)
        => _navigation = navigation;

    public async Task TriggerAsync()
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV008 (and SHAUNAV006 on both lines) ↓↓↓
        // _navigation.GoToAsync<StubHomePage>();         // SHAUNAV006 (unawaited)
        // _navigation.GoToAsync<StubHomePage>();         // SHAUNAV006 + SHAUNAV008 (concurrent)
        await Task.CompletedTask;
    }
}

// =============================================================================
// SHAUNAV009 — Application.Current.MainPage.Navigation access
// =============================================================================
// Expected: Warning at the ".Navigation" member-access expression whose receiver
//           ends in ".MainPage".
// Analyzer: ApplicationMainPageNavigationAnalyzer
// Detection: syntax — MemberAccessExpressionSyntax where Name == "Navigation"
//            and the receiver contains ".MainPage".
//
// To validate: uncomment the method body below, build, confirm SHAUNAV009 warning.
// -----------------------------------------------------------------------------
internal sealed class ShauNav009_ApplicationMainPageNavigation
{
    public async Task TriggerAsync()
    {
        // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV009 ↓↓↓
        // Note: must use regular '.' member access (not '?.') — the analyzer detects
        //       SimpleMemberAccessExpression. Null-conditional '?.' is a different SyntaxKind.
        // var nav = Application.Current.MainPage.Navigation;   // SHAUNAV009
        // if (nav is not null)
        //     await nav.PushAsync(new StubHomePage());
        await Task.CompletedTask;
    }
}

// =============================================================================
// SHAUNAV010 — ViewModel field or constructor parameter referencing MAUI types
// =============================================================================
// Expected: Warning on the type reference in the field declaration or parameter.
// Analyzer: ViewModelCoupledToMauiAnalyzer
// Detection: syntax — field declarations and constructor/method parameters whose
//            unqualified type name is Shell, NavigationPage, INavigation, or
//            INavigationService.
//
// Each sub-class below tests one banned type.
//
// To validate: uncomment the relevant class body, build, confirm SHAUNAV010 warning.
// -----------------------------------------------------------------------------

// Re-enable SHAUNAV010 for the section that specifically tests it.
#pragma warning restore SHAUNAV010

// ── SHAUNAV010 via INavigation field ─────────────────────────────────────────
internal sealed class ShauNav010_FieldINavigation
{
    // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV010 ↓↓↓
    // private readonly INavigation _navigation;   // SHAUNAV010

    public ShauNav010_FieldINavigation() { }
}

// ── SHAUNAV010 via Shell field ────────────────────────────────────────────────
internal sealed class ShauNav010_FieldShell
{
    // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV010 ↓↓↓
    // private readonly Shell _shell;   // SHAUNAV010

    public ShauNav010_FieldShell() { }
}

// ── SHAUNAV010 via NavigationPage field ──────────────────────────────────────
internal sealed class ShauNav010_FieldNavigationPage
{
    // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV010 ↓↓↓
    // private readonly NavigationPage _navPage;   // SHAUNAV010

    public ShauNav010_FieldNavigationPage() { }
}

// ── SHAUNAV010 via INavigation constructor parameter ─────────────────────────
internal sealed class ShauNav010_ConstructorParam
{
    // ↓↓↓ UNCOMMENT TO VALIDATE SHAUNAV010 ↓↓↓
    // public ShauNav010_ConstructorParam(INavigation navigation) { }   // SHAUNAV010 on the parameter type
    public ShauNav010_ConstructorParam() { }
}

// =============================================================================
// END OF ANALYZER VALIDATION SANDBOX
// =============================================================================
// Validation results — confirmed triggering correctly on first validation pass:
//   [x] SHAUNAV001 — Direct Shell.Current.GoToAsync                          (Warning)
//   [x] SHAUNAV002 — Direct NavigationPage API: PushAsync / PopAsync /
//                    PushModalAsync / PopModalAsync                           (Warning)
//   [x] SHAUNAV003 — Magic route string                                       (Warning)
//   [x] SHAUNAV004 — Loading page as nav target                               (Error)
//   [x] SHAUNAV005 — NoInternet page as nav target                            (Error)
//   [x] SHAUNAV006 — Unawaited navigation call                                (Warning)
//   [x] SHAUNAV007 — Navigation inside constructor                            (Warning)
//   [x] SHAUNAV008 — Concurrent unawaited navigation                          (Warning)
//   [x] SHAUNAV009 — Application.Current.MainPage.Navigation                  (Warning)
//                    NOTE: requires '.' not '?.' — null-conditional is not detected.
//   [x] SHAUNAV010 — ViewModel coupled to MAUI navigation types               (Warning)
//                    Confirmed via INavigation field and Shell field variants.
//
// Live violations found in production code during validation:
//   SHAUNAV002 — OverlayHost.cs: PushModalAsync / PopModalAsync (intentional platform-level usage)
//   SHAUNAV002 — AppShell.xaml.cs: PushAsync (intentional Shell-level usage)
// =============================================================================
