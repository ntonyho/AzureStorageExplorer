using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using Neudesic.AzureStorageExplorer.Data;
using Neudesic.AzureStorageExplorer.Dialogs;
using Neudesic.AzureStorageExplorer.Properties;

namespace Neudesic.AzureStorageExplorer.ViewModel
{
    /// <summary>
    /// The ViewModel for the application's main window.
    /// </summary>
    public class MainWindowViewModel : WorkspaceViewModel
    {
        private const string ConfigFilename = "AzureStorageExplorer.config";

        #region Fields

        ObservableCollection<AccountViewModel> _accounts;
        ReadOnlyCollection<CommandViewModel> _commands;
        ObservableCollection<WorkspaceViewModel> _workspaces;

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            base.DisplayName = Strings.MainWindowViewModel_DisplayName;

            LoadConfiguration();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Returns a read-only list of commands 
        /// that the UI can display and execute.
        /// </summary>
        public ReadOnlyCollection<CommandViewModel> Commands
        {
            get
            {
                if (_commands == null)
                {
                    List<CommandViewModel> cmds = this.CreateCommands();
                    _commands = new ReadOnlyCollection<CommandViewModel>(cmds);
                }
                return _commands;
            }
        }

        List<CommandViewModel> CreateCommands()
        {

            return new List<CommandViewModel>
            {
                new CommandViewModel(
                    Strings.MainWindowViewModel_Command_ViewStorageAccount,
                    new RelayCommand(param => this.ViewStorageAccount(param as StorageAccount)))
            };
        }

        #endregion

        #region Accounts

        /// <summary>
        /// Returns a read-only list of commands 
        /// that the UI can display and execute.
        /// </summary>
        public ObservableCollection<AccountViewModel> Accounts
        {
            get
            {
                return _accounts;
            }
            set
            {
                _accounts = value;
                OnPropertyChanged("Accounts");
            }
        }

        List<AccountViewModel> CreateAccounts()
        {
            return new List<AccountViewModel>
            {
                new AccountViewModel(
                    "-- Select a Storage Account --", String.Empty,
                    new RelayCommand(param => this.ViewStorageAccount(param as StorageAccount))),

                new AccountViewModel(
                    "neudesic", "rIruMtQv2WO3I0/tb5G9CeQ5zNhcsFMBebekiZ0nPnPElD7VMC+JvnLXMMoE+bK8uRHQQ+IDRVq1O4PnGO3AFQ==",
                    new RelayCommand(param => this.ViewStorageAccount(param as StorageAccount))),

                new AccountViewModel(
                    "neudesicteststorage", "uzYJTykLN3I1zfCzHXmlat6sbodpsUXt7dMUbeB8nigcfMGSi6yD2RfVrmhNAvleimFg1x+WtgcZ0GZAsZ0d1g==",
                    new RelayCommand(param => this.ViewStorageAccount(param as StorageAccount))),

                new AccountViewModel(
                    "perfectvacaystorage", "dyoAv4BJkHjWbIO0PO9KuU7+KTr+hbLMu04eBd+bXlB9VWGUYcO39ZgjAEUizGor6EMCDI3jX5SatlZh3k9A/Q==",
                    new RelayCommand(param => this.ViewStorageAccount(param as StorageAccount)))
            };
        }

        #endregion // Commands

        #region Workspaces

        /// <summary>
        /// Returns the collection of available workspaces to display.
        /// A 'workspace' is a ViewModel that can request to be closed.
        /// </summary>
        public ObservableCollection<WorkspaceViewModel> Workspaces
        {
            get
            {
                if (_workspaces == null)
                {
                    _workspaces = new ObservableCollection<WorkspaceViewModel>();
                    _workspaces.CollectionChanged += this.OnWorkspacesChanged;
                }
                return _workspaces;
            }
        }

