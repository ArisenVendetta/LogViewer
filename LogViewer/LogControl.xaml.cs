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
        private readonly object _pauseLock = new();
        private readonly object _paragraphLock = new();
        private Paragraph _paragraph = new();
        private bool _disposedValue;
        private readonly DispatcherTimer _updateTimer;
        private readonly List<LogEventArgs> _pendingLogsAdd = [];
        private readonly List<LogEventArgs> _pendingLogsRemove = [];
        private readonly object _pendingAddLock = new();
        private readonly object _pendingRemoveLock = new();

        public LogControl()
        {
            InitializeComponent();
            _viewModel = new LogControlViewModel(this.Dispatcher);
            this.DataContext = _viewModel;

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
            _updateTimer.Tick += (s, e) => FlushPendingLogs();
            _updateTimer.Start();

            DisplayLogsInRichTextBox(__logBox, _viewModel.LogEvents.Items);

            _viewModel.LogEvents.CollectionChanged += (s, e) =>
            {
                if (_viewModel.IsPaused) return;
                HandleRichTextBoxCollectionChangedUpdate(s, e);
            };
        }

        private void HandleRichTextBoxCollectionChangedUpdate(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            bool doReset = false;
            lock (_pauseLock)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                {
                    doReset = true;
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    var newItems = (e?.NewItems ?? new List<LogEventArgs>());
                    if (newItems.Count >= 250)
                        doReset = true;
                    else
                    {
                        lock (_pendingAddLock)
                        {
                            _pendingLogsAdd.AddRange(newItems.Cast<LogEventArgs>());
                        }
                    }
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    var oldItems = (e?.OldItems ?? new List<LogEventArgs>());
                    if (oldItems.Count >= 250)
                        doReset = true;
                    else
                    {
                        lock (_pendingRemoveLock)
                        {
                            _pendingLogsRemove.AddRange(oldItems.Cast<LogEventArgs>());
                        }
                    }
                }
            }

            if (doReset)
            {
                DisplayLogsInRichTextBox(__logBox, _viewModel.LogEvents.Items);
                lock (_pendingAddLock)
                {
                    _pendingLogsAdd.Clear();
                }
                lock (_pendingRemoveLock)
                {
                    _pendingLogsRemove.Clear();
                }
            }
        }

        private void FlushPendingLogs()
        {
            try
            {
                _updateTimer.Stop();

                RemoveOldLogLines();
                AddNewLogLines();
            }
            catch (Exception ex)
            {
                _viewModel.Logger.LogError(ex, "Error while flushing pending logs.");
            }
            finally
            {
                if (!_updateTimer.IsEnabled)
                    _updateTimer.Start();
            }
        }

        private void AddNewLogLines()
        {
            List<Inline> toRemove = [];
            int amountToRemove = (int)Math.Floor(_viewModel.MaxLogSize * 0.1);
            if (_pendingLogsRemove.Count > 0 && _pendingLogsRemove.Count >= amountToRemove)
            {
                lock (_pendingRemoveLock)
                {
                    var guids = _pendingLogsRemove.Take(amountToRemove).Select(x => x.Guid).ToList();
                    _pendingLogsRemove.RemoveRange(0, guids.Count);
                    lock (_paragraphLock)
                    {
                        toRemove = _paragraph.Inlines.Where(inline =>
                        {
                            if (inline.Tag is Guid guid)
                                return guids.Contains(guid);
                            return false;
                        }).ToList();
                    }
                }
            }

            if ((toRemove?.Count ?? 0) > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    lock (_paragraphLock)
                    {
                        foreach (var inline in toRemove ?? [])
                        {
                            _paragraph.Inlines.Remove(inline);
                        }
                    }
                });
            }
        }

        private void RemoveOldLogLines()
        {
            List<Inline> pending = [];
            if (_pendingLogsAdd.Count > 0)
            {
                List<LogEventArgs> toAdd;
                lock (_pendingAddLock)
                {
                    toAdd = new(_pendingLogsAdd.Take(250));
                    _pendingLogsAdd.RemoveRange(0, toAdd.Count);
                }

                foreach (var log in toAdd)
                {
                    pending.AddRange(CreateParagraphFromLogEventArgs(log));
                }
            }
            if ((pending?.Count ?? 0) > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    lock (_paragraphLock)
                    {
                        _paragraph.Inlines.AddRange(pending ?? []);
                    }
                    __logBox.ScrollToEnd();
                });
            }
        }

        private void DisplayLogsInRichTextBox(RichTextBox richTextBox, IEnumerable<LogEventArgs> logs)
        {
            var doc = new FlowDocument();
            lock (_paragraphLock)
            {
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
            }
            Dispatcher.Invoke(() =>
            {
                richTextBox.Document = doc;
                __logBox.ScrollToEnd();
            });
        }

        private IEnumerable<Inline> CreateParagraphFromLogEventArgs(LogEventArgs eventArgs)
        {
            if (eventArgs is null) yield break;

            var logParts = eventArgs.GetLogMessageParts();
            yield return new Run($"{logParts.Timestamp} ") { Tag = eventArgs.Guid };
            yield return new Run("[") { Tag = eventArgs.Guid };
            yield return new Run(logParts.LogHandle) { Foreground = GetColorForLogLevel(eventArgs.LogLevel), Tag = eventArgs.Guid };
            yield return new Run("] ") { Tag = eventArgs.Guid };
            yield return new Run(logParts.Body) { Foreground = new SolidColorBrush(eventArgs.LogColor), Tag = eventArgs.Guid };
            yield return new LineBreak() { Tag = eventArgs.Guid };
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
                    DisplayLogsInRichTextBox(__logBox, _viewModel.LogEvents.Items);
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
