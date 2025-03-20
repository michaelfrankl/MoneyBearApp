using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MoneyBear.Services;
using MoneyBear.ViewModels;
using MoneyBear.Views;
using NASPLOID.Models.MoneyBear;

namespace MoneyBear;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            desktop.MainWindow = new LoginView()
            {
                DataContext = new LoginViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        if (exception != null)
        {
            // Logge den Fehler (Console, File, Logger, etc.)
            Console.WriteLine($"Ein unerwarteter Fehler ist aufgetreten: {exception.Message}");

            // Hier könntest du auch ein Fenster anzeigen, das den Benutzer informiert
            MessageService.WarningMessageBoxClassic("Ein unerwarteter Fehler ist aufgetreten!", (int)ErrorEnum.Aborted);
        }
    }
}