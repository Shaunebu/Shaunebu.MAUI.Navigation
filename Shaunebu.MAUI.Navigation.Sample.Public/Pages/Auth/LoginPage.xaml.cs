using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Auth;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Auth;

[NavigationRoute("auth/login", Flow = "Auth")]
public sealed partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
