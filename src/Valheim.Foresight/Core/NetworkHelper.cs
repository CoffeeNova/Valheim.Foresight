namespace Valheim.Foresight.Core;

/// <summary>
/// Helper class for checking network and multiplayer state
/// </summary>
internal static class NetworkHelper
{
    /// <summary>
    /// Checks if the player is connected to a multiplayer server (not solo/local game)
    /// </summary>
    /// <returns>True if connected to a remote server, false if playing solo (local server)</returns>
    public static bool IsConnectedToServer()
    {
        if (ZNet.instance == null)
            return false;

        // In solo play, the player is both client and server
        // In multiplayer, only the host/dedicated server has IsServer() == true
        // So if we're NOT the server, we're connected to a remote server
        return !ZNet.instance.IsServer();
    }

    /// <summary>
    /// Checks if currently in solo/local game (player is hosting their own world)
    /// </summary>
    /// <returns>True if playing solo (local server), false if on remote server</returns>
    public static bool IsSoloGame()
    {
        if (ZNet.instance == null)
            return true; // Treat no network as solo

        // Solo game = player is the server (hosting locally)
        return ZNet.instance.IsServer();
    }

    /// <summary>
    /// Checks if this is a dedicated server instance
    /// </summary>
    /// <returns>True if running as dedicated server</returns>
    public static bool IsDedicatedServer()
    {
        if (ZNet.instance == null)
            return false;

        return ZNet.instance.IsDedicated();
    }
}
