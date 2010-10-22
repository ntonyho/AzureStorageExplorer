using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Neudesic.AzureStorageExplorer;
using Neudesic.AzureStorageExplorer.Data;
using Neudesic.AzureStorageExplorer.ViewModel;
using Microsoft.WindowsAzure.StorageClient;

namespace Neudesic.AzureStorageExplorer.ViewModel
{
    /// <summary>
    /// Represents an actionable item displayed by a View.
    /// </summary>
    public class BlobViewModel : ViewModelBase
    {
        private byte[] Bytes = null;
        private string Text = string.Empty;

        public Visibility TextSpinnerVisibility { get; set; }
        public Visibility PreviewTextVisibility { get; set; }

        public Visibility ImageSpinnerVisibility { get; set; }
        public Visibility PreviewImageVisibility { get; set; }

        private BlobDescriptor _blob;
        public BlobDescriptor Blob
        {
            get
            {
                return _blob;
            }
            set
            {
                _blob = value;
            }
        }

        public string Title
        {
            get
            {
                if (Blob != null)
                {
                    return "Blob Detail - " + Blob.CloudBlob.Uri.LocalPath;
                }
                else
                {
                    return "Blob Detail";
                }
            }
        }

        private bool imageVisible = false;
        public bool ImageVisible 
        { 
            get
            {
                return imageVisible;
            }
            set
            {
                if (value && !imageVisible)
                {
                    Bytes = null;
                    BackgroundWorker background = new BackgroundWorker();
                    background.DoWork += new DoWorkEventHandler(Background_LoadImage);
                    background.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Background_LoadImageCompleted);
                    background.RunWorkerAsync();
                }

                imageVisible = value;
            }
        }

        private bool videoVisible;
        public bool VideoVisible
        {
            get
            {
                return videoVisible;
            }
            set
            {
                videoVisible = value;

                if (value)
                {
                    OnPropertyChanged("PreviewVideo");
                }
            }
        }

        void Background_LoadText(object sender, DoWorkEventArgs e)
        {
            try
            {
                Text = Blob.CloudBlob.DownloadText();
            }
            catch (Exception ex)
            {
                Text = null;
            }
        }

