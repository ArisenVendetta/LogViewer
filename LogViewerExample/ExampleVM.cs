using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Media;
using LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyChanged;

namespace LogViewerExample
{
    /// <summary>
    /// Represents a view model that provides commands for generating and managing log messages,
    /// including support for continuous log generation and exception handling.
    /// </summary>
    /// <remarks>
    /// This example demonstrates both the new DI-based ILogger pattern and the legacy BaseLogger inheritance.
    /// In production code, prefer using ILogger&lt;T&gt; via dependency injection.
    /// </remarks>
    [AddINotifyPropertyChangedInterface]
    internal class ExampleVM : IDisposable, IAsyncDisposable
    {
        private bool _disposedValue;
        private List<Task>? _logGenerators;

        private readonly ILogger<ExampleVM> _logger;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleVM"/> class.
        /// Uses the new DI-based logging pattern with ILogger&lt;T&gt;.
        /// </summary>
        /// <param name="logger">The logger for this view model.</param>
        /// <param name="loggerFactory">The logger factory for creating additional loggers.</param>
        public ExampleVM(ILogger<ExampleVM> logger, ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));

            _logger = logger;
            _loggerFactory = loggerFactory;

            Commands = [
                new CustomCommand("Generate message for each log level", GenerateEachLogLevelAsync),
                new CustomCommand("Generate random logs continuously", GenerateContinuousLogMessagesAsync),
                new CustomCommand("Stop continuous log generation", StopContinuousLogMessagesAsync),
                new CustomCommand("Generate exception", new Command(GenerateException))
            ];

            _logger.LogInformation("Initialized {ClassName} with {CommandCount} commands.", nameof(ExampleVM), Commands.Count);
        }

        /// <summary>
        /// Gets the collection of commands associated with this instance.
        /// </summary>
        public ObservableCollection<CustomCommand> Commands { get; protected set; }

        /// <summary>
        /// Generates and logs a message for each log level asynchronously.
        /// </summary>
        private async Task GenerateEachLogLevelAsync()
        {
            Random random = new(Environment.TickCount);
            List<Action<string>> logFunctions = [
                msg => _logger.LogCritical("{Message} [LogCritical]", msg),
                msg => _logger.LogError("{Message} [LogError]", msg),
                msg => _logger.LogWarning("{Message} [LogWarning]", msg),
                msg => _logger.LogInformation("{Message} [LogInformation]", msg),
                msg => _logger.LogDebug("{Message} [LogDebug]", msg),
                msg => _logger.LogTrace("{Message} [LogTrace]", msg)
            ];

            _logger.LogInformation("Starting {MethodName} with {Count} log functions.",
                nameof(GenerateEachLogLevelAsync), logFunctions.Count);

            foreach (var logFunction in logFunctions)
            {
                logFunction($"This is an auto-generated log message [invoked by {nameof(GenerateEachLogLevelAsync)}]");
                await Task.Delay(random.Next(200, 800));
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CancellationTokenSource"/> used to signal cancellation for ongoing operations.
        /// </summary>
        private CancellationTokenSource? CancellationToken { get; set; }

        /// <summary>
        /// Asynchronously generates a continuous stream of log messages with randomized properties.
        /// </summary>
        private async Task GenerateContinuousLogMessagesAsync()
        {
            try
            {
                await StopContinuousLogMessagesAsync();

                await Task.Run(() =>
                {
                    Random random = new(Environment.TickCount);
                    _logGenerators = [];
                    CancellationToken = new CancellationTokenSource();
                    LogLevel[] logLevels =
                    [
                        LogLevel.Trace,
                        LogLevel.Debug,
                        LogLevel.Information,
                        LogLevel.Warning,
                        LogLevel.Error,
                        LogLevel.Critical
                    ];

                    _logger.LogInformation("Starting {MethodName} with {Count} log levels.",
                        nameof(GenerateContinuousLogMessagesAsync), logLevels.Length);

                    for (int i = 0; i < 300; i++)
                    {
                        Color randomColor = Color.FromArgb(255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                        SomeObject obj = new($"SomeObject{i:D4}", randomColor, logLevels[random.Next(0, logLevels.Length)]);
                        _logGenerators.Add(obj.SomeAction(random, CancellationToken));
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName}", nameof(GenerateContinuousLogMessagesAsync));
            }
        }

        /// <summary>
        /// Stops the continuous generation of log messages asynchronously.
        /// </summary>
        private async Task StopContinuousLogMessagesAsync()
        {
            if (CancellationToken is null) return;
            if (_logGenerators is null || _logGenerators.Count == 0) return;

            _logger.LogInformation("Stopping continuous log messages, cancelling and waiting for {Count} log generators to complete.",
                _logGenerators.Count);

            CancellationToken?.Cancel();
            await Task.WhenAll(_logGenerators);
        }

        /// <summary>
        /// Simulates the generation of a nested exception and logs it.
        /// </summary>
        private void GenerateException()
        {
            try
            {
                throw new Exception("This is the top level exception",
                    new Exception("This is the second level exception",
                        new Exception("This is the lowest level exception")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generated test exception");
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    CancellationToken?.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposedValue)
            {
                try
                {
                    await StopContinuousLogMessagesAsync();
                }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception
                catch (Exception)
                {
                    // swallow it, this is an example application and it's closing
                }
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception

                Dispose(disposing: false);
                _disposedValue = true;
                GC.SuppressFinalize(this);
            }
        }
        #endregion
    }
}
