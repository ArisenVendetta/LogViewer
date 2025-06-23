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
            BaseLogger.MaxLogQueueSize = 5000;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            LogManager.Shutdown();
            _loggerFactory?.Dispose();
        }
    }

    public static class Configuration
    {
        public static string ConfigPath { get; set; } = @"Config\config.json";
    }
}
