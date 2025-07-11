using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyChanged;

namespace LogViewerExample
{
    /// <summary>
    /// Represents a view model that provides commands for generating and managing log messages,  including support for
    /// continuous log generation and exception handling.
    /// </summary>
    /// <remarks>This class implements <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> to ensure
    /// proper  cleanup of resources, such as stopping continuous log generation tasks. It also extends  <see
    /// cref="BaseLogger"/>, enabling logging functionality for various log levels.</remarks>
    [AddINotifyPropertyChangedInterface]
    internal class ExampleVM : BaseLogger, IDisposable, IAsyncDisposable
    {
        private bool _disposedValue;
        private List<Task>? _logGenerators;

        private IServiceProvider _serviceProvider;
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleVM"/> class and sets up the available commands.
        /// </summary>
        /// <remarks>This constructor initializes the <see cref="Commands"/> collection with a predefined
        /// set of commands that can be executed by the view model. Each command represents a specific action, such as
        /// generating log messages or handling exceptions.</remarks>
        public ExampleVM(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<BaseLoggerProvider>().CreateLogger("SomeRandomLogger");

            Commands = [
                new CustomCommand("Generate message for each log level", GenerateEachLogLevelAsync),
                new CustomCommand("Generate random logs continuously", GenerateContinuousLogMessagesAsync),
                new CustomCommand("Stop continuous log generation", StopContinuousLogMessagesAsync),
                new CustomCommand("Generate exception", new Command(GenerateException))
            ];
            _logger.LogInformation($"Initialized {nameof(ExampleVM)} with {Commands.Count} commands.");
        }

        /// <summary>
        /// Gets the collection of commands associated with this instance.
        /// </summary>
        /// <remarks>The collection can be used to add, remove, or enumerate commands. Changes to the
        /// collection will automatically notify any observers, as it is an <see
        /// cref="ObservableCollection{T}"/>.</remarks>
        public ObservableCollection<CustomCommand> Commands { get; protected set; }

        /// <summary>
        /// Generates and logs a message for each log level asynchronously.
        /// </summary>
        /// <remarks>This method iterates through all available log levels (e.g., Critical, Warning,
        /// Information, Debug, Trace, and Error), invoking the corresponding logging function for each level. A random
        /// delay is introduced between log messages to simulate asynchronous behavior.</remarks>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        private async Task GenerateEachLogLevelAsync()
        {
            Random random = new(Environment.TickCount);
            List<Action<string>> logFunctions = [
                LogCritical,
                LogWarning,
                LogInformation,
                LogDebug,
                LogTrace,
                LogError
            ];
            _logger.LogInformation($"Starting {nameof(GenerateEachLogLevelAsync)} with {logFunctions.Count} log functions.");
            foreach (var logFunction in logFunctions)
            {
                logFunction($"This is an auto-generated log message [{logFunction.Method.Name}, invoked by {nameof(GenerateEachLogLevelAsync)}]");
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
        /// <remarks>This method stops any previously running log generation process before starting a new
        /// one.  It creates multiple log generators, each producing log messages with random colors and log levels. The
        /// method runs the log generation process on a background thread and supports cancellation.</remarks>
        /// <returns></returns>
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
                    {
                        LogLevel.Trace,
                        LogLevel.Debug,
                        LogLevel.Information,
                        LogLevel.Warning,
                        LogLevel.Error,
                        LogLevel.Critical
                    };
                    _logger.LogInformation($"Starting {nameof(GenerateContinuousLogMessagesAsync)} with {logLevels.Length} log levels.");
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
                LogException(ex, $"Error in {nameof(GenerateContinuousLogMessagesAsync)}");
            }
        }

        /// <summary>
        /// Stops the continuous generation of log messages asynchronously.
        /// </summary>
        /// <remarks>This method cancels any ongoing log generation tasks and waits for their completion.
        /// It has no effect if there are no active log generators or if the cancellation token is null.</remarks>
        /// <returns>A task that represents the asynchronous operation of stopping the log generators.</returns>
        private async Task StopContinuousLogMessagesAsync()
        {
            if (CancellationToken is null) return;
            if (_logGenerators is null || _logGenerators.Count == 0) return;
            _logger.LogInformation($"Stopping continuous log messages, cancelling and waiting for {_logGenerators.Count} log generators to complete.");
            CancellationToken?.Cancel();
            await Task.WhenAll(_logGenerators);
        }

        /// <summary>
        /// Simulates the generation of a nested exception and logs it.
        /// </summary>
        /// <remarks>This method creates a top-level exception with multiple inner exceptions for
        /// demonstration or testing purposes. The exception is caught and logged using the <see cref="LogException"/>
        /// method.</remarks>
        private void GenerateException()
        {
            try
            {
                throw new Exception("This is the top level exception", new Exception("This is the second level exception", new Exception("This is the lowest level exception")));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }

                _disposedValue = true;
            }
        }

        // ~ExampleVM()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

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
                catch (Exception)
                {
                    // swallow it, this is an example application and it's closing
                    ; // this is just to get rid of the compiler warning
                }

                Dispose(disposing: false);

                _disposedValue = true;
                GC.SuppressFinalize(this);
            }
        }
        #endregion
    }
}
