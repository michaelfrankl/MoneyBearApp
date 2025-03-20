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
using MoneyBear.Assets;
using MoneyBear.DesignData;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels.Home;

public partial class HomeOverViewEditorViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public HomeOverViewEditorViewModel(AppService appService, string windowTitle)
    {
        AppService = appService;
        WindowTitle = windowTitle;
        SearchText = string.Empty;

        InitGridWithData();

        RemoveAllLinesCmd = new RelayCommand<object>(_ => RemoveAllSelectedLines());
        AddDebitLinesCmd = new RelayCommand<object>(_ => AddDebitLines());
        AddOutgoingLinesCmd = new RelayCommand<object>(_ => AddOutgoingLines());
        AddSavedLinesCmd = new RelayCommand<object>(_ => AddSavedLines());
        AddSelectedGridLineCmd = new RelayCommand<object>(_ => AddSelectedGridLine());
        RemoveSelectedGridLineCmd = new RelayCommand<object>(_ => RemoveSelectedGridLine());
        SaveSelectedLinesToDBCmd = new RelayCommand<Window>(async (mainWindow) => await AddSelectedLinesToGlobalVariablesAsync(mainWindow));
    }

    public HomeOverViewEditorViewModel()
    {
        SearchText = string.Empty;
        IsDesignView = true;
        IsTaskActive = true;
        SampleGridData sampleGridData = new SampleGridData();
        GridLinePreview = sampleGridData.GetSampleGridData();
        SelectedGridLines = sampleGridData.GetSampleGridData();
    }

    private void InitGridWithData()
    {
        if(AppService.Session.GridLines == null) return;
        if(AppService.User == null) return;
        if(AppService.Session.GlobalVariables == null) return;
        
        MBGlobalVariables personPrefix = AppService.Session.GlobalVariables.FirstOrDefault(x => x.UserId == AppService.User.Id && x.Id.Equals("OverViewGridLines"));
        if (personPrefix == null)
        {
            SelectedGridLines = new ObservableCollection<GridLine>();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(personPrefix.ValueAsString))
            {
                List<int>? lineIds = personPrefix.ValueAsString.Split(',').Select(int.Parse).ToList();
                SelectedGridLines = new ObservableCollection<GridLine>();
                foreach (int id in lineIds)
                {
                    var gridline = AppService.Session.GridLines.FirstOrDefault(x => x.Id == id);
                    if(gridline == null)
                        continue;
                    SelectedGridLines.Add(gridline.Clone());
                }
            }else
                SelectedGridLines = new ObservableCollection<GridLine>();
        }
        GridLinePreview = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id 
                && x.ValidFrom <= DateOnly.FromDateTime(DateTime.Now) 
                && x.ValidTo >= DateOnly.FromDateTime(DateTime.Now))
            .Select(x => x.Clone())
            .OrderBy(x => x.Name));
    }

    private async Task AddSelectedLinesToGlobalVariablesAsync(Window window)
    {
        if(SelectedGridLines == null) return;
        if(AppService.Session.GridLines == null) return;
        if (SelectedGridLines.Count <= 0)
        {
            MessageService.NormalMessageBoxClassic("Es wurden keine Zeilen ausgewählt, welche gespeichert werden könnten!");
            return;
        }
        if(IsDesignView)
            return;

        IsTaskActive = true;

        string personPrefix = string.Empty;
        foreach (GridLine gridLine in SelectedGridLines)
        {
            personPrefix += gridLine.Id;
            personPrefix += ",";
        }
        personPrefix = personPrefix.TrimEnd(',');
        
        CancellationToken cancellationToken = new CancellationToken();
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBMiscService.SaveGridLinePrefixForHomeOverViewAsync(cancellationToken, personPrefix, GlobalVariables.HomeViewGrid , AppService.User.Id);
        }, cancellationToken);
        switch (result)
        {
            case (int)ErrorEnum.Success:
                MBGlobalVariables? newPrefix = await MBMiscService.GetGlobalVariableAsync(cancellationToken,
                    GlobalVariables.HomeViewGrid, AppService.User.Id);
                if (newPrefix != null)
                {
                    var savedValue = AppService.Session.GlobalVariables.FirstOrDefault(x => x.Id == newPrefix.Id && x.UserId == AppService.User.Id);
                    if (savedValue == null)
                    {
                        AppService.Session.GlobalVariables.Add(newPrefix);
                    }
                    else
                    {
                        savedValue.ValueAsString = newPrefix.ValueAsString;
                    }
                    IsTaskActive = false;
                    window.Close(SelectedGridLines);
                }
                break;
            case (int)ErrorEnum.ValueNotChanged:
                MessageService.NormalMessageBoxClassic("Es wurde keine Änderung festgestellt!");
                break;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Es ist ein Fehler aufgetreten!", (int)ErrorEnum.Aborted);
                break;
        }
        IsTaskActive = false;

    }

    private void AddSelectedGridLine()
    {
        if(GridLinePreview == null) return;
        if(SelectedPreviewGridLine == null) return;

        bool isLineFound = false;

        foreach (GridLine gridLine in SelectedGridLines)
        {
            if(gridLine.Id == SelectedPreviewGridLine.Id)
                isLineFound = true;
        }

        if (isLineFound)
        {
            MessageService.NormalMessageBoxClassic("Zeile wurde bereits hinzugefügt!");
            return;
        }
        SelectedGridLines.Add(SelectedPreviewGridLine);
    }

    private void RemoveSelectedGridLine()
    {
        if(SelectedGridLines == null) return;
        if(SelectedGridLine == null) return;
        if (SelectedGridLines.Count <= 0)
        {
            MessageService.NormalMessageBoxClassic("Es gibt keine Zeile, welche entfernt werden kann!");
            return;
        }
        SelectedGridLines.Remove(SelectedGridLine);
    }

    private void AddSavedLines()
    {
        if(AppService.Session.GridLines == null) return;
        if(AppService.User == null) return;
        if(AppService.Session.GlobalVariables == null) return;
        
        MBGlobalVariables? savedLines = AppService.Session.GlobalVariables.FirstOrDefault(x => x.UserId == AppService.User.Id && x.Id.Equals("OverViewGridLines"));
        if (savedLines == null)
        {
            MessageService.NormalMessageBoxClassic("Es wurden keine gespeicherten Zeilen gefunden!");
            return;
        }

        if (string.IsNullOrWhiteSpace(savedLines.ValueAsString))
        {
            MessageService.NormalMessageBoxClassic("Es wurden keine gespeicherten Zeileneinträge gefunden!");
            return;
        }
        List<int>? lineIds = savedLines.ValueAsString.Split(',').Select(int.Parse).ToList();
        if (lineIds == null)
        {
            MessageService.WarningMessageBoxClassic("Bei dem Laden der Zeilen ist ein Formatierungsfehler aufgetreten!", (int)ErrorEnum.Aborted);
            return;
        }
        SelectedGridLines = new ObservableCollection<GridLine>();
        foreach (int id in lineIds)
        {
            var gridline = AppService.Session.GridLines.FirstOrDefault(x => x.Id == id);
            if(gridline == null)
                continue;
            SelectedGridLines.Add(gridline.Clone());
        }
    }

    private void AddOutgoingLines()
    {
        if(AppService.Session.GridLines == null) return;
        if(AppService.User == null) return;
        
        SelectedGridLines = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id 
                && x.LineTypId == (int)LineTypeEnum.Ausgaben 
                && x.ValidFrom <= DateOnly.FromDateTime(DateTime.Now) 
                && x.ValidTo >= DateOnly.FromDateTime(DateTime.Now))
            .Select(x => x.Clone())
            .OrderBy(x => x.Number));
    }
    private void AddDebitLines()
    {
        if(AppService.Session.GridLines == null) return;
        if(AppService.User == null) return;
        
        SelectedGridLines = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id 
            && x.LineCategoryId.Equals("Kontostand")
            && x.ValidFrom <= DateOnly.FromDateTime(DateTime.Now) 
            && x.ValidTo >= DateOnly.FromDateTime(DateTime.Now))
            .Select(x => x.Clone())
            .OrderBy(x => x.Number));
    }

    private void RemoveAllSelectedLines()
    {
        if(SelectedGridLines != null)
            SelectedGridLines.Clear();
    }

    partial void OnSearchTextChanged(string value)
    {
        if(IsDesignView)
            return;
        if(value == null) return;
        if(GridLinePreview == null)return;
        if(AppService.Session.GridLines == null) return;
        if(AppService.User == null) return;

        GridLinePreview = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id
                && x.Name.ToLower().Contains(value.ToLower())
                && x.ValidFrom <= DateOnly.FromDateTime(DateTime.Now) 
                && x.ValidTo >= DateOnly.FromDateTime(DateTime.Now))
            .Select(x => x.Clone())
            .OrderBy(x => x.Name));
    }

    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private ObservableCollection<GridLine> _gridLinePreview;
    [ObservableProperty] private ObservableCollection<GridLine> _selectedGridLines;
    [ObservableProperty] private GridLine _selectedPreviewGridLine;
    [ObservableProperty] private GridLine _selectedGridLine;
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private bool _isDesignView;

    [ObservableProperty] private bool _isTaskActive;

    [ObservableProperty] private ICommand _removeSelectedGridLineCmd;
    [ObservableProperty] private ICommand _addSelectedGridLineCmd;
    [ObservableProperty] private ICommand _removeAllLinesCmd;
    [ObservableProperty] private ICommand _addDebitLinesCmd;
    [ObservableProperty] private ICommand _addOutgoingLinesCmd;
    [ObservableProperty] private ICommand _addSavedLinesCmd;

    [ObservableProperty] private ICommand _saveSelectedLinesToDBCmd;


}