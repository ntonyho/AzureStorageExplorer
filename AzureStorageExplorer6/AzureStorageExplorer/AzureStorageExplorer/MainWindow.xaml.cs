//*******************************
//*                             *
//*  Azure Storage Explorer v6  *
//*                             *
//*******************************
// Written by David Pallmann.
// This code is not complete.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using AzureStorageExplorer.Helpers;
using System.Windows;
namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Class Variables

        private const String cipherKey = "lkjsojkweu798ynfgs";

        public static List<AzureAccount> Accounts = null;
        public static System.Windows.Controls.TabControl StorageViewsTabControl = null;
        public static Dictionary<String, String> ContentTypes = new Dictionary<string, string>();

        #endregion

        #region Initialization

        // Constructor.

        public MainWindow()
        {
            InitializeComponent();
            StorageViewsTabControl = StorageViews;
            Accounts = new List<AzureAccount>();
            CenterWindowOnScreen();
            LoadAccountList();
            LoadContentTypes();
            DisplayAccountList();
        }

        #endregion

        #region UI Interaction Handlers

        //**********************
        //*                    *
        //*  AddAccount_Click  *
        //*                    *
        //**********************
        // Display dialog for adding a storage account.

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            AccountDialog dlg = new AccountDialog();
            dlg.Title = "Add Storage Account";
            dlg.IsEdit = false;

            if (dlg.ShowDialog().Value)
            {
                String accountName = dlg.AccountName.Text;
                
                AzureAccount account = new AzureAccount()
                {
                    IsDeveloperAccount = dlg.AccountTypeDev.IsChecked.Value,
                    Name = dlg.AccountName.Text,
                    Key = dlg.AccountKey.Text,
                    UseSSL = dlg.UseSSL,
                    EndpointDomain = dlg.EndpointDomain.Text.Trim()
                };

                if (String.IsNullOrEmpty(account.EndpointDomain))
                {
                    account.EndpointDomain = "core.windows.net";
                }

                if (account.IsDeveloperAccount)
                {
                    account.Name = "DevStorage";
                }

                Accounts.Add(account);

                SaveAccountList();

                DisplayAccountList();

                AddStorageView(accountName);

            }
        }

        //*************************
        //*                       *
        //*  RemoveAccount_Click  *
        //*                       *
        //*************************
        // Remove selected account.

        private void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            if (AccountList.SelectedIndex==-1 | AccountList.SelectedIndex==0) return;

            int index = AccountList.SelectedIndex-1;

            String accountName = Accounts[index].Name;

            // Confirm removal of storage account.

            if (System.Windows.MessageBox.Show("Are you sure you want to remove '" + accountName + "' from your list of storage accounts?", "Confirm Remove Storage Account", MessageBoxButton.YesNo)==MessageBoxResult.Yes)
            { 
                // If account is currently displayed, removed its tab.

                if (StorageViewsTabControl.Items != null)
                {
                    foreach(TabItem item in StorageViewsTabControl.Items)
                    {
                        StorageView view = item.Content as StorageView;
                        if (view != null && view.Account.Name==accountName)
                        { 
                            StorageViewsTabControl.Items.Remove(item);
                            break;
                        }
                    }
                }

                // Remove account from internal list and save it. Remove account from accounts selection list.

                Cursor = System.Windows.Input.Cursors.Wait;
                AccountList.Items.RemoveAt(index);
                AccountList.SelectedIndex = -1;
                Accounts.RemoveAt(index);

                SaveAccountList();

                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }


        //***********************
        //*                     *
        //*  AddStorageAccount  *
        //*                     *
        //***********************
        // Add a storage view tab.

        private void AddStorageView(String accountName)
        {
            AccountSelector.Visibility = Visibility.Collapsed;
            AccountMessage.Text = "Loading Storage Account " + accountName;
            AccountMessage.Visibility = Visibility.Visible;
            AccountAction.Visibility = Visibility.Visible;

            AzureAccount account = null;

            // Find the selected account name in the account list.

            foreach (AzureAccount acct in Accounts)
            {
                if (acct.Name == accountName)
                {
                    account = acct;
                }
            }

            if (account == null) return;

            // Check that the account isn't already in a tab; if it is, do nothing.

            if (StorageViewsTabControl.Items != null)
            {
                foreach (TabItem item in StorageViewsTabControl.Items)
                {
                    StorageView view = item.Content as StorageView;
                    if (view != null && view.Account.Name == accountName)
                    {
                        StorageViewsTabControl.SelectedItem = item;
                        AccountAction.Visibility = Visibility.Collapsed;
                        AccountSelector.Visibility = Visibility.Visible;
                        return;
                    }
                }
            }

            Task task = Task.Factory.StartNew(() =>
            {
            });

            task.ContinueWith((t) =>
            {
                TabItem item = new TabItem();
                //item.Header = account.Name;

                StackPanel panel = new StackPanel();
                panel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                TextBlock title = new TextBlock() { Text = account.Name + " " };
                panel.Children.Add(title);
                TextBlock closeBox = new TextBlock() { Text = "×" };
                closeBox.Tag = account.Name;
                closeBox.Cursor = System.Windows.Input.Cursors.Hand;
                closeBox.MouseDown += new MouseButtonEventHandler(CloseStorageView);
                panel.Children.Add(closeBox);
                item.Header = panel;
                StorageView storageView = new StorageView();

                storageView.Account = account;
                item.Content = storageView;

                storageView.LoadLeftPane();

                item.Content = storageView; ;

                StorageViews.Items.Add(item);

                StorageViews.SelectedItem = item;

                AccountAction.Visibility = Visibility.Collapsed;
                AccountSelector.Visibility = Visibility.Visible;

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void CloseStorageView(object sender, MouseButtonEventArgs e)
        {
            StorageViewsTabControl.Items.Remove(StorageViewsTabControl.SelectedItem);
            //TextBlock tb = sender as TextBlock;
            //String accountName = tb.Tag as String;
            //if (StorageViewsTabControl.Items != null)
            //{
            //    foreach(TabItem item in StorageViewsTabControl.Items)
            //    {
            //        StorageView view = item.Content as StorageView;
            //        if (view != null && view.Account.Name==accountName)
            //        {
            //            StorageViewsTabControl.Items.Remove()
            //            return;
            //        }
            //    }
            //}
        }

        #endregion

        //************************
        //*                      *
        //*  DisplayAccountList  *
        //*                      *
        //************************
        // Populate the account list combo box.

        private void DisplayAccountList()
        {
            AccountList.Items.Clear();
            AccountList.Items.Add("--- Select a Storage Account ---");
            if (Accounts.Count > 0)
            {
                AccountList.Items.Add("(all)");
            }
            foreach (AzureAccount account in Accounts)
            {
                AccountList.Items.Add(account.Name);
            }
            AccountList.SelectedIndex = 0;
        }


        //*********************
        //*                   *
        //*  LoadAccountList  *
        //*                   *
        //*********************
        // Load the account list combo box.

        private void LoadAccountList()
        {
            String filename = System.Windows.Forms.Application.UserAppDataPath + "\\AzureStorageExplorer6.dt1";

            Accounts.Clear();

            if (File.Exists(filename))
            {
                using (TextReader reader = File.OpenText(filename))
                {
                    reader.ReadLine();  // version

                    String line;
                    String[] items;
                    while ((line = reader.ReadLine()) != null)
                    {
                        try
                        {
                            line = StringCipher.Decrypt(line, cipherKey);

                            items = line.Split('|');
                            if (items.Length >= 4)
                            {
                                AzureAccount account = new AzureAccount()
                                {
                                    Name = items[0],
                                    Key = items[1],
                                    IsDeveloperAccount = (items[2] == "1"),
                                    UseSSL = (items[3] == "1")
                                };
                                if (items.Length >= 5)
                                {
                                    account.EndpointDomain = items[4];
                                }
                                else
                                {
                                    account.EndpointDomain = "core.windows.net";
                                }
                                Accounts.Add(account);
                            }
                        }
                        catch(Exception)
                        {
                            // If something is wrong in the account data file, don't let that stop the rest from loading.
                        }
                    }
                }
            }
        }


        //*********************
        //*                   *
        //*  SaveAccountList  *
        //*                   *
        //*********************
        // Save the account list to disk.

        private void SaveAccountList()
        {
            // Sort account list.

            Accounts = Accounts.OrderBy(o => o.Name).ToList();

            // Save account list, encrypted.

            String filename = System.Windows.Forms.Application.UserAppDataPath + "\\AzureStorageExplorer6.dt1";

            using (TextWriter writer = File.CreateText(filename))
            {
                writer.WriteLine("v6.0-1");
                foreach (AzureAccount account in Accounts)
                {
                    String line = account.Name + "|";
                    line = line + account.Key + "|";

                    if (account.IsDeveloperAccount)
                    {
                        line = line + "1|";
                    }
                    else
                    {
                        line = line + "0|";
                    }

                    if (account.UseSSL)
                    {
                        line = line + "1|";
                    }
                    else
                    {
                        line = line + "0|";
                    }

                    line = line + account.EndpointDomain + "|";
                    line = StringCipher.Encrypt(line, cipherKey);

                    writer.WriteLine(line);

                }
            }
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


        //**********************************
        //*                                *
        //*  AccountList_SelectionChanged  *
        //*                                *
        //**********************************
        // An account selection was made. Add a storage view tab. If (all) is selected, open a storage view tab for each account.

        private void AccountList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String name = AccountList.SelectedValue as String;
            if (name != "--- Select a Storage Account ---")
            {
                if (name == "(all)")
                {
                    // Close any open tabs.

                    while (StorageViewsTabControl.Items.Count > 0)
                    {
                        StorageViewsTabControl.Items.RemoveAt(0);
                    }

                    // Open a tab for each storage account.

                    int index = 0;
                    foreach(String item in AccountList.Items)
                    {
                        if (index > 1)
                        {
                            AddStorageView(item);
                        }
                        index++;
                    }
                }
                else
                { 
                    // Open a tab for the selected storage account.

                    AddStorageView(name);
                }
            }
        }


        //*********************
        //*                   *
        //*  RemoveActiveTab  *
        //*                   *
        //*********************
        // Remove a tab from the tab control.

        public static void RemoveActiveTab()
        {
            StorageViewsTabControl.Items.Remove(StorageViewsTabControl.SelectedItem);
        }

        #region Settings Menu

        //********************
        //*                  *
        //*  MainMenu_About  *
        //*                  *
        //********************
        //About Azure Storage Explorer.

        private void MainMenu_About(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Azure Storage Explorer 6 Preview 3 (6.0.3.1)", "About");
        }


        //************************
        //*                      *
        //*  SettingsIcon_Click  *
        //*                      *
        //************************
        // Display top right settings menu when setting icon is left-clicked.

        private void SettingsIcon_Click(object sender, MouseButtonEventArgs e)
        {
            MainContextMenu.PlacementTarget = this;
            MainContextMenu.IsOpen = true;
        }


        //*********************
        //*                   *
        //*  MainMenu_Portal  *
        //*                   *
        //*********************
        // Launch the Microsoft Azure management portal in a browser. manage.windowsazure.com

        private void MainMenu_Portal(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://manage.windowsazure.com");
            }
            catch(Exception)
            {
            }
        }

        //***************************
        //*                         *
        //*  MainMenu_CodeplexPage  *
        //*                         *
        //***************************
        // Launch the Azure Storage Explorer codeplex page.

        private void MainMenu_CodeplexPage(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://azurestorageexplorer.codeplex.com");
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Content Types

        //***********************
        //*                     *
        //*  ContentType_Click  *
        //*                     *
        //***********************
        // Edit content types.

        private void ContentType_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ContentTypesDialog dlg = new ContentTypesDialog();

            dlg.LoadContentTypes(ContentTypes);

            if (dlg.ShowDialog().Value)
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;

                ContentTypes = dlg.GetContentTypes();
                SaveContentTypes();

                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        //**********************
        //*                    *
        //*  LoadContentTypes  *
        //*                    *
        //**********************
        // Load save table of file types and content types. If file doesn't exist, set to default list.

        private void LoadContentTypes()
        {
            try
            {
                String filename = System.Windows.Forms.Application.UserAppDataPath + "\\AzureStorageExplorer6-ContentTypes.dt1";
                String line, name, value;

                MainWindow.ContentTypes.Clear();

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
                                try
                                {
                                    name = items[0];
                                    value = items[1];
                                    MainWindow.ContentTypes.Add(name, value);
                                }
                                catch (Exception)
                                {
                                }
                            } // end if items.Length >= 2
                        } // end while
                    } // end using TextReader
                } // end if
                else
                {
                    // Load default content types table.

                    MainWindow.ContentTypes.Add(".avi", "video/msvideo");
                    MainWindow.ContentTypes.Add(".bmp", "image/bmp");
                    MainWindow.ContentTypes.Add(".css", "text/css");
                    MainWindow.ContentTypes.Add(".dtd", "application/xml-dtd");
                    MainWindow.ContentTypes.Add(".doc", "application/msword");
                    MainWindow.ContentTypes.Add(".docx", "application/msword");
                    MainWindow.ContentTypes.Add(".exe", "application/octet-stream");
                    MainWindow.ContentTypes.Add(".gif", "image/gif");
                    MainWindow.ContentTypes.Add(".gz", "application/x-gzip");
                    MainWindow.ContentTypes.Add(".htm", "text/html");
                    MainWindow.ContentTypes.Add(".html", "text/html");
                    MainWindow.ContentTypes.Add(".jar", "application/java-archive");
                    MainWindow.ContentTypes.Add(".jpg", "image/jpeg");
                    MainWindow.ContentTypes.Add(".jpeg", "image/jpeg");
                    MainWindow.ContentTypes.Add(".js", "application/x-javascript");
                    MainWindow.ContentTypes.Add(".midi", "audio/x-midi");
                    MainWindow.ContentTypes.Add(".mp3", "audio/mpeg");
                    MainWindow.ContentTypes.Add(".mpg", "video/mpeg");
                    MainWindow.ContentTypes.Add(".mpeg", "video/mpeg");
                    MainWindow.ContentTypes.Add(".ogg", "audio/vorbis, application/ogg");
                    MainWindow.ContentTypes.Add(".pdf", "application/pdf");
                    MainWindow.ContentTypes.Add(".pl", "application/x-perl");
                    MainWindow.ContentTypes.Add(".png", "image/png");
                    MainWindow.ContentTypes.Add("ppt", "application/vnd.ms-powerpoint");
                    MainWindow.ContentTypes.Add(".pptx", "application/vnd.ms-powerpoint");
                    MainWindow.ContentTypes.Add(".ps", "application/postscript");
                    MainWindow.ContentTypes.Add(".qt", "video/quicktime");
                    MainWindow.ContentTypes.Add(".ra", "audio/x-pn-realaudio, audio/vnd.rn-realaudio");
                    MainWindow.ContentTypes.Add(".ram", "audio/x-pn-realaudio, audio/vnd.rn-realaudio");
                    MainWindow.ContentTypes.Add(".rdf", "application/rdf, application/rdf+xml");
                    MainWindow.ContentTypes.Add(".rtf", "application/rtf");
                    MainWindow.ContentTypes.Add(".sgml", "text/sgml");
                    MainWindow.ContentTypes.Add(".svg", "image/svg+xml");
                    MainWindow.ContentTypes.Add(".swf", "application/x-shockwave-flash");
                    MainWindow.ContentTypes.Add(".tar.gz", "application/x-tar");
                    MainWindow.ContentTypes.Add(".tgz", "application/x-tar");
                    MainWindow.ContentTypes.Add(".tiff", "image/tiff");
                    MainWindow.ContentTypes.Add(".tsv", "text/tab-separated-values");
                    MainWindow.ContentTypes.Add(".txt", "text/plain");
                    MainWindow.ContentTypes.Add(".wav", "audio/wav, audio/x-wav");
                    MainWindow.ContentTypes.Add(".xls", "application/vnd.ms-excel");
                    MainWindow.ContentTypes.Add(".xml", "application/xml");
                    MainWindow.ContentTypes.Add(".zip", "application/zip");
                }
            } // end try
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
            }
        }

        //**********************
        //*                    *
        //*  SaveContentTypes  *
        //*                    *
        //**********************
        // Save the content types to disk.

        private void SaveContentTypes()
        {
            // Sort account list.

            Accounts = Accounts.OrderBy(o => o.Name).ToList();

            String filename = System.Windows.Forms.Application.UserAppDataPath + "\\AzureStorageExplorer6-ContentTypes.dt1";

            using (TextWriter writer = File.CreateText(filename))
            {
                foreach (KeyValuePair<String, String> ct in ContentTypes)
                {
                    writer.WriteLine(ct.Key + "|" + ct.Value);
                }
            }
        }

        #endregion

    }
}
