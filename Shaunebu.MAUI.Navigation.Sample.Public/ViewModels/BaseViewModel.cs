using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shaunebu.MAUI.Navigation.Sample.Public.ViewModels;

/// <summary>
/// Minimal base class providing <see cref="INotifyPropertyChanged"/> and a busy-flag
/// pattern. ViewModels must remain completely decoupled from raw MAUI navigation APIs.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isBusy;

    /// <summary>Gets or sets a value indicating whether an async operation is running.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    /// <summary>Convenience inverse of <see cref="IsBusy"/> for button IsEnabled bindings.</summary>
    public bool IsNotBusy => !IsBusy;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;

        backingField = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
