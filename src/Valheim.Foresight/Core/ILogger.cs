namespace Valheim.Foresight.Core;

/// <summary>
/// Logging interface for the plugin
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a fatal error message
    /// </summary>
    void LogFatal(object data);

    /// <summary>
    /// Logs an error message
    /// </summary>
    void LogError(object data);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    void LogWarning(object data);

    /// <summary>
    /// Logs a general message
    /// </summary>
    void LogMessage(object data);

    /// <summary>
    /// Logs an info message
    /// </summary>
    void LogInfo(object data);

    /// <summary>
    /// Logs a debug message
    /// </summary>
    void LogDebug(object data);

    bool IsLogsEnabled { get; set; }
    bool IsDebugLogsEnabled { get; set; }
}
