namespace Shaunebu.MAUI.Navigation.Sample.Public.Services;

/// <summary>
/// Simulates a remote maintenance-mode flag.
/// Toggle <see cref="IsMaintenanceModeActive"/> to demonstrate the maintenance guard.
/// </summary>
public interface IMaintenanceService
{
    bool IsMaintenanceModeActive { get; set; }
}

/// <inheritdoc/>
public sealed class InMemoryMaintenanceService : IMaintenanceService
{
    public InMemoryMaintenanceService()
    {
        System.Diagnostics.Debug.WriteLine("InMemoryMaintenanceService.ctor: enter");
        System.Diagnostics.Debug.WriteLine("InMemoryMaintenanceService.ctor: exit");
    }

    public bool IsMaintenanceModeActive { get; set; } = false;
}
