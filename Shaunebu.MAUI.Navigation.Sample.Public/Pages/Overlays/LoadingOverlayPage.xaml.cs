namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Overlays;

public sealed partial class LoadingOverlayPage : ContentPage
{
    public LoadingOverlayPage()
    {
        InitializeComponent();
    }

    /// <summary>Updates the displayed message before the page is shown.</summary>
    public void SetMessage(string? message)
    {
        MessageLabel.Text = string.IsNullOrWhiteSpace(message) ? "Loading…" : message;
    }
}
