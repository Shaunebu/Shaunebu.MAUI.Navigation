namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Overlays;

public sealed partial class NoInternetOverlayPage : ContentPage
{
    public NoInternetOverlayPage()
    {
        InitializeComponent();
    }

    /// <summary>Updates the displayed message before the page is shown.</summary>
    public void SetMessage(string? message)
    {
        MessageLabel.Text = string.IsNullOrWhiteSpace(message) ? "No internet connection." : message;
    }
}
