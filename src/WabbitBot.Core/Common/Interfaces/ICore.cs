namespace WabbitBot.Core.Common.Interfaces
{
    /// <summary>
    /// Base interface for core services providing common operations
    /// </summary>
    public interface ICore
    {
        /// <summary>
        /// Initializes the core service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Validates the core service state
        /// </summary>
        Task ValidateAsync();
    }
}
