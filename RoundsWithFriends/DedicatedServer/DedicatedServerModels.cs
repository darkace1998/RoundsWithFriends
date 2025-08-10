using System;
using System.Collections.Generic;

namespace RWF.DedicatedServer
{
    /// <summary>
    /// Configuration for dedicated server connections
    /// </summary>
    public class DedicatedServerConfig
    {
        public string ServerAddress { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 7777;
        public int ConnectionTimeout { get; set; } = 30000;
        public bool AutoReconnect { get; set; } = true;
        public string PlayerName { get; set; } = "";
    }

    /// <summary>
    /// Represents information about a discovered server
    /// </summary>
    public class ServerInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Address { get; set; } = "";
        public int Port { get; set; }
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string GameMode { get; set; } = "";
        public bool RequiresPassword { get; set; }
        public int Ping { get; set; }
        public DateTime LastSeen { get; set; }
    }

    /// <summary>
    /// Connection states for dedicated server
    /// </summary>
    public enum DedicatedServerConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        InLobby,
        InGame,
        Reconnecting,
        Error
    }

    /// <summary>
    /// Events for dedicated server connection
    /// </summary>
    public class DedicatedServerEventArgs : EventArgs
    {
        public DedicatedServerConnectionState State { get; set; }
        public string Message { get; set; } = "";
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Server discovery events
    /// </summary>
    public class ServerDiscoveredEventArgs : EventArgs
    {
        public ServerInfo Server { get; set; } = new ServerInfo();
    }
}

#if !SERVER
namespace RWF.DedicatedServer
{
    /// <summary>
    /// Extension methods for integrating dedicated server support into the existing mod
    /// </summary>
    public static class DedicatedServerExtensions
    {
        private static readonly string DedicatedServerModeKey = RWFMod.GetCustomPropertyKey("DedicatedServerMode");

        /// <summary>
        /// Check if the current session is using dedicated server mode
        /// </summary>
        public static bool IsUsingDedicatedServer(this RWFMod mod)
        {
            return UnityEngine.PlayerPrefs.GetInt(DedicatedServerModeKey, 0) == 1;
        }

        /// <summary>
        /// Enable or disable dedicated server mode
        /// </summary>
        public static void SetDedicatedServerMode(this RWFMod mod, bool enabled)
        {
            UnityEngine.PlayerPrefs.SetInt(DedicatedServerModeKey, enabled ? 1 : 0);
        }

        /// <summary>
        /// Get the preferred server connection configuration
        /// </summary>
        public static DedicatedServerConfig GetServerConfig(this RWFMod mod)
        {
            return new DedicatedServerConfig
            {
                ServerAddress = UnityEngine.PlayerPrefs.GetString(RWFMod.GetCustomPropertyKey("ServerAddress"), "127.0.0.1"),
                ServerPort = UnityEngine.PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ServerPort"), 7777),
                ConnectionTimeout = UnityEngine.PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("ConnectionTimeout"), 30000),
                AutoReconnect = UnityEngine.PlayerPrefs.GetInt(RWFMod.GetCustomPropertyKey("AutoReconnect"), 1) == 1,
                PlayerName = UnityEngine.PlayerPrefs.GetString(RWFMod.GetCustomPropertyKey("PlayerName"), "")
            };
        }

        /// <summary>
        /// Save server connection configuration
        /// </summary>
        public static void SaveServerConfig(this RWFMod mod, DedicatedServerConfig config)
        {
            UnityEngine.PlayerPrefs.SetString(RWFMod.GetCustomPropertyKey("ServerAddress"), config.ServerAddress);
            UnityEngine.PlayerPrefs.SetInt(RWFMod.GetCustomPropertyKey("ServerPort"), config.ServerPort);
            UnityEngine.PlayerPrefs.SetInt(RWFMod.GetCustomPropertyKey("ConnectionTimeout"), config.ConnectionTimeout);
            UnityEngine.PlayerPrefs.SetInt(RWFMod.GetCustomPropertyKey("AutoReconnect"), config.AutoReconnect ? 1 : 0);
            UnityEngine.PlayerPrefs.SetString(RWFMod.GetCustomPropertyKey("PlayerName"), config.PlayerName);
        }
    }
}
#endif