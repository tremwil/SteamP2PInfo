using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;
using MahApps.Metro.Controls;
using System.Windows.Controls;
using System.Globalization;

namespace SteamP2PInfo.Config
{
    public class OverlayConfig : INotifyPropertyChanged
    {
        public class PingColorRange
        {
            [JsonProperty("threshold")]
            public double Threshold { get; set; }
            [JsonProperty("color")]
            public string Color { get; set; }
        }

        [JsonProperty("enabled")]
        [ConfigBindingElement("Overlay Enabled", typeof(ToggleSwitch), "IsOnProperty",
            Tooltip: "Enable/disable the overlay.",
            UIElementProperties: new object[] {
                new object[] { "OnContent", "Yes" },
                new object[] { "OffContent", "No" }
            })]
        public bool Enabled { get; set; }

        [JsonProperty("show_steam_id")]
        [ConfigBindingElement("Show Steam ID", typeof(ToggleSwitch), "IsOnProperty",
            Tooltip: "Should account Steam ID 64 be displayed in the overlay?",
            UIElementProperties: new object[] {
                new object[] { "OnContent", "Yes" },
                new object[] { "OffContent", "No" }
            })]
        public bool ShowSteamID { get; set; } = false;

        [JsonProperty("show_connection_quality")]
        [ConfigBindingElement("Show Connection Quality", typeof(ToggleSwitch), "IsOnProperty",
            Tooltip: "Display connection quality heuristic from 0 (worst possible) to 1 (perfect).",
            UIElementProperties: new object[] {
                new object[] { "OnContent", "Yes" },
                new object[] { "OffContent", "No" }
            })]
        public bool ShowConnectionQuality { get; set; } = true;

        [JsonProperty("hotkey")]
        [ConfigBindingElement("Overlay Hotkey", typeof(HotKeyBox), "HotKeyProperty",
            Tooltip: "Hotkey to show/hide the overlay.",
            ValueConverter: typeof(HotkeyConverter))]
        public int Hotkey { get; set; } = 0;

        [JsonProperty("banner_format")]
        [ConfigBindingElement("Banner Format", typeof(TextBox), "TextProperty",
            Tooltip: "Format string for the overlay banner text.")]
        public string BannerFormat { get; set; } = "[{time:HH:mm:ss}] SteamP2PInfo - by tremwil";

        [JsonProperty("font")]
        [ConfigFontSelector("Font", "Font for the text used in the overlay.")]
        public string Font { get; set; } = "Segoe UI, 20.25pt";

        [JsonProperty("x_offset")]
        [ConfigBindingElement("X Offset", typeof(NumericUpDown), "ValueProperty",
            Tooltip: "Horizontal offset from anchor point as a percentage of window width.",
            UIElementProperties: new object[] {
                new object[] { "Minimum", 0d },
                new object[] { "Maximum", 1d },
                new object[] { "Interval", 0.005d },
                new object[] { "ParsingNumberStyle", NumberStyles.Float },
                new object[] { "StringFormat", "F3" }
            })]
        public double XOffset { get; set; } = 0.025;

        [JsonProperty("y_offset")]
        [ConfigBindingElement("Y Offset", typeof(NumericUpDown), "ValueProperty",
            Tooltip: "Vertical offset from anchor point as a percentage of window width.",
            UIElementProperties: new object[] {
                new object[] { "Minimum", 0d },
                new object[] { "Maximum", 1d },
                new object[] { "Interval", 0.005d },
                new object[] { "ParsingNumberStyle", NumberStyles.Float },
                new object[] { "StringFormat", "F3" }
            })]
        public double YOffset { get; set; } = 0.025;

        [JsonProperty("anchor")]
        [ConfigEnumComboBox("Anchor", typeof(OverlayAnchor), 
            Tooltip: "Corner of the game window on which the overlay is anchored.")]
        public OverlayAnchor Anchor { get; set; } = OverlayAnchor.TopRight;

        [JsonProperty("text_color")]
        [ConfigBindingElement("Text Color", typeof(ColorPicker), "SelectedColorProperty",
            Tooltip: "Color of overlay text.")]
        public string TextColor { get; set; } = "#FFFFFFFF";

        [JsonProperty("stroke_color")]
        [ConfigBindingElement("Stroke Color", typeof(ColorPicker), "SelectedColorProperty",
            Tooltip: "Color of overlay text stroke (outline).")]
        public string StrokeColor { get; set; } = "#FF000000";

        [JsonProperty("stroke_width")]
        [ConfigBindingElement("Stroke width", typeof(NumericUpDown), "ValueProperty",
            Tooltip: "Width of stroke (outline) of the overlay text.",
            UIElementProperties: new object[] {
                new object[] { "Minimum", 0d },
                new object[] { "Interval", 0.5d },
                new object[] { "ParsingNumberStyle", NumberStyles.Float },
                new object[] { "StringFormat", "F1" }
            })]
        public double StrokeWidth { get; set; } = 2.0;

        [JsonProperty("ping_colors", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<PingColorRange> PingColors { get; set; } = new List<PingColorRange>()
        {
            new PingColorRange { Threshold = 0, Color = "#FF00BFFF" },
            new PingColorRange { Threshold = 50, Color = "#FF7CFC00" },
            new PingColorRange { Threshold = 100, Color = "#FFFFFF00" },
            new PingColorRange { Threshold = 200, Color = "#FFCD5C5C" }
        };

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
