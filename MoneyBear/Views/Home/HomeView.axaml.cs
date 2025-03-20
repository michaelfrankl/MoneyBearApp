using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MoneyBear.ViewModels;

namespace MoneyBear.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += WindowAttachedToVisualTree;
    }
    
    private void WindowAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
    {
        if (this.VisualRoot is Window window)
        {
            if(this.DataContext is HomeViewModel viewModel)
                viewModel.CurrentWindow = window;
        }
    }
}