using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using MoneyBear.ViewModels;

namespace MoneyBear.Views;

public partial class LoginView : Window
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void EnterPasswordEvent(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var viewModel = DataContext as LoginViewModel;
            await viewModel.Login(viewModel.SessionStatus);
            viewModel.IsLoginAllowed = false;
        }
    }
    
}