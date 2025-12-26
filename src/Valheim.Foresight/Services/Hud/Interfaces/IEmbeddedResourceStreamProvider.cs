using System.IO;

namespace Valheim.Foresight.Services.Hud.Interfaces;

/// <summary>
/// Provides access to embedded assembly resources
/// </summary>
public interface IEmbeddedResourceStreamProvider
{
    /// <summary>
    /// Opens a stream to the embedded resource
    /// </summary>
    Stream? Open(string resourceName);

    /// <summary>
    /// Gets all embedded resource names
    /// </summary>
    string[] GetNames();
}
