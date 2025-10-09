using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Core.Common.BotCore;
using Xunit;

namespace WabbitBot.Core.Tests.Common.Events
{
    public class EventBusIntegrationTests
    {
        static EventBusIntegrationTests()
        {
            GlobalEventBusProvider.Initialize(new GlobalEventBus());
            // Touch instance to bind to the initialized global bus
            _ = CoreEventBus.Instance;
        }
        private record TestGlobalEvent(Guid EventId, DateTime Timestamp) : IEvent
        {
            public EventBusType EventBusType => EventBusType.Global;
        }

        private record TestRequest(Guid EventId, DateTime Timestamp) : IEvent
        {
            public EventBusType EventBusType => EventBusType.Core;
        }

        private record TestResponse(Guid EventId, DateTime Timestamp) : IEvent
        {
            public EventBusType EventBusType => EventBusType.Core;
        }

        [Fact]
        public async Task Forwards_Core_To_Global()
        {
            var core = CoreEventBus.Instance;
            await core.InitializeAsync();

            var global = GlobalEventBusProvider.GetGlobalEventBus();
            var tcs = new TaskCompletionSource<TestGlobalEvent>();
            global.Subscribe<TestGlobalEvent>(e => { tcs.TrySetResult(e); return Task.CompletedTask; });

            var evt = new TestGlobalEvent(Guid.NewGuid(), DateTime.UtcNow);
            await core.PublishAsync(evt);

            var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(evt.EventId, received.EventId);
        }

        [Fact]
        public async Task Request_Response_Correlates_Or_TimesOut()
        {
            var core = CoreEventBus.Instance;
            await core.InitializeAsync();

            // Responding handler
            core.Subscribe<TestRequest>(async req =>
            {
                var resp = new TestResponse(req.EventId, DateTime.UtcNow);
                await core.PublishAsync(resp);
            });

            var request = new TestRequest(Guid.NewGuid(), DateTime.UtcNow);
            var response = await core.RequestAsync<TestRequest, TestResponse>(request);
            Assert.NotNull(response);
            Assert.Equal(request.EventId, response!.EventId);
        }
    }
}


