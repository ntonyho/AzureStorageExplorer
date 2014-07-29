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
        private bool DialogInitialized = false;
        private CloudBlockBlob BlockBlob = null;
        private CloudPageBlob PageBlob = null;
        public bool IsBlobChanged = false;

        #region Initialization

        public BlobProperties()
        {
            InitializeComponent();
            CenterWindowOnScreen();
            DialogInitialized = true;
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
                ContentTab.Visibility = System.Windows.Visibility.Visible;
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
                ContentTab.Visibility = System.Windows.Visibility.Collapsed;

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

                // Read page ranges in use and display in Pages tab.

                IEnumerable<Microsoft.WindowsAzure.Storage.Blob.PageRange> ranges = PageBlob.GetPageRanges();

                PageRanges.Items.Clear();

                long startPage, endPage;
                int rangeCount = 0;
                if (ranges != null)
                {
                    long pages = PageBlob.Properties.Length / 512;
                    foreach(Microsoft.WindowsAzure.Storage.Blob.PageRange range in ranges)
                    {
                        startPage = (range.StartOffset) / 512;
                        endPage = (range.EndOffset) / 512;
                        long offset = range.StartOffset;
                        long endOffset = offset + 512;
                        int index = 0;
                        for (long page = startPage; page <= endPage; page++)
                        {
                            index = PageRanges.Items.Add("Page " + page.ToString() + ": (" + offset.ToString() + " - " + endOffset.ToString() + ")");
                            rangeCount++;
                            offset += 512;
                            endOffset += 512;
                        }
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

        #region Content Tab button handlers

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
            if (!DialogInitialized) return;

            switch ((ContentViewType.SelectedItem as ComboBoxItem).Content as String)
            {
                case "Image":
                case "Video":
                case "Web":
                    ViewContentCaption.Text = "View";
                    ViewContent.Visibility = Visibility.Visible;
                    break;
                case "Text":
                    ViewContentCaption.Text = "Edit";
                    ViewContent.Visibility = Visibility.Visible;
                    break;
                default:
                    ViewContent.Visibility = Visibility.Hidden;
                    break;
            }
        }

        #endregion

        //********************
        //*                  *
        //*  PageRead_Click  *
        //*                  *
        //********************
        // Read a page.

        private void PageRead_Click(object sender, RoutedEventArgs e)
        {
            int pageNumber = 0;
            byte[] bytes = new byte[512];
            int offset = 0;

            try
            {
                int maxPageNumber = 100;     // TODO: look up max page of blob

                pageNumber = Convert.ToInt32(PageNumber.Text);
                if (pageNumber < 0 || pageNumber > maxPageNumber)
                {
                    MessageBox.Show("Cannot write page - page number is out of range.\n\nEnter a value from 0 to " + maxPageNumber.ToString(), "Invalid Page Number");
                    return;
                }

                Cursor = Cursors.Wait;

                byte[] data = new byte[512];

                PageBlob.DownloadRangeToByteArray(data, 0, pageNumber * 512, 512);

                PageHexData.Text = BitConverter.ToString(data).Replace("-", " ");

                Cursor = Cursors.Arrow;
            }
            catch (Exception ex)
            {
                MessageBox.Show("The following error occurred attempting to read the page:\n\n" + ex.Message, "Error reading page");
            }

        }

        //*********************
        //*                   *
        //*  PageWrite_Click  *
        //*                   *
        //*********************
        // Wrote a page.

        private void PageWrite_Click(object sender, RoutedEventArgs e)
        {
            int pageNumber = 0;
            byte[] bytes = new byte[512];
            int offset = 0;

            try
            {
                int maxPageNumber = 100;     // TODO: look up max page of blob

                pageNumber = Convert.ToInt32(PageNumber.Text);
                if (pageNumber < 0 || pageNumber > maxPageNumber)
                {
                    MessageBox.Show("Cannot write page - page number is out of range.\n\nEnter a value from 0 to " + maxPageNumber.ToString(), "Invalid Page Number");
                    return;
                }

                string hexValues = PageHexData.Text;
                string[] hexValuesSplit = hexValues.Split(' ');
                foreach (String hex in hexValuesSplit)
                {
                    bytes[offset++] = (byte)Convert.ToInt32(hex, 16);
                    if (offset == 512) break;
                }
                if (bytes.Length == 0)
                {
                    MessageBox.Show("Cannot write page - no values were provided", "Data Required");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error parsing data. Please enter one or more hex pairs, separated by spaces.\n\nExample: 01 02 03 0C 0D 0E 0F\n\nIf you enter less than 512 bytes, the data will be repeated to fill a 512-byte page.", "Invalid data");
            }

            try
            {
                Cursor = Cursors.Wait;

                byte[] data = new byte[512];
                int dataLength = offset;
                int off = 0;
                while(off < 512)
                {
                    for (int i = 0; i < dataLength; i++)
                    {
                        if (off + i < 512)
                        {
                            data[off + i] = bytes[i];
                        }
                    }
                    off += dataLength;
                }

                PageHexData.Text = BitConverter.ToString(data).Replace("-", " ");

                PageBlob.WritePages(new MemoryStream(data), pageNumber*512);

                Cursor = Cursors.Arrow;

            }
            catch (Exception ex)
            {
                MessageBox.Show("The following error occurred attempting to write to page " + pageNumber.ToString() + ":\n\n" + ex.Message, "Error Writing to Page");
            }
        }

        private void PageRanges_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String item = PageRanges.SelectedItem as string;
            if (item == null) return;
            long pageNumber = Convert.ToInt32(item.Split(':')[0].Substring(5));
            PageNumber.Text = pageNumber.ToString();
            PageRead_Click(sender, null);
        }
    }

}
