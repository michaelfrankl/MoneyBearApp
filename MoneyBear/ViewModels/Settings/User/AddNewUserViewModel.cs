using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.AspNetCore.DataProtection;
using MoneyBear.DesignData;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels.Settings.User;

public partial class AddNewUserViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public AddNewUserViewModel(AppService appService)
    {
        AppService = appService;
        InitUI();
        AddRoles();
        
        PasswordIconCmd = new RelayCommand(ChangePasswordIconAndDisplay);
        AddNewUserCmd = new RelayCommand<Window>(async (mainWindow) => await AddNewUser(mainWindow));
    }

    public AddNewUserViewModel()
    {
        NewUserName = "Martin";
        NewUserEmail = "martin@gmail.com";
        NewUserPassword = "123456";
        SampleUserData sampleUserData = new SampleUserData();
        Roles = sampleUserData.GetSampleRoles();
        PasswordChars = "*";
        NewUserRole = Roles[0];
        PasswordIcon = "fa-solid fa-eye";
        IsPasswordHidden = true;
        PasswordIconCmd = new RelayCommand(ChangePasswordIconAndDisplay);
        IsTaskActive = true;
    }

    private void InitUI()
    {
        if(AppService.Session.Roles == null)
            return;
        
        NewUserName = string.Empty;
        NewUserEmail = string.Empty;
        NewUserPassword = string.Empty;
        NewUserRole = AppService.Session.Roles[0];
        PasswordChars = "*";
        PasswordIcon = "fa-solid fa-eye";
        IsPasswordHidden = true;
        IsTaskActive = false;
    }

    
    private void ChangePasswordIconAndDisplay()
    {
        if (IsPasswordHidden)
        {
            PasswordIcon = "fa-solid fa-eye-slash";
            PasswordChars = "";
            IsPasswordHidden = false;
        }
        else
        {
            PasswordIcon = "fa-solid fa-eye";
            PasswordChars = "*";
            IsPasswordHidden = true;
        }
    }

    private void AddRoles()
    {
        if(AppService.Session.Roles == null)
            return;

        Roles = new ObservableCollection<MBRole>();
        
        foreach (MBRole role in AppService.Session.Roles)
        {
            Roles.Add(role.Clone());
        }
    }

    private async Task AddNewUser(Window mainWindow)
    {
        CancellationToken cancellationToken = new CancellationToken();
        IsTaskActive = true;
        if(!CheckInput())
            return;
        
        MBUser newUser = new MBUser()
        {
            Id = 0,
            Name = NewUserName,
            Email = NewUserEmail,
            Password = Program.Protector.Protect(NewUserPassword),
            Role = NewUserRole.RoleId,
        };

        int result = 0;

        await Task.Run(async () =>
        {
            result = await MBUSerService.AddUser(cancellationToken, newUser);
        }, cancellationToken);

        switch (result)
        {
            case (int)ErrorEnum.Success:
                IsTaskActive = false;
                mainWindow.Close();
                return;
            
            case (int)ErrorEnum.AlreadyExists:
                break;
            
            case (int)ErrorEnum.Aborted:
                break;
        }
        IsTaskActive = false;
    }

    private bool CheckInput()
    {
        if (string.IsNullOrWhiteSpace(NewUserName))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie einen Benutzernamen ein!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (string.IsNullOrWhiteSpace(NewUserEmail))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie eine Email-Adresse ein!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (!CheckValidEmail(NewUserEmail))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie eine gültige Email-Adresse ein!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (string.IsNullOrWhiteSpace(NewUserPassword))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie ein Passwort ein!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (NewUserRole == null)
        {
            MessageService.WarningMessageBoxClassic("Bitte wählen Sie eine Rolle aus!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        return true;
    }
    
    private bool CheckValidEmail(string email)
    {
        string validMuster = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, validMuster, RegexOptions.IgnoreCase);
    }

    [ObservableProperty] private string _newUserName;
    [ObservableProperty] private string _newUserPassword;
    [ObservableProperty] private string _passwordChars;
    [ObservableProperty] private string _passwordIcon;
    [ObservableProperty] private bool _isPasswordHidden;
    [ObservableProperty] private string _newUserEmail;
    [ObservableProperty] private MBRole _newUserRole;
    [ObservableProperty] private ObservableCollection<MBRole> _roles;
    [ObservableProperty] private bool _designView;
    [ObservableProperty] private bool _isTaskActive;
    [ObservableProperty] private ICommand _passwordIconCmd;
    [ObservableProperty] private ICommand _addNewUserCmd;
}