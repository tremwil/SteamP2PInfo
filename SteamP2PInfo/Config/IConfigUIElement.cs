using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SteamP2PInfo.Config
{
    public interface IConfigUIElement
    {
        string OptionName { get; }
        string Tooltip { get; }

        bool Multicolumn { get; }

        UIElement CreateUIElement(object source, string path);
    }
}
