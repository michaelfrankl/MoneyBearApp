using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBear.DesignData;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels;


public partial class GridAddCategoryViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public GridAddCategoryViewModel(AppService appService, string windowTitle)
    {
        AppService = appService;
        WindowTitle = windowTitle;
        IsTaskActive = false;
        SearchText = string.Empty;
        Categories = AppService.Session.LineCategories;

        AddCategoryCommand = new RelayCommand<Window>(async (mainWindow) => await ReturnNewCategory(mainWindow));
    }
    
    private async Task ReturnNewCategory(Window mainWindow)
    {
        if(AppService == null)
            return;
        if(AppService.Session.LineCategories == null)
            return;

        IsTaskActive = true;
        
        if (String.IsNullOrEmpty(NewCategoryName))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie eine Kategorie ein!", (int)ErrorEnum.NotValidInput);
            IsTaskActive = false;
            return;
        }

        LineCategory newLineCategory = new LineCategory()
        {
            Id = NewCategoryName
        };
        
        CancellationToken cancellationToken = new CancellationToken();
        int result = 0;

        await Task.Run(async () =>
        {
            result = await MBGridService.AddNewLineCategoryAsync(cancellationToken, newLineCategory);
        }, cancellationToken);

        switch (result)
        {
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Ein Fehler ist aufgetreten!", (int)ErrorEnum.Aborted);
                IsTaskActive = false;
                return;
            case (int)ErrorEnum.AlreadyExists:
                MessageService.WarningMessageBoxClassic("Diese Kategorie existiert schon!", (int)ErrorEnum.AlreadyExists);
                IsTaskActive = false;
                return;
            case (int)ErrorEnum.NullReference:
                IsTaskActive = false;
                return;
            case (int)ErrorEnum.Success:
                AppService.Session.LineCategories.Add(newLineCategory);
                mainWindow.Close(newLineCategory);
                return;
        }

    }
    
    public GridAddCategoryViewModel()
    {
        WindowTitle = "Grid-Zeilen Kategorie hinzufügen";
        IsTaskActive = true;
        SampleGridData sampleGridData = new SampleGridData();
        Categories = sampleGridData.GetSampleLineCategories();
        SearchText = string.Empty;
    }

    partial void OnSearchTextChanged(string text)
    {
        if(AppService == null)
            return;

        if (String.IsNullOrEmpty(text))
        {
            Categories = AppService.Session.LineCategories;
            return;
        }
        Categories = new ObservableCollection<LineCategory>(AppService.Session.LineCategories.Where(x => x.Id.ToLower().Contains(text.ToLower())));
    }

    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private string _newCategoryName;
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private ObservableCollection<LineCategory> _categories;
    [ObservableProperty] private ICommand _addCategoryCommand;
    [ObservableProperty] private bool _isTaskActive;
}