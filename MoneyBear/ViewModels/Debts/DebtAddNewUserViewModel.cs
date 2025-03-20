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

namespace MoneyBear.ViewModels;

public partial class DebtAddNewUserViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public DebtAddNewUserViewModel(AppService appService, string windowTitle)
    {
        AppService = appService;
        WindowTitle = windowTitle;

        EditingAllowed = true;

        SaveChangesCmd = new RelayCommand<Window>( mainWindow => AddNewDebtUser(mainWindow));
        CancelChangesCmd = new RelayCommand<Window>(mainWindow => CancelChanges(mainWindow));
    }

    public DebtAddNewUserViewModel()
    {
        WindowTitle = string.Empty;
        NewUserFirstName = string.Empty;
        NewUserLastName = string.Empty;
        EditingAllowed = true;
    }

    private async Task AddNewDebtUser(Window mainWindow)
    {
        EditingAllowed = false;
        if (String.IsNullOrWhiteSpace(NewUserFirstName) || String.IsNullOrWhiteSpace(NewUserLastName))
        {
            MessageService.NormalMessageBoxClassic("Bitte geben Sie einen Vor- und Nachnamen ein!");
            EditingAllowed = true;
            return;
        }
        CancellationToken cancellationToken = new CancellationToken();

        string debtName = $"{NewUserFirstName+" "+NewUserLastName}";
        var result = await MBMiscService.AddNewDebtUserAsync(cancellationToken, debtName, AppService.User.Id);
        switch (result)
        {
            case (int)ErrorEnum.Success:
                MBDebtUser newUser = new MBDebtUser(debtName, AppService.User.Id);
                mainWindow.Close(newUser);
                return;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Beim Speichern in der Datenbank ist ein Fehler aufgetreten.", (int)ErrorEnum.Aborted);
                break;
            case (int)ErrorEnum.AlreadyExists:
                MessageService.NormalMessageBoxClassic("Der jeweilige Nutzer existiert bereits!");
                break;
        }
        EditingAllowed = true;
    }

    private void CancelChanges(Window mainWindow)
        => mainWindow.Close(null);
    
    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private string _newUserFirstName;
    [ObservableProperty] private string _newUserLastName;

    [ObservableProperty] private bool _editingAllowed;
    
    [ObservableProperty] private ICommand _saveChangesCmd;
    [ObservableProperty] private ICommand _cancelChangesCmd;
}