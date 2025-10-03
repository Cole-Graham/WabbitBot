# Step 4: Event Organization Summary

**Date:** October 2, 2025  
**Status:** ✅ COMPLETED with correct organization

## Event Organization Principle

**Events are organized by domain entity, NOT in monolithic GlobalEvents.cs files.**

### DiscBot Event Files

```
src/WabbitBot.DiscBot/App/Events/
├── ScrimmageEvents.cs   - Scrimmage challenge/acceptance/decline/cancellation (4 Global events)
├── MatchEvents.cs       - Match provisioning, completion (2 Global + 3 DiscBot-local events)
├── GameEvents.cs        - Game start, completion, deck submission (2 Global + 7 DiscBot-local events)
```

### Core Event Files

```
src/WabbitBot.Core/Common/Events/
└── MatchEvents.cs       - Match provisioning requests (1 Global event)
```

## Event Distribution

### Global Events (Cross-Boundary)

**DiscBot → Core (8 events):**
- `ScrimmageEvents.cs`: 4 events (challenge, accept, decline, cancel)
- `MatchEvents.cs`: 2 events (provisioned, completed)
- `GameEvents.cs`: 2 events (started, completed)

**Core → DiscBot (1 event):**
- `MatchEvents.cs`: 1 event (provisioning requested)

**Total Global:** 9 events

### DiscBot-Local Events (15 events)

**MatchEvents.cs:** 3 events
- MatchThreadCreateRequested
- MatchContainerRequested
- MatchThreadCreated

**GameEvents.cs:** 7 events
- GameContainerRequested
- GameReplaySubmitted
- DeckDmStartRequested
- DeckDmUpdateRequested
- DeckDmConfirmRequested
- PlayerDeckSubmitted
- PlayerDeckConfirmed

## File Organization Benefits

1. **Domain-Driven** - Events grouped by business domain (Scrimmage, Match, Game)
2. **Maintainable** - Easy to find all events related to a feature
3. **Scalable** - New domains get their own files
4. **Clear Boundaries** - Global vs Local events clearly separated with region comments
5. **Consistent** - Follows existing Core event organization pattern

## Region Comments

Global events within domain files use clear region markers:

```csharp
#region Global Match Events (Cross-Boundary)

public record MatchProvisioned(...) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}

#endregion
```

## Source Generation (Step 6)

Source generators will:
- Detect events with `EventBusType.Global`
- Generate copies in target projects
- Preserve domain-based file organization
- Enable cross-project event communication

---

**Key Takeaway:** Events belong to their domain files (ScrimmageEvents, MatchEvents, GameEvents), NOT to generic GlobalEvents files. This makes the codebase more maintainable and aligns with domain-driven design principles.

