using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MoneyBear.ViewModels;

namespace MoneyBear.Views.Grid;

public partial class GridEditLineView : Window
{
    public GridEditLineView()
    {
        InitializeComponent();
    }
    
    private void Button_OnArrowUpClicked(object? sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as GridEditLineViewModel;
        if(viewModel == null)
            return;
        if (viewModel.EditGridLineNumber + 1 <= viewModel.GridLineNumberMaximum)
        {
            viewModel.EditGridLineNumber++;
            LineArrowDown.IsEnabled = true;
        }
        if (viewModel.EditGridLineNumber == viewModel.GridLineNumberMaximum)
            ((Button)sender).IsEnabled = false;

        if (PreviewGrid != null)
        {
            PreviewGrid.SelectedItem = viewModel.EditGridLine;
            PreviewGrid.UpdateLayout();
            PreviewGrid.ScrollIntoView(viewModel.EditGridLine, PreviewGrid.Columns[0]);
            PreviewGrid.Focus();
        }
    }

    private void Button_OnArrowDownClicked(object? sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as GridEditLineViewModel;
        if(viewModel == null)
            return;
        if (viewModel.EditGridLineNumber - 1 >= viewModel.GridLineNumberMinimum)
        {
            viewModel.EditGridLineNumber--;
            LineArrowUp.IsEnabled = true;
        }
        if(viewModel.EditGridLineNumber == viewModel.GridLineNumberMinimum)
            ((Button)sender).IsEnabled = false;
        
        if (PreviewGrid != null)
        {
            PreviewGrid.SelectedItem = viewModel.EditGridLine;
            PreviewGrid.UpdateLayout();
            PreviewGrid.ScrollIntoView(viewModel.EditGridLine, PreviewGrid.Columns[0]);
            PreviewGrid.Focus();
        }
    }
    
    // private bool IsInputValidDecimal(string input)
    // {
    //     foreach (char c in input)
    //     {
    //         if (!char.IsDigit(c) && c != '.' && c != ',')
    //         {
    //             return false;
    //         }
    //     }
    //
    //     return true;
    // }
    
    // private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    // {
    //     var textBox = sender as TextBox;
    //     var viewModel = this.DataContext as GridEditLineViewModel;
    //     if(textBox == null)
    //         return;
    //     if(textBox.Text.Length <= 0)
    //         return;
    //     if(viewModel == null)
    //         return;
    //     if (!IsInputValidDecimal(textBox.Text))
    //     {
    //         textBox.Text = "0";
    //         viewModel.EditGridLineValue = textBox.Text;
    //         //MessageService.WarningMessageBoxClassic("Es sind nur gültige Zahlenwerte als Eingabe erlaubt!", (int)ErrorEnum.NotValidInput);
    //     }
    //     if (!decimal.TryParse(textBox.Text, NumberStyles.Number, new CultureInfo("de-DE"), out decimal parsedValue))
    //     {
    //         textBox.Text = "0";
    //         viewModel.EditGridLineValue = textBox.Text;
    //         //MessageService.WarningMessageBoxClassic("Es sind nur gültige Zahlenwerte als Eingabe erlaubt!", (int)ErrorEnum.NotValidInput);
    //     }
    //     else
    //     {
    //         //textBox.Text = parsedValue.ToString("N", new CultureInfo("de-DE"));
    //         viewModel.EditGridLineValue = textBox.Text;
    //         if (viewModel.EditGridLine != null)
    //             viewModel.EditGridLine.JanuarValue = parsedValue;
    //     }
    // }
}