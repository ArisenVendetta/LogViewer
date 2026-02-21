using System.Windows.Media;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LogViewer.Tests
{
    public class BaseLoggerTests
    {
        private readonly TestBaseLoggerSink _sink = new();

        [Fact]
        public void Log_WritesToSink()
        {
            // Arrange
            var provider = CreateProvider();
            var logger = new BaseLogger("TestCategory", Colors.Blue, _sink, null, provider);

            // Act
            logger.LogInformation("Test message");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogHandle.Should().Be("TestCategory");
            _sink.ReceivedEvents[0].LogText.Should().Contain("Test message");
            _sink.ReceivedEvents[0].LogColor.Should().Be(Colors.Blue);
        }

        [Fact]
        public void Log_PassesThroughToInnerLogger()
        {
            // Arrange
            var innerLogger = new Mock<ILogger>();
            innerLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var provider = CreateProvider();
            var logger = new BaseLogger("TestCategory", Colors.Black, _sink, innerLogger.Object, provider);

            // Act
            logger.LogWarning("Warning message");

            // Assert
            innerLogger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Log_WithException_IncludesExceptionInMessage()
        {
            // Arrange
            var provider = CreateProvider();
            var logger = new BaseLogger("TestCategory", Colors.Red, _sink, null, provider);
            var exception = new InvalidOperationException("Test exception");

            // Act
            logger.LogError(exception, "Error occurred");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogText.Should().Contain("Error occurred");
            _sink.ReceivedEvents[0].LogText.Should().Contain("Test exception");
        }

        [Fact]
        public void BeginScope_ReturnsDisposable()
        {
            // Arrange
            var provider = CreateProvider();
            var logger = new BaseLogger("Test", Colors.Black, _sink, null, provider);

            // Act
            var scope = logger.BeginScope("test scope");

            // Assert
            scope.Should().BeNull(); // No inner logger, so null
        }

        [Fact]
        public void BeginScope_DelegatesToInnerLogger()
        {
            // Arrange
            var innerLogger = new Mock<ILogger>();
            var mockDisposable = new Mock<IDisposable>();
            innerLogger.Setup(l => l.BeginScope(It.IsAny<string>())).Returns(mockDisposable.Object);
            var provider = CreateProvider();
            var logger = new BaseLogger("Test", Colors.Black, _sink, innerLogger.Object, provider);

            // Act
            var scope = logger.BeginScope("test scope");

            // Assert
            scope.Should().NotBeNull();
            innerLogger.Verify(l => l.BeginScope("test scope"), Times.Once);
        }

        [Fact]
        public void Log_SetsCorrectLogLevel()
        {
            // Arrange
            var provider = CreateProvider();
            var logger = new BaseLogger("TestCategory", Colors.Black, _sink, null, provider);

            // Act
            logger.LogWarning("Warning message");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogLevel.Should().Be(LogLevel.Warning);
        }

        [Fact]
        public void Log_SetsTimestamp()
        {
            // Arrange
            var provider = CreateProvider();
            var logger = new BaseLogger("TestCategory", Colors.Black, _sink, null, provider);
            var beforeLog = DateTime.Now;

            // Act
            logger.LogInformation("Test");

            // Assert
            var afterLog = DateTime.Now;
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogDateTime.Should().BeOnOrAfter(beforeLog);
            _sink.ReceivedEvents[0].LogDateTime.Should().BeOnOrBefore(afterLog);
        }

        private BaseLoggerProvider CreateProvider() => new(_sink, null);
    }
}
