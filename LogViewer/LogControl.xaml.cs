using System;
using System.Collections.Generic;
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
        private readonly object _pauseLock = new object();
        private Paragraph? _paragraph;
        private bool _disposedValue;
        private readonly DispatcherTimer _updateTimer;
        private readonly List<LogEventArgs> _pendingLogs = new List<LogEventArgs>();

        public LogControl()
        {
            InitializeComponent();
            _viewModel = new LogControlViewModel(this.Dispatcher);
            this.DataContext = _viewModel;

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _updateTimer.Tick += (s, e) => FlushPendingLogs();
            _updateTimer.Start();

            _viewModel.LogEvents.CollectionChanged += (s, e) =>
            {
                if (_viewModel.IsPaused) return;
                lock (_pauseLock)
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                    {
                        DisplayLogsInRichTextBox(__logBox, _viewModel.LogEvents);
                    }
                    else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                    {
                        var newItems = (e?.NewItems ?? new List<LogEventArgs>());
                        _pendingLogs.AddRange(newItems.Cast<LogEventArgs>());
                    }
                    else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    {
                        if (_paragraph != null)
                        {
                            var oldItems = (e?.OldItems ?? new List<LogEventArgs>());
                            for (int i = 0; i < oldItems.Count; i++)
                            {
                                while (_paragraph.Inlines.FirstInline != null)
                                {
                                    var inline = _paragraph.Inlines.FirstInline;
                                    _paragraph.Inlines.Remove(inline);
                                    if (inline is LineBreak)
                                        break;
                                }
                            }
                        }
                    }
                }
            };
        }

        private void FlushPendingLogs()
        {
            if (_pendingLogs.Count == 0) return;
            if (_paragraph is null)
            {
                var doc = new FlowDocument();
                _paragraph = new Paragraph()
                {
                    Margin = new Thickness(0),
                    Padding = new Thickness(0)
                };
                doc.Blocks.Add(_paragraph);
                __logBox.Document = doc;
            }
            foreach (var log in _pendingLogs)
            {
                _paragraph.Inlines.AddRange(CreateParagraphFromLogEventArgs(log));
            }
            _pendingLogs.Clear();
            __logBox.ScrollToEnd();
        }

        private void DisplayLogsInRichTextBox(RichTextBox richTextBox, IEnumerable<LogEventArgs> logs)
        {
            var doc = new FlowDocument();
            _paragraph = new Paragraph()
            {
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            };
            foreach (var log in logs)
            {
                _paragraph.Inlines.AddRange(CreateParagraphFromLogEventArgs(log));
            }
            doc.Blocks.Add(_paragraph);
            richTextBox.Document = doc;
        }

        private IEnumerable<Inline> CreateParagraphFromLogEventArgs(LogEventArgs eventArgs)
        {
            if (eventArgs is null) yield break;

            var logParts = eventArgs.GetLogMessageParts();
            yield return new Run($"{logParts.Timestamp} ");
            yield return new Run("[");
            yield return new Run(logParts.LogHandle) { Foreground = GetColorForLogLevel(eventArgs.LogLevel) };
            yield return new Run("] ");
            yield return new Run(logParts.Body) { Foreground = new SolidColorBrush(eventArgs.LogColor) };
            yield return new LineBreak();
        }

        private SolidColorBrush GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Critical    => new SolidColorBrush(Colors.Red),
                LogLevel.Error       => new SolidColorBrush(Colors.OrangeRed),
                LogLevel.Warning     => new SolidColorBrush(Colors.Orange),
                LogLevel.Information => new SolidColorBrush(Colors.Black),
                LogLevel.Debug       => new SolidColorBrush(Colors.Blue),
                LogLevel.Trace       => new SolidColorBrush(Colors.Gray),
                _                    => new SolidColorBrush(Colors.Black)
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
                _viewModel.Logger.LogError(ex, "Error clearing logs.");
            }
        }

        private void Pause_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            lock (_pauseLock)
            {
                if (_viewModel.IsPaused)
                {
                    DisplayLogsInRichTextBox(__logBox, _viewModel.LogEvents);
                    __logBox.ScrollToEnd();
                }
                _viewModel.IsPaused = !_viewModel.IsPaused;
            }
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
