using System;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.Core;
using MoneyBear.Services;
using MoneyBear.ViewModels.Import;
using MoneyBear.ViewModels.Settings.User;
using MoneyBear.Views;
using MoneyBear.Views.Grid;
using MoneyBear.Views.Import;
using MoneyBear.Views.Settings.User;
using NASPLOID.Models;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels;

public partial class SettingViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public SettingViewModel(AppService appService)
    {
        AppService = appService;
        CurrentIPv4Address = GetPublicIPvAddress();
        GetLastImportInformationAsync();
        ChangeIconWithIpAddressChange();
        
        LogoutCmd = new RelayCommand<object>(async _ => await Logout());
        OpenAddImportValuesView = new RelayCommand<object>(_ => OpenGridAddImportView());
        OpenAddUserCmd = new RelayCommand(OpenAddUserView);
        OpenEditUserCmd = new RelayCommand(OpenEditUserView);
        HideIpAddressCmd = new RelayCommand(ChangeIconWithIpAddressChange);
    }

    public SettingViewModel()
    {
        AppService = new AppService();
        AppService.User = new MBUser()
        {
            Id = 1,
            Name = "michael",
            Email = "michael@gmail.com",
            Role = "Admin",
            IsAdministrator = true,
        };
        AppService.AppVersion = "1.0";
        AppService.AppName = "MoneyBear";
        AppService.IsAdmin = true;
        CurrentIPv4Address = "999.999.999.999";
        LatestAutoImport = "Nein";
        ChangeIconWithIpAddressChange();
    }
    
    private void ChangeIconWithIpAddressChange()
    {
        if (IsIpAdressHidden)
        {
            HiddenIcon = "fa-solid fa-eye-slash";
            HiddenChar = "";
            IsIpAdressHidden = false;
        }
        else
        {
            HiddenIcon = "fa-solid fa-eye";
            HiddenChar = "*";
            IsIpAdressHidden = true;
        }
    }

    private void OpenAddUserView()
    {
        Window currentWindow = null;
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new AddNewUserView()
            {
                DataContext = new AddNewUserViewModel(AppService)
            };
            desktop.MainWindow.Show();
        }
    }

    private void OpenEditUserView()
    {
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new EditUserView()
            {
                DataContext = new EditUserViewModel(AppService)
            };
            desktop.MainWindow.Show();
        }
    }

    private void OpenGridAddImportView()
    {
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new GridAddImportValuesView()
            {
                DataContext = new GridAddImportValuesViewModel(AppService)
            };
            desktop.MainWindow.Show();
        };
    }
    
    private async Task Logout()
    {
        var result = await MessageService.AnswerMessageBoxClassic("Aktuellen Benutzer abmelden!");
        if (result)
        {
        
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var loginWindow = new LoginView()
                {
                    DataContext = new LoginViewModel(AppService)
                };
                
                desktop.MainWindow = loginWindow;
                loginWindow.Show();
                AppService.MainWindow.Close();
            };
        }
    }

    private async Task GetLastImportInformationAsync()
    {
        CancellationToken cancellationToken = new CancellationToken();
        var result = await MBMiscService.LoadLatestGridAutoImportAsync(cancellationToken, DateTime.Now.Month,
            DateTime.Now.Year, AppService.User.Id);
        if (result == null)
        {
            LatestAutoImport = "Nein";
            return;
        }

        var resultSorted = result.OrderByDescending(x => x.LastImport);
        LatestAutoImport = resultSorted.First().LastImport.ToString()?? resultSorted.First().Month.ToString() +"."+ resultSorted.First().Year.ToString();
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

    [ObservableProperty] private string _loggedUserName;
    [ObservableProperty] private string _currentIPv4Address;
    [ObservableProperty] private string _latestAutoImport;
    [ObservableProperty] private string _hiddenChar;
    [ObservableProperty] private string _hiddenIcon;
    [ObservableProperty] private bool _isIpAdressHidden;
    [ObservableProperty] private ICommand _logoutCmd;
    [ObservableProperty] private ICommand _openAddImportValuesView;
    [ObservableProperty] private ICommand _openAddUserCmd;
    [ObservableProperty] private ICommand _openEditUserCmd;
    [ObservableProperty] private ICommand _hideIpAddressCmd;
}