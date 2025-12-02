using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoSubtitleGenerator.UI.Wpf.ViewModels;

/// <summary>
/// Base class for all ViewModels. Implements INotifyPropertyChanged for data binding.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed. Auto-filled by compiler.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the property value and raises PropertyChanged if the value changed.
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">New value to set.</param>
    /// <param name="propertyName">Name of the property. Auto-filled by compiler.</param>
    /// <returns>True if the value was changed, false if it was the same.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets the property value, raises PropertyChanged, and executes an action if the value changed.
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">New value to set.</param>
    /// <param name="onChanged">Action to execute after the property changes.</param>
    /// <param name="propertyName">Name of the property. Auto-filled by compiler.</param>
    /// <returns>True if the value was changed, false if it was the same.</returns>
    protected bool SetProperty<T>(
        ref T field, 
        T value, 
        System.Action onChanged, 
        [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            onChanged?.Invoke();
            return true;
        }
        return false;
    }
}
