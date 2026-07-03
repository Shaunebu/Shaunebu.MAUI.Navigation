using Microsoft.Extensions.Logging;
using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Extensions;
using Shaunebu.MAUI.Navigation.Sample.Public.Flows;
using Shaunebu.MAUI.Navigation.Sample.Public.Guards;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Auth;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Main;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Shared;
using Shaunebu.MAUI.Navigation.Sample.Public.Services;
using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Auth;
using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;
using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Shared;
#if DEBUG
using Shaunebu.MAUI.Navigation.Debugger.Extensions;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Debug;
#endif

namespace Shaunebu.MAUI.Navigation.Sample.Public;

/// <summary>
/// MAUI application entry point.
///
/// Demonstrates enterprise-grade DI wiring for Shaunebu.MAUI.Navigation:
/// - UseShaunebuNavigation with full options.
/// - Flow-scoped route registration (shared PrivacyPage per flow).
/// - Guard registration.
/// - Page + ViewModel transient registration.
/// - Overlay system enabled.
/// - Diagnostics logging enabled.
/// </summary>
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		System.Diagnostics.Debug.WriteLine("[Startup] MauiProgram.CreateMauiApp: enter");
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// ── Navigation library ────────────────────────────────────────────────────
		//
		// UseShaunebuNavigation wires all core services:
		//   INavigationHandler, INavigationFlowManager, IOverlayNavigationService,
		//   INavigationStackInspector, INavigationDiagnostics, guard pipeline, etc.
		//
		System.Diagnostics.Debug.WriteLine("[Startup] UseShaunebuNavigation: enter");
		builder.UseShaunebuNavigation(options =>
		{
			// Enterprise defaults demonstrated in the spec (section 64).
			// Use Shell navigation by default in the MAUI sample so Shell.Current and
			// the Shell navigation adapter are used for route-based and typed navigation.
			options.DefaultNavigationMode  = NavigationPresentationMode.Shell;
			options.PreventDoubleNavigation = true;
			options.DoubleNavigationThreshold = TimeSpan.FromMilliseconds(750);
			options.EnableDiagnostics = true;          // Emits structured ILogger output.
			options.EnableNavigationGuards = true;
			options.EnableBackButtonHandling = true;
			options.EnableOverlaySystem = true;
			options.ThrowOnNavigationFailure = false;  // Safe default: return NavigationResult.
			options.DefaultAnimated = true;

			// ── Route registration ─────────────────────────────────────────────
			//
			// All routes are now registered via the source generator.
			// GeneratedNavigationRegistration discovers all [NavigationRoute]-decorated pages,
			// including flow-scoped (Auth/Main) and shared/global routes.
			GeneratedNavigationRegistration.RegisterGeneratedRoutes(options);
		});
		System.Diagnostics.Debug.WriteLine("[Startup] UseShaunebuNavigation: exit");

#if DEBUG
        // ── Navigation debugger ───────────────────────────────────────────────────
        //
        // UseNavigationDebugger wires the full diagnostics platform:
        //   - NavigationDiagnosticsBus (channel-backed, bounded)
        //   - NavigationSessionRecorder (records all operations into a replay-ready session)
        //   - NavigationRuntimeWarningEngine (live rule evaluation)
        //   - NavigationDiagnosticsExporter / Importer (JSON export and import)
        //   - NavigationTimelineReplayer (M5/M6 replay infrastructure)
        //   - DebuggerOverlayHost (passive live-monitoring feed)
        //   - DebuggerDashboardViewModel + TimelinePanelViewModel (UI-bindable state)
        //
        // This call is conditionally compiled — the debugger package is NOT referenced
        // in Release builds, so zero debugger code ships in production.
        System.Diagnostics.Debug.WriteLine("[Startup] UseNavigationDebugger: enter");
		builder.Services.UseNavigationDebugger(opts =>
		{
			opts.EnableSessionRecording = true;
			opts.EnableRuntimeWarnings  = true;
			opts.EnableStackDiffing     = true;
			opts.EnableExport           = true;
			opts.EnableLiveStreaming     = true;
			// Keep MaxOperationRecords small for the sample to stay memory-friendly.
			opts.MaxOperationRecords    = 200;
		});
		System.Diagnostics.Debug.WriteLine("[Startup] UseNavigationDebugger: exit");

		// ── Live-streaming startup diagnostic ─────────────────────────────────────
		// Reports the resolved option values and DI registration state to both
		// Debug.WriteLine (IDE Output / logcat) and Console.WriteLine (adb logcat).
		{
			var sp           = builder.Services;
			var opts         = sp.LastOrDefault(d => d.ServiceType == typeof(Shaunebu.MAUI.Navigation.Debugger.Options.NavigationDebuggerOptions));
			var hasPublisher = sp.Any(d => d.ServiceType == typeof(Shaunebu.MAUI.Navigation.Debugger.Live.LiveNavigationPublisher));
			var hasInitSvc   = sp.Any(d => d.ServiceType == typeof(Microsoft.Maui.Hosting.IMauiInitializeService)
											   && d.ImplementationType?.Name == "LivePublisherMauiInitializer");

			// Resolve the options instance so we can print actual values.
			Shaunebu.MAUI.Navigation.Debugger.Options.NavigationDebuggerOptions? resolvedOpts = null;
			try
			{
				// Build a temporary scope just to read the options — the full app isn't built yet
				// so we use the registered singleton factory if available.
				if (opts?.ImplementationInstance is Shaunebu.MAUI.Navigation.Debugger.Options.NavigationDebuggerOptions o)
					resolvedOpts = o;
			}
			catch { /* best-effort */ }

			var enableLive = resolvedOpts?.EnableLiveStreaming;
			var livePort   = resolvedOpts?.LivePort;

			var msg = $"[StartupDiag] EnableLiveStreaming={enableLive?.ToString() ?? "?"}" +
					  $" | LivePort={livePort?.ToString() ?? "?"}" +
					  $" | LiveNavigationPublisher registered={hasPublisher}" +
					  $" | LivePublisherMauiInitializer registered={hasInitSvc}";

			Console.WriteLine(msg);
			System.Diagnostics.Debug.WriteLine(msg);
		}

		// Register debugger UI pages as transient so each navigation produces a fresh binding.
		builder.Services.AddTransient<DiagnosticsPage>();
		builder.Services.AddTransient<DebuggerShellPage>();
