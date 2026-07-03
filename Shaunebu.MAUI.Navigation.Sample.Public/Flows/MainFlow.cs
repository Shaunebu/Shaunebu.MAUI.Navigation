using Microsoft.Extensions.Logging;
using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Sample.Public.Pages.Main;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Flows;

/// <summary>
/// Main/Home flow — authenticated root.
/// Entry point: <see cref="HomePage"/>.
/// Demonstrates: authenticated flow, flow entry lifecycle.
/// </summary>
[NavigationFlow("Main")]
public sealed partial class MainFlow : INavigationFlow
{
    private readonly ILogger<MainFlow> _logger;

    public MainFlow(ILogger<MainFlow> logger)
    {
        System.Diagnostics.Debug.WriteLine("MainFlow.ctor: enter");
        _logger = logger;
        System.Diagnostics.Debug.WriteLine("MainFlow.ctor: exit");
    }

    public string Name => "Main";
    public Type RootPageType => typeof(HomePage);
    public bool RequiresAuthentication => true;

    public Task OnEnterAsync(NavigationFlowContext context)
    {
        _logger.LogInformation("[MainFlow] Entered. Previous flow: {PreviousFlow}", context.PreviousFlowName ?? "none");
        return Task.CompletedTask;
    }

    public Task OnExitAsync(NavigationFlowContext context)
    {
        _logger.LogInformation("[MainFlow] Exited.");
        return Task.CompletedTask;
    }
}
