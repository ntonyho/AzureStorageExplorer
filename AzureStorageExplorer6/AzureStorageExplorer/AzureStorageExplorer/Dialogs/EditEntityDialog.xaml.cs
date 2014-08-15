using System;
using System.Collections;
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
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorageExplorer
{
    /// <summary>
    /// Interaction logic for EditEntityDialog.xaml
    /// </summary>
    public partial class EditEntityDialog : Window
    {
        #region Variables

        public const String NULL_VALUE = "NULL ";
        
        private bool Initialized = false;

        private bool IsAddNew = false;
        private ElasticTableEntity Entity = null;
        private CloudTable Table = null;

        public int RecordsAdded = 0;
        public int RecordsUpdated = 0;

        int nextFieldId = 1;
        Dictionary<int, ListViewItem> fieldItems = new Dictionary<int, ListViewItem>();
        Dictionary<int, CheckBox> fieldCheckboxes = new Dictionary<int, CheckBox>();
        Dictionary<int, StackPanel> fieldRows = new Dictionary<int, StackPanel>();
        Dictionary<int, TextBox> fieldNames = new Dictionary<int, TextBox>();
        Dictionary<int, ComboBox> fieldTypes = new Dictionary<int, ComboBox>();
        Dictionary<int, TextBox> fieldValues = new Dictionary<int, TextBox>();

        #endregion

        #region Initialization

        public EditEntityDialog()
        {
            InitializeComponent();
            Initialized = true;
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


        //*******************
        //*                 *
        //*  InitForInsert  *
        //*                 *
        //*******************
        // Initialize dialog for inserting new records.
        
        public void InitForInsert(CloudTable table, Dictionary<String, bool> tableColumnNames)
        {
            this.IsAddNew = true;
            this.Table = table;
            this.Title = "Insert New Entity";
            this.CmdInsertUpdateEntity.Content = new TextBlock() { Text = "Insert Entity" };
            this.Heading.Text = "Enter fields and values for a new entity.";

            if (tableColumnNames != null)
            {
                foreach (KeyValuePair<String, bool> col in tableColumnNames)
                {
                    switch(col.Key)
                    {
                        case "PartitionKey":
                        case "RowKey":
                        case "Timestamp":
                            break;
                        default:
                            AddFieldRow(col.Key, "String", String.Empty);
                            break;
                    }
                }
            }
        }


        //*******************
        //*                 *
        //*  InitForUpdate  *
        //*                 *
        //*******************
        // Initialize dialog for updating an existing record.

        public void InitForUpdate(CloudTable table, Dictionary<String, bool> tableColumnNames, ElasticTableEntity entity)
        {
            String fieldType = null;

            this.Entity = entity;
            this.IsAddNew = false;
            this.Table = table;
            this.Title = "Update Entity";
            this.CmdInsertUpdateEntity.Content = new TextBlock() { Text = "Update Entity" };
            this.Heading.Text = "Edit entity fields and values.";

            if (tableColumnNames != null)
            {
                foreach (KeyValuePair<String, bool> col in tableColumnNames)
                {
                    switch (col.Key)
                    {
                        case "PartitionKey":
                            PartitionKey.Text = this.Entity.PartitionKey;
                            break;
                        case "RowKey":
                            RowKey.Text = this.Entity.RowKey;
                            break;
                        case "Timestamp":
                            break;
                        default:
                            {
                                String value = NULL_VALUE;
                                if (this.Entity.Properties.ContainsKey(col.Key))
                                {
                                    fieldType = this.Entity.Properties[col.Key].PropertyType.ToString();
                                    switch(fieldType)
                                    {
                                        case "Binary":
                                            value = BitConverter.ToString(this.Entity.Properties[col.Key].BinaryValue).Replace("-", " ");
                                            break;
                                        default:
                                            value = this.Entity.Properties[col.Key].PropertyAsObject.ToString();
                                            break;
                                    }
                                    if (value == null)
                                    {
                                        fieldType = "NULL";
                                    }
                                }
                                if (value == NULL_VALUE)
                                {
                                    value = String.Empty;
                                    fieldType = "Null";
                                }
                                AddFieldRow(col.Key, fieldType, value);
                            }
                            break;
                    }
                }
            }
        }

        //*****************
        //*               *
        //*  InitForCopy  *
        //*               *
        //*****************
        // Initialize dialog for inserting a copy of an existing record.

        public void InitForCopy(CloudTable table, Dictionary<String, bool> tableColumnNames, ElasticTableEntity entity)
        {
            String fieldType = null;
            
            this.Entity = entity;
            this.IsAddNew = true;
            this.Table = table;
            this.Title = "Copy Entity";
            this.CmdInsertUpdateEntity.Content = new TextBlock() { Text = "Insert Entity" };
            this.Heading.Text = "Enter fields and values for a new entity.";
            
            if (tableColumnNames != null)
            {
                foreach (KeyValuePair<String, bool> col in tableColumnNames)
                {
                    switch (col.Key)
                    {
                        case "PartitionKey":
                            PartitionKey.Text = this.Entity.PartitionKey;
                            break;
                        case "RowKey":
                            RowKey.Text = this.Entity.RowKey;
                            break;
                        case "Timestamp":
                            break;
                        default:
                            {
                                String value = NULL_VALUE;
                                if (this.Entity.Properties.ContainsKey(col.Key))
                                {
                                    fieldType = this.Entity.Properties[col.Key].PropertyType.ToString();
                                    switch (fieldType)
                                    {
                                        case "Binary":
                                            value = BitConverter.ToString(this.Entity.Properties[col.Key].BinaryValue).Replace("-", " ");
                                            break;
                                        default:
                                            value = this.Entity.Properties[col.Key].PropertyAsObject.ToString();
                                            break;
                                    }
                                    if (value == null)
                                    {
                                        fieldType = "NULL";
                                    }
                                }
                                if (value == NULL_VALUE)
                                {
                                    value = String.Empty;
                                    fieldType = "Null";
                                }
                                AddFieldRow(col.Key, fieldType, value);
                            }
                            break;
                    }
                }
            }
        }

        #endregion

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void AddField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AddFieldRow(String.Empty, "String", String.Empty);
        }


        //***************************
        //*                         *
        //*  RemoveField_MouseDown  *
        //*                         *
        //***************************
        // Remove selected fields.

        private void RemoveField_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Cursor = Cursors.Wait;

            try
            {
                List<int> fieldIds = new List<int>();
                foreach (KeyValuePair<int, CheckBox> selection in fieldCheckboxes)
                {
                    if (selection.Value.IsChecked.Value)
                    {
                        fieldIds.Add(selection.Key);
                    }
                }

                foreach(int fieldId in fieldIds)
                {
                    EntityTable.Items.Remove(fieldItems[fieldId]);

                    fieldItems.Remove(fieldId);
                    fieldRows.Remove(fieldId);
                    fieldCheckboxes.Remove(fieldId);
                    fieldNames.Remove(fieldId);
                    fieldTypes.Remove(fieldId);
                    fieldValues.Remove(fieldId);
                }

                this.Cursor = Cursors.Arrow;
            }
            catch(Exception ex)
            {
                this.Cursor = Cursors.Arrow;
                MessageBox.Show("An error occurred removing the selected fields from the dialog window:\n\n" + ex.Message, "Error");
            }
        }


        //*****************
        //*               *
        //*  AddFieldRow  *
        //*               *
        //*****************
        // Add a field row to the list box.

        private void AddFieldRow(String fieldName, String fieldType, String value)
        {
            int fieldId = nextFieldId++;
            
            StackPanel spOuter = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };

            CheckBox checkbox = new CheckBox()
                {
                    Margin = new Thickness(0, 4, 12, 0)
                };
            spOuter.Children.Add(checkbox);

            fieldCheckboxes.Add(fieldId, checkbox);
            
            TextBox fieldNameTextBox = new TextBox() { 
                        Name = "FieldName" + fieldId.ToString(), 
                        Text = fieldName, 
                        Background = new SolidColorBrush(Colors.LightYellow) 
                    };

            fieldNames.Add(fieldId, fieldNameTextBox);

            spOuter.Children.Add(new Border()
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    Child = fieldNameTextBox,
                    Width = 100.0,
                    Margin = new Thickness(0, 0, 12, 0)
                });

            ComboBox cb = new ComboBox()
            {
                Margin = new Thickness(0, 0, 12, 0),
                Width = 80.0
            };

            cb.Items.Add(new ComboBoxItem() { Content = "Binary", Tag = "Binary" });
            cb.Items.Add(new ComboBoxItem() { Content = "Boolean", Tag = "Boolean" });
            cb.Items.Add(new ComboBoxItem() { Content = "DateTime", Tag = "DateTime" });
            cb.Items.Add(new ComboBoxItem() { Content = "Double", Tag = "Double" });
            cb.Items.Add(new ComboBoxItem() { Content = "Guid", Tag = "Guid" });
            cb.Items.Add(new ComboBoxItem() { Content = "Int32", Tag = "Int32" });
            cb.Items.Add(new ComboBoxItem() { Content = "Int64", Tag = "Int64" });
            cb.Items.Add(new ComboBoxItem() { Content = "String", Tag = "String" /*, IsSelected = true */ });
            cb.Items.Add(new ComboBoxItem() { Content = "Null", Tag = "Null" });

            bool typeValidated = false;
            foreach(ComboBoxItem item in cb.Items)
            {
                if ((item.Content as String) == fieldType)
                {
                    typeValidated = true;
                    item.IsSelected = true;
                }
            }
            if (!typeValidated)
            {
                MessageBox.Show("AddFieldRow: Unrecognized field type '" + fieldType + "' (" + fieldName + ")");
            }

            fieldTypes.Add(fieldId, cb);

            spOuter.Children.Add(cb);

            TextBox fieldValueTextBox = new TextBox()
            {
                Name = "FieldValue" + fieldId.ToString(),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Width = 320.0,
                Margin = new Thickness(0, 0, 0, 4),
                Background = new SolidColorBrush(Colors.LightYellow),
                Text = value
            };

            fieldValues.Add(fieldId, fieldValueTextBox);

            spOuter.Children.Add(fieldValueTextBox);

            fieldRows.Add(fieldId, spOuter);

            ListViewItem lvi = new ListViewItem()
            {
                Content = spOuter
            };

            fieldItems.Add(fieldId, lvi);

            EntityTable.Items.Add(lvi);
        }

        // Disallow record grid row selections.

        private void EntityTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EntityTable.SelectedIndex = -1;
        }

        #region Insert / Update Record

        //******************************
        //*                            *
        //*  InsertUpdateEntity_Click  *
        //*                            *
        //******************************
        // Insert or update the entity in cloud storage.

        private void InsertUpdateEntity_Click(object sender, RoutedEventArgs e)
        {
            String action = "update entity";
            if (IsAddNew)
            {
                action = "insert entity";
            }

            // Construct entity

            ElasticTableEntity entity = new ElasticTableEntity();
            entity.RowKey = RowKey.Text;
            entity.PartitionKey = PartitionKey.Text;

            int fieldId;
            String fieldName, fieldType, fieldValue;

            foreach (KeyValuePair<int, TextBox> field in fieldNames)
            {
                fieldId = field.Key;

                TextBox nameTextBox = field.Value;
                ComboBox typeComboBox = fieldTypes[fieldId];
                TextBox valueTextBox = fieldValues[fieldId];

                fieldName = nameTextBox.Text;

                if (String.IsNullOrEmpty(fieldName))
                {
                    MessageBox.Show("Cannot " + action + ": '" + fieldName + "' is not a valid propert name", "Invalid Property Name");
                    return;
                }

                ComboBoxItem item = typeComboBox.SelectedItem as ComboBoxItem;
                fieldType = item.Content as String;

                fieldValue = valueTextBox.Text;

                switch (fieldType)
                {
                    case "Guid":
                        {
                            Guid guidValue;
                            if (Guid.TryParse(fieldValue, out guidValue))
                            {
                                entity[fieldName] = guidValue;
                            }
                            else
                            {
                                MessageBox.Show("Cannot update entity: " + fieldName + " does not contain a valid GUID value: " + fieldValue, "Invalid Value");
                                this.Cursor = Cursors.Arrow;
                                return;
                            }
                        }
                        break;
                    case "String":
                        entity[fieldName] = fieldValue;
                        break;
                    case "Binary":
                        {
                            try
                            { 
                                string hexValues = fieldValue;
                                string[] hexValuesSplit = hexValues.Split(' ');
                                byte[] bytes = new byte[hexValuesSplit.Length];
                                int offset = 0;
                                foreach (String hex in hexValuesSplit)
                                {
                                    bytes[offset++] = (byte)Convert.ToInt32(hex, 16);
                                }
                                entity[fieldName] = bytes;
                            }
                            catch(Exception ex)
                            {
                                MessageBox.Show("Cannot " + action + ": " + fieldName + " does not contain a valid hexadecimal bytes representation: " + fieldValue, "Invalid Value");
                                this.Cursor = Cursors.Arrow;
                                return;
                            }
                        }
                        break;
                    case "Boolean":
                        {
                            bool boolValue = false;

                            switch (fieldValue.ToLower())
                            {
                                case "1":
                                case "true":
                                case "yes":
                                case "on":
                                    fieldValue = "True";
                                    break;
                                case "0":
                                case "false":
                                case "no":
                                case "off":
                                    fieldValue = "False";
                                    break;
                            }

                            if (Boolean.TryParse(fieldValue, out boolValue))
                            {
                                entity[fieldName] = boolValue;
                            }
                            else
                            {
                                MessageBox.Show("Cannot " + action + ": " + fieldName + " does not contain a valid boolean value: " + fieldValue, "Invalid Value");
                                this.Cursor = Cursors.Arrow;
                                return;
                            }
                        }
                        break;
                    case "DateTime":
                        {
                            DateTime dateValue;
                            if (DateTime.TryParse(fieldValue, out dateValue))
                            {
                                entity[fieldName] = dateValue;
                            }
                            else
                            {
                                MessageBox.Show("Cannot update entity: " + fieldName + " does not contain a valid DateTime value: " + fieldValue, "Invalid Value");
                                this.Cursor = Cursors.Arrow;
                                return;
                            }
                        }
                        break;
                    case "Double":
                        {
                            double doubleValue = 0;
                            if (Double.TryParse(fieldValue, out doubleValue))
                            {
                                entity[fieldName] = doubleValue;
                            }
                            else
                            {
                                MessageBox.Show("Cannot " + action + ": " + fieldName + " does not contain a valid double-precision value: " + fieldValue, "Invalid Value");
                                this.Cursor = Cursors.Arrow;
                                return;
                            }
                        }
                        break;
                    case "Int32":
                        {
                            int intValue = 0;
                            if (Int32.TryParse(fieldValue, out intValue))
                            {
                                entity[fieldName] = intValue;
                            }
                            else
                            {
                                MessageBox.Show("Cannot " + action + ": " + fieldName + " does not contain a valid Int32 value: " + fieldValue, "Invalid Value");
                                this.Cursor = Cursors.Arrow;
                                return;
                            }
                        }
                        break;
                    case "Int64":
                        {
                            Int64 intValue = 0;
                            if (Int64.TryParse(fieldValue, out intValue))
                            {
                                entity[fieldName] = intValue;
                            }
                            else
                            {
                                MessageBox.Show("Cannot " + action + ": " + fieldName + " does not contain a valid Int64 value: " + fieldValue, "Invalid Value");
                                this.Cursor = Cursors.Arrow;
                                return;
                            }
                        }
                        break;
                    case "Null":
                        // Type "Null" means, do not add to entity.
                        break;
                    default:
                        MessageBox.Show("Cannot " + action + ": unknown type '" + fieldType + "'");
                        this.Cursor = Cursors.Arrow;
                        return;
                }
            } // next field

            try
            {
                if (IsAddNew)
                {
                    // Insert entity and keep dialog open.

                    this.Cursor = Cursors.Wait;

                    Table.Execute(TableOperation.Insert(entity));
                    RecordsAdded++;

                    Message.Text = "Records Added: " + RecordsAdded.ToString();

                    CmdClose.Content = new TextBlock() { Text = "Close" };

                    this.Cursor = Cursors.Arrow;

                    RowKey.Focus();
                }
                else
                {
                    // Update entity and close dialog.

                    this.Cursor = Cursors.Wait;

                    entity.ETag = "*";
                    //Table.Execute(TableOperation.Merge(entity));
                    Table.Execute(TableOperation.Replace(entity));
                    RecordsUpdated++;

                    Message.Text = "Records Updaed: " + RecordsUpdated.ToString();

                    CmdClose.Content = new TextBlock() { Text = "Close" };

                    this.Cursor = Cursors.Arrow;

                    //RowKey.Focus();
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Arrow;

                if (IsAddNew)
                {
                    Message.Text = "Error inserting record: " + ex.Message;
                }
                else
                {
                    Message.Text = "Error updating record: " + ex.Message;
                }

                RowKey.Focus();
            }
        }

        #endregion

        #region Close Dialog

        //********************
        //*                  *
        //*  CmdClose_Click  *
        //*                  *
        //********************
        // Close the dialog.

        private void CmdClose_Click(object sender, RoutedEventArgs e)
        {
            if (RecordsAdded > 0 || RecordsUpdated > 0)
            {
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }
        }

        #endregion


    }
}
