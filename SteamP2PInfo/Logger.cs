using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SteamP2PInfo
{
    public static class Logger
    {
        private static StreamWriter fs;
        private static DateTime lastLogCreated;
        private static string lastLoggedGame = "";

        private static void CreateOrOpenLogFile()
        {
            DateTime dateTime = DateTime.Now;

            if (fs == null || lastLogCreated.Day != dateTime.Day || lastLoggedGame != Config.GameConfig.Current.ProcessName)
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }

                string logDir = $"logs\\{Config.GameConfig.Current.ProcessName}\\";
                Directory.CreateDirectory(logDir);

                fs = File.AppendText(Path.Combine(logDir, $"{Config.GameConfig.Current.ProcessName}-{dateTime:yyyy-MM-dd}.log"));
                fs.AutoFlush = true;
                lastLogCreated = dateTime;
                lastLoggedGame = Config.GameConfig.Current.ProcessName;
            }
        }

        public static void Write(string message)
        {
            if (Config.GameConfig.Current == null || !Config.GameConfig.Current.LogActivity) return;
            CreateOrOpenLogFile();
            if (fs != null) fs.Write($"[{DateTime.Now:HH:mm:ss.ff}] {message}");
        }

        public static void WriteLine(string message)
        {
            if (Config.GameConfig.Current == null || !Config.GameConfig.Current.LogActivity) return;
            CreateOrOpenLogFile();
            if (fs != null) fs.WriteLine($"[{DateTime.Now:HH:mm:ss.ff}] {message}");
        }
    }
}
