using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Flows;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Auth;
using Shaunebu.MAUI.Navigation.Sample.Public.Services;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Auth;

/// <summary>
/// ViewModel for <see cref="Pages.Auth.LoginPage"/>.
///
/// Demonstrates:
/// - Loading overlay wrapping an async operation.
/// - Flow switching (Auth → Main) after successful login.
/// - Typed navigation to RegisterPage.
/// - Modal navigation to PrivacyPage.
/// - Structured NavigationResult handling.
/// </summary>
public sealed class LoginViewModel : BaseViewModel
{
    private readonly INavigationHandler _navigation;
    private readonly INavigationFlowManager _flowManager;
    private readonly IOverlayNavigationService _overlay;
    private readonly IAuthService _authService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string? _statusMessage;

    public LoginViewModel(
        INavigationHandler navigation,
        INavigationFlowManager flowManager,
        IOverlayNavigationService overlay,
        IAuthService authService)
    {
        _navigation = navigation;
        _flowManager = flowManager;
        _overlay = overlay;
        _authService = authService;

        LoginCommand = new AsyncRelayCommand(LoginAsync);
        GoToRegisterCommand = new AsyncRelayCommand(GoToRegisterAsync);
        OpenPrivacyModalCommand = new AsyncRelayCommand(OpenPrivacyModalAsync);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set
        {
            SetProperty(ref _statusMessage, value);
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);

    public AsyncRelayCommand LoginCommand { get; }
    public AsyncRelayCommand GoToRegisterCommand { get; }
    public AsyncRelayCommand OpenPrivacyModalCommand { get; }

    /// <summary>
    /// Demonstrates: loading overlay + async auth + flow switching.
    /// Uses cancellation-aware overlay hide in the finally block.
    /// </summary>
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        StatusMessage = null;
        IsBusy = true;

        // Overlay — NOT a navigation page push.
        await _overlay.ShowLoadingAsync(
            new Shaunebu.MAUI.Navigation.Overlays.LoadingOverlayOptions { Message = "Signing in…" },
            cancellationToken).ConfigureAwait(false);

        try
        {
            var success = await _authService.LoginAsync(Username, Password, cancellationToken)
                .ConfigureAwait(false);

            if (!success)
            {
                StatusMessage = "Invalid credentials. Try any non-empty username/password.";
                return;
            }

            // Switch to the authenticated root — clears the auth back stack.
            var result = await _flowManager.ResetToFlowAsync<MainFlow>(
                options =>
                {
                    options.ClearBackStack = true;
                    options.Animated = true;
                    options.Reason = "User authenticated";
                },
                cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded)
                StatusMessage = $"Navigation failed: {result.Message}";
        }
        finally
        {
            await _overlay.HideLoadingAsync(CancellationToken.None).ConfigureAwait(false);
            IsBusy = false;
        }
    }

    /// <summary>Demonstrates: typed push navigation.</summary>
    private Task GoToRegisterAsync(CancellationToken cancellationToken)
        => _navigation.GoToAsync<RegisterPage>(
            options => options.Animated = true,
            cancellationToken);

    /// <summary>Demonstrates: navigate to the shared privacy page using Shell routing.
    ///
    /// In Shell-based MAUI apps a NavigationPage may not be present. Use Shell navigation
    /// for route-based navigation (including pages that were previously presented modally
    /// via a NavigationPage) so the Shell adapter handles presentation.</summary>
    private Task OpenPrivacyModalAsync(CancellationToken cancellationToken)
        => _navigation.GoToAsync<Pages.Shared.PrivacyPage>(
            options =>
            {
                // Prefer Shell-based routing in MAUI sample so the Shell adapter is used.
                options.PresentationMode = NavigationPresentationMode.Shell;
                options.Animated = true;
                options.Reason = "Privacy opened from Login (shell)";
            },
            cancellationToken);
}
