# Generator Diagnostics

## Overview

The source generators emit compile-time diagnostics (SHAUNGEN001–SHAUNGEN007) to surface route configuration errors early — before they become runtime failures.

---

## Diagnostic Reference

### SHAUNGEN001 — Duplicate navigation route

| Property | Value |
|---|---|
| Severity | **Error** |
| Trigger | Two or more `[NavigationRoute]` attributes share the same route string |

```
Route 'main/home' is registered on both 'HomePage' and 'HomeV2Page'.
Each route must be unique.
```

**Fix:** Assign a unique route string to each page.

---

### SHAUNGEN002 — Non-partial class with [NavigationRoute]

| Property | Value |
|---|---|
| Severity | **Error** |
| Trigger | `[NavigationRoute]` applied to a class that is not declared `partial` |

```
'HomePage' must be declared as 'partial' to use [NavigationRoute]
```

**Fix:** Add the `partial` modifier to the page class.

```csharp
// Before
[NavigationRoute("main/home")]
public class HomePage : ContentPage { }

// After
[NavigationRoute("main/home")]
public partial class HomePage : ContentPage { }
```

---

### SHAUNGEN003 — Invalid route format

| Property | Value |
|---|---|
| Severity | **Error** |
| Trigger | Route string contains characters outside `[a-zA-Z0-9/_-]` or is empty |

```
Route 'main home' on 'HomePage' is invalid.
Routes must be non-empty and contain only alphanumeric characters, '/', '-', and '_'.
```

**Fix:** Use a path-like route string without spaces, dots, query strings, or URI schemes.

```csharp
// Invalid
[NavigationRoute("main home")]   // space
[NavigationRoute("main.home")]   // dot
[NavigationRoute("//main/home")] // scheme

// Valid
[NavigationRoute("main/home")]
[NavigationRoute("main-home")]
```

---

### SHAUNGEN004 — Conflicting generated name

| Property | Value |
|---|---|
| Severity | **Error** |
| Trigger | Two pages in different namespaces share the same simple class name, producing a name collision in generated extension methods |

```
Pages 'App.Home.HomePage' and 'App.Auth.HomePage' both produce
the generated name 'GoToHomePageAsync'. Rename one of the pages.
```

**Fix:** Rename one of the conflicting page classes so they produce distinct extension method names.

---

### SHAUNGEN005 — Invalid flow name

| Property | Value |
|---|---|
| Severity | **Warning** |
| Trigger | `Flow` property on `[NavigationRoute]` contains characters outside `[a-zA-Z0-9_-]` or is empty |

```
Flow name 'Auth Flow' on 'LoginPage' is invalid.
Flow names must be non-empty and contain only letters, digits, hyphens, and underscores.
```

**Fix:** Use a simple alphanumeric identifier: `"Auth"`, `"Main"`, `"Onboarding"`.

---

### SHAUNGEN006 — Non-partial record with [NavigationParameters]

| Property | Value |
|---|---|
| Severity | **Warning** |
| Trigger | `[NavigationParameters]` applied to a type that is not a `partial record` |

```
'ProductParams' must be a 'partial record' to use [NavigationParameters]
```

**Fix:** Declare the type as a `partial record`:

```csharp
// Before
[NavigationParameters]
public sealed record ProductParams(int Id);

// After
[NavigationParameters]
public sealed partial record ProductParams(int Id) : INavigationParameters;
```

---

### SHAUNGEN007 — Non-partial class with [NavigationFlow]

| Property | Value |
|---|---|
| Severity | **Error** |
| Trigger | `[NavigationFlow]` applied to a class that is not declared `partial` |

```
'AuthFlow' must be declared as 'partial' to use [NavigationFlow]
```

**Fix:** Add the `partial` modifier to the flow class.

---

## Suppression

Diagnostics can be suppressed in `.editorconfig` when a deviation is intentional:

```ini
# .editorconfig
[*.cs]
dotnet_diagnostic.SHAUNGEN005.severity = none
```

> Suppress sparingly. Most SHAUNGEN errors indicate genuine route configuration problems that will cause runtime navigation failures.

---

## Related Pages

- [Generators Overview](overview.md)
- [NavigationRouteAttribute](../navigation/navigation-routes.md)
- [Analyzers Overview](../analyzers/overview.md)
