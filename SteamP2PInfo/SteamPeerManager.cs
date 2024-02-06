using System.Collections.Generic;
using System.Linq;
using System.IO;
using Steamworks;

using SteamP2PInfo.Config;
using System.Text.RegularExpressions;
using System.Windows;

namespace SteamP2PInfo
{
    /// <summary>
    /// Manage a list of active Steam P2P peers. The peers must be in a steam lobby with the current user to be detected.
    /// They will automatically be removed from the list if no packet was sent/recieved for a set amount of time.
    /// </summary>
    static class SteamPeerManager
    {
        private static FileStream fs;
        private static StreamReader sr;
        private static FileSystemWatcher fsWatcher;
        private static bool mustReopenLog = true;
        private static long? lastPosInLog = null;
        private static Stopwatch sw = new Stopwatch();

        private static readonly Regex STEAMID3_REGEX = new Regex(@"\[U:1:(?<id>\d+)\]", RegexOptions.Compiled);
        private const long STEAMID64_BASE = 0x0110_0001_0000_0000;

        private const long PEER_TIMEOUT_MS = 5000;

        private static readonly Func<CSteamID, SteamPeerBase>[] PEER_FACTORIES =
            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(SteamPeerBase)))
                .Select(t => new Func<CSteamID, SteamPeerBase>((CSteamID sid) => Activator.CreateInstance(t, sid) as SteamPeerBase))
                .ToArray();
            

        /// <summary>
        /// List of peers mapped by Steam ID.
        /// </summary>
        private static Dictionary<CSteamID, SteamPeerInfo> mPeers = new Dictionary<CSteamID, SteamPeerInfo>();

        private static List<KeyValuePair<CSteamID, SteamPeerInfo>> inactivePeers = new List<KeyValuePair<CSteamID, SteamPeerInfo>>();

        public static void Init()
        {
            fsWatcher = new FileSystemWatcher(Path.GetDirectoryName(Settings.Default.SteamLogPath));
            fsWatcher.Filter = Path.GetFileName(Settings.Default.SteamLogPath);
            fsWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            fsWatcher.Changed += (e, s) => mustReopenLog = true;
            fsWatcher.EnableRaisingEvents = true;
        }

        private static CSteamID ExtractUser(string str)
        {
            Match m = STEAMID3_REGEX.Match(str);
            if (m.Success)
            {
                return new CSteamID(ulong.Parse(m.Groups["id"].Value) + STEAMID64_BASE);
            }
            else
            {
                return new CSteamID(0);
            }
        }

        private static SteamPeerBase GetPeer(CSteamID player)
        {
            SteamPeerBase peer = null;
            foreach (var factory in PEER_FACTORIES)
            {
                try
                Logger.WriteLine($"[PEER CONNECT] \"{peer.Name}\" (https://steamcommunity.com/profiles/{(ulong)peer.SteamID}) has connected via SteamNetworking");
                if (GameConfig.Current.SetPlayedWith)
                    SteamFriends.SetPlayedWith(player);
                return peer;
            }
            else
            {
                    peer = factory(player);
                    if (peer.UpdatePeerInfo())
                var connState = SteamNetworkingMessages.GetSessionConnectionInfo(ref netIdentity, out _, out _);
                if (SteamPeerNewAPI.IsConnStateOK(connState))
                {
                        Logger.WriteLine($"[PEER CONNECT] \"{peer.Name}\" (https://steamcommunity.com/profiles/{(ulong)peer.SteamID}) has connected via {peer.ConnectionTypeName}");
                    Logger.WriteLine($"[PEER CONNECT] \"{peer.Name}\" (https://steamcommunity.com/profiles/{(ulong)peer.SteamID}) has connected via SteamNetworkingMessages");
                    if (GameConfig.Current.SetPlayedWith)
                        SteamFriends.SetPlayedWith(player);

                    return peer;
                }
            }
                catch (Exception)
                {
                    peer?.Dispose();
                }
            }
            return null;
        }

        public async static void UpdatePeerList()
        {
            if (mustReopenLog)
            {
                sr?.Dispose();
                fs?.Close();
                fs?.Dispose();

                try
                {
                    fs = new FileStream(Settings.Default.SteamLogPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                    sr = new StreamReader(fs);
                    // If the file must be reopened, read from the last 256 bytes instead of from the end.
                    // This should prevent a rare case of lobby "missing" when the reopen occurs as the IPC calls
                    // are made my the game
                    if (fs.Length > 256) fs.Seek(-256, SeekOrigin.End);
                    mustReopenLog = false;
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("Steam IPC log file directory does not exist", "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            while (!mustReopenLog)
            {
                string line = await sr.ReadLineAsync();
                if (line == null) break;

                if (!line.Contains(GameConfig.Current.ProcessName))
                    continue;

                bool begin;
                if (line.Contains("BeginAuthSession"))
                {
                    begin = true;
                }
                else if (line.Contains("EndAuthSession"))
                {
                    begin = false;
                }
                else
                {
                    continue; // no idea wtf this line was, but it doesn't belong
                }

                CSteamID steamID = ExtractUser(line);

                if (steamID.m_SteamID != 0)
                {
                    if (steamID.BIndividualAccount())
                    {
                        if (begin)
                        {
                            if (!mPeers.TryGetValue(steamID, out SteamPeerInfo peer))
                            {
                                SteamPeerBase newPeer = GetPeer(steamID);
                                if (newPeer != null)
                                {
                                    mPeers.Add(steamID, new SteamPeerInfo(newPeer));
                                }
                                else
                                {
                                    Logger.WriteLine($"[CONNECT ERROR] could not establish connection to \"{steamID}\"");
                                }
                            }
                        }
                        else
                        {
                            // peer just disconnected
                            if (mPeers.TryGetValue(steamID, out SteamPeerInfo peer))
                            {
                                mPeers.Remove(steamID);
                                peer.disconnectReason = SteamPeerInfo.DisconnectReason.AUTH_SESSION_ENDED;
                                inactivePeers.Add(new KeyValuePair<CSteamID, SteamPeerInfo>(steamID, peer));
                            }
                        }
                    }
                    else
                    {
                        Logger.WriteLine($"[PARSE ERROR] \"{steamID}\" was not a valid steam user");
                    }
                }
            }

            // clean up old peers. We can't remove from a Dictionary while iterating, so we save the entries we need to delete and then do a second pass.
            foreach (var peerMapping in mPeers)
            {
                if (!peerMapping.Value.steamPeerBase.UpdatePeerInfo())
                {
                    peerMapping.Value.disconnectReason = SteamPeerInfo.DisconnectReason.PEER_DISCONNECTED;
                    inactivePeers.Add(peerMapping);
                }
            }
            foreach (var peer in inactivePeers)
            {
                switch (peer.Value.disconnectReason)
                {
                    case SteamPeerInfo.DisconnectReason.AUTH_SESSION_ENDED:
                        Logger.WriteLine($"[PEER DISCONNECT] \"{peer.Value.steamPeerBase.Name}\" (https://steamcommunity.com/profiles/{(ulong)peer.Value.steamPeerBase.SteamID}) game session ended.");
                        break;
                    case SteamPeerInfo.DisconnectReason.PEER_DISCONNECTED:
                        Logger.WriteLine($"[PEER DISCONNECT] \"{peer.Value.steamPeerBase.Name}\" (https://steamcommunity.com/profiles/{(ulong)peer.Value.steamPeerBase.SteamID}) peer disconnected from monitoring connection.");
                        break;
                    default:
                        Logger.WriteLine($"[PEER DISCONNECT] \"{peer.Value.steamPeerBase.Name}\" (https://steamcommunity.com/profiles/{(ulong)peer.Value.steamPeerBase.SteamID}) unknown reason.");
                        break;
                }
                peer.Value.steamPeerBase.Dispose();
                mPeers.Remove(peer.Key);
            }
            inactivePeers.Clear();
        }

        public static IEnumerable<SteamPeerBase> GetPeers()
        {
            return mPeers.Select(entry => entry.Value.steamPeerBase);
        }
    }
}
