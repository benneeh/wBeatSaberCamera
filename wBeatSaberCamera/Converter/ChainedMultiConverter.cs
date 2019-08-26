using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace wBeatSaberCamera.Converter
{
    public class ChainedMultiConverter : IMultiValueConverter
    {
        public IValueConverter Converter { private get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 1)
            {
                throw new InvalidOperationException("Expected at least 2 values");
            }

            return values.Aggregate((o1, o2) => Converter.Convert(o1, targetType, o2, culture));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("No two way conversion, one way binding only.");
        }
    }
}