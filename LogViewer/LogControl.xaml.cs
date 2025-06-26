using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Interaction logic for LogControl.xaml.
    /// Provides a WPF UserControl for displaying and managing real-time log events.
    /// </summary>
    public partial class LogControl : UserControl, IDisposable
    {
        /// <summary>
        /// The view model backing this control, responsible for log data and state.
        /// </summary>
        private readonly LogControlViewModel _viewModel;

        private bool _disposedValue;
        private ScrollViewer? _scrollViewer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogControl"/> class.
        /// Sets up data context, event handlers, and auto-scroll behavior.
        /// </summary>
        public LogControl()
        {
            InitializeComponent();
            _viewModel = new LogControlViewModel(Dispatcher);
            this.DataContext = _viewModel;

            _viewModel.LogEvents.CollectionChanged += (s, e) =>
            {
                if (_viewModel.IsPaused) return;

                // Auto-scroll to the end when new log events are added, unless paused.
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    // Lazily find the ScrollViewer for the log list if not already found.
                    if (_scrollViewer is null)
                        _scrollViewer = GetScrollViewer(__logList);

                    if (_scrollViewer is not null)
                    {
                        // Scroll to the end on the UI thread at background priority.
                        Dispatcher.InvokeAsync(() =>
                        {
                            _scrollViewer.ScrollToEnd();
                        }, DispatcherPriority.Background);
                    }
                }
            };
        }

        /// <summary>
        /// Handles the Unloaded event for the control, ensuring resources are disposed.
        /// </summary>
        private void LogViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.Dispose();
        }

        /// <summary>
        /// Handles the mouse down event for the "Clear" button, clearing all logs asynchronously.
        /// </summary>
        private async void Clear_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await _viewModel.ClearLogsAsync();
            }
            catch (Exception ex)
            {
                // Log any error that occurs during log clearing.
                BaseLogger.LogErrorException(_viewModel.Logger, "Error while clearing logs in LogControl.", ex);
            }
        }

        /// <summary>
        /// Handles the mouse down event for the "Pause" button, toggling pause/resume state.
        /// </summary>
        private void Pause_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel.IsPaused = !_viewModel.IsPaused;
            // Update the button content to reflect the new state.
            __pauseButton.Content = _viewModel.IsPaused ? "Resume" : "Pause";
        }

        /// <summary>
        /// Recursively searches for a <see cref="ScrollViewer"/> in the visual tree of the given dependency object.
        /// </summary>
        /// <param name="depObj">The root element to search from.</param>
        /// <returns>The first <see cref="ScrollViewer"/> found, or null if none exists.</returns>
        private static ScrollViewer? GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is null) return null;
            if (depObj is ScrollViewer scrollViewer)
                return scrollViewer;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _viewModel?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
