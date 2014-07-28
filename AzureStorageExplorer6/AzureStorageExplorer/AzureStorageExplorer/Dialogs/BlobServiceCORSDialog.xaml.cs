using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for BlobServiceCORSDialog.xaml
    /// </summary>
    public partial class BlobServiceCORSDialog : Window
    {
        private int EditMode = 0;       // 1=New Rule, 2=Edit Rule
        private int EditIndex = 0;      // RulesList item index for Edit Rule

        private ObservableCollection<CORSRule> _rules = new ObservableCollection<CORSRule>();
        public ObservableCollection<CORSRule> Rules { get { return _rules; } }

        public void SetRules(ObservableCollection<CORSRule> rules)
        {
            _rules = rules;

            int ruleNo = 1;
            foreach(CORSRule rule in _rules)
            {
                RulesList.Items.Add("Rule " + ruleNo.ToString() + ": " + rule.AllowedOrigins);
                ruleNo++;
            }
        }

        public BlobServiceCORSDialog()
        {
            InitializeComponent();
            CenterWindowOnScreen();

            //RulesDataGrid.ItemsSource = null;
            //_rules.Add(new CORSRule()
            //    {
            //        AllowedOrigins = "http://test.local",
            //        AllowedMethods = "PUT",
            //        AllowedHeaders = "x-ms-*,content-type,accept",
            //        ExposedHeaders = "x-ms-*",
            //        MaxAgeInSeconds = (60 * 60).ToString() // for an hour
            //    });
            //RulesDataGrid.ItemsSource = Rules;
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

        // Apply dialog - save updates rules.

        private void CmdApply_Click(object sender, RoutedEventArgs e)
        {
            List<CORSRule> ruleList = new List<CORSRule>();

            var r = _rules;
            var R = Rules;

            int count = 0;
            foreach (CORSRule rule in _rules)
            {
                count++;

                if (rule.AllowedOrigins == null) rule.AllowedOrigins = String.Empty;
                if (rule.AllowedMethods == null) rule.AllowedMethods = String.Empty;
                if (rule.AllowedHeaders == null) rule.AllowedHeaders = String.Empty;
                if (rule.ExposedHeaders == null) rule.ExposedHeaders = String.Empty;
                if (rule.MaxAgeInSeconds == null) rule.MaxAgeInSeconds = String.Empty;

                rule.AllowedOrigins = rule.AllowedOrigins.Trim();
                rule.AllowedMethods = rule.AllowedMethods.Trim();
                rule.AllowedHeaders = rule.AllowedHeaders.Trim();
                rule.ExposedHeaders = rule.ExposedHeaders.Trim();
                rule.MaxAgeInSeconds = rule.MaxAgeInSeconds.Trim();

                if (String.IsNullOrEmpty(rule.AllowedHeaders) && String.IsNullOrEmpty(rule.AllowedMethods) && String.IsNullOrEmpty(rule.AllowedOrigins) &&
                    String.IsNullOrEmpty(rule.ExposedHeaders) && String.IsNullOrEmpty(rule.MaxAgeInSeconds))
                {
                    // Ignore rule if all parts are blank
                }
                else
                {
                    // Validate CORS rule.

                    // Validate Allowed Origins.

                    String[] origins = rule.AllowedOrigins.Replace(" ", String.Empty).Split(',');
                    if (origins.Length < 1 || (origins.Length==1 && origins[0].Length==0))
                    {
                        MessageBox.Show("Error: Rule " + count.ToString() + " contains no Allowed Origins. A rule requires at least one Allowed Origin.", "CORS Rule Error");
                        return;
                    }
                    foreach(String origin in origins)
                    {
                        try
                        {
                            Uri uri = new Uri(origin);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("Error: Rule " + count.ToString() + " contains an invalid Allowed Origin '" + origin + "'. An Allowed Origin must be a value URI.", "CORS Rule Error");
                            return;
                        }
                    }

                    // Validate Allowed Methods.

                    String[] methods = rule.AllowedMethods.Replace(" ", String.Empty).ToUpper().Split(',');
                    if (methods.Length < 1)
                    {
                        MessageBox.Show("Error: Rule " + count.ToString() + " contains no Allowed Methods. A rule requires at least one Allowed Method.", "CORS Rule Error");
                        return;
                    }

                    foreach(String method in methods)
                    {
                        switch(method)
                        {
                            case "CONNECT":
                            case "DELETE":
                            case "GET":
                            case "MERGE":
                            case "NONE":
                            case "OPTIONS":
                            case "HEAD":
                            case "POST":
                            case "PUT":
                            case "TRACE":
                                break;
                            default:
                                MessageBox.Show("Error: Rule " + count.ToString() + " contains an unrecognized Allowed Method '" + method + "'. Allowable method names are GET, PUT, POST, and DELETE.", "CORS Rule Error");
                                return;
                        }
                    }

                    // TODO: Validate Allowed Headers.

                    // TODO: Validate Exposed Headers.

                    // TODO: Validate Max Age in Seconds.

                    int maxAge = 0;
                    if (!Int32.TryParse(rule.MaxAgeInSeconds, out maxAge) || maxAge < 1)
                    {
                        MessageBox.Show("Error: Rule " + count.ToString() + " contains an invalid value for Max Age in Seconds. A positive integer value is required.", "CORS Rule Error");
                        return;
                    }
                }
            }


            DialogResult = true;
        }

        // Cancel dialog.

        private void CmdCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        // Add a new rule.

        private void CmdNewRule_Click(object sender, RoutedEventArgs e)
        {
            RuleButtonPanel.Visibility = Visibility.Collapsed;

            EditMode = 1;
            AllowedOrigins.Text = String.Empty;
            AllowedMethods.Text = String.Empty;
            AllowedHeaders.Text = String.Empty;
            ExposedHeaders.Text = String.Empty;
            MaxAge.Text = String.Empty;

            EditRuleLabel.Visibility = Visibility.Visible;
            EditRuleLabel2.Visibility = Visibility.Visible;
            EditRulePanel.Visibility = Visibility.Visible;
            DialogButtonPanel.Visibility = Visibility.Collapsed;

            AllowedOrigins.Focus();
        }


        // Edit an existing rule.

        private void CmdEditRule_Click(object sender, RoutedEventArgs e)
        {
            if (RulesList.SelectedIndex == -1) return;

            RuleButtonPanel.Visibility = Visibility.Collapsed;
            
            EditMode = 2;
            EditIndex = RulesList.SelectedIndex;

            CORSRule rule = _rules[RulesList.SelectedIndex];

            AllowedOrigins.Text = rule.AllowedOrigins;
            AllowedMethods.Text = rule.AllowedMethods;
            AllowedHeaders.Text = rule.AllowedHeaders;
            ExposedHeaders.Text = rule.ExposedHeaders;
            MaxAge.Text = rule.MaxAgeInSeconds;

            EditRuleLabel.Visibility = Visibility.Visible;
            EditRuleLabel2.Visibility = Visibility.Visible;
            EditRulePanel.Visibility = Visibility.Visible;
            DialogButtonPanel.Visibility = Visibility.Collapsed;

            AllowedOrigins.Focus();
        }


        // Delete selected rule.

        private void CmdDeleteRule_Click(object sender, RoutedEventArgs e)
        {
            if (RulesList.SelectedIndex == -1) return;

            _rules.RemoveAt(RulesList.SelectedIndex);

            RulesList.Items.Clear();
            int ruleNo = 1;
            foreach (CORSRule rule in _rules)
            {
                RulesList.Items.Add("Rule " + ruleNo.ToString() + ": " + rule.AllowedOrigins);
                ruleNo++;
            }
        }

        // Save ruled edit.

        private void CmdSaveEdit_Click(object sender, RoutedEventArgs e)
        {
            CORSRule rule = null;

            if (EditMode == 1)   // New Rule
            {
                rule = new CORSRule()
                {
                    AllowedOrigins = AllowedOrigins.Text,
                    AllowedMethods = AllowedMethods.Text,
                    AllowedHeaders = AllowedHeaders.Text,
                    ExposedHeaders = ExposedHeaders.Text,
                    MaxAgeInSeconds = MaxAge.Text
                };
                _rules.Add(rule);
                RulesList.Items.Add("Rule " + _rules.Count.ToString() + ": " + rule.AllowedOrigins);
            }
            else if (EditMode == 2)   // Edit
            {
                rule = new CORSRule()
                {
                    AllowedOrigins = AllowedOrigins.Text,
                    AllowedMethods = AllowedMethods.Text,
                    AllowedHeaders = AllowedHeaders.Text,
                    ExposedHeaders = ExposedHeaders.Text,
                    MaxAgeInSeconds = MaxAge.Text
                };
                _rules[EditIndex] = rule;
                RulesList.Items[EditIndex] = "Rule " + _rules.Count.ToString() + ": " + rule.AllowedOrigins;
            }

            EditRuleLabel.Visibility = Visibility.Collapsed;
            EditRuleLabel2.Visibility = Visibility.Collapsed;
            EditRulePanel.Visibility = Visibility.Collapsed;
            RuleButtonPanel.Visibility = Visibility.Visible;
            DialogButtonPanel.Visibility = Visibility.Visible;
        }

        // Cancel editing.

        private void CmdCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditMode = 0;

            EditRuleLabel.Visibility = Visibility.Collapsed;
            EditRulePanel.Visibility = Visibility.Collapsed;
            RuleButtonPanel.Visibility = Visibility.Visible;
            DialogButtonPanel.Visibility = Visibility.Visible;
        }
    }
}
