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
    public partial class EntityQuery : Window
    {
        public List<CheckedListItem> EntityColumns = new List<CheckedListItem>();

        public EntityQuery()
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
            EntityColumns = entityColumns;

            if (EntityColumns != null)
            {
                foreach(CheckedListItem item in EntityColumns)
                {
                    Column1.Items.Add(new ComboBoxItem()
                        {
                            Content = item.Name
                        });
                    Column2.Items.Add(new ComboBoxItem()
                    {
                        Content = item.Name
                    }); 
                    Column3.Items.Add(new ComboBoxItem()
                    {
                        Content = item.Name
                    });
                }
            }
        
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

        }

        // Clear all query conditions.

        private void CmdClearAllConditions_Click(object sender, RoutedEventArgs e)
        {
            Column1.SelectedIndex = 0;
            Column2.SelectedIndex = 0;
            Column3.SelectedIndex = 0;

            Condition1.SelectedIndex = -1;
            Condition2.SelectedIndex = -1;
            Condition3.SelectedIndex = -1;

            AllEntities.IsChecked = true;
        }

        public void SetConditions(String[] names, String[] conditions, String[] values)
        {
            if (names.Length > 0)
            {
                foreach(ComboBoxItem item in Column1.Items)
                {
                    if (item.Content.ToString() == names[0])
                    {
                        Column1.SelectedItem = item;
                        break;
                    }
                }
                foreach (ComboBoxItem item in Condition1.Items)
                {
                    if (item.Content.ToString() == conditions[0])
                    {
                        Condition1.SelectedItem = item;
                        break;
                    }
                }
                Value1.Text = values[0];
            }

            if (names.Length > 1)
            {
                foreach (ComboBoxItem item in Column2.Items)
                {
                    if (item.Content.ToString() == names[1])
                    {
                        Column2.SelectedItem = item;
                        break;
                    }
                }
                foreach (ComboBoxItem item in Condition2.Items)
                {
                    if (item.Content.ToString() == conditions[1])
                    {
                        Condition2.SelectedItem = item;
                        break;
                    }
                }
                Value2.Text = values[1];
            }

            if (names.Length > 2)
            {
                foreach (ComboBoxItem item in Column3.Items)
                {
                    if (item.Content.ToString() == names[2])
                    {
                        Column2.SelectedItem = item;
                        break;
                    }
                }
                foreach (ComboBoxItem item in Condition3.Items)
                {
                    if (item.Content.ToString() == conditions[2])
                    {
                        Condition3.SelectedItem = item;
                        break;
                    }
                }
                Value3.Text = values[2];
            }
        }

        private void AllEntities_Checked(object sender, RoutedEventArgs e)
        {
            if (QueryPanel != null)
            {
                QueryPanel.Visibility = Visibility.Hidden;
            }
        }

        private void QueryEntities_Checked(object sender, RoutedEventArgs e)
        {
            if (QueryPanel != null)
            {
                QueryPanel.Visibility = Visibility.Visible;
            }
        }
    }
}
