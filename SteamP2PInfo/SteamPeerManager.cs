using System.Collections.Generic;
using System.Linq;
using System.IO;
using Steamworks;

using SteamP2PInfo.Config;
using System.Text.RegularExpressions;
using System.Windows;
using System.Diagnostics;
using System;
using System.Reflection;

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
                {
                    peer = factory(player);
                    if (peer.UpdatePeerInfo())
                    {
                        Logger.WriteLine($"[PEER CONNECT] \"{peer.Name}\" (https://steamcommunity.com/profiles/{(ulong)peer.SteamID}) has connected via {peer.ConnectionTypeName}");
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
            // Make sure we're constantly writing to the IPC log to force Steam to eventually flush
            // This call was chosen because it's not something a game will call often
            // Thus we avoid blowing up the IPC log with dummy calls
            SteamFriends.SendClanChatMessage(new CSteamID(0), "");

            if (mustReopenLog)
            {
                sr?.Dispose();
                fs?.Close();
                fs?.Dispose();

                try
                {
                    fs = new FileStream(Settings.Default.SteamLogPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                    sr = new StreamReader(fs);
                    // If the file had to be reopened, read from the last position we were at before
                    if (lastPosInLog is null)
                        fs.Seek(0, SeekOrigin.End);
                    else
                        fs.Seek((long)lastPosInLog, SeekOrigin.Begin);
                    mustReopenLog = false;
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("Steam IPC log file directory does not exist", "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            var logDisconnect = new Action<SteamPeerBase, CSteamID, string>((p, sid, reason) =>
            {
                if (p is null)
                    Logger.WriteLine($"[PEER DISCONNECT] (https://steamcommunity.com/profiles/{(ulong)sid}): {reason}");
                else
                    Logger.WriteLine($"[PEER DISCONNECT] \"{p.Name}\" (https://steamcommunity.com/profiles/{(ulong)sid}): {reason}");
            });

            while (!mustReopenLog)
            {
                string line = await sr.ReadLineAsync();
                if (line == null)
                {
                    lastPosInLog = fs.Position;
                    break;
                }

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
                else if (line.Contains("LeaveLobby"))
                {
                    foreach (var sid in mPeers.Keys)
                    {
                        logDisconnect(mPeers[sid].peer, sid, "Player left Steam lobby");
                    }
                    mPeers.Clear();
                    continue;
                }
                else continue;

                CSteamID steamID = ExtractUser(line);

                if (steamID.m_SteamID != 0)
                {
                    if (steamID.BIndividualAccount())
                    {
                        if (begin)
                        {
                            if (!mPeers.TryGetValue(steamID, out SteamPeerInfo peer))
                            {
                                var newPeerInfo = new SteamPeerInfo(GetPeer(steamID));
                                if (newPeerInfo.peer is null)
                                {
                                    Logger.WriteLine($"[PEER CONNECT] Player \"{steamID}\" was detected, but we don't have a P2P connection to them yet");
                                    newPeerInfo.lastDisconnectTimeMS = sw.ElapsedMilliseconds;
                                }
                                mPeers.Add(steamID, newPeerInfo);
                            }
                        }
                        else
                        {
                            // peer just disconnected
                            if (mPeers.TryGetValue(steamID, out SteamPeerInfo pInfo))
                            {
                                mPeers.Remove(steamID);
                                logDisconnect(pInfo.peer, steamID, "Auth session with peer ended");
                            }
                        }
                    }
                    else
                    {
                        Logger.WriteLine($"[PARSE ERROR] \"{steamID}\" was not a valid steam user");
                    }
                }
            }

            // clean up old peers.
            foreach (var sid in mPeers.Keys.ToArray())
            {
                var pInfo = mPeers[sid];
                bool isP2PConnected = false;
                if (pInfo.peer is null)
                    isP2PConnected = (pInfo.peer = GetPeer(sid)) != null;
                else
                    isP2PConnected = pInfo.peer.UpdatePeerInfo();

                if (pInfo.isConnected && !isP2PConnected)
                    pInfo.lastDisconnectTimeMS = sw.ElapsedMilliseconds;
                pInfo.isConnected = isP2PConnected;

                if (!isP2PConnected && sw.ElapsedMilliseconds - pInfo.lastDisconnectTimeMS > PEER_TIMEOUT_MS)
                {
                    mPeers.Remove(sid);
                    logDisconnect(pInfo.peer, sid, pInfo.peer is null ? "P2P connection was not established" : "Peer disconnected from P2P session");
                }
            }
        }

        public static IEnumerable<SteamPeerBase> GetPeers()
        {
            return mPeers.Values.Where(info => info.peer != null).Select(info => info.peer);
        }
    }
}
