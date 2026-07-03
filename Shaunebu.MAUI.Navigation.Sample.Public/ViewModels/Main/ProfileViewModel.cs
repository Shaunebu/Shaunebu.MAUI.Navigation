using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Main;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

/// <summary>
/// ViewModel for <see cref="Pages.Main.ProfilePage"/>.
///
/// Demonstrates:
/// - Typed navigation to EditProfilePage.
/// - Typed back navigation.
/// </summary>
public sealed class ProfileViewModel : BaseViewModel
{
    private readonly INavigationHandler _navigation;

    public ProfileViewModel(INavigationHandler navigation)
    {
        _navigation = navigation;
        GoToEditProfileCommand = new AsyncRelayCommand(GoToEditProfileAsync);
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
    }

    public AsyncRelayCommand GoToEditProfileCommand { get; }
    public AsyncRelayCommand GoBackCommand { get; }

    private Task GoToEditProfileAsync(CancellationToken cancellationToken)
        => _navigation.GoToAsync<EditProfilePage>(options => options.Animated = true, cancellationToken);

    private Task GoBackAsync(CancellationToken cancellationToken)
        => _navigation.GoBackAsync(options => options.Animated = true, cancellationToken);
}
