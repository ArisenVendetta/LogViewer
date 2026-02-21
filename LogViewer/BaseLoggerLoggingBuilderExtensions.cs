using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Extension methods for configuring LogViewer with <see cref="ILoggingBuilder"/> and <see cref="IServiceProvider"/>.
    /// </summary>
    public static class BaseLoggerLoggingBuilderExtensions
    {
        /// <summary>
        /// Attaches the resolved <see cref="ILoggerFactory"/> to LogViewer's sink, enabling internal logging
        /// for <see cref="LogControlViewModel"/>. Call this after <see cref="ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(IServiceCollection)"/>.
        /// </summary>
        /// <param name="provider">The service provider.</param>
        /// <returns>The service provider for chaining.</returns>
        public static IServiceProvider AttachLoggerFactoryToLogViewer(this IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            BaseLoggerSink.Instance.LoggerFactory = provider.GetService<ILoggerFactory>();
            return provider;
        }

        /// <summary>
        /// Adds LogViewer as a logging provider with default settings.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <returns>The logging builder for chaining.</returns>
        public static ILoggingBuilder AddLogViewer(this ILoggingBuilder builder)
            => AddLogViewerCore(builder, null, _ => { });

        /// <summary>
        /// Adds LogViewer as a logging provider with configuration options.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="configure">An action to configure <see cref="BaseLoggerProviderOptions"/>.</param>
        /// <returns>The logging builder for chaining.</returns>
        public static ILoggingBuilder AddLogViewer(
            this ILoggingBuilder builder,
            Action<BaseLoggerProviderOptions> configure)
            => AddLogViewerCore(builder, null, configure);

        /// <summary>
        /// Adds LogViewer as a logging provider that wraps an existing <see cref="ILoggerFactory"/>.
        /// Logs will be sent to both LogViewer and the wrapped factory.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="innerFactory">The factory to wrap (e.g., an NLog or Serilog factory).</param>
        /// <returns>The logging builder for chaining.</returns>
        public static ILoggingBuilder AddLogViewer(
            this ILoggingBuilder builder,
            ILoggerFactory innerFactory)
            => AddLogViewerCore(builder, innerFactory, _ => { });

        /// <summary>
        /// Adds LogViewer as a logging provider that wraps an existing <see cref="ILoggerFactory"/>
        /// with configuration options.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="innerFactory">The factory to wrap (e.g., an NLog or Serilog factory).</param>
        /// <param name="configure">An action to configure <see cref="BaseLoggerProviderOptions"/>.</param>
        /// <returns>The logging builder for chaining.</returns>
        public static ILoggingBuilder AddLogViewer(
            this ILoggingBuilder builder,
            ILoggerFactory innerFactory,
            Action<BaseLoggerProviderOptions> configure)
            => AddLogViewerCore(builder, innerFactory, configure);

        private static ILoggingBuilder AddLogViewerCore(
            ILoggingBuilder builder,
            ILoggerFactory? innerFactory,
            Action<BaseLoggerProviderOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new BaseLoggerProviderOptions();
            configure(options);

            // Apply global settings
            BaseLogger.LogDateTimeFormat = options.DateTimeFormat;
            BaseLogger.LogUTCTime = options.UseUtcTime;

            // Configure the singleton sink
            var sink = BaseLoggerSink.Instance;
            sink.MaxQueueSize = options.MaxQueueSize;

            // Register sink as singleton
            builder.Services.TryAddSingleton<IBaseLoggerSink>(sink);

            // Create and configure the provider
            var provider = new BaseLoggerProvider(sink, innerFactory)
            {
                MinimumLevel = options.MinimumLevel,
                StripNamespaceFromCategory = options.StripNamespaceFromCategory
            };

            // Apply category colors
            provider.SetCategoryColors(options.CategoryColors);

            // Register provider
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider>(provider));

            return builder;
        }
    }
}
