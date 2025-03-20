using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using MoneyBear.ViewModels.Home;

namespace MoneyBear.Views.Home;

public partial class HomeChartColorEditorView : Window
{
    public HomeChartColorEditorView()
    {
        InitializeComponent();
    }

    private void ColorPickerButton_OnColorChanged(ColorPickerButton sender, ColorButtonColorChangedEventArgs args)
    {
        var colorPicker = sender as ColorPickerButton;
        var viewModel = this.DataContext as HomeChartColorEditorViewModel;
        if(viewModel == null)
            return;
        if (colorPicker.Color != null)
        {
            viewModel.ColorPickerChanged(colorPicker.Color);
        }
    }
}