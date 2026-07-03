using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Main;

[NavigationRoute("main/edit-profile", Flow = "Main")]
public sealed partial class EditProfilePage : ContentPage
{
    public EditProfilePage(EditProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
