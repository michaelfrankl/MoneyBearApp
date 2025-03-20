using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBear.DesignData;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels.Import;


public partial class GridAddImportValuesViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public GridAddImportValuesViewModel(AppService appService)
    {
        AppService = appService;
        SearchText = string.Empty;
        IsTaskActive = false;
        PrepareGrid();
        LoadGridDefaultData();

        CloseViewCmd = new RelayCommand<Window>(CloseImportView);
    }

    private async Task LoadGridDefaultData()
    {
        if(AppService == null)
            return;
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.Session.GridImportValues == null)
            return;
        
        DateTime currentDate = DateTime.Now;
        DateOnly dateOnly = new DateOnly(currentDate.Year, currentDate.Month, currentDate.Day);
        PreviewLines = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id && 
            x.ValidFrom <= dateOnly && x.ValidTo >= dateOnly && 
            x.LineTypId != (int)LineTypeEnum.Summen && x.LineTypId != (int)LineTypeEnum.Kontostand));
        TempLines = PreviewLines;

        foreach (GridLine gridLine in PreviewLines)
        {
            MBGridImportValues? importValue = AppService.Session.GridImportValues.FirstOrDefault(x => x.LineId == gridLine.Id && x.UserId == AppService.User.Id);
            if (importValue == null)
            {
                gridLine.JanuarValue = 0;
                continue;
            }
            gridLine.JanuarValue = importValue.Value;
        }
    }

    private void CloseImportView(Window mainWindow)
    {
        mainWindow.Close();
    }

    public GridAddImportValuesViewModel()
    {
        SearchText = string.Empty;
        IsTaskActive = false;
        SampleGridData sampleGridData = new SampleGridData();
        PreviewLines = sampleGridData.GetSampleGridData();
        Categories = sampleGridData.GetSampleLineCategories();
        Frequencies = sampleGridData.GetSampleLineFrequencies();
    }


    public async Task SaveDefaultGridValueAsync(GridLine gridLine)
    {
        IsTaskActive = true;
        CancellationToken cancellationToken = new CancellationToken();
        int newId = 0;
        await Task.Run(async () =>
        {
            newId = await MBMiscService.SetDefaultGridValuesAsync(cancellationToken, gridLine, AppService.User.Id);

        }, cancellationToken);

        if (newId == (int)ErrorEnum.Aborted)
        {
            MessageService.WarningMessageBoxClassic("Vorgang konnte nicht durchgeführt werden!", (int)ErrorEnum.Aborted);
            IsTaskActive = false;
            return;
        }
        IsTaskActive = false;
    }

    private void PrepareGrid()
    {
        if (AppService == null)
        {
            MessageService.WarningMessageBoxClassic("AppService wurde nicht gefunden!", (int)ErrorEnum.NullReference);
            return;
        }

        if (AppService.Session.GridLines == null)
        {
            MessageService.WarningMessageBoxClassic("Es wurden keine Zeilen in der Session gefunden!", (int)ErrorEnum.NullReference);
            return;
        }

        if (AppService.Session.LineCategories == null)
        {
            MessageService.WarningMessageBoxClassic("Es wurden keine Kategorien in der Session gefunden!", (int)ErrorEnum.NullReference);
            return;
        }
        if (AppService.Session.LineFrequencies == null)
        {
            MessageService.WarningMessageBoxClassic("Es wurden keine Abbuchungsintervalle in der Session gefunden!", (int)ErrorEnum.NullReference);
            return;
        }

        Categories = AppService.Session.LineCategories;
        var sumCat = Categories.FirstOrDefault(x => x.Id == "Summe");
        if(sumCat != null)
            Categories.Remove(Categories.First(x => x.Id == "Summe"));
        Frequencies = AppService.Session.LineFrequencies;
        
        DateOnly dateOnly = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        PreviewLines = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id && x.ValidFrom <= dateOnly && x.ValidTo >= dateOnly && 
            x.LineTypId != (int)LineTypeEnum.Summen && x.LineTypId != (int)LineTypeEnum.Kontostand));
        TempLines = PreviewLines;
        
        LineFrequency defaultFrequency = new LineFrequency()
        {
            Id = "Kein Filter",
            Frequency = 0
        };
        Frequencies.Add(defaultFrequency);
        SelectedLineFrequency = defaultFrequency;
        
        LineCategory defaultCategory =
            AppService.Session.LineCategories.FirstOrDefault(x => x.Id == "Kein Filter");
        if (defaultCategory == null)
        {
            LineCategory defaultCat = new LineCategory()
            {
                Id = "Kein Filter",
            };
            Categories.Add(defaultCat);
            SelectedLineCategory = defaultCat;
        }
        else
        {
            SelectedLineCategory = defaultCategory;
        }
    }

    partial void OnSearchTextChanged(string searchText)
    {
        if(AppService == null)
            return;
        if(PreviewLines == null || TempLines == null)
            return;
        
        if (String.IsNullOrEmpty(searchText))
        {
            DateOnly dateOnly = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            PreviewLines = new ObservableCollection<GridLine>(TempLines.Where(x => x.Userid == AppService.User.Id && x.ValidFrom <= dateOnly && x.ValidTo >= dateOnly && 
                x.LineTypId != (int)LineTypeEnum.Summen && x.LineTypId != (int)LineTypeEnum.Kontostand));
        }
        else
        {
            PreviewLines = new ObservableCollection<GridLine>(TempLines.Where(x => x.Name.ToLower().Contains(searchText.ToLower())));
        }
    }

    partial void OnSelectedLineCategoryChanged(LineCategory category)
    {
        if(AppService == null)
            return;
        if(PreviewLines == null || TempLines == null)
            return;
        
        if(category == null)
            return;
        
        if (category.Id == "Kein Filter")
        {
            PreviewLines = TempLines;
            return;
        }
        PreviewLines = new ObservableCollection<GridLine>(TempLines.Where(x => x.LineCategoryId.ToLower().Contains(category.Id.ToLower())));
    }

    partial void OnSelectedLineFrequencyChanged(LineFrequency frequency)
    {
        if(AppService == null)
            return;
        if(PreviewLines == null || TempLines == null)
            return;
        
        if(frequency == null)
            return;
        
        if (frequency.Id == "Kein Filter")
        {
            PreviewLines = TempLines;
            return;
        }
        PreviewLines = new ObservableCollection<GridLine>(TempLines.Where(x => x.FrequencyId.ToLower().Contains(frequency.Id.ToLower())));
    }


    [ObservableProperty] private ObservableCollection<GridLine> _previewLines;
    [ObservableProperty] private ObservableCollection<GridLine> _tempLines;
    [ObservableProperty] private ObservableCollection<LineCategory> _categories;
    [ObservableProperty] private int _selectedCategoryIndex;
    [ObservableProperty] private ObservableCollection<LineFrequency> _frequencies;
    [ObservableProperty] private int _selectedFrequencyIndex;
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private LineCategory _selectedLineCategory;
    [ObservableProperty] private LineFrequency _selectedLineFrequency;
    [ObservableProperty] private ICommand _closeViewCmd;
    [ObservableProperty] private bool _isTaskActive;
}