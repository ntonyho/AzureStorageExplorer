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
    /// Interaction logic for UploadEntitiesDialog.xaml
    /// </summary>
    public partial class UploadEntitiesDialog : Window
    {
        private bool Initialized = false;

        public UploadEntitiesDialog()
        {
            InitializeComponent();
            CenterWindowOnScreen();
            Initialized = true;
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
        }

        private void CmdCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // Browse to select input file.

        private void CmdSelectInputFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose Entity Upload File";
            dlg.IsFolderPicker = false;
            //dlg.InitialDirectory = currentDirectory;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            //dlg.DefaultDirectory = currentDirectory;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            dlg.Filters.Add(new CommonFileDialogFilter("CSV File (*.csv)", ".csv"));
            dlg.Filters.Add(new CommonFileDialogFilter("JSON File (*.json)", ".json"));
            dlg.Filters.Add(new CommonFileDialogFilter("XML File (*.xml)", ".xml"));

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                InputFile.Focus();
                InputFile.Text = dlg.FileName;

                String filename =  dlg.FileName.ToLower();

                if (dlg.FileName.EndsWith(".csv"))
                {
                    UploadFormatCSV.IsChecked = true;
                }
                else if (dlg.FileName.EndsWith(".json"))
                {
                    UploadFormatJSON.IsChecked = true;
                }
                if (dlg.FileName.EndsWith(".xml"))
                {
                    UploadFormatXML.IsChecked = true;
                }
            }
        }

        // Start download. Handled in StorageView class.

        private void CmdUpload_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(PartitionKeyColumnName.Text))
            {
                PartitionKeyColumnName.Text = "PartitionKey";
            }

            if (String.IsNullOrEmpty(RowKeyColumnName.Text))
            {
                PartitionKeyColumnName.Text = "RowKey";
            }

            if (PartitionKeyColumnName.Text == RowKeyColumnName.Text)
            {
                MessageBox.Show("PartitionKey and RowKey must have distinct column names.", "Invalid Column Names");
                return;
            }


            DialogResult = true;
        }

        private void UploadFormatCSV_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void UploadFormat_Checked(object sender, RoutedEventArgs e)
        {
            if (!Initialized) return;

            EntityXPathPanel.Visibility = System.Windows.Visibility.Collapsed;
            OuterElementPanel.Visibility = System.Windows.Visibility.Collapsed;

            if (UploadFormatJSON.IsChecked.Value)
            {
                OuterElementPanel.Visibility = System.Windows.Visibility.Visible;
            }

            if (UploadFormatXML.IsChecked.Value)
            {
                EntityXPathPanel.Visibility = System.Windows.Visibility.Visible;
            }
        }
    }
}
