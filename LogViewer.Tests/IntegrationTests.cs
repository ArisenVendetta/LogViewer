using System.Windows.Media;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public void FullPipeline_LogsToSink()
        {
            // Arrange
            var sink = new TestBaseLoggerSink();
            var provider = new BaseLoggerProvider(sink, null);
            provider.SetCategoryColor("MyService", Colors.Blue);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(provider);
            });

            // Act
            var logger = loggerFactory.CreateLogger("MyService");
            logger.LogInformation("Hello, World!");
            logger.LogWarning("Warning message");
            logger.LogError("Error message");

            // Assert
            sink.ReceivedEvents.Should().HaveCount(3);
            sink.ReceivedEvents[0].LogLevel.Should().Be(LogLevel.Information);
            sink.ReceivedEvents[0].LogText.Should().Contain("Hello, World!");
            sink.ReceivedEvents[0].LogColor.Should().Be(Colors.Blue);

            sink.ReceivedEvents[1].LogLevel.Should().Be(LogLevel.Warning);
            sink.ReceivedEvents[2].LogLevel.Should().Be(LogLevel.Error);
        }

        [Fact]
        public void DependencyInjection_ResolvesILoggerCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddLogViewer();
            });
            services.AddTransient<TestService>();

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var service = serviceProvider.GetRequiredService<TestService>();
            service.DoWork();

            // Assert
            var sink = serviceProvider.GetRequiredService<IBaseLoggerSink>();
            sink.LogQueue.Should().NotBeEmpty();
        }

        [Fact]
        public void FullPipeline_WithInnerLogger_WritesToBoth()
        {
            // Arrange
            var testSink = new TestBaseLoggerSink();
            var innerLogs = new List<string>();

            // Create a simple inner factory that captures logs
            var innerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new TestLoggerProvider(innerLogs));
            });

            var provider = new BaseLoggerProvider(testSink, innerFactory);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(provider);
            });

            // Act
            var logger = loggerFactory.CreateLogger("Test");
            logger.LogInformation("Test message");

            // Assert
            testSink.ReceivedEvents.Should().HaveCount(1);
            testSink.ReceivedEvents[0].LogText.Should().Contain("Test message");

            innerLogs.Should().HaveCount(1);
            innerLogs[0].Should().Contain("Test message");
        }

        [Fact]
        public void CategoryColors_DisplayCorrectlyInLogEvent()
        {
            // Arrange
            var sink = new TestBaseLoggerSink();
            var provider = new BaseLoggerProvider(sink, null);
            var colors = new Dictionary<string, Color>
            {
                { "ServiceA", Colors.Red },
                { "ServiceB", Colors.Green },
                { "ServiceC", Colors.Blue }
            };
            provider.SetCategoryColors(colors);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(provider);
            });

            // Act
            loggerFactory.CreateLogger("ServiceA").LogInformation("From A");
            loggerFactory.CreateLogger("ServiceB").LogInformation("From B");
            loggerFactory.CreateLogger("ServiceC").LogInformation("From C");
            loggerFactory.CreateLogger("ServiceD").LogInformation("From D"); // Unknown - should be black

            // Assert
            sink.ReceivedEvents.Should().HaveCount(4);
            sink.ReceivedEvents[0].LogColor.Should().Be(Colors.Red);
            sink.ReceivedEvents[1].LogColor.Should().Be(Colors.Green);
            sink.ReceivedEvents[2].LogColor.Should().Be(Colors.Blue);
            sink.ReceivedEvents[3].LogColor.Should().Be(Colors.Black); // Default
        }

        [Fact]
        public void LogEventArgs_FormatsCorrectly()
        {
            // Arrange
            var logEvent = new LogEventArgs(LogLevel.Information, "TestHandle", "Test message", Colors.Blue)
            {
                LogDateTime = new DateTime(2024, 1, 15, 10, 30, 45, 123)
            };

            // Act
            var formatted = logEvent.ToString();
            var parts = logEvent.GetLogMessageParts();

            // Assert
            formatted.Should().Contain("TestHandle");
            formatted.Should().Contain("Test message");
            parts.LogHandle.Should().Be("TestHandle");
            parts.Body.Should().Be("Test message");
        }

        // Helper class for DI test
        private class TestService
        {
            private readonly ILogger<TestService> _logger;

            public TestService(ILogger<TestService> logger)
            {
                _logger = logger;
            }

            public void DoWork()
            {
                _logger.LogInformation("Doing work...");
            }
        }

        // Simple test logger provider for capturing inner logs
        private class TestLoggerProvider : ILoggerProvider
        {
            private readonly List<string> _logs;

            public TestLoggerProvider(List<string> logs)
            {
                _logs = logs;
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new TestLogger(_logs, categoryName);
            }

            public void Dispose() { }
        }

        private class TestLogger : ILogger
        {
            private readonly List<string> _logs;
            private readonly string _category;

            public TestLogger(List<string> logs, string category)
            {
                _logs = logs;
                _category = category;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _logs.Add($"[{_category}] {formatter(state, exception)}");
            }
        }
    }
}
