# Installation

## Overview

`Shaunebu.MAUI.Navigation` is distributed as a set of NuGet packages and an optional Visual Studio extension (VSIX). This page covers all installation paths.

---

## NuGet Packages

### Core Package

Add to your .NET MAUI application project:

```xml
<PackageReference Include="Shaunebu.MAUI.Navigation" Version="1.0.0-preview.1" />
```

This package registers all core services including `INavigationHandler`, `INavigationFlowManager`, `IOverlayNavigationService`, `INavigationStackInspector`, `INavigationDiagnostics`, and the navigation pipeline.

### Source Generators

```xml
<PackageReference Include="Shaunebu.MAUI.Navigation.Generators" Version="1.0.0-preview.1" />
```

Provides the `[NavigationRoute]` attribute and the incremental Roslyn generators that emit:
- Route constant classes
- `GeneratedNavigationRegistration.RegisterGeneratedRoutes(options)` 
- Typed navigation extension methods (per-page `GoTo*Async` helpers)

### Roslyn Analyzers

```xml
<PackageReference Include="Shaunebu.MAUI.Navigation.Analyzers"
				  Version="1.0.0-preview.1"
				  PrivateAssets="all">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Enforces 10 diagnostic rules (SHAUNAV001–SHAUNAV010) at compile time. These analyzers are development-only (`PrivateAssets="all"`) and do not ship in your NuGet output.

### Code Fixes

```xml
<PackageReference Include="Shaunebu.MAUI.Navigation.CodeFixes"
				  Version="1.0.0-preview.1"
				  PrivateAssets="all">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Provides automated Roslyn code fixes for select analyzer violations. Install alongside the Analyzers package.

### Navigation Debugger

```xml
<!-- Only add to the app project — guard with a condition or #if DEBUG usage -->
<PackageReference Include="Shaunebu.MAUI.Navigation.Debugger" Version="1.0.0-preview.1" />
```

Provides the runtime diagnostics platform: `NavigationDiagnosticsBus`, `NavigationSessionRecorder`, `NavigationRuntimeWarningEngine`, `NavigationStackDiffEngine`, `NavigationDiagnosticsExporter`, and `NavigationTimelineReplayer`.

> **Important:** Only call `UseNavigationDebugger(...)` inside a `#if DEBUG` block. The debugger package should **not** be referenced in Release build configurations of production apps.

---

## Recommended Project File Configuration

A typical `.csproj` for a production MAUI app:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
	<TargetFrameworks>net9.0-android;net9.0-ios;net9.0-maccatalyst</TargetFrameworks>
	<OutputType>Exe</OutputType>
	<RootNamespace>MyApp</RootNamespace>
	<Nullable>enable</Nullable>
	<ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
	<!-- Core navigation -->
	<PackageReference Include="Shaunebu.MAUI.Navigation"           Version="1.0.0-preview.1" />
	<PackageReference Include="Shaunebu.MAUI.Navigation.Generators" Version="1.0.0-preview.1" />

	<!-- Compile-time enforcement (dev-only) -->
	<PackageReference Include="Shaunebu.MAUI.Navigation.Analyzers"
					  Version="1.0.0-preview.1"
					  PrivateAssets="all">
	  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	</PackageReference>
	<PackageReference Include="Shaunebu.MAUI.Navigation.CodeFixes"
					  Version="1.0.0-preview.1"
					  PrivateAssets="all">
	  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
	</PackageReference>
  </ItemGroup>

  <!-- Debugger: referenced only in Debug configuration -->
  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
	<PackageReference Include="Shaunebu.MAUI.Navigation.Debugger" Version="1.0.0-preview.1" />
  </ItemGroup>
</Project>
```

---

## VSIX Extension Installation

The **Navigation Inspector** VSIX extension adds a dedicated tool window to Visual Studio for live navigation diagnostics and offline session analysis.

1. Open Visual Studio.
2. Go to **Extensions → Manage Extensions**.
3. Search for **"Shaunebu Navigation Inspector"**.
4. Click **Download** and restart Visual Studio.

> Compatible with Visual Studio 2019 (16.x) and later, including Visual Studio 2022 and Visual Studio 2026.

### Opening the Tool Window

After installation, open the inspector via:

- **View → Other Windows → Navigation Inspector**, or
- The keyboard shortcut configured in Visual Studio (defaults to `Ctrl+Shift+N, Ctrl+Shift+I`)

---

## Verifying Installation

After setup, build your project. If the generators are active you will see a generated file similar to:

```
obj/Debug/net9.0-android/generated/Shaunebu.MAUI.Navigation.Generators/
	GeneratedNavigationRegistration.g.cs
	NavigationRoutes.g.cs
```

If analyzers are active, violations like direct `Shell.Current.GoToAsync(...)` calls will produce warnings `SHAUNAV001` through `SHAUNAV010` in the Error List.

---

## Related Pages

- [Getting Started](getting-started.md)
- [Generators Overview](../generators/overview.md)
- [Analyzers Overview](../analyzers/overview.md)
- [VSIX Overview](../vsix/overview.md)
