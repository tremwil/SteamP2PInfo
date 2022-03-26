using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MahApps.Metro.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace SteamP2PInfo
{
    [ValueConversion(typeof(int), typeof(HotKey))]
    public class HotkeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int hk = (int)value;
            return new HotKey(KeyInterop.KeyFromVirtualKey(hk & 0xff), (ModifierKeys)(hk >> 8));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0;

            HotKey hk = (HotKey)value;
            return (int)hk.ModifierKeys << 8 | KeyInterop.VirtualKeyFromKey(hk.Key); 
        }
    }
}
