using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LogViewer;
using Microsoft.Extensions.Logging;
using PropertyChanged;

namespace LogViewerExample
{
    [AddINotifyPropertyChangedInterface]
    internal class ExampleVM : BaseLogger, IDisposable, IAsyncDisposable
    {
        private bool _disposedValue;
        private List<Task>? _logGenerators;

        public ExampleVM()
        {
            Commands = new ObservableCollection<CustomCommand>()
            {
                new CustomCommand("Generate message for each log level", GenerateEachLogLevelAsync),
                new CustomCommand("Generate random logs continuously", GenerateContinuousLogMessagesAsync),
                new CustomCommand("Stop continuous log generation", StopContinuousLogMessagesAsync),
                new CustomCommand("Generate exception", new Command(GenerateException))
            };
        }

        public ObservableCollection<CustomCommand> Commands { get; protected set; }


        private async Task GenerateEachLogLevelAsync()
        {
            Random random = new Random(Environment.TickCount);
            List<Action<string>> logFunctions = new List<Action<string>>()
            {
                LogCritical,
                LogWarning,
                LogInfo,
                LogDebug,
                LogTrace,
                LogError
            };
            foreach (var logFunction in logFunctions)
            {
                logFunction($"This is an auto-generated log message [{logFunction.Method.Name}, invoked by {nameof(GenerateEachLogLevelAsync)}]");
                await Task.Delay(random.Next(200, 800));
            }
        }

        private CancellationTokenSource? CancellationToken { get; set; }
        private async Task GenerateContinuousLogMessagesAsync()
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

                for (int i = 0; i < 100; i++)
                {
                    Color randomColor = Color.FromArgb(255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                    SomeObject obj = new($"SomeObject{i:D4}", randomColor, logLevels[random.Next(0, logLevels.Length)]);
                    _logGenerators.Add(obj.SomeAction(random, CancellationToken));
                }
            });
        }

        private async Task StopContinuousLogMessagesAsync()
        {
            if (CancellationToken is null) return;
            if (_logGenerators is null || _logGenerators.Count == 0) return;

            CancellationToken?.Cancel();
            await Task.WhenAll(_logGenerators);
        }

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
                }

                Dispose(disposing: false);

                _disposedValue = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
