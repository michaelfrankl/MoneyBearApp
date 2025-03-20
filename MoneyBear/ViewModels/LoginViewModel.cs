using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using MoneyBear.Assets;
using MoneyBear.Models;
using MoneyBear.Services;
using MoneyBear.ViewModels.Import;
using MoneyBear.Views;
using MoneyBear.Views.Import;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;

    public LoginViewModel()
    {
        AppService = new AppService(Program.Protector);
        EnvironmentMachineName = Environment.MachineName;
        CurrentIPv4Address = "0.0.0.0";
        InitLoginUserCB();
        PasswordChar = "*";
        PasswordIcon = "fa-solid fa-eye";
        IsPasswordHidden = true;
        LoginErrorMessage = string.Empty;
        LoginErrorVisible = false;
        LoginProgressBarVisible = false;
        //CurrentIPv4Address = GetPublicIPvAddress();
        IsLoginAllowed = true;
        IsLoginNameActive = true;
        IsLoginPasswordActive = true;
        SelectedLoginName = string.Empty;
        SelectedLoginPassword = string.Empty;
        SessionStatus = 0;
        LoginCmd = new RelayCommand<object>(async _ => await Login(0));
        PasswordIconCmd = new RelayCommand<object>(_ => ChangePasswordIconAndDisplay());
    }

    public LoginViewModel(AppService appService)
    {
        AppService = appService;
        AppService.SessionStatus = 1;
        EnvironmentMachineName = Environment.MachineName;
        CurrentIPv4Address = "127.0.0.1";
        InitLoginUserCB();
        PasswordChar = "*";
        PasswordIcon = "fa-solid fa-eye";
        IsPasswordHidden = true;
        LoginErrorMessage = string.Empty;
        LoginErrorVisible = false;
        LoginProgressBarVisible = false;
        CurrentIPv4Address = GetPublicIPvAddress();
        IsLoginAllowed = true;
        IsLoginNameActive = true;
        IsLoginPasswordActive = true;
        SelectedLoginName = string.Empty;
        SelectedLoginPassword = string.Empty;
        SessionStatus = 1;
        LoginCmd = new RelayCommand<object>(async _ => await Login(AppService.SessionStatus));
        PasswordIconCmd = new RelayCommand<object>(_ => ChangePasswordIconAndDisplay());
    }

    public void ChangePasswordIconAndDisplay()
    {
        if (IsPasswordHidden)
        {
            PasswordIcon = "fa-solid fa-eye-slash";
            PasswordChar = "";
            IsPasswordHidden = false;
        }
        else
        {
            PasswordIcon = "fa-solid fa-eye";
            PasswordChar = "*";
            IsPasswordHidden = true;
        }
    }


    private void InitLoginUserCB()
    {
        LoginNamesCBItems = new ObservableCollection<string>()
        {
            new string("michael"),
            new string("Demo")
        };
        if (Environment.MachineName.StartsWith("M"))
            LoginNamesCBIndex = 0;
        else
            LoginNamesCBIndex = 1;
        IsLoginNameActive = true;
    }

    private string GetPublicIPvAddress()
    {
        try
        {
            string response = new WebClient().DownloadString("https://api.ipify.org?format=json");
            string address = JsonObject.Parse(response)["ip"].ToString();
            return address;
        }
        catch (Exception ex)
        {
            return "0.0.0.0";
        }
    }

    public async Task Login(int status)
    {
        IsLoginAllowed = false;
        IsLoginNameActive = false;
        IsLoginPasswordActive = false;
        LoginErrorMessage = string.Empty;
        LoginErrorVisible = false;
        LoginProgressBarVisible = true;
        CancellationToken cancellationToken = new CancellationToken();

        if (status == 0)
        {
            var loginTask = Task.Run(
                async () => await MBSessionService.CreateMBSessionContext(SelectedLoginName, SelectedLoginPassword,
                    AppService.Protector, cancellationToken).ConfigureAwait(false), cancellationToken);
            loginTask.ContinueWith(task => ProccessLoginResult(task.Result, cancellationToken), cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            loginTask.ContinueWith(_ => ProccessLoginFailure(), cancellationToken, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler
                .FromCurrentSynchronizationContext());
        }
        else if(status == 1)
        {
            var loginTask = Task.Run(
                async () => await MBSessionService.IsUserListedInDataBase(SelectedLoginName, SelectedLoginPassword, AppService.Protector, cancellationToken).ConfigureAwait(false), cancellationToken);
            loginTask.ContinueWith(task => ProcessRenewLoginResults(task.Result.Item1, task.Result.Item2, task.Result.Item3, cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            loginTask.ContinueWith(_ => ProccessLoginFailure(), cancellationToken, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
    }

    private async void ProcessRenewLoginResults(int loginResult, string loginText, MBUser user, CancellationToken cancellationToken)
    {
        switch (loginResult)
        {
            case (int)LoginResult.Success:
                AppService.UserChange(user);
                AppService.Session.User = AppService.User;
                await ProcessSuccessLogin();
                return;
            case (int) LoginResult.UserNotFound:
                AbortLogin(1);
                return;
            case (int)LoginResult.Credentials:
                AbortLogin(2);
                return;
            default:
                AbortLogin(5);
                return;
        }
    }

    private async void ProccessLoginResult(MBSessionContext taskResult, CancellationToken cancellationToken)
    {
        if (AppService == null)
        {
            AbortLogin(3);
            return;
        }
        if (taskResult == null)
        {
            AbortLogin(0);
            return;
        }

        AppService.Session = taskResult;
        AppService.User = taskResult.User;

        if (taskResult.LoginError != null)
        {
            if (taskResult.LoginError == MBResources.UserNotFound)
            {
                AbortLogin(1);
                return;
            }

            if (taskResult.LoginError == MBResources.Credentials)
            {
                AbortLogin(2);
                return;
            }
        }

        if (CurrentIPv4Address.IsNullOrEmpty())
        {
            AbortLogin(4);
            return;
        }
        
        AppService.IsAdmin = AppService.User.Role switch
        {
            "Administrator" => true,
            _ => false
        };
        AppService.IsTester = AppService.User.Role switch
        {
            "Tester" => true,
            _ => false
        };
        await ProcessSuccessLogin(taskResult);
    }

    private async Task ProcessSuccessLogin(MBSessionContext taskResult)
    {
        Window currentWindow = null;
        CancellationToken cancellationToken = new CancellationToken();
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBMiscService.CheckGridImport(cancellationToken, AppService.User.Id);

        }, cancellationToken);
        if (result == (int)ErrorEnum.AlreadyImported)
        {
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
        else if (result == (int)ErrorEnum.NoImportFound)
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                currentWindow = desktop.MainWindow;

                desktop.MainWindow = new GridAutoImportView()
                {
                    DataContext = new GridAutoImportViewModel(AppService),
                };
                desktop.MainWindow.Show();
            }
            currentWindow?.Close();
        }
    }

    private async Task ProcessSuccessLogin()
    {
        Window currentWindow = null;
        CancellationToken cancellationToken = new CancellationToken();
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBMiscService.CheckGridImport(cancellationToken, AppService.User.Id);

        }, cancellationToken);
        if (result == (int)ErrorEnum.AlreadyImported)
        {
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
        else if (result == (int)ErrorEnum.NoImportFound)
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                currentWindow = desktop.MainWindow;

                desktop.MainWindow = new GridAutoImportView()
                {
                    DataContext = new GridAutoImportViewModel(AppService),
                };
                desktop.MainWindow.Show();
            }
            currentWindow?.Close();
        }
    }

    private void ProccessLoginFailure()
    {
        AbortLogin(3);
    }

    private void AbortLogin(int errorCode)
    {
        LoginProgressBarVisible = false;
        LoginErrorVisible = true;

        switch (errorCode)
        {
            case 0:
                LoginErrorMessage = MBResources.DBError;
                break;
            case 1:
                LoginErrorMessage = MBResources.UserNotFound;
                break;
            case 2:
                LoginErrorMessage = MBResources.Credentials;
                break;
            case 3: 
                LoginErrorMessage = MBResources.NullReference;
                break;
            case 4:
                LoginErrorMessage = MBResources.Ethernet;
                break;
            default:
                LoginErrorMessage = MBResources.MiscError;
                break;
        }
        
        IsLoginNameActive = true;
        IsLoginPasswordActive = true;
        IsLoginAllowed = true;
    }

    [ObservableProperty] private string _environmentMachineName;
    [ObservableProperty] private string _currentIPv4Address;
    [ObservableProperty] private ObservableCollection<string> _loginNamesCBItems;
    [ObservableProperty] private int _loginNamesCBIndex;
    [ObservableProperty] private string _selectedLoginName;
    [ObservableProperty] private string _selectedLoginPassword;
    [ObservableProperty] private bool _isLoginNameActive;
    [ObservableProperty] private bool _isLoginPasswordActive;
    [ObservableProperty] private bool _isLoginAllowed;
    [ObservableProperty] private string _loginErrorMessage;
    [ObservableProperty] private bool _loginErrorVisible;
    [ObservableProperty] private bool _loginProgressBarVisible;
    [ObservableProperty] private ICommand _loginCmd;
    [ObservableProperty] private int _sessionStatus;
    [ObservableProperty] private string _passwordChar;
    [ObservableProperty] private bool _isPasswordHidden;
    [ObservableProperty] private string _passwordIcon;
    [ObservableProperty] private ICommand _passwordIconCmd;
}