using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels.Import;

public partial class GridLineImportViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    public GridLineImportViewModel(string windowTitle, AppService appService)
    {
        WindowTitle = windowTitle;
        AppService = appService;
        ImportMessage = "";
        SetDefaultDateForDatePicker();

        ImportCmd = new RelayCommand<Window>(async (mainWindow) => await ImportSelectedGridData(mainWindow));
    }

    public GridLineImportViewModel()
    {
        ImportMessage = "Werte für Monat () werden importiert!";
        IsProgressBarActive = true;
        SetDefaultDateForDatePicker();
    }
    
    private void SetDefaultDateForDatePicker()
    {
        DateTime currentDate = DateTime.Now;
        DisplayDateStart = new DateTime(currentDate.Year, 1, 1);
        DisplayDateEnd = new DateTime(currentDate.Year, 12, 31);
        SelectedDate = DateTime.Today;
    }


    private async Task ImportSelectedGridData(Window mainWindow)
    {
        if (SelectedDate == null)
        {
            MessageService.WarningMessageBoxClassic("Es wurde kein Zeitraum angegeben!", (int)ErrorEnum.NotValidInput);
            return;
        }
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.Session.LineCalculations == null)
            return;
        
        IsProgressBarActive = true;
        ImportMessage = $"Werte für Monat {SelectedDate.Month}/{SelectedDate.Year} werden importiert!";
        CancellationToken cancellationToken = new CancellationToken();
        int importResult = 0;
        await Task.Run(async () =>
        {
            importResult = await MBMiscService.CheckIfMonthAlreadyImported(cancellationToken, SelectedDate.Month,
                SelectedDate.Year, AppService.User.Id);
        }, cancellationToken);
        switch (importResult)
        {
            case (int)ErrorEnum.AlreadyImported:
                var answer =
                    await MessageService.AnswerMessageBoxClassic("Für den Monat wurde bereits ein Import durchgeführt!");
                if (!answer)
                    return;
                break;
            case (int)ErrorEnum.NoImportFound:
                break;
        }
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBMiscService.ImportValuesForSpecificMonthAsync(cancellationToken, SelectedDate.Month, SelectedDate.Year, AppService.User.Id);

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
                AppService.Session.GridLines = await MBGridService.RefreshGridLinesAfterImportForSpecificMonth(cancellationToken, AppService.Session.GridLines, SelectedDate.Month, SelectedDate.Year, AppService.User.Id);
                MessageService.NormalMessageBoxClassic("Import wurde durchgeführt!");
                mainWindow.Close(SelectedDate);
                return;
        }
        IsProgressBarActive = false;
        ImportMessage = string.Empty;
    }

    [ObservableProperty] private DateTime _selectedDate;
    [ObservableProperty] private DateTime _displayDateStart;
    [ObservableProperty] private DateTime _displayDateEnd;
    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private string _importMessage;

    [ObservableProperty] private bool _isProgressBarActive;
    [ObservableProperty] private ICommand _importCmd;
}