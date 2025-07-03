using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.IO;

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
        private bool _logHandleIgnoreCase;
        private Regex _handleCheck = new(".*");
        private bool disposedValue;
        private ILogger _logger;
        private bool _isPaused;
        private bool _pauseEnabled = true;
        private readonly List<LogEventArgs> _pauseBuffer = [];
        private readonly object _pauseLock = new();

        /// <summary>
        /// Gets the logger instance for this view model.
        /// </summary>
#pragma warning disable CS8603, CS8601 // CS8603: Possible null reference return.
                                       // CS8601: Possible null reference assignment.
        internal ILogger Logger => _logger ??= CreateLoggerIfNotDesignMode(); // will never be null except for in design mode, where logging is not needed.
#pragma warning restore CS8603, CS8601

        /// <summary>
        /// Gets the observable collection of log events for data binding.
        /// </summary>
        public LogCollection LogEvents { get; } = [];

        /// <summary>
        /// Gets the command that exports application logs asynchronously.
        /// </summary>
        /// <remarks>
        /// Use this command to trigger the export of logs for diagnostic or archival purposes.
        /// Ensure that any required permissions or prerequisites for exporting logs are met before executing the command.
        /// </remarks>
        public IAsyncRelayCommand ExportLogsCommand { get; }
        /// <summary>
        /// Gets the command that clears all logs asynchronously.
        /// </summary>
        public IAsyncRelayCommand ClearLogsCommand { get; }
        /// <summary>
        /// Gets the command that toggles the paused state of the application.
        /// </summary>
        public RelayCommand TogglePauseCommand { get; }
        /// <summary>
        /// Gets the text displayed when the application is in a paused state.
        /// </summary>
        public string PausedText { get; internal set; } = "Pause";
        /// <summary>
        /// Gets the collection of file types supported for export.
        /// </summary>
        public static ObservableCollection<FileType> SupportedExportFileTypes => BaseLogger.SupportedExportFileTypes;
        /// <summary>
        /// Gets or sets the file type selected for export operations.
        /// </summary>
        public FileType SelectedExportFileType { get; set; } = new FileType("JSON", ".json");
        public int PauseBufferCount
        {
            get => _pauseBuffer?.Count ?? 0;
        }

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
                PausedText = _isPaused ? "Resume" : "Pause";
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether pausing is enabled.
        /// </summary>
        /// <remarks>When set to <see langword="false"/>, any active paused state will be
        /// cleared.</remarks>
        public bool PausingEnabled
        {
            get => _pauseEnabled;
            set
            {
                if (_pauseEnabled == value) return;
                _pauseEnabled = value;
                if (!_pauseEnabled && IsPaused)
                {
                    IsPaused = false;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether logs are currently being exported.
        /// </summary>
        public bool ExportingLogs { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether automatic scrolling is enabled.
        /// </summary>
        public bool AutoScroll { get; set; } = true;

        /// <summary>
        /// Gets or sets the format used for exporting log entries.
        /// </summary>
        public string LogExportFormat { get; set; } = BaseLogger.LogExportFormat;

        /// <summary>
        /// Gets or sets a value indicating whether the filter handling UI is visible.
        /// </summary>
        public bool HandleFilterVisible { get; set; } = true;

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
        /// Gets the effective log handle filter as a regex string.
        /// Setting this property updates the filter and visible logs.
        /// </summary>
        /// <remarks>
        /// If you set it to an empty string, whitespace or null, it defaults to ".*" (matches all handles). <br/>
        /// If you want to use wildcards, pass your wildcard pattern through <see cref="WildcardToRegex"/> method before assigning it.
        /// </remarks>
        public string LogHandleFilter
        {
            get => _logHandleFilter;
            set => SetRegexFilterIfValid(value);
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
#pragma warning disable CS8618, CS8601 // CS8618: Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
                                       // CS8601: Possible null reference assignment.
        public LogControlViewModel(Dispatcher dispatcher, string? logHandleFilter = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            LogHandleFilter = logHandleFilter ?? string.Empty;
            _logger = CreateLoggerIfNotDesignMode(); // will never be null except for in design mode, where logging is not needed.
            BaseLogger.DebugLogEvent += OnLogEventAsync;

            ClearLogsCommand = new AsyncRelayCommand(ClearLogsAsync);
            TogglePauseCommand = new(() =>
            {
                if (PausingEnabled)
                    IsPaused = !IsPaused;
            });
            ExportLogsCommand = new AsyncRelayCommand(async () => _ = await ExportLogsAsync());

            SelectedExportFileType = SupportedExportFileTypes.FirstOrDefault() ?? new FileType("JSON", ".json");
        }
#pragma warning restore CS8601, CS8601

        /// <summary>
        /// Needed to move creation of logger to a separate method check for design mode preventing the exception from showing in the XAML designer
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private ILogger? CreateLoggerIfNotDesignMode()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return null;
            return BaseLogger.LoggerFactory?.CreateLogger<LogControlViewModel>() ?? throw new InvalidOperationException($"Must call {nameof(BaseLogger)}.{nameof(BaseLogger.Initialize)} before creating an instance of {nameof(LogControlViewModel)}");
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
                await DispatchIfNecessaryAsync(async () => await AddAndTrimLogEventsIfNeededAsync(e));
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
        public static string WildcardToRegex(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return ".*";
            // Escape special regex characters, replace wildcard '*' with '.*' and replace wildcard '?' with '.'
            var parts = pattern.Split('|').Select(part => Regex.Escape(part).Replace(@"\*", ".*").Replace(@"\?", "."));
            return $"^(?:{string.Join("|", parts)})$";
        }

        /// <summary>
        /// Sets the log handle filter to the specified regular expression pattern if it is valid.
        /// </summary>
        /// <remarks>
        /// The method validates the provided regular expression pattern by attempting to create a new  <see cref="Regex"/> instance.
        /// If the pattern is invalid, the filter remains unchanged.
        /// </remarks>
        /// <param name="filter">
        /// The regular expression pattern to use as the log handle filter.
        /// If <paramref name="filter"/> is  <see langword="null"/> or consists only of whitespace, a default pattern of <c>".*"</c> is used, which matches all log handles.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the specified regular expression pattern is valid and the filter is updated;
        /// otherwise, <see langword="false"/> if the pattern is invalid.
        /// </returns>
        internal bool SetRegexFilterIfValid(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) filter = ".*";

            try
            {
                // Validate the regex pattern by creating a new Regex instance.
                _handleCheck = new Regex(filter, LogHandleIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                _logHandleFilter = filter;
                _ = UpdateVisibleLogsAsync();
                return true;
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, do not change the filter.
                return false;
            }
        }

        /// <summary>
        /// Clears all log events from the collection asynchronously, ensuring UI thread access.
        /// </summary>
        public async Task ClearLogsAsync()
        {
            try
            {
                await DispatchIfNecessaryAsync(() => LogEvents.Clear());
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
                var tempCopy = (BaseLogger.DebugLogQueue?.ToArray() ?? [])
                    .Where(e => IsLogEventHandleFiltered(e.LogHandle))
                    .OrderBy(x => x.LogDateTime)
                    .ToArray();

                // Trim to the most recent MaxLogSize entries if needed.
                tempCopy = tempCopy.Length > MaxLogSize ? tempCopy.Skip(tempCopy.Length - MaxLogSize).ToArray() : tempCopy;

                foreach (var logEvent in tempCopy)
                {
                    await DispatchIfNecessaryAsync(() => LogEvents.Add(logEvent));
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
                last = _pauseBuffer[^1];
                DispatchIfNecessary(() => LogEvents.AddRange(new List<LogEventArgs>(_pauseBuffer)));
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
                await DispatchIfNecessaryAsync(() => LogEvents.Add(e));

                int overFlow = LogEvents.Count - MaxLogSize;
                if (overFlow > 0)
                {
                    // Remove a little more than the overflow to reduce frequent trimming.
                    int amountToRemove = overFlow + ((int)(MaxLogSize * 0.1));
                    await DispatchIfNecessaryAsync(() => LogEvents.RemoveRange(0, amountToRemove)); // Remove oldest
                }
            }
            catch (Exception ex)
            {
                BaseLogger.LogErrorException(_logger, "Error while adding and trimming log events in LogControlViewModel.", ex);
            }
        }

        /// <summary>
        /// Executes the specified callback on the dispatcher thread if the current thread does not have access to it.
        /// </summary>
        /// <remarks>If the current thread has access to the dispatcher, the callback is executed
        /// immediately.  Otherwise, the callback is invoked on the dispatcher thread.</remarks>
        /// <typeparam name="T">The type of the result returned by the callback.</typeparam>
        /// <param name="callback">The function to execute. This function is invoked either directly or through the dispatcher.</param>
        private void DispatchIfNecessary<T>(Func<T> callback)
        {
            if (_dispatcher.CheckAccess())
            {
                callback();
            }
            else
            {
                _dispatcher.Invoke(callback);
            }
        }

        /// <summary>
        /// Executes the specified callback on the dispatcher thread if the current thread does not have access to it.
        /// </summary>
        /// <remarks>If the current thread has access to the dispatcher, the callback is executed
        /// immediately.  Otherwise, the callback is dispatched asynchronously to the dispatcher thread.</remarks>
        /// <param name="callback">The action to be executed. Cannot be <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task DispatchIfNecessaryAsync(Action callback)
        {
            if (_dispatcher.CheckAccess())
            {
                callback();
            }
            else
            {
                await _dispatcher.InvokeAsync(callback);
            }
        }

        /// <summary>
        /// Executes the specified callback on the dispatcher thread if the current thread does not have access to it.
        /// </summary>
        /// <remarks>If the current thread has access to the dispatcher, the callback is executed
        /// synchronously on the same thread. Otherwise, the callback is dispatched to the dispatcher thread and
        /// executed asynchronously.</remarks>
        /// <typeparam name="T">The type of the value returned by the callback.</typeparam>
        /// <param name="callback">The function to execute. This function is invoked either directly or via the dispatcher, depending on thread
        /// access.</param>
        /// <returns>A task that represents the asynchronous operation. The task's result is the value returned by the callback.</returns>
        private async Task<T> DispatchIfNecessaryAsync<T>(Func<T> callback)
        {
            if (_dispatcher.CheckAccess())
            {
                return callback();
            }
            else
            {
                return await _dispatcher.InvokeAsync(callback);
            }
        }

        /// <summary>
        /// Displays a save file dialog to the user and returns the selected file path, including the file name.
        /// </summary>
        /// <remarks>
        /// The dialog allows the user to specify a file name and location for exporting logs.
        /// The file type and extension are determined by the <see cref="SelectedExportFileType"/> property.
        /// If the user cancels the dialog or provides an invalid file path, the method returns <see langword="null"/>.
        /// </remarks>
        /// <returns>
        /// The full file path, including the file name, selected by the user;
        /// or <see langword="null"/> if the dialog is canceled or the file path is invalid.
        /// </returns>
        private string? GetLogExportFilePathWithName()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Title = "Export Logs",
                Filter = $"{SelectedExportFileType.Name} (*{SelectedExportFileType.Extension})|*{SelectedExportFileType.Extension}",
                DefaultExt = SelectedExportFileType?.Extension ?? ".json",
                AddExtension = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Ensure the file path is valid and return it.
                string filePath = saveFileDialog.FileName;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    return filePath;
                }
            }
            return null;
        }

        /// <summary>
        /// Exports the current log events to a file asynchronously.
        /// </summary>
        /// <remarks>This method exports the log events to a file in the format specified by the selected
        /// export file type. Supported file types include JSON, plain text, and CSV. The file path is determined
        /// dynamically,  and the user may cancel the operation during the file path selection process. If the export
        /// fails,  the returned <see cref="ExportLogResult"/> will contain an error message and, if applicable, an
        /// exception.</remarks>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the export operation.</param>
        /// <returns>An <see cref="ExportLogResult"/> object containing the result of the export operation,  including whether it
        /// was successful, the file path, file type, and any error information.</returns>
        public async Task<ExportLogResult> ExportLogsAsync(CancellationToken cancellationToken = default)
        {
            var output = new ExportLogResult()
            {
                Success = false
            };
            bool skipRestoreExportingLogs = ExportingLogs;
            try
            {
                ExportingLogs = true;

                string? filePath = await DispatchIfNecessaryAsync(GetLogExportFilePathWithName);
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    output.ErrorMessage = "Export cancelled by user.";
                    return output;
                }

                var exportLogs = new List<LogEventArgs>(LogEvents);
                output.FileType = SelectedExportFileType;
                output.FilePath = filePath;
                StringBuilder contents = (SelectedExportFileType?.Extension ?? ".json") switch
                {
                    ".json" => await GetLogsAsJsonTextAsync(exportLogs),
                    ".txt"  => await GetLogsAsTextAsync(exportLogs, LogExportFormat),
                    ".csv"  => await GetLogsAsCSVTextAsync(exportLogs),
                    _       => new StringBuilder()
                };

                await using var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8);
                await writer.WriteAsync(contents.ToString().AsMemory(), cancellationToken: cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                output.ErrorMessage = "Error while exporting logs in LogControlViewModel";
                output.Exception = ex;
                BaseLogger.LogErrorException(_logger, output.ErrorMessage, ex);
            }
            finally
            {
                if (!skipRestoreExportingLogs) ExportingLogs = false;
            }
            return output;
        }

        /// <summary>
        /// Converts a collection of log events into a JSON-formatted string asynchronously.
        /// </summary>
        /// <param name="logEvents">A collection of <see cref="LogEventArgs"/> representing the log events to be serialized. Cannot be <see
        /// langword="null"/>.</param>
        /// <returns>A <see cref="StringBuilder"/> containing the JSON-formatted representation of the log events.</returns>
        private async static Task<StringBuilder> GetLogsAsJsonTextAsync(IEnumerable<LogEventArgs> logEvents)
        {
            ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));

            return await Task.Run(() => new StringBuilder(JsonConvert.SerializeObject(logEvents, Formatting.Indented)));
        }

        /// <summary>
        /// Asynchronously converts a collection of log events into a single text representation.
        /// </summary>
        /// <remarks>This method processes the log events on a background thread to avoid blocking the
        /// calling thread. Each log event is formatted using the <see cref="LogEventArgs.FormatLogMessage"/> method,
        /// with the specified or default format.</remarks>
        /// <param name="logEvents">A collection of <see cref="LogEventArgs"/> instances representing the log events to process. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="logExportFormat">An optional format string used to format each log event. If <see langword="null"/>, a default format is
        /// applied.</param>
        /// <returns>A <see cref="StringBuilder"/> containing the formatted text representation of the provided log events.</returns>
        private async static Task<StringBuilder> GetLogsAsTextAsync(IEnumerable<LogEventArgs> logEvents, string? logExportFormat = null)
        {
            ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));

            return await Task.Run(() =>
            {
                StringBuilder sb = new();
                foreach (var logEvent in logEvents)
                {
                    sb.AppendLine(logEvent.FormatLogMessage(logExportFormat));
                }
                return sb;
            });
        }

        /// <summary>
        /// Converts a collection of log events into a CSV-formatted string asynchronously.
        /// </summary>
        /// <remarks>This method uses the CsvHelper library to serialize the provided log events into CSV
        /// format. The returned <see cref="StringBuilder"/> contains the CSV data, which can be further processed or
        /// saved as needed.</remarks>
        /// <param name="logEvents">The collection of log events to be converted. Cannot be <see langword="null"/>.</param>
        /// <returns>A <see cref="StringBuilder"/> containing the CSV-formatted representation of the log events.</returns>
        private async static Task<StringBuilder> GetLogsAsCSVTextAsync(IEnumerable<LogEventArgs> logEvents)
        {
            ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));

            StringBuilder sb = new();

            await using var writer = new StringWriter(sb);
            await using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<LogEventArgsMap>();
            await csv.WriteRecordsAsync(logEvents);

            return sb;
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