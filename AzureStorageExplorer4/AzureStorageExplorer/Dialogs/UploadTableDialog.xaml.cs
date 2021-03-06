﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Neudesic.AzureStorageExplorer.Windows;
using Neudesic.AzureStorageExplorer.ViewModel;

namespace Neudesic.AzureStorageExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for UploadTableDialog.xaml
    /// </summary>
    public partial class UploadTableDialog : Window
    {
        private bool DialogInitialized = false;

        public string Format
        {
            get
            {
                if (FormatCSV.IsChecked.Value)
                {
                    return "csv";
                }
                else if (FormatPlainXML.IsChecked.Value)
                {
                    return "xml";
                }
                else if (FormatAtomPub.IsChecked.Value)
                {
                    return "atom";
                }
                else
                {
                    return String.Empty;
                }
            }
            set
            {
                switch(value)
                {
                    case "csv":
                        FormatCSV.IsChecked = true;
                        break;
                    case "xml":
                        FormatPlainXML.IsChecked = true;
                        break;
                    case "atom":
                        FormatAtomPub.IsChecked = true;
                        break;
                }
            }
        }

        public UploadTableDialog()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(UploadTableDialog_Loaded);

            FileName.Focus();
        }

        void UploadTableDialog_Loaded(object sender, RoutedEventArgs e)
        {
            DialogInitialized = true;
        }

        private void UploadTable_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (String.IsNullOrEmpty(TableName.Text))
            {
                MessageBox.Show("A table name is required", "Table Name Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (String.IsNullOrEmpty(FileName.Text))
            {
                Browse_Click(this, new RoutedEventArgs());
                return false;
            }

            if (!StorageAccountViewModel.ValidTableName(TableName.Text))
            {
                MessageBox.Show("The table name is invalid.\r\n\r\nTable names must be valid DNS names, 3-63 characters in length, beginning with a letter and containing only alphanumeric characters. Table names are case-sensitive.", "Invalid Table Name", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg2 = new System.Windows.Forms.OpenFileDialog();
            dlg2.Title = "Choose Upload File Name";
            if (FormatCSV.IsChecked.Value)
            {
                dlg2.Filter = "Comma-separated Value Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                dlg2.FileName = TableName.Text + ".csv";
            }
            else if (FormatPlainXML.IsChecked.Value)
            {
                dlg2.Filter = "XML Files (*.xml)|*.xml|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                dlg2.FileName = TableName.Text + ".xml";
            }
            else if (FormatAtomPub.IsChecked.Value)
            {
                dlg2.Filter = "XML Files (*.xml)|*.xml|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                dlg2.FileName = TableName.Text + ".xml";
            }

            HwndSource source = PresentationSource.FromVisual(MainWindow.Window) as HwndSource;
            System.Windows.Forms.IWin32Window win = new OldWindow(source.Handle);

            System.Windows.Forms.DialogResult result = dlg2.ShowDialog(win);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FileName.Text = dlg2.FileName;
            }
        }

        private void FormatCSV_Checked(object sender, RoutedEventArgs e)
        {
            if (!DialogInitialized) return;
            OptionColumnDefinitions.IsChecked = true;
            OptionColumnDefinitions.IsEnabled = true;
        }

        private void FormatSQL_Checked(object sender, RoutedEventArgs e)
        {
            if (!DialogInitialized) return;
            OptionColumnDefinitions.IsChecked = false;
           OptionColumnDefinitions.IsEnabled = false;
        }

        private void FormatPlainXML_Checked(object sender, RoutedEventArgs e)
        {
            if (!DialogInitialized) return;
            OptionColumnDefinitions.IsChecked = false;
            OptionColumnDefinitions.IsEnabled = false;
        }

        private void FormatAtomPub_Checked(object sender, RoutedEventArgs e)
        {
            if (!DialogInitialized) return;
            OptionColumnDefinitions.IsChecked = false;
            OptionColumnDefinitions.IsEnabled = false;
        }

    }
}
