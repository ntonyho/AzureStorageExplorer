using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Analytics;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Data.OData;
using System.ComponentModel;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for StorageView.xaml
    /// </summary>
    public partial class StorageView : UserControl
    {
        #region Class Variables

        public AzureAccount Account = null;
        public String SelectedBlobContainer = null;
        public CloudBlobClient blobClient = null;
        public ObservableCollection<BlobItem> _BlobCollection = new ObservableCollection<BlobItem>();
        public ObservableCollection<BlobItem> BlobCollection { get { return _BlobCollection; } }

        private int NextAction = 1;
        private Dictionary<int, Action> Actions = new Dictionary<int, Action>();

        private String BlobSortHeader;
        private ListSortDirection BlobSortDirection;
        
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        #endregion

        #region Initialization

        public StorageView()
        {
            InitializeComponent();
        }

        //******************
        //*                *
        //*  LoadLeftPane  *
        //*                *
        //******************
        // Load a list of storage containers/queues/tables into the left pane of the storage view.

        public void LoadLeftPane()
        {
            Cursor = Cursors.Wait;

            AccountTitle.Text = Account.Name;

            ClearMainPane();

            TreeViewItem blobSection = new TreeViewItem()
            {
                Header = "Blob Containers",
                Tag = new OutlineItem()
                {
                    ItemType = 100,
                    Container = null
                }
            };


            //TreeViewItem blobSection = new TreeViewItem() { Header = "Blob Containers", Tag = 100 };
            TreeViewItem queueSection = new TreeViewItem() { Header = "Queues", Tag = 200 };
            TreeViewItem tableSection = new TreeViewItem() { Header = "Tables", Tag = 300 };

            AccountTreeView.Items.Clear();

            AccountTreeView.Items.Add(blobSection);
            AccountTreeView.Items.Add(queueSection);
            AccountTreeView.Items.Add(tableSection);

            CloudStorageAccount account = OpenStorageAccount();


            //serviceProperties.Cors.CorsRules.Clear(); // this will delete any existing CORS rules
            //var corsRule = new CorsRule()
            //{
            //    AllowedOrigins = new List<string> { "http://test.local" },
            //    AllowedMethods = CorsHttpMethods.Put,
            //    AllowedHeaders = new List<string> { "x-ms-*", "content-type", "accept" },
            //    ExposedHeaders = new List<string> { "x-ms-*" },
            //    MaxAgeInSeconds = 60 * 60 // for an hour
            //};



            blobClient = account.CreateCloudBlobClient();

            try
            { 
                var serviceProperties = blobClient.GetServiceProperties();

                if (serviceProperties.Cors.CorsRules.Count == 0)
                {
                    ButtonBlobServiceCORSIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/unchecked.png"));
                    ButtonBlobServiceCORSLabel.Text = "CORS";
                }
                else
                {
                    ButtonBlobServiceCORSIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/checked.png"));
                    ButtonBlobServiceCORSLabel.Text = "CORS (" + serviceProperties.Cors.CorsRules.Count.ToString() + ")";
                }
            }
            catch(Exception)
            {
                // Disallowed for developer storage account.
            }

            try
            {
                IEnumerable<CloudBlobContainer> containers = blobClient.ListContainers();
                if (containers != null)
                {
                    if (containers != null)
                    {
                        foreach (CloudBlobContainer container in containers)
                        {
                            StackPanel stack = new StackPanel();
                            stack.Orientation = Orientation.Horizontal;

                            Image cloudFolderImage = new Image();
                            cloudFolderImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_folder.png"));
                            cloudFolderImage.Height = 24;

                            Label label = new Label();
                            label.Content = container.Name;

                            stack.Children.Add(cloudFolderImage);
                            stack.Children.Add(label);

                            TreeViewItem blobItem = new TreeViewItem()
                            {
                                Header = stack,
                                Tag = new OutlineItem()
                                {
                                    ItemType = 101,
                                    Container = container.Name,
                                    Permissions = container.GetPermissions()
                                }
                            };
                            blobSection.Items.Add(blobItem);
                        }
                    }
                }
                blobSection.Header = "Blob Containers (" + containers.Count().ToString() + ")";

                blobSection.IsExpanded = true;

                CloudQueueClient queueClient = account.CreateCloudQueueClient();
                IEnumerable<CloudQueue> queues = queueClient.ListQueues();

                if (queues != null)
                {
                    foreach (CloudQueue queue in queues)
                    {
                        StackPanel stack = new StackPanel();
                        stack.Orientation = Orientation.Horizontal;

                        Image cloudFolderImage = new Image();
                        cloudFolderImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_queue.png"));
                        cloudFolderImage.Height = 24;

                        Label label = new Label();
                        label.Content = queue.Name;

                        stack.Children.Add(cloudFolderImage);
                        stack.Children.Add(label);

                        queueSection.Items.Add(new TreeViewItem()
                        {
                            Header = stack,
                            Tag = new OutlineItem()
                            {
                                ItemType = 201,
                                Container = queue.Name
                            }
                        });
                    }
                }
                queueSection.Header = "Queues (" + queues.Count().ToString() + ")";

                // OData version number occurs here:
                // Could not load file or assembly 'Microsoft.Data.OData, Version=5.6.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35' or one of its dependencies. The system cannot find the file specified.

                CloudTableClient tableClient = account.CreateCloudTableClient();
                IEnumerable<CloudTable> tables = tableClient.ListTables();
                if (tables != null)
                {
                    foreach (CloudTable table in tables)
                    {
                        StackPanel stack = new StackPanel();
                        stack.Orientation = Orientation.Horizontal;

                        Image cloudFolderImage = new Image();
                        cloudFolderImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_table.png"));
                        cloudFolderImage.Height = 24;

                        Label label = new Label();
                        label.Content = table.Name;

                        stack.Children.Add(cloudFolderImage);
                        stack.Children.Add(label);

                        tableSection.Items.Add(new TreeViewItem()
                        {
                            Header = stack,
                            Tag = new OutlineItem()
                            {
                                ItemType = 301,
                                Container = table.Name
                            }
                        });
                    }
                }
                tableSection.Header = "Tables (" + tables.Count().ToString() + ")";
            }
            catch(Exception ex)
            {

            }

            Cursor = Cursors.Arrow;
        }
        


        // Open and return cloud storage account.

        private CloudStorageAccount OpenStorageAccount()
        {
            CloudStorageAccount account = null;

            if (Account.IsDeveloperAccount)
            {
                account = CloudStorageAccount.DevelopmentStorageAccount;
            }
            else
            {
                account = new CloudStorageAccount(new StorageCredentials(Account.Name, Account.Key), Account.EndpointDomain, Account.UseSSL);
            }

            return account;
        }

        #endregion

        #region Left and Main Pane Interaction Handlers

        //*****************************************
        //*                                       *
        //*  AccountTreeView_SelectedItemChanged  *
        //*                                       *
        //*****************************************
        // Left pane tree item item clicked - an item was selected in the outline. Update main pane with content.

        private void ClearMainPane()
        {
            ContainerToolbarPanel.Visibility = Visibility.Collapsed;
            BlobToolbarPanel.Visibility = Visibility.Collapsed;
            ContainerPanel.Visibility = Visibility.Collapsed;
            ContainerListView.Visibility = Visibility.Collapsed;
            ButtonContainerAccess.Visibility = Visibility.Collapsed;
            ButtonDeleteContainer.Visibility = Visibility.Collapsed;
            ButtonBlobServiceCORS.Visibility = Visibility.Collapsed;
        }

        private void AccountTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (AccountTreeView.SelectedItem == null) return;
            TreeViewItem item = AccountTreeView.SelectedItem as TreeViewItem;

            ClearMainPane();

            if (item == null || !(item.Tag is OutlineItem)) return;

            ContainerTitle.Text = String.Empty;

            _BlobCollection.Clear();
            
            OutlineItem outlineItem = item.Tag as OutlineItem;

            switch (outlineItem.ItemType)
            {
                case 100:   // Blob Containers section
                    ContainerToolbarPanel.Visibility = Visibility.Visible;
                    ButtonBlobServiceCORS.Visibility = Visibility.Visible;
                    break;
                case 101:   // Blob container
                    ContainerImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_folder.png"));
                    ContainerPanel.Visibility = Visibility.Visible;
                    //ContainerPanel.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFD966"));
                    ContainerTitle.Text = outlineItem.Container;
                    ContainerType.Text = "blob container";
                    ContainerDetails.Text = String.Empty;
                    SelectedBlobContainer = outlineItem.Container;

                    ButtonDeleteContainer.Visibility = Visibility.Visible;
                    ButtonContainerAccess.Visibility = Visibility.Visible;

                    if (outlineItem.Permissions.PublicAccess ==  BlobContainerPublicAccessType.Container)
                    {
                        ButtonContainerAccessIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/public.png"));
                    }
                    else if (outlineItem.Permissions.PublicAccess ==  BlobContainerPublicAccessType.Blob)
                    {
                        ButtonContainerAccessIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/public.png"));
                    }
                    else
                    {
                        ButtonContainerAccessIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/private.png"));
                    }

                    ShowBlobContainer(SelectedBlobContainer);
                    break;
                case 200:   // Queues section
                    break;
                case 201:   // Queue
                    ContainerImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_queue.png"));
                    //ContainerPanel.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#7BDBA2"));
                    ContainerTitle.Text = outlineItem.Container;
                    ContainerType.Text = "queue";
                    break;
                case 300:   // Tables section
                    break;
                case 301:   // Table
                    ContainerImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_table.png"));
                    //ContainerPanel.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#9FCFFA"));
                    ContainerTitle.Text = outlineItem.Container;
                    ContainerType.Text = "table";
                    break;
                default:
                    break;
            }
        }

        //*******************************************
        //*                                         *
        //*  ContainerListView_ColumnHeaderClicked  *
        //*                                         *
        //*******************************************
        // Main pane container list column clicked - sort and re-display the blob list.

        private void ContainerListView_ColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;

                    BlobSortHeader = header;
                    BlobSortDirection = direction;

                    SortBlobList();

                    //Sort(header, direction);


                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header 
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }


                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        //******************
        //*                *
        //*  SortBlobList  *
        //*                *
        //******************
        // Sort the blob list by selected column / direction.

        private void SortBlobList()
        {
            ContainerListView.ItemsSource = null;

            IEnumerable<BlobItem> x;
            switch (BlobSortHeader)
            {
                case "Name":
                    x = from b in _BlobCollection select b;
                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.Name, StringComparer.CurrentCultureIgnoreCase));
                    }
                    else
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.Name, StringComparer.CurrentCultureIgnoreCase));
                    }
                    break;
                case "Type":
                    x = from b in _BlobCollection select b;
                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.BlobType, StringComparer.CurrentCultureIgnoreCase));
                    }
                    else
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.BlobType, StringComparer.CurrentCultureIgnoreCase));
                    }
                    break;
                case "Length":
                    x = from b in _BlobCollection select b;
                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.Length));
                    }
                    else
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.Length));
                    }
                    break;
                case "Content Type":
                    x = from b in _BlobCollection select b;
                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.ContentType, StringComparer.CurrentCultureIgnoreCase));
                    }
                    else
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.ContentType, StringComparer.CurrentCultureIgnoreCase));
                    }
                    break;
                case "Encoding":
                    x = from b in _BlobCollection select b;
                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.Encoding, StringComparer.CurrentCultureIgnoreCase));
                    }
                    else
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.Encoding, StringComparer.CurrentCultureIgnoreCase));
                    }
                    break;
                case "Last Modified":
                    x = from b in _BlobCollection select b;
                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.LastModified));
                    }
                    else
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.LastModified));
                    }
                    break;
                case "ETag":
                    x = from b in _BlobCollection select b;
                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.ETag, StringComparer.CurrentCultureIgnoreCase));
                    }
                    else
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.ETag, StringComparer.CurrentCultureIgnoreCase));
                    }
                    break;
                case "Copy State":
                    x = from b in _BlobCollection select b;
                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.CopyState, StringComparer.CurrentCultureIgnoreCase));
                    }
                    else
                    {
                        _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.CopyState, StringComparer.CurrentCultureIgnoreCase));
                    }
                    break;
                default:
                    break;
            }

            ContainerListView.ItemsSource = BlobCollection;
        }

        #endregion

        #region Toolbar Button Handlers

        //**************************
        //*                        *
        //*  AccountRefresh_Click  *
        //*                        *
        //**************************
        // Refresh storage account.

        private void AccountRefresh_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            LoadLeftPane();

            Cursor = Cursors.Arrow;
        }


        //************************
        //*                      *
        //*  NewContainer_Click  *
        //*                      *
        //************************
        // Create a new blob container.

        private void NewContainer_Click(object sender, RoutedEventArgs e)
        {
            NewContainerDialog dlg = new NewContainerDialog();

            if (dlg.ShowDialog().Value)
            {
                bool isError = false;
                String errorMessage = null;

                String containerName = dlg.Container.Text;

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_NEW_CONTAINER,
                    IsCompleted = false,
                    Message = "Creating container " + containerName
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                // Execute background task to create the container.

                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (blobClient == null)
                        {
                            CloudStorageAccount account = OpenStorageAccount();
                            blobClient = account.CreateCloudBlobClient();
                        }
                        CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                        container.CreateIfNotExists();
                    }
                    catch (Exception ex)
                    {
                        isError = true;
                        errorMessage = ex.Message;
                    }
                    Actions[action.Id].IsCompleted = true;
                });

                // Task complete - update UI.

                task.ContinueWith((t) =>
                {
                    LoadLeftPane();
                    UpdateStatus();

                    if (isError)
                    {
                        MessageBox.Show(errorMessage, "Exception Creating Blob Container");
                    }
                    //else
                    //{
                    //    SelectContainer(containerName);
                    //}
                }, TaskScheduler.FromCurrentSynchronizationContext());

            }
        }


        //***************************
        //*                         *
        //*  DeleteContainer_Click  *
        //*                         *
        //***************************
        // Delete selected blob container.

        private void DeleteContainer_Click(object sender, RoutedEventArgs e)
        {
            String message = "To delete a blob container, select a container and then clck the Delete Container button.";

            if (AccountTreeView.SelectedItem == null || !(AccountTreeView.SelectedItem is TreeViewItem))
            {
                MessageBox.Show(message, "Container Selection Required");
                return;
            }

            TreeViewItem tvi = AccountTreeView.SelectedItem as TreeViewItem;

            if (!(tvi.Tag is OutlineItem))
            {
                MessageBox.Show(message, "Container Selection Required");
                return;
            }

            OutlineItem item = tvi.Tag as OutlineItem;

            if (item.ItemType != 101)
            {
                MessageBox.Show(message, "Container Selection Required");
                return;
            }

            String containerName = SelectedBlobContainer;

            if (MessageBox.Show("Are you SURE you want to delete blob container " + SelectedBlobContainer + "?\n\nThe container and all blobs it contains will be permanently deleted",
                "Confirm Container Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                bool isError = false;
                String errorMessage = null;

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_DELETE_CONTAINER,
                    IsCompleted = false,
                    Message = "Deleting container " + containerName
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                // Execute background task to create the container.

                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (blobClient == null)
                        {
                            CloudStorageAccount account = OpenStorageAccount();
                            blobClient = account.CreateCloudBlobClient();
                        }
                        CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                        container.DeleteIfExists();
                    }
                    catch (Exception ex)
                    {
                        isError = true;
                        errorMessage = ex.Message;
                    }
                    Actions[action.Id].IsCompleted = true;
                });

                // Task complete - update UI.

                task.ContinueWith((t) =>
                {
                    LoadLeftPane();
                    UpdateStatus();

                    if (isError)
                    {
                        MessageBox.Show(errorMessage, "Exception Deleting Blob Container" + containerName);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());

            }
        }

        //private void SelectContainer(String containerName)
        //{
        //    ClearMainPane();
        //    if (AccountTreeView.Items != null && (AccountTreeView.Items[0] as TreeViewItem).Items != null && (AccountTreeView.Items[0] as TreeViewItem).Items.Count > 0)
        //    {
        //        foreach(TreeViewItem tvi in (AccountTreeView.Items[0] as TreeViewItem).Items)
        //        {
        //            if (tvi.Tag is OutlineItem)
        //            {
        //                OutlineItem item = tvi.Tag as OutlineItem;
        //                if (/* item.ItemType==101 && */ item.ItemName == containerName)
        //                {
        //                    tvi.IsSelected = true;
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}

        //****************************
        //*                          *
        //*  BlobUploadButton_Click  *
        //*                          *
        //****************************
        //Upload file(s) to current blob container.

        private void BlobUploadButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box 
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "All files (*.*)|*.*|Image files (*.bmp,*.ico;*.jpg,*.gif,*.png,*.tif)|*.bmp;*.ico;*.jpg;*.jpeg;*.gif;*.png;*.tif"; // Filter files by extension 
            dlg.Multiselect = true;

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                String[] files = dlg.FileNames;
                if (files != null)
                {
                    UploadFiles(files, SelectedBlobContainer);
                }
            }
        }

        //**********************
        //*                    *
        //*  BlobDelete_Click  *
        //*                    *
        //**********************
        // Delete selected blobs

        private void BlobDelete_Click(object sender, RoutedEventArgs e)
        {
            List<String> blobs = new List<string>();

            foreach (BlobItem blob in ContainerListView.SelectedItems)
            {
                blobs.Add(blob.Name);
            }

            int count = blobs.Count();

            if (count == 0)
            {
                MessageBox.Show("No blobs are selected. To delete blobs, select one or more from the list then click the Delete toolbar button.", "Selection Required");
                return;
            }

            String message = "Are you sure you want to delete these " + count.ToString() + " blobs?";
            if (count == 1)
            {
                message = "Are you sure you want to delete this blob?";
            }

            message = message + "\n";
            int n = 0;
            foreach (String name in blobs)
            {
                n++;
                if (n < 10)
                {
                    message = message + "\n" + name;
                }
                if (n == 10)
                {
                    message = message + "\n(" + (count - 10).ToString() + " more)";
                }
            }

            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(message, "Confirm Delete", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                message = null;
                if (ContainerListView.SelectedItems.Count == 1)
                {
                    message = "Deleting blob " + ContainerListView.SelectedItems[0] + " from container " + SelectedBlobContainer;
                }
                else
                {
                    message = "Deleting " + ContainerListView.SelectedItems.Count.ToString() + " blobs from container " + SelectedBlobContainer;
                }

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_DELETE_BLOBS,
                    IsCompleted = false,
                    Message = message
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                Cursor = Cursors.Wait;

                Task task = Task.Factory.StartNew(() =>
                {
                    CloudBlobContainer container = blobClient.GetContainerReference(SelectedBlobContainer);

                    int deletedCount = 0;
                    foreach (String blobName in blobs)
                    {
                        ICloudBlob blob = container.GetBlobReferenceFromServer(blobName);
                        if (blob.BlobType == BlobType.BlockBlob)
                        {
                            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                            if (blockBlob.DeleteIfExists())
                            {
                                deletedCount++;
                            }
                        }
                        else if (blob.BlobType == BlobType.PageBlob)
                        {
                            CloudPageBlob pageBlob = container.GetPageBlobReference(blobName);
                            if (pageBlob.DeleteIfExists())
                            {
                                deletedCount++;
                            }
                        }
                    }

                    Actions[action.Id].IsCompleted = true;
                });

                task.ContinueWith((t) =>
                {
                    UpdateStatus();

                    Cursor = Cursors.Arrow;

                    ShowBlobContainer(SelectedBlobContainer);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        //*************************
        //*                       *
        //*  BlobSelectAll_Click  *
        //*                       *
        //*************************
        // Select all blobs in the container.

        private void BlobSelectAll_Click(object sender, RoutedEventArgs e)
        {
            ContainerListView.SelectAll();
        }

        //******************************
        //*                            *
        //*  BlobClearSelection_Click  *
        //*                            *
        //******************************
        // Clear selection

        private void BlobClearSelection_Click(object sender, RoutedEventArgs e)
        {
            ContainerListView.SelectedIndex = -1;
        }

        //******************************
        //*                            *
        //*  BlobDownloadButton_Click  *
        //*                            *
        //******************************
        // Download blob(s) toolbar button handler.

        private void BlobDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            List<String> blobs = new List<string>();

            foreach (BlobItem blob in ContainerListView.SelectedItems)
            {
                blobs.Add(blob.Name);
            }

            if (blobs.Count() == 0)
            {
                MessageBox.Show("No blobs are selected. To download blobs, first select one or more blobs then click the Download toolbar button.", "Selection Required");
                return;
            }

            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose Blob Download Folder";
            dlg.IsFolderPicker = true;
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

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                String folder = dlg.FileName;
                DownloadFiles(SelectedBlobContainer, blobs.ToArray(), folder);
            }
        }

        //********************
        //*                  *
        //*  BlobCopy_Click  *
        //*                  *
        //********************
        // Make a copy of the selected blob under a new name - in the same container, in a different container, or in a different storage account.
        // IN PROGRESS

        private void BlobCopy_Click(object sender, RoutedEventArgs e)
        {
            bool Success = false;
            String ErrorMessage = null;
            bool overwrite = false;

            CloudStorageAccount sourceAccount = OpenStorageAccount();
            CloudStorageAccount destAccount = sourceAccount;

            // Validate a single blob has been selected.

            if (ContainerListView.SelectedItems.Count != 1)
            {
                MessageBox.Show("In order to copy a blob, please select one blob then click the Copy toolbar button", "Single Selection Requireed");
                return;
            }

            // Display the copy blob dialog.

            CopyBlob dlg = new CopyBlob();
            dlg.SourceAccount.Text = this.Account.Name;
            dlg.DestAccount.Text = this.Account.Name;
            dlg.SourceContainer.Text = SelectedBlobContainer;
            dlg.DestContainer.Text = SelectedBlobContainer;
            dlg.SourceBlob.Text = (ContainerListView.SelectedItems[0] as BlobItem).Name;
            dlg.DestBlob.Text = dlg.SourceBlob.Text;

            if (dlg.ShowDialog().Value)
            {
                // Proceeding with copy - perform background blob copy.

                overwrite = dlg.Overwrite.IsChecked.Value;

                String sourceAccountName = dlg.SourceAccount.Text;
                String destAccountName = dlg.DestAccount.Text;

                String blobName = dlg.SourceBlob.Text;
                String destName = dlg.DestBlob.Text;

                String sourceContainerName = dlg.SourceContainer.Text;
                String destContainerName = dlg.DestContainer.Text;

                String message = "Copying blob " + dlg.SourceBlob.Text + " to " + destName + " in container " + SelectedBlobContainer;

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_COPY_BLOB,
                    IsCompleted = false,
                    Message = message
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                Cursor = Cursors.Wait;

                Task task = Task.Factory.StartNew(() =>
                {
                    CloudBlobContainer container = blobClient.GetContainerReference(SelectedBlobContainer);

                    if (destAccountName != sourceAccountName)
                    {
                        bool accountFound = false;
                        if (MainWindow.Accounts != null)
                        {
                            foreach (AzureAccount account in MainWindow.Accounts)
                            {
                                if (account.Name == destAccountName)
                                {
                                    accountFound = true;
                                    if (account.IsDeveloperAccount)
                                    {
                                        destAccount = CloudStorageAccount.DevelopmentStorageAccount;
                                    }
                                    else
                                    {
                                        destAccount = new CloudStorageAccount(new StorageCredentials(account.Name, account.Key), account.EndpointDomain, account.UseSSL);
                                    }
                                }
                            }
                        }
                        if (!accountFound)
                        {
                            // TODO: fail with error message
                            return;
                        }
                    }

                    CloudBlobClient destBlobClient = destAccount.CreateCloudBlobClient();

                    CloudBlobContainer sourceContainer = blobClient.GetContainerReference(sourceContainerName);
                    CloudBlobContainer destContainer = destBlobClient.GetContainerReference(destContainerName);

                    ICloudBlob sourceBlob = sourceContainer.GetBlobReferenceFromServer(blobName);
                    
                    bool proceedWithCopy = true;

                    try
                    {
                        if (overwrite)
                        {
                            if (sourceBlob.BlobType == BlobType.BlockBlob)
                            {
                                CloudBlockBlob targetBlockBlob = destContainer.GetBlockBlobReference(destName);
                                targetBlockBlob.DeleteIfExists(); ;
                                Success = true;
                            }
                            else if (sourceBlob.BlobType == BlobType.PageBlob)
                            {
                                CloudPageBlob targetPageBlob = destContainer.GetPageBlobReference(destName);
                                targetPageBlob.DeleteIfExists(); ;
                                Success = true;
                            }
                        }
                        else
                        {
                            if (sourceBlob.BlobType == BlobType.BlockBlob)
                            {
                                CloudBlockBlob targetBlockBlob = destContainer.GetBlockBlobReference(destName);
                                if (targetBlockBlob.Exists())
                                {
                                    Success = false;
                                    proceedWithCopy = false;
                                    ErrorMessage = "Cannot copy blob " + sourceContainer + "/" + blobName + " to " + destContainer + "/" + destName + " - destination blob already exists";
                                }
                            }
                            else if (sourceBlob.BlobType == BlobType.PageBlob)
                            {
                                CloudPageBlob targetPageBlob = destContainer.GetPageBlobReference(destName);
                                if (targetPageBlob.Exists())
                                {
                                    Success = false;
                                    proceedWithCopy = false;
                                    ErrorMessage = "Cannot copy blob " + sourceContainer + "/" + blobName + " to " + destContainer + "/" + destName + " - destination blob already exists";
                                }
                            }
                        }

                        if (proceedWithCopy)
                        {
                            if (sourceBlob.BlobType==BlobType.BlockBlob)
                            {
                                CloudBlockBlob sourceBlockBlob = sourceContainer.GetBlockBlobReference(blobName);
                                CloudBlockBlob targetBlockBlob = destContainer.GetBlockBlobReference(destName);
                                targetBlockBlob.StartCopyFromBlob(sourceBlockBlob);
                                Success = true;
                            }
                            else if (sourceBlob.BlobType == BlobType.PageBlob)
                            {
                                CloudPageBlob sourcePageBlob = sourceContainer.GetPageBlobReference(blobName);
                                CloudPageBlob targetPageBlob = destContainer.GetPageBlobReference(destName);
                                targetPageBlob.StartCopyFromBlob(sourcePageBlob);
                                Success = true;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Success = false;
                        ErrorMessage = "Error attempting to copy blob " + sourceContainer + "/" + blobName + " to " + destContainer + "/" + destName + " - " + ex.Message;
                    }

                    Actions[action.Id].IsCompleted = true;
                });

                task.ContinueWith((t) =>
                {
                    UpdateStatus();

                    if (!Success)
                    {
                        MessageBox.Show(ErrorMessage, "Error Copying Blob");
                    }

                    Cursor = Cursors.Arrow;

                    ShowBlobContainer(SelectedBlobContainer);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        //***********************
        //*                     *
        //*  BlobRefresh_Click  *
        //*                     *
        //***********************
        // Refresh the list of blobs.

        private void BlobRefresh_Click(object sender, RoutedEventArgs e)
        {
            ContainerListView.ItemsSource = null;
            ShowBlobContainer(SelectedBlobContainer);
        }

        //***************************
        //*                         *
        //*  ContainerAccess_Click  *
        //*                         *
        //***************************
        // Change container access level.

        private void ContainerAccess_Click(object sender, RoutedEventArgs e)
        {
            ContainerSecurity dlg = new ContainerSecurity();

            // Set up Access Level tab

            dlg.ContainerName.Text = SelectedBlobContainer;

            TreeViewItem tvi = AccountTreeView.SelectedItem as TreeViewItem;
            if (tvi == null) return;

            OutlineItem item = tvi.Tag as OutlineItem;
            if (item == null) return;

            if (item.Permissions == null) return;

            switch (item.Permissions.PublicAccess)
            {
                case BlobContainerPublicAccessType.Container:
                    dlg.AccessContainer.IsChecked = true;
                    break;
                case BlobContainerPublicAccessType.Blob:
                    dlg.AccessBlob.IsChecked = true;
                    break;
                case BlobContainerPublicAccessType.Off:
                    dlg.AccessNone.IsChecked = true;
                    break;
            }

            // Set up Shared Access Signatures tab

            if (blobClient == null)
            {
                CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(Account.Name, Account.Key), Account.EndpointDomain, Account.UseSSL);
                blobClient = account.CreateCloudBlobClient();
            }

            CloudBlobContainer container = blobClient.GetContainerReference(SelectedBlobContainer);

            String blobName = null;
            if (ContainerListView.SelectedItem != null)
            {
                BlobItem blobItem = ContainerListView.SelectedItem as BlobItem;
                if (blobItem != null)
                {
                    blobName = blobItem.Name;
                }
            }

            dlg.SetContainer(blobClient, container, blobName);

            if (dlg.ShowDialog().Value)
            {
                if (dlg.AccessLevelModified)
                {
                    bool isError = false;
                    String errorMessage = null;

                    int accessLevel = 0;

                    if (dlg.AccessContainer.IsChecked.Value) accessLevel = 1;
                    if (dlg.AccessBlob.IsChecked.Value) accessLevel = 2;
                    if (dlg.AccessNone.IsChecked.Value) accessLevel = 3;

                    String containerName = dlg.ContainerName.Text;

                    Action action = new Action()
                    {
                        Id = NextAction++,
                        ActionType = Action.ACTION_CONTAINER_ACCESS_LEVEL,
                        IsCompleted = false,
                        Message = "Setting container " + containerName + " access level"
                    };
                    Actions.Add(action.Id, action);

                    UpdateStatus();

                    // Execute background task to create the container.

                    Task task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            if (blobClient == null)
                            {
                                CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(Account.Name, Account.Key), Account.EndpointDomain, Account.UseSSL);
                                blobClient = account.CreateCloudBlobClient();
                            }
                            //CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                            BlobContainerPermissions permissions = container.GetPermissions();
                            switch (accessLevel)
                            {
                                case 1:
                                    permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                                    break;
                                case 2:
                                    permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
                                    break;
                                case 3:
                                    permissions.PublicAccess = BlobContainerPublicAccessType.Off;
                                    break;
                            }
                            item.Permissions = permissions;
                            container.SetPermissions(permissions);

                        }
                        catch (Exception ex)
                        {
                            isError = true;
                            errorMessage = ex.Message;
                        }
                        Actions[action.Id].IsCompleted = true;
                    });

                    // Task complete - update UI.

                    task.ContinueWith((t) =>
                    {
                        switch (accessLevel)
                        {
                            case 1:
                                ButtonContainerAccessIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/public.png"));
                                break;
                            case 2:
                                ButtonContainerAccessIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/public.png"));
                                break;
                            case 3:
                                ButtonContainerAccessIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/private.png"));
                                break;
                        }

                        UpdateStatus();

                        if (isError)
                        {
                            MessageBox.Show(errorMessage, "Exception Setting Blob Container Permissions");
                        }
                        //else
                        //{
                        //    SelectContainer(containerName);
                        //}
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }

            return;
        }


        //***************************
        //*                         *
        //*  BlobServiceCORS_Click  *
        //*                         *
        //***************************
        // Configure Blob Service CORS rules.

        private void BlobServiceCORS_Click(object sender, RoutedEventArgs e)
        {
            BlobServiceCORSDialog dlg = new BlobServiceCORSDialog();

            // Load dialog with current blob servicec CORS rules.

            if (blobClient == null)
            {
                CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(Account.Name, Account.Key), Account.UseSSL);
                blobClient = account.CreateCloudBlobClient();
            }

            var serviceProperties = blobClient.GetServiceProperties();

            ObservableCollection<CORSRule> rules = new ObservableCollection<CORSRule>();

            foreach(CorsRule rule in serviceProperties.Cors.CorsRules)
            {
                rules.Add(new CORSRule(rule));
            }

            dlg.SetRules(rules);

            // Display dialog.

            if (dlg.ShowDialog().Value)
            {
                // Update blob service CORS rules.

                Cursor = Cursors.Wait;

                serviceProperties.Cors.CorsRules.Clear();
                foreach (CORSRule rule in dlg.Rules)
                {
                    serviceProperties.Cors.CorsRules.Add(rule.ToCorsRule());
                }
                blobClient.SetServiceProperties(serviceProperties);

                // Update Configure CORS button icon and label.

                if (serviceProperties.Cors.CorsRules.Count == 0)
                {
                    ButtonBlobServiceCORSIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/unchecked.png"));
                    ButtonBlobServiceCORSLabel.Text = "CORS";
                }
                else
                {
                    ButtonBlobServiceCORSIcon.Source = new BitmapImage(new Uri("pack://application:,,/Images/checked.png"));
                    ButtonBlobServiceCORSLabel.Text = "CORS (" + serviceProperties.Cors.CorsRules.Count.ToString() + ")";
                }

                Cursor = Cursors.Arrow;

                return;
            }
        }


        //*******************
        //*                 *
        //*  BlobNew_Click  *
        //*                 *
        //*******************
        // Create a new blob.

        private void BlobNew_Click(object sender, RoutedEventArgs e)
        {
            NewBlobDialog dlg = new NewBlobDialog();
            if (dlg.ShowDialog().Value)
            {
                String blobName = dlg.BlobName.Text;
                String blobText = dlg.BlobText.Text;

                try
                {
                    Cursor = Cursors.Wait;

                    if (blobClient == null)
                    {
                        CloudStorageAccount account = OpenStorageAccount();
                        blobClient = account.CreateCloudBlobClient();
                    }

                    CloudBlobContainer container = blobClient.GetContainerReference(SelectedBlobContainer);

                    if (dlg.BlobTypeBlock.IsChecked.Value)
                    {
                        // Create a block blob.

                        CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

                        if (blob.Exists())
                        {
                            Cursor = Cursors.Arrow;
                            MessageBox.Show("The blob '" + blobName + "' already exists.", "Blob Already Exists");
                            return;
                        }

                        blob.UploadText(blobText);

                        ShowBlobContainer(SelectedBlobContainer);
                    }
                    else
                    {
                        // Create a page blob.

                        CloudPageBlob blob = container.GetPageBlobReference(blobName);

                        if (blob.Exists())
                        {
                            Cursor = Cursors.Arrow;
                            MessageBox.Show("The blob '" + blobName + "' already exists.", "Blob Already Exists");
                            return;
                        }

                        //var cond = AccessCondition. .GenerateEmptyCondition; //.GenerateIfNotModifiedSinceCondition(timeOffset);
                        var options = new BlobRequestOptions();
                        blob.Create(dlg.PageBlobSize, null, options);

                        ShowBlobContainer(SelectedBlobContainer);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("An error occurred creating the blob '" + blobName + ":\n\n" + ex.Message, "Error Creating Blob");
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }


        #endregion

        #region Update UI

        //******************
        //*                *
        //*  UpdateStatus  *
        //*                *
        //******************
        // Update status messages. Call from UI thread.

        public void UpdateStatus()
        {
            int count = 0;
            StatusMessage.Inlines.Clear();
            if (Actions != null)
            {
                foreach (KeyValuePair<int, Action> action in Actions)
                {
                    if (!action.Value.IsCompleted)
                    {
                        if (count > 0)
                        {
                            StatusMessage.Inlines.Add(new LineBreak());
                        }

                        Run run = new Run(action.Value.Message);
                        StatusMessage.Inlines.Add(run);
                        count++;
                    }
                }
            }
            if (count == 0)
            {
                StatusMessage.Visibility = Visibility.Collapsed;
                //UpdateLayout();
            }
            else
            {
                StatusMessage.Visibility = Visibility.Visible;
                //UpdateLayout();
            }
        }

        //***********************
        //*                     *
        //*  ShowBlobContainer  *
        //*                     *
        //***********************
        // Get and show blobs in selected blob container. Call from UI thread.

        public void ShowBlobContainer(String containerName) //, CancellationToken token, TaskScheduler uiTask)
        {
            Cursor = Cursors.Wait;
            ContainerListView.ItemsSource = BlobCollection;

            int containerCount = 0;
            long containerSize = 0;
            _BlobCollection.Clear();
            ContainerListView.Visibility = Visibility.Visible;
            ContainerToolbarPanel.Visibility = Visibility.Visible;
            BlobToolbarPanel.Visibility = Visibility.Visible;
            IEnumerable<IListBlobItem> blobs = blobClient.GetContainerReference(containerName).ListBlobs();
            if (blobs != null)
            {
                foreach (IListBlobItem item in blobs)
                {
                    if (item.GetType() == typeof(CloudBlobDirectory))
                    {
                    }
                    else if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        containerCount++;
                        CloudBlockBlob blockBlob = item as CloudBlockBlob;
                        _BlobCollection.Add(new BlobItem()
                        {
                            Name = blockBlob.Name,
                            BlobType = "Block",
                            ContentType = blockBlob.Properties.ContentType,
                            Encoding = blockBlob.Properties.ContentEncoding,
                            Length = blockBlob.Properties.Length,
                            LengthText = LengthText(blockBlob.Properties.Length),
                            ETag = blockBlob.Properties.ETag,
                            LastModified = blockBlob.Properties.LastModified.Value.DateTime,
                            LastModifiedText = blockBlob.Properties.LastModified.Value.ToString(),
                            CopyState = CopyStateText(blockBlob.CopyState)
                        });
                        containerSize += blockBlob.Properties.Length;
                    }
                    else if (item.GetType() == typeof(CloudPageBlob))
                    {
                        containerCount++;
                        CloudPageBlob pageBlob = item as CloudPageBlob;
                        _BlobCollection.Add(new BlobItem()
                        {
                            Name = pageBlob.Name,
                            BlobType = "Page",
                            ContentType = pageBlob.Properties.ContentType,
                            Encoding = pageBlob.Properties.ContentEncoding,
                            Length = pageBlob.Properties.Length,
                            LengthText = LengthText(pageBlob.Properties.Length),
                            ETag = pageBlob.Properties.ETag,
                            LastModified = pageBlob.Properties.LastModified.Value.DateTime,
                            LastModifiedText = pageBlob.Properties.LastModified.Value.ToString(),
                            CopyState = CopyStateText(pageBlob.CopyState)
                        });
                        containerSize += pageBlob.Properties.Length;
                    }
                } // end foreach

                SortBlobList();

                ContainerDetails.Text = "(" + containerCount.ToString() + " blobs, " + LengthText(containerSize) + ")  as of " + DateTime.Now.ToString();

                Cursor = Cursors.Arrow;
            }
        }


        //******************************
        //*                            *
        //*  BlobViewProperties_Click  *
        //*                            *
        //******************************
        // Display blob properties for selected blob.

        private void BlobViewProperties_Click(object sender, RoutedEventArgs e)
        {
            // Validate a single blob has been selected.

            if (ContainerListView.SelectedItems.Count != 1)
            {
                MessageBox.Show("In order to view or modify a blob's properties, please select one blob then click the View toolbar button", "Single Selection Requireed");
                return;
            }

            String blobName = (ContainerListView.SelectedItems[0] as BlobItem).Name;

            BlobProperties dlg = new BlobProperties();

            CloudBlobContainer container = blobClient.GetContainerReference(SelectedBlobContainer);

            ICloudBlob blob = container.GetBlobReferenceFromServer(blobName);
            if (blob.BlobType == BlobType.BlockBlob)
            {
                //CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                CloudBlockBlob blockBlob = container.GetBlobReferenceFromServer(blobName) as CloudBlockBlob;
                Microsoft.WindowsAzure.Storage.Blob.BlobProperties props = blockBlob.Properties;
                dlg.ShowBlockBlob(blockBlob);
            }
            else if (blob.BlobType == BlobType.PageBlob)
            {
                //CloudPageBlob pageBlob = container.GetPageBlobReference(blobName);
                CloudPageBlob pageBlob = container.GetBlobReferenceFromServer(blobName) as CloudPageBlob;
                Microsoft.WindowsAzure.Storage.Blob.BlobProperties props = pageBlob.Properties;
                dlg.ShowPageBlob(pageBlob);
            }

            if (dlg.ShowDialog().Value)
            {
                // TODO: apply updates
                if (dlg.IsBlobChanged)
                {
                    ShowBlobContainer(SelectedBlobContainer);
                }
            }
        }


        #endregion

        #region Blob Operations

        //*****************
        //*               *
        //*  UploadFiles  *
        //*               *
        //*****************
        // Upload a list of local files up to a blob container. Call from UI thread. Performs heavy lifting in a background task.

        public void UploadFiles(String[] files, String containerName)
        {
            Action action = new Action()
            {
                Id = NextAction++,
                ActionType = Action.ACTION_UPLOAD_BLOBS,
                IsCompleted = false,
                Message = "Uploading " + files.Length.ToString() + " files to container " + containerName
            };
            Actions.Add(action.Id, action);

            UpdateStatus();

            // Execute background task to perform the uploading.

            bool isError = false;
            String errorMessage = null;

            Task task = Task.Factory.StartNew(() =>
            {

                if (files != null)
                {
                    try
                    {
                        CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                        container.CreateIfNotExists();

                        foreach (String file in files)
                        {
                            String blobName = file;
                            int index = blobName.LastIndexOf("\\");
                            if (index != -1)
                            {
                                blobName = blobName.Substring(index + 1);
                            }
                            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

                            // Upload content to the blob, which will create the blob if it does not already exist.
                            //blob.UploadFromFileAsync(file);
                            blob.UploadFromFile(file, System.IO.FileMode.Open);
                        }
                    }
                    catch (Exception ex)
                    {
                        isError = true;
                        errorMessage = ex.Message;
                    }
                }

                Actions[action.Id].IsCompleted = true;
            });

            // Task complete - update UI.

            task.ContinueWith((t) =>
            {
                ShowBlobContainer(containerName);
                UpdateStatus();

                if (isError)
                {
                    MessageBox.Show(errorMessage, "Exception Uploading Files");
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());

        }

        //*******************
        //*                 *
        //*  DownloadFiles  *
        //*                 *
        //*******************
        // Download collection of blobs to a local file folder. Call from UI thread. Performs heavy lifting in a background task.

        public void DownloadFiles(String containerName, String[] blobs, string folder)
        {
            // Create task action.

            String message = null;

            if (blobs.Length == 1)
            {
                message = "Downloading " + blobs.Length.ToString() + " blobs from container " + containerName + " to folder " + folder;
            }
            else
            {
                message = "Downloading blob " + blobs[0] + " from container " + containerName + " to folder " + folder;
            }

            Action action = new Action()
            {
                Id = NextAction++,
                ActionType = Action.ACTION_DOWNLOAD_BLOBS,
                IsCompleted = false,
                Message = message
            };
            Actions.Add(action.Id, action);

            UpdateStatus();

            // Execute background task to perform the downloading.

            Task task = Task.Factory.StartNew(() =>
            {
                if (blobs != null)
                {
                    CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                    container.CreateIfNotExists();

                    foreach (String name in blobs)
                    {
                        String blobName = name;
                        int index = blobName.LastIndexOf("\\");
                        if (index != -1)
                        {
                            blobName = blobName.Substring(index + 1);
                        }
                        CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

                        // Upload content to the blob, which will create the blob if it does not already exist.
                        //blob.UploadFromFileAsync(file);
                        //blob.UploadFromFile(file, System.IO.FileMode.Open);

                        String path = folder + "\\" + blobName;
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }

                        blob.DownloadToFile(path, System.IO.FileMode.CreateNew);
                    }
                }

                Actions[action.Id].IsCompleted = true;
            });

            // Task complete - update UI.

            task.ContinueWith((t) =>
            {
                UpdateStatus();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion

        #region Helper Functions

        //****************
        //*              *
        //*  LengthText  *
        //*              *
        //****************
        // Return length in text form with most appropriate units.

        public String LengthText(long length)
        {
            decimal n = Convert.ToDecimal(length);
            if (length == 1)
            {
                return "1 byte";
            }
            else if (length < 1024)
            {
                return length.ToString() + " bytes";
            }
            else if (length < (1024 * 1024))
            {
                return Math.Round(n / 1024, 2).ToString() + "K";
            }
            else if (length < (1024 * 1024 * 1024))
            {
                return Math.Round(n / (1024 * 1024), 2).ToString() + "M";
            }
            else
            {
                return Math.Round(n / (1024 * 1024 * 1024), 2).ToString() + "G";
            }
        }

        private String CopyStateText(CopyState state)
        {
            if (state == null)
            {
                return String.Empty;
            }
            else
            {
                switch(state.Status)
                {
                    case CopyStatus.Pending:
                        return "Pending";
                    case CopyStatus.Success:
                        return "Success";
                    case CopyStatus.Aborted:
                        return "Aborted";
                    case CopyStatus.Failed:
                        return "Failed";
                    case CopyStatus.Invalid:
                        return "Invalid";
                    default:
                        return "Other";
                }
            }

        }

        #endregion

        #region Menu Item Handlers
        
        private void MenuItem_StorageAccount_ViewConnectionString_Click(object sender, RoutedEventArgs e)
        {
            CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(Account.Name, Account.Key), Account.UseSSL);

            String connectionString = null;
           
            if (this.Account.IsDeveloperAccount)
            {
                connectionString = "UseDevelopmentStorage=true;";
            }
            else
            {
                String protocol = "http";
                if (this.Account.UseSSL)
                {
                    protocol = protocol + "s";
                }
                connectionString = String.Format("DefaultEndpointsProtocol={0};AccountName={1};AccountKey={2};",
                                                    protocol, this.Account.Name, this.Account.Key);
            }

            MessageBox.Show("The connection string for this storage account is (copied to clipboard):\n\n" + connectionString, "Storage Account Connection String");

            Clipboard.SetData(DataFormats.Text, (Object)connectionString);
        }

        #endregion

        private void MenuItem_StorageAccount_CloseTab_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.RemoveActiveTab();
        }

        private void ContainerListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BlobViewProperties_Click(this, null);
        }



    }

}

#if X
            CloudAnalyticsClient analyticsClient = account.CreateCloudAnalyticsClient();
            analyticsClient.GetLogDirectory

                var serviceProperties = blobClient.GetServiceProperties();
    serviceProperties.Cors.CorsRules.Clear(); // this will delete any existing CORS rules
    var corsRule = new CorsRule()
    {
        AllowedOrigins = new List<string> { "http://test.local" },
        AllowedMethods = CorsHttpMethods.Put,
        AllowedHeaders = new List<string> { "x-ms-*", "content-type", "accept" },
        ExposedHeaders = new List<string> { "x-ms-*" },
        MaxAgeInSeconds = 60 * 60 // for an hour
    };
 
    serviceProperties.Cors.CorsRules.Add(corsRule);
    blobClient.SetServiceProperties(serviceProperties);
}
#endif
