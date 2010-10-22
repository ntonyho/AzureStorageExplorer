﻿using System;
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
    public partial class CopyContainerDialog : Window
    {
        public CopyContainerDialog()
        {
            InitializeComponent();

            DestContainerName.Focus();
        }

        private void CreateContainer_Click(object sender, RoutedEventArgs e)
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
            if (String.IsNullOrEmpty(SourceContainerName.Text))
            {
                MessageBox.Show("A source container name is required", "Source Container Name Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else if (String.IsNullOrEmpty(DestContainerName.Text))
            {
                MessageBox.Show("A destination container name is required", "Destination Container Name Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }
    }
}
