using System;
using System.Collections.Generic;
using System.Configuration;
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
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for Account.xaml
    /// </summary>
    public partial class AccountDialog : Window
    {
        private bool DialogInitialized = false;
        
        public bool UseSSL = false;
        public bool IsEdit = false;

        public const String DefaultEndpoint = "core.windows.net";
        public const String ChinaEndpoint = "core.chinacloudapi.cn";

        public AccountDialog()
        {
            InitializeComponent();
            this.Owner = Application.Current.MainWindow;
            DialogInitialized = true;
        }

        public void SetEndpoint(String endpoint)
        {
            if (endpoint == DefaultEndpoint || endpoint == String.Empty)
            {
                EndpointDefault.IsChecked = true;
            }
            else if (endpoint == ChinaEndpoint)
            {
                EndpointChina.IsChecked = true;
            }
            else
            {
                EndpointOther.IsChecked = true;
                EndpointDomain.Text = endpoint;
            }
        }

        // Test access to the account.

        private void TestAccount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Message.Text = String.Empty;

                String accountName = AccountName.Text;
                String accountKey = AccountKey.Text;

                bool isDevStorage = false;
                if (AccountTypeDev.IsChecked.Value)
                {
                    isDevStorage = true;
                }
                else
                {
                    if (String.IsNullOrEmpty(accountName) || String.IsNullOrEmpty(accountKey))
                    {
                        Message.Text = "An account name and account key are required";
                        return;
                    }
                }

                Cursor = Cursors.Wait;

                ButtonPanel.Visibility = System.Windows.Visibility.Hidden;
                Message.Text = "Testing account access...";

                bool useSSL = false;
                if (UseHTTPS.IsChecked.HasValue && UseHTTPS.IsChecked.Value)
                {
                    useSSL = true;
                }

                TestAccountAccess(accountName, accountKey, isDevStorage);

                Message.Text = "✔ Account access successful";
                ButtonPanel.Visibility = System.Windows.Visibility.Visible;

                Cursor = Cursors.Arrow;
            }
            catch (Exception ex)
            {
                Message.Text = "× Error accessing account";
                ButtonPanel.Visibility = System.Windows.Visibility.Visible;
                Cursor = Cursors.Arrow;
                MessageBox.Show("An error occurred attempting to access the account:\n\n" + ex.Message, "Error Accessing Account");
            }
        }

        // Save account.

        private void SaveAccount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                Message.Text = String.Empty;

                String accountName = AccountName.Text;
                String accountKey = AccountKey.Text;

                bool isDevStorage = false;
                if (AccountTypeDev.IsChecked.Value)
                {
                    isDevStorage = true;
                    accountName = "DevStorage";
                    AccountName.Text = accountName;
                }

                if (!isDevStorage)
                {
                    if (String.IsNullOrEmpty(accountName) || String.IsNullOrEmpty(accountKey))
                    {
                        Cursor = Cursors.Arrow;
                        Message.Text = "An account name and account key are required";
                        return;
                    }
                }

                if (!IsEdit)
                {
                    foreach (AzureAccount account in MainWindow.Accounts)
                    {
                        if (account.Name == accountName)
                        {
                            Cursor = Cursors.Arrow;
                            Message.Text = "Error: an account with that name already exists";
                            return;
                        }
                    }
                }

                ButtonPanel.Visibility = System.Windows.Visibility.Hidden;
                Message.Text = "Testing account access...";

                UseSSL = false;
                if (UseHTTPS.IsChecked.HasValue && UseHTTPS.IsChecked.Value)
                {
                    UseSSL = true;
                }

                // Attempt to list blob containers to test the account can be accessed.

                TestAccountAccess(accountName, accountKey, isDevStorage);

                Message.Text = "Updating account list...";
                ButtonPanel.Visibility = System.Windows.Visibility.Visible;

                Cursor = Cursors.Arrow;

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                Message.Text = "× Error accessing account";
                ButtonPanel.Visibility = System.Windows.Visibility.Visible;
                Cursor = Cursors.Arrow;
                MessageBox.Show("An error occurred attempting to access the account:\n\n" + ex.Message, "Error Accessing Account");
            }
        }

        private void TestAccountAccess(string accountName, string accountKey, bool isDevStorage)
        {
            CloudStorageAccount account = null;

            if (isDevStorage)
            {
                account = CloudStorageAccount.DevelopmentStorageAccount;
            }
            else
            {
                account = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), EndpointDomain.Text, UseSSL);
            }

            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            IEnumerable<CloudBlobContainer> containers = blobClient.ListContainers();
            foreach (Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer container in containers)
            {
            }
        }

        private void CancelAccount_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }


        private void AccountType_Checked(object sender, RoutedEventArgs e)
        {
            if (AccountTypeDev == null) return;

            if (AccountTypeDev.IsChecked.Value)
            {
                AccountNameLabel.Visibility = Visibility.Collapsed;
                AccountName.Visibility = Visibility.Collapsed;
                AccountName.Text = "DevStorage";
                AccountKeyLabel.Visibility = Visibility.Collapsed;
                AccountKey.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                AccountNameLabel.Visibility = Visibility.Visible;
                AccountName.Visibility = Visibility.Visible;
                AccountName.Text = String.Empty;
                AccountName.IsReadOnly = false;
                AccountKeyLabel.Visibility = Visibility.Visible;
                AccountKey.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void EndpointDefault_Click(object sender, RoutedEventArgs e)
        {
            //if (!DialogInitialized) return;
            EndpointDomain.Focus();
            EndpointDomain.Text = DefaultEndpoint;
        }

        private void EndpointChina_Click(object sender, RoutedEventArgs e)
        {
            //if (!DialogInitialized) return;
            EndpointDomain.Focus();
            EndpointDomain.Text = ChinaEndpoint;
        }

        private void EndpointOther_Click(object sender, RoutedEventArgs e)
        {
            //if (!DialogInitialized) return;

            if (EndpointDomain.Text == DefaultEndpoint || EndpointDomain.Text == ChinaEndpoint)
            {
                EndpointDomain.Text = String.Empty;
            }
            EndpointDomain.Focus();
        }
    }
}
