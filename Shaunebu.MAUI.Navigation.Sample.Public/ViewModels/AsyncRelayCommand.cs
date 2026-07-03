using System.Windows.Input;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels;

/// <summary>
/// Minimal async relay command used across sample ViewModels.
/// Prevents double-invocation while the async delegate is running.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private int _isExecuting;

    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
        => Interlocked.CompareExchange(ref _isExecuting, 0, 0) == 0
           && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        Interlocked.Exchange(ref _isExecuting, 1);
        RaiseCanExecuteChanged();
        try
        {
            await _execute(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _isExecuting, 0);
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
        => MainThread.BeginInvokeOnMainThread(
            () => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
}

/// <summary>Synchronous relay command variant.</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
