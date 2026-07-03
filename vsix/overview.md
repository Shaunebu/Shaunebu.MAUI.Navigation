# VSIX Extension Overview

## Overview

The **Navigation Session Inspector** is a Visual Studio extension (`.vsix`) that brings the debugger's session recording, timeline replay, stack diffing, and runtime warnings into a native Visual Studio tool window.

It connects to a running MAUI debug session that has `UseNavigationDebugger` registered and lets you inspect the full navigation history without leaving Visual Studio.

---

## Installation

1. Open Visual Studio.
2. Go to **Extensions → Manage Extensions**.
3. Search for **"Shaunebu Navigation Inspector"**.
4. Click **Download**, then restart Visual Studio to complete installation.

> The extension targets **.NET Framework 4.8** (the VS extension host process). It is compatible with Visual Studio 2019 (16.x) and later.

---

## Opening the Tool Window

After installation, open the Navigation Inspector from the Visual Studio menu:

**View → Other Windows → Navigation Session Inspector**


 ![https://jpdblog.blob.core.windows.net/apps/Libraries/NavigationVsix.gif](https://jpdblog.blob.core.windows.net/apps/Libraries/NavigationVsix.gif)


Or use the keyboard shortcut if you assigned one during install.

---

## Architecture

```
Visual Studio Process (.NET Framework 4.8)
  └── NavigationInspectorPackage          (AsyncPackage)
		├── OpenInspectorCommand          (menu command handler)
		├── NavigationInspectorToolWindow (tool window host)
		│     └── WpfUiDispatcher         (marshal calls to UI thread)
		└── Shaunebu.MAUI.Navigation.Vsix.Desktop
			  ├── DebuggerDashboardViewModel  (from Debugger package)
			  ├── TimelinePanelViewModel
			  ├── WarningsPanelViewModel
			  └── StackPanelViewModel
```

The VSIX package (`NavigationInspectorPackage`) hosts the tool window and wires up the WPF UI. All navigation intelligence — replay, diffing, warnings — comes from the `Shaunebu.MAUI.Navigation.Debugger` and `Shaunebu.MAUI.Navigation.Vsix.Desktop` assemblies.

---

## NavigationInspectorPackage

The package auto-loads on shell initialization via three `[ProvideAutoLoad]` registrations:

| Context | GUID | Trigger |
|---|---|---|
| `UICONTEXT_ShellInitialized` | `E7B90C87-...` | VS shell fully loaded |
| `UICONTEXT_NoSolution` | `ADFC4E64-...` | No solution open |
| `UICONTEXT_SolutionExists` | `F1536EF8-...` | Solution loaded |

`AllowsBackgroundLoading = true` ensures the package does not block VS startup.

---

## Tool Window

The `NavigationInspectorToolWindow` hosts:

| Panel | Description |
|---|---|
| **Live Events** | Real-time stream of navigation events during a debug session |
| **Navigation Stack** | Current navigation and modal stack state |
| **Timeline** | Frame-by-frame replay of a recorded or imported session |
| **Warnings** | Runtime warnings from the warning engine |

---

## Opening a Session File

To replay a session exported from a device or emulator:

1. Export the session from your app: `await _exporter.ExportToFileAsync(session, path, DiagnosticsExportFormat.Json)`.
2. Transfer the `.json` file to your development machine.
3. In the Navigation Inspector tool window, click **Open Session**.
4. Select the `.json` file.
5. Use the step forward/backward controls to replay.

See [Exporting Sessions](../debugger/exporting-sessions.md) for export guidance.

---

## WpfUiDispatcher

All ViewModel property changes are dispatched to the VS UI thread via `WpfUiDispatcher`, which wraps `JoinableTaskFactory.SwitchToMainThreadAsync`. This ensures `PropertyChanged` events are always raised on the correct thread without blocking the background processing pipeline.

---

## Related Pages

- [Debugger Overview](../debugger/overview.md)
- [Exporting Sessions](../debugger/exporting-sessions.md)
- [Timeline Replay](../debugger/timeline-replay.md)
- [Runtime Warnings](../debugger/runtime-warnings.md)
- [Installation](../installation.md)
