using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
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
using System.Web.Script.Serialization;

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for StorageView.xaml
    /// </summary>
    public partial class StorageView : UserControl
    {
        #region Class Variables

        public const String NULL_VALUE = "NULL ";

        public int LastItemType = 0;

        public AzureAccount Account = null;
        public String SelectedBlobContainer = null;
        public String SelectedQueueContainer = null;
        public String SelectedTableContainer = null;
        public CloudBlobClient blobClient = null;
        public CloudTableClient tableClient = null;
        public CloudQueueClient queueClient = null;

        public ObservableCollection<BlobItem> _BlobCollection = new ObservableCollection<BlobItem>();
        public ObservableCollection<BlobItem> BlobCollection { get { return _BlobCollection; } }

        public ObservableCollection<MessageItem> _MessageCollection = new ObservableCollection<MessageItem>();
        public ObservableCollection<MessageItem> MessageCollection { get { return _MessageCollection; } }

        public Dictionary<String, bool> TableColumnNames = new Dictionary<string, bool>();
        public ObservableCollection<EntityItem> _EntityCollection = new ObservableCollection<EntityItem>();
        public ObservableCollection<EntityItem> EntityCollection { get { return _EntityCollection; } }

        private int NextAction = 1;
        private Dictionary<int, Action> Actions = new Dictionary<int, Action>();
        private int NextError = 1;
        private Dictionary<int, Action> Errors = new Dictionary<int, Action>();

        private String BlobSortHeader;
        private ListSortDirection BlobSortDirection = ListSortDirection.Ascending;

        private String MessageSortHeader = "InsertionTime";
        private ListSortDirection MessageSortDirection = ListSortDirection.Ascending;

        private String EntitySortHeader;
        private ListSortDirection EntitySortDirection = ListSortDirection.Ascending;

        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private GridViewColumnHeader _lastMessageHeaderClicked = null;
        private ListSortDirection _lastMessageDirection = ListSortDirection.Ascending;

        private GridViewColumnHeader _lastEntityHeaderClicked = null;
        private ListSortDirection _lastEntityDirection = ListSortDirection.Ascending;



        // Blob filters

        private int MaxBlobCountFilter = -1;    // Max number of blobs to return, or -1 for filter off
        private String BlobNameFilter = null;   // Name to wildcard match, or null for filter off
        private long MinBlobSize = -1;           // Min blob size, -1 for filter off
        private long MaxBlobSize = -1;           // Min blob size, -1 for filter off
        private int BlobTypeFilter = 0;         // 0 = All blobs, 1 = Only block blobs, 2 = Only page blobs

        // Entity filters

        private int MaxEntityCountFilter = -1;  // Max number of entities to return, or -1 for filter off
        private String EntityTextFilter = null; // Text to wildcard match across entity columns, or null for filter off

        private bool EntityQueryEnabled = false;
        private String[] EntityQueryColumnName = null;
        private String[] EntityQueryCondition = null;
        private String[] EntityQueryValue = null;
        
        #endregion

        #region Initialization

        public StorageView()
        {
            InitializeComponent();
            LoadDefaultBlobFilter();
            LoadDefaultEntityFilter();
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

            NewAction();

            AccountTitle.Text = Account.Name;

            ClearMainPane();

            TreeViewItem blobSection = new TreeViewItem()
            {
                Header = "Blob Containers",
                Tag = new OutlineItem()
                {
                    ItemType = ItemType.BLOB_SERVICE /* 100 */,
                    Container = null
                }
            };

            TreeViewItem queueSection = new TreeViewItem()
            {
                Header = "Queues",
                Tag = new OutlineItem()
                {
                    ItemType = ItemType.QUEUE_SERVICE /* 200 */,
                    Container = null
                }
            };

            TreeViewItem tableSection = new TreeViewItem()
            {
                Header = "Tables",
                Tag = new OutlineItem()
                {
                    ItemType = ItemType.TABLE_SERVICE /* 300 */,
                    Container = null
                }
            };

            AccountTreeView.Items.Clear();

            AccountTreeView.Items.Add(blobSection);
            AccountTreeView.Items.Add(queueSection);
            AccountTreeView.Items.Add(tableSection);

            CloudStorageAccount account = OpenStorageAccount();

            blobClient = account.CreateCloudBlobClient();
            tableClient = account.CreateCloudTableClient();
            queueClient = account.CreateCloudQueueClient();

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
                // Check for $logs container and add it if present ($logs is not included in the general ListContainers call).
                
                CloudBlobContainer logsContainer = blobClient.GetContainerReference("$logs");
                if (logsContainer.Exists())
                {
                    StackPanel stack = new StackPanel();
                    stack.Orientation = Orientation.Horizontal;

                    Image cloudFolderImage = new Image();
                    cloudFolderImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_folder.png"));
                    cloudFolderImage.Height = 24;

                    Label label = new Label();
                    label.Content = logsContainer.Name;

                    stack.Children.Add(cloudFolderImage);
                    stack.Children.Add(label);

                    TreeViewItem blobItem = new TreeViewItem()
                    {
                        Header = stack,
                        Tag = new OutlineItem()
                        {
                            ItemType = ItemType.BLOB_CONTAINER,
                            Container = logsContainer.Name,
                            Permissions = logsContainer.GetPermissions()
                        }
                    };
                    blobSection.Items.Add(blobItem);
                }

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
                                    ItemType = ItemType.BLOB_CONTAINER,
                                    Container = container.Name,
                                    Permissions = container.GetPermissions()
                                }
                            };
                            blobSection.Items.Add(blobItem);
                        }
                    }
                }
                blobSection.Header = "Blob Containers (" + containers.Count().ToString() + ")";

                switch(LastItemType)
                {
                    case ItemType.BLOB_SERVICE:
                    case ItemType.BLOB_CONTAINER:
                        blobSection.IsExpanded = true;
                        break;
                    case ItemType.QUEUE_SERVICE:
                    case ItemType.QUEUE_CONTAINER:
                        queueSection.IsExpanded = true;
                        break;
                    case ItemType.TABLE_SERVICE:
                    case ItemType.TABLE_CONTAINER:
                        tableSection.IsExpanded = true;
                        break;
                    default:
                        blobSection.IsExpanded = true;
                        break;
                }


            }
            catch (Exception ex)
            {
                ShowError("Error enumering blob containers in the storage account: " + ex.Message);
            }

            try
            { 
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
                                ItemType = ItemType.QUEUE_CONTAINER,
                                Container = queue.Name
                            }
                        });
                    }
                }
                queueSection.Header = "Queues (" + queues.Count().ToString() + ")";
            }
            catch (Exception ex)
            {
                ShowError("Error enumering queues in storage account: " + ex.Message);
            }

            // OData version number occurs here:
            // Could not load file or assembly 'Microsoft.Data.OData, Version=5.6.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35' or one of its dependencies. The system cannot find the file specified.

            try
            {
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
                                ItemType = ItemType.TABLE_CONTAINER,
                                Container = table.Name
                            }
                        });
                    }
                }
                tableSection.Header = "Tables (" + tables.Count().ToString() + ")";
            }
            catch(Exception ex)
            {
                ShowError("Error enumerating tables in storage account: " + ex.Message);
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

        //*******************
        //*                 *
        //*  ClearMainPane  *
        //*                 *
        //*******************
        // Clear the main pane content.

        private void ClearMainPane()
        {
            ContainerToolbarPanel.Visibility = Visibility.Collapsed;
            BlobToolbarPanel.Visibility = Visibility.Collapsed;
            EntityToolbarPanel.Visibility = Visibility.Collapsed;
            ContainerPanel.Visibility = Visibility.Collapsed;
            ContainerListView.Visibility = Visibility.Collapsed;

            ButtonContainerAccess.Visibility = Visibility.Collapsed;
            ButtonDeleteContainer.Visibility = Visibility.Collapsed;
            ButtonBlobServiceCORS.Visibility = Visibility.Collapsed;


            QueueToolbarPanel.Visibility = Visibility.Collapsed;
            ButtonDeleteQueue.Visibility = Visibility.Collapsed;
            
            MessageToolbarPanel.Visibility = Visibility.Collapsed;

            MessageListView.Visibility = System.Windows.Visibility.Collapsed;

            TableToolbarPanel.Visibility = Visibility.Collapsed;
            TableListView.Visibility = Visibility.Collapsed;

            ButtonDeleteTable.Visibility = Visibility.Collapsed;
            EntityDownloadButton.Visibility = Visibility.Collapsed;
            EntityUploadButton.Visibility = Visibility.Collapsed;
        }

        //*****************************************
        //*                                       *
        //*  AccountTreeView_SelectedItemChanged  *
        //*                                       *
        //*****************************************
        // An item in the left pane was selected. Update the main pane.

        private void AccountTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (AccountTreeView.SelectedItem == null) return;

            NewAction();

            TreeViewItem item = AccountTreeView.SelectedItem as TreeViewItem;

            ClearMainPane();

            if (item == null || !(item.Tag is OutlineItem)) return;

            ContainerTitle.Text = String.Empty;

            _BlobCollection.Clear();
            
            OutlineItem outlineItem = item.Tag as OutlineItem;

            LastItemType = outlineItem.ItemType;

            switch (outlineItem.ItemType)
            {
                case ItemType.BLOB_SERVICE:   // Blob Containers section
                    ContainerToolbarPanel.Visibility = Visibility.Visible;
                    ButtonBlobServiceCORS.Visibility = Visibility.Visible;
                    break;
                case ItemType.BLOB_CONTAINER:   // Blob container
                    ContainerImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_folder.png"));
                    ContainerPanel.Visibility = Visibility.Visible;
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
                case ItemType.QUEUE_SERVICE:   // Queues section
                    QueueToolbarPanel.Visibility = Visibility.Visible;
                    break;
                case ItemType.QUEUE_CONTAINER:   // Queue
                    ContainerImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_queue.png"));
                    ContainerPanel.Visibility = Visibility.Visible;
                    ContainerTitle.Text = outlineItem.Container;
                    ContainerType.Text = "queue";
                    ContainerDetails.Text = String.Empty;
                    SelectedTableContainer = outlineItem.Container;

                    QueueToolbarPanel.Visibility = Visibility.Visible;
                    ButtonDeleteQueue.Visibility = Visibility.Visible;

                    SelectedQueueContainer = outlineItem.Container;
                    ShowQueueContainer(SelectedQueueContainer);
                    break;
                case ItemType.TABLE_SERVICE:   // Tables section
                    TableToolbarPanel.Visibility = Visibility.Visible;
                    break;
                case ItemType.TABLE_CONTAINER:   // Table
                    TableToolbarPanel.Visibility = Visibility.Visible;
                    ButtonDeleteTable.Visibility = Visibility.Visible;
                    EntityDownloadButton.Visibility = Visibility.Visible;
                    EntityUploadButton.Visibility = Visibility.Visible;

                    ContainerImage.Source = new BitmapImage(new Uri("pack://application:,,/Images/cloud_table.png"));
                    ContainerPanel.Visibility = Visibility.Visible;
                    ContainerTitle.Text = outlineItem.Container;
                    ContainerType.Text = "table";
                    ContainerDetails.Text = String.Empty;
                    SelectedTableContainer = outlineItem.Container;

                    TableColumnNames.Clear();

                    ShowTableContainer(SelectedTableContainer);
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
            NewAction();

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
            IEnumerable<BlobItem> x;
            
            try
            { 
                ContainerListView.ItemsSource = null;

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
            catch (Exception ex)
            {
                x = from b in _BlobCollection select b;
                if (BlobSortDirection == ListSortDirection.Ascending)
                {
                    _BlobCollection = new ObservableCollection<BlobItem>(x.OrderBy(w => w.Name, StringComparer.CurrentCultureIgnoreCase));
                }
                else
                {
                    _BlobCollection = new ObservableCollection<BlobItem>(x.OrderByDescending(w => w.Name, StringComparer.CurrentCultureIgnoreCase));
                }
                ContainerListView.ItemsSource = BlobCollection;
                ShowError("Error sorting blob list: " + ex.Message);
            }
        }

        #endregion

        #region Blob Toolbar Button Handlers

        //*******************************************
        //*                                         *
        //*  EntityListView_ColumnHeaderClicked  *
        //*                                         *
        //*******************************************
        // Main pane table entity list column clicked - sort and re-display the entity list.

        private void EntityListView_ColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            NewAction();

            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                this.Cursor = Cursors.Wait;

                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastEntityHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastEntityDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;

                    EntitySortHeader = header;
                    EntitySortDirection = direction;

                    SortEntityList();


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
                    if (_lastEntityHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastEntityHeaderClicked.Column.HeaderTemplate = null;
                    }


                    _lastEntityHeaderClicked = headerClicked;
                    _lastEntityDirection = direction;
                }

                this.Cursor = Cursors.Arrow;
            }
        }

        //********************
        //*                  *
        //*  SortEntityList  *
        //*                  *
        //********************
        // Sort the entity list by selected column / direction.

        private void SortEntityList()
        {
            IEnumerable<EntityItem> entities = null;

            try
            {
                if (TableColumnNames.ContainsKey(EntitySortHeader))
                {
                    TableListView.ItemsSource = null;

                    entities = from e in _EntityCollection select e;

                    if (EntitySortDirection == ListSortDirection.Ascending)
                    {
                        _EntityCollection = new ObservableCollection<EntityItem>(entities.OrderBy(e => e.Fields[EntitySortHeader], StringComparer.CurrentCultureIgnoreCase));
                    }
                    else
                    {
                        _EntityCollection = new ObservableCollection<EntityItem>(entities.OrderByDescending(e => e.Fields[EntitySortHeader], StringComparer.CurrentCultureIgnoreCase));
                    }

                    TableListView.ItemsSource = EntityCollection;
                }
            }
            catch(Exception ex)
            {
                entities = from e in _EntityCollection select e;

                if (EntitySortDirection == ListSortDirection.Ascending)
                {
                    _EntityCollection = new ObservableCollection<EntityItem>(entities.OrderBy(e => e.Fields["RowKey"], StringComparer.CurrentCultureIgnoreCase));
                }
                else
                {
                    _EntityCollection = new ObservableCollection<EntityItem>(entities.OrderByDescending(e => e.Fields["RowKey"], StringComparer.CurrentCultureIgnoreCase));
                }

                TableListView.ItemsSource = EntityCollection;

                ShowError("Error sorting entity list: " + ex.Message);
            }
        }


        //**************************
        //*                        *
        //*  AccountRefresh_Click  *
        //*                        *
        //**************************
        // Refresh storage account.

        private void AccountRefresh_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;

            NewAction();

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
            NewAction();

            NewContainerDialog dlg = new NewContainerDialog();

            if (dlg.ShowDialog().Value)
            {
                bool isError = false;
                String errorMessage = null;

                String containerName = dlg.Container.Text;

                int accessLevel = 3;
                if (dlg.AccessContainer.IsChecked.Value) accessLevel = 1;
                if (dlg.AccessBlob.IsChecked.Value) accessLevel = 2;
                if (dlg.AccessNone.IsChecked.Value) accessLevel = 3;

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
                    LoadLeftPane();
                    UpdateStatus();

                    if (isError)
                    {
                        ShowError("Error Creating Blob Container " + containerName + ": " + errorMessage);
                    }
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
            NewAction();

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

            if (item.ItemType != ItemType.BLOB_CONTAINER)
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

                // Execute background task to delete the container.

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
                        ShowError("Error Deleting Blob Container " + containerName + ": " + errorMessage);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void ContainerListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BlobViewProperties_Click(this, null);
        }

        //**********************
        //*                    *
        //*  BlobFilter_Click  *
        //*                    *
        //**********************
        // Display the blob filter dialog.

        private void BlobFilter_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            // Initialize the dialog.

            BlobFilter dlg = new BlobFilter();

            dlg.BlobSortHeader = BlobSortHeader;
            dlg.BlobSortDirection = BlobSortDirection;

            if (MaxBlobCountFilter != -1)
            {
                dlg.MaxBlobCount.Text = MaxBlobCountFilter.ToString();
            }
            else
            {
                dlg.MaxBlobCount.Text = String.Empty;
            }

            switch (BlobTypeFilter)
            {
                case 0:
                    dlg.TypeAllBlobs.IsChecked = true;
                    break;
                case 1:
                    dlg.TypeBlockBlobs.IsChecked = true;
                    break;
                case 2:
                    dlg.TypePageBlobs.IsChecked = true;
                    break;
            }

            if (BlobNameFilter != null)
            {
                dlg.NameText.Text = BlobNameFilter;
            }

            if (MinBlobSize != -1)
            {
                dlg.MinSize.Text = LengthText(MinBlobSize, false);
            }

            if (MaxBlobSize != -1)
            {
                dlg.MaxSize.Text = LengthText(MaxBlobSize, false);
            }

            // Display dialog.

            if (dlg.ShowDialog().Value)
            {
                // Capture updated filter settings.

                BlobSortHeader = dlg.BlobSortHeader;
                BlobSortDirection = dlg.BlobSortDirection;

                MaxBlobCountFilter = -1;
                if (!String.IsNullOrEmpty(dlg.MaxBlobCount.Text) && Int32.TryParse(dlg.MaxBlobCount.Text, out MaxBlobCountFilter))
                {
                    if (MaxBlobCountFilter <= 0)
                    {
                        MaxBlobCountFilter = -1;
                    }
                }

                BlobTypeFilter = 0;
                if (dlg.TypeAllBlobs.IsChecked.Value)
                {
                    BlobTypeFilter = 0;
                }
                else if (dlg.TypeBlockBlobs.IsChecked.Value)
                {
                    BlobTypeFilter = 1;
                }
                else if (dlg.TypePageBlobs.IsChecked.Value)
                {
                    BlobTypeFilter = 2;
                }

                BlobNameFilter = null;
                if (!String.IsNullOrEmpty(dlg.NameText.Text))
                {
                    BlobNameFilter = dlg.NameText.Text;
                }

                MinBlobSize = -1;
                if (!String.IsNullOrEmpty(dlg.MinSize.Text) /* && Int64.TryParse(dlg.MinSize.Text, out MinBlobSize) */)
                {
                    MinBlobSize = GetLength(dlg.MinSize.Text);

                    if (MinBlobSize <= 0)
                    {
                        MinBlobSize = -1;
                    }
                }

                MaxBlobSize = -1;
                if (!String.IsNullOrEmpty(dlg.MaxSize.Text) /* && Int64.TryParse(dlg.MaxSize.Text, out MaxBlobSize) */)
                {
                    MaxBlobSize = GetLength(dlg.MaxSize.Text);

                    if (MaxBlobSize <= 0 || (MinBlobSize != -1 && MaxBlobSize < MinBlobSize))
                    {
                        MaxBlobSize = -1;
                    }
                }

                if (MaxBlobCountFilter != -1 ||
                    BlobNameFilter != null ||
                    MinBlobSize != -1 ||
                    MaxBlobSize != -1 ||
                    BlobTypeFilter != 0)
                {
                    BlobFilter.IsChecked = true;
                }
                else
                {
                    BlobFilter.IsChecked = false;
                }

                if (dlg.SaveAsDefaultFilter.IsChecked.Value)
                {
                    SaveDefaultBlobFilter();
                }

                // Refresh the blob list display with the new filter settings.

                ShowBlobContainer(SelectedBlobContainer);
            }
        }

        //****************************
        //*                          *
        //*  BlobUploadButton_Click  *
        //*                          *
        //****************************
        //Upload file(s) to current blob container.

        private void BlobUploadButton_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

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
            NewAction();

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
            NewAction();

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
            NewAction();

            ContainerListView.SelectedIndex = -1;
        }

        //******************************
        //*                            *
        //*  BlobDownloadButton_Click  *
        //*                            *
        //******************************
        // Download blob(s).

        private void BlobDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

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

            NewAction();

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
                        ShowError(ErrorMessage);
                    }

                    Actions[action.Id].IsCompleted = true;
                });

                task.ContinueWith((t) =>
                {
                    UpdateStatus();

                    if (!Success)
                    {
                        ShowError("Error copying blob: "+ ErrorMessage);
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
            NewAction();

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
            NewAction();

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
                            ShowError("Error setting blob container permissions: " + errorMessage);
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
            NewAction();
            
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
            NewAction();

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
                    ShowError("Error creating blob " + blobName + ": " + ex.Message);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        #endregion

        #region Queue Toolbar Button Handlers

        //*****************************************
        //*                                       *
        //*  MessageListView_ColumnHeaderClicked  *
        //*                                       *
        //*****************************************
        // Main pane table message list column clicked - sort and re-display the message list.

        private void MessageListView_ColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            NewAction();

            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                this.Cursor = Cursors.Wait;

                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastMessageHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastMessageDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;

                    MessageSortHeader = header;
                    MessageSortDirection = direction;

                    SortMessageList();


                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header 
                    if (_lastMessageHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastMessageHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastMessageHeaderClicked = headerClicked;
                    _lastMessageDirection = direction;
                }

                this.Cursor = Cursors.Arrow;
            }
        }


        //*********************
        //*                   *
        //*  SortMessageList  *
        //*                   *
        //*********************
        // Sort the message list by selected column / direction.

        private void SortMessageList()
        {
            IEnumerable<MessageItem> x;

            try
            {
                MessageListView.ItemsSource = null;

                switch (MessageSortHeader)
                {
                    case "Id":
                        x = from m in _MessageCollection select m;
                        if (MessageSortDirection == ListSortDirection.Ascending)
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderBy(w => w.Id, StringComparer.CurrentCultureIgnoreCase));
                        }
                        else
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderByDescending(w => w.Id, StringComparer.CurrentCultureIgnoreCase));
                        }
                        break;
                    case "StringValue":
                        x = from m in _MessageCollection select m;
                        if (MessageSortDirection == ListSortDirection.Ascending)
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderBy(w => w.StringValue, StringComparer.CurrentCultureIgnoreCase));
                        }
                        else
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderByDescending(w => w.StringValue, StringComparer.CurrentCultureIgnoreCase));
                        }
                        break;
                    case "DQCount":
                        x = from m in _MessageCollection select m;
                        if (MessageSortDirection == ListSortDirection.Ascending)
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderBy(w => w.DequeueCount));
                        }
                        else
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderByDescending(w => w.DequeueCount));
                        }
                        break;
                    case "PopReceipt":
                        x = from m in _MessageCollection select m;
                        if (MessageSortDirection == ListSortDirection.Ascending)
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderBy(w => w.PopReceipt, StringComparer.CurrentCultureIgnoreCase));
                        }
                        else
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderByDescending(w => w.PopReceipt, StringComparer.CurrentCultureIgnoreCase));
                        }
                        break;
                    case "InsertionTime":
                        x = from m in _MessageCollection select m;
                        if (MessageSortDirection == ListSortDirection.Ascending)
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderBy(w => w.InsertionTime, StringComparer.CurrentCultureIgnoreCase));
                        }
                        else
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderByDescending(w => w.InsertionTime, StringComparer.CurrentCultureIgnoreCase));
                        }
                        break;
                    case "ExpirationTime":
                        x = from m in _MessageCollection select m;
                        if (MessageSortDirection == ListSortDirection.Ascending)
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderBy(w => w.ExpirationTime));
                        }
                        else
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderByDescending(w => w.ExpirationTime));
                        }
                        break;
                    case "NextVisibleTime":
                        x = from m in _MessageCollection select m;
                        if (MessageSortDirection == ListSortDirection.Ascending)
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderBy(w => w.NextVisibleTime, StringComparer.CurrentCultureIgnoreCase));
                        }
                        else
                        {
                            _MessageCollection = new ObservableCollection<MessageItem>(x.OrderByDescending(w => w.NextVisibleTime, StringComparer.CurrentCultureIgnoreCase));
                        }
                        break;
                    default:
                        break;
                }

                MessageListView.ItemsSource = MessageCollection;
            }
            catch (Exception ex)
            {
                x = from m in _MessageCollection select m;
                if (MessageSortDirection == ListSortDirection.Ascending)
                {
                    _MessageCollection = new ObservableCollection<MessageItem>(x.OrderBy(w => w.InsertionTime, StringComparer.CurrentCultureIgnoreCase));
                }
                else
                {
                    _MessageCollection = new ObservableCollection<MessageItem>(x.OrderByDescending(w => w.InsertionTime, StringComparer.CurrentCultureIgnoreCase));
                }
                MessageListView.ItemsSource = MessageCollection;
                ShowError("Error sorting message list: " + ex.Message);
            }
        }




        //********************
        //*                  *
        //*  NewQueue_Click  *
        //*                  *
        //********************
        // Create a new queue.

        private void NewQueue_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            NewQueueDialog dlg = new NewQueueDialog();

            if (dlg.ShowDialog().Value)
            {
                bool isError = false;
                String errorMessage = null;

                String queueName = dlg.Queue.Text;

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_NEW_QUEUE,
                    IsCompleted = false,
                    Message = "Creating queue " + queueName
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                // Execute background task to create the queue.

                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (queueClient == null)
                        {
                            CloudStorageAccount account = OpenStorageAccount();
                            queueClient = account.CreateCloudQueueClient();
                        }
                        CloudQueue queue = queueClient.GetQueueReference(queueName);

                        queue.CreateIfNotExists();
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
                        ShowError("Error creating queue " + queueName + ": " + errorMessage);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

        }


        //***********************
        //*                     *
        //*  DeleteQueue_Click  *
        //*                     *
        //***********************
        // Delete selected queue.
        
        private void DeleteQueue_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            String message = "To delete a queue, select a queue and then clck the Delete Queue button.";

            if (AccountTreeView.SelectedItem == null || !(AccountTreeView.SelectedItem is TreeViewItem))
            {
                MessageBox.Show(message, "Queue Selection Required");
                return;
            }

            TreeViewItem tvi = AccountTreeView.SelectedItem as TreeViewItem;

            if (!(tvi.Tag is OutlineItem))
            {
                MessageBox.Show(message, "Queue Selection Required");
                return;
            }

            OutlineItem item = tvi.Tag as OutlineItem;

            if (item.ItemType != ItemType.QUEUE_CONTAINER)
            {
                MessageBox.Show(message, "Queue Selection Required");
                return;
            }

            String queueName = SelectedQueueContainer;

            if (MessageBox.Show("Are you SURE you want to delete queue " + SelectedQueueContainer + "?\n\nThe queue and all messages it contains will be permanently deleted",
                "Confirm Queue Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                bool isError = false;
                String errorMessage = null;

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_DELETE_QUEUE,
                    IsCompleted = false,
                    Message = "Deleting queue " + queueName
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                // Execute background task to delete the queue.

                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (queueClient == null)
                        {
                            CloudStorageAccount account = OpenStorageAccount();
                            queueClient = account.CreateCloudQueueClient();
                        }
                        CloudQueue queuee = queueClient.GetQueueReference(queueName);
                        queuee.DeleteIfExists();
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
                        ShowError("Error deleting queue " + queueName + ": " + errorMessage);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }


        //**************************
        //*                        *
        //*  MessageRefresh_Click  *
        //*                        *
        //**************************
        // Refresh queue message list.

        private void MessageRefresh_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            MessageListView.ItemsSource = null;
            ShowQueueContainer(SelectedQueueContainer);
        }


        //**********************
        //*                    *
        //*  MessageNew_Click  *
        //*                    *
        //**********************
        // Create a new queue message.

        private void MessageNew_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            NewMessageDialog dlg = new NewMessageDialog();
            if (dlg.ShowDialog().Value)
            {
                String messageText = dlg.MessageText.Text;

                try
                {
                    Cursor = Cursors.Wait;

                    if (queueClient == null)
                    {
                        CloudStorageAccount account = OpenStorageAccount();
                        queueClient = account.CreateCloudQueueClient();
                    }

                    CloudQueue container = queueClient.GetQueueReference(SelectedQueueContainer);

                    // Create queue message.

                    CloudQueueMessage message = new CloudQueueMessage(messageText);
                    container.AddMessage(message);

                    ShowQueueContainer(SelectedQueueContainer);

                }
                catch (Exception ex)
                {
                    ShowError("Error creating message for queue " + SelectedQueueContainer + ": " + ex.Message);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }


        //***********************
        //*                     *
        //*  MessageCopy_Click  *
        //*                     *
        //***********************
        // Copy selected queue message.

        private void MessageCopy_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            // Validate a single message has been selected.

            if (MessageListView.SelectedItems.Count != 1)
            {
                MessageBox.Show("In order to copy a message, please select one message then click the Copy toolbar button", "Single Selection Requireed");
                return;
            }

            MessageItem selectedMessage = MessageListView.SelectedItems[0] as MessageItem;
            if (selectedMessage == null) return;

            // Display the copy message dialog.

            NewMessageDialog dlg = new NewMessageDialog();
            dlg.Title = "Copy Queue Message";
            dlg.MessageText.Text = selectedMessage.StringValue;

            if (dlg.ShowDialog().Value)
            {
                String messageText = dlg.MessageText.Text;

                try
                {
                    Cursor = Cursors.Wait;

                    if (queueClient == null)
                    {
                        CloudStorageAccount account = OpenStorageAccount();
                        queueClient = account.CreateCloudQueueClient();
                    }

                    CloudQueue container = queueClient.GetQueueReference(SelectedQueueContainer);

                    // Create queue message.

                    CloudQueueMessage message = new CloudQueueMessage(messageText);
                    container.AddMessage(message);

                    ShowQueueContainer(SelectedQueueContainer);

                }
                catch (Exception ex)
                {
                    ShowError("Error creating message for queue " + SelectedQueueContainer + ": " + ex.Message);
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }

        }


        //**********************
        //*                    *
        //*  MessagePop_Click  *
        //*                    *
        //**********************
        // Pop top message off the current queue.

        private void MessagePop_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            if (MessageCollection.Count() == 0)
            {
                MessageBox.Show("The queue is empty.", "No Messages in Queue");
                return;
            }

            String message = "Are you sure you want to pop the top message from the queue?";

            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(message, "Confirm Delete", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                message = null;
                message = "Deleting top message from queue " + SelectedQueueContainer;

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_DELETE_MESSAGES,
                    IsCompleted = false,
                    Message = message
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                Cursor = Cursors.Wait;

                Task task = Task.Factory.StartNew(() =>
                {
                    CloudQueue container = queueClient.GetQueueReference(SelectedQueueContainer);

                    int deletedCount = 0;
                    CloudQueueMessage msg = container.GetMessage();
                    deletedCount++;

                    Actions[action.Id].IsCompleted = true;
                });

                task.ContinueWith((t) =>
                {
                    UpdateStatus();

                    Cursor = Cursors.Arrow;

                    ShowQueueContainer(SelectedQueueContainer);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

        }


        #endregion

        #region Table Entity Toolbar handlers

        //********************
        //*                  *
        //*  NewTable_Click  *
        //*                  *
        //********************
        // Create a new table.

        private void NewTable_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            NewTableDialog dlg = new NewTableDialog();

            if (dlg.ShowDialog().Value)
            {
                bool isError = false;
                String errorMessage = null;

                String tableName = dlg.Table.Text;

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_NEW_TABLE,
                    IsCompleted = false,
                    Message = "Creating table " + tableName
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                // Execute background task to create the table.

                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (tableClient == null)
                        {
                            CloudStorageAccount account = OpenStorageAccount();
                            tableClient = account.CreateCloudTableClient();
                        }
                        CloudTable table = tableClient.GetTableReference(tableName);

                        table.CreateIfNotExists();
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
                        ShowError("Error creating table " + tableName + ": " + errorMessage);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        //***********************
        //*                     *
        //*  DeleteTable_Click  *
        //*                     *
        //***********************
        // Delete selected table.

        private void DeleteTable_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            String message = "To delete a table, select a table and then clck the Delete Table button.";

            if (AccountTreeView.SelectedItem == null || !(AccountTreeView.SelectedItem is TreeViewItem))
            {
                MessageBox.Show(message, "Table Selection Required");
                return;
            }

            TreeViewItem tvi = AccountTreeView.SelectedItem as TreeViewItem;

            if (!(tvi.Tag is OutlineItem))
            {
                MessageBox.Show(message, "Table Selection Required");
                return;
            }

            OutlineItem item = tvi.Tag as OutlineItem;

            if (item.ItemType != ItemType.TABLE_CONTAINER)
            {
                MessageBox.Show(message, "Table Selection Required");
                return;
            }

            String tableName = SelectedTableContainer;

            if (MessageBox.Show("Are you SURE you want to delete table " + SelectedTableContainer + "?\n\nThe table and all entities it contains will be permanently deleted",
                "Confirm Table Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                bool isError = false;
                String errorMessage = null;

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_DELETE_TABLE,
                    IsCompleted = false,
                    Message = "Deleting table " + tableName
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                // Execute background task to delete the table.

                Task task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (tableClient == null)
                        {
                            CloudStorageAccount account = OpenStorageAccount();
                            tableClient = account.CreateCloudTableClient();
                        }
                        CloudTable table = tableClient.GetTableReference(tableName);
                        table.DeleteIfExists();
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
                        ShowError("Error deleting table " + tableName + ": " + errorMessage);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void TableListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EntityView_Click(sender, null);
        }


        //*******************************
        //*                             *
        //* EntityDownloadButton_Click  *
        //*                             *
        //*******************************
        // Download selected entities to a local file.

        private void EntityDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            List<EntityItem> entities = new List<EntityItem>();

            foreach (EntityItem entity in TableListView.SelectedItems)
            {
                entities.Add(entity);
            }

            DownloadEntitiesDialog dlg = new DownloadEntitiesDialog();

            dlg.OutputFile.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + Account.Name + "_" + SelectedTableContainer;

            dlg.SetEntityCounts(_EntityCollection.Count(), entities.Count());

            if (dlg.ShowDialog().Value)
            {
                bool downloadAll = dlg.DownloadAllEntities.IsChecked.Value;
                
                String format = "csv";
                if (dlg.DownloadFormatCSV.IsChecked.Value)
                {
                    format = "csv";
                }
                else if (dlg.DownloadFormatJSON.IsChecked.Value)
                {
                    format = "json";
                }
                if (dlg.DownloadFormatXML.IsChecked.Value)
                {
                    format = "xml";
                }

                // Get output file. If a file extension was not specified, add one based on the format selection.

                String outputFile = dlg.OutputFile.Text;

                String name = outputFile;
                int index = name.LastIndexOf("\\");
                if (index != -1)
                {
                    name = name.Substring(index);
                }
                if (!name.Contains("."))
                {
                    outputFile = outputFile + "." + format;
                }

                bool autoOpen = dlg.AutoOpen.IsChecked.Value;

                if (File.Exists(outputFile))
                {
                    if (MessageBox.Show("Output file " + outputFile + " already exists - overwrite it?", "Confirm Overwrite", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                
                if (!downloadAll)
                {
                    DownloadEntities(SelectedTableContainer, entities.ToArray(), format, outputFile, autoOpen);
                }
                else
                {
                    DownloadEntities(SelectedTableContainer, _EntityCollection.ToArray(), format, outputFile, autoOpen);
                }
            }
        }


        //*****************************
        //*                           *
        //* EntityUploadButton_Click  *
        //*                           *
        //*****************************
        // Upload selected entities to a local file.

        private void EntityUploadButton_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            UploadEntitiesDialog dlg = new UploadEntitiesDialog();

            bool stopOnError = dlg.StopOnError.IsChecked.Value;

            dlg.InputFile.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + Account.Name + "_" + SelectedTableContainer;

            if (dlg.ShowDialog().Value)
            {
                String outerElementName = dlg.OuterElementName.Text;

                String format = "csv";
                if (dlg.UploadFormatCSV.IsChecked.Value)
                {
                    format = "csv";
                }
                else if (dlg.UploadFormatJSON.IsChecked.Value)
                {
                    format = "json";
                }
                if (dlg.UploadFormatXML.IsChecked.Value)
                {
                    format = "xml";
                    outerElementName = dlg.EntityXPath.Text;
                }

                // Get input file. If a file extension was not specified, add one based on the format selection.

                String inputFile = dlg.InputFile.Text;

                String name = inputFile;
                int index = name.LastIndexOf("\\");
                if (index != -1)
                {
                    name = name.Substring(index);
                }
                if (!name.Contains("."))
                {
                    inputFile = inputFile + "." + format;
                }

                if (!File.Exists(inputFile))
                {
                    MessageBox.Show("Input file " + inputFile + " not found.", "File Not Found");
                    return;
                }

                String partitionKeyColumnName = dlg.PartitionKeyColumnName.Text;
                String rowKeyColumnName = dlg.RowKeyColumnName.Text;

                UploadEntities(SelectedTableContainer, format, inputFile, outerElementName, partitionKeyColumnName, rowKeyColumnName, stopOnError);
            }

        }


        //*************************
        //*                       *
        //*  EntityRefresh_Click  *
        //*                       *
        //*************************
        // Refresh the list of entities.

        private void EntityRefresh_Click(object sender, RoutedEventArgs e)
        {
            NewAction();
            TableListView.ItemsSource = null;
            ShowTableContainer(SelectedTableContainer);
        }

        //***************************
        //*                         *
        //*  EntitySelectAll_Click  *
        //*                         *
        //***************************
        // Select all entities in the container.

        private void EntitySelectAll_Click(object sender, RoutedEventArgs e)
        {
            NewAction();
            TableListView.SelectAll();
        }

        //********************************
        //*                              *
        //*  EntityClearSelection_Click  *
        //*                              *
        //********************************
        // Clear entity selection.

        private void EntityClearSelection_Click(object sender, RoutedEventArgs e)
        {
            NewAction();
            TableListView.SelectedIndex = -1;
        }

        //***********************
        //*                     *
        //*  EntityQuery_Click  *
        //*                     *
        //***********************
        // Display the entity query dialog.

        private void EntityQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NewAction();

                // Initialize the diaog.

                EntityQuery dlg = new EntityQuery();

                // Load table columns.

                int columnId = 0;
                List<CheckedListItem> entityColumns = new List<CheckedListItem>();

                foreach (KeyValuePair<String, bool> column in TableColumnNames)
                {
                    entityColumns.Add(new CheckedListItem()
                    {
                        Id = columnId++,
                        IsChecked = column.Value,
                        Name = column.Key
                    });
                }
                dlg.SetEntityColumns(entityColumns);

                // Load query parameters

                if (!EntityQueryEnabled)
                {
                    dlg.AllEntities.IsChecked = true;
                }
                else
                {
                    dlg.QueryEntities.IsChecked = true;
                    if (EntityQueryColumnName.Length > 0)
                    {
                        dlg.SetConditions(EntityQueryColumnName, EntityQueryCondition, EntityQueryValue);
                    }
                }

                // Display dialog.

                if (dlg.ShowDialog().Value)
                {
                    // Capture updated query parameters

                    if (dlg.AllEntities.IsChecked.Value)
                    {
                        EntityQueryEnabled = false;
                    }
                    else
                    {
                        EntityQueryEnabled = true;

                        int count = 0;
                        if (dlg.Column1.SelectedIndex > 0) count++;
                        if (dlg.Column2.SelectedIndex > 0) count++;
                        if (dlg.Column3.SelectedIndex > 0) count++;

                        EntityQueryColumnName = new String[count];
                        EntityQueryCondition = new String[count];
                        EntityQueryValue = new String[count];

                        int col = 0;

                        if (dlg.Column1.SelectedIndex > 0)
                        {
                            EntityQueryColumnName[col] = (dlg.Column1.SelectedItem as ComboBoxItem).Content as String;
                            EntityQueryCondition[col] = (dlg.Condition1.SelectedItem as ComboBoxItem).Content as String;
                            EntityQueryValue[col] = dlg.Value1.Text;
                            col++;
                        }

                        if (dlg.Column2.SelectedIndex > 0)
                        {
                            EntityQueryColumnName[col] = (dlg.Column2.SelectedItem as ComboBoxItem).Content as String;
                            EntityQueryCondition[col] = (dlg.Condition2.SelectedItem as ComboBoxItem).Content as String;
                            EntityQueryValue[col] = dlg.Value2.Text;
                            col++;
                        }

                        if (dlg.Column3.SelectedIndex > 0)
                        {
                            EntityQueryColumnName[col] = (dlg.Column3.SelectedItem as ComboBoxItem).Content as String;
                            EntityQueryCondition[col] = (dlg.Condition3.SelectedItem as ComboBoxItem).Content as String;
                            EntityQueryValue[col] = dlg.Value3.Text;
                            col++;
                        }
                    }
                    ShowTableContainer(SelectedTableContainer);
                }
            }
            catch(Exception ex)
            {
                ShowError("Error processing query: " + ex.Message);
            }
        }

        //************************
        //*                      *
        //*  EntityFilter_Click  *
        //*                      *
        //************************
        // Display the entity filter dialog.

        private void EntityFilter_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            // Initialize the dialog.

            EntityFilter dlg = new EntityFilter();

            dlg.EntitySortHeader = EntitySortHeader;
            dlg.EntitySortDirection = EntitySortDirection;

            if (MaxEntityCountFilter != -1)
            {
                dlg.MaxEntityCount.Text = MaxEntityCountFilter.ToString();
            }
            else
            {
                dlg.MaxEntityCount.Text = String.Empty;
            }

            if (EntityTextFilter != null)
            {
                dlg.EntityText.Text = EntityTextFilter;
            }

            // Load table columns.

            int columnId = 0;
            List<CheckedListItem> entityColumns = new List<CheckedListItem>();

            foreach (KeyValuePair<String, bool> column in TableColumnNames)
            {
                entityColumns.Add(new CheckedListItem()
                {
                    Id = columnId++,
                    IsChecked = column.Value,
                    Name = column.Key
                });
            }
            dlg.SetEntityColumns(entityColumns);

            // Display dialog.

            if (dlg.ShowDialog().Value)
            {
                // Capture updated filter settings.

                EntitySortHeader = dlg.EntitySortHeader;
                EntitySortDirection = dlg.EntitySortDirection;

                MaxEntityCountFilter = -1;
                if (!String.IsNullOrEmpty(dlg.MaxEntityCount.Text) && Int32.TryParse(dlg.MaxEntityCount.Text, out MaxEntityCountFilter))
                {
                    if (MaxEntityCountFilter <= 0)
                    {
                        MaxEntityCountFilter = -1;
                    }
                }

                EntityTextFilter = null;
                if (!String.IsNullOrEmpty(dlg.EntityText.Text))
                {
                    EntityTextFilter = dlg.EntityText.Text;
                }

                if (dlg.EntityColumns != null)
                {
                    foreach (CheckedListItem item in dlg.EntityColumns)
                    {
                        TableColumnNames[item.Name] = item.IsChecked;
                    }
                }

                if (MaxEntityCountFilter != -1 ||
                    EntityTextFilter != null ||
                    !AllTableColumnNamesChecked())
                {
                    EntityFilter.IsChecked = true;
                }
                else
                {
                    EntityFilter.IsChecked = false;
                }

                if (dlg.SaveAsDefaultFilter.IsChecked.Value)
                {
                    SaveDefaultEntityFilter();
                }

                // Refresh the blob list display with the new filter settings.

                ShowTableContainer(SelectedTableContainer);
            }
        }

        // Return true if all table column names are checked.

        private bool AllTableColumnNamesChecked()
        {
            bool result = true;

            if (TableColumnNames != null)
            {
                foreach (KeyValuePair<String, bool> col in TableColumnNames)
                {
                    if (!col.Value)
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }
        
        //*********************
        //*                   *
        //*  EntityNew_Click  *
        //*                   *
        //*********************
        // Insert a new entity into the currently selected table.

        private void EntityNew_Click(object sender, RoutedEventArgs e)
        {
            EditEntityDialog dlg = new EditEntityDialog();

            CloudTable table = tableClient.GetTableReference(SelectedTableContainer);
            dlg.InitForInsert(table, TableColumnNames);

            if (dlg.ShowDialog().Value)
            {
                if (dlg.RecordsAdded > 0)
                {
                    ShowTableContainer(SelectedTableContainer);
                }
            }
        }


        //**********************
        //*                    *
        //*  EntityView_Click  *
        //*                    *
        //**********************
        // View/edit seleted entity.

        private void EntityView_Click(object sender, RoutedEventArgs e)
        {
            if (TableListView.SelectedItems.Count != 1)
            {
                MessageBox.Show("In order to view or edit an entity, please select one entity then click the View toolbar button", "Single Selection Requireed");
                return;
            }

            EntityItem entity = (TableListView.SelectedItems[0]) as EntityItem;
            if (entity == null) return;

            EditEntityDialog dlg = new EditEntityDialog();

            CloudTable table = tableClient.GetTableReference(SelectedTableContainer);
            dlg.InitForUpdate(table, TableColumnNames, entity);

            if (dlg.ShowDialog().Value)
            {
                if (dlg.RecordsUpdated > 0)
                {
                    ShowTableContainer(SelectedTableContainer);
                }
            }
        }


        //**********************
        //*                    *
        //*  EntityView_Click  *
        //*                    *
        //**********************
        // View/edit seleted entity.

        private void EntityCopy_Click(object sender, RoutedEventArgs e)
        {
            if (TableListView.SelectedItems.Count != 1)
            {
                MessageBox.Show("In order to copy an entity, please select one entity then click the View toolbar button", "Single Selection Requireed");
                return;
            }

            EntityItem entity = (TableListView.SelectedItems[0]) as EntityItem;
            if (entity == null) return;

            EditEntityDialog dlg = new EditEntityDialog();

            CloudTable table = tableClient.GetTableReference(SelectedTableContainer);
            dlg.InitForCopy(table, TableColumnNames, entity);

            if (dlg.ShowDialog().Value)
            {
                if (dlg.RecordsAdded > 0)
                {
                    ShowTableContainer(SelectedTableContainer);
                }
            }
        }


        //************************
        //*                      *
        //*  EntityDelete_Click  *
        //*                      *
        //************************
        //Delete selected entities.

        private void EntityDelete_Click(object sender, RoutedEventArgs e)
        {
            NewAction();

            List<EntityItem> entities = new List<EntityItem>();

            foreach (EntityItem entity in TableListView.SelectedItems)
            {
                entities.Add(entity);
            }

            int count = entities.Count();

            if (count == 0)
            {
                MessageBox.Show("No entities are selected. To delete entities, select one or more from the list then click the Delete toolbar button.", "Selection Required");
                return;
            }

            String message = "Are you sure you want to delete these " + count.ToString() + " entities?";
            if (count == 1)
            {
                message = "Are you sure you want to delete this entity?";
            }

            message = message + "\n";
            int n = 0;
            foreach (EntityItem entity in entities)
            {
                n++;
                if (n < 10)
                {
                    message = message + "\n" + entity.PartitionKey + "|" + entity.RowKey;
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
                    message = "Deleting entity " + TableListView.SelectedItems[0] + " from table " + SelectedTableContainer;
                }
                else
                {
                    message = "Deleting " + TableListView.SelectedItems.Count.ToString() + " entities from table " + SelectedTableContainer;
                }

                Action action = new Action()
                {
                    Id = NextAction++,
                    ActionType = Action.ACTION_DELETE_ENTITIES,
                    IsCompleted = false,
                    Message = message
                };
                Actions.Add(action.Id, action);

                UpdateStatus();

                Cursor = Cursors.Wait;

                Task task = Task.Factory.StartNew(() =>
                {
                    CloudTable table = tableClient.GetTableReference(SelectedTableContainer);

                    int deletedCount = 0;
                    foreach (EntityItem entity in entities)
                    {
                        table.Execute(TableOperation.Delete(entity));
                        deletedCount++;
                    }

                    Actions[action.Id].IsCompleted = true;
                });

                task.ContinueWith((t) =>
                {
                    UpdateStatus();

                    Cursor = Cursors.Arrow;

                    ShowTableContainer(SelectedTableContainer);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

        }


        #endregion

        #region Update UI

        #region Action Status Messages

        //******************
        //*                *
        //*  UpdateStatus  *
        //*                *
        //******************
        // Update status messages. If there are multiple actions in progress, stack the messages. Call from UI thread.

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
            }
            else
            {
                StatusMessage.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Error Messages

        //***************
        //*             *
        //*  NewAction  *
        //*             *
        //***************
        // A new action has been started by the user. Perform any display clean-up here. Call from UI thread.

        private void NewAction()
        {
            //Errors.Clear();
            //UpdateErrors();
        }

        //*****************
        //*               *
        //*  ClearErrors  *
        //*               *
        //*****************
        // Clear error messages. Call from UI thread.

        private void ClearErrors()
        {
            Errors.Clear();
            UpdateErrors();
        }

        //***************
        //*             *
        //*  ShowError  *
        //*             *
        //***************
        // Add an error message and display it. Call from UI thread.

        private void ShowError(String message)
        {
            Errors.Add(NextError, new Action()
            {
                ActionType = -1,
                Id = NextError++,
                IsCompleted = false,
                Message = message
            });
            UpdateErrors();
        }

        //******************
        //*                *
        //*  UpdateErrors  *
        //*                *
        //******************
        // Update error message display. If there are multiple errors, stack them.

        public void UpdateErrors()
        {
            int count = 0;
            ErrorMessage.Inlines.Clear();
            if (Actions != null)
            {
                foreach (KeyValuePair<int, Action> error in Errors)
                {
                    if (!error.Value.IsCompleted)
                    {
                        if (count > 0)
                        {
                            ErrorMessage.Inlines.Add(new LineBreak());
                        }

                        Run closeBox = new Run("× ");
                        closeBox.Tag = error.Key;
                        ErrorMessage.Inlines.Add(closeBox);
                        closeBox.MouseDown += ErrorMessageMouseDown;
                        
                        Run run = new Run(error.Value.Message);
                        run.FontWeight = FontWeights.Bold;
                        run.Foreground = new SolidColorBrush(Colors.DarkRed);
                        ErrorMessage.Inlines.Add(run);

                        count++;
                    }
                }
            }
            if (count == 0)
            {
                ErrorMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }

        private void ErrorMessageMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Run)
            {
                Run run = sender as Run;
                int errorId = (int)run.Tag;
                Errors.Remove(errorId);
                UpdateErrors();
            }
        }

        #endregion


        //***********************
        //*                     *
        //*  ShowBlobContainer  *
        //*                     *
        //***********************
        // Get and show blobs in selected blob container. Call from UI thread.

        public void ShowBlobContainer(String containerName) //, CancellationToken token, TaskScheduler uiTask)
        {
            try
            {
                this.Cursor = Cursors.Wait;

                ContainerDetails.Text = "Loading blob list...";

                ContainerListView.ItemsSource = null; //  BlobCollection;

                int containerCount = 0;
                long containerSize = 0;
                _BlobCollection.Clear();
                ContainerListView.Visibility = Visibility.Visible;
                ContainerToolbarPanel.Visibility = Visibility.Visible;
                BlobToolbarPanel.Visibility = Visibility.Visible;

                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                if (container != null)
                {
                    IEnumerable<IListBlobItem> blobs = container.ListBlobs(null, true, BlobListingDetails.All);
                    if (blobs != null)
                    {
                        foreach (IListBlobItem item in blobs)
                        {
                            try
                            {
                                if (MaxBlobCountFilter != -1 && containerCount >= MaxBlobCountFilter) break;

                                if (item.GetType() == typeof(CloudBlobDirectory))
                                {
                                }
                                else if (item.GetType() == typeof(CloudBlockBlob))
                                {
                                    CloudBlockBlob blockBlob = item as CloudBlockBlob;

                                    if (BlobTypeFilter != 2)
                                    {
                                        if (BlobNameFilter == null || blockBlob.Name.IndexOf(BlobNameFilter, 0, StringComparison.OrdinalIgnoreCase) != -1)
                                        {
                                            if ((MinBlobSize == -1 || blockBlob.Properties.Length >= MinBlobSize) &&
                                                (MaxBlobSize == -1 || blockBlob.Properties.Length <= MaxBlobSize))
                                            {
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
                                                containerCount++;
                                                containerSize += blockBlob.Properties.Length;
                                            }
                                        }
                                    }
                                }
                                else if (item.GetType() == typeof(CloudPageBlob))
                                {
                                    CloudPageBlob pageBlob = item as CloudPageBlob;

                                    if (BlobTypeFilter != 1)
                                    {
                                        if (BlobNameFilter == null || pageBlob.Name.IndexOf(BlobNameFilter, 0, StringComparison.OrdinalIgnoreCase) != -1)
                                        {
                                            if ((MinBlobSize == -1 || pageBlob.Properties.Length >= MinBlobSize) &&
                                                (MaxBlobSize == -1 || pageBlob.Properties.Length <= MaxBlobSize))
                                            {
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
                                                containerCount++;
                                                containerSize += pageBlob.Properties.Length;
                                            }
                                        }
                                    }
                                }
                            }
                            catch(Exception)
                            {

                            }
                        } // end foreach

                        ContainerListView.ItemsSource = BlobCollection;

                        SortBlobList();

                        if (containerCount == 1)
                        {
                            ContainerDetails.Text = "(1 blob, " + LengthText(containerSize) + ") as of " + DateTime.Now.ToString();
                        }
                        else
                        {
                            ContainerDetails.Text = "(" + containerCount.ToString() + " blobs, " + LengthText(containerSize) + ") as of " + DateTime.Now.ToString();
                        }

                        this.Cursor = Cursors.Arrow;
                    }
                }
            }
            catch(Exception ex)
            {
                this.Cursor = Cursors.Wait;
                ShowError("Error retrieving blob list: " + ex.Message);
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
                CloudPageBlob pageBlob = container.GetBlobReferenceFromServer(blobName) as CloudPageBlob;
                Microsoft.WindowsAzure.Storage.Blob.BlobProperties props = pageBlob.Properties;
                dlg.ShowPageBlob(pageBlob);
            }

            if (dlg.ShowDialog().Value)
            {
                if (dlg.IsBlobChanged)
                {
                    ShowBlobContainer(SelectedBlobContainer);
                }
            }
        }


        //************************
        //*                      *
        //*  ShowTableContainer  *
        //*                      *
        //************************
        // Get and show entities in selected table container. Call from UI thread.

        public void ShowTableContainer(String tableName)
        {
            try
            {
                this.Cursor = Cursors.Wait;

                ContainerDetails.Text = "Loading entity list...";

                TableListView.ItemsSource = null;

                // Create a temporary copy of the TableColumnNames table and add columns as we encounter them.
                // This is done to prume away previously saved colum names that are no longer present in the data.

                Dictionary<String, bool> tempTableColumnNames = new Dictionary<string, bool>();

                TableListViewGridView.Columns.Clear();

                AddTableListViewColumn("PartitionKey");
                AddTableListViewColumn("RowKey");
                AddTableListViewColumn("Timestamp", false);

                tempTableColumnNames.Add("PartitionKey", TableColumnNames["PartitionKey"]);
                tempTableColumnNames.Add("RowKey", TableColumnNames["RowKey"]);
                tempTableColumnNames.Add("Timestamp", TableColumnNames["Timestamp"]);

                int containerCount = 0;
                long containerSize = 0;
                _EntityCollection.Clear();
                TableListView.Visibility = Visibility.Visible;
                EntityToolbarPanel.Visibility = Visibility.Visible;

                CloudTable table = tableClient.GetTableReference(tableName);

                // Query the table and retrieve a collection of entities.

                var query = new TableQuery<ElasticTableEntity>();

                IEnumerable<ElasticTableEntity> entities = null;

                if (EntityQueryEnabled)
                {
                    EntityQuery.IsChecked = true;

                    IEnumerable<ElasticTableEntity> q = null;

                    switch (EntityQueryCondition[0])
                    {
                        case "equals":
                            q = table.ExecuteQuery(query).Where(e => e.Value(EntityQueryColumnName[0]) == EntityQueryValue[0]).Select(e => e);
                            break;
                        case "does not equal":
                            q = table.ExecuteQuery(query).Where(e => e.Value(EntityQueryColumnName[0]) != EntityQueryValue[0]).Select(e => e);
                            break;
                        case "contains":
                            q = table.ExecuteQuery(query).Where(e => e.Value(EntityQueryColumnName[0]).Contains(EntityQueryValue[0])).Select(e => e);
                            break;
                        case "starts with":
                            q = table.ExecuteQuery(query).Where(e => e.Value(EntityQueryColumnName[0]).StartsWith(EntityQueryValue[0])).Select(e => e);
                            break;
                        case "ends with":
                            q = table.ExecuteQuery(query).Where(e => e.Value(EntityQueryColumnName[0]).EndsWith(EntityQueryValue[0])).Select(e => e);
                            break;
                    }

                    if (EntityQueryColumnName.Length > 1)
                    {
                        switch (EntityQueryCondition[1])
                        {
                            case "equals":
                                q = q.Where(e => e.Value(EntityQueryColumnName[1]) == EntityQueryValue[1]);
                                break;
                            case "does not equal":
                                q = q.Where(e => e.Value(EntityQueryColumnName[1]) != EntityQueryValue[1]);
                                break;
                            case "contains":
                                q = q.Where(e => e.Value(EntityQueryColumnName[1]).Contains(EntityQueryValue[1]));
                                break;
                            case "starts with":
                                q = q.Where(e => e.Value(EntityQueryColumnName[1]).StartsWith(EntityQueryValue[1]));
                                break;
                            case "ends with":
                                q = q.Where(e => e.Value(EntityQueryColumnName[1]).EndsWith(EntityQueryValue[1]));
                                break;
                        }
                    }

                    if (EntityQueryColumnName.Length > 2)
                    {
                        switch (EntityQueryCondition[2])
                        {
                            case "equals":
                                q = q.Where(e => e.Value(EntityQueryColumnName[2]) == EntityQueryValue[2]);
                                break;
                            case "does not equal":
                                q = q.Where(e => e.Value(EntityQueryColumnName[2]) != EntityQueryValue[2]);
                                break;
                            case "contains":
                                q = q.Where(e => e.Value(EntityQueryColumnName[2]).Contains(EntityQueryValue[2]));
                                break;
                            case "starts with":
                                q = q.Where(e => e.Value(EntityQueryColumnName[2]).StartsWith(EntityQueryValue[2]));
                                break;
                            case "ends with":
                                q = q.Where(e => e.Value(EntityQueryColumnName[2]).EndsWith(EntityQueryValue[2]));
                                break;
                        }
                    }

                    entities = q.ToList();
                }
                else
                {
                    EntityQuery.IsChecked = false;
                    entities = table.ExecuteQuery(query).ToList();
                }

                if (entities != null)
                {
                    // Iterate through the list of entities.
                    // Ensure a bound column exists in the list view for each.
                    // Add a representation of each entity to the items source for the list view.

                    bool match = false;

                    foreach (ElasticTableEntity entity in entities)
                    {
                        match = false;

                        if (EntityTextFilter == null) match = true;

                        if (MaxEntityCountFilter != -1 && containerCount >= MaxEntityCountFilter) break;

                        foreach (KeyValuePair<String, EntityProperty> prop in entity.Properties)
                        {
                            AddTableListViewColumn(prop.Key);

                            if (!tempTableColumnNames.ContainsKey(prop.Key))
                            {
                                tempTableColumnNames.Add(prop.Key, TableColumnNames[prop.Key]);
                            }
                        }

                        EntityItem item = new EntityItem(entity);

                        if (EntityTextFilter != null)
                        {
                            if (entity.RowKey.IndexOf(EntityTextFilter, 0, StringComparison.OrdinalIgnoreCase) != -1 ||
                                entity.PartitionKey.IndexOf(EntityTextFilter, 0, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                match = true;
                            }
                            else
                            {
                                foreach(KeyValuePair<String, String> field in item.Fields)
                                {
                                    if (field.Value.IndexOf(EntityTextFilter, 0, StringComparison.OrdinalIgnoreCase) != -1)
                                    {
                                        match = true;
                                    }
                                }
                            }
                        }

                        if (match)
                        {
                            _EntityCollection.Add(item);
                            containerCount++;
                        }

                        TableColumnNames = tempTableColumnNames;
                    }
                }


                if (_EntityCollection != null)
                {
                    foreach (EntityItem entity in _EntityCollection)
                    {
                        entity.AddMissingFields(TableColumnNames);
                    }
                }

                //SortEntityList();

                if (containerCount == 1)
                {
                    ContainerDetails.Text = "(1 entity) as of " + DateTime.Now.ToString();
                }
                else
                {
                    ContainerDetails.Text = "(" + containerCount.ToString() + " entities) as of " + DateTime.Now.ToString();
                }

                TableListView.ItemsSource = EntityCollection;

                this.Cursor = Cursors.Arrow;
            }
            catch(Exception ex)
            {
                this.Cursor = Cursors.Arrow;
                ShowError("Error querying table: " + ex.Message);
            }
        }


        //****************************
        //*                          *
        //*  AddTableListViewColumn  *
        //*                          *
        //****************************
        // Add a column to the internal list of table columns (if not already present) and add a databound column to the entity list view (if not already present).

        private void AddTableListViewColumn(String columnName, bool enabled = true)
        {
            // If the column is not in the internal list of table column names, add it now and default to visible.

            if (!TableColumnNames.ContainsKey(columnName))
            {
                TableColumnNames.Add(columnName, enabled);
            }

            // Check to see if the column is already in the entity list view.

            if (TableColumnNames[columnName])
            {
                bool colExists = false;
                foreach (GridViewColumn col in TableListViewGridView.Columns)
                {
                    if (col.Header.ToString() == columnName)
                    {
                        colExists = true;
                        break;
                    }
                }

                // If the column is not in the entity list view, and 

                if (!colExists)
                {
                    GridViewColumn column = new GridViewColumn();
                    column.Header = columnName;
                    column.DisplayMemberBinding = new Binding("Fields[" + columnName + "]");

                    TableListViewGridView.Columns.Add(column);
                }
            }
        }


        #endregion

        #region Blob Operations

        //***************************
        //*                         *
        //*  LoadDefaultBlobFilter  *
        //*                         *
        //***************************
        // If a default blob filter configuration has been saved for this user, load it now.

        private void LoadDefaultBlobFilter()
        {
            try
            {
                String filename = System.Windows.Forms.Application.UserAppDataPath + "\\AzureStorageExplorer6-DefaultBlobFilter.dt1";
                String line, name, value;

                if (File.Exists(filename))
                {
                    using (TextReader reader = File.OpenText(filename))
                    {
                        string[] items = null;
                        while((line = reader.ReadLine()) != null)
                        {
                            items = line.Split('|');
                            if (items.Length >= 2)
                            {
                                name = items[0];
                                value = items[1];
                                switch(name)
                                {
                                    case "MaxBlobCount":
                                        MaxBlobCountFilter = Convert.ToInt32(value);
                                        if (MaxBlobCountFilter <= 0)
                                        {
                                            MaxBlobCountFilter = -1;
                                        }
                                        break;
                                    case "BlobName":
                                        BlobNameFilter = value;
                                        if (String.IsNullOrEmpty(value))
                                        {
                                            BlobNameFilter = null;
                                        }
                                        break;
                                    case "MinBlobSize":
                                        MinBlobSize = Convert.ToInt32(value);
                                        if (MinBlobSize <= 0)
                                        {
                                            MinBlobSize = -1;
                                        }
                                        break;
                                    case "MaxBlobSize":
                                        MaxBlobSize = Convert.ToInt32(value);
                                        if (MaxBlobSize <= 0)
                                        {
                                            MaxBlobSize = -1;
                                        }
                                        break;
                                    case "BlobType":
                                        BlobTypeFilter = Convert.ToInt32(value);
                                        switch(BlobTypeFilter)
                                        {
                                            case 0:
                                            case 1:
                                            case 2:
                                                break;
                                            default:
                                                BlobTypeFilter = 0;
                                                break;
                                        }
                                        break;
                                    case "BlobSortHeader":
                                        BlobSortHeader = value;
                                        break;
                                    case "BlobSortDirection":
                                        switch (value)
                                        {
                                            case "D":
                                                BlobSortDirection = ListSortDirection.Descending;
                                                break;
                                            case "A":
                                            default:
                                                BlobSortDirection = ListSortDirection.Ascending;
                                                break;
                                        }
                                        break;
                                } // end switch
                            } // end if items.Length >= 2
                        } // end while
                    } // end using TextReader

                    // Set Filter toolbar button state

                    if (MaxBlobCountFilter != -1 ||
                        BlobNameFilter != null ||
                        MinBlobSize != -1 ||
                        MaxBlobSize != -1 ||
                        BlobTypeFilter != 0)
                    {
                        BlobFilter.IsChecked = true;
                    }
                    else
                    {
                        BlobFilter.IsChecked = false;
                    }

                } // end if
            } // end try
            catch(Exception ex)
            {
                //Console.WriteLine(ex.Message);
            }
        }


        //***************************
        //*                         *
        //*  SaveDefaultBlobFilter  *
        //*                         *
        //***************************
        // Save default blob filter configuration.

        private void SaveDefaultBlobFilter()
        {
            String filename = System.Windows.Forms.Application.UserAppDataPath + "\\AzureStorageExplorer6-DefaultBlobFilter.dt1";

            try
            {
                using (TextWriter writer = File.CreateText(filename))
                {
                    if (MaxBlobCountFilter != -1)
                    {
                        writer.WriteLine("MaxBlobCount|" + MaxBlobCountFilter.ToString());
                    }

                    if (BlobNameFilter != null)
                    {
                        writer.WriteLine("BlobName|" + BlobNameFilter);
                    }

                    if (MinBlobSize != -1)
                    {
                        writer.WriteLine("MinBlobSize|" + MinBlobSize.ToString());
                    }

                    if (MaxBlobSize != -1)
                    {
                        writer.WriteLine("MaxBlobSize|" + MaxBlobSize.ToString());
                    }

                    writer.WriteLine("BlobType|" + BlobTypeFilter.ToString());

                    writer.WriteLine("BlobSortHeader|" + BlobSortHeader);

                    if (BlobSortDirection == ListSortDirection.Ascending)
                    {
                        writer.WriteLine("BlobSortDirection|A");
                    }
                    else
                    {
                        writer.WriteLine("BlobSortDirection|D");
                    }
                }
            }
            catch(Exception ex)
            {
                ShowError("Error saving blob filter settings to file " + filename + ": " + ex.Message);
            }
        }

        //*****************
        //*               *
        //*  UploadFiles  *
        //*               *
        //*****************
        // Upload a list of local files up to a blob container. Call from UI thread. Performs heavy lifting in a background task.

        public void UploadFiles(String[] files, String containerName)
        {
            Dictionary<String, String> contentTypes = MainWindow.ContentTypes;

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

                            bool isPageBlob = false;
                            
                            if (blobName.ToLower().EndsWith(".vhd"))
                            {
                                isPageBlob = true;
                            }

                            if (isPageBlob)
                            {
                                CloudPageBlob blob = container.GetPageBlobReference(blobName);

                                blob.UploadFromFile(file, System.IO.FileMode.Open);

                                foreach(KeyValuePair<String, String> ct in contentTypes)
                                {
                                    if (blob.Name.EndsWith(ct.Key))
                                    {
                                        blob.Properties.ContentType = ct.Value;
                                        blob.SetProperties();
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                                            {
                                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

                                blob.UploadFromFile(file, System.IO.FileMode.Open);

                                foreach(KeyValuePair<String, String> ct in contentTypes)
                                {
                                    if (blob.Name.EndsWith(ct.Key))
                                    {
                                        blob.Properties.ContentType = ct.Value;
                                        blob.SetProperties();
                                        break;
                                    }
                                }
                            }
                            }
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
                    ShowError("Error uploading files: " + errorMessage);
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

        #region Queue Operations

        //************************
        //*                      *
        //*  ShowQueueContainer  *
        //*                      *
        //************************
        // Get and show messages in selected queue container. Call from UI thread.

        public void ShowQueueContainer(String containerName)
        {
            try
            {
                this.Cursor = Cursors.Wait;

                ContainerDetails.Text = "Loading message list...";

                MessageListView.ItemsSource = null; //  MessageCollection;

                int containerCount = 0;
                long containerSize = 0;
                _MessageCollection.Clear();
                MessageListView.Visibility = Visibility.Visible;
                QueueToolbarPanel.Visibility = Visibility.Visible;
                MessageToolbarPanel.Visibility = Visibility.Visible;
                IEnumerable<CloudQueueMessage> messages = queueClient.GetQueueReference(containerName).PeekMessages(CloudQueueMessage.MaxNumberOfMessagesToPeek);
                if (messages != null)
                {
                    foreach (CloudQueueMessage message in messages)
                    {
                        //if (MaxMessageCountFilter != -1 && containerCount >= MaxMessageCountFilter) break;

                        //if (BlobNameFilter == null || blockBlob.Name.IndexOf(BlobNameFilter, 0, StringComparison.OrdinalIgnoreCase) != -1)
                        //{
                            //if ((MinBlobSize == -1 || blockBlob.Properties.Length >= MinBlobSize) &&
                            //    (MaxBlobSize == -1 || blockBlob.Properties.Length <= MaxBlobSize))
                            //{
                                MessageItem messageItem = new MessageItem()
                                {
                                    Id = message.Id,
                                    DequeueCount = message.DequeueCount,
                                    //InsertionTime = message.InsertionTime.Value,
                                    //ExpirationTime = message.ExpirationTime.Value,
                                    //NextVisibleTime = message.NextVisibleTime.Value,
                                    PopReceipt = message.PopReceipt,
                                    //BytesValue = "byte[" + message.AsBytes.Length.ToString() + "]",
                                    //StringValue = message.AsString
                                };

                                String stringValue = message.AsString;
                                if (stringValue == null)
                                {
                                    messageItem.StringValue = "NULL";
                                }
                                else
                                {
                                    messageItem.StringValue = stringValue;
                                }

                                if (message.PopReceipt == null)
                                {
                                    messageItem.PopReceipt = "NULL";
                                }
                                else
                                {
                                    messageItem.PopReceipt = message.PopReceipt;
                                }

                                if (message.InsertionTime.HasValue)
                                {
                                    messageItem.InsertionTime = message.InsertionTime.Value.ToString();
                                }
                                else
                                {
                                    messageItem.ExpirationTime = "NULL";
                                }

                                if (message.ExpirationTime.HasValue)
                                {
                                    messageItem.ExpirationTime = message.ExpirationTime.Value.ToString();
                                }
                                else
                                {
                                    messageItem.ExpirationTime = "NULL";
                                }

                                if (message.NextVisibleTime.HasValue)
                                {
                                    messageItem.NextVisibleTime = message.NextVisibleTime.Value.ToString();
                                }
                                else
                                {
                                    messageItem.NextVisibleTime = "NULL";
                                }

                                _MessageCollection.Add(messageItem);
                                containerCount++;
                                //containerSize += blockBlob.Properties.Length;
                            //}
                        //}

                    } // end foreach

                    MessageListView.ItemsSource = MessageCollection;

                    SortMessageList();

                    if (containerCount == 1)
                    {
                        ContainerDetails.Text = "(1 message) as of " + DateTime.Now.ToString();
                    }
                    else
                    {
                        if (containerCount >= CloudQueueMessage.MaxNumberOfMessagesToPeek)
                        {
                            ContainerDetails.Text = "(top " + containerCount.ToString() + " messages) as of " + DateTime.Now.ToString();
                        }
                        else
                        {
                            ContainerDetails.Text = "(" + containerCount.ToString() + " messages) as of " + DateTime.Now.ToString();
                        }
                    }

                    this.Cursor = Cursors.Arrow;
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Wait;
                ShowError("Error retrieving message list: " + ex.Message);
            }
        }


        #endregion

        #region Entity Operations

        //*****************************
        //*                           *
        //*  LoadDefaultEntityFilter  *
        //*                           *
        //*****************************
        // If a default entity filter configuration has been saved for this user, load it now.

        private void LoadDefaultEntityFilter()
        {
            try
            {
                String filename = System.Windows.Forms.Application.UserAppDataPath + "\\AzureStorageExplorer6-DefaultEntityFilter.dt1";
                String line, name, value;

                if (File.Exists(filename))
                {
                    using (TextReader reader = File.OpenText(filename))
                    {
                        string[] items = null;
                        while ((line = reader.ReadLine()) != null)
                        {
                            items = line.Split('|');
                            if (items.Length >= 2)
                            {
                                name = items[0];
                                value = items[1];
                                switch (name)
                                {
                                    case "MaxEntityCount":
                                        MaxEntityCountFilter = Convert.ToInt32(value);
                                        if (MaxEntityCountFilter <= 0)
                                        {
                                            MaxEntityCountFilter = -1;
                                        }
                                        break;
                                    case "EntityText":
                                        EntityTextFilter = value;
                                        if (String.IsNullOrEmpty(value))
                                        {
                                            EntityTextFilter = null;
                                        }
                                        break;
                                    case "EntitySortHeader":
                                        EntitySortHeader = value;
                                        break;
                                    case "EntitySortDirection":
                                        switch (value)
                                        {
                                            case "D":
                                                EntitySortDirection = ListSortDirection.Descending;
                                                break;
                                            case "A":
                                            default:
                                                EntitySortDirection = ListSortDirection.Ascending;
                                                break;
                                        }
                                        break;
                                } // end switch
                            } // end if items.Length >= 2
                        } // end while
                    } // end using TextReader

                    // Set Filter toolbar button state

                    if (MaxEntityCountFilter != -1 ||
                        EntityTextFilter != null ||
                        !AllTableColumnNamesChecked())
                    {
                        EntityFilter.IsChecked = true;
                    }
                    else
                    {
                        EntityFilter.IsChecked = false;
                    }

                } // end if
            } // end try
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
            }
        }

        //*****************************
        //*                           *
        //*  SaveDefaultEntityFilter  *
        //*                           *
        //*****************************
        // Save default entity filter configuration.

        private void SaveDefaultEntityFilter()
        {
            String filename = System.Windows.Forms.Application.UserAppDataPath + "\\AzureStorageExplorer6-DefaultEntityFilter.dt1";

            try
            {
                using (TextWriter writer = File.CreateText(filename))
                {
                    if (MaxEntityCountFilter != -1)
                    {
                        writer.WriteLine("MaxEntityCount|" + MaxEntityCountFilter.ToString());
                    }

                    if (EntityTextFilter != null)
                    {
                        writer.WriteLine("EntityText|" + EntityTextFilter);
                    }

                    writer.WriteLine("EntitySortHeader|" + EntitySortHeader);

                    if (EntitySortDirection == ListSortDirection.Ascending)
                    {
                        writer.WriteLine("EntitySortDirection|A");
                    }
                    else
                    {
                        writer.WriteLine("EntitySortDirection|D");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Error savinng entity filter settings to file " + filename + ": " + ex.Message);
            }
        }

        //**********************
        //*                    *
        //*  DownloadEntities  *
        //*                    *
        //**********************
        // Download a collection of entities to a local file in a selected download format.

        private void DownloadEntities(String tableName, IEnumerable<EntityItem> entities, String format, String outputFile, bool autoOpen)
        {
            // Create task action.

            String message = null;

            if (entities.Count() == 1)
            {
                message = "Downloading " + entities.Count().ToString() + " entities from table " + tableName + " to file " + outputFile;
            }
            else
            {
                message = "Downloading 1 entity trom table " + tableName + " to file " + outputFile;
            }

            Action action = new Action()
            {
                Id = NextAction++,
                ActionType = Action.ACTION_DOWNLOAD_ENTITIES,
                IsCompleted = false,
                Message = message
            };
            Actions.Add(action.Id, action);

            UpdateStatus();

            // Execute background task to perform the downloading.

            Task task = Task.Factory.StartNew(() =>
            {
                if (entities != null)
                {
                    CloudTable table = tableClient.GetTableReference(tableName);
                    //table.CreateIfNotExists();

                    if (File.Exists(outputFile))
                    {
                        File.Delete(outputFile);
                    }

                    // CSV format export

                    int e = 0;
                    int f = 0;

                    using (TextWriter writer = File.CreateText(outputFile))
                    {
                        // Write header.

                        switch (format)
                        {
                            case "csv":
                                if (TableColumnNames != null)
                                {
                                    foreach (KeyValuePair<String, bool> col in TableColumnNames)
                                    {
                                        if (col.Value)
                                        {
                                            if (f == 0)
                                            {
                                                writer.Write("\"" + col.Key + "\"");
                                            }
                                            else
                                            {
                                                writer.Write(",\"" + col.Key + "\"");
                                            }
                                            f++;
                                        } // end if
                                    } // next col
                                    writer.WriteLine();
                                } // end if (TableColumns != null)
                                break;
                            case "json":
                                writer.WriteLine("{");
                                writer.WriteLine("    \"Entities\": [");
                                break;
                            case "xml":
                                writer.WriteLine("<?xml version=\"1.0\" ?>");
                                writer.WriteLine("<Entities xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                                break;
                            default:
                                break;
                        }

                        // Write entities - iterate through each entity in the collection and write out a line/record.

                        String colName;
                        String value;

                        foreach (EntityItem entity in entities)
                        {
                            switch (format)
                            {
                            case "csv":
                                {
                                    f = 0;

                                    foreach (KeyValuePair<String, bool> col in TableColumnNames)
                                    {
                                        // Write out each column that has not been turned off in a filter.

                                        if (col.Value)
                                        {
                                            colName = col.Key;
                                            if (entity.Fields.ContainsKey(colName))
                                            {
                                                value = entity.Fields[colName];
                                            }
                                            else
                                            {
                                                value = String.Empty;
                                            }
                                            if (f == 0)
                                            {
                                                writer.Write(quote_csv(value));
                                            }
                                            else
                                            {
                                                writer.Write("," + quote_csv(value));
                                            }
                                            f++;
                                        } // end if
                                    } // next col
                                    writer.WriteLine();
                                } // end CSV
                                break;
                            case "xml":
                                {
                                    writer.WriteLine("  <Entity>");
                                    foreach (KeyValuePair<String, bool> col in TableColumnNames)
                                    {
                                        // Write out each column that has not been turned off in a filter.

                                        if (col.Value)
                                        {
                                            colName = col.Key;
                                            if (entity.Fields.ContainsKey(colName))
                                            {
                                                value = entity.Fields[colName];
                                            }
                                            else
                                            {
                                                value = String.Empty;
                                            }
                                            if (value == NULL_VALUE)
                                            {
                                                writer.WriteLine("    <" + colName + " xsi:nil=\"true\" />");
                                            }
                                            else
                                            {
                                                writer.Write("    <" + colName + ">");
                                                writer.Write(quote_xml(value));
                                                writer.WriteLine("</" + colName + ">");
                                            }
                                        } // end if
                                    } // next col
                                    writer.WriteLine("  </Entity>");
                                } // end XML
                                break;
                            case "json":
                                {
                                    f = 0;
                                    if (e > 0)
                                    {
                                        writer.WriteLine("        },");
                                    }
                                    writer.WriteLine("        {");
                                    foreach (KeyValuePair<String, bool> col in TableColumnNames)
                                    {
                                        // Write out each column that has not been turned off in a filter.

                                        if (col.Value)
                                        {
                                            colName = col.Key;
                                            if (entity.Fields.ContainsKey(colName))
                                            {
                                                value = entity.Fields[colName];
                                            }
                                            else
                                            {
                                                value = String.Empty;
                                            }
                                            if (f > 0)
                                            {
                                                writer.WriteLine(",");
                                            }
                                            writer.Write("            \"" + colName + "\": " + quote_json(value));
                                        } // end if
                                        f++;
                                    } // next col
                                    //writer.WriteLine("      }");
                                    writer.WriteLine();
                                } // end XML
                                break;
                            default:
                                break;
                            } // end switch
                            e++;
                            //writer.WriteLine();
                        } // next entity

                        // Write footer.

                        switch (format)
                        {
                            case "csv":
                                break;
                            case "json":
                                writer.WriteLine();
                                writer.WriteLine("        }");
                                writer.WriteLine("    ]");
                                writer.WriteLine("}");
                                break;
                            case "xml":
                                writer.WriteLine("</Entities>");
                                break;
                        }

                    } // end using TextWriter
                } // end if entities != nulll

                Actions[action.Id].IsCompleted = true;
            });

            // Task complete - update UI.

            task.ContinueWith((t) =>
            {
                UpdateStatus();

                if (autoOpen)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(outputFile);
                    }
                    catch(Exception)
                    {
                        // File could not be opened / no app was associated with its file type.
                    }
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());

        }


        //********************
        //*                  *
        //*  UploadEntities  *
        //*                  *
        //********************
        // Upload a collection of entities from a local file in a selected download format.

        private void UploadEntities(String tableName, String format, String inputFile, String outerElementName, String partitionKeyColumnName, String rowKeyColumnName, bool stopOnError)
        {
            int recordNumber = 1;
            int recordsAdded = 0;
            int recordErrors = 0;
            String serializationError = null;

            String xpath = outerElementName;

            // Create task action.

            String message = null;

            message = "Uploading entities to table " + tableName + " from file " + inputFile;

            Action action = new Action()
            {
                Id = NextAction++,
                ActionType = Action.ACTION_DOWNLOAD_ENTITIES,
                IsCompleted = false,
                Message = message
            };
            Actions.Add(action.Id, action);

            UpdateStatus();

            // Execute background task to perform the downloading.

            Task task = Task.Factory.StartNew(() =>
            {
                CloudTable table = tableClient.GetTableReference(tableName);
                //table.CreateIfNotExists();

                // Upload data using the specified format (csv, json, or xml).

                switch (format)
                {
                    case "json":
                        {
                            // JSON format upload

                            //  {
                            //    "Entities": [
                            //        {
                            //            "RowKey": "Batman",
                            //            "PartitionKey": "DC Comics",
                            //            "Timestamp": "8/6/2014 4:07:06 AM +00:00",
                            //            "Debut": "5/1/1939 12:00:00 AM",
                            //            "SecretIdentity": "Bruce Wayne"
                            //        },
                            //        {
                            //            "RowKey": "Green Lantern",
                            //            "PartitionKey": "DC Comics",
                            //            "Timestamp": "8/6/2014 4:10:52 AM +00:00",
                            //            "Debut": "7/1/1940 12:00:00 AM",
                            //            "SecretIdentity": "Hal Jordan"
                            //        },
                            //        ...
                            //    ]
                            //}

                            Dictionary<String, Object> entries = null;

                            // Parse the data in the JavaScriptSerializer.

                            try
                            {
                                JavaScriptSerializer ser = new JavaScriptSerializer();
                                entries = ser.DeserializeObject(File.ReadAllText(inputFile)) as Dictionary<String, Object>;
                            }
                            catch (Exception ex)
                            {
                                serializationError = "An error occurred deserializing the JSON file: " + ex.Message;
                            }

                            // Walk the result object graph and extract entities.

                            if (entries != null)
                            {
                                foreach (KeyValuePair<String, Object> entry in entries)
                                {
                                    if (entry.Key == outerElementName)
                                    {
                                        Object[] entities = entry.Value as Object[];

                                        foreach (object ent in entities)
                                        {
                                            if (ent is Dictionary<String, Object>)
                                            {
                                                if (WriteEntity(tableName, ent as Dictionary<String, Object>, partitionKeyColumnName, rowKeyColumnName))
                                                {
                                                    recordsAdded++;
                                                    recordNumber++;
                                                }
                                                else
                                                {
                                                    recordErrors++;
                                                    if (stopOnError)
                                                    {
                                                        recordNumber++;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case "xml":
                        {
                            // XML format upload

                            XmlDocument doc = null;

                            try
                            {
                                doc = new XmlDocument();
                                doc.LoadXml(File.ReadAllText(inputFile));

                                //foreach (XmlElement entities in doc.DocumentElement.GetElementsByTagName(outerElementName))

                                XmlNodeList nodes = doc.DocumentElement.SelectNodes(xpath);
                                //foreach (XmlElement entities in doc.DocumentElement)
                                foreach (XmlElement entities in nodes)
                                {
                                    //<Entities xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                                    //  <Entity>
                                    //    <RowKey>Batman</RowKey>
                                    //    <PartitionKey>DC Comics</PartitionKey>
                                    //    <Timestamp>8/6/2014 4:07:06 AM +00:00</Timestamp>
                                    //    <Debut>5/1/1939 12:00:00 AM</Debut>
                                    //    <SecretIdentity>Bruce Wayne</SecretIdentity>
                                    //  </Entity>

                                    List<String> columns = new List<string>();
                                    List<String> values = new List<string>();

                                    foreach (XmlElement entity in entities.ChildNodes) // .GetElementsByTagName("Entity"))
                                    //foreach (XmlElement entity in entities.GetElementsByTagName(outerElementName))
                                    {
                                        foreach (XmlNode field in entity.ChildNodes)
                                        {
                                            if (field is XmlText)
                                            {
                                                XmlText node = field as XmlText;
                                                columns.Add(node.ParentNode.Name);
                                                values.Add(node.Value);
                                            }
                                        }
                                    }

                                    if (WriteEntity(tableName, columns.ToArray(), values.ToArray(), partitionKeyColumnName, rowKeyColumnName))
                                    {
                                        recordsAdded++;
                                        recordNumber++;
                                    }
                                    else
                                    {
                                        recordErrors++;
                                        if (stopOnError)
                                        {
                                            recordNumber++;
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                serializationError = "An error occurred parsing the XML file: " + ex.Message;
                            }
                        }
                        break;
                    case "csv":
                        {
                            // CSV format upload. Uses CsvHelper from http://joshclose.github.io/CsvHelper/.

                            try
                            { 
                            using (TextReader reader = File.OpenText(inputFile))
                            {
                                var csv = new CsvHelper.CsvReader(reader);
                                String[] columns = null;
                                String[] values = null;

                                while(csv.Read())
                                {
                                    // If first pass, retrieve column names.

                                    if (recordNumber == 1)
                                    {
                                        columns = csv.FieldHeaders;
                                        values = new String[columns.Length];
                                    }
                                    recordNumber++;

                                    // Retrieve record values.

                                    int col = 0;
                                    foreach (String column in columns)
                                    {
                                        values[col] = csv.GetField(column);
                                        col++;
                                    }

                                    // Write entity.

                                    if (WriteEntity(tableName, columns, values, partitionKeyColumnName, rowKeyColumnName))
                                    {
                                        recordsAdded++;
                                        recordNumber++;
                                    }
                                    else
                                    {
                                        recordErrors++;
                                        if (stopOnError)
                                        {
                                            recordNumber++;
                                            break;
                                        }
                                    }
                                }
                            }
                            }
                            catch (Exception ex)
                            {
                                serializationError = "An error occurred parsing the CSV file: " + ex.Message;
                            }
                        }
                        break;
                    default:
                        ShowError("Cannot upload - unknown format '" + format + "'");
                        break;
                }

                Actions[action.Id].IsCompleted = true;
            });

            // Task complete - update UI.

            task.ContinueWith((t) =>
            {
                if (serializationError != null)
                {
                    ShowError(serializationError);
                }

                switch (recordErrors)
                {
                    case 0:
                        break;
                    case 1:
                        ShowError("An error occurred inserting entity nunber " + (recordNumber-1).ToString() + ".");
                        break;
                    default:
                        ShowError(recordErrors.ToString() + " errors occurred inserting entities.");
                        break;
                }
                UpdateStatus();
                ShowTableContainer(SelectedTableContainer);

            }, TaskScheduler.FromCurrentSynchronizationContext());

        }


        //*****************
        //*               *
        //*  WriteEntity  *
        //*               *
        //*****************
        // Write an entity, source from an array of column names and an array of values.

        private bool WriteEntity(string tableName, String[] columns, String[] values, String partitionKeyColumnName, String rowKeyColumnName)
        {
            try
            {
                ElasticTableEntity entity = new ElasticTableEntity();

                String fieldName, fieldValue;

                int col = 0;
                foreach (String value in values)
                {
                    fieldName = columns[col];
                    fieldValue = values[col];

                    if (fieldName == partitionKeyColumnName)
                    {
                        entity.PartitionKey = fieldValue;
                    }
                    else if (fieldName == rowKeyColumnName)
                    {
                        entity.RowKey = fieldValue;
                    }
                    else if (fieldName == "Timestamp")
                    {

                    }
                    else
                    {
                        entity[fieldName] = fieldValue;
                    }

                    col++;
                } // next field

                CloudTable table = tableClient.GetTableReference(tableName);
                table.Execute(TableOperation.Insert(entity));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }


        //*****************
        //*               *
        //*  WriteEntity  *
        //*               *
        //*****************
        // Write an entity, source from a dictionary of column names and values.

        private bool WriteEntity(string tableName, Dictionary<String, Object> dict, String partitionKeyColumnName, String rowKeyColumnName)
        {
            try
            {
                ElasticTableEntity entity = new ElasticTableEntity();

                String fieldName, fieldValue;

                foreach (KeyValuePair<String, Object> field in dict)
                {
                    fieldName = field.Key;
                    fieldValue = field.Value as string;

                    if (fieldName == partitionKeyColumnName)
                    {
                        entity.PartitionKey = fieldValue;
                    }
                    else if (fieldName == rowKeyColumnName)
                    {
                        entity.RowKey = fieldValue;
                    }
                    else if (fieldName == "Timestamp")
                    {

                    }
                    else
                    {
                        entity[fieldName] = fieldValue;
                    }
                } // next field

                CloudTable table = tableClient.GetTableReference(tableName);
                table.Execute(TableOperation.Insert(entity));
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        #endregion

        #region Helper Functions

        //***************
        //*             *
        //*  quote_csv  *
        //*             *
        //***************
        // "Quote" a string value for CSV export.
        // Return a copy of the string value. If the string contains commas, enclose in quotes. If string is a null value, return an empty string.

        private string quote_csv(String value)
        {
            if (String.IsNullOrEmpty(value) || value==NULL_VALUE)
            {
                return "\"\"";
            }
            else
            {
                value = value.Replace("\"", String.Empty);
                if (value.Contains(","))
                {
                    return "\"" + value + "\"";
                }
                else
                {
                    return value;
                }
            }
        }

        //****************
        //*              *
        //*  quote_json  *
        //*              *
        //****************
        // "Quote" a string value for JSON export.
        // Return a copy of the string value, enclosed in double quotation marks. 
        // If the string contains double quotation marks, enclose in single quotes. If string is a null value, return an empty string.

        private string quote_json(String value)
        {
            String qc = "\"";

            if (value == null || value == NULL_VALUE)
            {
                return "null";
            }
            else
            {
                if (value.Contains(qc))
                {
                    qc = "'";
                }
                return qc + value + qc;
            }
        }

        //***************
        //*             *
        //*  quote_xml  *
        //*             *
        //***************
        // "Quote" a string value for XML export.
        // Return a copy of the string value. If string is a null value, return an empty string.
        // TODO: encode special characters for XML

        private string quote_xml(String value)
        {
            if (String.IsNullOrEmpty(value) || value == NULL_VALUE)
            {
                return String.Empty;
            }
            else
            {
                String result = System.Security.SecurityElement.Escape(value);
                return result;
            }
        }

        // Process n | nK | nM | nG and return integer number of bytes.

        public long GetLength(String text)
        {
            int multiplier = 1;

            String size = text.ToUpper();
            if (size.EndsWith("K"))
            {
                multiplier = 1024;
                size = size.Substring(0, size.Length - 1);
            }
            else if (size.EndsWith("M"))
            {
                multiplier = 1024 * 1024;
                size = size.Substring(0, size.Length - 1);
            }
            else if (size.EndsWith("G"))
            {
                multiplier = 1024 * 1024 * 1024;
                size = size.Substring(0, size.Length - 1);
            }
            size = size.Trim();
            if (size.Length == 0)
            {
                return -1;
            }

            int count = 0;

            if (!Int32.TryParse(size, out count))
            {
                return -1;
            }

            return count * multiplier;


        }


        //****************
        //*              *
        //*  LengthText  *
        //*              *
        //****************
        // Return length in text form with most appropriate units.

        public String LengthText(long length, bool showBytesText = true)
        {
            decimal n = Convert.ToDecimal(length);
            if (length == 1)
            {
                if (showBytesText)
                {
                    return "1 byte";
                }
                else
                {
                    return "1";
                }
            }
            else if (length < 1024)
            {
                if (showBytesText)
                {
                    return length.ToString() + " bytes";
                }
                else
                {
                    return length.ToString();
                }
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

        private void MenuItem_StorageAccount_CloseTab_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.RemoveActiveTab();
        }

        #endregion


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
