using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace SteamP2PInfo.Config
{
    public class GameConfig : INotifyPropertyChanged
    {
        /// <summary>
        /// Name of the game process. Used to identify which config to load when attaching to a game, and to find the window.
        /// </summary>
        [JsonProperty("process_name")]
        public string ProcessName { get; set; } = "";
        /// <summary>
        /// Name of the game's window. Used to itentify which window to target with the overlay.
        /// </summary>
        [JsonProperty("window_name")]
        public string WindowName { get; set; } = "";

        /// <summary>
        /// The Steam App ID of the game.
        /// </summary>
        [JsonProperty("steam_appid")]
        public int SteamAppId { get; set; } = 0;

        /// <summary>
        /// If true, will call SteamFriends.SetPlayedWith on each detected peer. Mainly intended to add this
        /// feature to games that don't support it, like Elden Ring. 
        /// </summary>
        [JsonProperty("set_played_with")]
        [ConfigBindingElement("Set Played With", typeof(ToggleSwitch), "IsOnProperty",
            Tooltip: "If enabled, will make peers show up in the Steam \"Recent Players\" list.",
            UIElementProperties: new object[] {
                new object[] { "OnContent", "Yes" },
                new object[] { "OffContent", "No" }
            })]
        public bool SetPlayedWith { get; set; } = false;

        // TODO: IMPLEMENT LOGGING
        ///// <summary>
        ///// If true, will dump peer information into a game-specific log file.
        ///// </summary>
        //[JsonProperty("log_activity")]
        //[ConfigBindingElement("Log Activity", typeof(ToggleSwitch), "IsOnProperty",
        //    Tooltip: "If enabled, will log each peer connection / disconnection in a game-specific log file.",
        //    UIElementProperties: new object[] {
        //        new object[] { "OnContent", "Yes" },
        //        new object[] { "OffContent", "No" }
        //    })]
        //public bool LogActivity { get; set; } = true;

        /// <summary>
        /// If true, the hotkey system will be enabled while attached to this game.
        /// The hotkey system 
        /// </summary>
        [JsonProperty("hotkeys_enabled")]
        [ConfigBindingElement("Enable Hotkeys", typeof(ToggleSwitch), "IsOnProperty",
            Tooltip: "If enabled, the hotkey system will be active while this game is running.",
            UIElementProperties: new object[] {
                new object[] { "OnContent", "Yes" },
                new object[] { "OffContent", "No" }
            })]
        public bool HotkeysEnabled { get; set; } = true;

        /// <summary>
        /// Overlay configuration for this game. Includes things like placement, enabled/disabled, etc.
        /// </summary>
        [JsonProperty("overlay")]
        [ConfigCategory("Overlay Config")]
        public OverlayConfig OverlayConfig { get; private set; } = new OverlayConfig();

        /// <summary>
        /// Configuration of the currently selected game.
        /// </summary>
        public static GameConfig Current { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Load a settings file as the current game settings, or create a new file if the game does not have associated settings yet.
        /// Returns true if this process name was not found and a new config file was created.
        /// </summary>
        /// <param name="processName"></param>
        public static bool LoadOrCreate(string processName)
        {
            if (!Directory.Exists("config"))
                Directory.CreateDirectory("config");

            if (!File.Exists($"config\\{processName}.json"))
            {
                Current = new GameConfig() { ProcessName = processName };
                Current.Save();
                return true;
            }
            else
            {
                string json = File.ReadAllText($"config\\{processName}.json");
                Current = JsonConvert.DeserializeObject<GameConfig>(json);
                return false;
            }
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(Current, Formatting.Indented);
            File.WriteAllText($"config\\{Current.ProcessName}.json", json);
        }
    }
}
