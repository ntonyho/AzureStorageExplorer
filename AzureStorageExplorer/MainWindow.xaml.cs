using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Neudesic.AzureStorageExplorer;
using Neudesic.AzureStorageExplorer.Data;
using Neudesic.AzureStorageExplorer.ViewModel;
using Neudesic.AzureStorageExplorer.Dialogs;

namespace Neudesic.AzureStorageExplorer
{
    public partial class MainWindow : System.Windows.Window
    {
        #region Properties

        public static System.Windows.Window Window { get; set; }

        public static List<Exception> Exceptions = new List<Exception>();

        public MainWindowViewModel ViewModel
        {
            get
            {
                return DataContext as MainWindowViewModel;
            }
        }

        #endregion

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();

            Window = this as System.Windows.Window;
            this.Loaded += new System.Windows.RoutedEventHandler(MainWindow_Loaded);
        }

        void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (StorageAccountsComboBox.ItemsSource != null)
            {
                StorageAccountsComboBox.SelectedIndex = 0;
            }

            if (ViewModel.ShowWelcomeOnStartup)
            {
                ShowWelcome();
            }
        }

        #endregion

        #region Storage Account Navigation and Administration

        // A storage account has been selected. Execute the ViewStorageAccount command for the selected account,
        // which will open a workspace tab.

        private void StorageAccountsComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (StorageAccountsComboBox.SelectedIndex > 0)
            {
                AccountViewModel avm = StorageAccountsComboBox.SelectedItem as AccountViewModel;
                RelayCommand command = avm.Command as RelayCommand;
                StorageAccount account = avm.Account;

                if (avm.AccountName == "DevStorage" && !DeveloperStorageRunning())
                {
                    MessageBox.Show("Windows Azure Developer Storage is not running.\r\n\r\nThe process DSService.exe is not detected", "Developer Storage Not Detected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                
                if (command != null)
                {
                    command.Execute(account);
                }
            }
        }

        // Return true if Developer Storage is running by scanning for the DSService.exe process.

        private bool DeveloperStorageRunning()
        {
            const string name = "DSService";

            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains(name))
                {
                    return true;
                }
            }
            return false;
        }

        private void AddStorageAccount_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddAccountDialog dlg = new AddAccountDialog();
            dlg.Owner = MainWindow.Window;
            dlg.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            dlg.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            if (dlg.ShowDialog().Value)
            {
                try
                {
                    string name = dlg.AccountName.Text;
                    string key = dlg.AccountKey.Text;

                    if (name == "DevStorage" && !DeveloperStorageRunning())
                    {
                        MessageBox.Show("Windows Azure Developer Storage is not running.\r\n\r\nThe process DSService.exe is not detected", "Developer Storage Not Detected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                
                    StorageAccountsComboBox.SelectedItem = ViewModel.AddAccount(name, key);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred saving the configuration.\r\n\r\n" + ex.ToString(),
                        "Could Not Save Configuration", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void RemoveStorageAccount_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (StorageAccountsComboBox.SelectedIndex <= 0 || StorageAccountsComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a storage account to remove.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Exclamation);               
                return;
            }

            AccountViewModel avm = StorageAccountsComboBox.SelectedItem as AccountViewModel;

            if (MessageBox.Show("Are you SURE you want to remove the account '" + avm.AccountName + 
                "'?\r\n\r\nThis will remove the account name and key from Azure Storage Explorer's saved configuration.\r\n\r\nCloud storage is not affected by this action.",
                "Confirm Account Delete", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                try
                {
                    StorageAccountsComboBox.SelectedIndex = 0;
                    ViewModel.RemoveAccount(avm);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred saving the configuration.\r\n\r\n" + ex.ToString(),
                        "Could Not Save Configuration", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        #endregion

        #region Menu Navigation

        #region View Menu

        #region View Errors

        private void ViewErrorsCanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ViewErrorsExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ViewErrorsDialog dlg = new ViewErrorsDialog(MainWindow.Exceptions);
            dlg.Owner = MainWindow.Window;
            dlg.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            dlg.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            dlg.ShowDialog();
        }

        #endregion

        #endregion

        #region Help Menu

        #region Show Welcome Screen

        private void HelpWelcomeCanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void HelpWelcomeExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ShowWelcome();
        }

        private void ShowWelcome()
        {
            WelcomeDialog dlg = new WelcomeDialog();
            dlg.Owner = MainWindow.Window;
            dlg.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            dlg.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            dlg.ShowWelcomeOnStartup = ViewModel.ShowWelcomeOnStartup;
            dlg.ShowDialog();
            ViewModel.ShowWelcomeOnStartup = dlg.ShowWelcomeOnStartup;
        }

        #endregion

        #region Give Feedback

        private void HelpFeedbackCanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void HelpFeedbackExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            GiveFeedbackOnCodeplex();
        }

        public static void GiveFeedbackOnCodeplex()
        {
            string url = "http://azurestorageexplorer.codeplex.com/discussions";
            System.Diagnostics.Process.Start(url);
        }

        #endregion

        #region Help About

        private void HelpAboutCanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void HelpAboutExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Azure Storage Explorer version 4.0 Beta 1 (10.22.2010).\r\n\r\nA community donation of Neudesic.", "About Azure Storage Explorer", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        #endregion

        #region Help (F1) - User Guide

        private void HelpCanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void HelpExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            MainWindow.OpenUserGuide();
        }

        public static void OpenUserGuide()
        {
            try
            {
                string url = "\"Docs\\Azure Storage Explorer 4 User Guide.pdf\"";
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("The user guide could not be opened.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        #endregion

        #endregion

        #endregion
    }
}