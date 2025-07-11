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
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;

namespace LogViewerExample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string nlogConfigPath = Path.Combine(Path.GetDirectoryName(Configuration.ConfigPath) ?? ".", "nlog.config");
            LogManager.ThrowConfigExceptions = true;
            LogManager.Configuration = new XmlLoggingConfiguration(nlogConfigPath);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddNLog();
            });

            BaseLogger.Initialize(loggerFactory);

            var serviceCollection = new ServiceCollection();

            var baseLoggerProvider = new BaseLoggerProvider(Microsoft.Extensions.Logging.LogLevel.Information);
            Dictionary<string, Color> colorMap = [];
            baseLoggerProvider.SetCategoryColor(colorMap);

            serviceCollection.AddSingleton(baseLoggerProvider);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                ServiceProvider?.GetRequiredService<ILoggerProvider>()?.Dispose();
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
