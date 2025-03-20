using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.EntityFrameworkCore;
using MoneyBear.DesignData;
using MoneyBear.Services;
using MoneyBear.Views;
using NASPLOID;
using NASPLOID.Models;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels.Import;

public partial class GridImportSelectedLinesViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public GridImportSelectedLinesViewModel(AppService appService)
    {
        AppService = appService;
        SearchText = string.Empty;
        InitDatePicker();
        InitGrid();
        AddLineCmd = new RelayCommand(AddLineToSelectedLineList);
        RemoveLineCmd = new RelayCommand(RemoveLineFromSelectedLineList);
        ImportSelectedLinesCmd = new RelayCommand<Window>(async (mainWindow) => ImportSelectedLines(mainWindow));
    }

    public GridImportSelectedLinesViewModel()
    {
        InitDatePicker();
        SampleGridData sampleGridData = new SampleGridData();
        PreviewGridLines = sampleGridData.GetSampleGridData();
        SelectedLines = new ObservableCollection<GridLine>();
        SelectedLines.Add(PreviewGridLines[0]);
        SelectedLines.Add(PreviewGridLines[1]);
        SelectedLines.Add(PreviewGridLines[2]);
        IsTaskActive = false;
        IsRemovingAllowed = true;
        SearchText = string.Empty;
    }

    private void InitGrid()
    {
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.User == null)
            return;
        PreviewGridLines = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x =>
                x.Userid == AppService.User.Id
                && (x.ValidFrom.Month <= SelectedDate.Month && x.ValidFrom.Year <= SelectedDate.Year || x.ValidFrom.Year < SelectedDate.Year)
                && x.ValidTo.Month >= SelectedDate.Month && x.ValidTo.Year >= SelectedDate.Year
                && x.LineTypId != (int)LineTypeEnum.Summen && x.LineTypId != (int)LineTypeEnum.Kontostand)
            .Select(x => x.Clone()).OrderBy(x => x.Name));
        if (PreviewGridLines.Count == 0)
        {
            MessageService.WarningMessageBoxClassic("Es wurden keine Gridzeilen für den Zeitraum gefunden!",
                (int)ErrorEnum.NoMatchFound);
        }
        TempLines = new ObservableCollection<GridLine>(PreviewGridLines.Select(x => x.Clone()));
        SelectedLines = new ObservableCollection<GridLine>();
    }

    private async Task ImportSelectedLines(Window mainWindow)
    {
        if (SelectedLines.Count == 0)
        {
            MessageService.NormalMessageBoxClassic("Es wurden keine Zeilen zum importieren selektier!");
            return;
        }
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.Session.LineCalculations == null)
            return;
        
        if(IsTaskActive)
            return;
        
        IsTaskActive = true;

        CancellationToken cancellationToken = new CancellationToken();
        int importResult = 0;
        await Task.Run(async () =>
        {
            importResult = await MBMiscService.ImportValuesForSpecificLinesAndMonthAsync(cancellationToken, SelectedLines, SelectedDate.Month, SelectedDate.Year, AppService.User.Id);
        }, cancellationToken);
        switch (importResult)
        {
            case (int)ErrorEnum.Success:
                foreach (GridLine selectedLine in SelectedLines)
                {
                    GridLine? lineToEdit = AppService.Session.GridLines.FirstOrDefault(x => x.Id == selectedLine.Id);
                    await using var context = new DatabaseContext();
                    GRID_STORAGE? lineStorage = await context.GRID_STORAGEs.FirstOrDefaultAsync(x => x.GL_ID == selectedLine.Id 
                        && x.LINE_USERID == AppService.User.Id && x.LINE_MONTH == SelectedDate.Month && x.LINE_YEAR == SelectedDate.Year, cancellationToken);
                    if (lineToEdit != null)
                    {
                        if(lineStorage != null)
                            SetValueForSpecificMonth(lineToEdit, lineStorage.VALUE, SelectedDate.Month);
                        else
                        {
                            MessageService.WarningMessageBoxClassic("Es ist ein Problem aufgetreten!", (int)ErrorEnum.NullReference);
                            IsTaskActive = false;
                            return;
                        }
                    }
                }
                IsTaskActive = false;
                MessageService.NormalMessageBoxClassic("Import wurde durchgeführt!");
                mainWindow.Close(SelectedDate);
                return;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Vorgang konnte nicht durchgeführt werden!", (int)ErrorEnum.Aborted);
                break;
            case (int)ErrorEnum.NoValidImport:
                MessageService.WarningMessageBoxClassic("Operation abgebrochen!", (int)ErrorEnum.NoValidImport);
                break;
        }
        IsTaskActive = false;
    }

    private void AddLineToSelectedLineList()
    {
        if(IsTaskActive)
            return;
        if (SelectedPreviewGridLine == null)
        {
            MessageService.NormalMessageBoxClassic("Wählen Sie eine Zeile aus, welche Sie importieren möchten!");
            return;
        }
        if (SelectedLines.Contains(SelectedPreviewGridLine))
        {
            MessageService.NormalMessageBoxClassic("Die ausgwählte Zeile wurde bereits zur Importliste hinzugefügt!");
            return;
        }
        SelectedLines.Add(SelectedPreviewGridLine);
        IsRemovingAllowed = true;
    }

    private void RemoveLineFromSelectedLineList()
    {
        if(IsTaskActive)
            return;
        if (SelectedSelectedGridLine == null)
        {
            MessageService.NormalMessageBoxClassic("Wählen Sie eine Zeile aus, welche Sie nicht mehr importieren möchten!");
            return; 
        }

        if (SelectedLines.Count > 0)
            SelectedLines.Remove(SelectedSelectedGridLine);

        if (SelectedLines.Count == 0)
        {
            IsRemovingAllowed = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if(PreviewGridLines == null)
            return;
        if(TempLines == null)
            return;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            PreviewGridLines = TempLines;
            return;
        }
        PreviewGridLines = new ObservableCollection<GridLine>(TempLines.Where(x => x.Name.ToLower().Contains(value.ToLower())));
    }

    private void InitDatePicker()
    {
        DisplayDateStart = new DateTime(DateTime.Now.Year, 1, 1);
        DisplayDateEnd = new DateTime(DateTime.Now.Year, 12, 31);
        SelectedDate = DateTime.Now;
    }

    private void SetValueForSpecificMonth(GridLine line, decimal? value, int month)
    {
        value ??= 0;

        switch (month)
        {
            case (int)MonthEnum.Januar:
                line.JanuarValue = value;
                break;
            case (int)MonthEnum.Februar:
                line.FebruarValue = value;
                break;
            case (int)MonthEnum.März:
                line.MärzValue = value;
                break;
            case (int)MonthEnum.April:
                line.AprilValue = value;
                break;
            case (int)MonthEnum.Mai:
                line.MaiValue = value;
                break;
            case (int)MonthEnum.Juni:
                line.JuniValue = value;
                break;
            case (int)MonthEnum.Juli:
                line.JuliValue = value;
                break;
            case (int)MonthEnum.August:
                line.AugustValue = value;
                break;
            case (int)MonthEnum.September:
                line.SeptemberValue = value;
                break;
            case (int)MonthEnum.Oktober:
                line.OktoberValue = value;
                break;
            case (int)MonthEnum.November:
                line.NovemberValue = value;
                break;
            case (int)MonthEnum.Dezember:
                line.DezemberValue = value;
                break;
        }
    }
    
    [ObservableProperty] private DateTime _selectedDate;
    [ObservableProperty] private DateTime _displayDateStart;
    [ObservableProperty] private DateTime _displayDateEnd;

    [ObservableProperty] private string _searchText;

    [ObservableProperty] private ObservableCollection<GridLine> _previewGridLines;
    [ObservableProperty] private ObservableCollection<GridLine> _tempLines;
    [ObservableProperty] private GridLine _selectedPreviewGridLine;
    [ObservableProperty] private ObservableCollection<GridLine> _selectedLines;
    [ObservableProperty] private GridLine _selectedSelectedGridLine;

    [ObservableProperty] private bool _isTaskActive;
    
    [ObservableProperty] private ICommand _addLineCmd;
    [ObservableProperty] private ICommand _removeLineCmd;
    [ObservableProperty] private bool _isRemovingAllowed;
    [ObservableProperty] private ICommand _importSelectedLinesCmd;
}