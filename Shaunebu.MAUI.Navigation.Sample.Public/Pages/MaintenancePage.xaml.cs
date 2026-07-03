using Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Shared;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Pages;

[NavigationRoute("shared/maintenance", IsShared = true)]
public sealed partial class MaintenancePage : ContentPage
{
    public MaintenancePage(MaintenanceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
