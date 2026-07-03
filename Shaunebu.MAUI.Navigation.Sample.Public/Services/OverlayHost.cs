using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Overlays;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Overlays;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Services;

/// <summary>
/// Minimal sample-level overlay host.
///
/// Listens to <see cref="IOverlayNavigationService.StateChanged"/> and presents
/// or dismisses real modal <see cref="ContentPage"/> overlays so they visually
/// layer above all Shell content without touching the navigation stack.
///
/// This is deliberately sample-scope: it uses the library's state service as the
/// single source of truth and reacts only to state changes.
/// </summary>
public sealed class OverlayHost : IDisposable
{
    private readonly IOverlayNavigationService _overlayService;

    private LoadingOverlayPage? _loadingPage;
    private NoInternetOverlayPage? _noInternetPage;

    private bool _loadingVisible;
    private bool _noInternetVisible;

    public OverlayHost(IOverlayNavigationService overlayService)
    {
        _overlayService = overlayService;
        _overlayService.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Marshal to the UI thread — StateChanged is raised from async overlay operations
        // that may complete on a thread-pool thread.
        MainThread.BeginInvokeOnMainThread(SyncOverlays);
    }

    private void SyncOverlays()
    {
        var snapshot = _overlayService.GetSnapshot();
        SyncLoading(snapshot.IsLoadingVisible);
        SyncNoInternet(snapshot.IsNoInternetVisible);
    }

    private void SyncLoading(bool shouldBeVisible)
    {
        if (shouldBeVisible == _loadingVisible) return;
        _loadingVisible = shouldBeVisible;

        if (shouldBeVisible)
        {
            var message = (_overlayService.GetParameter("Loading") as LoadingOverlayOptions)?.Message;
            _loadingPage = new LoadingOverlayPage();
            _loadingPage.SetMessage(message);
            PushOverlayModal(_loadingPage);
        }
        else
        {
            PopOverlayModal(ref _loadingPage);
        }
    }

    private void SyncNoInternet(bool shouldBeVisible)
    {
        if (shouldBeVisible == _noInternetVisible) return;
        _noInternetVisible = shouldBeVisible;

        if (shouldBeVisible)
        {
            var message = (_overlayService.GetParameter("NoInternet") as NoInternetOverlayOptions)?.Message;
            _noInternetPage = new NoInternetOverlayPage();
            _noInternetPage.SetMessage(message);
            PushOverlayModal(_noInternetPage);
        }
        else
        {
            PopOverlayModal(ref _noInternetPage);
        }
    }

    private static void PushOverlayModal(ContentPage page)
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is null) return;

        // Present as a modal so it sits above the entire Shell visual tree.
        _ = window.Page.Navigation.PushModalAsync(page, animated: false);
    }

    private static void PopOverlayModal<T>(ref T? page) where T : ContentPage
    {
        if (page is null) return;
        var captured = page;
        page = null;

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is null) return;

        var modalStack = window.Page.Navigation.ModalStack;
        if (modalStack.Contains(captured))
            _ = window.Page.Navigation.PopModalAsync(animated: false);
    }

    public void Dispose()
    {
        _overlayService.StateChanged -= OnStateChanged;
    }
}
