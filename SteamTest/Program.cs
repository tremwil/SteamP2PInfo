using System;
using System.Text;
using System.Threading;
using Steamworks;
using System.Diagnostics;
using System.IO;

namespace SteamTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!SteamAPI.Init())
            {
                Console.WriteLine("Steam API FAIL");
                return;
            }

            Process.Start("cmd.exe", "/c start steam://open/console");

            string logPath = "C:\\Program Files (x86)\\Steam\\logs\\ipc_SteamClient.log";
            var monitor = new SteamLogLobbyMonitor(logPath);
            monitor.LobbyJoined += Monitor_LobbyJoined;
            monitor.LobbyLeft += Monitor_LobbyLeft;
            
            while (true)
            {
                monitor.Update();
                Thread.Sleep(500);
            }

        }

        private static void Monitor_LobbyLeft(object sender, CSteamID lobby)
        {
            Console.WriteLine("LEAVE" + lobby.ToString());
        }

        private static void Monitor_LobbyJoined(object sender, CSteamID lobby)
        {
            Console.WriteLine("JOIN" + lobby.ToString());
        }
    }
}
