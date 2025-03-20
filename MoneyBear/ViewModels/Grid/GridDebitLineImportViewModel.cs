using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using MoneyBear.DesignData;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels;

public partial class GridDebitLineImportViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public GridDebitLineImportViewModel(AppService appService)
    {
        AppService = appService;
        InitGrid();
        IsTaskActive = false;
        AffectedDates = new ObservableCollection<DateTime>();
        CloseWindowCmd = new RelayCommand<Window>(async mainWindow => await CloseAndCalculate(mainWindow));
    }

    public GridDebitLineImportViewModel()
    {
        SampleGridData sampleGridData = new SampleGridData();
        IsTaskActive = true;
        PreviewDebitLines = new ObservableCollection<GridLine>(sampleGridData.GetSampleGridData().Where(x => x.LineCategoryId == "Kontostand"));
        SetDefaultDateForDatePicker();
        SelectedGridLine = PreviewDebitLines[0];
        DebitLineName = SelectedGridLine.Name;
        DebitLineValue = SelectedGridLine.JanuarValue;
        DebitGridLineValidFrom = new DateTime(SelectedGridLine.ValidFrom.Year, SelectedGridLine.ValidFrom.Month, SelectedGridLine.ValidFrom.Day);
        DebitGridLineValidTo = new DateTime(SelectedGridLine.ValidTo.Year, SelectedGridLine.ValidTo.Month,
            SelectedGridLine.ValidTo.Day);
        PreviewEditLines =
        [
            SelectedGridLine
        ];
    }

    private async Task CloseAndCalculate(Window window)
    {
        IsTaskActive = true;
        var result =
            await MessageService.AnswerMessageBoxClassic(
                "Möchten Sie das Fenster verlassen und die Berechnung durchführen lassen?");
        if (result)
        {
            window.Close(AffectedDates);
            return;
        }
        IsTaskActive = false;
    }

    public async Task<int> SaveDebitLineChanges(GridLine line, decimal value, int month, int year)
    {
        CancellationToken cancellationToken = new CancellationToken();
        
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBMiscService.ImportDebitLineValueForSpecificMonth(cancellationToken, line, value, month, year, AppService.User.Id);

        }, cancellationToken);
        AffectedDates.Add(new DateTime(year, month, 1));
        return result;
    }

    private void InitGrid()
    {
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.User == null)
            return;
        
        PreviewDebitLines = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id 
            && x.LineTypId == (int)LineTypeEnum.Kontostand 
            && x.LineCategoryId == "Kontostand"
            && x.ValidFrom.Year <= DateTime.Now.Year && x.ValidTo.Year >= DateTime.Now.Year).Select(x => x.Clone()));
        if (PreviewDebitLines.Count > 0)
        {
            SelectedGridLine = PreviewDebitLines[0];
            DebitLineName = SelectedGridLine.Name;
            DebitLineValue = SelectedGridLine.JanuarValue;
            DebitGridLineValidFrom = new DateTime(SelectedGridLine.ValidFrom.Year, SelectedGridLine.ValidFrom.Month, SelectedGridLine.ValidFrom.Day);
            DebitGridLineValidTo = new DateTime(SelectedGridLine.ValidTo.Year, SelectedGridLine.ValidTo.Month,
                SelectedGridLine.ValidTo.Day);
            PreviewEditLines =
            [
                SelectedGridLine
            ];
        }
        else
        {
            MessageService.WarningMessageBoxClassic("Es konnten keine Kontostand-Zeilen gefunden werden!", (int)ErrorEnum.NoMatchFound);
            PreviewEditLines = new ObservableCollection<GridLine>();
            PreviewDebitLines = new ObservableCollection<GridLine>();
        }
    }
    
    private void SetDefaultDateForDatePicker()
    {
        DisplayDateStart = new DateTime(DateTime.Now.Year, 1, 1);
        DisplayDateEnd = new DateTime((DateTime.Now.Year+5), 12, 31);
        DebitGridLineValidFrom = DateTime.Now;
        DebitGridLineValidTo = DateTime.Now;
    }

    partial void OnSelectedGridLineChanged(GridLine line)
    {
        if(line == null)
            return;
        SelectedGridLine = line;
        DebitLineName = line.Name;
        DebitLineValue = line.JanuarValue;
        DebitGridLineValidFrom = new DateTime(SelectedGridLine.ValidFrom.Year, SelectedGridLine.ValidFrom.Month, SelectedGridLine.ValidFrom.Day);
        DebitGridLineValidTo = new DateTime(SelectedGridLine.ValidTo.Year, SelectedGridLine.ValidTo.Month,
            SelectedGridLine.ValidTo.Day);
        PreviewEditLines =
        [
            SelectedGridLine
        ];
    }

    [ObservableProperty] private ObservableCollection<GridLine> _previewDebitLines;
    [ObservableProperty] private ObservableCollection<GridLine> _previewEditLines;
    [ObservableProperty] private GridLine _selectedGridLine;
    [ObservableProperty] private string _debitLineName;
    [ObservableProperty] private decimal? _debitLineValue;
    [ObservableProperty] private DateTime _displayDateStart;
    [ObservableProperty] private DateTime _displayDateEnd;
    [ObservableProperty] private DateOnly _dateToDisplay;
    [ObservableProperty] private DateTime _debitGridLineValidFrom;
    [ObservableProperty] private DateTime _debitGridLineValidTo;

    [ObservableProperty] private ICommand _closeWindowCmd;
    [ObservableProperty] private ObservableCollection<DateTime> _affectedDates;


    [ObservableProperty] private bool _isTaskActive;
}