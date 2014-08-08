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
    public partial class ContentTypesDialog : Window
    {
        private int nextTypeId = 1;
        private Dictionary<int, ListViewItem> listViewItems = new Dictionary<int, ListViewItem>();
        private Dictionary<int, StackPanel> rowPanels = new Dictionary<int, StackPanel>();
        private Dictionary<int, CheckBox> checkboxes = new Dictionary<int, CheckBox>();

        public ContentTypesDialog()
        {
            InitializeComponent();
            CenterWindowOnScreen();
        }

        public void LoadContentTypes(Dictionary<String, String> contentTypes)
        {
            ContentTypeListView.Items.Clear();
            foreach(KeyValuePair<String, String> ct in contentTypes)
            {
                AddContentType(ct.Key, ct.Value);
            }
        }

        private StackPanel AddContentType(String fileType, String contentType)
        {
            StackPanel sp = new StackPanel()
            {
                Name = "Row" + nextTypeId.ToString(),
                Orientation = Orientation.Horizontal
            };

            CheckBox cb = new CheckBox()
            {
                Name = "CheckBox" + nextTypeId.ToString(),
                Width = 24,
                Margin = new Thickness(0, 4, 0, 0),
                Background = new SolidColorBrush(Colors.LightYellow)
            };
            sp.Children.Add(cb);

            TextBox tb1 = new TextBox()
            {
                Name = "TextBoxFileType" + nextTypeId.ToString(),
                Text = fileType,
                Width = 120,
                Margin = new Thickness(0, 0, 16, 0),
                Background = new SolidColorBrush(Colors.LightYellow)
            };
            sp.Children.Add(tb1);
            RegisterName(tb1.Name, tb1);

            TextBox tb2 = new TextBox()
            {
                Name = "TextBoxContentType" + nextTypeId.ToString(),
                Text = contentType,
                Width = 270,
                Background = new SolidColorBrush(Colors.LightYellow)
            };
            sp.Children.Add(tb2);
            RegisterName(tb2.Name, tb2);

            ListViewItem lvi = new ListViewItem()
            {
                Content = sp
            };

            ContentTypeListView.Items.Add(lvi);

            rowPanels.Add(nextTypeId, sp);
            checkboxes.Add(nextTypeId, cb);
            listViewItems.Add(nextTypeId, lvi);

            nextTypeId++;

            return sp;
        }

        // Copy command issued - return 

        private void CmdApply_Click(object sender, RoutedEventArgs e)
        {
 
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

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        // Add a row to the list.

        private void AddRow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StackPanel row = AddContentType(String.Empty, String.Empty);
            ContentTypeListView.ScrollIntoView(ContentTypeListView.Items[ContentTypeListView.Items.Count - 1]);
        }

        private void RemoveSelectedRows_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (listViewItems != null)
            {
                List<int> idsToRemove = new List<int>();

                foreach(KeyValuePair<int, CheckBox> cb in checkboxes)
                {
                    if (cb.Value.IsChecked.Value)
                    {
                        idsToRemove.Add(cb.Key);
                    }
                }

                foreach(int id in idsToRemove)
                {
                    ContentTypeListView.Items.Remove(listViewItems[id]);
                    listViewItems.Remove(id);
                    rowPanels.Remove(id);
                    checkboxes.Remove(id);
                }
            }
        }

        // Return the updated list of content types.

        public Dictionary<String, String> GetContentTypes()
        {
            Dictionary<String, String> contentTypes = new Dictionary<string, string>();

            int id = 0;
            foreach(KeyValuePair<int, StackPanel> ct in rowPanels)
            {
                id = ct.Key;

                TextBox tb1 = ContentTypeListView.FindName("TextBoxFileType" + id.ToString()) as TextBox;
                TextBox tb2 = ContentTypeListView.FindName("TextBoxContentType" + id.ToString()) as TextBox;

                if (tb1 != null && tb2 != null)
                {
                    String fileType = tb1.Text.Trim();
                    String contentType = tb2.Text.Trim();
                    if (!String.IsNullOrEmpty(fileType) && !String.IsNullOrEmpty(contentType) && !contentTypes.ContainsKey(fileType))
                    { 
                        contentTypes.Add(tb1.Text, tb2.Text);
                    }
                }
            }

            return contentTypes;
        }
    }
}
