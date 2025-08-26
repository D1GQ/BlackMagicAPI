namespace BlackMagicAPI.Interfaces;

/// <summary>
/// Defines the contract for compatibility checking within the BlackMagicAPI framework.
/// </summary>
public interface ICompatibility
{
    /// <summary>
    /// Gets the compatibility version of the implementing component.
    /// This version is used to determine compatibility between different components
    /// of the BlackMagicAPI ecosystem.
    /// </summary>
    /// <value>
    /// A <see cref="Version"/> object representing the compatibility version
    /// of the implementing component.
    /// </value>
    /// <remarks>
    /// The compatibility version follows semantic versioning (Major.Minor.Build) format
    /// and is used to ensure that different components can work together properly.
    /// Components with the same major version are considered compatible, while
    /// different major versions indicate breaking changes.
    /// </remarks>
    internal Version CompatibilityVersion { get; }
}