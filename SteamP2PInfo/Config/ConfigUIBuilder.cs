using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace SteamP2PInfo.Config
{
    public static class ConfigUIBuilder
    {
        public static Grid CreateConfigEditor(object config)
        {
            PropertyInfo[] editableProperties = config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(p => p.GetCustomAttributes().Any(a => a is IConfigUIElement)).ToArray();

            Grid grid = new Grid() 
            { 
                Margin = new Thickness(10),
                ColumnDefinitions =
                {
                    new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) },
                    new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) }
                }
            };
            for (int i = 0; i < editableProperties.Length; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });

                var attr = editableProperties[i].GetCustomAttributes().Where(a => a is IConfigUIElement).FirstOrDefault() as IConfigUIElement;

                UIElement elem = attr.CreateUIElement(config, editableProperties[i].Name);
                elem.SetValue(Grid.RowProperty, 2 * i);
                if (attr.Multicolumn)
                {
                    elem.SetValue(Grid.ColumnProperty, 0);
                    elem.SetValue(Grid.ColumnSpanProperty, 2);
                }
                else 
                {
                    Label label = new Label()
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = attr.OptionName,
                        ToolTip = attr.Tooltip
                    };
                    label.SetValue(Grid.RowProperty, 2 * i);
                    label.SetValue(Grid.ColumnProperty, 0);
                    grid.Children.Add(label);

                    elem.SetValue(Grid.ColumnProperty, 1);
                }
                grid.Children.Add(elem);
            }
            return grid;
        }
    }
}
