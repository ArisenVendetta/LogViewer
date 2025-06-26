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
    /// Interaction logic for LogControl.xaml
    /// </summary>
    public partial class LogControl : UserControl, IDisposable
    {
        private readonly LogControlViewModel _viewModel;

        private bool _disposedValue;
        private ScrollViewer? _scrollViewer;

        public LogControl()
        {
            InitializeComponent();
            _viewModel = new LogControlViewModel(Dispatcher);
            this.DataContext = _viewModel;

            _viewModel.LogEvents.CollectionChanged += (s, e) =>
            {
                if (_viewModel.IsPaused) return;

                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    if (_scrollViewer is null)
                        _scrollViewer = GetScrollViewer(__logList);

                    if (_scrollViewer is not null)
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            _scrollViewer.ScrollToEnd();
                        }, DispatcherPriority.Background);
                    }
                }
            };
        }

        private void LogViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.Dispose();
        }

        private async void Clear_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await _viewModel.ClearLogsAsync();
            }
            catch (Exception ex)
            {
                BaseLogger.LogErrorException(_viewModel.Logger, "Error while clearing logs in LogControl.", ex);
            }
        }

        private void Pause_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel.IsPaused = !_viewModel.IsPaused;
            __pauseButton.Content = _viewModel.IsPaused ? "Resume" : "Pause";
        }

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
