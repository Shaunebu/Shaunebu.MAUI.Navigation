using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Guards;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

/// <summary>
/// ViewModel for <see cref="Pages.Main.EditProfilePage"/>.
///
/// Demonstrates:
/// - UnsavedChangesGuard integration via <see cref="IUnsavedChangesSource"/>.
/// - INavigationAware lifecycle callbacks.
/// - Typed back navigation (guard fires automatically before popping).
/// </summary>
public sealed class EditProfileViewModel : BaseViewModel, IUnsavedChangesSource, INavigationAware
{
    private readonly INavigationHandler _navigation;
    private string _displayName = string.Empty;
    private bool _hasUnsavedChanges;

    public EditProfileViewModel(INavigationHandler navigation)
    {
        _navigation = navigation;
        SaveCommand = new RelayCommand(Save);
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
    }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (SetProperty(ref _displayName, value))
                HasUnsavedChanges = true;
        }
    }

    /// <summary>
    /// Exposed to <see cref="EditProfileUnsavedChangesGuard"/>.
    /// When true, the guard will prompt the user to confirm discarding changes.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

    public RelayCommand SaveCommand { get; }
    public AsyncRelayCommand GoBackCommand { get; }

    private void Save()
    {
        // Simulate save — in a real app persist to a service.
        HasUnsavedChanges = false;
    }

    private Task GoBackAsync(CancellationToken cancellationToken)
        => _navigation.GoBackAsync(
            options =>
            {
                options.Animated = true;
                options.RespectGuards = true; // Guard fires here.
            },
            cancellationToken);

    public Task OnNavigatedToAsync(NavigationContext context)
    {
        DisplayName = "Jane Doe"; // Simulate loaded profile.
        HasUnsavedChanges = false;
        return Task.CompletedTask;
    }

    public Task OnNavigatingFromAsync(NavigationContext context) => Task.CompletedTask;
    public Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;
}
