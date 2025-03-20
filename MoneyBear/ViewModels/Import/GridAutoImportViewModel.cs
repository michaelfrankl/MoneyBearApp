using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBear.Services;
using MoneyBear.Views;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels.Import;

public partial class GridAutoImportViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public GridAutoImportViewModel(AppService appService)
    {
        AppService = appService;

        ImportDataCmd = new RelayCommand<object>(async _ => await ImportValuesAndOpenMainWindow());
        CancelImportCmd = new RelayCommand<object>(_ => CancelImportAndOpenMainWindow());
    }

    public GridAutoImportViewModel()
    {
        IsTaskActive = true;
    }

    private async Task ImportValuesAndOpenMainWindow()
    {
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.Session.LineCalculations == null)
            return;
        IsTaskActive = true;
        
        CancellationToken cancellationToken = new CancellationToken();
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBMiscService.ImportValuesForSpecificMonthAsync(cancellationToken, DateTime.Now.Month, DateTime.Now.Year, AppService.User.Id);
        }, cancellationToken);
        switch (result)
        {
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Vorgang konnte nicht durchgeführt werden!", (int)ErrorEnum.Aborted);
                break;
            case (int)ErrorEnum.NullReference:
                MessageService.WarningMessageBoxClassic("Es ist ein Fehler aufgetreten!", (int)ErrorEnum.NullReference);
                break;
            case (int)ErrorEnum.NoValidImport:
                MessageService.WarningMessageBoxClassic("Operation abgebrochen!", (int)ErrorEnum.NoValidImport);
                break;
            case (int)ErrorEnum.Success:
                AppService.Session.GridLines = await MBGridService.RefreshGridLinesAfterImportForSpecificMonth(cancellationToken, AppService.Session.GridLines, DateTime.Now.Month, DateTime.Now.Year, AppService.User.Id);
                int sumResult = await MBGridService.RecalculateSumLinesForSpecificMonthAsync(
                    DateTime.Now.Month, DateTime.Now.Year, AppService.User.Id, AppService.Session.GridLines, AppService.Session.LineCalculations, cancellationToken);
                switch (sumResult)
                {
                    case (int)ErrorEnum.Aborted:
                        MessageService.WarningMessageBoxClassic("Summenberechnung konnte nicht durchgeführt werden!", (int)ErrorEnum.Aborted);
                        break;
                }
                IsTaskActive = false;
                OpenMainWindow();
                return;
        }
        IsTaskActive = false;
    }

    private void CancelImportAndOpenMainWindow()
        => OpenMainWindow();

    private void OpenMainWindow()
    {
        Window currentWindow = null;
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            currentWindow = desktop.MainWindow;

            desktop.MainWindow = new MainWindow()
            {
                DataContext = new MainWindowViewModel(AppService),
            };
            desktop.MainWindow.Show();
            AppService.MainWindow = desktop.MainWindow;
        }
        currentWindow?.Close();
    }


    [ObservableProperty] private ICommand _importDataCmd;
    [ObservableProperty] private ICommand _cancelImportCmd;

    [ObservableProperty] private bool _isTaskActive;
}