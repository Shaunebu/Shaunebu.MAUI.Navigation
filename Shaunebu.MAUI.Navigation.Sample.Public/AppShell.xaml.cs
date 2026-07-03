using Microsoft.Extensions.Logging;
using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Flows;
using Shaunebu.MAUI.Navigation.Sample.Public.Services;

namespace Shaunebu.MAUI.Navigation.Sample.Public;

public sealed partial class AppShell : Shell
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AppShell> _logger;
    // Overlay host is held as a field so it is not garbage-collected.
    private readonly OverlayHost _overlayHost;
    private bool _initialized;

    public AppShell(
        IServiceProvider services,
        ILogger<AppShell> logger,
        OverlayHost overlayHost)
    {
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine("[Startup] AppShell.ctor: ENTER");
        System.Diagnostics.Debug.WriteLine("[Startup] TEST CONFIGURATION: Absolute Minimal Shell (Test 1)");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");

        try
        {
            System.Diagnostics.Debug.WriteLine("[Startup] AppShell.ctor: Calling InitializeComponent...");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[Startup] AppShell.ctor: ✓ InitializeComponent completed successfully");

            // Log Shell structure immediately after XAML load
            var itemCount = Items?.Count ?? 0;
            System.Diagnostics.Debug.WriteLine($"[Startup] AppShell.ctor: Shell.Items.Count = {itemCount}");

            if (Items != null)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    System.Diagnostics.Debug.WriteLine($"[Startup] AppShell.ctor:   [{i}] ShellItem Route='{item.Route ?? "(null)"}' Title='{item.Title ?? "(null)"}'");

                    if (item.Items != null)
                    {
                        foreach (var section in item.Items)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Startup] AppShell.ctor:       ShellSection Route='{section.Route ?? "(null)"}'");
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("[Startup] AppShell.ctor: Shell structure logged successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("[Startup] AppShell.ctor: ✗✗✗ EXCEPTION DURING INITIALIZATION ✗✗✗");
            System.Diagnostics.Debug.WriteLine($"[Startup] Exception Type: {ex.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"[Startup] Exception Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Startup] Stack Trace:");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[Startup] Inner Exception: {ex.InnerException.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"[Startup] Inner Message: {ex.InnerException.Message}");
            }

            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");

            logger.LogError(ex, "AppShell InitializeComponent failed");
            throw;
        }

        _services = services;
        _logger = logger;
        // Resolve eagerly so StateChanged subscription is active before any navigation.
        _overlayHost = overlayHost;

        System.Diagnostics.Debug.WriteLine("[Startup] AppShell.ctor: Dependencies assigned");
        System.Diagnostics.Debug.WriteLine("[Startup] AppShell.ctor: EXIT (constructor completed successfully)");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
            return;

        // Mark initialized immediately to avoid duplicate scheduling.
        _initialized = true;

        // Defer startup navigation until after first frame to avoid Android Shell startup timing issues.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () => { _ = InitializeStartupNavigationAsync(); });
    }

    private async Task InitializeStartupNavigationAsync()
    {
        try
        {
            // Ensure Shell knows about all registered routes from the navigation registry so
            // Shell.GoToAsync can resolve registered route strings (e.g. "auth/login").
            try
            {
                var registry = _services.GetService(typeof(Shaunebu.MAUI.Navigation.Routing.INavigationRouteRegistry)) as Shaunebu.MAUI.Navigation.Routing.INavigationRouteRegistry;
                if (registry is not null)
                {
                    foreach (var desc in registry.GetRegisteredRoutes())
                    {
                        try
                        {
                            // Only register truly global routes with MAUI Routing. Flow-scoped
                            // routes are represented in the Shell visual hierarchy (AppShell.xaml)
                            // and registering them as global routes causes Shell to treat them
                            // as 'global' which prevents absolute root replacement. Skip those.
                            if (string.IsNullOrEmpty(desc.FlowName))
                                Microsoft.Maui.Controls.Routing.RegisterRoute(desc.Route, desc.PageType);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }
            }
            catch
            {
                // Non-fatal — shell route registration best-effort only.
            }

            var flowManager = _services.GetRequiredService<INavigationFlowManager>();

            // Ensure MAUI Shell internal routing has initialized. Some platforms may
            // not have Shell's internal navigation table ready immediately on OnAppearing,
            // so wait a short time (with timeout) before performing absolute/flow-root
            // navigation. This avoids ShellUriHandler inability to resolve //auth/login.
            try
            {
                // Wait until Shell.Current has been fully initialized and the Shell visual
                // tree (with our ShellItems) is present. Some platforms initialize the
                // Shell internals slightly after OnAppearing — poll with a short timeout.
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.Elapsed < TimeSpan.FromSeconds(3))
                {
                    var shell = Microsoft.Maui.Controls.Shell.Current;
                    if (shell is not null)
                    {
                        // Ensure shell handler is ready and our auth ShellItem exists.
                        var handlerReady = shell.Handler is not null;
                        var hasAuthItem = shell.Items.Any(i => string.Equals(i.Route, "auth", StringComparison.OrdinalIgnoreCase));
                        if (handlerReady && hasAuthItem)
                            break;
                    }

                    await Task.Delay(50).ConfigureAwait(false);
                }
            }
            catch
            {
                // ignore and proceed — we will attempt navigation regardless.
            }

#if DEBUG
            // ── Startup route diagnostics ─────────────────────────────────────
            // Log every route registered in the Shell visual hierarchy so we can
            // verify auth/register is reachable before any navigation is attempted.
            // Remove or gate behind a debug flag for production builds.
            try
            {
                LogShellRouteTree();
            }
            catch (Exception diagEx)
            {
                _logger.LogWarning(diagEx, "Shell route diagnostics failed.");
            }
#endif

            // Use StartFlowAsync on startup instead of ResetToFlowAsync. ResetToFlowAsync
            // attempts an absolute root replacement which can fail under certain Shell
            // configurations (absolute routing requirements). Starting the flow will
            // push the flow's root in a compatible way while preserving stability.
            var result = await flowManager.StartFlowAsync<AuthFlow>();

            // Log startup navigation result to aid troubleshooting when running the sample.
            try
            {
                _logger.LogInformation("Startup navigation completed. Success={Succeeded} FailureReason={FailureReason} Message={Message}",
                    result?.Succeeded, result?.FailureReason, result?.Message);
            }
            catch
            {
                // Swallow logging errors to avoid crashing startup.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup navigation failed.");
        }
    }

#if DEBUG
    /// <summary>
    /// Logs the full Shell route tree to help diagnose "unable to figure out route" failures.
    ///
    /// For every route string of the form "//flowName/pageName" to work at runtime, the Shell
    /// visual tree must have:
    ///   ShellItem.Route == "flowName"
    ///   ShellSection.Route == "pageName"   ← must be explicit and match the ShellContent route
    ///   ShellContent.Route == "pageName"
    ///
    /// If a ShellSection has no explicit Route, MAUI assigns "ShellSection_N" which breaks
    /// absolute navigation to "//flowName/pageName".
    /// </summary>
    private void LogShellRouteTree()
    {
        var shell = Shell.Current;
        if (shell is null)
        {
            _logger.LogWarning("[ShellDiagnostics] Shell.Current is null — cannot enumerate routes.");
            return;
        }

        _logger.LogInformation("[ShellDiagnostics] Shell route tree ({ItemCount} ShellItem(s)):", shell.Items.Count);

        foreach (var item in shell.Items)
        {
            _logger.LogInformation("[ShellDiagnostics]   ShellItem Route={ItemRoute}", item.Route);
            foreach (var section in item.Items)
            {
                _logger.LogInformation("[ShellDiagnostics]     ShellSection Route={SectionRoute}", section.Route);
                foreach (var content in section.Items)
                {
                    var absoluteUri = $"//{item.Route}/{content.Route}";
                    _logger.LogInformation("[ShellDiagnostics]       ShellContent Route={ContentRoute}  →  {AbsoluteUri}",
                        content.Route, absoluteUri);
                }
            }
        }

        // Verify key routes.
        CheckRoute("//auth/login");
        CheckRoute("//auth/register");
        CheckRoute("//main/home");

        void CheckRoute(string uri)
        {
            // Walk the tree to see if this absolute URI resolves.
            var parts = uri.TrimStart('/').Split('/');
            if (parts.Length < 2)
            {
                _logger.LogWarning("[ShellDiagnostics] Cannot validate route — too few segments: {Uri}", uri);
                return;
            }

            var itemRoute = parts[0];
            var contentRoute = parts[1];

            var found = shell.Items
                .Where(i => string.Equals(i.Route, itemRoute, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => i.Items)
                .SelectMany(s => s.Items)
                .Any(c => string.Equals(c.Route, contentRoute, StringComparison.OrdinalIgnoreCase));

            if (found)
                _logger.LogInformation("[ShellDiagnostics] ✓ Route resolved in visual tree: {Uri}", uri);
            else
                _logger.LogWarning("[ShellDiagnostics] ✗ Route NOT found in visual tree: {Uri}  " +
                    "— check that ShellSection.Route and ShellContent.Route both equal '{ContentRoute}' " +
                    "under ShellItem.Route='{ItemRoute}'.", uri, contentRoute, itemRoute);
        }
    }
#endif

    private async void OnDiagnosticsClicked(object sender, EventArgs e)
    {
#if DEBUG
        try
        {
            var page = _services.GetService(typeof(Pages.Debug.DiagnosticsPage)) as Page;
            if (page is not null)
                await Shell.Current.Navigation.PushAsync(page).ConfigureAwait(false);
        }
        catch
        {
            // Swallow — diagnostics must never crash the sample.
        }
#endif
    }
}