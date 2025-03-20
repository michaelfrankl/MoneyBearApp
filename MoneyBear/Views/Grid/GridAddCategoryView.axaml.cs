using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MoneyBear.Views.Grid;

public partial class GridAddCategoryView : Window
{
    public GridAddCategoryView()
    {
        InitializeComponent();
        if (AddBtn != null)
            AddBtn.IsEnabled = false;

    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if(textBox == null)
            return;
        if(AddBtn == null)
            return;
        if(textBox.Text.Length <= 2)
            AddBtn.IsEnabled = false;
        else
            AddBtn.IsEnabled = true;
    }
}