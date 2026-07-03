using Microsoft.Extensions.Logging;
using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Auth;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Flows;

/// <summary>
/// Auth flow — unauthenticated root.
/// Entry point: <see cref="LoginPage"/>.
/// Demonstrates: flow entry/exit lifecycle, unauthenticated root switching.
/// </summary>
[NavigationFlow("Auth")]
public sealed partial class AuthFlow : INavigationFlow
{
    private readonly ILogger<AuthFlow> _logger;

    public AuthFlow(ILogger<AuthFlow> logger)
    {
        System.Diagnostics.Debug.WriteLine("AuthFlow.ctor: enter");
        _logger = logger;
        System.Diagnostics.Debug.WriteLine("AuthFlow.ctor: exit");
    }

    public string Name => "Auth";
    public Type RootPageType => typeof(LoginPage);
    public bool RequiresAuthentication => false;

    public async Task OnEnterAsync(NavigationFlowContext context)
    {
        System.Diagnostics.Debug.WriteLine($"AuthFlow.OnEnterAsync: enter Flow={Name} Previous={context.PreviousFlowName ?? "none"} Operation={context.Operation?.PresentationMode}");
        _logger.LogInformation("[AuthFlow] Entered. Previous flow: {PreviousFlow}", context.PreviousFlowName ?? "none");
        await Task.CompletedTask.ConfigureAwait(false);
        System.Diagnostics.Debug.WriteLine($"AuthFlow.OnEnterAsync: exit Flow={Name}");
    }

    public async Task OnExitAsync(NavigationFlowContext context)
    {
        System.Diagnostics.Debug.WriteLine($"AuthFlow.OnExitAsync: enter Flow={Name} Previous={context.PreviousFlowName ?? "none"}");
        _logger.LogInformation("[AuthFlow] Exited.");
        await Task.CompletedTask.ConfigureAwait(false);
        System.Diagnostics.Debug.WriteLine($"AuthFlow.OnExitAsync: exit Flow={Name}");
    }
}
