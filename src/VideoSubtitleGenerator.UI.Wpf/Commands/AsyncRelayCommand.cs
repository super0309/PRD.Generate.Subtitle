using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using VideoSubtitleGenerator.Core;

namespace VideoSubtitleGenerator.UI.Wpf.Commands;

/// <summary>
/// An asynchronous command implementation that can execute async tasks with cancellation support.
/// Implements ICommand for use with WPF data binding.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, CancellationToken, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isExecuting;

    /// <summary>
    /// Gets whether the command is currently executing.
    /// </summary>
    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting != value)
            {
                _isExecuting = value;
                RaiseCanExecuteChanged();
            }
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Initializes a new instance of AsyncRelayCommand.
    /// </summary>
    /// <param name="execute">The async execution logic.</param>
    /// <param name="canExecute">The execution status logic (optional).</param>
    public AsyncRelayCommand(
        Func<object?, CancellationToken, Task> execute,
        Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Simplified constructor for commands without cancellation token.
    /// </summary>
    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        : this((param, _) => execute(param), canExecute)
    {
    }

    /// <summary>
    /// Simplified constructor for parameterless commands.
    /// </summary>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this((_, __) => execute(), canExecute != null ? _ => canExecute() : null)
    {
    }

    public bool CanExecute(object? parameter)
    {
        // Cannot execute if already running
        if (IsExecuting)
            return false;

        return _canExecute == null || _canExecute(parameter);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        IsExecuting = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await _execute(parameter, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled, don't propagate
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            // Log or handle exception
            System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand exception: {ex.Message}");
            // In production, use proper logging
            throw;
        }
        finally
        {
            IsExecuting = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Cancels the currently executing command.
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Raises the CanExecuteChanged event.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// A strongly-typed version of AsyncRelayCommand.
/// </summary>
public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, CancellationToken, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isExecuting;

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting != value)
            {
                _isExecuting = value;
                RaiseCanExecuteChanged();
            }
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public AsyncRelayCommand(
        Func<T?, CancellationToken, Task> execute,
        Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        : this((param, _) => execute(param), canExecute)
    {
    }

    public bool CanExecute(object? parameter)
    {
        if (IsExecuting)
            return false;

        return _canExecute == null || _canExecute((T?)parameter);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        IsExecuting = true;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await _execute((T?)parameter, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand<{typeof(T).Name}> exception: {ex.Message}");
            throw;
        }
        finally
        {
            IsExecuting = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
