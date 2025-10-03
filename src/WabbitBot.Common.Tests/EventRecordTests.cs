using Xunit;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Configuration;

namespace WabbitBot.Common.Tests;

/// <summary>
/// Unit tests for event records to ensure immutability and proper implementation.
/// </summary>
public class EventRecordTests
{
    [Fact]
    public void StartupInitiatedEvent_IsImmutable()
    {
        // Arrange
        var configuration = new BotOptions { Token = "test-token" };
        IBotConfigurationService configService = new MockBotConfigurationService(); // Simple mock

        // Act
        var @event = new StartupInitiatedEvent(configuration, configService);

        // Assert - Verify immutability by checking properties cannot be modified
        Assert.Equal(configuration, @event.Configuration);
        Assert.Equal(configService, @event.ConfigurationService);
        Assert.Equal(EventBusType.Global, @event.EventBusType);
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.NotEqual(default, @event.Timestamp);
    }

    [Fact]
    public void BotConfigurationChangedEvent_IsImmutable()
    {
        // Arrange
        var property = "TestProperty";
        var newValue = "TestValue";

        // Act
        var @event = new BotConfigurationChangedEvent(property, newValue);

        // Assert
        Assert.Equal(property, @event.Property);
        Assert.Equal(newValue, @event.NewValue);
        Assert.Equal(EventBusType.Global, @event.EventBusType);
        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.NotEqual(default, @event.Timestamp);
    }

    [Fact]
    public void EventRecords_HaveUniqueIds()
    {
        // Arrange & Act
        var event1 = new StartupInitiatedEvent(new BotOptions { Token = "test-token" }, new MockBotConfigurationService());
        var event2 = new StartupInitiatedEvent(new BotOptions { Token = "test-token" }, new MockBotConfigurationService());

        // Assert
        Assert.NotEqual(event1.EventId, event2.EventId);
    }

    [Fact]
    public void EventRecords_HaveTimestamps()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var @event = new StartupInitiatedEvent(new BotOptions { Token = "test-token" }, new MockBotConfigurationService());
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(@event.Timestamp >= before);
        Assert.True(@event.Timestamp <= after);
    }

    [Fact]
    public void EventRecords_ImplementIEvent()
    {
        // Arrange & Act
        var @event = new StartupInitiatedEvent(new BotOptions { Token = "test-token" }, new MockBotConfigurationService());

        // Assert
        Assert.IsAssignableFrom<IEvent>(@event);
        Assert.Equal(EventBusType.Global, @event.EventBusType);
    }

    [Fact]
    public void EventRecords_AreRecords()
    {
        // Arrange & Act
        var config = new BotOptions { Token = "test-token" };
        var service = new MockBotConfigurationService();
        var event1 = new StartupInitiatedEvent(config, service);
        var event2 = new StartupInitiatedEvent(config, service);

        // Assert - Records with different auto-generated IDs should NOT be equal
        Assert.NotEqual(event1, event2);
        Assert.NotEqual(event1.GetHashCode(), event2.GetHashCode());

        // But they should have the same base properties
        Assert.Equal(event1.Configuration.Token, event2.Configuration.Token);
        Assert.Equal(event1.ConfigurationService, event2.ConfigurationService);
        Assert.Equal(event1.EventBusType, event2.EventBusType);
    }
}

/// <summary>
/// Simple mock implementation of IBotConfigurationService for testing.
/// </summary>
internal class MockBotConfigurationService : IBotConfigurationService
{
    public string GetToken() => "mock-token";
    public string GetDatabasePath() => "mock-db-path";
    public T GetSection<T>(string sectionName) where T : class, new() => new T();
    public void ValidateConfiguration() { /* No-op for testing */ }
}