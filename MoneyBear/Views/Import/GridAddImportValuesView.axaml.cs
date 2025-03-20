using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MoneyBear.ViewModels;
using MoneyBear.ViewModels.Import;
using NASPLOID.Models.MoneyBear;

namespace MoneyBear.Views.Import;

public partial class GridAddImportValuesView : Window
{
    public GridAddImportValuesView()
    {
        InitializeComponent();

        if (CloseBtn != null)
            CloseBtn.IsEnabled = true;
    }
    
    private void OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        var gridLine = e.Row.DataContext as GridLine;
        var viewModel = this.DataContext as GridAddImportValuesViewModel;
        if(viewModel == null)
            return;
        
        if (gridLine != null)
        {
            if (e.Column.Header == null) return;
            if(e.Column.Header.ToString().Length <= 0) return;
            
            if (viewModel.IsTaskActive)
            {
                e.Cancel = true;
                return;
            }
            if (gridLine.LineTypId == (int)LineTypeEnum.Summen || gridLine.LineTypId == (int)LineTypeEnum.Kontostand)
                e.Cancel = true;
            else
                e.Cancel = false;
        }
    }
    
    private async void OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        var viewModel = this.DataContext as GridAddImportValuesViewModel;
        var dataGrid = sender as DataGrid;
        if(viewModel == null) return;
        if(dataGrid == null) return;
        var textBox = e.EditingElement as TextBox;
        if (textBox != null)
        {
            if (!IsInputValidDecimal(textBox.Text))
            {
                textBox.Text = "0";
            }
            if (!decimal.TryParse(textBox.Text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("de-DE"), out decimal parsedValue))
            {
                textBox.Text = "0";
                //Info-Message!
            }
            else
            {
                try
                {
                    textBox.Text = parsedValue.ToString("C2", new CultureInfo("de-DE"));
                }
                catch (Exception exception)
                {
                    textBox.Text = "0";
                    textBox.Text = parsedValue.ToString("C2", new CultureInfo("de-DE"));
                }
                var gridLine = e.Row.DataContext as GridLine;
                if (gridLine != null)
                {
                    await viewModel.SaveDefaultGridValueAsync(gridLine);
                }
            }
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
    
    private void OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.EditingElement is TextBox textBox)
        {
            textBox.TextInput += OnTextInputValidation;
        }
    }
    
    private void OnTextInputValidation(object? sender, TextInputEventArgs e)
    {
        var textBox = sender as TextBox;
        if(textBox == null) return;
        if (e.Text == null) return;
        string input = e.Text;
        if (!IsInputValidDecimal(input))
        {
            e.Handled = true;
        }
    }
}