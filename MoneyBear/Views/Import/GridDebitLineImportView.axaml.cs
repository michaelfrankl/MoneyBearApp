using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MoneyBear.Services;
using MoneyBear.ViewModels;
using NASPLOID.Models.MoneyBear;

namespace MoneyBear.Views.Import;

public partial class GridDebitLineImportView : Window
{
    public GridDebitLineImportView()
    {
        InitializeComponent();
    }
    
    private void OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        var gridLine = e.Row.DataContext as GridLine;
        var viewModel = this.DataContext as GridDebitLineImportViewModel;
        if(viewModel == null) return;
        
        if (gridLine != null)
        {
            if (e.Column.Header == null) return;
            if(e.Column.Header.ToString() == null) return;

            if (viewModel.IsTaskActive)
            {
                e.Cancel = true;
                return;
            }

            if (gridLine.ValidFrom.Month > GetMonthNumberFromName(e.Column.Header.ToString()) && gridLine.ValidFrom.Year >= DateTime.Now.Year)
            {
                e.Cancel = true;
                return;
            }

            if (gridLine.ValidTo.Month < GetMonthNumberFromName(e.Column.Header.ToString()) &&
                gridLine.ValidTo.Year <= DateTime.Now.Year)
            {
                e.Cancel = true;
                return;
            }
        }
    }
    private async void OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        var viewModel = this.DataContext as GridDebitLineImportViewModel;
        var dataGrid = sender as DataGrid;
        if(viewModel == null) return;
        if(dataGrid == null) return;
        if(e.Column.Header == null) return;
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
                    viewModel.IsTaskActive = true;
                    int result = await viewModel.SaveDebitLineChanges(gridLine, parsedValue,
                        GetMonthNumberFromName(e.Column.Header.ToString()), DateTime.Now.Year);
                    if (result == (int)ErrorEnum.Aborted)
                    {
                        MessageService.WarningMessageBoxClassic("Der neue Wert konnte nicht gespeichert werden!", (int)ErrorEnum.Aborted);
                    }else if (result == (int)ErrorEnum.Success)
                    {
                        GridLine lineToEdit = viewModel.AppService.Session.GridLines.FirstOrDefault(x => x.Id == gridLine.Id);
                        if(lineToEdit != null)
                            SetValueForSpecificMonth(lineToEdit, parsedValue, GetMonthNumberFromName(e.Column.Header.ToString()));
                    }
                    viewModel.IsTaskActive = false;
                }
            }
        }
    }
    
    private int GetMonthNumberFromName(string month)
    {
        DateTime date = DateTime.ParseExact(month, "MMMM", new CultureInfo("de-De"));
        return date.Month;
    }

    private void SetValueForSpecificMonth(GridLine gridLine, decimal value, int month)
    {
        switch (month)
        {
            case (int)MonthEnum.Januar:
                gridLine.JanuarValue = value;
                break;
            case (int)MonthEnum.Februar:
                gridLine.FebruarValue = value;
                break;
            case (int)MonthEnum.März:
                gridLine.MärzValue = value;
                break;
            case (int)MonthEnum.April:
                gridLine.AprilValue = value;
                break;
            case (int)MonthEnum.Mai:
                gridLine.MaiValue = value;
                break;
            case (int)MonthEnum.Juni:
                gridLine.JuniValue = value;
                break;
            case (int)MonthEnum.Juli:
                gridLine.JuliValue = value;
                break;
            case (int)MonthEnum.August:
                gridLine.AugustValue = value;
                break;
            case (int)MonthEnum.September:
                gridLine.SeptemberValue = value;
                break;
            case (int)MonthEnum.Oktober:
                gridLine.OktoberValue = value;
                break;
            case (int)MonthEnum.November:
                gridLine.NovemberValue = value;
                break;
            case (int)MonthEnum.Dezember:
                gridLine.DezemberValue = value;
                break;
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