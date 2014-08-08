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
using Microsoft.WindowsAPICodePack.Dialogs;

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for NewContainerDialog.xaml
    /// </summary>
    public partial class DownloadEntitiesDialog : Window
    {
        public DownloadEntitiesDialog()
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

        public void SetEntityCounts(int totalCount, int selectedCount)
        {
            DownloadSelectedEntities.Content = "Download Selected Entities (" + selectedCount.ToString() + ")";
            DownloadAllEntities.Content = "Download All Entities (" + totalCount.ToString() + ")";

            if (selectedCount > 0)
            {
                DownloadSelectedEntities.IsChecked = true;
            }
            else
            {
                DownloadAllEntities.IsChecked = true;
            }
        }

        private void CmdCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // Browse to select output file.

        private void CmdSelectOutputFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose Entity Download File";
            dlg.IsFolderPicker = false;
            //dlg.InitialDirectory = currentDirectory;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            //dlg.DefaultDirectory = currentDirectory;
            dlg.EnsureFileExists = false;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            dlg.Filters.Add(new CommonFileDialogFilter("CSV File (*.csv)", ".csv"));
            dlg.Filters.Add(new CommonFileDialogFilter("XML File (*.xml)", ".xml"));

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                OutputFile.Focus();
                OutputFile.Text = dlg.FileName;
            }
        }

        // Start download. Handled in StorageView class.

        private void CmdDownload_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
