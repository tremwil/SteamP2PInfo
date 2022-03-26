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
    public class ConfigBindingElementAttribute : Attribute, IConfigUIElement
    {
        public string OptionName { get; }
        public string Tooltip { get; }

        public bool Multicolumn => false;

        public DependencyProperty TargetProperty { get; }
        public IValueConverter ValueConverter { get; }

        public Type UIElementType { get; }

        public Dictionary<string, object> UIElementProperties { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="OptionName">Name of the option in the config editor.</param>
        /// <param name="UIElementType">Name of the UI element to be created for editing this value.</param>
        /// <param name="TargetProperty">Name of the DependencyProperty that will be bound to the config value.</param>
        /// <param name="Tooltip">Tooltip for this field in the config editor.</param>
        /// <param name="ValueConverter">Optional value converter which will be used by the binding.</param>
        /// <param name="UIElementProperties">List of key-value pairs specifying initiation properties for the UI element.</param>
        public ConfigBindingElementAttribute(string OptionName, Type UIElementType, string TargetProperty, string Tooltip = null, Type ValueConverter = null, object[] UIElementProperties = null)
        {
            this.OptionName = OptionName;
            this.UIElementType = UIElementType;
            this.TargetProperty = (DependencyProperty)UIElementType.GetField(TargetProperty, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
            this.ValueConverter = ValueConverter == null ? null : (IValueConverter)Activator.CreateInstance(ValueConverter);
            this.Tooltip = Tooltip;

            this.UIElementProperties = new Dictionary<string, object>();

            if (UIElementProperties != null)
            {
                for (int i = 0; i < UIElementProperties.Length; i++)
                {
                    object[] keyval = (object[])UIElementProperties[i];
                    this.UIElementProperties[(string)keyval[0]] = keyval[1];
                }
            }
        }

        public UIElement CreateUIElement(object source, string path)
        {
            Binding binding = new Binding(path) 
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            if (ValueConverter != null) binding.Converter = ValueConverter;

            UIElement element = (UIElement)Activator.CreateInstance(UIElementType);
            foreach (var kv in UIElementProperties)
            {
                PropertyInfo prop = UIElementType.GetProperty(kv.Key, BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public);
                if (prop != null) prop.SetValue(element, kv.Value);

                FieldInfo field = UIElementType.GetField(kv.Key, BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public);
                if (field != null) field.SetValue(element, kv.Value);
            }
            BindingOperations.SetBinding(element, TargetProperty, binding);
            return element;
        }
    }
}