        void Background_LoadTextCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TextSpinnerVisibility = Visibility.Collapsed;
            PreviewTextVisibility = Visibility.Visible;
            OnPropertyChanged("TextSpinnerVisibility");
            OnPropertyChanged("PreviewTextVisibility");
            OnPropertyChanged("PreviewText");
        }


        void Background_LoadImage(object sender, DoWorkEventArgs e)
        {
            try
            {
                Bytes = Blob.CloudBlob.DownloadByteArray();
            }
            catch (Exception ex)
            {
                Bytes = null;
            }
        }

        void Background_LoadImageCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ImageSpinnerVisibility = Visibility.Collapsed;
            PreviewImageVisibility = Visibility.Visible;
            OnPropertyChanged("ImageSpinnerVisibility");
            OnPropertyChanged("PreviewImageVisibility");
            OnPropertyChanged("PreviewImage");
        }

        public Byte[] PreviewImage
        {
            get
            {
                if (ImageVisible)
                {
                    return Bytes;
                }
                else
                {
                    return null;
                }
            }
        }

        public string PreviewVideo
        {
            get
            {
                if (VideoVisible)
                {
                    return Blob.CloudBlob.Uri.AbsoluteUri;
                }
                else
                {
                    return null;
                }
            }
        }

        private bool textVisible = false;
        public bool TextVisible
        {
            get
            {
                return textVisible;
            }
            set
            {
                if (value && !textVisible)
                {
                    Text = String.Empty;
                    BackgroundWorker background = new BackgroundWorker();
                    background.DoWork += new DoWorkEventHandler(Background_LoadText);
                    background.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Background_LoadTextCompleted);
                    background.RunWorkerAsync();
                }

                textVisible = value;
            }
        }

        public string PreviewText
        {
            get
            {
                return Text;
            }
        }

        private List<Property> properties = null;
        public ObservableCollection<Property> Properties
        {
            get
            {
                CloudBlob blob = Blob.CloudBlob;
                bool editable = false;
                if (properties == null)
                {
                    properties = new List<Property>();
                    properties.Add(new Property("(Name)", blob.Uri.LocalPath));
                    if (!editable) properties.Add(new Property("AbsoluteUri", blob.Attributes.Uri.AbsoluteUri));
                    if (!editable) properties.Add(new Property("Blob Type", blob.Properties.BlobType.ToString()));
                    properties.Add(new Property("CacheControl", blob.Properties.CacheControl));
                    properties.Add(new Property("ContentEncoding", blob.Properties.ContentEncoding));
                    properties.Add(new Property("ContentLanguage", blob.Properties.ContentLanguage));
                    properties.Add(new Property("ContentMD5", blob.Properties.ContentMD5));
                    properties.Add(new Property("ContentType", blob.Properties.ContentType));
                    if (!editable) properties.Add(new Property("ETag", blob.Properties.ETag));
                    if (!editable) properties.Add(new Property("LastModifiedUtc", blob.Properties.LastModifiedUtc.ToString()));
                    if (!editable) properties.Add(new Property("Length", blob.Properties.Length.ToString()));
                }
                return new ObservableCollection<Property>(properties);
            }
            set
            {
                properties = new List<Property>(value);
            }
        }

        public BlobViewModel(BlobDescriptor blob)
        {
            //if (command == null)
            //    throw new ArgumentNullException("command");

            TextSpinnerVisibility = Visibility.Visible;
            PreviewTextVisibility = Visibility.Collapsed;

            ImageSpinnerVisibility = Visibility.Visible;
            PreviewImageVisibility = Visibility.Collapsed;

            Blob = blob;

            base.DisplayName = blob.CloudBlob.Uri.LocalPath;
            // this.Command = command;
        }

        //public ICommand Command { get; private set; }


        public override string ToString()
        {
            return Blob.CloudBlob.Uri.LocalPath;
        }

        #region Blob Actions

        public void SaveText(string text)
        {
            Blob.CloudBlob.UploadText(text);
            Text = text;
            OnPropertyChanged("PreviewText");
        }

        public void SaveTextFile(string filename)
        {
            SaveText(File.ReadAllText(filename));
        }

        public void SaveImageFile(string filename)
        {
            Blob.CloudBlob.UploadFile(filename);
            OnPropertyChanged("PreviewImage");
        }

        public void SaveVideoFile(string filename)
        {
            Blob.CloudBlob.UploadFile(filename);
            OnPropertyChanged("PreviewImage");
        }

        public void SaveProperties()
        {
            CloudBlob blob = Blob.CloudBlob;
            foreach (Property prop in Properties)
            {
                switch (prop.PropertyName)
                {
                    case "(Name)":
                        if (prop.PropertyValue != BlobDescriptor.BlobName(blob))
                        {
                            CloudBlobContainer container = blob.Container;
                            if (container != null)
                            {
                                CloudBlob newBlob = container.GetBlobReference(prop.PropertyValue);
                                newBlob.UploadText(String.Empty);
                                newBlob.CopyFromBlob(blob);
                                blob.Delete();
                                blob = newBlob;
                            }
                        }
                        break;
                    case "Blob Type":
                        // Can't be set - blob.Properties.BlobType = prop.PropertyValue;
                        break;
                    case "CacheControl":
                        blob.Properties.CacheControl = prop.PropertyValue;
                        break;
                    case "ContentEncoding":
                        blob.Properties.ContentEncoding = prop.PropertyValue;
                        break;
                    case "ContentLanguage":
                        blob.Properties.ContentLanguage = prop.PropertyValue;
                        break;
                    case "ContentMD5":
                        blob.Properties.ContentMD5 = prop.PropertyValue;
                        break;
                    case "ContentType":
                        blob.Properties.ContentType = prop.PropertyValue;
                        break;
                    case "ETag":
                        //Can't be set - blob.Properties.ETag = prop.PropertyValue;
                        break;
                    case "LastModifiedUtc":
                        //Can't be set
                        break;
                    case "Length":
                        break;
                }
            }
            blob.SetProperties();
        }

        #endregion

    }
}