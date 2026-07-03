using Microsoft.Extensions.Logging;
using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Flows;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Main;
using Shaunebu.MAUI.Navigation.Sample.Public.Parameters;
using Shaunebu.MAUI.Navigation.Sample.Public.Services;
#if DEBUG
using Shaunebu.MAUI.Navigation.Debugger.Diagnostics;
#endif

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

/// <summary>
/// ViewModel for <see cref="Pages.Main.HomePage"/>.
///
/// Demonstrates:
/// - Typed push navigation to Settings, Profile, ProductDetails.
/// - Typed navigation to shared PrivacyPage (resolves to main/privacy route).
/// - Modal navigation to TermsPage.
/// - Loading and NoInternet overlays.
/// - Navigation stack inspection.
/// - Maintenance guard toggle.
/// - Flow switching (logout → AuthFlow).
/// - Duplicate navigation prevention (fast double-tap handled by library).
/// </summary>
public sealed class HomeViewModel : BaseViewModel
{
    private readonly INavigationHandler _navigation;
    private readonly INavigationFlowManager _flowManager;
    private readonly IOverlayNavigationService _overlay;
    private readonly INavigationStackInspector _stackInspector;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IAuthService _authService;
    private readonly ILogger<HomeViewModel> _logger;

    private string _stackInfo = "Tap 'Refresh Stack Info'";
    private bool _isMaintenanceModeActive;
#if DEBUG
    private string _perfReport = "Tap 'Perf Report' to sample counters.";
#endif

    public HomeViewModel(
        INavigationHandler navigation,
        INavigationFlowManager flowManager,
        IOverlayNavigationService overlay,
        INavigationStackInspector stackInspector,
        IMaintenanceService maintenanceService,
        IAuthService authService)
    {
        _navigation = navigation;
        _flowManager = flowManager;
        _overlay = overlay;
        _stackInspector = stackInspector;
        _maintenanceService = maintenanceService;
        _authService = authService;

        GoToSettingsCommand = new AsyncRelayCommand(GoToSettingsAsync);
        GoToProfileCommand = new AsyncRelayCommand(GoToProfileAsync);
        GoToProductDetailsCommand = new AsyncRelayCommand(GoToProductDetailsAsync);
        OpenPrivacyCommand = new AsyncRelayCommand(OpenPrivacyAsync);
        OpenTermsModalCommand = new AsyncRelayCommand(OpenTermsModalAsync);
        ShowLoadingCommand = new AsyncRelayCommand(ShowLoadingDemoAsync);
        ShowNoInternetCommand = new AsyncRelayCommand(ShowNoInternetDemoAsync);
        RefreshStackInfoCommand = new RelayCommand(RefreshStackInfo);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        RunNavigationScenarioCommand = new AsyncRelayCommand(RunNavigationScenarioAsync);
#if DEBUG
        ShowPerfReportCommand  = new RelayCommand(SamplePerfCounters);
        ResetPerfCountersCommand = new RelayCommand(ResetPerfCounters);
#endif
    }

    public string StackInfo { get => _stackInfo; private set => SetProperty(ref _stackInfo, value); }

#if DEBUG
    /// <summary>Most-recently sampled debugger performance counter report.</summary>
    public string PerfReport { get => _perfReport; private set => SetProperty(ref _perfReport, value); }

    /// <summary>Samples the current <see cref="DebuggerPerformanceCounters"/> and updates <see cref="PerfReport"/>.</summary>
    public RelayCommand ShowPerfReportCommand { get; }

    /// <summary>Resets all performance counters to zero.</summary>
    public RelayCommand ResetPerfCountersCommand { get; }

    private void SamplePerfCounters()
        => PerfReport = DebuggerPerformanceCounters.Report();

    private void ResetPerfCounters()
    {
        DebuggerPerformanceCounters.Reset();
        PerfReport = "Counters reset.";
    }
#endif

    /// <summary>
    /// Demonstrates: maintenance guard toggle.
    /// When set to true, the maintenance guard will redirect any navigation to MaintenancePage.
    /// </summary>
    public bool IsMaintenanceModeActive
    {
        get => _isMaintenanceModeActive;
        set
        {
            SetProperty(ref _isMaintenanceModeActive, value);
            _maintenanceService.IsMaintenanceModeActive = value;
        }
    }

    public AsyncRelayCommand GoToSettingsCommand { get; }
    public AsyncRelayCommand GoToProfileCommand { get; }
    public AsyncRelayCommand GoToProductDetailsCommand { get; }
    public AsyncRelayCommand OpenPrivacyCommand { get; }
    public AsyncRelayCommand OpenTermsModalCommand { get; }
    public AsyncRelayCommand ShowLoadingCommand { get; }
    public AsyncRelayCommand ShowNoInternetCommand { get; }
    public RelayCommand RefreshStackInfoCommand { get; }
    public AsyncRelayCommand LogoutCommand { get; }
    public AsyncRelayCommand RunNavigationScenarioCommand { get; }

    /// <summary>Demonstrates: typed push navigation guarded by SampleMaintenanceGuard.</summary>
    private Task GoToSettingsAsync(CancellationToken cancellationToken)
        => _navigation.GoToAsync<SettingsPage>(options => options.Animated = true, cancellationToken);

    /// <summary>Demonstrates: typed push navigation.</summary>
    private Task GoToProfileAsync(CancellationToken cancellationToken)
        => _navigation.GoToAsync<ProfilePage>(options => options.Animated = true, cancellationToken);

