using Shaunebu.MAUI.Navigation.Abstractions;
using Shaunebu.MAUI.Navigation.Parameters;
using Shaunebu.MAUI.Navigation.Sample.Public.Parameters;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels.Main;

/// <summary>
/// ViewModel for <see cref="Pages.Main.ProductDetailsPage"/>.
///
/// Demonstrates:
/// - Strongly typed parameter receiving via <see cref="INavigationParameterReceiver{TParameters}"/>.
/// - INavigationAware lifecycle.
/// - Cancellation-aware back navigation.
/// </summary>
public sealed class ProductDetailsViewModel : BaseViewModel,
    INavigationParameterReceiver<ProductDetailsParameters>,
    INavigationAware
{
    private readonly INavigationHandler _navigation;
    private Guid _productId;
    private string _productName = string.Empty;

    public ProductDetailsViewModel(INavigationHandler navigation)
    {
        _navigation = navigation;
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
    }

    public Guid ProductId { get => _productId; private set => SetProperty(ref _productId, value); }
    public string ProductName { get => _productName; private set => SetProperty(ref _productName, value); }

    public AsyncRelayCommand GoBackCommand { get; }

    /// <summary>
    /// Called by the navigation handler to deliver typed parameters.
    /// Invoked before <see cref="OnNavigatedToAsync"/>.
    /// </summary>
    public Task ReceiveParametersAsync(ProductDetailsParameters parameters)
    {
        ProductId = parameters.ProductId;
        ProductName = parameters.ProductName;
        return Task.CompletedTask;
    }

    public Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;
    public Task OnNavigatingFromAsync(NavigationContext context) => Task.CompletedTask;
    public Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;

    private Task GoBackAsync(CancellationToken cancellationToken)
        => _navigation.GoBackAsync(options => options.Animated = true, cancellationToken);
}
