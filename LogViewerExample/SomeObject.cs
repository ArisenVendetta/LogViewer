using System.Windows.Media;
using LogViewer;
using Microsoft.Extensions.Logging;

namespace LogViewerExample
{
    public class SomeObject : BaseLogger
    {
        private Action<string> LogMethod { get; set; }

        public SomeObject(string name, LogLevel desiredLogLevel, Color logColour) : base(name, logColour, desiredLogLevel)
        {
            LogMethod = LogLevel switch
            {
                LogLevel.Trace       => LogTrace,
                LogLevel.Debug       => LogDebug,
                LogLevel.Information => LogInfo,
                LogLevel.Warning     => LogWarning,
                LogLevel.Error       => LogError,
                LogLevel.Critical    => LogCritical,
                LogLevel.None        => LogTrace,
                _                    => LogInfo
            };
        }
        public SomeObject(string name, LogLevel desiredLogLevel = LogLevel.Information) : this(name, desiredLogLevel, Colors.Black)
        {

        }

        public async Task SomeAction(Random random, CancellationTokenSource cts)
        {
            if (random is null)
            {
                LogError($"Random generator is null, exiting {nameof(SomeAction)}");
                return;
            }
            if (cts is null)
            {
                LogError($"Cancellation token is null, will be unable to stop once started, exiting {nameof(SomeAction)}");
                return;
            }
            while (cts.Token.IsCancellationRequested == false)
            {
                LogMethod($"This is an auto-generated log message [{LogMethod.Method.Name}, invoked by {nameof(SomeAction)}]");
                await Task.Delay(random.Next(150, 750));
            }
        }
    }
}