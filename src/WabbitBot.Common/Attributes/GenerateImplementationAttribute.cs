using System;

namespace WabbitBot.Common.Attributes
{
    /// <summary>
    /// Attribute to mark a class for source-generated method implementations.
    /// Only use this when you want the source generator to create partial method implementations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GenerateImplementationAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the GenerateImplementationAttribute
        /// </summary>
        public GenerateImplementationAttribute()
        {
        }
    }
}