#endif

		// ── Navigation guards ─────────────────────────────────────────────────────
		//
		// Guards are evaluated in registration order.
		// SampleMaintenanceGuard runs first so maintenance mode takes priority.
		// SampleAuthGuard redirects unauthenticated users away from main/* routes.
		builder.Services.AddSingleton<INavigationGuard, SampleMaintenanceGuard>();
		builder.Services.AddSingleton<INavigationGuard, SampleAuthGuard>();

        // ── Flows ─────────────────────────────────────────────────────────────────
		// Register concrete flow types and map the INavigationFlow interface to the same instances.
		// This ensures resolving by concrete type (ResolveFlow<TFlow>() uses GetService(typeof(TFlow))).
		builder.Services.AddSingleton<AuthFlow>();
		builder.Services.AddSingleton<MainFlow>();
		builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<AuthFlow>());
		builder.Services.AddSingleton<INavigationFlow>(sp => sp.GetRequiredService<MainFlow>());

		// ── Application services ──────────────────────────────────────────────────
		builder.Services.AddSingleton<IAuthService, InMemoryAuthService>();
		builder.Services.AddSingleton<IMaintenanceService, InMemoryMaintenanceService>();

		// ── Sample overlay host ───────────────────────────────────────────────────
		// OverlayHost subscribes to IOverlayNavigationService.StateChanged and presents
		// real modal overlay pages above Shell content. It is resolved eagerly in AppShell
		// so the subscription is active before any navigation occurs.
		builder.Services.AddSingleton<OverlayHost>();

		// ── Shell + App ───────────────────────────────────────────────────────────
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<App>();

		// ── Framework Isolation Test Shells ───────────────────────────────────────
		// STEP 2: Shell with FlyoutBehavior="Disabled"
		// STEP 3: Shell with raw ContentPage
		builder.Services.AddTransient<TestShellStep2>();
		builder.Services.AddTransient<TestShellStep3>();

		// Restore standard runtime registrations (instrumentation removed)

		// ── Pages (Transient — new instance per navigation) ───────────────────────
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<ProfilePage>();
		builder.Services.AddTransient<EditProfilePage>();
		builder.Services.AddTransient<ProductDetailsPage>();
		builder.Services.AddTransient<PrivacyPage>();
		builder.Services.AddTransient<TermsPage>();
		builder.Services.AddTransient<MaintenancePage>();

		// ── ViewModels (Transient — matches page lifetime) ────────────────────────
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<ProfileViewModel>();
		builder.Services.AddTransient<EditProfileViewModel>();
		builder.Services.AddTransient<ProductDetailsViewModel>();
		builder.Services.AddTransient<PrivacyViewModel>();
		builder.Services.AddTransient<TermsViewModel>();
		builder.Services.AddTransient<MaintenanceViewModel>();

		// ── Unsaved-changes guard ─────────────────────────────────────────────────
		//
		// EditProfileUnsavedChangesGuard inspects the current page's BindingContext
		// at guard evaluation time. It is registered globally but short-circuits
		// when the current page's ViewModel does not implement IUnsavedChangesSource.
		builder.Services.AddSingleton<INavigationGuard, EditProfileUnsavedChangesGuard>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		System.Diagnostics.Debug.WriteLine("[Startup] MauiProgram.CreateMauiApp: exit (building app)");
		return builder.Build();
	}
}

