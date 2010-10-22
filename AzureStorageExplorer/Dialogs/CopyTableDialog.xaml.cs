using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Neudesic.AzureStorageExplorer.Dialogs
{
    /// <summary>
    /// Interaction logic for CopyContainerDialog.xaml
    /// </summary>
    public partial class CopyTableDialog : Window
    {
        public CopyTableDialog()
        {
            InitializeComponent();

            DestTableName.Focus();
        }

        private void CreateTable_Click(object sender, RoutedEventArgs e)
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
            if (String.IsNullOrEmpty(SourceTableName.Text))
            {
                MessageBox.Show("A source table name is required", "Source Table Name Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else if (String.IsNullOrEmpty(DestTableName.Text))
            {
                MessageBox.Show("A destination table name is required", "Destination Table Name Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }
    }
}
