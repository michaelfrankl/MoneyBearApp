using CommunityToolkit.Mvvm.ComponentModel;
using MoneyBear.Services;

namespace MoneyBear.ViewModels.Debts.DebtsEditView;

public partial class DebtsEditViewImageViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public DebtsEditViewImageViewModel(AppService appService)
    {
        AppService = appService;
    }

    public DebtsEditViewImageViewModel()
    {
        
    }
}