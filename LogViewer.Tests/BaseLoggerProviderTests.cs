using System.Windows.Media;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    public class BaseLoggerProviderTests
    {
        private readonly TestBaseLoggerSink _sink = new();

        [Fact]
        public void CreateLogger_ReturnsLogViewerLogger()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var logger = provider.CreateLogger("TestCategory");

            // Assert
            logger.Should().BeOfType<BaseLogger>();
        }

        [Fact]
        public void CreateLogger_CachesLoggersByCategory()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var logger1 = provider.CreateLogger("TestCategory");
            var logger2 = provider.CreateLogger("TestCategory");

            // Assert
            logger1.Should().BeSameAs(logger2);
        }

        [Fact]
        public void CreateLogger_DifferentCategoriesReturnDifferentLoggers()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var logger1 = provider.CreateLogger("Category1");
            var logger2 = provider.CreateLogger("Category2");

            // Assert
            logger1.Should().NotBeSameAs(logger2);
        }

        [Fact]
        public void SetCategoryColor_UpdatesColorMapping()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);
            provider.SetCategoryColor("TestCategory", Colors.Blue);

            // Act
            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Test");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogColor.Should().Be(Colors.Blue);
        }

        [Fact]
        public void SetCategoryColor_Generic_UsesTypeName()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);
            provider.SetCategoryColor<BaseLoggerProviderTests>(Colors.Green);

            // Act
            var logger = provider.CreateLogger(nameof(BaseLoggerProviderTests));
            logger.LogInformation("Test");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogColor.Should().Be(Colors.Green);
        }

        [Fact]
        public void SetCategoryColors_SetsMultipleColors()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);
            var colors = new Dictionary<string, Color>
            {
                { "Category1", Colors.Red },
                { "Category2", Colors.Blue }
            };

            // Act
            provider.SetCategoryColors(colors);
            var logger1 = provider.CreateLogger("Category1");
            var logger2 = provider.CreateLogger("Category2");
            logger1.LogInformation("Test1");
            logger2.LogInformation("Test2");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(2);
            _sink.ReceivedEvents[0].LogColor.Should().Be(Colors.Red);
            _sink.ReceivedEvents[1].LogColor.Should().Be(Colors.Blue);
        }

        [Fact]
        public void DefaultColor_IsBlack()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var logger = provider.CreateLogger("UnknownCategory");
            logger.LogInformation("Test");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogColor.Should().Be(Colors.Black);
        }

        [Fact]
        public void MinimumLevel_DefaultsToTrace()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Assert
            provider.MinimumLevel.Should().Be(LogLevel.Trace);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var act = () =>
            {
                provider.Dispose();
                provider.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SetCategoryColor_WithNullOrEmpty_Throws()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act & Assert
            var act1 = () => provider.SetCategoryColor(null!, Colors.Blue);
            var act2 = () => provider.SetCategoryColor("", Colors.Blue);
            var act3 = () => provider.SetCategoryColor("   ", Colors.Blue);

            act1.Should().Throw<ArgumentException>();
            act2.Should().Throw<ArgumentException>();
            act3.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithInnerFactory_CreatesWrappedLoggers()
        {
            // Arrange
            var innerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace));
            var provider = new BaseLoggerProvider(_sink, innerFactory);

            // Act
            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Test message");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
        }
    }
}
