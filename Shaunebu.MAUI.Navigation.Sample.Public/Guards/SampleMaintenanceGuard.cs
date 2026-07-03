using Shaunebu.MAUI.Navigation.Guards;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages;
using Shaunebu.MAUI.Navigation.Sample.Public.Services;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Guards;

/// <summary>
/// Sample maintenance guard.
/// Redirects all navigation to <see cref="MaintenancePage"/> when
/// <see cref="IMaintenanceService.IsMaintenanceModeActive"/> is <see langword="true"/>.
/// </summary>
public sealed class SampleMaintenanceGuard : MaintenanceGuard<MaintenancePage>
{
    private readonly IMaintenanceService _maintenanceService;

    public SampleMaintenanceGuard(IMaintenanceService maintenanceService)
        {
            System.Diagnostics.Debug.WriteLine("SampleMaintenanceGuard.ctor: enter");
            _maintenanceService = maintenanceService;
            System.Diagnostics.Debug.WriteLine("SampleMaintenanceGuard.ctor: exit");
        }

    protected override Task<bool> IsMaintenanceModeActiveAsync(CancellationToken cancellationToken)
        => Task.FromResult(_maintenanceService.IsMaintenanceModeActive);
}
