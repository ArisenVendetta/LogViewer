using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using LogViewer;
using PropertyChanged;

namespace LogViewerExample
{
    /// <summary>
    /// Represents a command that can execute a specified action, optionally with an argument or asynchronously.
    /// </summary>
    /// <remarks>This class provides a flexible way to define and execute commands, supporting both
    /// synchronous and asynchronous actions. It is commonly used in scenarios where commands need to be bound to UI
    /// elements or executed programmatically. The command can be enabled or disabled dynamically using the <see
    /// cref="Enabled"/> property.</remarks>
    [AddINotifyPropertyChangedInterface]
    public class CustomCommand : BaseLogger
    {
        private Command? CallbackMethod { get; }
        private CommandWithArg? CallbackMethodWithArg { get; }
        private Func<Task>? CallbackMethodAsync { get; }
        private Func<object, Task>? CallbackMethodWithArgAsync { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the feature is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Gets the description associated with the current object.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// Gets or sets the argument associated with the operation.
        /// </summary>
        public object? Argument { get; set; }
        /// <summary>
        /// Gets the command that executes the primary operation associated with this object.
        /// </summary>
        public RelayCommand? RunCommand { get; }

        private CustomCommand(string description, string? id = null) : base(id)
        {
            Description = description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomCommand"/> class with the specified description, action,
        /// and optional identifier.
        /// </summary>
        /// <param name="description">A brief description of the command. This value cannot be null or empty.</param>
        /// <param name="action">The delegate to execute when the command is run. This value cannot be null.</param>
        /// <param name="id">An optional unique identifier for the command. If not provided, a default identifier will be used.</param>
        public CustomCommand(string description, Command action, string? id = null) : this(description, id)
        {
            CallbackMethod = action;
            RunCommand = new RelayCommand(Run);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomCommand"/> class with the specified description, action,
        /// and optional identifier.
        /// </summary>
        /// <param name="description">A brief description of the command's purpose. Cannot be null or empty.</param>
        /// <param name="action">The delegate to execute when the command is invoked. Cannot be null.</param>
        /// <param name="id">An optional unique identifier for the command. If not provided, a default value will be used.</param>
        public CustomCommand(string description, CommandWithArg action, string? id = null) : this(description, id)
        {
            CallbackMethodWithArg = action;
            RunCommand = new RelayCommand(RunWithArg);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomCommand"/> class with the specified description, action,
        /// and optional identifier.
        /// </summary>
        /// <param name="description">A brief description of the command. This value cannot be null or empty.</param>
        /// <param name="action">The asynchronous callback method to execute when the command is invoked. This value cannot be null.</param>
        /// <param name="id">An optional unique identifier for the command. If not provided, the identifier will be null.</param>
        public CustomCommand(string description, Func<Task> action, string? id = null) : this(description, id)
        {
            CallbackMethodAsync = action;
            RunCommand = new RelayCommand(async () => { await RunAsync(); });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomCommand"/> class with the specified description, action,
        /// and optional identifier.
        /// </summary>
        /// <param name="description">A brief description of the command's purpose or functionality. Cannot be null or empty.</param>
        /// <param name="action">The asynchronous callback method to execute when the command is invoked. Cannot be null.</param>
        /// <param name="id">An optional unique identifier for the command. If not provided, a default value will be used.</param>
        public CustomCommand(string description, Func<object, Task> action, string? id = null) : this(description, id)
        {
            CallbackMethodWithArgAsync = action;
            RunCommand = new RelayCommand(async () => { await RunWithArgAsync(); });
        }

        /// <summary>
        /// Executes the callback method on the application's main UI thread.
        /// </summary>
        /// <remarks>This method disables the current operation while the callback is being executed and
        /// re-enables it afterward.  If an exception occurs during the callback execution, it is logged.</remarks>
        private void Run() => Application.Current.Dispatcher?.Invoke(() =>
        {
            try
            {
                Enabled = false;
                CallbackMethod?.Invoke();
            }
            catch (Exception ex)
            {
                LogException(ex, $"Error in {nameof(Run)}");
            }
            finally { Enabled = true; }
        });

        /// <summary>
        /// Executes the specified callback method on the application's dispatcher thread, passing the provided
        /// argument.
        /// </summary>
        /// <remarks>This method ensures that the callback is invoked on the UI thread using the
        /// application's dispatcher.  If <see cref="Argument"/> is null, the current instance is passed to the callback
        /// instead.</remarks>
        private void RunWithArg() => Application.Current.Dispatcher?.Invoke(() =>
        {
            try
            {
                Enabled = false;
                CallbackMethodWithArg?.Invoke(Argument ?? this);
            }
            catch (Exception ex)
            {
                LogException(ex, $"Error in {nameof(RunWithArg)}");
            }
            finally { Enabled = true; }
        });

        /// <summary>
        /// Executes the asynchronous callback method on the application's dispatcher thread.
        /// </summary>
        /// <remarks>This method disables the associated functionality by setting <see cref="Enabled"/> to
        /// <see langword="false"/> while the callback is running, and re-enables it upon completion or in case of an
        /// exception. If <see cref="CallbackMethodAsync"/> is <see langword="null"/>, the method exits without
        /// performing any action.</remarks>
        /// <returns></returns>
        private async Task RunAsync() => await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                Enabled = false;
                if (CallbackMethodAsync is null) return;
                await CallbackMethodAsync();
            }
            catch (Exception ex)
            {
                LogException(ex, $"Error in {nameof(RunAsync)}");
            }
            finally { Enabled = true; }
        });

        /// <summary>
        /// Executes the callback method asynchronously on the application's UI thread, passing the specified argument.
        /// </summary>
        /// <remarks>This method disables the associated functionality while the callback is executing and
        /// re-enables it upon completion. If <see cref="CallbackMethodWithArgAsync"/> is null, the method exits without
        /// performing any action. Any exceptions thrown during the execution of the callback are logged.</remarks>
        /// <returns></returns>
        private async Task RunWithArgAsync() => await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                Enabled = false;
                if (CallbackMethodWithArgAsync is null) return;
                await CallbackMethodWithArgAsync(Argument ?? this);
            }
            catch (Exception ex)
            {
                LogException(ex, $"Error in {nameof(RunWithArgAsync)}");
            }
            finally { Enabled = true; }
        });
    }
}
