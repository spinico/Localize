namespace Spinico.Localize
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;

    class BindingNodeConverter : IMultiValueConverter
    {
        private BindingNode _root;

        public BindingNodeConverter(BindingNode root)
        {
            _root = root;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return GetMultiBindingValue(_root, values, targetType, culture);
        }

        private object GetMultiBindingValue(BindingNode node, object[] values, Type targetType, CultureInfo culture)
        {
            if (IsPropertyUnset(values))
            {
                return null;
            }

            if (node.MultiBinding == null)
            {
                throw new NullReferenceException("The MultiBinding reference is null.");
            }

            var multiBinding = node.MultiBinding;

            // Recursive call for MultiBinding nodes
            var objects = node.Nodes.Select(x => x.MultiBinding != null ? 
                              GetMultiBindingValue(x as BindingNode, values, targetType, culture) : 
                              values[x.Index]).ToArray();
            
            return multiBinding.Converter.Convert(objects, targetType, null, multiBinding.ConverterCulture ?? culture);
        }

        private bool IsPropertyUnset(object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != DependencyProperty.UnsetValue)
                    return false;                    
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}