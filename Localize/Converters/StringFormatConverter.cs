namespace Spinico.Localize
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    class StringFormatConverter : IMultiValueConverter
    {
        /// <summary>
        /// The related resource key 
        /// </summary>
        /// <remarks>
        /// This is only used as a design mode value 
        /// </remarks>
        private string _key;

        /// <summary>
        /// A reference to the <see cref="Localize.FormatCase"/> method        
        /// </summary>
        private Func<string, ECaseFormat, string> _formatCase;

        public object Default { get; set; }
        public object Plural { get; set; }
        public object Negative { get; set; }
        public object Empty { get; set; }
        public object Null { get; set; }

        public ECaseFormat Case { get; set; }


        public StringFormatConverter(string key, Func<string, ECaseFormat, string> formatCase)
        {
            if (formatCase == null)
                throw new ArgumentNullException("The formatCase argument must not be null.");

            _key = key;
            _formatCase = formatCase;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {            
            object value = null;

            // The first value is used as the plural/singular flag
            if (values != null && values.Length > 0)
            {
                string text;
                double number;
                string format = "{0}";
        
                value = values[0];

                if (value != null)
                {
                    if (this.Default is String)
                    {
                        format = this.Default as String;
                        
                        if (double.TryParse(value.ToString(), out number))
                        {
                            if (number < 0 && this.Negative is String)
                            {
                                format = this.Negative as String;
                            }
                            else if (number == 0 && this.Empty is String)
                            {
                                format = this.Empty as String;
                            }
                            else if (number > 1 && this.Plural is String)
                            {
                                format = this.Plural as String;
                            }
                        }
                    }
                }
                else if (this.Null is String)
                {
                    format = this.Null as String;
                }

                try
                {
                    text = string.Format(format, values);
                }
                catch (FormatException)
                {
                    throw new FormatException("The values count (" + values.Length + ") does not match the expected format string: " + format);
                }

                return _formatCase(text, this.Case);
            }

            // In design mode, the {#<key>} indicates that the actual value will be resolved at runtime
            return (value == null && _key != null) ? _formatCase("{#" + _key + "}", this.Case) : value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}