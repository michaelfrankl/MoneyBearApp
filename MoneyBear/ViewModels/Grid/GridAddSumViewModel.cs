using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBear.DesignData;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels;

public partial class GridAddSumViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public GridAddSumViewModel(AppService appService, string windowTitle, GridLine currentGridLine, string currentCalculation)
    {
        AppService = appService;
        WindowTitle = windowTitle;
        CurrentGridLine = currentGridLine;
        DateOnly currentDate = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        PreviewGridData = new ObservableCollection<GridLine>(AppService.Session.GridLines.
            Where(x => x.ValidFrom <= currentDate && x.ValidTo >= currentDate && x.Userid == AppService.User.Id).OrderBy(x => x.Number));
        TempGridData = new ObservableCollection<GridLine>(PreviewGridData.Select(x => x.Clone()));
        Categories = new ObservableCollection<LineCategory>(AppService.Session.LineCategories.Select(x => x.Clone()));
        SearchText = string.Empty;
        SetDefaultLineCalculation(currentCalculation);
        SetCategoryDefaultIndex();
        SetDefaultDataForBuchungsFilterCB();
        IsTaskActive = false;
        
        ReturnSumLineCmd =
            new RelayCommand<Window>(async (mainWindow) => await ReturnNewSumToNewGridLine(mainWindow));

    }

    public GridAddSumViewModel()
    {
        WindowTitle = "Grid-Zeile Summenbildung";
        SampleGridData sampleGridData = new SampleGridData();
        PreviewGridData = sampleGridData.GetSampleGridData();
        TempGridData = PreviewGridData;
        SearchText = string.Empty;
        Categories = sampleGridData.GetSampleLineCategories();
        SelectedGridLine = PreviewGridData[0];
        NewGridLineSumCalculation = string.Empty;
        IsTaskActive = true;
        SetDefaultDataForBuchungsFilterCB();
    }
    
    private void SetDefaultDataForBuchungsFilterCB()
    {
        BuchungsFilterGridItemsCB = new ObservableCollection<string>()
        {
            "Kein Filter",
            "Bis 15. (Mitte)",
            "15. bis 31",
        };
        SelectedBuchungsFilterItemIndex = 0;
    }
    
    partial void OnSelectedBuchungsFilterItemChanged(string buchungsFilter)
    {
        if(buchungsFilter == null)
            return;
        
        if(IsDesignView)
            return;
        
        int filter = buchungsFilter switch
        {
            "Kein Filter" => 0,
            "Bis 15. (Mitte)" => 1,
            "15. bis 31" => 2,
        };
        FilterGridDataWithBuchungsFilter(filter);
    }

    private void FilterGridDataWithBuchungsFilter(int filter)
    {
        if(filter == null)
            return;
        
        if(IsDesignView)
            return;
        
        switch (filter)
        {
            case 0:
                PreviewGridData = TempGridData;
                break;
            case 1:
                PreviewGridData = new ObservableCollection<GridLine>(TempGridData.Where(x => x.ValidFrom.Day <= 15).Select(x => x.Clone()));
                break;
            case 2:
                PreviewGridData = new ObservableCollection<GridLine>(TempGridData.Where(x => x.ValidFrom.Day >= 15).Select(x => x.Clone()));
                break;
        }
    }

    private void SetDefaultLineCalculation(string currentCalculation)
    {
        if (currentCalculation == null)
        {
            NewGridLineSumCalculation = string.Empty;
            return;
        }
        NewGridLineSumCalculation = currentCalculation;
    }

    partial void OnSearchTextChanged(string text)
    {
        if (text == null)
            return;
        if (text.Length <= 0)
        {
            PreviewGridData = TempGridData;
            return;
        }
        PreviewGridData = new ObservableCollection<GridLine>(TempGridData.Where(x => x.Name.ToLower().Contains(text.ToLower())));
    }

    private void SetCategoryDefaultIndex()
    {
        LineCategory category = new LineCategory()
        {
            Id = "Kein Filter",
        };
        if(Categories == null) return;
        Categories.Add(category);
        SelectedGridLineCategoryIndex = Categories.IndexOf(category);
        SelectedGridLineCategory = Categories.First(x => x.Id == category.Id);
    }

    partial void OnSelectedGridLineCategoryChanged(LineCategory category)
    {
        if (category == null)
            return;
        
        DateOnly currentDate = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        switch (category.Id)
        {
            case "Kein Filter":
                PreviewGridData = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.ValidFrom <= currentDate && x.ValidTo >= currentDate && x.Userid == AppService.User.Id).OrderBy(x => x.Number));
                return;
        }
        PreviewGridData = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.ValidFrom <= currentDate && x.ValidTo >= currentDate && x.LineCategoryId == category.Id && x.Userid == AppService.User.Id).OrderBy(x => x.Number));
    }

    private async Task ReturnNewSumToNewGridLine(Window mainWindow)
    {
        IsTaskActive = true;
        CancellationToken cancellationToken = new CancellationToken();
        int newId = 0;
        await Task.Run(async () =>
        {
            newId = await MBGridService.GetAvailableIdForNewCalculationAsync(cancellationToken);

        }, cancellationToken);
        
        if (newId == (int)ErrorEnum.Aborted)
        {
            MessageService.WarningMessageBoxClassic("Fehler beim Laden der Berechnungen aus der Datenbank",(int)ErrorEnum.NullReference);
            return;
        }
        if (NewGridLineSumCalculation[NewGridLineSumCalculation.Length - 1] == '+' ||
            NewGridLineSumCalculation[NewGridLineSumCalculation.Length - 1] == '-')
        {
            string resultString = NewGridLineSumCalculation[..^1];
            NewGridLineSumCalculation = resultString;
        }
            
        LineCalculation newLineCalculation = new LineCalculation()
        {
            Id = newId,
            Calculation = NewGridLineSumCalculation,
        };
        mainWindow.Close(newLineCalculation);
    }
    
    
    public bool AddLineToSumCalculation(GridLine line)
    {
        if(line == null)
            return false;
        if(NewGridLineSumCalculation == null)
            return false;
        if (NewGridLineSumCalculation.Length == 0)
        {
            NewGridLineSumCalculation += line.Id;
            return true;
        }

        if (NewGridLineSumCalculation[NewGridLineSumCalculation.Length - 1] == '+' ||
            NewGridLineSumCalculation[NewGridLineSumCalculation.Length - 1] == '-')
        {
            NewGridLineSumCalculation += line.Id;
            return true;
        }
        return false;
    }

    public bool AddPlusOperatorToSumCalculation()
    {
        if(NewGridLineSumCalculation == null)
            return false;
        if(NewGridLineSumCalculation.Length < 1)
            return false;
        if (NewGridLineSumCalculation[NewGridLineSumCalculation.Length - 1] != '+' || NewGridLineSumCalculation[NewGridLineSumCalculation.Length - 1] != '-')
        {
            NewGridLineSumCalculation += "+";
            return true;
        }
        return false;
    }

    public bool AddMinusOperatorToSumCalculation()
    {
        if(NewGridLineSumCalculation == null)
            return false;
        if(NewGridLineSumCalculation.Length < 1)
            return false;
        if (NewGridLineSumCalculation[NewGridLineSumCalculation.Length - 1] != '+' || NewGridLineSumCalculation[NewGridLineSumCalculation.Length - 1] != '-')
        {
            NewGridLineSumCalculation += "-";
            return true;
        }

        return false;
    }

    public bool DeleteLastPartOfSumCalculation()
    {
        if(NewGridLineSumCalculation == null)
            return false;
        if(NewGridLineSumCalculation.Length < 1)
            return false;
        string result = string.Empty;
        if (NewGridLineSumCalculation.Length - 1 == 0)
        {
            result = NewGridLineSumCalculation[..^1];
            NewGridLineSumCalculation = result;
            return true;
        }
        bool cleared = false;
        int counter = 0;
        
        do
        {
            if (NewGridLineSumCalculation.Length - 1 < 0)
            {
                cleared = true;
                break;
            }
            if (NewGridLineSumCalculation[NewGridLineSumCalculation.Length-1] == '+' || NewGridLineSumCalculation[NewGridLineSumCalculation.Length-1] == '-')
            {
                if (counter >= 1)
                    result = NewGridLineSumCalculation;
                else
                    result = NewGridLineSumCalculation[..^1];
                cleared = true;
                break;
            }
            result = NewGridLineSumCalculation[..^1];
            NewGridLineSumCalculation = result;
            counter++;
            cleared = false;

        } while (cleared == false);
        NewGridLineSumCalculation = result;
        return cleared;
    }
    
    
    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private ObservableCollection<GridLine> _previewGridData;
    [ObservableProperty] private ObservableCollection<GridLine> _tempGridData;
    [ObservableProperty] private GridLine _currentGridLine;
    [ObservableProperty] private GridLine _selectedGridLine;
    [ObservableProperty] private string _newGridLineSumCalculation;
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private ObservableCollection<LineCategory> _categories;
    [ObservableProperty] private ObservableCollection<string> _buchungsFilterGridItemsCB;
    [ObservableProperty] private string _selectedBuchungsFilterItem;
    [ObservableProperty] private int _selectedBuchungsFilterItemIndex;
    [ObservableProperty] private LineCategory _selectedGridLineCategory;
    [ObservableProperty] private int _selectedGridLineCategoryIndex;
    [ObservableProperty] private ICommand _addLineToSumCmd;
    [ObservableProperty] private ICommand _addPlusOperatorCmd;
    [ObservableProperty] private ICommand _addMinusOperatorCmd;
    [ObservableProperty] private ICommand _deleteLineFromSumCmd;
    [ObservableProperty] private ICommand _returnSumLineCmd;
    [ObservableProperty] private bool _isTaskActive;
    [ObservableProperty] private bool _isDesignView;
}