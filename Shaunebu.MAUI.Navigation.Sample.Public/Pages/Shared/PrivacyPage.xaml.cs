using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Shared;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Shared;

[NavigationRoute("shared/privacy", IsShared = true)]
public sealed partial class PrivacyPage : ContentPage
{
    public PrivacyPage(PrivacyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
