using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Flows;
using Shaunebu.MAUI.Navigation.Sample.Public.Services;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Shared;

/// <summary>
/// ViewModel for <see cref="Pages.MaintenancePage"/>.
///
/// Demonstrates:
/// - A page shown by the SampleMaintenanceGuard redirect.
/// - The ability to retry / exit maintenance mode.
/// </summary>
public sealed class MaintenanceViewModel : BaseViewModel
{
    private readonly INavigationFlowManager _flowManager;
    private readonly IMaintenanceService _maintenanceService;

    public MaintenanceViewModel(
        INavigationFlowManager flowManager,
        IMaintenanceService maintenanceService)
    {
        _flowManager = flowManager;
        _maintenanceService = maintenanceService;
        RetryCommand = new AsyncRelayCommand(RetryAsync);
    }

    public AsyncRelayCommand RetryCommand { get; }

    /// <summary>
    /// Disables maintenance mode and resets to the Main flow.
    /// In a real app this would re-check the remote flag instead.
    /// </summary>
    private async Task RetryAsync(CancellationToken cancellationToken)
    {
        _maintenanceService.IsMaintenanceModeActive = false;

        await _flowManager.ResetToFlowAsync<MainFlow>(
            options =>
            {
                options.ClearBackStack = true;
                options.Animated = true;
                options.Reason = "Exiting maintenance mode";
            },
            cancellationToken).ConfigureAwait(false);
    }
}
