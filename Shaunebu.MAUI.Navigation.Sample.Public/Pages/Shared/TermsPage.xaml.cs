using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Shared;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Shared;

[NavigationRoute("shared/terms", IsShared = true)]
public sealed partial class TermsPage : ContentPage
{
    public TermsPage(TermsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