        void OnWorkspacesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.NewItems)
                    workspace.RequestClose += this.OnWorkspaceRequestClose;

            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.OldItems)
                    workspace.RequestClose -= this.OnWorkspaceRequestClose;
        }

        void OnWorkspaceRequestClose(object sender, EventArgs e)
        {
            WorkspaceViewModel workspace = sender as WorkspaceViewModel;
            workspace.Dispose();
            this.Workspaces.Remove(workspace);
        }

        #endregion // Workspaces

        #region Private Helpers

        void ViewStorageAccount(StorageAccount account)
        {
            // See if the storage account is already displayed in a tab. If it is, select it.

            foreach (WorkspaceViewModel wvm in this.Workspaces)
            {
                if (wvm.DisplayName == account.Name)
                {
                    this.SetActiveWorkspace(wvm);
                    return;
                }
            }

            // Add a new workspace tab for the selected storage account.

            StorageAccountViewModel workspace = new StorageAccountViewModel(account);
            this.Workspaces.Add(workspace);

            this.SetActiveWorkspace(workspace);
        }

        void SetActiveWorkspace(WorkspaceViewModel workspace)
        {
            Debug.Assert(this.Workspaces.Contains(workspace));

            ICollectionView collectionView = CollectionViewSource.GetDefaultView(this.Workspaces);
            if (collectionView != null)
                collectionView.MoveCurrentTo(workspace);
        }

        #endregion

        #region Storage Accounts Load/Store

        private void LoadConfiguration()
        {
            string name, value;
            int pos;
            List<AccountViewModel> accounts = new List<AccountViewModel>();
            AccountViewModel avm = null;

            accounts.Add(
                new AccountViewModel(
                        "-- Select a Storage Account --", String.Empty,
                        new RelayCommand(param => this.ViewStorageAccount(param as StorageAccount)))
                        );

            String folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AzureStorageExplorer\\";
            Directory.CreateDirectory(folder);
            string filename = folder + ConfigFilename;

            if (File.Exists(filename))
            {
                string item = String.Empty;
                string line;
                using (TextReader tr = File.OpenText(filename))
                {
                    while ((line = tr.ReadLine()) != null)
                    {
                        if (line.StartsWith("["))
                        {
                            if (avm != null && !string.IsNullOrEmpty(avm.DisplayName))
                            {
                                accounts.Add(avm);
                            }
                            switch (line)
                            {
                                case "[Account]":
                                    item = line;
                                    avm = new AccountViewModel(null, null, new RelayCommand(param => this.ViewStorageAccount(param as StorageAccount)));
                                    break;
                                case "[Options]":
                                    item = line;
                                    break;
                            }
                        }
                        else
                        {
                            pos = line.IndexOf('=');
                            if (pos != -1)
                            {
                                name = line.Substring(0, pos);
                                value = line.Substring(pos + 1);
                                switch (item)
                                {
                                    case "[Options]":
                                        switch (name)
                                        {
                                            case "ShowWelcomeOnStartup":
                                                showWelcomeOnStartup = (value == "1");
                                                break;
                                        }
                                        break;
                                    case "[Account]":
                                        switch (name)
                                        {
                                            case "Name":
                                                avm.AccountName = value;
                                                break;
                                            case "ConnectionString":
                                                avm.Key = value;
                                                break;
                                            case "AutoOpen":
                                                //account.AutoOpen = Boolean.Parse(value);
                                                break;
                                            case "UseHttps":
                                                //account.UseHttps = Boolean.Parse(value);
                                                break;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    tr.Close();

                    if (avm != null)
                    {
                        if (avm != null && !string.IsNullOrEmpty(avm.AccountName))
                        {
                            accounts.Add(avm);
                        }
                        avm = null;
                    }
                }
            }

            Accounts = new ObservableCollection<AccountViewModel>(accounts);
        }

        private string BoolText(bool value)
        {
            if (value)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }

        private void SaveConfiguration()
        {
            String folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AzureStorageExplorer\\";
            Directory.CreateDirectory(folder);
            string filename = folder +  ConfigFilename;

            using (TextWriter tw = File.CreateText(filename))
            {
                tw.WriteLine("[Options]");
                tw.WriteLine("ShowWelcomeOnStartup=" + BoolText(ShowWelcomeOnStartup));

                if (Accounts != null)
                {
                    foreach (AccountViewModel avm in Accounts)
                    {
                        if (avm.AccountName == "DevStorage" || !String.IsNullOrEmpty(avm.Key))
                        {
                            tw.WriteLine("[Account]");
                            tw.WriteLine("Name=" + avm.AccountName);
                            tw.WriteLine("ConnectionString=" + avm.Key);
                        }
                    }

                }
                tw.Close();
            }
        }

        #endregion

        public AccountViewModel AddAccount(string name, string key)
        {
            AccountViewModel avm = new AccountViewModel(name, key,
                new RelayCommand(param => this.ViewStorageAccount(param as StorageAccount))
                );

            Accounts.Add(avm);
            
            List<AccountViewModel> list = new List<AccountViewModel>(_accounts.ToArray());
            list.Sort();
            _accounts = new ObservableCollection<AccountViewModel>(list);

            SaveConfiguration();

            OnPropertyChanged("Accounts");

            return avm;
        }

        public void RemoveAccount(AccountViewModel avm)
        {
            Accounts.Remove(avm);
            SaveConfiguration();
            OnPropertyChanged("Accounts");
        }

        private bool showWelcomeOnStartup = true;
        public bool ShowWelcomeOnStartup
        {
            get
            {
                return showWelcomeOnStartup;
            }
            set
            {
                if (showWelcomeOnStartup != value)
                {
                    showWelcomeOnStartup = value;
                    SaveConfiguration();
                }
            }
        }

    }
}