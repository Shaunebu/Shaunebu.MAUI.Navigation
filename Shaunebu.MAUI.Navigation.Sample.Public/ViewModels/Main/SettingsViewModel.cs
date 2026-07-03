using Shaunebu.MAUI.Navigation.Abstractions;
#if DEBUG
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Debug;
#endif

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

/// <summary>
/// ViewModel for <see cref="Pages.Main.SettingsPage"/>.
///
/// Demonstrates:
/// - INavigationAware lifecycle callbacks.
/// - Typed back navigation.
/// - Maintenance guard in action (this page is the guard's test target).
/// - DEBUG-only: opens the Navigation Debugger Shell.
/// </summary>
public sealed class SettingsViewModel : BaseViewModel, INavigationAware
{
    private readonly INavigationHandler _navigation;
    private string _info = string.Empty;

    public SettingsViewModel(INavigationHandler navigation)
    {
        _navigation = navigation;
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
#if DEBUG
        OpenDebuggerShellCommand = new AsyncRelayCommand(OpenDebuggerShellAsync);
#else
        OpenDebuggerShellCommand = new AsyncRelayCommand(_ => Task.CompletedTask);
#endif
    }

    public string Info { get => _info; private set => SetProperty(ref _info, value); }

    /// <summary>Returns <see langword="true"/> only in DEBUG builds.</summary>
    public bool IsDebuggerAvailable =>
#if DEBUG
        true;
#else
        false;
#endif

    public AsyncRelayCommand GoBackCommand { get; }

    /// <summary>Opens the <c>debug/shell</c> route. No-op in Release builds.</summary>
    public AsyncRelayCommand OpenDebuggerShellCommand { get; }

    /// <summary>Demonstrates: INavigationAware.OnNavigatedToAsync for loading data.</summary>
    public Task OnNavigatedToAsync(NavigationContext context)
    {
        Info = $"Arrived at Settings. Operation: {context.Operation?.Id}";
        return Task.CompletedTask;
    }

    public Task OnNavigatingFromAsync(NavigationContext context) => Task.CompletedTask;
    public Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;

    private Task GoBackAsync(CancellationToken cancellationToken)
        => _navigation.GoBackAsync(options => options.Animated = true, cancellationToken);

#if DEBUG
    private Task OpenDebuggerShellAsync(CancellationToken cancellationToken)
        => _navigation.GoToAsync<DebuggerShellPage>(cancellationToken: cancellationToken);
#endif
}
