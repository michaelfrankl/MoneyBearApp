using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Windowing;
using MoneyBear.Services;
using MoneyBear.ViewModels;
using NASPLOID.Models.MoneyBear;

namespace MoneyBear.Views;

public partial class DebtView : UserControl
{
    public DebtView()
    {
        InitializeComponent();

        this.AttachedToVisualTree += WindowAttachedToVisualTree;
        
        if (GridData != null)
        {
            foreach (var column in GridData.Columns)
            {
                if (column is DataGridTextColumn textColumn && textColumn.Header?.ToString().ToString() == "Summe")
                {
                    if (textColumn.Binding is Binding binding)
                    {
                        textColumn.Binding = new Binding(binding.Path)
                        {
                            Mode = binding.Mode,
                            Converter = binding.Converter,
                            StringFormat = binding.StringFormat,
                            ConverterCulture = new CultureInfo("de-DE"),
                            UpdateSourceTrigger = binding.UpdateSourceTrigger
                        };
                    }
                }
            }
        }
    }

    private void WindowAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if(sender == null || e == null)
            return;
        
        if (this.VisualRoot is Window window)
        {
            if(this.DataContext is DebtViewModel viewModel)
                viewModel.CurrentWindow = window;
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

    private async void GridData_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        var viewModel = this.DataContext as DebtViewModel;
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
                if (e.Row.DataContext is MBDebtList mbdebtList)
                {
                    mbdebtList.DebtSum = parsedValue;
                    int result = await viewModel.SaveDebtValueChanges(mbdebtList);
                     if (result == (int)ErrorEnum.Aborted)
                     {
                         MessageService.WarningMessageBoxClassic("Der Wert konnte nicht gespeichert werden!", (int)ErrorEnum.Aborted);
                     }
                }
            }
        }
    }

    private void GridData_OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
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