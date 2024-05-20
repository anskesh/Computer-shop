using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ComputerShop.CustomControl;

public partial class LabeledComboBox : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(LabeledComboBox), new PropertyMetadata(default(string)));

    public LabeledComboBox()
    {
        InitializeComponent();
    }

    public LabeledComboBox(string label, List<string> values = null) : this()
    {
        OriginalLabel = label;
        Label = label + ":";
        
        if (values == null)
            return;

        ComboBox.Items.Clear();
        
        foreach (var value in values)
            ComboBox.Items.Add(value);
        
        if (label != "ID Акции")
        {
            ComboBox.SelectedIndex = 0;
        }
        else
        {
            ComboBox.Items.Add("");
            _nullIndex = ComboBox.Items.Count - 1;
            ComboBox.SelectionChanged += OnSelectionChanged;
        }
    }

    private int _nullIndex;

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboBox.SelectedIndex == _nullIndex)
            ComboBox.SelectedIndex = -1;
    }

    public string Label
    {
        get => (string) GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string OriginalLabel { get; private set; }
}