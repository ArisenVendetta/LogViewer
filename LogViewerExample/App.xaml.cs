using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows;
using LogViewer;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Config;

namespace LogViewerExample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ILoggerFactory? _loggerFactory = null;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string nlogConfigPath = Path.Combine(Path.GetDirectoryName(Configuration.ConfigPath) ?? ".", "nlog.config");
            LogManager.ThrowConfigExceptions = true;
            LogManager.Configuration = new XmlLoggingConfiguration(nlogConfigPath);

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddNLog();
            });

            BaseLogger.Initialize(_loggerFactory);
            BaseLogger.LogUTCTime = false;
            BaseLogger.LogDateTimeFormat = "o";
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                _loggerFactory?.Dispose();
                LogManager.Shutdown();
            }
            catch (Exception)
            {
                // swallow it, this is an example application and it's closing
            }
        }
    }

    public static class Configuration
    {
        public static string ConfigPath { get; set; } = @"Config\config.json";
    }
}
