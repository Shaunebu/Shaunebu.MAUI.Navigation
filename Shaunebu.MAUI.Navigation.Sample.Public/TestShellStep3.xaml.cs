using Microsoft.Extensions.Logging;

namespace Shaunebu.MAUI.Navigation.Sample.Public;

public sealed partial class TestShellStep3 : Shell
{
    private readonly ILogger<TestShellStep3> _logger;

    public TestShellStep3(ILogger<TestShellStep3> logger)
    {
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep3.ctor: ENTER");
        System.Diagnostics.Debug.WriteLine("[Startup] FRAMEWORK TEST: STEP 3 - Shell with raw ContentPage");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");

        try
        {
            System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep3: Calling InitializeComponent...");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep3: ✓ InitializeComponent completed");

            System.Diagnostics.Debug.WriteLine($"[Startup] TestShellStep3: FlyoutBehavior = {FlyoutBehavior}");
            System.Diagnostics.Debug.WriteLine($"[Startup] TestShellStep3: Shell.Items.Count = {Items?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep3: ✗✗✗ EXCEPTION ✗✗✗");
            System.Diagnostics.Debug.WriteLine($"[Startup] Exception Type: {ex.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"[Startup] Exception Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Startup] Stack Trace:");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");

            logger.LogError(ex, "TestShellStep3 InitializeComponent failed");
            throw;
        }

        _logger = logger;

        System.Diagnostics.Debug.WriteLine("[Startup] TestShellStep3.ctor: EXIT");
        System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
    }
}
