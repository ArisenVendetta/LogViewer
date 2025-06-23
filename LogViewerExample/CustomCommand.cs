using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace LogViewerExample
{
    public class CustomCommand
    {
        private bool _enabled = true;
        private string _description = string.Empty;

        private CustomCommand(string description)
        {
            Description = description;
        }

        public CustomCommand(string description, Command action) : this(description)
        {
            CallbackMethod = action;
            RunCommand = new RelayCommand(Run);
        }

        public CustomCommand(string description, CommandWithArg action) : this(description)
        {
            CallbackMethodWithArg = action;
            RunCommand = new RelayCommand(RunWithArg);
        }

        public CustomCommand(string description, Func<Task> action) : this(description)
        {
            CallbackMethodAsync = action;
            RunCommand = new RelayCommand(async () => { await RunAsync(); });
        }

        public CustomCommand(string description, Func<object, Task> action) : this(description)
        {
            CallbackMethodWithArgAsync = action;
            RunCommand = new RelayCommand(async () => { await RunWithArgAsync(); });
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                NotifyPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                NotifyPropertyChanged();
            }
        }

        public object? Argument { get; set; }

        private Command? CallbackMethod { get; set; }
        private CommandWithArg? CallbackMethodWithArg { get; set; }
        private Func<Task>? CallbackMethodAsync { get; set; }
        private Func<object, Task>? CallbackMethodWithArgAsync { get; set; }


        public RelayCommand? RunCommand { get; private set; }

        private void Run() => Application.Current.Dispatcher?.Invoke(() =>
        {
            try
            {
                Enabled = false;
                CallbackMethod?.Invoke();
            }
            catch (Exception) { }
            finally { Enabled = true; }
        });

        private void RunWithArg() => Application.Current.Dispatcher?.Invoke(() =>
        {
            try
            {
                Enabled = false;
                CallbackMethodWithArg?.Invoke(Argument ?? this);
            }
            catch (Exception) { }
            finally { Enabled = true; }
        });

        private async Task RunAsync() => await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                Enabled = false;
                if (CallbackMethodAsync is null) return;
                await CallbackMethodAsync();
            }
            catch (Exception) { }
            finally { Enabled = true; }
        });

        private async Task RunWithArgAsync() => await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                Enabled = false;
                if (CallbackMethodWithArgAsync is null) return;
                await CallbackMethodWithArgAsync(Argument ?? this);
            }
            catch (Exception) { }
            finally { Enabled = true; }
        });

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
