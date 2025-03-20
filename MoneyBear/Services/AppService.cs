using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.DataProtection;
using MoneyBear.Assets;
using MoneyBear.ViewModels;
using MySqlX.XDevAPI;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;
using Org.BouncyCastle.Crypto.Digests;

namespace MoneyBear.Services;

public partial class AppService : ViewModelBase
{
    private static AppService AppServiceInstance;
    
    public AppService()
    {
        if (AppServiceInstance == null)
        {
            AppName = "MoneyBear";
            AppVersion = "1.0";
            AppServiceInstance = this;
            MBUser user = new MBUser()
            {
                Id = 1,
                Name = "michael",
                Email = "test@gmail.com",
                Password = "123456",
                Role = "Administrator",
                IsAdministrator = true
            };
            AppServiceInstance.User = user;
        }
        TaskInfo = "";
        IsTaskInfoVisible = false;
        IsTaskProgressRingVisible = false;
        MainWindow = new Window();
    }

    public AppService(IDataProtector protector)
    {
        Protector = protector;
        if (AppServiceInstance == null)
        {
            AppName = "MoneyBear";
            AppVersion = "1.0";
            AppServiceInstance = this;
        }
        TaskInfo = "";
        IsTaskInfoVisible = false;
        IsTaskProgressRingVisible = false;
        MainWindow = new Window();
    }

    private async Task InitAppService()
    {
        CancellationToken cancellationToken = new CancellationToken();
        Session = await MBSessionService.CreateMBSessionContext(cancellationToken);
    }

    public static AppService Instance
    {
        get
        {
            if(AppServiceInstance == null)
                AppServiceInstance = new AppService();
            return AppServiceInstance;
        }
    }

    public void UserChange(MBUser newUser)
    {
        AppServiceInstance.User = newUser;
        AppServiceInstance.IsAdmin = false;
        AppServiceInstance.IsTester = false;
        if (newUser.Role == "Administrator")
            AppServiceInstance.IsAdmin = true;
    }

    public void SetTaskProgressInfo(string taskText)
    {
        TaskInfo = taskText;
        IsTaskInfoVisible = true;
        IsTaskProgressRingVisible = true;
    }

    public void DisableTaskProgressInfo()
    {
        TaskInfo = "";
        IsTaskInfoVisible = false;
        IsTaskProgressRingVisible = false;
    }

    public async Task RefreshSessionData()
    {
        CancellationToken cancellationToken = new CancellationToken();
        (AppServiceInstance.Session.GridLines, AppServiceInstance.Session.GridStorages) =
            await MBSessionService.RefreshGridDataAsync(cancellationToken);
    }

    [ObservableProperty] private MBSessionContext _session;
    [ObservableProperty] private string _appName;
    [ObservableProperty] private string _appVersion;
    [ObservableProperty] private MBUser _user;
    [ObservableProperty] private bool _isAdmin;
    [ObservableProperty] private bool _isTester;
    [ObservableProperty] private IDataProtector _protector;
    [ObservableProperty] private int _sessionStatus;
    [ObservableProperty] private string _taskInfo;
    [ObservableProperty] private bool _isTaskInfoVisible;
    [ObservableProperty] private bool _isTaskProgressRingVisible;
    [ObservableProperty] private static Window _mainWindow;
}