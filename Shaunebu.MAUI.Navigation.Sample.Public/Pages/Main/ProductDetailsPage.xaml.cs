using Shaunebu.MAUI.Navigation.Sample.Public.Parameters;
using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages.Main;

[NavigationRoute("main/product-details", Flow = "Main",
    ParametersType = typeof(ProductDetailsParameters))]
public sealed partial class ProductDetailsPage : ContentPage
{
    public ProductDetailsPage(ProductDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
