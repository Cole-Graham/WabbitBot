# WARNO Deck Parser

C# implementation of WARNO deck code parser/encoder, inspired by [warno-deck-utils](https://github.com/izohek/warno-deck-utils).

## Overview

This utility provides functionality to:
- **Decode** WARNO deck strings into structured `Deck` objects
- **Encode** `Deck` objects back into deck strings
- **Lookup** unit and division descriptors using a configurable lookup service

## Components

### Core Classes

- **`Deck`**: Represents a complete WARNO deck with division and cards
- **`DeckCard`**: Represents a single unit card in a deck with veterancy
- **`DeckUnit`**: Represents a unit with ID and optional descriptor
- **`DeckTransport`**: Represents a transport vehicle for a unit
- **`DeckDivision`**: Represents a division with ID and optional descriptor

### Parser

- **`DeckParser`**: Static class with `DecodeDeckString` and `EncodeDeck` methods

### Lookup Service

- **`IDeckLookupService`**: Interface for custom lookup implementations
- **`GenericDeckLookupAdapter`**: Dictionary-based lookup implementation

## Usage

### Basic Decoding

```csharp
using WabbitBot.Core.Common.Utilities;

// Decode a deck string
var result = DeckParser.DecodeDeckString("BASE64_DECK_STRING_HERE");

if (result.Success)
{
    var deck = result.Data;
    Console.WriteLine($"Division ID: {deck.Division.Id}");
    Console.WriteLine($"Number of cards: {deck.Cards.Count}");
    Console.WriteLine($"Modded: {deck.Modded}");
    
    foreach (var card in deck.Cards)
    {
        Console.WriteLine($"  Unit {card.Unit.Id}, Veterancy {card.Veterancy}");
        if (card.Transport is not null)
        {
            Console.WriteLine($"    Transport: {card.Transport.Id}");
        }
    }
}
else
{
    Console.WriteLine($"Failed to decode: {result.ErrorMessage}");
}
```

### Decoding with Descriptors

```csharp
// Create lookup dictionaries (you would load these from NDF files)
var unitData = new Dictionary<int, string>
{
    { 1, "Descriptor_Unit_Infantry_Rifleman" },
    { 2, "Descriptor_Unit_Tank_M1A1" },
    { 3, "Descriptor_Unit_Transport_M113" },
};

var divisionData = new Dictionary<int, string>
{
    { 1, "Descriptor_Division_US_1st_Armored" },
    { 2, "Descriptor_Division_US_82nd_Airborne" },
};

// Create lookup service
var lookupService = new GenericDeckLookupAdapter(unitData, divisionData);

// Decode with descriptors
var result = DeckParser.DecodeDeckString("BASE64_DECK_STRING_HERE", lookupService);

if (result.Success)
{
    var deck = result.Data;
    Console.WriteLine($"Division: {deck.Division.Descriptor}");
    
    foreach (var card in deck.Cards)
    {
        Console.WriteLine($"  Unit: {card.Unit.Descriptor}");
        if (card.Transport is not null)
        {
            Console.WriteLine($"    Transport: {card.Transport.Descriptor}");
        }
    }
}
```

### Encoding a Deck

```csharp
// Create a deck
var deck = new Deck(
    modded: false,
    division: new DeckDivision(id: 1),
    cards: new List<DeckCard>
    {
        new DeckCard(new DeckUnit(10), null, 0), // Unit 10, no transport, no veterancy
        new DeckCard(new DeckUnit(20), new DeckTransport(30), 1), // Unit 20 with transport 30, veterancy 1
        new DeckCard(new DeckUnit(25), null, 2), // Unit 25, no transport, veterancy 2
    }
);

// Encode to deck string
var result = DeckParser.EncodeDeck(deck);

if (result.Success)
{
    string deckString = result.Data;
    Console.WriteLine($"Deck string: {deckString}");
}
else
{
    Console.WriteLine($"Failed to encode: {result.ErrorMessage}");
}
```

### Custom Lookup Service

Implement your own lookup service for custom data sources:

```csharp
public class NdfFileLookupService : IDeckLookupService
{
    private readonly string _ndfPath;
    
    public NdfFileLookupService(string ndfPath)
    {
        _ndfPath = ndfPath;
    }
    
    public string? UnitForId(int id)
    {
        // Load from NDF file, database, etc.
        // Return null if not found
        return LoadUnitFromNdf(id);
    }
    
    public string? DivisionForId(int id)
    {
        // Load from NDF file, database, etc.
        // Return null if not found
        return LoadDivisionFromNdf(id);
    }
    
    private string? LoadUnitFromNdf(int id) { /* implementation */ }
    private string? LoadDivisionFromNdf(int id) { /* implementation */ }
}
```

## Deck String Format

The deck string is a base64-encoded binary format:

1. **Header** (4 bytes):
   - Magic bytes: `0x44 0x45` ('D' 'E')
   - Version: `0x01`
   - Flags: `0x4D` for modded, `0x00` for unmodded

2. **Division ID**: Variable-length integer (LEB128 encoding)

3. **Number of cards**: Variable-length integer

4. **For each card**:
   - Unit ID: Variable-length integer
   - Transport ID: Variable-length integer (0 if no transport)
   - Veterancy: Variable-length integer (0-5)

### Variable-Length Encoding (LEB128)

Integers are encoded using LEB128 (Little Endian Base 128):
- 7 bits of data per byte
- MSB (bit 7) indicates if more bytes follow
- Efficient for small integers (most common in deck codes)

## Integration with Replay Parsing

The `PlayerDeckContent` field in `ReplayPlayer` entities contains base64-encoded deck data that can be decoded using this parser:

```csharp
// From a ReplayPlayer entity
var replayPlayer = /* get from database */;

if (!string.IsNullOrEmpty(replayPlayer.PlayerDeckContent))
{
    var deckResult = DeckParser.DecodeDeckString(
        replayPlayer.PlayerDeckContent,
        lookupService
    );
    
    if (deckResult.Success)
    {
        var deck = deckResult.Data;
        // Use deck information
    }
}
```

## Testing

The implementation includes comprehensive unit tests in `WabbitBot.Core.Tests/DeckParserTests.cs`:

```bash
dotnet test --filter "FullyQualifiedName~DeckParserTests"
```

All tests verify:
- Basic encoding/decoding round-trip
- Descriptor lookup integration
- Modded deck flag preservation
- Veterancy level preservation
- Error handling for invalid inputs
- Edge cases (empty decks, null values, etc.)

## Future Enhancements

Potential improvements for future versions:

1. **NDF File Integration**: Direct NDF file parsing for automatic descriptor lookups
2. **Deck Validation**: Validate decks against division restrictions and unit availability
3. **Deck Statistics**: Calculate deck cost, unit type distribution, etc.
4. **Deck Comparison**: Compare two decks and highlight differences
5. **Deck Templates**: Save and load deck templates
6. **Deck Sharing**: Generate shareable URLs or QR codes for decks

## References

- Original TypeScript implementation: [warno-deck-utils](https://github.com/izohek/warno-deck-utils)
- WARNO Deck Analyzer: [warno.site](https://warno.site/)
- LEB128 encoding: [Wikipedia](https://en.wikipedia.org/wiki/LEB128)

