using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using PropertyChanged;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    [AddINotifyPropertyChangedInterface]
    public class LogControlViewModel : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private string _logHandleFilter = ".*";
        private string _originalLogHandleFilter = "*";
        private bool _logHandleIgnoreCase;
        private Regex _handleCheck = new(".*");
        private bool disposedValue;
        private ILogger _logger;
        private bool _isPaused;
        private List<LogEventArgs> _pauseBuffer = [];
        private object _pauseLock = new();

        public ILogger Logger => _logger ??= BaseLogger.LoggerFactory?.CreateLogger<LogControlViewModel>() ?? throw new InvalidOperationException($"Must call {nameof(BaseLogger)}.{nameof(BaseLogger.Initialize)} before creating an instance of {nameof(LogControlViewModel)}");
        public LogCollection LogEvents { get; } = [];
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (_isPaused == value) return;
                lock (_pauseLock)
                {
                    _isPaused = value;
                    if (!_isPaused)
                    {
                        ResumeAndFlushLogs();
                    }
                }
            }
        }

        public bool LogHandleIgnoreCase
        {
            get => _logHandleIgnoreCase;
            set
            {
                if (_logHandleIgnoreCase == value) return;

                _logHandleIgnoreCase = value;
                _handleCheck = new Regex(_logHandleFilter, _logHandleIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                _ = UpdateVisibleLogsAsync();
            }
        }

        public string OriginalLogHandleFilter
        {
            get => _originalLogHandleFilter;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) _originalLogHandleFilter = "*";
                else _originalLogHandleFilter = value;

                //sanitize the original log handle filter by removing excluded characters using the same process BaseLogger does
                foreach (var c in BaseLogger.ExcludeCharsFromName.Union([' '])) _originalLogHandleFilter = _originalLogHandleFilter.Replace(c.ToString(), "").Trim();

                LogHandleFilter = _originalLogHandleFilter;
            }
        }

        public string LogHandleFilter
        {
            get => _logHandleFilter;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) _logHandleFilter = ".*";
                else _logHandleFilter = WildcardToRegex(value);
                _handleCheck = new Regex(_logHandleFilter, LogHandleIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                _ = UpdateVisibleLogsAsync();
            }
        }

        public int MaxLogSize { get; set; } = BaseLogger.MaxLogQueueSize;

        public LogControlViewModel(Dispatcher dispatcher, string? logHandleFilter = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            LogHandleFilter = logHandleFilter ?? string.Empty;
            _logger = BaseLogger.LoggerFactory?.CreateLogger<LogControlViewModel>() ?? throw new InvalidOperationException($"Must call {nameof(BaseLogger)}.{nameof(BaseLogger.Initialize)} before creating an instance of {nameof(LogControlViewModel)}");
            BaseLogger.DebugLogEvent += OnLogEventAsync;
        }

        private async Task OnLogEventAsync(object sender, LogEventArgs e)
        {
            if (e is null) return;

            if (IsLogEventHandleFiltered(e.LogHandle))
            {
                lock (_pauseLock)
                {
                    if (IsPaused)
                    {
                        _pauseBuffer.Add(e);
                        return;
                    }
                }

                if (_dispatcher.CheckAccess())
                    await AddAndTrimLogEventsIfNeededAsync(e);
                else
                    await _dispatcher.InvokeAsync(async () => await AddAndTrimLogEventsIfNeededAsync(e));
            }
        }

        private bool IsLogEventHandleFiltered(string? logHandle)
        {
            if (string.IsNullOrWhiteSpace(LogHandleFilter)) return true;
            if (string.IsNullOrWhiteSpace(logHandle)) return false;
            // Check if the filter string contains the log handle
            return _handleCheck.IsMatch(logHandle);
        }

        internal static string WildcardToRegex(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return ".*";
            // Escape special regex characters, replace wildcard '*' with '.*' and replace wildcard '?' with '.'
            var parts = pattern.Split('|').Select(part => Regex.Escape(part).Replace(@"\*", ".*").Replace(@"\?", "."));
            return $"^(?:{string.Join("|", parts)})$";
        }

        public async Task ClearLogsAsync()
        {
            try
            {
                if (_dispatcher.CheckAccess())
                    LogEvents.Clear();
                else
                    await _dispatcher.InvokeAsync(() => LogEvents.Clear());
            }
            catch (Exception ex)
            {
                BaseLogger.LogErrorException(_logger, "Error while clearing visible logs in LogControlViewModel.", ex);
            }
        }

        private async Task UpdateVisibleLogsAsync()
        {
            try
            {
                await ClearLogsAsync();
                var tempCopy = (BaseLogger.DebugLogQueue?.ToArray() ?? Array.Empty<LogEventArgs>())
                    .OrderBy(x => x.LogDateTime)
                    .Where(e => IsLogEventHandleFiltered(e.LogHandle))
                    .ToArray();

                tempCopy = tempCopy.Length > MaxLogSize ? tempCopy.Skip(tempCopy.Length - MaxLogSize).ToArray() : tempCopy;

                foreach (var logEvent in tempCopy)
                {
                    if (_dispatcher.CheckAccess())
                        LogEvents.Add(logEvent);
                    else
                        await _dispatcher.InvokeAsync(() => LogEvents.Add(logEvent));
                }
            }
            catch (Exception ex)
            {
                BaseLogger.LogErrorException(_logger, "Error while updating visible logs in LogControlViewModel.", ex);
            }
        }

        private void ResumeAndFlushLogs()
        {
            if (_pauseBuffer.Count == 0) return;

            LogEventArgs last;
            lock (_pauseLock)
            {
                var temp = _pauseBuffer.Take(_pauseBuffer.Count - 1).ToList();
                last = _pauseBuffer.Last();
                if (_dispatcher.CheckAccess())
                    LogEvents.AddRange(new List<LogEventArgs>(_pauseBuffer));
                else
                    _dispatcher.Invoke(() => LogEvents.AddRange(new List<LogEventArgs>(_pauseBuffer)));
                _pauseBuffer.Clear();
            }
            AddAndTrimLogEventsIfNeededAsync(last).GetAwaiter().GetResult();
        }

        private async Task AddAndTrimLogEventsIfNeededAsync(LogEventArgs e)
        {
            try
            {
                if (_dispatcher.CheckAccess())
                    LogEvents.Add(e);
                else
                    await _dispatcher.InvokeAsync(() => LogEvents.Add(e));

                int overFlow = LogEvents.Count - MaxLogSize;
                if (overFlow > 0)
                {
                    int amountToRemove = overFlow + ((int)(MaxLogSize * 0.1));
                    if (_dispatcher.CheckAccess())
                        LogEvents.RemoveRange(0, amountToRemove); // Remove oldest
                    else
                        await _dispatcher.InvokeAsync(() => LogEvents.RemoveRange(0, amountToRemove)); // Remove oldest
                }
            }
            catch (Exception ex)
            {
                BaseLogger.LogErrorException(_logger, "Error while adding and trimming log events in LogControlViewModel.", ex);
            }
            finally
            {

            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    BaseLogger.DebugLogEvent -= OnLogEventAsync;
                }

                disposedValue = true;
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