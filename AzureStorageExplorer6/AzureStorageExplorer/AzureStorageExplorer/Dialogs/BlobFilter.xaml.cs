using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for BlobFilter.xaml
    /// </summary>
    public partial class BlobFilter : Window
    {
        public String BlobSortHeader = null;
        public ListSortDirection BlobSortDirection = ListSortDirection.Ascending;


        public BlobFilter()
        {
            InitializeComponent();
            CenterWindowOnScreen();
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

        private void CmdApplyClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CmdCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CmdClearAllFilters_Click(object sender, RoutedEventArgs e)
        {
            MaxBlobCount.Text = String.Empty;
            TypeAllBlobs.IsChecked = true;
            NameText.Text = String.Empty;
            MinSize.Text = String.Empty;
            MaxSize.Text = String.Empty;

           BlobSortHeader = null;
           BlobSortDirection = ListSortDirection.Ascending;

            MaxBlobCount.Focus();
        }
    }
}
