using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Core.Common.Events;

// CRUD events removed: UserCreatedEvent, UserUpdatedEvent, UserArchivedEvent, UserDeletedEvent
// These were database operations and violate the critical principle that events are not for CRUD.
