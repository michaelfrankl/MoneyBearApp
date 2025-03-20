using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.Core;
using MoneyBear.ViewModels;
using NASPLOID.Models.MoneyBear;

namespace MoneyBear.Views.Grid;

public partial class GridAddLineView : Window
{
    public GridAddLineView()
    {
        InitializeComponent();
    }


    private void Button_OnArrowUpClicked(object? sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as GridAddLineViewModel;
        if(viewModel == null)
            return;
        if (viewModel.NewGridLineNumber + 1 <= viewModel.GridLineNumberMaximum)
        {
            viewModel.NewGridLineNumber++;
            LineArrowDown.IsEnabled = true;
        }
        if (viewModel.NewGridLineNumber == viewModel.GridLineNumberMaximum)
            ((Button)sender).IsEnabled = false;

        if (PreviewGrid != null)
        {
            PreviewGrid.SelectedItem = viewModel.NewGridLine;
            PreviewGrid.UpdateLayout();
            PreviewGrid.ScrollIntoView(viewModel.NewGridLine, PreviewGrid.Columns[0]);
            PreviewGrid.Focus();
        }
    }

    private void Button_OnArrowDownClicked(object? sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as GridAddLineViewModel;
        if(viewModel == null)
            return;
        if (viewModel.NewGridLineNumber - 1 >= viewModel.GridLineNumberMinimum)
        {
            viewModel.NewGridLineNumber--;
            LineArrowUp.IsEnabled = true;
        }
        if(viewModel.NewGridLineNumber == viewModel.GridLineNumberMinimum)
            ((Button)sender).IsEnabled = false;
        
        if (PreviewGrid != null)
        {
            PreviewGrid.SelectedItem = viewModel.NewGridLine;
            PreviewGrid.UpdateLayout();
            PreviewGrid.ScrollIntoView(viewModel.NewGridLine, PreviewGrid.Columns[0]);
            PreviewGrid.Focus();
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

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        var viewModel = this.DataContext as GridAddLineViewModel;
        if(textBox == null)
            return;
        if(textBox.Text.Length <= 0)
            return;
        if(viewModel == null)
            return;
        if (!IsInputValidDecimal(textBox.Text))
        {
            textBox.Text = "0";
            viewModel.NewGridLineValue = textBox.Text;
            //MessageService.WarningMessageBoxClassic("Es sind nur gültige Zahlenwerte als Eingabe erlaubt!", (int)ErrorEnum.NotValidInput);
        }
        if (!decimal.TryParse(textBox.Text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("de-DE"), out decimal parsedValue))
        {
            textBox.Text = "0";
            viewModel.NewGridLineValue = textBox.Text;
            //MessageService.WarningMessageBoxClassic("Es sind nur gültige Zahlenwerte als Eingabe erlaubt!", (int)ErrorEnum.NotValidInput);
        }
        else
        {
            //textBox.Text = parsedValue.ToString("N", new CultureInfo("de-DE"));
            viewModel.NewGridLineValue = textBox.Text;
            if (viewModel.NewGridLine != null)
                viewModel.NewGridLine.JanuarValue = parsedValue;
        }
    }
}