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
    internal class ExampleVM : BaseLogger
    {
        public ExampleVM()
        {
            Commands = new ObservableCollection<CustomCommand>()
            {
                new CustomCommand("Generate message for each log level", GenerateEachLogLevelAsync),
                new CustomCommand("Generate random logs continuously", GenerateContinuousLogMessagesAsync),
                new CustomCommand("Stop continuous log generation", new Command(StopContinuousLogMessages)),
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
            await Task.Run(async () =>
            {
                Random random = new Random(Environment.TickCount);
                List<Task> logGenerators = new List<Task>();
                CancellationToken = new CancellationTokenSource();
                LogLevel[] logLevels = new LogLevel[]
                {
                    LogLevel.Trace,
                    LogLevel.Debug,
                    LogLevel.Information,
                    LogLevel.Warning,
                    LogLevel.Error,
                    LogLevel.Critical,
                    LogLevel.None
                };

                for (int i = 0; i < 100; i++)
                {
                    Color randomColor = Color.FromArgb(255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                    SomeObject obj = new SomeObject($"SomeObject{i:D4}", logLevels[random.Next(0, logLevels.Length)], randomColor);
                    logGenerators.Add(obj.SomeAction(random, CancellationToken));
                }

                await Task.WhenAll(logGenerators);
            });
        }

        private void StopContinuousLogMessages() => CancellationToken?.Cancel();

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
    }
}
