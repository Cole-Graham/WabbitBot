using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Utilities
{
    #region DeckUnit
    /// <summary>
    /// Represents a unit in a deck, containing either a unit ID or a descriptor.
    /// </summary>
    public class DeckUnit
    {
        /// <summary>
        /// Numeric unit ID from DeckSerializer.ndf
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Optional unit descriptor (e.g., "Descriptor_Unit_M1A1_Abrams_SOK")
        /// Populated if a lookup service is provided during decoding.
        /// </summary>
        public string? Descriptor { get; set; }

        public DeckUnit(int id, string? descriptor = null)
        {
            Id = id;
            Descriptor = descriptor;
        }
    }
    #endregion

    #region DeckTransport
    /// <summary>
    /// Represents a transport vehicle for a unit in a deck.
    /// </summary>
    public class DeckTransport
    {
        /// <summary>
        /// Numeric transport unit ID from DeckSerializer.ndf
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Optional transport descriptor (e.g., "Descriptor_Unit_M113_Transport")
        /// Populated if a lookup service is provided during decoding.
        /// </summary>
        public string? Descriptor { get; set; }

        public DeckTransport(int id, string? descriptor = null)
        {
            Id = id;
            Descriptor = descriptor;
        }
    }
    #endregion

    #region DeckDivision
    /// <summary>
    /// Represents a division in a deck.
    /// </summary>
    public class DeckDivision
    {
        /// <summary>
        /// Numeric division ID from DeckSerializer.ndf
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Optional division descriptor (e.g., "Descriptor_Deck_Division_US_1st_Armored")
        /// Populated if a lookup service is provided during decoding.
        /// </summary>
        public string? Descriptor { get; set; }

        public DeckDivision(int id, string? descriptor = null)
        {
            Id = id;
            Descriptor = descriptor;
        }
    }
    #endregion

    #region DeckCard
    /// <summary>
    /// Represents a single card (unit selection) in a deck.
    /// </summary>
    public class DeckCard
    {
        /// <summary>
        /// The unit for this card.
        /// </summary>
        public DeckUnit Unit { get; set; } = null!;

        /// <summary>
        /// The transport for this unit, if any.
        /// </summary>
        public DeckTransport? Transport { get; set; }

        /// <summary>
        /// Veterancy level of the unit (0-5, where 0 is no veterancy).
        /// </summary>
        public int Veterancy { get; set; }

        public DeckCard() { }

        public DeckCard(DeckUnit unit, DeckTransport? transport, int veterancy)
        {
            Unit = unit;
            Transport = transport;
            Veterancy = veterancy;
        }
    }
    #endregion

    #region Deck
    /// <summary>
    /// Represents a complete WARNO deck decoded from a deck string.
    /// </summary>
    public class Deck
    {
        /// <summary>
        /// Whether this deck uses modded content.
        /// </summary>
        public bool Modded { get; set; }

        /// <summary>
        /// The division for this deck.
        /// </summary>
        public DeckDivision Division { get; set; } = null!;

        /// <summary>
        /// Number of cards in this deck.
        /// </summary>
        public int NumberCards { get; set; }

        /// <summary>
        /// The cards (unit selections) in this deck.
        /// </summary>
        public List<DeckCard> Cards { get; set; } = new();

        public Deck() { }

        public Deck(bool modded, DeckDivision division, List<DeckCard> cards)
        {
            Modded = modded;
            Division = division;
            Cards = cards;
            NumberCards = cards.Count;
        }
    }
    #endregion

    #region IDeckLookupService
    /// <summary>
    /// Service interface for looking up unit and division descriptors from deck parsing IDs.
    /// Implementations can provide custom databases of unit and division data.
    /// </summary>
    public interface IDeckLookupService
    {
        /// <summary>
        /// Looks up a unit descriptor by its deck parsing ID.
        /// </summary>
        /// <param name="id">The numeric unit ID from DeckSerializer.ndf</param>
        /// <returns>The unit descriptor, or null if not found</returns>
        string? UnitForId(int id);

        /// <summary>
        /// Looks up a division descriptor by its deck parsing ID.
        /// </summary>
        /// <param name="id">The numeric division ID from DeckSerializer.ndf</param>
        /// <returns>The division descriptor, or null if not found</returns>
        string? DivisionForId(int id);
    }
    #endregion

    #region GenericDeckLookupAdapter
    /// <summary>
    /// Generic implementation of IDeckLookupService that uses dictionaries for lookups.
    /// </summary>
    public class GenericDeckLookupAdapter : IDeckLookupService
    {
        private readonly Dictionary<int, string> _unitData;
        private readonly Dictionary<int, string> _divisionData;

        /// <summary>
        /// Creates a new GenericDeckLookupAdapter with the provided unit and division data.
        /// </summary>
        /// <param name="unitData">Dictionary mapping unit IDs to descriptors</param>
        /// <param name="divisionData">Dictionary mapping division IDs to descriptors</param>
        public GenericDeckLookupAdapter(Dictionary<int, string> unitData, Dictionary<int, string> divisionData)
        {
            _unitData = unitData;
            _divisionData = divisionData;
        }

        /// <inheritdoc />
        public string? UnitForId(int id)
        {
            return _unitData.TryGetValue(id, out var descriptor) ? descriptor : null;
        }

        /// <inheritdoc />
        public string? DivisionForId(int id)
        {
            return _divisionData.TryGetValue(id, out var descriptor) ? descriptor : null;
        }
    }
    #endregion
}
