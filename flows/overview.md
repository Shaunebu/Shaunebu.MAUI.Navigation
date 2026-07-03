# Flows Overview

## Overview

A **navigation flow** is a named application context — a logical grouping of pages and behavior that represents a major state in the app. Common flows include:

| Flow | Purpose |
|---|---|
| `AuthFlow` | Unauthenticated entry point (Login, Register, Privacy) |
| `MainFlow` | Authenticated home experience (Home, Settings, Profile) |
| `OnboardingFlow` | First-run walkthrough |
| `MaintenanceFlow` | App-wide maintenance gate |

Flows are the recommended mechanism for **major application state transitions**. Use `INavigationFlowManager.ResetToFlowAsync<TFlow>()` instead of manually manipulating the back stack or calling `SetRootAsync` directly.

---

## Why Use Flows?

Without flows, transitioning from login to home requires:
1. `SetRootAsync<HomePage>()` — to replace the root
2. Manual cleanup — to clear any lingering session state
3. Shell route rewiring — to update which routes are accessible

With flows:
```csharp
// One call handles root replacement, lifecycle hooks, and flow context
await _flowManager.ResetToFlowAsync<MainFlow>();
```

The flow manager:
- Fires `OnExitAsync` on the outgoing flow
- Fires `OnEnterAsync` on the incoming flow
- Sets the root page to `INavigationFlow.RootPageType`
- Clears the back stack
- Updates `INavigationFlowManager.CurrentFlow`
- Records the transition in `FlowHistory`

---

## Flow-Aware Route Resolution

When multiple flows register the same page type, the `INavigationRouteRegistry` selects the route belonging to the currently active flow:

```csharp
options.RegisterPage<PrivacyPage>("auth/privacy",  flow: "Auth");
options.RegisterPage<PrivacyPage>("main/privacy",  flow: "Main");

// Navigation — route selected based on CurrentFlow
await _navigation.GoToAsync<PrivacyPage>();
// Auth flow active → navigates to "auth/privacy"
// Main flow active → navigates to "main/privacy"
```

---

## Flow Lifecycle

```
App start
   │
   ▼
ResetToFlowAsync<AuthFlow>()
   │  OnEnterAsync(context)
   │
   ▼
[Auth pages: Login, Register, Privacy]
   │
   ▼  (login success)
ResetToFlowAsync<MainFlow>()
   │  Previous flow: OnExitAsync(context)
   │  New flow:      OnEnterAsync(context)
   │
   ▼
[Main pages: Home, Settings, Profile]
   │
   ▼  (logout)
ResetToFlowAsync<AuthFlow>()
   │  Main flow: OnExitAsync(context)
   │  Auth flow: OnEnterAsync(context)
   ▼
[Auth pages again — clean state]
```

---

## INavigationFlowManager — Quick Reference

```csharp
// Read current state
INavigationFlow? current = _flowManager.CurrentFlow;
IReadOnlyList<INavigationFlow> history = _flowManager.FlowHistory;

// Transition to a flow (fires entry/exit hooks, clears back stack)
await _flowManager.ResetToFlowAsync<MainFlow>();

// Start a flow without clearing back stack
await _flowManager.StartFlowAsync<OnboardingFlow>();

// Complete current flow and return to previous
await _flowManager.CompleteCurrentFlowAsync();
```

See [flow-manager.md](flow-manager.md) for the full API.

---

## Registration

Register each flow type as **both** its concrete type and `INavigationFlow`. This allows the flow manager to resolve by type while also enumerating all registered flows:

```csharp
// MauiProgram.cs
builder.Services.AddSingleton<AuthFlow>();
builder.Services.AddSingleton<MainFlow>();
builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<AuthFlow>());
builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<MainFlow>());
```

---

## Related Pages

- [Creating Flows](creating-flows.md)
- [Flow Manager](flow-manager.md)
- [Flow Context](flow-context.md)
- [Navigation Routes](../navigation/navigation-routes.md)
- [Guards Overview](../guards/overview.md)
