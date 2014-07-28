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
    /// Interaction logic for CopyBlob.xaml
    /// </summary>
    public partial class CopyBlob : Window
    {
        public CopyBlob()
        {
            InitializeComponent();
            CenterWindowOnScreen();
        }

        // Copy command issued - return 

        private void CmdCopy_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(SourceAccount.Text) || String.IsNullOrEmpty(DestAccount.Text))
            {
                return;
            }
            if (String.IsNullOrEmpty(SourceContainer.Text) || String.IsNullOrEmpty(DestContainer.Text))
            { 
                return;
            }
            if (String.IsNullOrEmpty(SourceBlob.Text) || String.IsNullOrEmpty(DestBlob.Text))
            {
                return;
            }
            if (SourceAccount.Text == DestAccount.Text && 
                SourceContainer.Text == DestContainer.Text && 
                SourceBlob.Text == DestBlob.Text)
            {
                MessageBox.Show("Source and destination blobs must have different names when copying within the same container.", "Unique Destination Name Required");
                return;
            }

            // TODO: check destination storage account exists
            // TODO: check destination container exists

            DialogResult = true;
        }

        private void CmdCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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
    }
}
