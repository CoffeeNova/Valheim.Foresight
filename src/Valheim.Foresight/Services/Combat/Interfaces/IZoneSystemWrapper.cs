using System.Collections.Generic;

namespace Valheim.Foresight.Services.Combat.Interfaces;

/// <summary>
/// Wrapper interface for ZoneSystem singleton to enable testing
/// </summary>
public interface IZoneSystemWrapper
{
    /// <summary>
    /// Checks if ZoneSystem is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets a global key value
    /// </summary>
    bool GetGlobalKey(string key, out string value);

    /// <summary>
    /// Checks if a global key exists
    /// </summary>
    bool GetGlobalKey(string key);

    /// <summary>
    /// Tries to get a value from the global keys dictionary
    /// </summary>
    bool TryGetGlobalKeyValue(string key, out string value);

    /// <summary>
    /// Gets all global keys
    /// </summary>
    List<string> GetGlobalKeys();
}
