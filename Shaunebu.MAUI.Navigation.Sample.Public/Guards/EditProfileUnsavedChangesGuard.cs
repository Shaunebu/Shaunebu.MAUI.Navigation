using Shaunebu.MAUI.Navigation.Guards;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Guards;

/// <summary>
/// A navigation guard that prevents leaving a page whose ViewModel implements
/// <see cref="IUnsavedChangesSource"/> while it has unsaved changes.
///
/// Demonstrates: guard targeting a specific ViewModel contract via the current page's
/// BindingContext, combined with a dialog-based discard confirmation.
///
/// Note: The guard inspects Application.Current here because the sample app is intentionally
/// demonstrating a page-level unsaved-changes pattern. In production, prefer injecting a
/// dedicated scoped state service rather than reading the current page directly.
/// </summary>
public sealed class EditProfileUnsavedChangesGuard : UnsavedChangesGuard
{
    protected override Task<bool> HasUnsavedChangesAsync(CancellationToken cancellationToken)
    {
        var source = GetCurrentUnsavedChangesSource();
        return Task.FromResult(source?.HasUnsavedChanges ?? false);
    }

    protected override async Task<bool> ConfirmDiscardAsync(
        NavigationGuardContext context,
        CancellationToken cancellationToken)
    {
        bool confirmed = false;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is null) { confirmed = true; return; }

            confirmed = await page
                .DisplayAlert(
                    "Unsaved Changes",
                    "You have unsaved changes. Discard and leave?",
                    "Discard",
                    "Stay")
                .ConfigureAwait(false);
        }).ConfigureAwait(false);
        return confirmed;
    }

    /// <summary>
    /// Walks the current navigation stack to find a page whose BindingContext
    /// implements <see cref="IUnsavedChangesSource"/>.
    /// </summary>
    private static IUnsavedChangesSource? GetCurrentUnsavedChangesSource()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is null) return null;

        // Check the top of the navigation stack.
        if (window.Page is NavigationPage navPage)
        {
            var top = navPage.CurrentPage;
            if (top?.BindingContext is IUnsavedChangesSource s) return s;
        }

        if (window.Page is Shell shell)
        {
            var current = shell.CurrentPage;
            if (current?.BindingContext is IUnsavedChangesSource s) return s;
        }

        return window.Page.BindingContext as IUnsavedChangesSource;
    }

    public EditProfileUnsavedChangesGuard()
    {
        System.Diagnostics.Debug.WriteLine("EditProfileUnsavedChangesGuard.ctor: enter");
        System.Diagnostics.Debug.WriteLine("EditProfileUnsavedChangesGuard.ctor: exit");
    }
}

/// <summary>Marker interface for ViewModels that expose an unsaved-changes flag.</summary>
public interface IUnsavedChangesSource
{
    bool HasUnsavedChanges { get; }
}
