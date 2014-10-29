using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace AzureStorageExplorer.Helpers
{
    public abstract class BaseConverter : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    [ValueConversion(typeof(int), typeof(BitmapImage))]
    public class ItemTypeToImageConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            int? itemType = value is int ? (int) value : (int?)null;

            switch (itemType)
            {
                case ItemType.BLOB_CONTAINER:   // Blob container
                    return new BitmapImage(new Uri("pack://application:,,/Images/cloud_folder.png"));
                case ItemType.QUEUE_CONTAINER:   // Queue
                    return new BitmapImage(new Uri("pack://application:,,/Images/cloud_queue.png"));
                case ItemType.TABLE_CONTAINER:
                    return new BitmapImage(new Uri("pack://application:,,/Images/cloud_table.png"));
                default:
                    throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                        System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}