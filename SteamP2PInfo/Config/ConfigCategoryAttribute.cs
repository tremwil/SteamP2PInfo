using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace SteamP2PInfo.Config
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ConfigCategoryAttribute : Attribute, IConfigUIElement
    {
        public string OptionName { get; }
        public string Tooltip { get; }
        public bool Multicolumn => true;

        public ConfigCategoryAttribute(string categoryName, string tooltip = null)
        {
            OptionName = categoryName;
            Tooltip = tooltip;
        }

        public UIElement CreateUIElement(object source, string path)
        {
            // Create a collapsible panel in which the settings in this category will be placed
            Expander expander = new Expander()
            {
                Header = OptionName,
                ToolTip = Tooltip
            };

            object subConfig = source.GetType().GetProperty(path, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).GetValue(source);
            expander.Content = ConfigUIBuilder.CreateConfigEditor(subConfig);
            return expander;
        }
    }
}
