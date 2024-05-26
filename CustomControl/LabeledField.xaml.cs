using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ComputerShop.CustomControl;

public partial class LabeledField : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(LabeledField), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(LabeledField), new PropertyMetadata(default(string)));

    public LabeledField()
    {
        InitializeComponent();
    }

    public LabeledField(string label, string text = "") : this()
    {
        OriginalLabel = label;
        Label = label + ":";
        
        Text = text;
    }

    public string Label
    {
        get => (string) GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string OriginalLabel { get; private set; }

    public string Text
    {
        get => (string) GetValue(TextProperty);
        set
        {
            var newValue = !Label.Contains("Дата") ? value : value.Split(' ')[0];
            SetValue(TextProperty, newValue);
        }
    }

    private List<string> _nonStringKeys = new()
        {"Скидка", "Количество", "Ширина", "Вес", "Высота", "Гарантия", "Цена"};

    public string GetText()
    {
        if (_nonStringKeys.Contains(OriginalLabel))
        {
            return Text;
        }
        
        return $"\"{Text}\"";
    }
}