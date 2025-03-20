using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DynamicData;
using MoneyBear.Converter;
using MoneyBear.Models;
using MoneyBear.Services;
using MoneyBear.ViewModels;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.Views;

public partial class GridView : UserControl
{
    public GridView()
    {
        InitializeComponent();
        //GenerateGridColumnsWithPreviewSelction();

        this.AttachedToVisualTree += WindowAttachedToVisualTree;
    }

    private void GenerateGridColumnsWithPreviewSelction()
    {
        var decimalConverter = new DecimalConverter();
        GridViewDataGrid.Columns.Clear();
        DateTime? currentDate = GridDatePicker.SelectedDate;
        string monthName = currentDate.Value.ToString("MMMM");
        MonthPreview preview = MonthPreviewExtensions.GetMonthPreview(GridMonthsPreviewComboBox.SelectedIndex!);
        var viewModel = this.DataContext as GridViewModel;
        if (viewModel == null) return;
        
        GridViewDataGrid.Columns.Add(new DataGridTextColumn()
        {
            Header = "Zeilenname",
            Binding = new Binding("Name"),
            IsReadOnly = true,
        });
        
        GridViewDataGrid.Columns.Add(new DataGridTextColumn()
        {
            Header = monthName,
            Binding = new Binding(monthName+"Value")
            {
                StringFormat = "C2",
                Converter = decimalConverter,
                ConverterCulture = new CultureInfo("de-DE"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            }
        });
        if (preview == MonthPreview.CurrentMonth) return;

        for (int i = 1; i <= (int)preview; i++)
        {
            currentDate = currentDate.Value.AddMonths(1);
            monthName = currentDate.Value.ToString("MMMM");
            GridViewDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = monthName,
                Binding = new Binding(monthName+"Value")
                {
                    StringFormat = "C2",
                    Converter = decimalConverter,
                    ConverterCulture = new CultureInfo("de-DE"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            });
        }
    }

    private void OnCreateGridBtnClick(object? sender, RoutedEventArgs e)
    {
        GenerateGridColumnsWithPreviewSelction();
    }

    private void OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        var gridLine = e.Row.DataContext as GridLine;
        DateTime? currentDate = GridDatePicker.SelectedDate;
        if(currentDate == null) return;
        int currentGridYear = currentDate.Value.Year;
        int currentGridMonth = currentDate.Value.Month;
        var viewModel = this.DataContext as GridViewModel;
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
            {
                e.Cancel = true;
                return;
            }
            
            if (currentGridYear > gridLine.ValidTo.Year)
            {
                e.Cancel = true;
                return;
            }

            if (currentGridYear > gridLine.ValidTo.Year && GetMonthNumberFromName(e.Column.Header.ToString()) > gridLine.ValidTo.Month)
            {
                e.Cancel = true;
                return;
            }

            if (currentGridYear <= gridLine.ValidTo.Year && GetMonthNumberFromName(e.Column.Header.ToString()) > gridLine.ValidTo.Month)
            {
                e.Cancel = true;
                return;
            }

            if (currentGridYear <= gridLine.ValidTo.Year && (GetMonthNumberFromName(e.Column.Header.ToString()) < gridLine.ValidFrom.Month) && currentGridYear <= gridLine.ValidFrom.Year)
            {
                e.Cancel = true;
                return;
            }
        }
    }

    private bool IsCellEditable(GridLine item)
    {
        DateTime? currentDate = GridDatePicker.SelectedDate;
        if(currentDate == null) return true;
        int currenYear = currentDate.Value.Year;
        int currentMonth = currentDate.Value.Month;

        if (currenYear <= item.ValidTo.Year || currenYear <= item.ValidTo.Year && currentMonth <= item.ValidTo.Month)
            return false;
        return true;
    }

    private int GetMonthNumberFromName(string month)
    {
        try
        {
            DateTime date = DateTime.ParseExact(month, "MMMM", new CultureInfo("de-De"));
            return date.Month;
        }
        catch (Exception e)
        {
            return 0;
        }
        
    }


    private async void OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        var viewModel = this.DataContext as GridViewModel;
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
                    int result = await viewModel.SaveGridValueChanges(parsedValue, gridLine.Id, GetMonthNumberFromName(e.Column.Header.ToString()), GridDatePicker.SelectedDate.Value.Year);
                    if (result == (int)ErrorEnum.Aborted)
                    {
                        Console.WriteLine("Grid-Value Save Problems!");
                    }
                }
            }
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

    private void OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.EditingElement is TextBox textBox)
        {
            textBox.TextInput += OnTextInputValidation;
        }
    }

    private async void GridViewDataGrid_OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        var viewModel = this.DataContext as GridViewModel;
        if (viewModel == null) return;
        if(e.Column.Header == null) return;
        if(e.Column.Header.ToString().Length <= 0) return;
        if(GridDatePicker == null) return;
        DateTime? currentDate = GridDatePicker.SelectedDate;

        if (e.Row?.DataContext is GridLine gridLine)
        {
            int selectedMonth = GetMonthNumberFromName(e.Column.Header.ToString());
            if (selectedMonth == 0)
            {
                viewModel.DefaultCommentSection();
                return;
            }
            var result = await viewModel.LoadCommentData(gridLine, selectedMonth, currentDate.Value.Year);
            switch (result)
            {
                case (int)ErrorEnum.NullReference:
                    MessageService.WarningMessageBoxClassic("Keine Daten für diese Zeile vorhanden!"+Environment.NewLine+"Import für den selektierten Monat durchführen", (int)ErrorEnum.NullReference);
                    break;
            }
        }
    }
    
    private void WindowAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if(sender == null || e == null)
            return;
        
        if (this.VisualRoot is Window window)
        {
            if(this.DataContext is GridViewModel viewModel)
                viewModel.CurrentWindow = window;
        }
    }
}