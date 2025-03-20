using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MoneyBear.ViewModels;
using NASPLOID.Models.MoneyBear;
using ZstdSharp.Unsafe;

namespace MoneyBear.Views.Grid;

public partial class GridAddSumView : Window
{
    public GridAddSumView()
    {
        InitializeComponent();
        DisableButtons();
    }

    private void DisableButtons()
    {
        var viewModel = DataContext as GridAddSumViewModel;
        if(viewModel == null)
            return;
        if (viewModel.NewGridLineSumCalculation.Length <= 0)
        {
            if(AddLineBtn != null)
                AddLineBtn.IsEnabled = true;
            if(AddPlusBtn != null)
                AddPlusBtn.IsEnabled = false;
            if(AddMinusBtn != null)
                AddMinusBtn.IsEnabled = false;
            if(DeleteLineBtn != null)
                DeleteLineBtn.IsEnabled = false;
        }
        else if (viewModel.NewGridLineSumCalculation.Length >= 3)
        {
            if (AddLineBtn != null)
                AddLineBtn.IsEnabled = false;
            if (AddPlusBtn != null)
                AddPlusBtn.IsEnabled = true;
            if (AddMinusBtn != null)
                AddMinusBtn.IsEnabled = true;
            if (DeleteLineBtn != null)
                DeleteLineBtn.IsEnabled = true;
        }
    }

    private void AddLineBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as GridAddSumViewModel;
        if(viewModel == null)
            return;
        if (ListBox != null)
        {
            GridLine line = ListBox.SelectedValue as GridLine;
            if (line != null)
            {
                if (viewModel.AddLineToSumCalculation(line))
                {
                    AddLineBtn.IsEnabled = false;
                    AddPlusBtn.IsEnabled = true;
                    AddMinusBtn.IsEnabled = true;
                    DeleteLineBtn.IsEnabled = true;
                }
            }
        }
    }

    private void AddPlusBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as GridAddSumViewModel;
        if(viewModel == null)
            return;
        if (viewModel.AddPlusOperatorToSumCalculation())
        {
            AddMinusBtn.IsEnabled = false;
            AddPlusBtn.IsEnabled = false;
            AddLineBtn.IsEnabled = true;
            DeleteLineBtn.IsEnabled = true;
        }
    }

    private void AddMinusBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as GridAddSumViewModel;
        if(viewModel == null)
            return;
        if (viewModel.AddMinusOperatorToSumCalculation())
        {
            AddMinusBtn.IsEnabled = false;
            AddPlusBtn.IsEnabled = false;
            AddLineBtn.IsEnabled = true;
            DeleteLineBtn.IsEnabled = true;
        }
        
    }

    private void DeleteLineBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as GridAddSumViewModel;
        if (viewModel == null)
            return;
        if(viewModel.NewGridLineSumCalculation == null)
            return;
        if(viewModel.NewGridLineSumCalculation.Length < 1)
            return;
        if (viewModel.DeleteLastPartOfSumCalculation())
        {
            if (viewModel.NewGridLineSumCalculation.Length <= 0)
            {
                AddLineBtn.IsEnabled = true;
                AddPlusBtn.IsEnabled = false;
                AddMinusBtn.IsEnabled = false;
                DeleteLineBtn.IsEnabled = false;
                return;
            }
            if (viewModel.NewGridLineSumCalculation[viewModel.NewGridLineSumCalculation.Length-1] == '+' ||
                viewModel.NewGridLineSumCalculation[viewModel.NewGridLineSumCalculation.Length-1] == '-')
            {
                AddLineBtn.IsEnabled = true;
                AddPlusBtn.IsEnabled = false;
                AddMinusBtn.IsEnabled = false;
            }
            else
            {
                AddLineBtn.IsEnabled = false;
                AddPlusBtn.IsEnabled = true;
                AddMinusBtn.IsEnabled = true;
            }
        }
    }

    private void TextBox_OnSumCalculationTextChanged(object? sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox != null)
        {
            var confirmBtn = ConfirmBtn;
            if (confirmBtn != null)
            {
                if (textBox.Text == null)
                {
                    confirmBtn.IsEnabled = false;
                    return;
                }
                if (textBox.Text.Length >= 3)
                {
                    confirmBtn.IsEnabled = true;
                    return;
                }
                confirmBtn.IsEnabled = false;
            }
        }
    }
}