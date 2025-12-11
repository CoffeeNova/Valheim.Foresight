using BepInEx.Logging;

namespace Valheim.Foresight.Core;

/// <summary>
/// BepInEx logger implementation with configurable log levels
/// </summary>
public sealed class ForesightLogger : ILogger
{
    private readonly ManualLogSource _logSource;

    public bool IsLogsEnabled { get; set; }
    public bool IsDebugLogsEnabled { get; set; }

    public ForesightLogger(ManualLogSource logSource)
    {
        _logSource = logSource ?? throw new System.ArgumentNullException(nameof(logSource));
    }

    public void LogFatal(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogFatal(data);
    }

    public void LogError(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogError(data);
    }

    public void LogWarning(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogWarning(data);
    }

    public void LogMessage(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogMessage(data);
    }

    public void LogInfo(object data)
    {
        if (IsLogsEnabled)
            _logSource.LogInfo(data);
    }

    public void LogDebug(object data)
    {
        if (IsLogsEnabled && IsDebugLogsEnabled)
            _logSource.LogDebug(data);
    }
}
