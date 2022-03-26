using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Steamworks;
using System.Text.RegularExpressions;

namespace SteamTest
{
    internal class SteamLogLobbyMonitor : IDisposable
    {
        FileStream fs;
        StreamReader sr;

        HashSet<CSteamID> lobbies;

        public CSteamID ExtractLobby(string str)
        {
            Regex steamid3 = new Regex(@"\[L:1:(?<id>\d+)\]");

            Match m = steamid3.Match(str);
            if (m.Success)
            {
                return new CSteamID(0x186000000000000ul | ulong.Parse(m.Groups["id"].Value));
            }
            else return new CSteamID(0);
        }

        public void Dispose()
        {
            sr.Dispose();
            fs.Dispose();
        }

        public SteamLogLobbyMonitor(string path)
        {
            lobbies = new HashSet<CSteamID>();

            fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            sr = new StreamReader(fs);
            sr.ReadToEnd();
        }

        public void Update()
        {
            string lines = sr.ReadToEnd();
            foreach (string line in lines.Split('\n'))
            {
                CSteamID lobby = ExtractLobby(line);

                if (line.Contains("IClientMatchmaking::LeaveLobby") && LobbyLeft != null && lobbies.Contains(lobby))
                    LobbyLeft(this, lobby);

                else if (lobby.IsLobby() && LobbyJoined != null && !lobbies.Contains(lobby))
                {
                    lobbies.Add(lobby);
                    LobbyJoined(this, lobby);
                }
            }
        }

        public delegate void LobbyEventHandler(object sender, CSteamID lobby);

        public event LobbyEventHandler LobbyJoined;
        public event LobbyEventHandler LobbyLeft;
    }
}
