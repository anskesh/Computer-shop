using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ComputerShop.CustomControl;

namespace ComputerShop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OleDbConnection _oleDbConnection;
        private Button _previousButton;

        private Dictionary<string, TableData> _tables = new();
        private Dictionary<string, string> _keysToTables;

        public MainWindow()
        {
            InitializeComponent();

            _keysToTables = new Dictionary<string, string>() // создание ассоциайций внешних ключей с таблицами
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
            string connectionString = Environment.Is64BitOperatingSystem
                ? "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=ComputerShop.mdb"
                : "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=ComputerShop.mdb";

            try
            {
                _oleDbConnection = new OleDbConnection(connectionString);
                _oleDbConnection.Open();
            }
            catch (Exception exception)
            {
                MessageBox.Show("Не удалось подключиться к базе данных!", "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            InitializeTablesInfo();
            ChangeTable(ButtonsPanel.Children[0]); // активация кнопки
            ExecuteQuery(null, null);
        }

        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
            if (_oleDbConnection != null && _oleDbConnection.State != ConnectionState.Closed)
                _oleDbConnection.Close();
        }

        private void InitializeTablesInfo()
        {
            DataTable schemaTables = _oleDbConnection.GetSchema("Tables");
            var tableNames = new List<string>();

            foreach (DataRow row in schemaTables.Rows)
            {
                string tableType = row["TABLE_TYPE"].ToString();
                if (tableType != "TABLE") continue; // нужны только простые таблицы
                
                string tableName = row["TABLE_NAME"].ToString();
                tableNames.Add(tableName);
            }

            foreach (string tableName in tableNames)
            {
                DataTable schemaColumns = _oleDbConnection.GetSchema("Columns", new[] {null, null, tableName, null});
                var orderedColumns = schemaColumns.AsEnumerable()
                    .OrderBy(row => row.Field<long>("ORDINAL_POSITION"))
                    .Select(row => row["COLUMN_NAME"].ToString());

                _tables.Add(tableName, new TableData()
                {
                    Fields = orderedColumns.ToList()
                });

                UpdateForeignIDs(tableName);
            }
        }

        private void UpdateForeignIDs(string tableName)
        {
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
                    query =
                        @"SELECT Сотрудники.[ID Сотрудника], Сотрудники.[Фамилия], Сотрудники.[Имя], Сотрудники.[Отчество]
                                FROM Сотрудники";
                    break;
                case "Поставки":
                    query =
                        @"SELECT Поставки.[ID Поставки], Склады.[Название], Города.[Город], Поставщики.[Название], FORMAT(Поставки.[Дата поставки], 'dd.mm.yyyy')
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
                    query = @"SELECT Акции.[ID Акции], Акции.[Название], 'До ' & FORMAT(Акции.[Дата конца], 'dd.mm.yyyy')
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

            var command = new OleDbCommand(query, _oleDbConnection);
            OleDbDataReader reader = command.ExecuteReader();
            var stringBuilder = new StringBuilder();

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

        private void UpdateQueryField(string tableName)
        {
            foreach (CheckboxGroup checkboxGroup in CheckboxPanel.Children)
            {
                if (_keysToTables[checkboxGroup.LabelID] == tableName)
                {
                    checkboxGroup.UpdateData(_tables[tableName]);
                    return;
                }
            }
        }

        private void ChangeTable(object sender, RoutedEventArgs e = null)
        {
            Button button = sender as Button;
            ChangeButton(button);

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
                        Склады.[Название] & ' ' & Поставщики.[Название] & ' ' & FORMAT(Поставки.[Дата поставки], 'dd.mm.yyyy') AS Поставщик,
                        Акции.[Название] AS Акция,
                        Количество,
                        FORMAT([Дата покупки], 'dd.mm.yyyy') AS [Дата покупки]
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
                        FORMAT(Поставки.[Дата поставки], 'dd.mm.yyyy') AS [Дата поставки]
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
                case "Акции":
                    query = @"
                            SELECT Акции.[ID Акции],
                                [Название],
                                [Описание],
                                [Скидка],
                                FORMAT([Дата начала], 'dd.mm.yyyy') AS [Дата начала],
                                FORMAT([Дата конца], 'dd.mm.yyyy') AS [Дата конца]
                            FROM Акции";
                    break;
                default:
                    query = $"SELECT * FROM [{button.Content}]";
                    break;
            }

            var dataAdapter = new OleDbDataAdapter(query, _oleDbConnection);
            var dataTable = new DataTable();
            dataAdapter.Fill(dataTable);

            EditableGrid.ItemsSource = dataTable.DefaultView;
        }

        private void ChangeButton(Button newButton)
        {
            if (_previousButton != null)
                _previousButton.Background = new SolidColorBrush(Colors.LightGray);

            TableTabControl.SelectedIndex = 0;
            SelectedTableLabel.Content = newButton.Content;
            newButton.Background = new SolidColorBrush(Colors.DarkGray);
            _previousButton = newButton;
        }

        private void InitializeTableInfo(string tableName)
        {
            AddStackPanel.Children.Clear();
            EditStackPanel.Children.Clear();

            for (int i = 0; i < _tables[tableName].Fields.Count; i++)
            {
                if (i == 0) continue; // нельзя поменять или установить первичный ключ
                string column = _tables[tableName].Fields[i];

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

        private List<string> GetForeignValues(string columnName)
        {
            string tableName = _keysToTables[columnName];
            return _tables[tableName].Values;
        }

        #region EditingData

        private string GetForeignIndex(string columnName, int comboBoxIndex)
        {
            if (comboBoxIndex == -1) // пустое значение
                return "NULL";

            string tableName = _keysToTables[columnName];
            return _tables[tableName].Ids[comboBoxIndex];
        }

        private void AddRecord(object sender, RoutedEventArgs e)
        {
            if (TableTabControl.SelectedIndex != 1)
            {
                TableTabControl.SelectedIndex = 1;
                return;
            }

            var values = new List<string>();
            string tableName = _previousButton.Content.ToString();

            foreach (var child in AddStackPanel.Children)
            {
                switch (child)
                {
                    case LabeledField labeledField:
                        values.Add(labeledField.GetText());
                        break;
                    case LabeledComboBox labeledComboBox:
                        values.Add(GetForeignIndex(labeledComboBox.OriginalLabel, labeledComboBox.ComboBox.SelectedIndex));
                        break;
                }
            }

            var query = new StringBuilder();
            query.Append($"INSERT INTO [{tableName}] ");
            query.Append($"({_tables[tableName].GetFields()}) VALUES ");
            query.Append($"({String.Join(", ", values.ToArray())})");

            try
            {
                var command = new OleDbCommand(query.ToString(), _oleDbConnection);
                command.ExecuteNonQuery();

                ChangeTable(_previousButton); // активация кнопки
                UpdateForeignIDs(tableName);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Не удалось добавить запись, заполните поля корректными значениями.",
                    "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnEditTabClicked(object sender, MouseButtonEventArgs e)
        {
            FillEditingRecord();
        }

        private void EditRecord(object sender, RoutedEventArgs e)
        {
            if (TableTabControl.SelectedIndex != 2)
            {
                FillEditingRecord();
                return;
            }

            if (EditableGrid.SelectedIndex == -1)
                return;

            var values = new List<string>();
            string tableName = SelectedTableLabel.Content.ToString();

            foreach (UIElement child in EditStackPanel.Children)
            {
                switch (child)
                {
                    case LabeledField labeledField:
                        values.Add($"[{labeledField.OriginalLabel}]={labeledField.GetText()}");
                        break;
                    case LabeledComboBox labeledComboBox:
                        values.Add($"[{labeledComboBox.OriginalLabel}]={GetForeignIndex(labeledComboBox.OriginalLabel, labeledComboBox.ComboBox.SelectedIndex)}");
                        break;
                }
            }

            var updateQuery = new StringBuilder();
            updateQuery.Append($"UPDATE [{tableName}] SET ");
            updateQuery.Append($"{String.Join(", ", values)}");
            updateQuery.Append($" WHERE [{EditableGrid.Columns[0].Header}]={_selectedIndex}");

            try
            {
                var command = new OleDbCommand(updateQuery.ToString(), _oleDbConnection);
                command.ExecuteNonQuery();

                ChangeTable(_previousButton);
                UpdateForeignIDs(tableName);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Не удалось изменить значения. Введите корректные данные.", "Ошибка изменения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string _selectedIndex;
        
        private void FillEditingRecord()
        {
            if (TableTabControl.SelectedIndex == 2) // если уже находимся в этой вкладке, значит выбрали
                return;
                
            if (EditableGrid.SelectedIndex == -1)
            {
                MessageBox.Show("Сначала необходимо выбрать запись для изменения.", "Ошибка изменения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedItem = EditableGrid.SelectedCells[0].Column.GetCellContent(EditableGrid.SelectedItem) as TextBlock;
            _selectedIndex = selectedItem.Text;
            TableTabControl.SelectedIndex = 2;

            string query = @$"SELECT * 
                                    FROM [{SelectedTableLabel.Content}] 
                                    WHERE [{EditableGrid.Columns[0].Header}] = {_selectedIndex}";

            var command = new OleDbCommand(query, _oleDbConnection);
            OleDbDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                for (int i = 1; i < reader.FieldCount; i++)
                {
                    UIElement child = EditStackPanel.Children[i - 1];

                    switch (child)
                    {
                        case LabeledField labeledField:
                            labeledField.Text = reader[i].ToString();
                            break;
                        case LabeledComboBox labeledComboBox:
                        {
                            string valueTableName = _keysToTables[labeledComboBox.OriginalLabel];
                            labeledComboBox.ComboBox.SelectedIndex = _tables[valueTableName].Ids.IndexOf(reader[i].ToString());
                            break;
                        }
                    }
                }
            }
        }

        private void RemoveRecord(object sender, RoutedEventArgs e)
        {
            if (EditableGrid.SelectedIndex == -1)
            {
                MessageBox.Show("Необходимо выбрать запись для удаления.", "Ошибка удаления", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить выбранную запись?",
                "Предупреждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;

            string tableName = SelectedTableLabel.Content.ToString();
            var selectedItem = EditableGrid.SelectedCells[0].Column.GetCellContent(EditableGrid.SelectedItem) as TextBlock;
            string query = @$"DELETE FROM [{tableName}]
                                WHERE [{EditableGrid.Columns[0].Header}]={selectedItem.Text}";

            try
            {
                var command = new OleDbCommand(query, _oleDbConnection);
                command.ExecuteNonQuery();

                ChangeTable(_previousButton);
                UpdateForeignIDs(tableName);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"Не удалось удалить запись. Проверьте, чтобы никакая другая таблица не ссылалась на первичный ключ удаляемой записи.",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelEditingRecord(object sender, RoutedEventArgs e)
        {
            TableTabControl.SelectedIndex = 0;
        }

        #endregion

        #region QueryTab

        private void ExecuteQuery(object sender, RoutedEventArgs e)
        {
            string query = @"
                    SELECT
                        [ID Заказа],
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
                        FORMAT(Поставки.[Дата поставки], 'dd.mm.yyyy') AS [Дата поставки],
                        Акции.[Название] AS [Название акции],
                        Заказы.[Количество],
                        FORMAT([Дата покупки], 'dd.mm.yyyy') AS [Дата покупки]
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

            var allQuery = new StringBuilder(query);
            var checkGroups = new List<string>(); // список групп
            var checkboxes = new List<string>(); // список всех отмеченных значений
            bool isChecked = false; // если ни один не отмечен - не редактируем запрос

            foreach (CheckboxGroup checkboxGroup in CheckboxPanel.Children)
            {
                checkboxes.Clear();

                for (int i = 0; i < checkboxGroup.CheckboxPanel.Children.Count; i++)
                {
                    if (checkboxGroup.CheckboxPanel.Children[i] is not CheckBox checkbox) continue;
                    if (checkbox.IsChecked != true) continue;

                    isChecked = true;
                    string tableName = _keysToTables[checkboxGroup.LabelID];
                    checkboxes.Add($"[{tableName}].[{checkboxGroup.LabelID}]={_tables[tableName].Ids[i]}");
                }

                switch (checkboxes.Count)
                {
                    case > 1:
                        checkGroups.Add($"({String.Join(" OR ", checkboxes.ToArray())})");
                        break;
                    case 1:
                        checkGroups.Add($"{String.Join(" OR ", checkboxes.ToArray())}");
                        break;
                }
            }

            if (isChecked) allQuery.Append("\nWHERE ");
            allQuery.Append($"{String.Join( "\nAND ", checkGroups.ToArray())}");

            var dataAdapter = new OleDbDataAdapter(allQuery.ToString(), _oleDbConnection);
            var dataTable = new DataTable();

            dataAdapter.Fill(dataTable);
            QueryTable.ItemsSource = dataTable.DefaultView;
        }

        private void ResetQuery(object sender, RoutedEventArgs e)
        {
            foreach (UIElement child in CheckboxPanel.Children)
            {
                if (child is not CheckboxGroup checkBoxGroup) continue;

                foreach (CheckBox checkBox in checkBoxGroup.CheckboxPanel.Children)
                    checkBox.IsChecked = false;
            }

            ExecuteQuery(null, null); // вызываем обновление таблицы по новому запросу
        }

        #endregion
    }
}