    /// <summary>Demonstrates: typed navigation with strongly typed parameters via generated overload.</summary>
    private Task GoToProductDetailsAsync(CancellationToken cancellationToken)
        => _navigation.GoToProductDetailsPageAsync(
            new ProductDetailsParameters(Guid.NewGuid(), "Enterprise Widget Pro"),
            options => options.Animated = true,
            cancellationToken);

    /// <summary>
    /// Demonstrates: global shared route resolution.
    /// PrivacyPage is registered as shared/privacy (global) so it resolves
    /// unambiguously regardless of the active flow.
    /// </summary>
    private Task OpenPrivacyAsync(CancellationToken cancellationToken)
        => _navigation.GoToAsync<Pages.Shared.PrivacyPage>(
            options =>
            {
                options.Animated = true;
                options.Reason = "Privacy opened from Home (Main flow route)";
            },
            cancellationToken);

    /// <summary>Demonstrates: modal navigation.</summary>
    private Task OpenTermsModalAsync(CancellationToken cancellationToken)
        => _navigation.ShowModalAsync<Pages.Shared.TermsPage>(
            options =>
            {
                options.Animated = true;
                options.Reason = "Terms opened modally from Home";
            },
            cancellationToken);

    /// <summary>
    /// Demonstrates: loading overlay state tracking.
    /// Shows a real visual loading overlay for 2 seconds via the overlay host.
    /// </summary>
    private async Task ShowLoadingDemoAsync(CancellationToken cancellationToken)
    {
        await _overlay.ShowLoadingAsync(
            new Shaunebu.MAUI.Navigation.Overlays.LoadingOverlayOptions { Message = "Simulating network call…" },
            cancellationToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _overlay.HideLoadingAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs a scripted navigation scenario to validate flow switching, shared pages,
    /// modal behavior, and back/stack correctness. This is intentionally a sample
    /// helper to exercise realistic runtime navigation paths.
    /// </summary>
    private async Task RunNavigationScenarioAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogInformation("[Scenario] Starting navigation scenario.");

            var res1 = await _navigation.GoToProductDetailsPageAsync(
                new ProductDetailsParameters(Guid.NewGuid(), "Scenario Widget"),
                options => options.Animated = true,
                cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation("[Scenario] ProductDetails: Succeeded={Succeeded} Message={Message}", res1.Succeeded, res1.Message);

            await Task.Delay(300, cancellationToken).ConfigureAwait(false);

            var res2 = await _navigation.GoToAsync<SettingsPage>(options => options.Animated = true, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("[Scenario] Settings: Succeeded={Succeeded} Message={Message}", res2.Succeeded, res2.Message);

            // Show modal and then close it via navigation handler.
            var res3 = await _navigation.ShowModalAsync<Pages.Shared.TermsPage>(options => options.Animated = true, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("[Scenario] Terms modal shown: Succeeded={Succeeded}", res3.Succeeded);

            await Task.Delay(200, cancellationToken).ConfigureAwait(false);

            await _navigation.CloseModalAsync(options => options.Animated = true, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("[Scenario] Terms modal closed.");

            // Reset to Auth flow then return to Main to validate root transitions.
            var res4 = await _flowManager.ResetToFlowAsync<AuthFlow>(options => { options.ClearBackStack = true; options.Animated = false; }, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("[Scenario] Reset to AuthFlow: Succeeded={Succeeded} Message={Message}", res4.Succeeded, res4.Message);

            var res5 = await _flowManager.ResetToFlowAsync<MainFlow>(options => { options.ClearBackStack = true; options.Animated = false; }, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("[Scenario] Reset back to MainFlow: Succeeded={Succeeded} Message={Message}", res5.Succeeded, res5.Message);

            _logger?.LogInformation("[Scenario] Navigation scenario complete.");
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("[Scenario] Canceled.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Scenario] Unexpected error while running navigation scenario.");
        }
    }

    /// <summary>
    /// Demonstrates: no-internet overlay state tracking.
    /// Shows a real visual no-internet overlay for 2 seconds via the overlay host.
    /// </summary>
    private async Task ShowNoInternetDemoAsync(CancellationToken cancellationToken)
    {
        await _overlay.ShowNoInternetAsync(
            new Shaunebu.MAUI.Navigation.Overlays.NoInternetOverlayOptions { Message = "No internet connection." },
            cancellationToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _overlay.HideNoInternetAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Demonstrates: stack inspection — useful for debugging and diagnostics.
    /// Reads the current navigation and modal stacks without causing side effects.
    /// </summary>
    private async void RefreshStackInfo()
    {
        var snapshot = await _stackInspector.GetSnapshotAsync().ConfigureAwait(false);
        var navStack = snapshot.NavigationStack is not null && snapshot.NavigationStack.Count > 0
            ? string.Join(" → ", snapshot.NavigationStack)
            : "(empty)";

        var modalStack = snapshot.ModalStack is not null && snapshot.ModalStack.Count > 0
            ? string.Join(", ", snapshot.ModalStack)
            : "(empty)";

        StackInfo = $"Flow: {snapshot.CurrentFlow ?? "none"}\n"
                  + $"Nav: {navStack}\n"
                  + $"Modal: {modalStack}";
    }

    /// <summary>
    /// Demonstrates: flow switching with back stack reset.
    /// Logs out the user and returns to the Auth flow.
    /// </summary>
    private async Task LogoutAsync(CancellationToken cancellationToken)
    {
        _authService.Logout();

        await _flowManager.ResetToFlowAsync<AuthFlow>(
            options =>
            {
                options.ClearBackStack = true;
                options.Animated = true;
                options.Reason = "User logged out";
            },
            cancellationToken).ConfigureAwait(false);
    }
}
