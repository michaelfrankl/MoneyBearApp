using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBear.DesignData;
using MoneyBear.Services;
using MoneyBear.Views.Grid;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels;

public partial class GridEditLineViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public GridEditLineViewModel(string windowTitle, AppService appService)
    {
        WindowTitle = windowTitle;
        AppService = appService;
        EditingAllowed = true;
        IsSpecialLineSelected = false;
        SetDefaultDateForDatePicker();
        EditingAllowed = true;
        DesignView = false;
        IsSumLineSelected = false;
        IsSpecialLineSelected = false;
        AddCategories();
        AddLineTypes();
        AddLineFrequencies();
        GridLineNumberIncrement = 1;
        GridLineNumberMinimum = 1;
        AddCurrentGridLines();
        GridLineNumberMaximum = PreviewGridLines.Count;
        OpenSumEditWindowCmd = new RelayCommand<Window>(async (mainWindow) => await OpenGridLineCalculationView(mainWindow));
        OpenAddCategoryWindowCmd = new RelayCommand<Window>(async (mainWindow) => await OpenAddCategoryView(mainWindow));
        
        EditGridLineCmd = new RelayCommand<Window>(async (mainWindow) => await EditExistingGridLine(mainWindow));
    }

    public GridEditLineViewModel()
    {
        SampleGridData sampleGridData = new SampleGridData();
        PreviewGridLines = sampleGridData.GetSampleGridData();
        LineCategories = sampleGridData.GetSampleLineCategories();
        LineTypes = sampleGridData.GetLineTypes();
        LineFrequencies = sampleGridData.GetSampleLineFrequencies();
        GridLineNumberIncrement = 1;
        GridLineNumberMinimum = 1;
        GridLineNumberMaximum = PreviewGridLines.Count;
        EditingAllowed = true;
        DesignView = true;
        IsSpecialLineSelected = false;
        EditGridLine = PreviewGridLines.First();
        EditGridLineName = PreviewGridLines.First().Name;
        EditGridLineValue = PreviewGridLines.First().JanuarValue.ToString();
        EditGridLineNumber = PreviewGridLines.First().Number;
        EditGridLineCategory = LineCategories.First(x => x.Id == EditGridLine.LineCategoryId);
        EditGridLineType = LineTypes.First(x => x.Id == EditGridLine.LineTypId);
        SetDefaultDateForDatePicker();
        IsProgressBarActive = false;

    }

    private async Task EditExistingGridLine(Window mainWindow)
    {
        if(AppService == null)
            return;
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.User == null)
            return;
        if(AppService.Session.LineTyps == null)
            return;
        if(IsProgressBarActive)
            return;

        var result = await CheckValidation();
        if (!result)
            return;

        IsProgressBarActive = true;
        
        EditGridLine.Name = EditGridLineName;
        EditGridLine.Number = EditGridLineNumber;
        EditGridLine.LineCategory = EditGridLineCategory;
        EditGridLine.LineCategoryId = EditGridLineCategory.Id;
        if (IsDebitSumLineSelected)
            EditGridLineType = AppService.Session.LineTyps.First(x => x.Id == (int)LineTypeEnum.Summen);
        EditGridLine.LineTyp = EditGridLineType;
        EditGridLine.LineTypId = EditGridLineType.Id;
        if (EditGridLineType.Id == (int)LineTypeEnum.Summen)
        {
            EditGridLine.LineCalculation = new LineCalculation()
            {
               Id = EditGridLineLineCalculation.Id,
               Calculation = EditGridLineLineCalculation.Calculation
            };
        }
        else
        {
            EditGridLine.LineCalculationId = null;
        }

        EditGridLine.ValidFrom = DateOnly.FromDateTime(EditGridLineValidFrom);
        EditGridLine.ValidTo = DateOnly.FromDateTime(EditGridLineValidTo);
        EditGridLine.Userid = AppService.User.Id;
        EditGridLine.Frequency = EditGridLineFrequency;
        EditGridLine.FrequencyId = EditGridLineFrequency.Id;
        
        CancellationToken cancellationToken = new CancellationToken();
        var transactionResult = await MBGridService.EditExistingGridLineAsync(cancellationToken, PreviewGridLines, EditGridLine);
        switch (transactionResult.Item1)
        {
            case (int)ErrorEnum.Success:
                var lineInAppService = AppService.Session.GridLines.First(x => x.Id == EditGridLine.Id);
                lineInAppService.Name = EditGridLine.Name;
                lineInAppService.LineCategory = EditGridLineCategory;
                lineInAppService.LineCategoryId = EditGridLine.LineCategoryId;
                lineInAppService.LineTyp = EditGridLineType;
                lineInAppService.LineTypId = EditGridLine.LineTypId;
                lineInAppService.LineCalculation = transactionResult.Item2;
                lineInAppService.LineCalculationId = transactionResult.Item2.Id;
                if (lineInAppService.LineCalculationId != null && transactionResult.Item2.Id != 0)
                {
                    var listedCalculation =
                        AppService.Session.LineCalculations.FirstOrDefault(x =>
                            x.Id == lineInAppService.LineCalculationId);
                    if (listedCalculation != null)
                    {
                        listedCalculation.Calculation = lineInAppService.LineCalculation.Calculation;
                    }
                }
                lineInAppService.ValidFrom = EditGridLine.ValidFrom;
                lineInAppService.ValidTo = EditGridLine.ValidTo;
                lineInAppService.Frequency = EditGridLineFrequency;
                lineInAppService.FrequencyId = EditGridLine.FrequencyId;
                IsProgressBarActive = false;
                MessageService.NormalMessageBoxClassic("Grid-Zeile wurde geändert!");
                mainWindow.Close(EditGridLine);
                return;
            case (int)ErrorEnum.NullReference:
                MessageService.WarningMessageBoxClassic("Grid-Zeile existiert nicht in der Datenbank!", (int)ErrorEnum.NullReference);
                break;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Die Änderungen konnten nicht gespeichert werden!", (int)ErrorEnum.Aborted);
                break;
        }
        IsProgressBarActive = false;
    }
    
    private async Task<bool> CheckValidation()
    {
        if (String.IsNullOrWhiteSpace(EditGridLineName))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie einen Zeilennamen an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (String.IsNullOrWhiteSpace(EditGridLineValue))
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie einen Zeilenwert an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (EditGridLineCategory == null)
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie einen Kategorie an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (EditGridLineType == null)
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie eine Zeilenkategorie an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (EditGridLineFrequency == null)
        {
            MessageService.WarningMessageBoxClassic("Bitte geben Sie eine Abbuchungsfrequenz an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (EditGridLineValidFrom > EditGridLineValidTo)
        {
            MessageService.WarningMessageBoxClassic("Bitte geben passen die Gültigkeit der Zeile an!", (int)ErrorEnum.NotValidInput);
            return false;
        }

        if (EditGridLineType.Id == (int)LineTypeEnum.Summen)
        {
            if (String.IsNullOrWhiteSpace(EditGridLineSum))
            {
                MessageService.WarningMessageBoxClassic("Bitte geben Sie eine gültige Summenberechnung an!", (int)ErrorEnum.NotValidInput);
                return false;
            }
        }
        
        return true;
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

    private async Task OpenGridLineCalculationView(Window window)
    {
        var sumView = new GridAddSumView()
        {
            DataContext = new GridAddSumViewModel(AppService, "Grid-Zeile Summenbildung", EditGridLine, EditGridLineSum)
        };
        var result = await sumView.ShowDialog<LineCalculation>(window);
        if (result != null)
        {
            EditGridLineLineCalculation = result;
            EditGridLine.LineCalculation = result;
            EditGridLineSum = result.Calculation;
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
            EditGridLineCategory = result;
            //TODO: Combobox im Hauptgrid muss aktualisiert werden!
            //Hauptgrid befindet sich in einem userControl, daher muss irgendwie eine Aktualisierungs-Option eingebaut werden
        }
    }
    
    private void AddCurrentGridLines()
    {
        if(AppService == null)
            return;
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.User == null)
            return;
        DateTime currentDate = DateTime.Now;
        DateOnly date = DateOnly.FromDateTime(currentDate);
        PreviewGridLines = new ObservableCollection<GridLine>();

        foreach (var line in AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id && ((x.ValidFrom.Month <= date.Month && x.ValidFrom.Year <= date.Year) || x.ValidFrom.Year <= date.Year) 
                     && (x.ValidTo.Month >= date.Month && x.ValidTo.Year >= date.Year)))
        {
            PreviewGridLines.Add(line.Clone());
        }
        PreviewGridLines = new ObservableCollection<GridLine>(PreviewGridLines.OrderBy(x => x.Number));
        if (PreviewGridLines.Count <= 0)
        {
            MessageService.WarningMessageBoxClassic("Keine aktuellen Zeilen gefunden, bitte Import ausführen!", (int)ErrorEnum.NoValidImport);
            return;
        }
        EditGridLine = PreviewGridLines.First();
        SelectedGridLine = EditGridLine;
    }

    partial void OnSelectedGridLineChanged(GridLine line)
    {
        if(line == null)
            return;
        if(EditGridLine == null)
            return;
        
        EditGridLine = line;
        EditGridLineName = line.Name;
        EditGridLineNumber = line.Number;
        EditGridLineValue = line.JanuarValue.ToString();
        EditGridLineValidFrom = new DateTime(line.ValidFrom.Year, line.ValidFrom.Month, line.ValidFrom.Day);
        EditGridLineValidTo = new DateTime(line.ValidTo.Year, line.ValidTo.Month, line.ValidTo.Day);
        
        EditGridLineType = LineTypes.First(x => x.Id == EditGridLine.LineTypId);
        if (EditGridLineType.Id == (int)LineTypeEnum.Summen)
        {
            if (LineCategories.FirstOrDefault(x => x.Id == "Summe") == null)
            {
                LineCategory sumCat = new LineCategory()
                {
                    Id = "Summe"
                };
                LineCategories.Add(sumCat);
            }
            EditGridLine.LineCalculation = AppService.Session.LineCalculations.FirstOrDefault(x => x.Id == line.LineCalculationId);
            EditGridLineSum = EditGridLine.LineCalculation.Calculation;
        }
        if (EditGridLineType.Id == (int)LineTypeEnum.Kontostand)
        {
            if (LineCategories.FirstOrDefault(x => x.Id == "Kontostand") == null)
            {
                LineCategory kontoCat = new LineCategory()
                {
                    Id = "Kontostand"
                };
                LineCategories.Add(kontoCat);
            }
        }
        EditGridLineCategory = LineCategories.First(x => x.Id == EditGridLine.LineCategoryId);
        EditGridLineFrequency = LineFrequencies.First(x => x.Id == EditGridLine.FrequencyId);
    }
    
    partial void OnEditGridLineNumberChanged(int value)
    {
        if (!PreviewGridLines.Contains(EditGridLine))
            return;
        
        foreach (GridLine gridLine in PreviewGridLines)
        {
            if (gridLine.Number >= value && EditGridLine.Number > gridLine.Number)
                gridLine.Number = value + 1;
            if (value == gridLine.Number)
                gridLine.Number = value - 1;
        }

        EditGridLineNumber = value;
        EditGridLine.Number = EditGridLineNumber;
        PreviewGridLines = new ObservableCollection<GridLine>(PreviewGridLines.OrderBy(x => x.Number));
    }
    
    partial void OnEditGridLineTypeChanged(LineTyp type)
    {
        if(type == null)
            return;
        
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
            if (LineCategories.FirstOrDefault(x => x.Id == "Summe") == null)
            {
                LineCategory sumCat = new LineCategory()
                {
                    Id = "Summe"
                };
                LineCategories.Add(sumCat);
                EditGridLineCategory = sumCat;
            }
            else
            {
                EditGridLineCategory = LineCategories.First(x => x.Id == "Summe");
            }
            EditGridLineSum = string.Empty;
            EditGridLineFrequency = LineFrequencies.First(x => x.Id == "Monat");
        }
        else
        {
            EditGridLineSum = string.Empty;
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
        
        if (value)
        {
            if (LineCategories.FirstOrDefault(x => x.Id == "Kontostand") == null)
            {
                LineCategory kontoCat = new LineCategory()
                {
                    Id = "Kontostand"
                };
                LineCategories.Add(kontoCat);
                EditGridLineCategory = kontoCat;
            }
            else
            {
                EditGridLineCategory = LineCategories.First(x => x.Id == "Kontostand");
            }

            EditGridLineFrequency = LineFrequencies.First(x => x.Id == "Monat");
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
    
    private void SetDefaultDateForDatePicker()
    {
        DisplayDateStart = new DateTime((DateTime.Now.Year-1), 1, 1);
        DisplayDateEnd = new DateTime((DateTime.Now.Year+5), 12, 31);
        EditGridLineValidFrom = DateTime.Now;
        EditGridLineValidTo = DateTime.Now;
    }


    [ObservableProperty] private  string _windowTitle;
    [ObservableProperty] private bool _designView;
    [ObservableProperty] private ObservableCollection<GridLine> _previewGridLines;
    [ObservableProperty] private bool _editingAllowed;
    [ObservableProperty] private string _editGridLineName;
    [ObservableProperty] private string _editGridLineValue;
    [ObservableProperty] private GridLine _editGridLine;
    [ObservableProperty] private int _editGridLineNumber;
    [ObservableProperty] private int _gridLineNumberMinimum;
    [ObservableProperty] private int _gridLineNumberMaximum;
    [ObservableProperty] private int _gridLineNumberIncrement;
    [ObservableProperty] private GridLine _selectedGridLine;
    [ObservableProperty] private ObservableCollection<LineCategory> _lineCategories;
    [ObservableProperty] private LineCategory _editGridLineCategory;
    [ObservableProperty] private ObservableCollection<LineTyp> _lineTypes;
    [ObservableProperty] private LineTyp _editGridLineType;
    [ObservableProperty] private ObservableCollection<LineFrequency> _lineFrequencies;
    [ObservableProperty] private LineFrequency _editGridLineFrequency;
    [ObservableProperty] private bool _isSpecialLineSelected;
    [ObservableProperty] private bool _isSumLineSelected;
    [ObservableProperty] private bool _isDebitLineSelected;
    [ObservableProperty] private bool _isDebitSumLineSelected;
    [ObservableProperty] private string _editGridLineSum;
    [ObservableProperty] private LineCalculation _editGridLineLineCalculation;
    [ObservableProperty] private DateOnly _dateToDisplay;
    [ObservableProperty] private DateTime _displayDateStart;
    [ObservableProperty] private DateTime _displayDateEnd;
    [ObservableProperty] private DateTime _editGridLineValidFrom;
    [ObservableProperty] private DateTime _editGridLineValidTo;
    [ObservableProperty] private bool _isProgressBarActive;
    [ObservableProperty] private ICommand _openAddCategoryWindowCmd;
    [ObservableProperty] private ICommand _openSumEditWindowCmd;
    [ObservableProperty] private ICommand _editGridLineCmd;
}