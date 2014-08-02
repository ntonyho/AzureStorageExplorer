using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for NewBlobDialog.xaml
    /// </summary>
    public partial class NewBlobDialog : Window
    {
        private bool Initialized = false;
        public int PageBlobSize = 0;

        public NewBlobDialog()
        {
            InitializeComponent();
            Initialized = true;
            CenterWindowOnScreen();
            //BlobName.Focus();
            BlobTypeBlock.Focus();
        }

        //**************************
        //*                        *
        //*  CenterWindowOnScreen  *
        //*                        *
        //**************************
        // Center the main window on the screen.

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void BlobType_Checked(object sender, RoutedEventArgs e)
        {
            if (!Initialized) return;

            if (BlobTypePage.IsChecked.Value)
            {
                BlobTextLabel.Visibility = Visibility.Collapsed;
                BlobText.Visibility = Visibility.Collapsed;

                BlobSizeLabel.Visibility = Visibility.Visible;
                BlobSize.Visibility = Visibility.Visible;
            }
            else
            {
                BlobTextLabel.Visibility = Visibility.Visible;
                BlobText.Visibility = Visibility.Visible;

                BlobSizeLabel.Visibility = Visibility.Collapsed;
                BlobSize.Visibility = Visibility.Collapsed;
            }
        }

        private void CreateBlob_Click(object sender, RoutedEventArgs e)
        {
            if (BlobName.Text.Length==0)
            {
                MessageBox.Show("A blob name is required.", "Name Required");
                return;
            }

            if (BlobTypePage.IsChecked.Value)
            {
                int multiplier = 1;

                String size = BlobSize.Text.ToUpper();
                if (size.EndsWith("K"))
                {
                    multiplier = 1024;
                    size = size.Substring(0, size.Length-1);
                }
                else if (size.EndsWith("M"))
                {
                    multiplier = 1024 * 1024;
                    size = size.Substring(0, size.Length - 1);
                }
                else if (size.EndsWith("G"))
                {
                    multiplier = 1024 * 1024 * 1024;
                    size = size.Substring(0, size.Length-1);
                }
                size = size.Trim();
                if (size.Length==0)
                {
                    MessageBox.Show("APlease enter a size to allocate for the page blob. Enter an integer number. Add a K, M, or G suffix if you wish to specify kilobytes, megabytes, or gigabytes.", "Size Required");
                    return;
                }

                if (!Int32.TryParse(size, out PageBlobSize))
                {
                    MessageBox.Show("The size is invalid. Please enter a size to allocate for the page blob. Enter an integer number. Add a K, M, or G suffix if you wish to specify kilobytes, megabytes, or gigabytes.", "Invalid Size");
                    return;
                }
                PageBlobSize = PageBlobSize * multiplier;


                if ((Convert.ToInt32(PageBlobSize / 512) * 512)!=PageBlobSize)
                {
                    MessageBox.Show("The size is invalid. A page blob size must be an exact multiple of 512 bytes. Please enter a size to allocate for the page blob. Enter an integer number. Add a K, M, or G suffix if you wish to specify kilobytes, megabytes, or gigabytes.", "Invalid Size");
                    return;
                }
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
