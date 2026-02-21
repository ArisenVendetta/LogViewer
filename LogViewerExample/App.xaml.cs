using System.IO;
using System.Windows;
using System.Windows.Media;
using LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;

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
            // Configure NLog
            string nlogConfigPath = Path.Combine(Path.GetDirectoryName(Configuration.ConfigPath) ?? ".", "nlog.config");
            LogManager.ThrowConfigExceptions = true;
            LogManager.Configuration = new XmlLoggingConfiguration(nlogConfigPath);

            // Set up dependency injection with the new AddLogViewer pattern
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);

                // Add NLog for file logging
                builder.AddNLog();

                // Add LogViewer for real-time UI logging with colors
                builder.AddLogViewer(options =>
                {
                    options.MinimumLevel = Microsoft.Extensions.Logging.LogLevel.Trace;
                    options.MaxQueueSize = 10000;

                    // Configure category colors for different services
                    options.CategoryColors["ExampleVM"] = Colors.DodgerBlue;
                    options.CategoryColors["SomeObject"] = Colors.MediumPurple;
                    options.CategoryColors["MainWindow"] = Colors.OrangeRed;
                });
            });

            // Register transient services that use ILogger<T>
            services.AddTransient<ExampleVM>();

            ServiceProvider = services.BuildServiceProvider();
            ServiceProvider.AttachLoggerFactoryToLogViewer();

            // Initialize BaseLogger for classes that extend it (like SomeObject)
            var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
            BaseLogger.Initialize(loggerFactory);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                // Dispose providers via DI
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                LogManager.Shutdown();
            }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception
            catch (Exception)
            {
                // swallow it, this is an example application and it's closing
            }
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception
        }
    }

    public static class Configuration
    {
        public static string ConfigPath { get; set; } = @"Config\config.json";
    }
}
