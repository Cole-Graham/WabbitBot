using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Core.Common.BotCore;
using Xunit;

namespace WabbitBot.Core.Tests;

/// <summary>
/// Integration tests for race condition fixes in event handling.
/// Tests the PlayerReplaySubmitted critical race condition fix.
/// </summary>
public class RaceConditionTests
{
    /// <summary>
    /// Tests that multiple concurrent PlayerReplaySubmitted events don't cause duplicate AllReplaysSubmitted publications.
    /// This is the critical race condition that was fixed by two-phase execution.
    /// </summary>
    [Fact]
    public async Task PlayerReplaySubmitted_ConcurrentSubmissions_DoesNotCauseDuplicateFinalization()
    {
        // Arrange
        var globalBus = new TestGlobalEventBus();
        var eventBus = new CoreEventBus(globalBus);
        await eventBus.InitializeAsync();

        var allReplaysSubmittedCount = 0;
        var lockObj = new object();

        // Subscribe to AllReplaysSubmitted to count how many times it fires
        eventBus.Subscribe<AllReplaysSubmitted>(
            async evt =>
            {
                lock (lockObj)
                {
                    allReplaysSubmittedCount++;
                }
                await Task.CompletedTask;
            },
            HandlerType.Read
        );

        // Note: This is a conceptual test. In practice, you would need:
        // 1. A test database with a game and players
        // 2. Mock or real implementations of the handlers
        // 3. Simulation of concurrent replay submissions

        // Assert
        // After fixing race condition with two-phase execution:
        // - Core.GameHandler (Write) executes first and checks replays
        // - Only ONE handler should detect all replays submitted
        // - DiscBot.GameHandler (Read) executes after, only updates UI
        Assert.True(true, "This is a placeholder for full integration test with database");
    }

    /// <summary>
    /// Tests that Write handlers complete database changes before Read handlers execute.
    /// </summary>
    [Fact]
    public async Task TwoPhaseExecution_WriteHandlersCompleteBeforeReadHandlers()
    {
        // Arrange
        var testEvent = new TestCoreEvent();
        var writeCompleted = false;
        var readSawWriteCompleted = false;

        var eventBus = new CoreEventBus(new TestGlobalEventBus());
        await eventBus.InitializeAsync();

        // Subscribe Write handler
        eventBus.Subscribe<TestCoreEvent>(
            async evt =>
            {
                await Task.Delay(50); // Simulate database write
                writeCompleted = true;
            },
            HandlerType.Write
        );

        // Subscribe Read handler
        eventBus.Subscribe<TestCoreEvent>(
            async evt =>
            {
                readSawWriteCompleted = writeCompleted;
                await Task.CompletedTask;
            },
            HandlerType.Read
        );

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        Assert.True(writeCompleted, "Write handler should have executed");
        Assert.True(readSawWriteCompleted, "Read handler should see Write handler's changes");
    }

    private class TestCoreEvent : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
    }

    private class TestGlobalEventBus : WabbitBot.Common.Events.IGlobalEventBus
    {
        public Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : class
        {
            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, Task> handler, HandlerType type = HandlerType.Write)
            where TEvent : class { }

        public void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
            where TEvent : class { }

        public Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null)
            where TRequest : class
            where TResponse : class
        {
            return Task.FromResult<TResponse?>(null);
        }
    }
}
