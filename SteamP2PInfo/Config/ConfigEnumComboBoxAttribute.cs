using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Reflection;

namespace SteamP2PInfo.Config
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ConfigEnumComboBoxAttribute : Attribute, IConfigUIElement
    {
        public string OptionName { get; }
        public string Tooltip { get; }
        public bool Multicolumn => false;

        public Type EnumType { get; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="OptionName">Name of the option in the config editor.</param>
        /// <param name="EnumType">Type of the enumeration whose values are to be converted.</param>
        /// <param name="Tooltip">Tooltip for this field in the config editor.</param>
        public ConfigEnumComboBoxAttribute(string OptionName, Type EnumType, string Tooltip = null)
        {
            this.OptionName = OptionName;
            this.EnumType = EnumType;
            this.Tooltip = Tooltip;
        }

        public UIElement CreateUIElement(object source, string path)
        {
            Binding binding = new Binding(path)
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                Converter = new EnumIndexConverter<Enum>(EnumType),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            ComboBox comboBox = new ComboBox() { ItemsSource = Enum.GetNames(EnumType) };
            BindingOperations.SetBinding(comboBox, ComboBox.SelectedIndexProperty, binding);
            return comboBox;
        }
    }
}
