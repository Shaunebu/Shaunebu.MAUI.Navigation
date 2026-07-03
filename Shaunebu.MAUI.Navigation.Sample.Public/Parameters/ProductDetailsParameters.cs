using Shaunebu.MAUI.Navigation.Parameters;

namespace Shaunebu.MAUI.Navigation.Sample.Public.Parameters;

/// <summary>Strongly typed parameters for navigating to <see cref="Pages.Main.ProductDetailsPage"/>.</summary>
/// <param name="ProductId">The unique identifier of the product to display.</param>
/// <param name="ProductName">The display name shown in the page title.</param>
[NavigationParameters]
public sealed partial record ProductDetailsParameters(Guid ProductId, string ProductName) : INavigationParameters;
