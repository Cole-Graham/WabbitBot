using System;
using System.Collections.Generic;
using WabbitBot.Core.Common.Utilities;
using Xunit;

namespace WabbitBot.Core.Tests
{
    public class DeckParserTests
    {
        [Fact]
        public void DecodeDeckString_WithValidDeck_ReturnsSuccess()
        {
            // Arrange - Create a simple deck
            var deck = new Deck(
                modded: false,
                division: new DeckDivision(id: 1),
                cards: new List<DeckCard>
                {
                    new DeckCard(new DeckUnit(10), null, 0),
                    new DeckCard(new DeckUnit(20), new DeckTransport(30), 1),
                }
            );

            // Act - Encode and then decode
            var encodeResult = DeckParser.EncodeDeck(deck);
            Assert.True(encodeResult.Success, $"Encoding failed: {encodeResult.ErrorMessage}");

            var decodeResult = DeckParser.DecodeDeckString(encodeResult.Data!);

            // Assert
            Assert.True(decodeResult.Success, $"Decoding failed: {decodeResult.ErrorMessage}");
            Assert.NotNull(decodeResult.Data);

            var decodedDeck = decodeResult.Data;
            Assert.Equal(deck.Modded, decodedDeck.Modded);
            Assert.Equal(deck.Division.Id, decodedDeck.Division.Id);
            Assert.Equal(deck.Cards.Count, decodedDeck.Cards.Count);

            // Verify first card
            Assert.Equal(10, decodedDeck.Cards[0].Unit.Id);
            Assert.Null(decodedDeck.Cards[0].Transport);
            Assert.Equal(0, decodedDeck.Cards[0].Veterancy);

            // Verify second card
            Assert.Equal(20, decodedDeck.Cards[1].Unit.Id);
            Assert.NotNull(decodedDeck.Cards[1].Transport);
            var transport = decodedDeck.Cards[1].Transport;
            Assert.NotNull(transport);
            Assert.Equal(30, transport.Id);
            Assert.Equal(1, decodedDeck.Cards[1].Veterancy);
        }

        [Fact]
        public void DecodeDeckString_WithLookupService_PopulatesDescriptors()
        {
            // Arrange
            var unitData = new Dictionary<int, string>
            {
                { 10, "Descriptor_Unit_Infantry_Rifleman" },
                { 20, "Descriptor_Unit_Tank_M1A1" },
                { 30, "Descriptor_Unit_Transport_M113" },
            };

            var divisionData = new Dictionary<int, string> { { 1, "Descriptor_Division_US_1st_Armored" } };

            var lookupService = new GenericDeckLookupAdapter(unitData, divisionData);

            var deck = new Deck(
                modded: false,
                division: new DeckDivision(id: 1),
                cards: new List<DeckCard> { new DeckCard(new DeckUnit(10), new DeckTransport(30), 0) }
            );

            // Act
            var encodeResult = DeckParser.EncodeDeck(deck);
            Assert.True(encodeResult.Success);

            var decodeResult = DeckParser.DecodeDeckString(encodeResult.Data!, lookupService);

            // Assert
            Assert.True(decodeResult.Success);
            Assert.NotNull(decodeResult.Data);

            var decodedDeck = decodeResult.Data;
            Assert.Equal("Descriptor_Division_US_1st_Armored", decodedDeck.Division.Descriptor);
            Assert.Equal("Descriptor_Unit_Infantry_Rifleman", decodedDeck.Cards[0].Unit.Descriptor);
            Assert.Equal("Descriptor_Unit_Transport_M113", decodedDeck.Cards[0].Transport?.Descriptor);
        }

        [Fact]
        public void DecodeDeckString_WithModdedDeck_PreservesModdedFlag()
        {
            // Arrange
            var deck = new Deck(
                modded: true,
                division: new DeckDivision(id: 1),
                cards: new List<DeckCard> { new DeckCard(new DeckUnit(10), null, 0) }
            );

            // Act
            var encodeResult = DeckParser.EncodeDeck(deck);
            Assert.True(encodeResult.Success);

            var decodeResult = DeckParser.DecodeDeckString(encodeResult.Data!);

            // Assert
            Assert.True(decodeResult.Success);
            Assert.NotNull(decodeResult.Data);
            Assert.True(decodeResult.Data.Modded);
        }

        [Fact]
        public void DecodeDeckString_WithEmptyString_ReturnsFailure()
        {
            // Act
            var result = DeckParser.DecodeDeckString(string.Empty);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("cannot be null or empty", result.ErrorMessage);
        }

        [Fact]
        public void DecodeDeckString_WithInvalidBase64_ReturnsFailure()
        {
            // Act
            var result = DeckParser.DecodeDeckString("Not Valid Base64!!!");

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void EncodeDeck_WithNullDeck_ReturnsFailure()
        {
            // Act
            var result = DeckParser.EncodeDeck(null!);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("cannot be null", result.ErrorMessage);
        }

        [Fact]
        public void EncodeDeck_WithNoDivision_ReturnsFailure()
        {
            // Arrange
            var deck = new Deck { Division = null!, Cards = new List<DeckCard>() };

            // Act
            var result = DeckParser.EncodeDeck(deck);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("must have a division", result.ErrorMessage);
        }

        [Fact]
        public void EncodeDeck_WithNoCards_ReturnsFailure()
        {
            // Arrange
            var deck = new Deck { Division = new DeckDivision(1), Cards = new List<DeckCard>() };

            // Act
            var result = DeckParser.EncodeDeck(deck);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("must have at least one card", result.ErrorMessage);
        }

        [Fact]
        public void EncodeDeck_WithVaryingVeterancy_PreservesValues()
        {
            // Arrange
            var deck = new Deck(
                modded: false,
                division: new DeckDivision(id: 1),
                cards: new List<DeckCard>
                {
                    new DeckCard(new DeckUnit(10), null, 0),
                    new DeckCard(new DeckUnit(11), null, 1),
                    new DeckCard(new DeckUnit(12), null, 2),
                    new DeckCard(new DeckUnit(13), null, 3),
                }
            );

            // Act
            var encodeResult = DeckParser.EncodeDeck(deck);
            Assert.True(encodeResult.Success);

            var decodeResult = DeckParser.DecodeDeckString(encodeResult.Data!);

            // Assert
            Assert.True(decodeResult.Success);
            Assert.NotNull(decodeResult.Data);

            for (int i = 0; i < deck.Cards.Count; i++)
            {
                Assert.Equal(deck.Cards[i].Veterancy, decodeResult.Data.Cards[i].Veterancy);
            }
        }
    }
}
