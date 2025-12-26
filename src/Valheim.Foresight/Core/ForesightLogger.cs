using BepInEx.Logging;
using UnityEngine;

namespace Valheim.Foresight.Core;

/// <summary>
/// BepInEx logger implementation with configurable log levels
/// </summary>
public sealed class ForesightLogger : ILogger
{
    private readonly ManualLogSource _logSource;

    public bool IsLogsEnabled { get; set; }
    public bool IsDebugLogsEnabled { get; set; }

    /// <summary>
    /// Creates a new logger wrapping a BepInEx log source
    /// </summary>
    public ForesightLogger(ManualLogSource logSource)
    {
        _logSource = logSource ?? throw new System.ArgumentNullException(nameof(logSource));
    }

    /// <inheritdoc/>
    public void LogFatal(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogFatal(data);
    }

    /// <inheritdoc/>
    public void LogError(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogError(data);
    }

    /// <inheritdoc/>
    public void LogWarning(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogWarning(data);
    }

    /// <inheritdoc/>
    public void LogMessage(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogMessage(data);
    }

    /// <inheritdoc/>
    public void LogInfo(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogInfo(data);
    }

    /// <inheritdoc/>
    public void LogDebug(object data)
    {
        if (IsLogsEnabled && IsDebugLogsEnabled)
            _logSource.LogDebug(data);
    }
}
