using WabbitBot.Common.Events;
using WabbitBot.Common.Events.Interfaces;
using Xunit;

namespace WabbitBot.Common.Tests;

/// <summary>
/// Tests for two-phase event handler execution (Write handlers before Read handlers).
/// </summary>
public class EventBusTwoPhaseTests
{
    private class TestEvent : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    [Fact]
    public async Task PublishAsync_ExecutesWriteHandlersBeforeReadHandlers()
    {
        // Arrange
        var eventBus = new GlobalEventBus();
        var executionOrder = new List<string>();
        var writeCompleted = new TaskCompletionSource<bool>();

        // Register Write handler
        eventBus.Subscribe<TestEvent>(
            async evt =>
            {
                executionOrder.Add("Write");
                await Task.Delay(50); // Simulate work
                writeCompleted.SetResult(true);
            },
            HandlerType.Write
        );

        // Register Read handler that checks if Write completed
        eventBus.Subscribe<TestEvent>(
            async evt =>
            {
                // Read handler should only execute after Write handler completes
                Assert.True(writeCompleted.Task.IsCompleted, "Write handler must complete before Read handler starts");
                executionOrder.Add("Read");
                await Task.CompletedTask;
            },
            HandlerType.Read
        );

        // Act
        await eventBus.PublishAsync(new TestEvent());

        // Assert
        Assert.Equal(2, executionOrder.Count);
        Assert.Equal("Write", executionOrder[0]);
        Assert.Equal("Read", executionOrder[1]);
    }

    [Fact]
    public async Task PublishAsync_MultipleWriteHandlersExecuteConcurrently()
    {
        // Arrange
        var eventBus = new GlobalEventBus();
        var concurrentExecution = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        // Register multiple Write handlers
        for (int i = 0; i < 3; i++)
        {
            eventBus.Subscribe<TestEvent>(
                async evt =>
                {
                    lock (lockObj)
                    {
                        concurrentExecution++;
                        maxConcurrent = Math.Max(maxConcurrent, concurrentExecution);
                    }

                    await Task.Delay(50);

                    lock (lockObj)
                    {
                        concurrentExecution--;
                    }
                },
                HandlerType.Write
            );
        }

        // Act
        await eventBus.PublishAsync(new TestEvent());

        // Assert - Write handlers should have executed concurrently
        Assert.True(maxConcurrent > 1, $"Expected concurrent execution, but maxConcurrent was {maxConcurrent}");
    }

    [Fact]
    public async Task PublishAsync_MultipleReadHandlersExecuteConcurrently()
    {
        // Arrange
        var eventBus = new GlobalEventBus();
        var concurrentExecution = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        // Register multiple Read handlers
        for (int i = 0; i < 3; i++)
        {
            eventBus.Subscribe<TestEvent>(
                async evt =>
                {
                    lock (lockObj)
                    {
                        concurrentExecution++;
                        maxConcurrent = Math.Max(maxConcurrent, concurrentExecution);
                    }

                    await Task.Delay(50);

                    lock (lockObj)
                    {
                        concurrentExecution--;
                    }
                },
                HandlerType.Read
            );
        }

        // Act
        await eventBus.PublishAsync(new TestEvent());

        // Assert - Read handlers should have executed concurrently
        Assert.True(maxConcurrent > 1, $"Expected concurrent execution, but maxConcurrent was {maxConcurrent}");
    }

    [Fact]
    public async Task PublishAsync_HandlerExceptionDoesNotBlockOtherPhase()
    {
        // Arrange
        var eventBus = new GlobalEventBus();
        var readHandlerExecuted = false;

        // Register Write handler that throws
        eventBus.Subscribe<TestEvent>(
            async evt =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Write handler error");
            },
            HandlerType.Write
        );

        // Register Read handler
        eventBus.Subscribe<TestEvent>(
            async evt =>
            {
                readHandlerExecuted = true;
                await Task.CompletedTask;
            },
            HandlerType.Read
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await eventBus.PublishAsync(new TestEvent());
        });

        // Read handler should not have executed because Write phase threw
        Assert.False(readHandlerExecuted, "Read handler should not execute when Write phase throws");
    }

    [Fact]
    public async Task PublishAsync_EmptyHandlerListsDoNotCauseErrors()
    {
        // Arrange
        var eventBus = new GlobalEventBus();

        // Act & Assert - Should not throw
        await eventBus.PublishAsync(new TestEvent());
    }

    [Fact]
    public async Task PublishAsync_OnlyWriteHandlers_ExecutesSuccessfully()
    {
        // Arrange
        var eventBus = new GlobalEventBus();
        var writeExecuted = false;

        eventBus.Subscribe<TestEvent>(
            async evt =>
            {
                writeExecuted = true;
                await Task.CompletedTask;
            },
            HandlerType.Write
        );

        // Act
        await eventBus.PublishAsync(new TestEvent());

        // Assert
        Assert.True(writeExecuted);
    }

    [Fact]
    public async Task PublishAsync_OnlyReadHandlers_ExecutesSuccessfully()
    {
        // Arrange
        var eventBus = new GlobalEventBus();
        var readExecuted = false;

        eventBus.Subscribe<TestEvent>(
            async evt =>
            {
                readExecuted = true;
                await Task.CompletedTask;
            },
            HandlerType.Read
        );

        // Act
        await eventBus.PublishAsync(new TestEvent());

        // Assert
        Assert.True(readExecuted);
    }

    [Fact]
    public async Task PublishAsync_DefaultHandlerType_IsTreatedAsWrite()
    {
        // Arrange
        var eventBus = new GlobalEventBus();
        var executionOrder = new List<string>();

        // Register handler without specifying type (should default to Write)
        eventBus.Subscribe<TestEvent>(async evt =>
        {
            executionOrder.Add("Default");
            await Task.CompletedTask;
        });

        // Register explicit Read handler
        eventBus.Subscribe<TestEvent>(
            async evt =>
            {
                executionOrder.Add("Read");
                await Task.CompletedTask;
            },
            HandlerType.Read
        );

        // Act
        await eventBus.PublishAsync(new TestEvent());

        // Assert - Default handler should execute before Read handler
        Assert.Equal("Default", executionOrder[0]);
        Assert.Equal("Read", executionOrder[1]);
    }
}
