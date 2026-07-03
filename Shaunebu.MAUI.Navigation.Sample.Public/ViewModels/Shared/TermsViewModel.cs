using Shaunebu.MAUI.Navigation.Abstractions;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Shared;

/// <summary>
/// ViewModel for <see cref="Pages.Shared.TermsPage"/> (modal).
///
/// Demonstrates: modal close via <see cref="INavigationHandler.CloseModalAsync"/>.
/// </summary>
public sealed class TermsViewModel : BaseViewModel
{
    private readonly INavigationHandler _navigation;

    public TermsViewModel(INavigationHandler navigation)
    {
        _navigation = navigation;
        CloseCommand = new AsyncRelayCommand(CloseAsync);
    }

    public AsyncRelayCommand CloseCommand { get; }

    private Task CloseAsync(CancellationToken cancellationToken)
        => _navigation.CloseModalAsync(options => options.Animated = true, cancellationToken);
}
