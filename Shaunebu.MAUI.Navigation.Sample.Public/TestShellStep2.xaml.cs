using Microsoft.Extensions.Logging;

namespace Shaunebu.MAUI.Navigation.Sample.Public;

public sealed partial class TestShellStep2 : Shell
{
    private readonly ILogger<TestShellStep2> _logger;

    public TestShellStep2(ILogger<TestShellStep2> logger)
    {
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep2.ctor: ENTER");
        System.Diagnostics.Debug.WriteLine("[Startup] FRAMEWORK TEST: STEP 2 - Shell with FlyoutBehavior=Disabled");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");

        try
        {
            System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep2: Calling InitializeComponent...");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep2: ✓ InitializeComponent completed");

            System.Diagnostics.Debug.WriteLine($"[Startup] TestShellStep2: FlyoutBehavior = {FlyoutBehavior}");
            System.Diagnostics.Debug.WriteLine($"[Startup] TestShellStep2: Shell.Items.Count = {Items?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep2: ✗✗✗ EXCEPTION ✗✗✗");
            System.Diagnostics.Debug.WriteLine($"[Startup] Exception Type: {ex.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"[Startup] Exception Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Startup] Stack Trace:");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");

            logger.LogError(ex, "TestShellStep2 InitializeComponent failed");
            throw;
        }

        _logger = logger;

        System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep2.ctor: EXIT");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
    }
}
