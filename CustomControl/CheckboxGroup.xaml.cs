using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ComputerShop.CustomControl;

public partial class CheckboxGroup : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(CheckboxGroup), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty LabelIDProperty = DependencyProperty.Register(nameof(LabelID), typeof(string), typeof(CheckboxGroup), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<string>), typeof(CheckboxGroup), new PropertyMetadata(default(ObservableCollection<string>)));

    public CheckboxGroup()
    {
        InitializeComponent();

        DataContext = this;
        Items = new ObservableCollection<string>();
    }

    public string LabelID
    {
        get => (string) GetValue(LabelIDProperty);
        set => SetValue(LabelIDProperty, value);
    }
    
    public string Label
    {
        get => (string) GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public ObservableCollection<string> Items
    {
        get => (ObservableCollection<string>) GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private TableData _tableData;

    public void UpdateData(TableData tableData = null)
    {
        if (_tableData == null)
            _tableData = tableData;
        
        if (_tableData == null && tableData == null)
            return;

        Items = new ObservableCollection<string>(tableData.Values);
        CheckboxPanel.Children.Clear();
        
        foreach (var item in Items)
        {
            var checkBox = new CheckBox();
            checkBox.Content = item;
            checkBox.ToolTip = item;
            checkBox.Style = (Style) FindResource("Checkbox");
            CheckboxPanel.Children.Add(checkBox);
        }
    }
}