#if DEBUG
using Shaunebu.MAUI.Navigation.Debugger.Abstractions;
#endif

namespace Shaunebu.MAUI.Navigation.Sample.Public;

public partial class App : Application
{
	private readonly AppShell _shell;
#if DEBUG
	// The service provider is stored instead of IDebuggerOverlayHost so that the
	// debugger object graph (DebuggerOverlayHost, DebuggerDashboardViewModel,
	// NavigationTimelineReplayer, two System.Threading.Timers via DebuggerUiThrottle, …)
	// is NOT constructed on the main thread during App.ctor / CreateWindow.
	// Resolution is deferred to ScheduleStartupAttach which runs on a background thread
	// 350 ms after startup, well after the first frame has rendered.
	private readonly IServiceProvider _services;

	// Resolved lazily inside ScheduleStartupAttach (background thread, post-startup).
	private IDebuggerOverlayHost? _overlayHost;

	// Tracks whether the first deferred attach after startup has already been scheduled.
	// This prevents Window.Resumed — which MAUI fires during its own startup sequence on
	// Android — from triggering attach before Shell is initialized.
	private int _startupAttachScheduled;

	// Guards against concurrent lifecycle calls racing each other. Only one
	// attach/detach operation should be in-flight at a time.
	private int _lifecycleOpInFlight;
#endif

	public App(AppShell shell, IServiceProvider services)
	{
		System.Diagnostics.Debug.WriteLine("[Startup] App.ctor: enter");
		InitializeComponent();
		_shell = shell;
#if DEBUG
		// Store the provider only — do NOT resolve IDebuggerOverlayHost here.
		// Resolving it here would construct the entire debugger graph synchronously
		// on the main thread, causing the Android "skipped frames" splash freeze.
		_services = services;
#endif
		System.Diagnostics.Debug.WriteLine("[Startup] App.ctor: exit");
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		System.Diagnostics.Debug.WriteLine("[Startup] App.CreateWindow: enter");
		var w = new Window(_shell);

#if DEBUG
		// Wire lifecycle events unconditionally; all handlers are no-ops until
		// _startupAttachScheduled transitions from 0 → 1 inside ScheduleStartupAttach.
		w.Stopped    += OnWindowStopped;
		w.Resumed    += OnWindowResumed;
		w.Destroying += OnWindowDestroying;

		// Schedule the first debugger attach after the Shell startup defer completes.
		// AppShell.OnAppearing delays 250 ms before its first navigation push;
		// we wait slightly longer (350 ms) so Shell is fully initialized before
		// the stack inspector is ever called.
		//
		// Critically: IDebuggerOverlayHost is resolved INSIDE the background Task.Run
		// inside ScheduleStartupAttach, NOT here. This keeps the entire debugger object
		// graph off the main thread during startup.
		ScheduleStartupAttach(w);
#endif

		System.Diagnostics.Debug.WriteLine("[Startup] App.CreateWindow: exit");
		return w;
	}

#if DEBUG
	private void ScheduleStartupAttach(Window window)
	{
		// Use Interlocked to guarantee only one startup attach is ever scheduled.
		if (Interlocked.CompareExchange(ref _startupAttachScheduled, 1, 0) != 0)
			return;

		// Dispatch onto the main thread with a delay that clears the AppShell startup
		// navigation defer (250 ms) by a safe margin.
		window.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () =>
		{
			// Resolve and attach on a background thread so that:
			//   1. The heavy debugger object graph is constructed off the UI thread.
			//   2. System.Threading.Timer creation (inside DebuggerUiThrottle) does not
			//      block the main thread (JNI-heavy on Android).
			//   3. AttachAsync itself never blocks the UI thread.
			RunLifecycleOp(async () =>
			{
				// First resolution of IDebuggerOverlayHost — this constructs the entire
				// debugger graph but we are now on a thread-pool thread, not the UI thread.
				_overlayHost = _services.GetService<IDebuggerOverlayHost>();
				if (_overlayHost is null) return;

				// Signal startup complete so the stack inspector is safe to call.
				_overlayHost.SetStartupReady();
				System.Diagnostics.Debug.WriteLine("[Startup] Debugger attach: begin");
				await _overlayHost.AttachAsync().ConfigureAwait(false);
				System.Diagnostics.Debug.WriteLine("[Startup] Debugger attach: complete");
			});
		});
	}

	private void OnWindowStopped(object? sender, EventArgs e)
	{
		// Ignore events that arrive before the startup attach has been scheduled.
		if (_startupAttachScheduled == 0) return;
		var host = _overlayHost;
		if (host is null) return;
		RunLifecycleOp(async () => await host.DetachAsync().ConfigureAwait(false));
	}

	private void OnWindowResumed(object? sender, EventArgs e)
	{
		// Ignore Window.Resumed during MAUI startup (fires before ScheduleStartupAttach).
		if (_startupAttachScheduled == 0) return;
		var host = _overlayHost;
		if (host is null) return;
		RunLifecycleOp(async () => await host.AttachAsync().ConfigureAwait(false));
	}

	private void OnWindowDestroying(object? sender, EventArgs e)
	{
		var host = _overlayHost;
		if (host is null) return;
		RunLifecycleOp(async () => await host.DetachAsync().ConfigureAwait(false));
	}

	/// <summary>
	/// Runs an attach/detach lifecycle operation on a background thread.
	/// Uses an interlocked in-flight counter to prevent concurrent operations from
	/// racing each other — if an op is already running the new request is dropped
	/// (lifecycle state will self-correct on the next event).
	/// </summary>
	private void RunLifecycleOp(Func<Task> op)
	{
		// Only allow one lifecycle operation at a time.
		if (Interlocked.CompareExchange(ref _lifecycleOpInFlight, 1, 0) != 0)
			return;

		_ = Task.Run(async () =>
		{
			try
			{
				await op().ConfigureAwait(false);
			}
			catch
			{
				// Lifecycle operations must never crash the app.
			}
			finally
			{
				Interlocked.Exchange(ref _lifecycleOpInFlight, 0);
			}
		});
	}
#endif
}
