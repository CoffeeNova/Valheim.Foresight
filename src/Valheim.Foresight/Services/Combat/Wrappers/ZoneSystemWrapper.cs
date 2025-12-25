using System.Collections.Generic;
using Valheim.Foresight.Services.Combat.Interfaces;

namespace Valheim.Foresight.Services.Combat.Wrappers;

/// <summary>
/// Concrete implementation wrapping ZoneSystem singleton
/// </summary>
public sealed class ZoneSystemWrapper : IZoneSystemWrapper
{
    /// <inheritdoc/>
    public bool IsInitialized => ZoneSystem.instance != null;

    /// <inheritdoc/>
    public bool GetGlobalKey(string key, out string value)
    {
        value = string.Empty;

        if (ZoneSystem.instance == null)
            return false;

        return ZoneSystem.instance.GetGlobalKey(key, out value);
    }

    /// <inheritdoc/>
    public bool GetGlobalKey(string key)
    {
        if (ZoneSystem.instance == null)
            return false;

        return ZoneSystem.instance.GetGlobalKey(key);
    }

    /// <inheritdoc/>
    public bool TryGetGlobalKeyValue(string key, out string value)
    {
        value = string.Empty;

        if (ZoneSystem.instance == null || ZoneSystem.instance.m_globalKeysValues == null)
            return false;

        return ZoneSystem.instance.m_globalKeysValues.TryGetValue(key, out value);
    }

    /// <inheritdoc/>
    public List<string> GetGlobalKeys()
    {
        return ZoneSystem.instance?.GetGlobalKeys() ?? new List<string>();
    }
}
