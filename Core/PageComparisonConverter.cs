using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Tmds.DBus.Protocol;

namespace Quicksave_Clipboard.Core
{
    class PageComparisonConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is string pageString && values[1] is int currentPage)
            {
                if (int.TryParse(pageString, out int page))
                {
                    // Return "Active" if this is the current page, otherwise return null/empty
                    //MessageBox.Show($"Comparing page {page} with current page {currentPage+1}");
                    return page == currentPage+1 ? "Active" : null;
                }
            }
            return null; // Default to not active
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
