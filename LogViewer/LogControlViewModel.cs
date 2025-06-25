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
        private bool _logHandleIgnoreCase = false;
        private Regex _handleCheck = new Regex(".*");
        private bool disposedValue;
        private ILogger _logger;

        public ILogger Logger => _logger ??= BaseLogger.LoggerFactory?.CreateLogger<LogControlViewModel>() ?? throw new InvalidOperationException($"Must call {nameof(BaseLogger)}.{nameof(BaseLogger.Initialize)} before creating an instance of {nameof(LogControlViewModel)}");
        public LogCollection LogEvents { get; } = new();
        public bool IsPaused { get; set; } = false;

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
                _logger.LogError(ex, "Error while clearing visible logs in LogControlViewModel.");
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
                _logger.LogError(ex, "Error while updating visible logs in LogControlViewModel.");
            }
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
                _logger.LogError(ex, "Error while adding and trimming log events in LogControlViewModel.");
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