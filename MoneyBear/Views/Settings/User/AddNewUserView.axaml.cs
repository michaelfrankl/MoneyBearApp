using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MoneyBear.Views.Settings.User;

public partial class AddNewUserView : Window
{
    public AddNewUserView()
    {
        InitializeComponent();
    }

    private void EmailTxtBoxChanged(object? sender, TextChangedEventArgs e)
    {
        var txtBox = sender as TextBox;
        string emailString = txtBox.Text;

        if (string.IsNullOrEmpty(emailString))
            return;

        if (CheckValidEmail(emailString))
        {
            txtBox.Foreground = Avalonia.Media.Brushes.Green;
        }
        else
        {
            txtBox.Foreground = Avalonia.Media.Brushes.Red;
        }
    }

    private bool CheckValidEmail(string email)
    {
        string validMuster = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, validMuster, RegexOptions.IgnoreCase);
    }
}