﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Resources;
using System.Xml;
using System.Xml.Linq;

namespace Neudesic.AzureStorageExplorer.Data
{
    /// // This clas represents an Azure storage account.

    public class StorageAccount
    {
        #region Fields

        private string name;
        private string key;

        #endregion // Fields

        #region Constructor

        public StorageAccount(string name, string key)
        {
            Name = name;
            Key = key;
        }

        #endregion // Constructor

        #region Public Interface

        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                key = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        #endregion

        #region Private Helpers

        static Stream GetResourceStream(string resourceFile)
        {
            Uri uri = new Uri(resourceFile, UriKind.RelativeOrAbsolute);

            StreamResourceInfo info = Application.GetResourceStream(uri);
            if (info == null || info.Stream == null)
                throw new ApplicationException("Missing resource file: " + resourceFile);

            return info.Stream;
        }

        #endregion 
    }
}