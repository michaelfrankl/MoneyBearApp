using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using MoneyBear.ViewModels;

namespace MoneyBear.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void NavViewControl_OnSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.IsSettingsSelected)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            if(viewModel == null) return;
            
            
            viewModel.CurrentView = new SettingView()
            {
                DataContext = new SettingViewModel(viewModel.AppService)
            };
        }
    }
}