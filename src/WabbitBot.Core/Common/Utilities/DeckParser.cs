using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Utilities
{
    /// <summary>
    /// Parser for WARNO deck strings. Provides methods to decode deck strings into Deck objects
    /// and encode Deck objects back into deck strings.
    ///
    /// Deck string format:
    /// - Base64-encoded binary data
    /// - Header: 4 bytes (magic + version)
    /// - Division ID: variable-length integer
    /// - Number of cards: variable-length integer
    /// - For each card:
    ///   - Unit ID: variable-length integer
    ///   - Transport ID: variable-length integer (0 if no transport)
    ///   - Veterancy: variable-length integer
    /// </summary>
    public static class DeckParser
    {
        // Magic bytes for deck format identification
        private const byte MAGIC_BYTE_1 = 0x44; // 'D'
        private const byte MAGIC_BYTE_2 = 0x45; // 'E'
        private const byte VERSION_BYTE = 0x01;
        private const byte MODDED_FLAG = 0x4D; // 'M' - indicates modded deck
        #region Decoding

        /// <summary>
        /// Decodes a WARNO deck string into a Deck object.
        /// </summary>
        /// <param name="deckString">The deck string to decode</param>
        /// <param name="lookupService">Optional lookup service to populate descriptors</param>
        /// <returns>Result containing the decoded Deck or an error message</returns>
        /// <exception cref="ArgumentException">Thrown if the deck string is invalid</exception>
        public static Result<Deck> DecodeDeckString(string deckString, IDeckLookupService? lookupService = null)
        {
            if (string.IsNullOrWhiteSpace(deckString))
            {
                return Result<Deck>.Failure("Deck string cannot be null or empty");
            }

            try
            {
                // Decode from base64
                byte[] data = Convert.FromBase64String(deckString);

                if (data.Length < 4)
                {
                    return Result<Deck>.Failure("Deck string too short - invalid format");
                }

                var reader = new DeckBinaryReader(data);

                // Read header
                byte magic1 = reader.ReadByte();
                byte magic2 = reader.ReadByte();
                byte version = reader.ReadByte();
                byte flags = reader.ReadByte();

                bool modded = flags == MODDED_FLAG;

                // Validate magic bytes
                if (magic1 != MAGIC_BYTE_1 || magic2 != MAGIC_BYTE_2)
                {
                    return Result<Deck>.Failure(
                        $"Invalid magic bytes: expected 0x{MAGIC_BYTE_1:X2}{MAGIC_BYTE_2:X2}, got 0x{magic1:X2}{magic2:X2}"
                    );
                }

                if (version != VERSION_BYTE)
                {
                    return Result<Deck>.Failure($"Unsupported deck version: {version} (expected {VERSION_BYTE})");
                }

                // Read division ID
                int divisionId = reader.ReadVarInt();
                var division = new DeckDivision(divisionId, lookupService?.DivisionForId(divisionId));

                // Read number of cards
                int numberCards = reader.ReadVarInt();

                if (numberCards < 0 || numberCards > 100)
                {
                    return Result<Deck>.Failure($"Invalid number of cards: {numberCards}");
                }

                // Read cards
                var cards = new List<DeckCard>();
                for (int i = 0; i < numberCards; i++)
                {
                    int unitId = reader.ReadVarInt();
                    int transportId = reader.ReadVarInt();
                    int veterancy = reader.ReadVarInt();

                    var unit = new DeckUnit(unitId, lookupService?.UnitForId(unitId));
                    var transport =
                        transportId != 0 ? new DeckTransport(transportId, lookupService?.UnitForId(transportId)) : null;

                    cards.Add(new DeckCard(unit, transport, veterancy));
                }

                var deck = new Deck(modded, division, cards);
                return Result<Deck>.CreateSuccess(deck);
            }
            catch (FormatException ex)
            {
                return Result<Deck>.Failure($"Invalid base64 string: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<Deck>.Failure($"Failed to decode deck string: {ex.Message}");
            }
        }

        #endregion

        #region Encoding

        /// <summary>
        /// Encodes a Deck object into a deck string.
        /// </summary>
        /// <param name="deck">The deck to encode</param>
        /// <returns>Result containing the encoded deck string or an error message</returns>
        public static Result<string> EncodeDeck(Deck deck)
        {
            if (deck is null)
            {
                return Result<string>.Failure("Deck cannot be null");
            }

            if (deck.Division is null)
            {
                return Result<string>.Failure("Deck must have a division");
            }

            if (deck.Cards is null || deck.Cards.Count == 0)
            {
                return Result<string>.Failure("Deck must have at least one card");
            }

            if (deck.Cards.Count > 100)
            {
                return Result<string>.Failure("Deck cannot have more than 100 cards");
            }

            try
            {
                var writer = new DeckBinaryWriter();

                // Write header
                writer.WriteByte(MAGIC_BYTE_1);
                writer.WriteByte(MAGIC_BYTE_2);
                writer.WriteByte(VERSION_BYTE);
                writer.WriteByte(deck.Modded ? MODDED_FLAG : (byte)0x00);

                // Write division ID
                writer.WriteVarInt(deck.Division.Id);

                // Write number of cards
                writer.WriteVarInt(deck.Cards.Count);

                // Write cards
                foreach (var card in deck.Cards)
                {
                    if (card.Unit is null)
                    {
                        return Result<string>.Failure("Card must have a unit");
                    }

                    writer.WriteVarInt(card.Unit.Id);
                    writer.WriteVarInt(card.Transport?.Id ?? 0);
                    writer.WriteVarInt(card.Veterancy);
                }

                // Convert to base64
                byte[] data = writer.ToArray();
                string deckString = Convert.ToBase64String(data);

                return Result<string>.CreateSuccess(deckString);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to encode deck: {ex.Message}");
            }
        }

        #endregion

        #region Binary Reader/Writer Helpers

        /// <summary>
        /// Binary reader that supports variable-length integer encoding.
        /// </summary>
        private class DeckBinaryReader
        {
            private readonly byte[] _data;
            private int _position;

            public DeckBinaryReader(byte[] data)
            {
                _data = data;
                _position = 0;
            }

            public byte ReadByte()
            {
                if (_position >= _data.Length)
                {
                    throw new InvalidOperationException("Unexpected end of deck data");
                }

                return _data[_position++];
            }

            /// <summary>
            /// Reads a variable-length integer.
            /// Uses LEB128 encoding: 7 bits of data per byte, MSB indicates continuation.
            /// </summary>
            public int ReadVarInt()
            {
                int result = 0;
                int shift = 0;

                while (true)
                {
                    if (_position >= _data.Length)
                    {
                        throw new InvalidOperationException("Unexpected end of deck data while reading VarInt");
                    }

                    byte b = ReadByte();
                    result |= (b & 0x7F) << shift;

                    if ((b & 0x80) == 0)
                    {
                        break;
                    }

                    shift += 7;

                    if (shift >= 32)
                    {
                        throw new InvalidOperationException("VarInt too long");
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Binary writer that supports variable-length integer encoding.
        /// </summary>
        private class DeckBinaryWriter
        {
            private readonly List<byte> _data;

            public DeckBinaryWriter()
            {
                _data = new List<byte>();
            }

            public void WriteByte(byte value)
            {
                _data.Add(value);
            }

            /// <summary>
            /// Writes a variable-length integer.
            /// Uses LEB128 encoding: 7 bits of data per byte, MSB indicates continuation.
            /// </summary>
            public void WriteVarInt(int value)
            {
                if (value < 0)
                {
                    throw new ArgumentException("VarInt cannot be negative", nameof(value));
                }

                while (value >= 0x80)
                {
                    WriteByte((byte)((value & 0x7F) | 0x80));
                    value >>= 7;
                }

                WriteByte((byte)value);
            }

            public byte[] ToArray()
            {
                return _data.ToArray();
            }
        }

        #endregion
    }
}
