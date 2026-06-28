// Caminho do arquivo: Engeradios.Desktop/Helpers/UIConverters.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace Engeradios.Desktop.Helpers
{
    public class BoolToCloudConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSync) return isSync ? "☁️ Sim" : "⏳ Fila";
            return "⏳";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToLockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isProtected) return isProtected ? "🔒 Sim" : "🔓 Não";
            return "🔓";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}