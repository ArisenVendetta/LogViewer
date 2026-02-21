using System.Windows.Media;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    public class BaseLoggerLoggingBuilderExtensionsTests
    {
        [Fact]
        public void AddLogViewer_RegistersProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer());

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");

            // Assert
            logger.Should().NotBeNull();
        }

        [Fact]
        public void AddLogViewer_RegistersSink()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer());

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var sink = serviceProvider.GetService<IBaseLoggerSink>();

            // Assert
            sink.Should().NotBeNull();
            sink.Should().BeSameAs(BaseLoggerSink.Instance);
        }

        [Fact]
        public void AddLogViewer_WithOptions_AppliesMaxQueueSize()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer(options =>
            {
                options.MaxQueueSize = 5000;
            }));

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var sink = serviceProvider.GetRequiredService<IBaseLoggerSink>();

            // Assert
            sink.MaxQueueSize.Should().Be(5000);
        }

        [Fact]
        public void AddLogViewer_WithOptions_AppliesMinimumLevel()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer(options =>
            {
                options.MinimumLevel = LogLevel.Warning;
            }));

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");

            // Assert
            // Trace level should not be enabled when minimum is Warning
            logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
            logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        }

        [Fact]
        public void AddLogViewer_WithCategoryColors_SetsColors()
        {
            // Arrange - use a TestBaseLoggerSink to isolate this test
            var testSink = new TestBaseLoggerSink();
            var provider = new BaseLoggerProvider(testSink, null);
            provider.SetCategoryColor("ColorTestCategory", Colors.Purple);

            // Act
            var logger = provider.CreateLogger("ColorTestCategory");
            logger.LogInformation("Test message");

            // Assert
            testSink.ReceivedEvents.Should().HaveCount(1);
            testSink.ReceivedEvents[0].LogColor.Should().Be(Colors.Purple);
        }

        [Fact]
        public void AddLogViewer_WithDateTimeFormat_AppliesFormat()
        {
            // Arrange
            var services = new ServiceCollection();
            var customFormat = "HH:mm:ss";
            services.AddLogging(builder => builder.AddLogViewer(options =>
            {
                options.DateTimeFormat = customFormat;
            }));

            // Act
            services.BuildServiceProvider();

            // Assert
            BaseLogger.LogDateTimeFormat.Should().Be(customFormat);

            // Cleanup - restore default
            BaseLogger.LogDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff (zzz)";
        }

        [Fact]
        public void AddLogViewer_WithUtcTime_AppliesUtcSetting()
        {
            // Arrange
            var originalValue = BaseLogger.LogUTCTime;
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer(options =>
            {
                options.UseUtcTime = true;
            }));

            // Act
            services.BuildServiceProvider();

            // Assert
            BaseLogger.LogUTCTime.Should().BeTrue();

            // Cleanup
            BaseLogger.LogUTCTime = originalValue;
        }

        [Fact]
        public void AddLogViewer_WithNullBuilder_Throws()
        {
            // Arrange
            ILoggingBuilder? builder = null;

            // Act
            var act = () => builder!.AddLogViewer();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddLogViewer_WithNullConfigure_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddLogging(builder => builder.AddLogViewer((Action<BaseLoggerProviderOptions>)null!));

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
