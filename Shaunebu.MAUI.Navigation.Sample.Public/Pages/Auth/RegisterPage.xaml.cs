using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Auth;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Auth;

[NavigationRoute("auth/register", Flow = "Auth")]
public sealed partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
