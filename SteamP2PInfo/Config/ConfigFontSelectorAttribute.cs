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
    public class ConfigFontSelectorAttribute : Attribute, IConfigUIElement
    {
        public string OptionName { get; }
        public string Tooltip { get; }
        public bool Multicolumn => false;

        Button changeFontButton;
        object source;
        string path;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OptionName">Name of the option in the config editor.</param>
        /// <param name="EnumType">Type of the enumeration whose values are to be converted.</param>
        /// <param name="Tooltip">Tooltip for this field in the config editor.</param>
        public ConfigFontSelectorAttribute(string OptionName, string Tooltip = null)
        {
            this.OptionName = OptionName;
            this.Tooltip = Tooltip;
        }

        public UIElement CreateUIElement(object source, string path)
        {
            this.source = source;
            this.path = path;

            Binding binding = new Binding(path)
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            changeFontButton = new Button();
            changeFontButton.Click += ChangeFontButton_Click;

            BindingOperations.SetBinding(changeFontButton, Button.ContentProperty, binding);
            return changeFontButton;
        }

        private void ChangeFontButton_Click(object sender, RoutedEventArgs e)
        {
            PropertyInfo tgtProperty = source.GetType().GetProperty(path, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            var converter = TypeDescriptor.GetConverter(typeof(System.Drawing.Font));
            var fD = new System.Windows.Forms.FontDialog();
            fD.Font = (System.Drawing.Font)converter.ConvertFromInvariantString((string)tgtProperty.GetValue(source));

            if (fD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tgtProperty.SetValue(source, converter.ConvertToInvariantString(fD.Font));
            }
        }
    }
}
