// CORSRule - bindable edition of an Azure CORS Rule.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Analytics;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.ComponentModel;

namespace AzureStorageExplorer
{
    public class CORSRule : INotifyPropertyChanged, IEditableObject
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region IEditableObject Members

        private CORSRule copy;

        public void BeginEdit()
        {
            if (this.copy == null)
                this.copy = new CORSRule();
 
            copy.AllowedOrigins = this.AllowedOrigins;
            copy.AllowedMethods = this.AllowedMethods;
            copy.AllowedHeaders = this.AllowedHeaders;
            copy.ExposedHeaders = this.ExposedHeaders;
            copy.MaxAgeInSeconds = this.MaxAgeInSeconds;
        }
 
        public void CancelEdit()
        {
            this.AllowedOrigins = copy.AllowedOrigins;
            this.AllowedMethods = copy.AllowedMethods;
            this.AllowedHeaders = copy.AllowedHeaders;
            this.ExposedHeaders = copy.ExposedHeaders;
            this.MaxAgeInSeconds = copy.MaxAgeInSeconds;
        }
 
      public void EndEdit()
      {
            copy.AllowedOrigins = null;
            copy.AllowedMethods = null;
            copy.AllowedHeaders = null;
            copy.ExposedHeaders = null;
            copy.MaxAgeInSeconds = null;
      }

        #endregion IEditableObject Members

      // Create the OnPropertyChanged method to raise the event 
      protected void OnPropertyChanged(string name)
      {
          PropertyChangedEventHandler handler = PropertyChanged;
          if (handler != null)
          {
              handler(this, new PropertyChangedEventArgs(name));
          }
      }

        internal String _allowedOrigins;
        public String AllowedOrigins
        {
            get
            {
                return _allowedOrigins;
            }
            set
            {
                _allowedOrigins = value;
                OnPropertyChanged("AllowedOrigins");
            }
        }

        internal String _allowedMethods;
        public String AllowedMethods
        {
            get
            {
                return _allowedMethods;
            }
            set
            {
                _allowedMethods = value;
                OnPropertyChanged("AllowedMethods");
            }
        }

        internal String _allowedHeaders;
        public String AllowedHeaders
        {
            get
            {
                return _allowedHeaders;
            }
            set
            {
                _allowedHeaders = value;
                OnPropertyChanged("AllowedHeaders");
            }
        }

        internal String _exposedHeaders;
        public String ExposedHeaders
        {
            get
            {
                return _exposedHeaders;
            }
            set
            {
                _exposedHeaders = value;
                OnPropertyChanged("ExposedHeaders");
            }
        }

        internal String _maxAgeInSeconds;
        public String MaxAgeInSeconds
        {
            get
            {
                return _maxAgeInSeconds;
            }
            set
            {
                _maxAgeInSeconds = value;
                OnPropertyChanged("MaxAgeInSeconds");
            }
        }


        //public String AllowedOrigins { get; set; }
        //public String AllowedMethods { get; set; }
        //public String AllowedHeaders { get; set; }
        //public String ExposedHeaders { get; set; }
        //public String MaxAgeInSeconds { get; set; }

        public CORSRule() { }

        // Constructor - create from an Azure CorsRule object.

        public CORSRule(CorsRule rule)
        {
            // Retrieve allowed origins.

            if (rule.AllowedOrigins != null)
            {
                String origins = String.Empty;
                foreach (String origin in rule.AllowedOrigins)
                {
                    if (origins.Length > 0)
                    {
                        origins = origins + ",";
                    }
                    origins = origins + origin;
                }
                this.AllowedOrigins = origins;
            }

            // Retrieve allowed headers.

            if (rule.AllowedHeaders != null)
            {
                String headers = String.Empty;
                foreach (String header in rule.AllowedHeaders)
                {
                    if (headers.Length > 0)
                    {
                        headers = headers + ",";
                    }
                    headers = headers + header;
                }
                this.AllowedHeaders = headers;
            }

            // Retrieve exposed headers.

            if (rule.ExposedHeaders != null)
            {
                String headers = String.Empty;
                foreach (String header in rule.ExposedHeaders)
                {
                    if (headers.Length > 0)
                    {
                        headers = headers + ",";
                    }
                    headers = headers + header;
                }
                this.ExposedHeaders = headers;
            }

            // Retrieve allowed methods.

            String methods = String.Empty;

            if ((rule.AllowedMethods & CorsHttpMethods.Connect)==CorsHttpMethods.Connect) { methods = methods + "CONNECT,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.Delete) == CorsHttpMethods.Delete) { methods = methods + "DELETE,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.Get) == CorsHttpMethods.Get) { methods = methods + "GET,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.Head) == CorsHttpMethods.Head) { methods = methods + "HEAD,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.Merge) == CorsHttpMethods.Merge) { methods = methods + "MERGE,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.None) == CorsHttpMethods.None) { methods = methods + "NONE,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.Options) == CorsHttpMethods.Options) { methods = methods + "OPTIONS,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.Post) == CorsHttpMethods.Post) { methods = methods + "POST,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.Put) == CorsHttpMethods.Put) { methods = methods + "PUT,"; };
            if ((rule.AllowedMethods & CorsHttpMethods.Trace) == CorsHttpMethods.Trace) { methods = methods + "TRACE,"; };

            if (methods.Length > 0 && methods.EndsWith(","))
            {
                methods = methods.Substring(0, methods.Length - 1);
            }

            this.AllowedMethods = methods;

            this.MaxAgeInSeconds = rule.MaxAgeInSeconds.ToString();
        }

        // Return the Azure CORSRule data for this rule.

        public CorsRule ToCorsRule()
        {
            CorsRule rule = new CorsRule();

            rule.AllowedOrigins = this.AllowedOrigins.Replace(" ", String.Empty).Split(',');
            rule.AllowedHeaders = this.AllowedHeaders.Replace(" ", String.Empty).Split(',');
            rule.ExposedHeaders = this.ExposedHeaders.Replace(" ", String.Empty).Split(',');

            foreach(String method in this.AllowedMethods.Replace(" ", String.Empty).Split(','))
            {
                switch(method.Trim().ToUpper())
                {
                    case "CONNECT":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Connect;
                        break;
                    case "DELETE":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Delete;
                        break;
                    case "GET":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Get;
                        break;
                    case "HEAD":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Head;
                        break;
                    case "MERGE":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Merge;
                        break;
                    case "OPTIONS":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Options;
                        break;
                    case "POST":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Post;
                        break;
                    case "PUT":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Put;
                        break;
                    case "TRACE":
                        rule.AllowedMethods = rule.AllowedMethods | CorsHttpMethods.Trace;
                        break;
                }
            }

            int age = 0;
            Int32.TryParse(this.MaxAgeInSeconds, out age);
            rule.MaxAgeInSeconds = age;

            return rule;

        }
    }

}
