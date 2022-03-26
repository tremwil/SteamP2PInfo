using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SteamP2PInfo.Config
{
    [ValueConversion(typeof(Enum), typeof(int))]
    public class EnumIndexConverter<TEnum> : IValueConverter
    {
        public Type EnumType { get; }

        public EnumIndexConverter()
        {
            EnumType = typeof(TEnum);
        }

        public EnumIndexConverter(Type EnumType)
        {
            this.EnumType = EnumType;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object[] values = Enum.GetValues(EnumType).Cast<object>().ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Equals(value)) return i;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object[] values = Enum.GetValues(EnumType).Cast<object>().ToArray();

            int index = (int)value;
            if (index < 0 || index >= values.Length)
                return null;

            return Enum.ToObject(EnumType, values[index]);
        }
    }
}
