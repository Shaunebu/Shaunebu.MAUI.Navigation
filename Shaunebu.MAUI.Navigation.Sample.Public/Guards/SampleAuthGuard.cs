using Shaunebu.MAUI.Navigation.Guards;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Auth;
using Shaunebu.MAUI.Navigation.Sample.Public.Services;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Guards;

/// <summary>
/// Sample authentication guard.
/// Redirects unauthenticated users to <see cref="LoginPage"/> when they attempt to
/// navigate to a page that requires authentication.
/// </summary>
public sealed class SampleAuthGuard : AuthenticationGuard<LoginPage>
{
    private readonly IAuthService _authService;

    public SampleAuthGuard(IAuthService authService)
    {
        System.Diagnostics.Debug.WriteLine("SampleAuthGuard.ctor: enter");
        _authService = authService;
        System.Diagnostics.Debug.WriteLine("SampleAuthGuard.ctor: exit");
    }

    protected override Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken)
        => Task.FromResult(_authService.IsAuthenticated);

    protected override Task<bool> RequiresAuthenticationAsync(
        NavigationGuardContext context,
        CancellationToken cancellationToken)
    {
        // In the sample, any page inside the Main flow requires authentication.
        // You can extend this to check route descriptors or custom attributes.
        var requiresAuth = context.TargetRoute?.StartsWith("main/", StringComparison.OrdinalIgnoreCase) ?? false;
        return Task.FromResult(requiresAuth);
    }
}
