using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for BlobProperties.xaml
    /// </summary>
    public partial class BlobProperties : Window
    {
        private bool Initialized = false;
        private CloudBlockBlob BlockBlob = null;
        private CloudPageBlob PageBlob = null;
        public bool IsBlobChanged = false;

        #region Initialization

        public BlobProperties()
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

        //*******************
        //*                 *
        //*  ShowBlockBlob  *
        //*                 *
        //*******************
        // Display properties for a specific block blob.

        public void ShowBlockBlob(CloudBlockBlob blob)
        {
            Cursor = Cursors.Wait;
            try
            { 
                PagesTab.Visibility = System.Windows.Visibility.Collapsed;

                BlockBlob = blob;
                this.Title = "View Blob - " + blob.Name;
                IsBlobChanged = false;

                PropBlobType.Text = "Block";
                PropContainer.Text = blob.Container.Name;
                if (blob.CopyState != null)
                { 
                    PropCopyState.Text = blob.CopyState.ToString();
                }
                PropName.Text = blob.Name;
                PropParent.Text = blob.Parent.Container.Name;
                PropSnapshotQualifiedStorageUri.Text = blob.SnapshotQualifiedStorageUri.ToString().Replace("; ", ";\n");
                PropSnapshotQualifiedUri.Text = blob.SnapshotQualifiedUri.ToString().Replace("; ", ";\n");
                if (blob.SnapshotTime.HasValue)
                {
                    PropSnapshotTime.Text = blob.SnapshotTime.ToString();
                }
                PropStorageUri.Text = blob.StorageUri.ToString().Replace("; ", ";\n");
                PropStreamMinimumReadSizeInBytes.Text = blob.StreamMinimumReadSizeInBytes.ToString();
                PropStreamWriteSizeInBytes.Text = blob.StreamWriteSizeInBytes.ToString();
                PropUri.Text = blob.Uri.ToString().Replace("; ", ";\n");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            Cursor = Cursors.Arrow;
        }


        //******************
        //*                *
        //*  ShowPageBlob  *
        //*                *
        //******************
        // Display properties for a specific page blob.

        public void ShowPageBlob(CloudPageBlob blob)
        {
            Cursor = Cursors.Wait;

            try
            { 
                PagesTab.Visibility = System.Windows.Visibility.Visible;

                PageBlob = blob;
                IsBlobChanged = false;
                this.Title = "View Blob - " + blob.Name;

                PropBlobType.Text = "Page";
                PropContainer.Text = blob.Container.Name;
                if (blob.CopyState != null)
                {
                    PropCopyState.Text = blob.CopyState.ToString();
                }
                PropName.Text = blob.Name;
                PropParent.Text = blob.Parent.Container.Name;
                PropSnapshotQualifiedStorageUri.Text = blob.SnapshotQualifiedStorageUri.ToString().Replace("; ", ";\n");
                PropSnapshotQualifiedUri.Text = blob.SnapshotQualifiedUri.ToString().Replace("; ", ";\n");
                if (blob.SnapshotTime.HasValue)
                {
                    PropSnapshotTime.Text = blob.SnapshotTime.ToString();
                }
                PropStorageUri.Text = blob.StorageUri.ToString().Replace("; ", ";\n");
                PropStreamMinimumReadSizeInBytes.Text = blob.StreamMinimumReadSizeInBytes.ToString();
                PropStreamWriteSizeInBytes.Text = blob.StreamWriteSizeInBytes.ToString();
                PropUri.Text = blob.Uri.ToString().Replace("; ", ";\n");

                IEnumerable<Microsoft.WindowsAzure.Storage.Blob.PageRange> ranges = PageBlob.GetPageRanges();

                PageRanges.Items.Clear();

                int rangeCount = 0;
                if (ranges != null)
                {
                    long pages = PageBlob.Properties.Length / 512;
                    long page = 0;
                    foreach(Microsoft.WindowsAzure.Storage.Blob.PageRange range in ranges)
                    {
                        page = (range.EndOffset) / 512;
                        PageRanges.Items.Add("Page " + page.ToString() + "/" + pages.ToString() + ": (" + range.StartOffset.ToString() + " - " + range.EndOffset.ToString());
                        rangeCount++;
                    }
                }
                if (rangeCount == 0)
                {
                    PageRanges.Items.Add("None - no pages allocated");
                }

                Cursor = Cursors.Arrow;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Properties tab button handlers

        private void PropertiesApply_Click(object sender, RoutedEventArgs e)
        {
            // TODO: implement
        }

        private void PropertiesCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = IsBlobChanged;
        }

        #endregion

        private void ViewContent_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
                TextContent.Visibility = Visibility.Collapsed;
                TextContent.Text = String.Empty;
                ImageContent.Visibility = Visibility.Collapsed;
                ImageContent.Source = null;
                VideoContent.Visibility = Visibility.Collapsed;
                VideoContent.Source = null;
                WebContent.Visibility = Visibility.Collapsed;
                WebContent.Source = null;
                ContentButtonPanel.Visibility = Visibility.Hidden;

                switch((ContentViewType.SelectedItem as ComboBoxItem).Content as String)
                {
                    case "Image":
                        Cursor = Cursors.Wait;
                        byte[] imageData = new byte[BlockBlob.Properties.Length];
                        BlockBlob.DownloadToByteArray(imageData, 0);

                        if (imageData != null && imageData.Length > 0)
                        { 
                            var image = new BitmapImage();
                            using (var mem = new MemoryStream(imageData))
                            {
                                mem.Position = 0;
                                image.BeginInit();
                                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.UriSource = null;
                                image.StreamSource = mem;
                                image.EndInit();
                            }
                            image.Freeze();
                            ImageContent.Source = image;
                        }
                        ImageContent.Visibility = Visibility.Visible;
                        ContentSave.Visibility = Visibility.Collapsed;
                        ContentButtonPanel.Visibility = Visibility.Visible;
                        Cursor = Cursors.Arrow;
                        break;
                    case "Text":
                        Cursor = Cursors.Wait;
                        TextContent.Visibility = Visibility.Visible;
                        TextContent.Text = BlockBlob.DownloadText();
                        ContentSave.Visibility = Visibility.Visible;
                        ContentButtonPanel.Visibility = Visibility.Visible;
                        Cursor = Cursors.Arrow;
                        break;
                    case "Video":
                        Cursor = Cursors.Wait;
                        VideoContent.Source = BlockBlob.Uri;
                        VideoContent.Visibility = Visibility.Visible;
                        ContentSave.Visibility = Visibility.Collapsed;
                        ContentButtonPanel.Visibility = Visibility.Visible;
                        Cursor = Cursors.Arrow;
                        break;
                    case "Web":
                        Cursor = Cursors.Wait;
                        WebContent.Visibility = Visibility.Visible;
                        WebContent.Source = BlockBlob.Uri;
                        ContentSave.Visibility = Visibility.Collapsed;
                        ContentButtonPanel.Visibility = Visibility.Visible;
                        Cursor = Cursors.Arrow;
                        break;
                    default:
                        break;
                }
            }
            catch(Exception ex)
            {
                Cursor = Cursors.Arrow;
                MessageBox.Show(ex.Message);
            }
        }

        private void callback()
        {

        }

        private void ContentSave_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to update the content of this blob?", "Confirm Content Update", MessageBoxButton.YesNo)==MessageBoxResult.Yes)
            Cursor = Cursors.Wait;
            BlockBlob.UploadText(TextContent.Text);
            IsBlobChanged = true;
            Cursor = Cursors.Arrow;
        }

        private void ContentViewType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Initialized) return;

            switch ((ContentViewType.SelectedItem as ComboBoxItem).Content as String)
            {
                case "Image":
                case "Video":
                    ViewContentCaption.Text = "View";
                    break;
                case "Text":
                    ViewContentCaption.Text = "Edit";
                    break;
                default:
                    ViewContentCaption.Text = "View";
                    break;
            }
        }
    }
}
