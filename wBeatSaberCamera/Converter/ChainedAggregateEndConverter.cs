using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace wBeatSaberCamera.Converter
{
    public class ChainedAggregateEndConverter : IMultiValueConverter
    {
        public IValueConverter AggregateConverter { private get; set; }

        public IValueConverter EndConverter { private get; set; }

        public Type AggrgateType { private get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 1)
            {
                throw new InvalidOperationException("Expected at least 2 values");
            }

            return EndConverter.Convert(values.Aggregate((o1, o2) => AggregateConverter.Convert(o1, AggrgateType, o2, culture)), targetType, parameter, culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("No two way conversion, one way binding only.");
        }
    }
}