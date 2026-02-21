using System.Windows.Media;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    public class BaseLoggerSinkTests
    {
        [Fact]
        public void Write_EnqueuesLogEvent()
        {
            // Arrange
            var sink = BaseLoggerSink.CreateForTesting();
            var logEvent = new LogEventArgs(LogLevel.Information, "Test", "Test message", Colors.Black)
            {
                LogDateTime = DateTime.Now
            };

            // Act
            sink.Write(logEvent);

            // Assert
            sink.LogQueue.Should().HaveCount(1);
            sink.LogQueue.TryPeek(out var result).Should().BeTrue();
            result.Should().Be(logEvent);
        }

        [Fact]
        public async Task Write_RaisesLogReceivedEvent()
        {
            // Arrange
            var sink = BaseLoggerSink.CreateForTesting();
            var logEvent = new LogEventArgs(LogLevel.Information, "Test", "Test message", Colors.Black)
            {
                LogDateTime = DateTime.Now
            };
            LogEventArgs? receivedEvent = null;
            sink.LogReceived += (sender, e) =>
            {
                receivedEvent = e;
                return Task.CompletedTask;
            };

            // Act
            sink.Write(logEvent);

            // Assert - give async event time to fire
            await Task.Delay(100);
            receivedEvent.Should().Be(logEvent);
        }

        [Fact]
        public void Write_TrimsQueueWhenExceedsMaxSize()
        {
            // Arrange
            var sink = BaseLoggerSink.CreateForTesting();
            sink.MaxQueueSize = 10;

            // Act - add more than max size
            for (int i = 0; i < 20; i++)
            {
                var logEvent = new LogEventArgs(LogLevel.Information, "Test", $"Message {i}", Colors.Black)
                {
                    LogDateTime = DateTime.Now
                };
                sink.Write(logEvent);
            }

            // Assert - queue should be trimmed
            sink.LogQueue.Count.Should().BeLessThanOrEqualTo(10);
        }

        [Fact]
        public void MaxQueueSize_CanBeChanged()
        {
            // Arrange
            var sink = BaseLoggerSink.CreateForTesting();

            // Act
            sink.MaxQueueSize = 5000;

            // Assert
            sink.MaxQueueSize.Should().Be(5000);
        }

        [Fact]
        public void Write_WithNullEvent_DoesNotThrow()
        {
            // Arrange
            var sink = BaseLoggerSink.CreateForTesting();

            // Act
            var act = () => sink.Write(null!);

            // Assert
            act.Should().NotThrow();
            sink.LogQueue.Should().BeEmpty();
        }

        [Fact]
        public void Instance_ReturnsSameSingleton()
        {
            // Act
            var instance1 = BaseLoggerSink.Instance;
            var instance2 = BaseLoggerSink.Instance;

            // Assert
            instance1.Should().BeSameAs(instance2);
        }

        [Fact]
        public async Task Write_ConcurrentAccess_DoesNotThrow()
        {
            // Arrange
            var sink = BaseLoggerSink.CreateForTesting();
            sink.MaxQueueSize = 100;
            var tasks = new List<Task>();

            // Act - simulate concurrent logging from multiple threads
            for (int i = 0; i < 10; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var logEvent = new LogEventArgs(
                            LogLevel.Information,
                            $"Thread{threadId}",
                            $"Message {j}",
                            Colors.Black)
                        {
                            LogDateTime = DateTime.Now
                        };
                        sink.Write(logEvent);
                    }
                }));
            }

            // Assert - should complete without throwing
            await Task.WhenAll(tasks);
            sink.LogQueue.Should().NotBeEmpty();
        }
    }
}
