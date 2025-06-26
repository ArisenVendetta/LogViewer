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
    /// <summary>
    /// ViewModel for the LogControl user interface.
    /// Manages log event filtering, pausing, and thread-safe updates to the log collection for WPF data binding.
    /// </summary>
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
        private readonly List<LogEventArgs> _pauseBuffer = [];
        private readonly object _pauseLock = new();

        /// <summary>
        /// Gets the logger instance for this view model.
        /// </summary>
        internal ILogger Logger => _logger ??= BaseLogger.LoggerFactory?.CreateLogger<LogControlViewModel>() ?? throw new InvalidOperationException($"Must call {nameof(BaseLogger)}.{nameof(BaseLogger.Initialize)} before creating an instance of {nameof(LogControlViewModel)}");

        /// <summary>
        /// Gets the observable collection of log events for data binding.
        /// </summary>
        public LogCollection LogEvents { get; } = [];

        /// <summary>
        /// Gets or sets whether log updates are paused.
        /// When set to true, incoming log events are buffered and not shown until resumed.
        /// </summary>
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

        /// <summary>
        /// Gets or sets whether log handle filtering is case-insensitive.
        /// Changing this property updates the filter and visible logs.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the original (user-supplied) log handle filter string, before wildcard/regex conversion.
        /// Setting this property also updates the effective filter.
        /// </summary>
        public string OriginalLogHandleFilter
        {
            get => _originalLogHandleFilter;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) _originalLogHandleFilter = "*";
                else _originalLogHandleFilter = value;

                //sanitize the original log handle filter by removing excluded characters using the same process BaseLogger does
                foreach (var c in BaseLogger.ExcludeCharsFromHandle.Union([' '])) _originalLogHandleFilter = _originalLogHandleFilter.Replace(c.ToString(), "").Trim();

                LogHandleFilter = _originalLogHandleFilter;
            }
        }

        /// <summary>
        /// Gets the effective log handle filter as a regex string.
        /// Setting this property updates the filter and visible logs.
        /// </summary>
        public string LogHandleFilter
        {
            get => _logHandleFilter;
            private set
            {
                if (string.IsNullOrWhiteSpace(value)) _logHandleFilter = ".*";
                else _logHandleFilter = WildcardToRegex(value);
                _handleCheck = new Regex(_logHandleFilter, LogHandleIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                _ = UpdateVisibleLogsAsync();
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of log events to keep in the collection.
        /// </summary>
        public int MaxLogSize { get; set; } = BaseLogger.MaxLogQueueSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogControlViewModel"/> class.
        /// </summary>
        /// <param name="dispatcher">The WPF dispatcher for UI thread synchronization.</param>
        /// <param name="logHandleFilter">Optional initial log handle filter.</param>
        /// <exception cref="ArgumentNullException">Thrown if dispatcher is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if logger factory is not initialized.</exception>
        public LogControlViewModel(Dispatcher dispatcher, string? logHandleFilter = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            LogHandleFilter = logHandleFilter ?? string.Empty;
            _logger = BaseLogger.LoggerFactory?.CreateLogger<LogControlViewModel>() ?? throw new InvalidOperationException($"Must call {nameof(BaseLogger)}.{nameof(BaseLogger.Initialize)} before creating an instance of {nameof(LogControlViewModel)}");
            BaseLogger.DebugLogEvent += OnLogEventAsync;
        }

        /// <summary>
        /// Handles log events from the logger, applying filtering and pause logic.
        /// </summary>
        /// <param name="sender">The log event sender.</param>
        /// <param name="e">The log event arguments.</param>
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

                // Ensure log updates are performed on the UI thread.
                if (_dispatcher.CheckAccess())
                    await AddAndTrimLogEventsIfNeededAsync(e);
                else
                    await _dispatcher.InvokeAsync(async () => await AddAndTrimLogEventsIfNeededAsync(e));
            }
        }

        /// <summary>
        /// Determines if a log event's handle matches the current filter.
        /// </summary>
        /// <param name="logHandle">The log handle to check.</param>
        /// <returns>True if the log handle matches the filter; otherwise, false.</returns>
        private bool IsLogEventHandleFiltered(string? logHandle)
        {
            if (string.IsNullOrWhiteSpace(LogHandleFilter)) return true;
            if (string.IsNullOrWhiteSpace(logHandle)) return false;
            // Check if the filter string contains the log handle
            return _handleCheck.IsMatch(logHandle);
        }

        /// <summary>
        /// Converts a wildcard pattern (with * and ?) to a regex string.
        /// Supports multiple patterns separated by '|'.
        /// </summary>
        /// <param name="pattern">The wildcard pattern.</param>
        /// <returns>A regex string equivalent to the wildcard pattern.</returns>
        internal static string WildcardToRegex(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return ".*";
            // Escape special regex characters, replace wildcard '*' with '.*' and replace wildcard '?' with '.'
            var parts = pattern.Split('|').Select(part => Regex.Escape(part).Replace(@"\*", ".*").Replace(@"\?", "."));
            return $"^(?:{string.Join("|", parts)})$";
        }

        /// <summary>
        /// Clears all log events from the collection asynchronously, ensuring UI thread access.
        /// </summary>
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

        /// <summary>
        /// Updates the visible logs in the collection based on the current filter and maximum size.
        /// </summary>
        private async Task UpdateVisibleLogsAsync()
        {
            try
            {
                await ClearLogsAsync();
                var tempCopy = (BaseLogger.DebugLogQueue?.ToArray() ?? Array.Empty<LogEventArgs>())
                    .OrderBy(x => x.LogDateTime)
                    .Where(e => IsLogEventHandleFiltered(e.LogHandle))
                    .ToArray();

                // Trim to the most recent MaxLogSize entries if needed.
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

        /// <summary>
        /// Resumes log updates after a pause and flushes any buffered log events to the collection.
        /// </summary>
        private void ResumeAndFlushLogs()
        {
            if (_pauseBuffer.Count == 0) return;

            LogEventArgs last;
            lock (_pauseLock)
            {
                // Add all buffered events to the collection.
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

        /// <summary>
        /// Adds a log event to the collection and trims old entries if the maximum size is exceeded.
        /// </summary>
        /// <param name="e">The log event to add.</param>
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
                    // Remove a little more than the overflow to reduce frequent trimming.
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