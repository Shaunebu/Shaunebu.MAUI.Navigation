using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Main;

[NavigationRoute("main/home", Flow = "Main")]
public sealed partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
