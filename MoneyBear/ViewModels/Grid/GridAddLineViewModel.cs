using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
using DynamicData;
using MoneyBear.DesignData;
using MoneyBear.Services;
using MoneyBear.Views.Grid;
using NASPLOID;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Utilities;

namespace MoneyBear.ViewModels;

public partial class GridAddLineViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;

    public GridAddLineViewModel(string windowTitle, AppService appService)
    {
        WindowTitle = windowTitle;
        AppService = appService;
        SetDefaultDateForDatePicker();
        EditingAllowed = true;
        DesignView = false;
        IsSumLineSelected = false;
        IsSpecialLineSelected = false;
        AddCategories();
        AddLineTypes();
        AddLineFrequencies();
        AddCurrentGridLines();
        GridLineNumberIncrement = 1;
        GridLineNumberMinimum = 1;
        AddExampleNewGridLine();

        //AddNewGridLineCalculationCmd = new RelayCommand<Window>(_ => OpenGridLineCalculationView);
        OpenSumEditWindowCmd = new RelayCommand<Window>(async (mainWindow) => await OpenGridLineCalculationView(mainWindow));
        OpenAddCategoryWindowCmd = new RelayCommand<Window>(async (mainWindow) => await OpenAddCategoryView(mainWindow));
        AddNewGridLineCmd = new RelayCommand<Window>(async (mainWindow) => await AddNewGridLine(mainWindow));
    }

    public GridAddLineViewModel()
    {
        SampleGridData sampleGridData = new SampleGridData();
        PreviewGridLines = sampleGridData.GetSampleGridData();
        SetDefaultDateForDatePicker();
        EditingAllowed = true;
        GridLineNumberIncrement = 1;
        GridLineNumberMinimum = 1;
        GridLineNumberMaximum = PreviewGridLines.Count+1;
        DesignView = true;
        IsSumLineSelected = false;
        IsDebitLineSelected = true;
        IsDebitSumLineSelected = true;
        IsSpecialLineSelected = true;
        NewGridLineSum = "5+7-12+85";
        AddExampleNewGridLine();
    }

    private void AddCategories()
    {
        if (AppService.Session.LineCategories == null)
            return;

        LineCategories = new ObservableCollection<LineCategory>();
        foreach (var category in AppService.Session.LineCategories)
        {
            LineCategories.Add(category.Clone());
        }
        if(AppService.Session.LineCategories.FirstOrDefault(x => x.Id == "Summe") != null)
            LineCategories.Remove(LineCategories.First(x => x.Id == "Summe"));
        if(AppService.Session.LineCategories.FirstOrDefault(x => x.Id == "Kontostand") != null)
            LineCategories.Remove(LineCategories.First(x => x.Id == "Kontostand"));
        if(AppService.Session.LineCategories.FirstOrDefault(x => x.Id == "Kein Filter") != null)
            LineCategories.Remove(LineCategories.First(x => x.Id == "Kein Filter"));
        LineCategories = new ObservableCollection<LineCategory>(LineCategories.OrderBy(x => x.Id));
    }

    private void AddLineTypes()
    {
        if(AppService.Session.LineTyps == null)
            return;

        LineTypes = new ObservableCollection<LineTyp>();
        foreach (LineTyp lineTyp in AppService.Session.LineTyps)
        {
            LineTypes.Add(lineTyp.Clone());
        }
    }

    private void AddLineFrequencies()
    {
        if(AppService.Session.LineFrequencies == null)
            return;
        LineFrequencies = new ObservableCollection<LineFrequency>();
        foreach (LineFrequency lineFrequency in AppService.Session.LineFrequencies)
        {
            LineFrequencies.Add(lineFrequency.Clone());
        }
    }

    private async Task AddNewGridLine(Window window)
    {
        if(AppService == null)
            return;
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.User == null)
            return;
        if(AppService.Session.LineTyps == null)
            return;

        var result = await CheckValidation();
        if (!result)
            return;

        CancellationToken cancellationToken = new CancellationToken();
        IsProgressBarActive = true;
        
        NewGridLine.Name = NewGridLineName;
        NewGridLine.Number = NewGridLineNumber;
        NewGridLine.LineCategory = NewGridLineCategory;
        NewGridLine.LineCategoryId = NewGridLineCategory.Id;
        if (IsDebitSumLineSelected)
            NewGridLineType = AppService.Session.LineTyps.First(x => x.Id == (int)LineTypeEnum.Summen);
        NewGridLine.LineTyp = NewGridLineType;
        NewGridLine.LineTypId = NewGridLineType.Id;
        if (NewGridLineType.Id == (int)LineTypeEnum.Summen)
        {
            NewGridLine.LineCalculation = new LineCalculation()
            {
                Id = NewGridLineCalculation.Id,
                Calculation = NewGridLineCalculation.Calculation
            };
        }
        else
        {
            NewGridLine.LineCalculationId = null;
        }
        NewGridLine.ValidFrom = DateOnly.FromDateTime(NewGridLineValidFrom);
        NewGridLine.ValidTo = DateOnly.FromDateTime(NewGridLineValidTo);
        NewGridLine.Userid = AppService.User.Id;
        NewGridLine.Frequency = NewGridLineFrequency;
        NewGridLine.FrequencyId = NewGridLineFrequency.Id;
        if (NewGridLineType.Id == (int)LineTypeEnum.Summen || NewGridLineType.Id == (int)LineTypeEnum.Kontostand)
            ValueMarkedAsDefault = false;
        if (AppService.Session.LineCalculations != null && NewGridLineType.Id == (int)LineTypeEnum.Summen)
        {
            var isCalculationListed = AppService.Session.LineCalculations.FirstOrDefault(x => x.Id == NewGridLineCalculation.Id);
            if (isCalculationListed == null)
            {
                AppService.Session.LineCalculations.Add(NewGridLineCalculation);
            }
        }
        
        var transactionResult = await MBGridService.AddNewGridLineAsync(cancellationToken, PreviewGridLines, NewGridLine, ValueMarkedAsDefault);
        switch (transactionResult.Item1)
        {
            case (int)ErrorEnum.Success:
                foreach (GridLine gridLine in AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id))
                {
                    GridLine? line = PreviewGridLines.FirstOrDefault(x => x.Id == gridLine.Id);
                    if (line != null)
                    {
                        gridLine.Number = line.Number;
                    }
                }
                int startMonth = NewGridLine.ValidFrom.Month;
                decimal? value = NewGridLine.JanuarValue;
                ResetAllGridLineValues(NewGridLine);
                switch (startMonth)
                {
                    case (int)MonthEnum.Januar:
                        NewGridLine.JanuarValue = value;
                        break;
                    case (int)MonthEnum.Februar:
                        NewGridLine.FebruarValue = value;
                        break;
                    case (int)MonthEnum.März:
                        NewGridLine.MärzValue = value;
                        break;
                    case (int)MonthEnum.April:
                        NewGridLine.AprilValue = value;
                        break;
                    case (int)MonthEnum.Mai:
                        NewGridLine.MaiValue = value;
                        break;
                    case (int)MonthEnum.Juni:
                        NewGridLine.JuniValue = value;
                        break;
                    case (int)MonthEnum.Juli:
                        NewGridLine.JuliValue = value;
                        break;
                    case (int)MonthEnum.August:
                        NewGridLine.AugustValue = value;
                        break;
                    case (int)MonthEnum.September:
                        NewGridLine.SeptemberValue = value;
                        break;
                    case (int)MonthEnum.Oktober:
                        NewGridLine.OktoberValue = value;
                        break;
                    case (int)MonthEnum.November:
                        NewGridLine.NovemberValue = value;
                        break;
                    case (int)MonthEnum.Dezember:
                        NewGridLine.DezemberValue = value;
                        break;
                }
                if (NewGridLine.LineTypId == (int)LineTypeEnum.Summen)
                {
                    NewGridLine.LineCalculationId = transactionResult.Item3.Id;
                }
                AppService.Session.GridLines.Add(NewGridLine);
                if (ValueMarkedAsDefault)
                {
                    MBGridImportValues importValue =
                        await MBMiscService.GetGridImportValueAsync(cancellationToken, transactionResult.Item2,
                            AppService.User.Id);
                    if (importValue.Id != 0)
                    {
                        if(AppService.Session.GridImportValues != null)
                            AppService.Session.GridImportValues.Add(importValue);
                    }
                }
                
                window.Close(NewGridLine);
                return;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Zeile konnte nicht angelegt werden"+Environment.NewLine+"Bitte versuchen Sie es erneut!", (int)ErrorEnum.Aborted);
                break;
        }
        IsProgressBarActive = false;
    }

    private void ResetAllGridLineValues(GridLine gridLine)
    {
        gridLine.JanuarValue = 0;
        gridLine.FebruarValue = 0;
        gridLine.MärzValue = 0;
        gridLine.AprilValue = 0;
        gridLine.MaiValue = 0;
        gridLine.JuniValue = 0;
        gridLine.JuliValue = 0;
        gridLine.AugustValue = 0;
        gridLine.SeptemberValue = 0;
        gridLine.OktoberValue = 0;
        gridLine.NovemberValue = 0;
        gridLine.DezemberValue = 0;
    }

    private async Task<bool> CheckValidation()
    {
        if (String.IsNullOrWhiteSpace(NewGridLineName))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie einen Zeilennamen an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (String.IsNullOrWhiteSpace(NewGridLineValue))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie einen Zeilenwert an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (NewGridLineCategory == null)
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie einen Kategorie an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (NewGridLineType == null)
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie eine Zeilenkategorie an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (NewGridLineFrequency == null)
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie eine Abbuchungsfrequenz an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (NewGridLineValidFrom > NewGridLineValidTo)
        {
            MessageService.WarningMessageBoxClassic("Bitte geben passen die Gültigkeit der Zeile an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (NewGridLineType.Id == (int)LineTypeEnum.Summen)
        {
            if (String.IsNullOrWhiteSpace(NewGridLineSum))
            {
                MessageService.WarningMessageBoxClassic("Bitte geben Sie eine gültige Summenberechnung an!", (int)ErrorEnum.NotValidInput);
                return false;
            }
        }
        
        return true;
    }

    private void AddCurrentGridLines()
    {
        if(AppService == null)
            return;
        if (AppService.Session.GridLines == null)
            return;
        if(AppService.User == null)
            return;
        DateTime currentDate = DateTime.Now;
        DateOnly date = DateOnly.FromDateTime(currentDate);
        PreviewGridLines = new ObservableCollection<GridLine>();
        foreach (var line in AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id && ((x.ValidFrom.Month <= date.Month && x.ValidFrom.Year <= date.Year || x.ValidFrom.Year < date.Year)) && (x.ValidTo.Month >= date.Month && x.ValidTo.Year >= date.Year)))
        {
            PreviewGridLines.Add(line.Clone());
        }
        PreviewGridLines = new ObservableCollection<GridLine>(PreviewGridLines.OrderBy(x => x.Number));
    }

    private async Task AddExampleNewGridLine()
    {
        if (PreviewGridLines.Count == 0)
        {
            //neuer Benutzer ohne eine vorhandene Zeile
            PreviewGridLines = new ObservableCollection<GridLine>();
            CancellationToken cancellationToken = new CancellationToken();
            NewGridLineId = await MBGridService.GetAvailableGridLineMaxIdAsync(cancellationToken);
            NewGridLineNumber = 1;
            GridLineNumberMaximum = 1;
        }
        else
        {
            GridLineNumberMaximum = PreviewGridLines.Count+1;
            NewGridLineId = PreviewGridLines.Max(x => x.Id) + 1;
            NewGridLineNumber = PreviewGridLines.Max(x => x.Number) +1;
            
        }
        NewGridLineName = "Beispielzeile";
        NewGridLineValue = "0";
        //String.Format("{0:N2}", NewGridLineValue);
        NewGridLine = new GridLine()
        {
            Id = NewGridLineId,
            Name = NewGridLineName,
            Number = NewGridLineNumber,
            JanuarValue = decimal.Parse(NewGridLineValue)
        };
        PreviewGridLines.Add(NewGridLine);
    }
    
    private void SetDefaultDateForDatePicker()
    {
        DisplayDateStart = new DateTime((DateTime.Now.Year-1), 1, 1);
        DisplayDateEnd = new DateTime((DateTime.Now.Year+5), 12, 31);
        NewGridLineValidFrom = DateTime.Now;
        NewGridLineValidTo = DateTime.Now;
    }

    partial void OnNewGridLineNumberChanged(int value)
    {
        if (!PreviewGridLines.Contains(NewGridLine))
            return;
        
        foreach (GridLine gridLine in PreviewGridLines)
        {
            if(gridLine.Id == NewGridLineId)
                continue;
            //Ascending
            if (gridLine.Number >= value && NewGridLine.Number > gridLine.Number)
                gridLine.Number = value + 1;
            if (value == gridLine.Number)
                gridLine.Number = value - 1;
        }

        NewGridLineNumber = value;
        NewGridLine.Number = NewGridLineNumber;
        PreviewGridLines = new ObservableCollection<GridLine>(PreviewGridLines.OrderBy(x => x.Number));
    }

    partial void OnNewGridLineNameChanged(string linename)
    {
        if(NewGridLine == null || linename == null)
            return;
        NewGridLine.Name = linename;
    }

    partial void OnNewGridLineTypeChanged(LineTyp type)
    {
        IsSumLineSelected = type.Id switch
        {
            (int)LineTypeEnum.Einnahmen => false,
            (int)LineTypeEnum.Ausgaben => false,
            (int)LineTypeEnum.Summen => true,
            (int)LineTypeEnum.Kontostand => false,
        };
        IsDebitLineSelected = type.Id switch
        {
            (int)LineTypeEnum.Einnahmen => false,
            (int)LineTypeEnum.Ausgaben => false,
            (int)LineTypeEnum.Summen => false,
            (int)LineTypeEnum.Kontostand => true,
        };
    }

    partial void OnIsSumLineSelectedChanged(bool value)
    {
        if(DesignView)
            return;
        
        if (value)
        {
            ValueMarkedAsDefault = false;
            if (LineCategories.FirstOrDefault(x => x.Id == "Summe") == null)
            {
                LineCategory sumCat = new LineCategory()
                {
                    Id = "Summe"
                };
                LineCategories.Add(sumCat);
                NewGridLineCategory = sumCat;
            }
            else
            {
                NewGridLineCategory = LineCategories.First(x => x.Id == "Summe");
            }
            NewGridLineSum = string.Empty;
            NewGridLineFrequency = LineFrequencies.First(x => x.Id == "Monat");
        }
        else
        {
            if(LineCategories.FirstOrDefault(x => x.Id == "Summe") != null)
                LineCategories.Remove(LineCategories.First(x => x.Id == "Summe"));
        }

        if (IsDebitLineSelected)
            IsSpecialLineSelected = true;
        else
            IsSpecialLineSelected = value;
    }

    partial void OnIsDebitLineSelectedChanged(bool value)
    {
        if(DesignView)
            return;

        IsDebitSumLineSelected = NewGridLine.LineTypId == (int)LineTypeEnum.Summen;
        
        if (value)
        {
            ValueMarkedAsDefault = false;
            if (LineCategories.FirstOrDefault(x => x.Id == "Kontostand") == null)
            {
                LineCategory kontoCat = new LineCategory()
                {
                    Id = "Kontostand"
                };
                LineCategories.Add(kontoCat);
                NewGridLineCategory = kontoCat;
            }
            else
            {
                NewGridLineCategory = LineCategories.First(x => x.Id == "Kontostand");
            }
            NewGridLineFrequency = LineFrequencies.First(x => x.Id == "Monat");
        }
        else
        {
            if(LineCategories.FirstOrDefault(x => x.Id == "Kontostand") != null)
                LineCategories.Remove(LineCategories.First(x => x.Id == "Kontostand"));
        }

        if (IsSumLineSelected)
            IsSpecialLineSelected = true;
        else
            IsSpecialLineSelected = value;
    }
    
    private void OpenGridLineCaluclationWindow()
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new GridAddSumView()
            {
                DataContext = new GridAddSumViewModel(AppService, "Grid-Zeile Summenbildung", NewGridLine, NewGridLineSum),
            };
            desktop.MainWindow.Show();
        }
    }

    private async Task OpenGridLineCalculationView(Window window)
    {
        var sumView = new GridAddSumView()
        {
            DataContext = new GridAddSumViewModel(AppService, "Grid-Zeile Summenbildung", NewGridLine, NewGridLineSum)
        };
        var result = await sumView.ShowDialog<LineCalculation>(window);
        if (result != null)
        {
            NewGridLineCalculation = result;
            NewGridLineSum = NewGridLineCalculation.Calculation;
        }
    }

    private async Task OpenAddCategoryView(Window window)
    {
        var categoryView = new GridAddCategoryView()
        {
            DataContext = new GridAddCategoryViewModel(AppService, "Grid-Zeilen Kategorie hinzufügen")
        };
        var result = await categoryView.ShowDialog<LineCategory>(window);
        if (result != null)
        {
            LineCategories.Add(result);
            NewGridLineCategory = result;
            LineCategories = new ObservableCollection<LineCategory>(LineCategories.OrderBy(x => x.Id));
        }
    }
    
    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private ObservableCollection<GridLine> _previewGridLines;
    [ObservableProperty] private int _newGridLineId;
    [ObservableProperty] private string _newGridLineName;
    [ObservableProperty] private string _newGridLineValue;
    [ObservableProperty] private bool _valueMarkedAsDefault;
    [ObservableProperty] private int _newGridLineNumber;
    [ObservableProperty] private int _gridLineNumberMinimum;
    [ObservableProperty] private int _gridLineNumberMaximum;
    [ObservableProperty] private int _gridLineNumberIncrement;
    [ObservableProperty] private LineCategory _newGridLineCategory;
    [ObservableProperty] private ICommand _addNewGridLineCategoryCmd;
    [ObservableProperty] private IRelayCommand<Window> _openAddCategoryWindowCmd;
    [ObservableProperty] private LineTyp _newGridLineType;
    [ObservableProperty] private bool _isSumLineSelected;
    [ObservableProperty] private string _newGridLineSum;
    [ObservableProperty] private LineCalculation _newGridLineCalculation;
    [ObservableProperty] private ICommand _addNewGridLineCalculationCmd;
    [ObservableProperty] private IRelayCommand<Window> _openSumEditWindowCmd;
    [ObservableProperty] private DateTime _newGridLineValidFrom;
    [ObservableProperty] private DateTime _newGridLineValidTo;
    [ObservableProperty] private DateOnly _dateToDisplay;
    [ObservableProperty] private DateTime _displayDateStart;
    [ObservableProperty] private DateTime _displayDateEnd;
    [ObservableProperty] private LineFrequency _newGridLineFrequency;
    [ObservableProperty] private bool _editingAllowed;
    [ObservableProperty] private bool _isSpecialLineSelected;
    [ObservableProperty] private bool _isDebitLineSelected;
    [ObservableProperty] private bool _isDebitSumLineSelected;
    [ObservableProperty] private bool _isProgressBarActive;
    [ObservableProperty] private ICommand _addNewGridLineCmd;
    [ObservableProperty] private ObservableCollection<LineCategory> _lineCategories;
    [ObservableProperty] private ObservableCollection<LineFrequency> _lineFrequencies;
    [ObservableProperty] private ObservableCollection<LineTyp> _lineTypes;
    [ObservableProperty] private GridLine _newGridLine;
    [ObservableProperty] private bool _designView;

    [ObservableProperty] private string _backGroundColor;
    [ObservableProperty] private string _foregroundColor;
}