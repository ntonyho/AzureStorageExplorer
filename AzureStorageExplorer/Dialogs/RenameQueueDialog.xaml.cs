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
    public partial class RenameQueueDialog : Window
    {
        public RenameQueueDialog()
        {
            InitializeComponent();

            DestQueueName.Focus();
        }

        private void CreateQueue_Click(object sender, RoutedEventArgs e)
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
            if (String.IsNullOrEmpty(SourceQueueName.Text))
            {
                MessageBox.Show("A source queue name is required", "Source Queue Name Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else if (String.IsNullOrEmpty(DestQueueName.Text))
            {
                MessageBox.Show("A destination queue name is required", "Destination Queue Name Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }
    }
}
