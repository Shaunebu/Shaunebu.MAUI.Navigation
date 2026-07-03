using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Flows;
using Shaunebu.MAUI.Navigation.Sample.Public.Services;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Auth;

/// <summary>
/// ViewModel for <see cref="Pages.Auth.RegisterPage"/>.
///
/// Demonstrates:
/// - Typed back navigation (<see cref="INavigationHandler.GoBackAsync"/>).
/// - Shared PrivacyPage resolved through the Auth flow route.
/// - Loading overlay for async registration.
/// - Structured NavigationResult handling.
/// </summary>
public sealed class RegisterViewModel : BaseViewModel
{
    private readonly INavigationHandler _navigation;
    private readonly INavigationFlowManager _flowManager;
    private readonly IOverlayNavigationService _overlay;
    private readonly IAuthService _authService;

    private string _fullName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;

    public RegisterViewModel(
        INavigationHandler navigation,
        INavigationFlowManager flowManager,
        IOverlayNavigationService overlay,
        IAuthService authService)
    {
        _navigation = navigation;
        _flowManager = flowManager;
        _overlay = overlay;
        _authService = authService;

        RegisterCommand = new AsyncRelayCommand(RegisterAsync);
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
        OpenPrivacyCommand = new AsyncRelayCommand(OpenPrivacyAsync);
    }

    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    public AsyncRelayCommand RegisterCommand { get; }
    public AsyncRelayCommand GoBackCommand { get; }
    public AsyncRelayCommand OpenPrivacyCommand { get; }

    /// <summary>Demonstrates: loading overlay + flow switching after registration.</summary>
    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        await _overlay.ShowLoadingAsync(
            new Shaunebu.MAUI.Navigation.Overlays.LoadingOverlayOptions { Message = "Creating account…" },
            cancellationToken).ConfigureAwait(false);

        try
        {
            // Simulate registration — any non-empty credentials succeed.
            await _authService.LoginAsync(Email, Password, cancellationToken).ConfigureAwait(false);

            await _flowManager.ResetToFlowAsync<MainFlow>(
                options =>
                {
                    options.ClearBackStack = true;
                    options.Animated = true;
                    options.Reason = "Account registered";
                },
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _overlay.HideLoadingAsync(CancellationToken.None).ConfigureAwait(false);
            IsBusy = false;
        }
    }

    /// <summary>Demonstrates: GoBackAsync — deterministic back navigation.</summary>
    private Task GoBackAsync(CancellationToken cancellationToken)
        => _navigation.GoBackAsync(options => options.Animated = true, cancellationToken);

    /// <summary>
    /// Demonstrates: flow-aware shared route resolution.
    /// When the current flow is Auth, this resolves to auth/privacy.
    /// </summary>
    private Task OpenPrivacyAsync(CancellationToken cancellationToken)
        => _navigation.GoToAsync<Pages.Shared.PrivacyPage>(
            options =>
            {
                options.Animated = true;
                options.Reason = "Privacy opened from Register";
            },
            cancellationToken);
}
