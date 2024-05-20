using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ComputerShop.CustomControl;
using Microsoft.VisualBasic;

namespace ComputerShop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private String _connectionString;
        private OleDbConnection _oleDbConnection;
        private OleDbDataAdapter _dataAdapter;
        private DataTable _dataTable;

        // Orders
        private ComboBox _productComboBox;
        private ComboBox _clientComboBox;
        private ComboBox _employeeComboBox;
        private ComboBox _deliveryComboBox;
        private ComboBox _offerComboBox;
        
        // Products
        private ComboBox _categoryComboBox;
        private ComboBox _specificationComboBox;
        private ComboBox _manufacterComboBox;
        
        // Deliveries
        private ComboBox _supplierComboBox;
        private ComboBox _storageComboBox;
        
        // Other
        private ComboBox _countryComboBox;
        private ComboBox _cityComboBox;

        private Dictionary<string, TableData> _tables = new Dictionary<string, TableData>();
        private Dictionary<string, string> _keysToTables;
        
        public MainWindow()
        {
            InitializeComponent();

            _keysToTables = new Dictionary<string, string>()
            {
                {"ID Товара", "Товары"},
                {"ID Клиента", "Клиенты"},
                {"ID Сотрудника", "Сотрудники"},
                {"ID Поставки", "Поставки"},
                {"ID Акции", "Акции"},
                {"ID Категории", "Категории товаров"},
                {"ID Спецификации", "Спецификации"},
                {"ID Производителя", "Производители"},
                {"ID Страны", "Страны"},
                {"ID Поставщика", "Поставщики"},
                {"ID Склада", "Склады"},
                {"ID Города", "Города"},
            };
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _connectionString = Environment.Is64BitOperatingSystem ? "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=ComputerShop.mdb" : "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=ComputerShop.mdb";
            
            try
            {
                _oleDbConnection = new OleDbConnection(_connectionString);
                _oleDbConnection.Open();
            }
            catch (Exception exception)
            {
                MessageBox.Show("Не удалось подключиться к базе данных!");
            }
            
            InitializeTablesInfo();
            OnTableButtonClicked(ButtonsPanel.Children[0]); // активация кнопки
            OnSelectClicked(null, null);
        }
        
        private void InitializeTablesInfo()
        {
            DataTable schemaTables = _oleDbConnection.GetSchema("Tables");
            List<string> tableNames = new List<string>();

            foreach (DataRow row in schemaTables.Rows)
            {
                string tableType = row["TABLE_TYPE"].ToString();
                
                if (tableType == "TABLE") // нужны только простые таблицы
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    tableNames.Add(tableName);
                }
            }

            foreach (var tableName in tableNames)
            {
                DataTable schemaColumns = _oleDbConnection.GetSchema("Columns", new[] {null, null, tableName, null});
                var orderedColumns = schemaColumns.AsEnumerable()
                    .OrderBy(row => row.Field<long>("ORDINAL_POSITION"))
                    .Select(row => row["COLUMN_NAME"].ToString());

                _tables.Add(tableName, new TableData()
                {
                    Fields = orderedColumns.ToList()
                });
                
                UpdateIDs(tableName);
            }
        }

        private void UpdateQueryField(string tableName)
        {
            foreach (UIElement child in CheckboxPanel.Children)
            {
                if (child is CheckboxGroup checkboxGroup && _keysToTables[checkboxGroup.LabelID] == tableName)
                    checkboxGroup.UpdateData(_tables[tableName]);
            }
        }
        
        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
            if (_oleDbConnection != null && _oleDbConnection.State != ConnectionState.Closed)
                _oleDbConnection.Close();
        }

        private List<string> GetForeignValues(string columnName)
        {
            string tableName = _keysToTables[columnName];
            return _tables[tableName].Values;
        }

        private string GetForeignIndex(string columnName, int comboBoxIndex)
        {
            if (comboBoxIndex == -1) // пустое значение
                return "NULL";
            
            string tableName = _keysToTables[columnName];
            return _tables[tableName].Ids[comboBoxIndex];
        }
        
        private void InitializeTableInfo(string tableName)
        {
            AddStackPanel.Children.Clear();
            EditStackPanel.Children.Clear();

            for (int i = 0; i < _tables[tableName].Fields.Count; i++)
            {
                if (i == 0) // нельзя поменять или установить первичный ключ
                    continue;
                
                var column = _tables[tableName].Fields[i];
                if (column.Contains("ID"))
                {
                    AddStackPanel.Children.Add(new LabeledComboBox(column, GetForeignValues(column)));
                    EditStackPanel.Children.Add(new LabeledComboBox(column, GetForeignValues(column)));
                }
                else
                {
                    AddStackPanel.Children.Add(new LabeledField(column));
                    EditStackPanel.Children.Add(new LabeledField(column));
                }
            }
        }

        private void UpdateIDs(string tableName, bool needUpdate = true)
        {
            if (!needUpdate && _tables[tableName].Values.Count > 0) // данные уже загружены
                return;
            
            string query;

            switch (tableName)
            {
                case "Товары":
                    query = @"SELECT Товары.[ID Товара], Товары.[Название]
                                FROM Товары";
                    break;
                case "Клиенты":
                    query = @"SELECT Клиенты.[ID Клиента], Клиенты.[Имя], Клиенты.[E-mail]
                                FROM Клиенты";
                    break;
                case "Сотрудники":
                    query = @"SELECT Сотрудники.[ID Сотрудника], Сотрудники.[Фамилия], Сотрудники.[Имя], Сотрудники.[Отчество]
                                FROM Сотрудники";
                    break;
                case "Поставки":
                    query = @"SELECT Поставки.[ID Поставки], Склады.[Название], Города.[Город], Поставщики.[Название], Поставки.[Дата поставки]
                                FROM ((Поставки
                                INNER JOIN Склады ON Поставки.[ID Склада] = Склады.[ID Склада])
                                INNER JOIN Города ON Склады.[ID Города] = Города.[ID Города])
                                INNER JOIN Поставщики ON Поставки.[ID Поставщика] = Поставщики.[ID Поставщика]";
                    break;
                case "Поставщики":
                    query = @"SELECT Поставщики.[ID Поставщика], Поставщики.[Название]
                                FROM Поставщики";
                    break;
                case "Склады":
                    query = @"SELECT Склады.[ID Склада], Склады.[Название], Города.[Город]
                                FROM Склады
                                INNER JOIN Города ON Склады.[ID Города] = Города.[ID Города]";
                    break;
                case "Города":
                    query = @"SELECT Города.[ID Города], Города.[Город]
                                FROM Города";
                    break;
                case "Акции":
                    query = @"SELECT Акции.[ID Акции], Акции.[Название], 'До ' & Акции.[Дата конца]
                                FROM Акции";
                    break;
                case "Категории товаров":
                    query = @"SELECT [Категории товаров].[ID Категории], [Категории товаров].[Название]
                                FROM [Категории товаров]";
                    break;
                case "Спецификации":
                    query = @"SELECT Спецификации.[ID Спецификации], Спецификации.[Общее]
                                FROM Спецификации";
                    break;
                case "Производители":
                    query = @"SELECT Производители.[ID Производителя], Производители.[Название]
                                FROM Производители";
                    break;
                case "Страны":
                    query = @"SELECT Страны.[ID Страны], Страны.[Страна]
                                FROM Страны";
                    break;
                default:
                    return;
            }

            _tables[tableName].Values.Clear();
            _tables[tableName].Ids.Clear();
            
            OleDbCommand command = new OleDbCommand(query, _oleDbConnection);
            OleDbDataReader reader = command.ExecuteReader();
            StringBuilder stringBuilder = new StringBuilder();
            
            while (reader.Read())
            {
                stringBuilder.Clear();
                
                for (int i = 1; i < reader.FieldCount; i++) // 0 - индекс
                    stringBuilder.Append(reader[i] + " ");
                
                _tables[tableName].Ids.Add(reader[0].ToString());
                _tables[tableName].Values.Add(stringBuilder.ToString());
            }
            
            UpdateQueryField(tableName);
        }

        private Button _previousButton;
        
        private void OnTableButtonClicked(object sender, RoutedEventArgs e = null)
        {
            if (sender is not Button button)
                return;

            if (_previousButton != null)
                _previousButton.Background = new SolidColorBrush(Colors.LightGray);

            TableTabControl.SelectedIndex = 0;
            SelectedTableLabel.Content = button.Content;
            button.Background = new SolidColorBrush(Colors.DarkGray);
            _previousButton = button;

            string query;
            string tableName = button.Content.ToString();
            
            InitializeTableInfo(tableName);

            switch (tableName)
            {
                case "Заказы":
                    query = @"
                    SELECT 
                        Заказы.[ID Заказа],
                        Товары.[Название] AS Товар,
                        Клиенты.[Имя] AS Клиент,
                        Сотрудники.[Фамилия] & ' ' & Сотрудники.[Имя] & ' ' & Сотрудники.[Отчество] AS Сотрудник,
                        Склады.[Название] & ' ' & Поставщики.[Название] & ' ' & Поставки.[Дата поставки] AS Поставщик,
                        Акции.[Название] AS Акция,
                        Количество,
                        [Дата покупки]
                    FROM ((((((Заказы
                    INNER JOIN Товары ON Заказы.[ID Товара] = Товары.[ID Товара])
                    INNER JOIN Клиенты ON Заказы.[ID Клиента] = Клиенты.[ID Клиента])
                    INNER JOIN Сотрудники ON Заказы.[ID Сотрудника] = Сотрудники.[ID Сотрудника])
                    LEFT JOIN Акции ON Заказы.[ID Акции] = Акции.[ID Акции])
                    INNER JOIN Поставки ON Заказы.[ID Поставки] = Поставки.[ID Поставки])
                    INNER JOIN Поставщики ON Поставки.[ID Поставщика] = Поставщики.[ID Поставщика])
                    INNER JOIN Склады ON Поставки.[ID Склада] = Склады.[ID Склада];";
                    break;
                case "Товары":
                    query = @"
                    SELECT 
                        Товары.[ID Товара],
                        Товары.[Название],
                        [Категории товаров].[Название] AS Категория,
                        Спецификации.[Общее] AS Общее,
                        'Ширина: ' & Спецификации.[Ширина] & ' Высота: ' & Спецификации.[Высота] & ' Вес: ' & Спецификации.[Вес] AS Габариты,
                        Спецификации.[Гарантия] AS [Гарантия],
                        Производители.[Название] AS Производитель,
                        Товары.[Цена]
                    FROM (((Товары
                    INNER JOIN [Категории товаров] ON Товары.[ID Категории] = [Категории товаров].[ID Категории])
                    INNER JOIN Спецификации ON Товары.[ID Спецификации] = Спецификации.[ID Спецификации])
                    INNER JOIN Производители ON Товары.[ID Производителя] = Производители.[ID Производителя]);";
                    break;
                case "Производители":
                    query = @"
                    SELECT 
                        Производители.[ID Производителя],
                        Производители.[Название],
                        Страны.[Страна]
                    FROM (Производители
                    INNER JOIN Страны ON Производители.[ID Страны] = Страны.[ID Страны]);";
                    break;
                case "Поставки":
                    query = @"
                    SELECT 
                        Поставки.[ID Поставки],
                        Склады.[Название] & ' ' & Города.[Город] AS Склад,
                        Поставщики.[Название] AS Поставщик,
                        Поставки.[Дата поставки]
                    FROM (((Поставки
                    INNER JOIN Склады ON Поставки.[ID Склада] = Склады.[ID Склада])
                    INNER JOIN Города ON Склады.[ID Города] = Города.[ID Города])
                    INNER JOIN Поставщики ON Поставки.[ID Поставщика] = Поставщики.[ID Поставщика]);";
                    break;
                case "Склады":
                    query = @"
                    SELECT
                        Склады.[ID Склада],
                        Склады.[Название],
                        Города.[Город] AS Город
                    FROM (Склады
                    INNER JOIN Города ON Склады.[ID Города] = Города.[ID Города]);";
                    break;
                default:
                    query = $"SELECT * FROM [{button.Content}]";
                    break;
            }
            
            _dataAdapter = new OleDbDataAdapter(query, _oleDbConnection);

            _dataTable = new DataTable();
            _dataAdapter.Fill(_dataTable);

            EditableGrid.ItemsSource = _dataTable.DefaultView;
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            if (TableTabControl.SelectedIndex != 1)
            {
                TableTabControl.SelectedIndex = 1;
                return;
            }

            List<string> values = new List<string>() ;
            string tableName = _previousButton.Content.ToString();
            
            foreach (var child in AddStackPanel.Children)
            {
                if (child is LabeledField labeledField)
                {
                    values.Add(labeledField.GetText());
                }
                else if (child is LabeledComboBox labeledComboBox)
                {
                    values.Add(GetForeignIndex(labeledComboBox.OriginalLabel, labeledComboBox.ComboBox.SelectedIndex));
                }
            }

            StringBuilder query = new StringBuilder();
            query.Append($"INSERT INTO [{tableName}] ");
            query.Append($"({_tables[tableName].GetFields()}) VALUES ");
            query.Append($"({Strings.Join(values.ToArray(), ", ")})");
            
            try
            {
                OleDbCommand command = new OleDbCommand(query.ToString(), _oleDbConnection);
                command.ExecuteNonQuery();
                
                OnTableButtonClicked(_previousButton); // активация кнопки
                UpdateIDs(tableName);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Не удалось добавить запись, заполните поля корректными значениями.", "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string _lastEditIndex;
        
        private void OnEditTabClicked(object sender, MouseButtonEventArgs e)
        {
            FillEditData();
        }
        
        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (TableTabControl.SelectedIndex != 2)
            {
                FillEditData();
                return;
            }
            
            if (_lastEditIndex == null)
                return;
            
            List<string> values = new List<string>() ;
            string tableName = SelectedTableLabel.Content.ToString();
            
            foreach (var child in EditStackPanel.Children)
            {
                if (child is LabeledField labeledField)
                {
                    values.Add($"[{labeledField.OriginalLabel}]={labeledField.GetText()}");
                }
                else if (child is LabeledComboBox labeledComboBox)
                {
                    values.Add($"[{labeledComboBox.OriginalLabel}]={GetForeignIndex(labeledComboBox.OriginalLabel, labeledComboBox.ComboBox.SelectedIndex)}");
                }
            }

            StringBuilder updateQuery = new StringBuilder();
            updateQuery.Append($"UPDATE [{tableName}] SET ");
            updateQuery.Append($"{Strings.Join(values.ToArray(), ", ")}");
            updateQuery.Append($" WHERE [{EditableGrid.Columns[0].Header}]={_lastEditIndex}");
            
            try
            {
                OleDbCommand command = new OleDbCommand(updateQuery.ToString(), _oleDbConnection);
                command.ExecuteNonQuery();
                
                OnTableButtonClicked(_previousButton);
                UpdateIDs(tableName);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Не удалось изменить значения. Введите корректные данные.", "Ошибка изменения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillEditData()
        {
            if (EditableGrid.SelectedIndex == -1)
            {
                MessageBox.Show("Сначала необходимо выбрать запись для изменения.", "Ошибка изменения", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
                
            var selectedItem = EditableGrid.SelectedCells[0].Column.GetCellContent(EditableGrid.SelectedItem) as TextBlock;
            TableTabControl.SelectedIndex = 2;

            _lastEditIndex = selectedItem.Text;
                
            string query = @$"SELECT * 
                                    FROM [{SelectedTableLabel.Content}] 
                                    WHERE [{EditableGrid.Columns[0].Header}] = {selectedItem.Text}";

            OleDbCommand command = new OleDbCommand(query, _oleDbConnection);
            OleDbDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                for (int i = 1; i < reader.FieldCount; i++)
                {
                    UIElement child = EditStackPanel.Children[i - 1];
                        
                    if (child is LabeledField labeledField)
                    {
                        labeledField.Text = reader[i].ToString();
                    }
                    else if (child is LabeledComboBox labeledComboBox)
                    {
                        string valueTableName = _keysToTables[labeledComboBox.OriginalLabel];
                        labeledComboBox.ComboBox.SelectedIndex = _tables[valueTableName].Ids.IndexOf(reader[i].ToString());;
                    }
                }
            }
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (EditableGrid.SelectedIndex == -1)
            {
                MessageBox.Show("Необходимо выбрать запись для удаления.", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить выбранную запись?", "Предупреждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;
            
            var selectedItem = EditableGrid.SelectedCells[0].Column.GetCellContent(EditableGrid.SelectedItem) as TextBlock;

            string query = @$"DELETE FROM [{SelectedTableLabel.Content}]
                                WHERE [{EditableGrid.Columns[0].Header}]={selectedItem.Text}";
            
            try
            {
                OleDbCommand command = new OleDbCommand(query, _oleDbConnection);
                command.ExecuteNonQuery();
                
                OnTableButtonClicked(_previousButton);
                UpdateIDs(SelectedTableLabel.Content.ToString());
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Не удалось удалить запись. Проверьте, чтобы никакая другая таблица не ссылалась на первичный ключ удаляемой записи.", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            TableTabControl.SelectedIndex = 0;
        }

        private void OnSelectClicked(object sender, RoutedEventArgs e)
        {
            string query = @"
                    SELECT 
                        Товары.[Название],
                        [Категории товаров].Название AS Категория,
                        Производители.[Название] AS Производители,
                        Страны.[Страна],
                        Клиенты.[Имя] AS [Имя клиента],
                        Клиенты.[E-mail],
                        Сотрудники.[Фамилия],
                        Сотрудники.[Имя] AS [Имя сотрудника],
                        Сотрудники.[Отчество],
                        Склады.[Название] AS [Название склада],
                        Города.[Город],
                        Поставщики.[Название] AS [Название поставщика],
                        Поставки.[Дата поставки],
                        Акции.[Название] AS [Название акции],
                        Заказы.[Количество],
                        Заказы.[Дата покупки]
                    FROM ((((((((((Заказы
                        INNER JOIN Товары ON Заказы.[ID Товара] = Товары.[ID Товара])
                        INNER JOIN [Категории товаров] ON Товары.[ID Категории] = [Категории товаров].[ID Категории])
                        INNER JOIN Производители ON Товары.[ID Производителя] = Производители.[ID Производителя])
                        INNER JOIN Страны ON Производители.[ID Страны] = Страны.[ID Страны])
                        INNER JOIN Клиенты ON Заказы.[ID Клиента] = Клиенты.[ID Клиента])
                        INNER JOIN Сотрудники ON Заказы.[ID Сотрудника] = Сотрудники.[ID Сотрудника])
                        INNER JOIN Поставки ON Заказы.[ID Поставки] = Поставки.[ID Поставки])
                        INNER JOIN Склады ON Поставки.[ID Склада] = Склады.[ID Склада])
                        INNER JOIN Города ON Склады.[ID Города] = Города.[ID Города])
                        INNER JOIN Поставщики ON Поставки.[ID Поставщика] = Поставщики.[ID Поставщика])
                        LEFT JOIN Акции ON Заказы.[ID Акции] = Акции.[ID Акции]";

            StringBuilder allQuery = new StringBuilder(query);
            bool isChecked = false; // если хотя бы один отмечен
            List<string> checkGroups = new List<string>();
            
            foreach (UIElement child in CheckboxPanel.Children)
            {
                if (child is CheckboxGroup checkboxGroup)
                {
                    List<string> checkboxes = new List<string>();

                    for (int i = 0; i < checkboxGroup.CheckboxPanel.Children.Count; i++)
                    {
                        if (checkboxGroup.CheckboxPanel.Children[i] is CheckBox checkbox)
                        {
                            if (checkbox.IsChecked == true)
                            {
                                isChecked = true;
                                string tableName = _keysToTables[checkboxGroup.LabelID];
                                checkboxes.Add($"[{tableName}].[{checkboxGroup.LabelID}]={_tables[tableName].Ids[i]}");
                            }
                        }
                    }
                    if (checkboxes.Count > 1)
                        checkGroups.Add($"({Strings.Join(checkboxes.ToArray(), " OR ")})");
                    else if (checkboxes.Count == 1)
                        checkGroups.Add($"{Strings.Join(checkboxes.ToArray(), " OR ")}");
                }
            }

            if (isChecked)
                allQuery.Append("\nWHERE ");

            allQuery.Append($"{Strings.Join(checkGroups.ToArray(), "\nAND ")}");
            var dataAdapter = new OleDbDataAdapter(allQuery.ToString(), _oleDbConnection);

            var dataTable = new DataTable();
            dataAdapter.Fill(dataTable);

            QueryTable.ItemsSource = dataTable.DefaultView;
        }

        private void OnCancelQuery(object sender, RoutedEventArgs e)
        {
            foreach (UIElement child in CheckboxPanel.Children)
            {
                if (child is CheckboxGroup checkBoxGroup)
                {
                    foreach (var groupChild in checkBoxGroup.CheckboxPanel.Children)
                    {
                        if (groupChild is CheckBox checkBox)
                            checkBox.IsChecked = false;
                    }
                }
            }
            
            OnSelectClicked(null, null);
        }
    }
}