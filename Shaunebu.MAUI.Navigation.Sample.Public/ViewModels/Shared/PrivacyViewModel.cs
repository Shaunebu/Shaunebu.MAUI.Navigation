using Shaunebu.MAUI.Navigation.Abstractions;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Shared;

/// <summary>
/// ViewModel for <see cref="Pages.Shared.PrivacyPage"/>.
///
/// Demonstrates:
/// - A page reusable across multiple flows (Auth + Main).
/// - The library resolves the correct flow-scoped route automatically.
/// - Modal close vs back navigation.
/// </summary>
public sealed class PrivacyViewModel : BaseViewModel
{
    private readonly INavigationHandler _navigation;

    public PrivacyViewModel(INavigationHandler navigation)
    {
        _navigation = navigation;
        CloseCommand = new AsyncRelayCommand(CloseAsync);
    }

    public AsyncRelayCommand CloseCommand { get; }

    /// <summary>
    /// Handles both modal dismiss and back navigation transparently.
    /// The library resolves the correct dismiss strategy.
    /// </summary>
    private Task CloseAsync(CancellationToken cancellationToken)
        => _navigation.GoBackAsync(options => options.Animated = true, cancellationToken);
}
