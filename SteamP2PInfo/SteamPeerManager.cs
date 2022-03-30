using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Steamworks;
using System.Diagnostics;

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

        /// <summary>
        /// List of Steam lobbies the local player is currently in.
        /// </summary>
        private static HashSet<CSteamID> mLobbies = new HashSet<CSteamID>();

        /// <summary>
        /// List of peers mapped by Steam ID.
        /// </summary>
        private static Dictionary<CSteamID, SteamPeerBase> mPeers = new Dictionary<CSteamID, SteamPeerBase>();

        public static void Init()
        {
            fsWatcher = new FileSystemWatcher(Path.GetDirectoryName(Settings.Default.SteamLogPath));
            fsWatcher.Filter = Path.GetFileName(Settings.Default.SteamLogPath);
            fsWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            fsWatcher.Changed += (e, s) => mustReopenLog = true;
            fsWatcher.EnableRaisingEvents = true;
        }

        private static CSteamID ExtractLobby(string str)
        {
            Regex steamid3 = new Regex(@"\[L:1:(?<id>\d+)\]");

            Match m = steamid3.Match(str);
            if (m.Success)
            {
                return new CSteamID(0x186000000000000ul | ulong.Parse(m.Groups["id"].Value));
            }
            else return new CSteamID(0);
        }

        public async static void UpdatePeerList()
        {
            if (mustReopenLog)
            {
                sr?.Dispose();
                sr = null;

                fs?.Close();
                fs?.Dispose();
                fs = null;

                try
                {
                    fs = new FileStream(Settings.Default.SteamLogPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                    sr = new StreamReader(fs);
                    sr.BaseStream.Seek(0, SeekOrigin.End);
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

                if (!line.Contains("eldenring.exe") || line.Contains("IClientMatchmaking::LeaveLobby"))
                    continue;

                CSteamID lobby = ExtractLobby(line);

                if (lobby.IsLobby() && !mLobbies.Contains(lobby))
                    mLobbies.Add(lobby);
            }

            mLobbies.RemoveWhere(lobbyID =>
            {
                int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
                if (numMembers == 0) return true;

                bool localPlayerInLobby = false;
                for (int i = 0; i < numMembers; i++)
                {
                    CSteamID player = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);

                    if (player == (CSteamID)0 || mPeers.ContainsKey(player)) continue;
                    if (player == SteamUser.GetSteamID())
                    {
                        localPlayerInLobby = true;
                        continue;
                    }
                    if (SteamNetworking.GetP2PSessionState(player, out P2PSessionState_t pConnectionState) && SteamPeerOldAPI.IsSessionStateOK(pConnectionState))
                    {
                        mPeers[player] = new SteamPeerOldAPI(player);
                        Logger.WriteLine($"[PEER CONNECT] \"{mPeers[player].Name}\" (https://steamcommunity.com/profiles/{(ulong)mPeers[player].SteamID}) has connected via SteamNetworking");
                        if (GameConfig.Current.SetPlayedWith)
                            SteamFriends.SetPlayedWith(player);
                    }
                    else
                    {
                        SteamNetworkingIdentity netIdentity = new SteamNetworkingIdentity();
                        netIdentity.SetSteamID(player);
                        var connState = SteamNetworkingMessages.GetSessionConnectionInfo(ref netIdentity, out _, out _);
                        if (SteamPeerNewAPI.IsConnStateOK(connState))
                        {
                            mPeers[player] = new SteamPeerNewAPI(player);
                            Logger.WriteLine($"[PEER CONNECT] \"{mPeers[player].Name}\" (https://steamcommunity.com/profiles/{(ulong)mPeers[player].SteamID}) has connected via SteamNetworkingMessages");
                            if (GameConfig.Current.SetPlayedWith)
                                SteamFriends.SetPlayedWith(player);
                        }
                    }
                }
                return !localPlayerInLobby;
            });

            foreach (var player in mPeers.Keys.ToArray())
            {
                bool hasLobbyInCommon = false;
                foreach (var lobby in mLobbies)
                {
                    if (SteamFriends.IsUserInSource(player, lobby))
                    {
                        hasLobbyInCommon = true;
                        break;
                    }
                }
                if (!hasLobbyInCommon || !mPeers[player].UpdatePeerInfo())
                {
                    Logger.WriteLine($"[PEER DISCONNECT] \"{mPeers[player].Name}\" (https://steamcommunity.com/profiles/{(ulong)mPeers[player].SteamID}) has left the lobby or lost P2P connection");
                    mPeers[player].Dispose();
                    mPeers.Remove(player);
                }
            }
        }

        public static IEnumerable<SteamPeerBase> GetPeers()
        {
            return mPeers.Values;
        }
    }
}
