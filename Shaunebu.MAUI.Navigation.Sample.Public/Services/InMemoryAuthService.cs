namespace Shaunebu.MAUI.Navigation.Sample.Public.Services;

/// <summary>
/// In-memory implementation of <see cref="IAuthService"/> for demonstration purposes.
/// Accepts any non-empty username/password combination.
/// </summary>
public sealed class InMemoryAuthService : IAuthService
{
    public bool IsAuthenticated { get; private set; }

    public InMemoryAuthService()
    {
        System.Diagnostics.Debug.WriteLine("InMemoryAuthService.ctor: enter");
        System.Diagnostics.Debug.WriteLine("InMemoryAuthService.ctor: exit");
    }

    public async Task<bool> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Simulate a network round-trip.
        await Task.Delay(800, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        IsAuthenticated = true;
        return true;
    }

    public void Logout() => IsAuthenticated = false;
}
