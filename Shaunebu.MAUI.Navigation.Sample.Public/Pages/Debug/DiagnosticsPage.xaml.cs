using Shaunebu.MAUI.Navigation.Abstractions;
#if DEBUG
using Shaunebu.MAUI.Navigation.Debugger.UI.ViewModels;
#endif

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Debug;

public partial class DiagnosticsPage : ContentPage
{
    private readonly INavigationStackInspector _inspector;
    private readonly IOverlayNavigationService _overlay;
#if DEBUG
    private readonly DebuggerDashboardViewModel? _dashboard;
#endif

    public DiagnosticsPage(
        INavigationStackInspector inspector,
        IOverlayNavigationService overlay
#if DEBUG
        , DebuggerDashboardViewModel? dashboard = null
#endif
        )
    {
        InitializeComponent();
        _inspector = inspector;
        _overlay = overlay;
#if DEBUG
        _dashboard = dashboard;
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task RefreshAsync()
    {
        var snapshot = await _inspector.GetSnapshotAsync().ConfigureAwait(false);
        FlowLabel.Text = snapshot.CurrentFlow ?? "(none)";
        NavStackLabel.Text = snapshot.NavigationStack is not null && snapshot.NavigationStack.Count > 0
            ? string.Join(" → ", snapshot.NavigationStack)
            : "(empty)";

        ModalStackLabel.Text = snapshot.ModalStack is not null && snapshot.ModalStack.Count > 0
            ? string.Join(", ", snapshot.ModalStack)
            : "(empty)";

        var overlays = await _overlay.GetSnapshotAsync().ConfigureAwait(false);
        OverlaysLabel.Text = overlays?.VisibleOverlays is not null && overlays.VisibleOverlays.Count > 0
            ? string.Join(", ", overlays.VisibleOverlays)
            : "(none)";

#if DEBUG
        // In DEBUG builds populate the operations timeline from the typed dashboard ViewModel.
        if (_dashboard is not null)
        {
            OpsCollection.ItemsSource = _dashboard.Events.Events.Count > 0
                ? (System.Collections.IEnumerable)_dashboard.Events.Events
                : null;
            return;
        }
#endif
        OpsCollection.ItemsSource = null;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await RefreshAsync().ConfigureAwait(false);
    }
}

