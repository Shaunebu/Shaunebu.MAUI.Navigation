namespace Shaunebu.MAUI.Navigation.Sample.Public.Services;

/// <summary>
/// Simulates authentication state for the sample app.
/// In a real app this would wrap a token store or identity provider.
/// </summary>
public interface IAuthService
{
    /// <summary>Gets a value indicating whether the user is currently authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Simulates a login operation (always succeeds after a brief delay).</summary>
    Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>Clears the authentication state.</summary>
    void Logout();
}
