using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MoneyBear.ViewModels;

namespace MoneyBear.Views.Debts;

public partial class DebtAddView : Window
{
    public DebtAddView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += WindowAttachedToVisualTree;
    }
    
    private void WindowAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if(sender == null || e == null)
            return;
        
        if (this.VisualRoot is Window window)
        {
            if(this.DataContext is DebtAddViewModel viewModel)
                viewModel.CurrentWindow = window;
        }
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        var viewModel = this.DataContext as DebtAddViewModel;
        if(textBox == null)
            return;
        if(textBox.Text.Length <= 0)
            return;
        if(viewModel == null)
            return;
        if (!IsInputValidDecimal(textBox.Text))
        {
            textBox.Text = "0";
            viewModel.DebtSum = textBox.Text;
        }
        if (!decimal.TryParse(textBox.Text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("de-DE"), out decimal parsedValue))
        {
            textBox.Text = "0";
            viewModel.DebtSum = textBox.Text;
        }
        else
        {
            viewModel.DebtSum = textBox.Text;
        }
    }
    
    private bool IsInputValidDecimal(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;
        
        CultureInfo culture = new CultureInfo("de-DE");
        input = input.Replace(culture.NumberFormat.CurrencySymbol, string.Empty).Trim();
        var regex = @"^-?\d+([.,]?\d{0,2})?$";

        return Regex.IsMatch(input, regex);
    }
}