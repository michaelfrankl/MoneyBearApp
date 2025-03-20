using CommunityToolkit.Mvvm.ComponentModel;
using MoneyBear.Services;

namespace MoneyBear.ViewModels;

public partial class DebtsImageViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;

    public DebtsImageViewModel(AppService appService, string windowTitle)
    {
        AppService = appService;
        WindowTitle = windowTitle;
    }

    public DebtsImageViewModel()
    {
        WindowTitle = "Eintrag bearbeiten";
    }
    
    
    [ObservableProperty] private string _windowTitle;
}