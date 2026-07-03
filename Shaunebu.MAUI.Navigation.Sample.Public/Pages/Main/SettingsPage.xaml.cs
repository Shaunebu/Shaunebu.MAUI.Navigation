using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Main;

[NavigationRoute("main/settings", Flow = "Main")]
public sealed partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
