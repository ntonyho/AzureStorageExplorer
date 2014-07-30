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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Shared.Protocol;
//using Microsoft.WindowsAzure.Storage.Analytics;
//using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for ContainerSecurity.xaml
    /// </summary>
    public partial class ContainerSecurity : Window
    {
        public bool AccessLevelModified = false;
        public CloudBlobClient BlobClient = null;
        public CloudBlobContainer Container = null;

        public ContainerSecurity()
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

        public void SetContainer(CloudBlobClient client, CloudBlobContainer container, String blobName = null)
        {
            this.BlobClient = client;
            this.Container = container;
            SASStartDate.SelectedDate = DateTime.Today;
            SASEndDate.SelectedDate = DateTime.Today.AddDays(7);
            if (!String.IsNullOrEmpty(blobName))
            {
                SASBlobName.Text = blobName;
            }
        }

        #region Shared Access Signature Tab handlers

        private void CmdSASCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        //**************************
        //*                        *
        //*  CmdSASGenerate_Click  *
        //*                        *
        //**************************
        // Generate an ad-hoc shared access signature.

        private void CmdSASGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (!SASStartDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please enter a starting date.", "Start Date Required");
                return;
            }

            if (!SASEndDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please enter an end date.", "End Date Required");
                return;
            }

            if (SASStartDate.SelectedDate.Value > SASEndDate.SelectedDate.Value)
            {
                MessageBox.Show("Please enter a valid date range. Start date may not exceed end date.", "End Date Required");
                return;
            }

            // Parameters have been set. Proceed to generate ad-hoc shared access signature.

            try
            {
                Cursor = Cursors.Wait;

                SASInitialPanel.Text = "Generating Shared Access Signature...";

                SharedAccessBlobPermissions permissions = SharedAccessBlobPermissions.None;
                if (SASActionDelete.IsChecked.Value)
                {
                    permissions = permissions | SharedAccessBlobPermissions.Delete;
                }
                if (SASActionList.IsChecked.Value)
                {
                    permissions = permissions | SharedAccessBlobPermissions.List;
                }
                if (SASActionRead.IsChecked.Value)
                {
                    permissions = permissions | SharedAccessBlobPermissions.Read;
                }
                if (SASActionWrite.IsChecked.Value)
                {
                    permissions = permissions | SharedAccessBlobPermissions.Write;
                }

                String signature = Container.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    Permissions = permissions,
                    SharedAccessStartTime = SASStartDate.SelectedDate.Value,
                    SharedAccessExpiryTime = SASEndDate.SelectedDate.Value.AddDays(1)
                });

                if (String.IsNullOrEmpty(SASBlobName.Text))
                {
                    signature = Container.Uri.ToString() + signature;
                }
                else
                {
                    signature = Container.Uri.ToString() + "/" + SASBlobName.Text + signature;
                }

                SASInitialPanel.Visibility = Visibility.Collapsed;
                SASGeneratedUri.Text = signature;
                SASResultsPanel.Visibility = Visibility.Visible;

                Clipboard.SetText(signature);

                Cursor = Cursors.Arrow;
            }
            catch(Exception ex)
            {
                MessageBox.Show("An error occurred attempting to generate a shared access signature:\n\n" + ex.Message, "Error Generating Signature");
            }
        }

        #endregion

        #region Access Level Tab handlers

        private void CmdApply_Click(object sender, RoutedEventArgs e)
        {
            AccessLevelModified = true;
            DialogResult = true;
        }

        private void CmdCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        #endregion

        private void CmdSASTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                String uri = SASGeneratedUri.Text;
                System.Diagnostics.Process.Start(uri);
            }
            catch(Exception ex)
            {
                MessageBox.Show("An error occurred attempting to browse to the Uri:\n\n" + ex.Message, "Error  Browsing to Uri");
            }
        }
    }
}
