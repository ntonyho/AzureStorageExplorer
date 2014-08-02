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
    /// Interaction logic for EntityFilter.xaml
    /// </summary>
    public partial class EntityFilter : Window
    {
        public String EntitySortHeader = null;
        public ListSortDirection EntitySortDirection = ListSortDirection.Ascending;
        public List<CheckedListItem> EntityColumns = new List<CheckedListItem>();

        public EntityFilter()
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

        public void SetEntityColumns(List<CheckedListItem> entityColumns)
        {
            ListBoxEntityColumns.ItemsSource = null;

            EntityColumns = entityColumns;

            ListBoxEntityColumns.ItemsSource = EntityColumns;
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
            MaxEntityCount.Text = String.Empty;
            EntityText.Text = String.Empty;

           EntitySortHeader = null;
           EntitySortDirection = ListSortDirection.Ascending;

           if (EntityColumns != null)
           {
               ListBoxEntityColumns.ItemsSource = null;
               foreach(CheckedListItem col in EntityColumns)
               {
                   col.IsChecked = true;
               }
               ListBoxEntityColumns.ItemsSource = EntityColumns;
           }

           MaxEntityCount.Focus();
        }

        private void CmdSelectAllColumns_Click(object sender, RoutedEventArgs e)
        {
            if (EntityColumns != null)
            {
                ListBoxEntityColumns.ItemsSource = null;
                foreach (CheckedListItem col in EntityColumns)
                {
                    col.IsChecked = true;
                }
                ListBoxEntityColumns.ItemsSource = EntityColumns;
            }
        }

        private void CmdClearAllColumns_Click(object sender, RoutedEventArgs e)
        {
            if (EntityColumns != null)
            {
                ListBoxEntityColumns.ItemsSource = null;
                foreach (CheckedListItem col in EntityColumns)
                {
                    col.IsChecked = false;
                }
                ListBoxEntityColumns.ItemsSource = EntityColumns;
            }
        }
    }
}
