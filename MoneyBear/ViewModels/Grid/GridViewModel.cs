using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.IdentityModel.Tokens;
using MoneyBear.DesignData;
using MoneyBear.Models;
using MoneyBear.Services;
using MoneyBear.ViewModels.Import;
using MoneyBear.Views.Grid;
using MoneyBear.Views.Import;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels;

public partial class GridViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    public GridViewModel(AppService appService)
    {
        AppService = appService;
        IsDesignView = false;
        SetDefaultDateForDatePicker();
        SetDefaultDataForGridMonthCB();
        SetDefaultDataForBuchungsFilterCB();
        SetDefaultDataForGridCategoriesItemsCB();
        if(AppService.Session.GridLines != null)
            TempGridViewData = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id 
                && x.ValidFrom <= DateOnly.FromDateTime(DateTime.Now)
                && x.ValidTo >= DateOnly.FromDateTime(DateTime.Now)));
        else
            TempGridViewData = new ObservableCollection<GridLine>();

        CreateGridCmd = new RelayCommand<object>(async _ => await CreateGridWithSettings());
        //RefreshGridCmd = new RelayCommand<object>(_ => RefreshGridWithSettings());
        ImportData = new RelayCommand<object>(_ => LoadImportDataWindow());
        AddNewGridLineCmd = new RelayCommand<object>(_ => LoadLineAddingWindow());
        EditGridLineCmd = new RelayCommand<object>(_ => LoadEditGridLineWindow());
        ImportDebitData = new RelayCommand<object>(_ => LoadImportDebitLineWindow());
        ImportSelectedData = new RelayCommand<object>(_ => LoadSelectedImportDataView());
        
        InitCommentSection();
        SaveCommentCmd = new RelayCommand<object>(async _ => await SaveComment());
        DeleteCommentCmd = new RelayCommand<object>(_ => DeleteComment());
        
    }

    public GridViewModel()
    {
        //AppService = AppService.Instance;
        IsDesignView = true;
        SetDefaultDateForDatePicker();
        SetDefaultDataForGridMonthCB();
        SetDefaultDataForBuchungsFilterCB();
        //SetDefaultDataForGridCategoriesItemsCB();
        SampleGridData sampleGridData = new SampleGridData();
        GridViewData = sampleGridData.GetSampleGridData();
        TempGridViewData = GridViewData;
        
        InitCommentSection();
    }

    private void InitCommentSection()
    {
        if (IsDesignView)
        {
            CurrentCommentCount = "0";
            CommentWarningMessage = "Keine Zeile wurde ausgewählt!";
            TestComment = "Lorem Ipsum is simply dummy text of the printing and " +
                          "typesetting industry. Lorem Ipsum has been the industry's standard " +
                          "dummy text ever since the 1500s, when an unknown printer took a galley " +
                          "of type and scrambled it to make a type specimen book. It has survived " +
                          "not only five centuries, but also the leap into electronic typesetting, " +
                          "remaining essentially unchanged. It was popularised in the 1960s with the " +
                          "release of Letraset sheets containing Lorem Ipsum passages, and more recently" +
                          " with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.";
        }
        else
        {
            CurrentCommentCount = string.Empty;
            CommentWarningMessage = string.Empty;
        }
        CurrentGridLineComment = string.Empty;
        CurrentGridLineName = string.Empty;
        CurrentGridLineMonth = 0;
        CurrentGridLineYear = DateTime.Now.Year;
        IsCommentWarningVisible = false;
        IsCommentEditingAllowed = true;
        IsSavingAllowed = false;
    }

    public void DefaultCommentSection()
    {
        CurrentGridLineComment = string.Empty;
        CurrentGridLineName = string.Empty;
        CurrentGridLineMonth = 0;
        CurrentGridLineYear = DateTime.Now.Year;
        IsCommentWarningVisible = true;
        CommentWarningMessage = "Selektieren sie den Wert einer Zeile um ein Kommentar eingeben zu können!";
        IsSavingAllowed = false;
    }

    public async Task<int> LoadCommentData(GridLine line, int month, int year)
    {
        CurrentGridLine = line;
        CurrentGridLineName = line.Name ?? string.Empty;
        CurrentGridLineMonth = month;
        CurrentGridLineYear = year;
        IsCommentWarningVisible = false;
        AppService.SetTaskProgressInfo("Kommentar wird geladen");

        CancellationToken cancellationToken = new CancellationToken();
        string result = string.Empty;
        await Task.Run(async () =>
        {
            result = await MBGridService.GetCommentForGridLine(cancellationToken, line.Id, month, year,
                AppService.User.Id);

        }, cancellationToken);
        if (result == "null")
        {
            IsSavingAllowed = false;
            IsCommentWarningVisible = true;
            CommentWarningMessage = "Für die ausgewählte Zeile muss ein Import durchgeführt werden!";
            AppService.DisableTaskProgressInfo();
            return (int)ErrorEnum.NullReference;
        }
        //TODO: Kommentar ist jetzt entweder vorhanden, wenn länge größer 0 ist oder halt ""
        CurrentGridLineComment = result;
        CurrentCommentCount = CurrentGridLineComment.Length.ToString();
        if (CurrentGridLineComment.Length < 1024)
        {
            IsSavingAllowed = true;
            IsCommentWarningVisible = false;
        }
        else
        {
            IsSavingAllowed = false;
            IsCommentWarningVisible = true;
            CommentWarningMessage = "Maximale Anzahl an Zeichen wurde überschritten!";
        }
        AppService.DisableTaskProgressInfo();
        return (int)ErrorEnum.Success;
    }

    private async Task SaveComment()
    {
        if (CurrentGridLine == null)
        {
            IsSavingAllowed = false;
            IsCommentWarningVisible = true;
            CommentWarningMessage = "Keine gültige Zeile ausgewählt!";
            return;
        }
        if (CurrentGridLineComment.Length > 1024)
        {
            IsSavingAllowed = false;
            IsCommentWarningVisible = true;
            CommentWarningMessage = "Maximale Anzahl an Zeichen wurde überschritten!";
            return;
        }
        
        if(AppService == null) return;
        if (AppService.IsTaskProgressRingVisible)
        {
            IsSavingAllowed = false;
            IsCommentWarningVisible = true;
            CommentWarningMessage = "Bitte warten Sie bis der geänderte Wert gespeichert wurde!";
            return;
        }
        AppService.SetTaskProgressInfo("Kommentar wird gespeichert!");
        IsCommentEditingAllowed = false;
        CancellationToken cancellationToken = new CancellationToken();
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBGridService.SaveCommentForGridLine(cancellationToken, CurrentGridLine.Id, CurrentGridLineComment, CurrentGridLineMonth, CurrentGridLineYear,
                AppService.User.Id);

        }, cancellationToken);
        switch (result)
        {
            case (int)ErrorEnum.NullReference:
                MessageService.WarningMessageBoxClassic("Kommentar konnte nicht gespeichert werden!", (int)ErrorEnum.NullReference);
                AppService.DisableTaskProgressInfo();
                IsCommentEditingAllowed = true;
                return;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Kommentar konnte nicht gespeichert werden!", (int)ErrorEnum.Aborted);
                AppService.DisableTaskProgressInfo();
                IsCommentEditingAllowed = true;
                return;
            case (int)ErrorEnum.Success:
                MessageService.NormalMessageBoxClassic("Kommentar wurde gespeichert!");
                break;
        }
        AppService.DisableTaskProgressInfo();
        IsCommentEditingAllowed = true;
    }

    private async Task DeleteComment()
    {
        if (CurrentGridLine == null)
        {
            IsSavingAllowed = false;
            IsCommentWarningVisible = true;
            CommentWarningMessage = "Keine gültige Zeile ausgewählt!";
            return;
        }
        CurrentGridLineComment = string.Empty;
        CurrentCommentCount = CurrentGridLineComment.Length.ToString();
        IsSavingAllowed = true;
        IsCommentWarningVisible = false;
    }

    partial void OnCurrentGridLineCommentChanged(string value)
    {
        if(value == null)
            return;
        
        if (value.Length > 1024)
        {
            if (CommentWarningMessage != null)
                CommentWarningMessage = "Maximale Anzahl an Zeichen wurde überschritten!";
            if(IsSavingAllowed != null)
                IsSavingAllowed = false;
            if(IsCommentWarningVisible != null)
                IsCommentWarningVisible = true;
            if(CurrentCommentCount != null)
                CurrentCommentCount = value.Length.ToString();
        }
        else
        {
            if (IsSavingAllowed != null)
                IsSavingAllowed = true;
            if(IsCommentWarningVisible != null)
                IsCommentWarningVisible = false;
            if(CurrentCommentCount != null)
                CurrentCommentCount = value.Length.ToString();
        }
    }

    private async Task LoadSelectedImportDataView()
    {
        if(AppService.Session.GridLines == null || AppService.Session.LineCalculations == null)
            return;
        
        var importSelectedLinesView = new GridImportSelectedLinesView()
        {
            DataContext = new GridImportSelectedLinesViewModel(AppService)
        };
        var result = await importSelectedLinesView.ShowDialog<DateTime>(CurrentWindow);
        if (result != null)
        {
            await CreateGridWithSettings();
            IsTaskActive = true;
            CancellationToken cancellationToken = new CancellationToken();
            int sumResult = await MBGridService.RecalculateSumLinesForSpecificMonthAsync(
                result.Month, result.Year, AppService.User.Id,
                AppService.Session.GridLines, AppService.Session.LineCalculations, cancellationToken);
            if (sumResult == (int)ErrorEnum.Success)
            {
                AppService.DisableTaskProgressInfo();
                IsTaskActive = false;
            }
        }
    }
    
    private async Task LoadImportDebitLineWindow()
    {
        if(AppService.Session.GridLines == null || AppService.Session.LineCalculations == null)
            return;
        
        var debitLineImportView = new GridDebitLineImportView()
        {
            DataContext = new GridDebitLineImportViewModel(AppService)
        };
        var result = await debitLineImportView.ShowDialog<ObservableCollection<DateTime>>(CurrentWindow);
        if (result != null)
        {
            await CreateGridWithSettings();
            IsTaskActive = true;
            CancellationToken cancellationToken = new CancellationToken();
            if (result.Count <= 0)
                return;
            foreach (DateTime dateTime in result)
            {
                int sumResult = await MBGridService.RecalculateSumLinesForSpecificMonthAsync(
                    dateTime.Month, dateTime.Year, AppService.User.Id,
                    AppService.Session.GridLines, AppService.Session.LineCalculations, cancellationToken);
                if (sumResult == (int)ErrorEnum.Aborted)
                {
                    MessageService.WarningMessageBoxClassic($"Bei der Berechnung für Monat: {dateTime.Month}/{dateTime.Year} ist ein Fehler aufgetreten!", (int)ErrorEnum.Aborted);
                }
            }
            AppService.DisableTaskProgressInfo();
            IsTaskActive = false;

        }
    }

    private async Task LoadLineAddingWindow()
    {
        if(AppService.Session.GridLines == null || AppService.Session.LineCalculations == null)
            return;
        
        var gridAddLineView = new GridAddLineView()
        {
            DataContext = new GridAddLineViewModel("Grid-Zeile hinzufügen!", AppService)
        };
        var result = await gridAddLineView.ShowDialog<GridLine>(CurrentWindow);
        if (result != null)
        {
            await CreateGridWithSettings();
            IsTaskActive = true;
            CancellationToken cancellationToken = new CancellationToken();
            int sumResult = await MBGridService.RecalculateSumLinesForSpecificMonthAsync(
                result.ValidFrom.Month, result.ValidFrom.Year, AppService.User.Id,
                AppService.Session.GridLines, AppService.Session.LineCalculations, cancellationToken);
            if (sumResult == (int)ErrorEnum.Success)
            {
                AppService.DisableTaskProgressInfo();
                IsTaskActive = false;
            }
        }
    }
    
    private async Task LoadEditGridLineWindow()
    {
        if (AppService.Session.GridLines == null || AppService.Session.LineCalculations == null)
        {
            MessageService.WarningMessageBoxClassic("Es wurden keine Grid-Zeilen gefunden, welche bearbeitet werden können!", (int)ErrorEnum.NoMatchFound);
            return;
        }
        DateTime currentDate = DateTime.Now;
        DateOnly date = DateOnly.FromDateTime(currentDate);
        var previewGridLines = new ObservableCollection<GridLine>();
        foreach (var line in AppService.Session.GridLines.Where(x => x.Userid == AppService.User.Id && ((x.ValidFrom.Month <= date.Month && x.ValidFrom.Year <= date.Year || x.ValidFrom.Year < date.Year)) 
                     && (x.ValidTo.Month >= date.Month && x.ValidTo.Year >= date.Year)))
        {
            previewGridLines.Add(line.Clone());
        }
        previewGridLines = new ObservableCollection<GridLine>(previewGridLines.OrderBy(x => x.Number));
        if (previewGridLines.Count <= 0)
        {
            MessageService.WarningMessageBoxClassic("Es wurden keine aktuellen Zeilen gefunden!"+ Environment.NewLine+"Bitte import durchführen!", (int)ErrorEnum.NoValidImport);
            return;
        }

        var editView = new GridEditLineView()
        {
            DataContext = new GridEditLineViewModel("Grid-Zeile bearbeiten", AppService),
        };
        var result = await editView.ShowDialog<GridLine>(CurrentWindow);
        if (result != null)
        {
            await CreateGridWithSettings();
            IsTaskActive = true;
            CancellationToken cancellationToken = new CancellationToken();
            int sumResult = await MBGridService.RecalculateSumLinesForSpecificMonthAsync(
                SelectedGridDate.Month, SelectedGridDate.Year, AppService.User.Id,
                AppService.Session.GridLines, AppService.Session.LineCalculations, cancellationToken);
            if (sumResult == (int)ErrorEnum.Success)
            {
                AppService.DisableTaskProgressInfo();
                IsTaskActive = false;
            }
        }
    }

    private async Task LoadImportDataWindow()
    {
        if(AppService.Session.GridLines == null || AppService.Session.LineCalculations == null)
            return;
        
        var importView = new GridLineImportView()
        {
            DataContext = new GridLineImportViewModel("Import", AppService)
        };
        var result = await importView.ShowDialog<DateTime>(CurrentWindow);
        if (result != null)
        {
            IsTaskActive = true;
            CancellationToken cancellationToken = new CancellationToken();
            int sumResult = await MBGridService.RecalculateSumLinesForSpecificMonthAsync(
                result.Month, result.Year, AppService.User.Id,
                AppService.Session.GridLines, AppService.Session.LineCalculations, cancellationToken);
            if (sumResult == (int)ErrorEnum.Success)
            {
                AppService.DisableTaskProgressInfo();
                IsTaskActive = false;
            }
        }
    }

    // private void RefreshGridWithSettings()
    // {
    //     //
    // }

    private async Task CreateGridWithSettings()
    {
        if(IsDesignView)
            return;
        
        if (AppService == null) return;
        if (AppService.Session == null) return;
        if (AppService.Session.GridLines == null) return;
        if (AppService.Session.GridLines.Count <= 0) return;
        if (AppService.Session.GridStorages == null) return;
        if(AppService.Session.GridStorages.Count <= 0) return;
        if (SelectedGridDate == null) return;
        
        AppService.SetTaskProgressInfo("Grid wird erstellt!");
        
        ObservableCollection<GridLine> linesToDisplay = new ObservableCollection<GridLine>(AppService.Session.GridLines
            .Where(x => (x.ValidFrom.Month <= (SelectedGridDate.Date.Month+SelectedGridMonthPreviewItemsCBIndex) && x.ValidFrom.Year == SelectedGridDate.Date.Year || x.ValidFrom.Year < SelectedGridDate.Date.Year) 
                        && (x.ValidTo.Month >= (SelectedGridDate.Date.Month+SelectedGridMonthPreviewItemsCBIndex) && x.ValidTo.Year == SelectedGridDate.Date.Year || x.ValidTo.Year > SelectedGridDate.Date.Year)
                && x.ValidFrom.Year <= SelectedGridDate.Date.Year && x.ValidTo.Year >= DateTime.Now.Year && x.Userid == AppService.User.Id).OrderBy(n => n.Number));
        
        if (linesToDisplay.Count == 0)
        {
            MessageService.WarningMessageBoxClassic("Grid-Zeilen konnten nicht geladen werden!",(int)ErrorEnum.NoMatchFound);
            GridViewData = new ObservableCollection<GridLine>();
            AppService.DisableTaskProgressInfo();
        }
        else
        {
            GridViewData = linesToDisplay;
            TempGridViewData = new ObservableCollection<GridLine>(GridViewData.Select(x => x.Clone()));
            AppService.DisableTaskProgressInfo();
        }
    }

    public async Task<int> SaveGridValueChanges(decimal value, int lineId, int month, int year)
    {
        CancellationToken cancellationToken = new CancellationToken();
        int valueChangedResult = await MBGridService.CheckIfGridLineValueChanged(lineId, value, month, year, cancellationToken);
        if (valueChangedResult == (int)ErrorEnum.ValueNotChanged)
            return (int)ErrorEnum.NoMatchFound;
        if (valueChangedResult == (int)ErrorEnum.NullReference)
        {
            MessageService.WarningMessageBoxClassic("Grid-Zeile existiert nicht, führe einen Import für den Monat aus!",(int)ErrorEnum.NullReference);
            return (int)ErrorEnum.NullReference;
        }
        IsTaskActive = true;
        AppService.SetTaskProgressInfo("Speichere Werte!");
        int result = await MBGridService.SaveValueForSelectedGridLineAsync(lineId, value, month, year, cancellationToken);
        if (result == (int)ErrorEnum.Success)
        {
            AppService.SetTaskProgressInfo("Berechne Summen!");
            int sumResult = await MBGridService.RecalculateSumLinesForSpecificMonthAsync(
                month, year, AppService.User.Id,
                AppService.Session.GridLines, AppService.Session.LineCalculations, cancellationToken);
            if (sumResult == (int)ErrorEnum.Success)
            {
                AppService.DisableTaskProgressInfo();
                IsTaskActive = false;
                return (int)ErrorEnum.Success;
            }
        }
        AppService.DisableTaskProgressInfo();
        IsTaskActive = false;
        return (int)ErrorEnum.Aborted;
    }

    public void SetDefaultDataForGridMonthCB()
    {
        GridMonthPreviewItemsCB = new ObservableCollection<string>();
        int currentMonth = SelectedGridDate.Month;
        int itemsToAdd = 12 - currentMonth;
        GridMonthPreviewItemsCB.Add("+0 Monat(e)");
        for (int i = 1; i <= itemsToAdd; i++)
        {
            GridMonthPreviewItemsCB.Add("+"+(i)+" Monat(e)");
        }
        SelectedGridMonthPreviewItemsCBIndex = 0;
        CurrentMonthPreview = MonthPreviewExtensions.GetMonthPreview(SelectedGridMonthPreviewItemsCBIndex);
    }

    partial void OnSelectedGridDateChanged(DateTime time)
    {
        SetDefaultDataForGridMonthCB();
    }

    partial void OnSelectedGridMonthPreviewItemCBChanged(string value)
    {
        CurrentMonthPreview = MonthPreviewExtensions.GetMonthPreview(SelectedGridMonthPreviewItemsCBIndex);
    }

    private void SetDefaultDateForDatePicker()
    {
        DisplayDateStart = new DateTime(DateTime.Now.Year, 1, 1);
        DisplayDateEnd = new DateTime(DateTime.Now.Year, 12, 31);
        SelectedGridDate = DateTime.Now;
    }
    
    private void SetDefaultDataForBuchungsFilterCB()
    {
        BuchungsFilterGridItemsCB = new ObservableCollection<string>()
        {
            "Kein Filter",
            "Bis 15. (Mitte)",
            "15. bis 31",
        };
        BuchungsFilterGridItemIndex = 0;
    }

    private void SetDefaultDataForGridCategoriesItemsCB()
    {
        if(IsDesignView)
            return;
        
        if(AppService == null) return;
        if (AppService.Session == null) return;
        if (AppService.Session.LineCategories == null) return;
        if (AppService.Session.LineCategories.Count <= 0)
        {
            MessageService.WarningMessageBoxClassic("Keine Kategorien gefunden!", (int)ErrorEnum.NoMatchFound);
            return;
        }
        GridLineCategoriesItemsCB = AppService.Session.LineCategories;
        var defaultCat = AppService.Session.LineCategories.FirstOrDefault(x => x.Id == "Kein Filter");
        if (defaultCat == null)
        {
            LineCategory fCat = new LineCategory()
            {
                Id = "Kein Filter",
            };
            AppService.Session.LineCategories.Add(fCat);
        }
        GridLineCategoryIndex = AppService.Session.LineCategories.IndexOf(AppService.Session.LineCategories.First(x => x.Id == "Kein Filter"));
            
    }

    partial void OnSearchGridFilterChanged(string text)
    {
        if(text == null)
            return;
        
        if(IsDesignView)
            return;
        
        if (text.IsNullOrEmpty() || text.Length <= 0)
        {
            GridViewData = TempGridViewData;
            return;
        }
        GridViewData = new ObservableCollection<GridLine>(TempGridViewData.Where(x => x.Name.ToLower().Contains(text.ToLower())));
    }

    partial void OnSelectedBuchungsFilterGridItemChanged(string buchungsFilter)
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
                GridViewData = TempGridViewData;
                break;
            case 1:
                GridViewData = new ObservableCollection<GridLine>(TempGridViewData.Where(x => x.ValidFrom.Day <= 15).Select(x => x.Clone()));
                break;
            case 2:
                GridViewData = new ObservableCollection<GridLine>(TempGridViewData.Where(x => x.ValidFrom.Day >= 15).Select(x => x.Clone()));
                break;
        }
    }

    partial void OnSelectedGridLineCategoryChanged(LineCategory category)
    {
        if (category == null)
            return;
        
        if(IsDesignView)
            return;
        
        if (category.Id == "Kein Filter")
        {
            GridViewData = TempGridViewData;
            return;
        }
        GridViewData = new ObservableCollection<GridLine>(TempGridViewData.Where(x => x.LineCategoryId.ToLower() == category.Id.ToLower()));
    }
    
    [ObservableProperty] private ObservableCollection<GridLine> _gridViewData;
    [ObservableProperty] private ObservableCollection<GridLine> _tempGridViewData;
    [ObservableProperty] private DataGrid _currrentDataGrid;
    [ObservableProperty] private MonthPreview _currentMonthPreview;
    [ObservableProperty] private LineCategory _selectedGridLineCategory;
    [ObservableProperty] private int _gridLineCategoryIndex;
    [ObservableProperty] private ObservableCollection<LineCategory> _gridLineCategoriesItemsCB;
    [ObservableProperty] private string _selectedBuchungsFilterGridItem;
    [ObservableProperty] private int _buchungsFilterGridItemIndex;
    [ObservableProperty] private ObservableCollection<string> _buchungsFilterGridItemsCB;
    [ObservableProperty] private string _searchGridFilter;
    [ObservableProperty] private ICommand _refreshGridCmd;
    [ObservableProperty] private ICommand _createGridCmd;
    [ObservableProperty] private string _selectedGridMonthPreviewItemCB;
    [ObservableProperty] private int _selectedGridMonthPreviewItemsCBIndex;
    [ObservableProperty] private ObservableCollection<string> _gridMonthPreviewItemsCB;
    [ObservableProperty] private DateTime _selectedGridDate;
    [ObservableProperty] private DateOnly _dateToDisplay;
    [ObservableProperty] private DateTime _displayDateStart;
    [ObservableProperty] private DateTime _displayDateEnd;
    [ObservableProperty] private bool _isTaskActive;
    [ObservableProperty] private bool _isDesignView;

    [ObservableProperty] private Window _currentWindow;

    #region Comment Section

    [ObservableProperty] private GridLine _currentGridLine;
    [ObservableProperty] private int _currentGridLineMonth;
    [ObservableProperty] private int _currentGridLineYear;
    [ObservableProperty] private string _currentGridLineName;
    [ObservableProperty] private string _currentGridLineComment;
    [ObservableProperty] private string _testComment;
    [ObservableProperty] private string _currentCommentCount;
    [ObservableProperty] private string _commentWarningMessage;
    [ObservableProperty] private bool _isCommentWarningVisible;
    [ObservableProperty] private bool _isSavingAllowed;
    [ObservableProperty] private bool _isCommentEditingAllowed;

    [ObservableProperty] private ICommand _saveCommentCmd;
    [ObservableProperty] private ICommand _deleteCommentCmd;
    #endregion
    
    
    [ObservableProperty] private ICommand _addNewGridLineCmd;
    [ObservableProperty] private ICommand _editGridLineCmd;
    [ObservableProperty] private ICommand _importData;
    [ObservableProperty] private ICommand _importDebitData;
    [ObservableProperty] private ICommand _importSelectedData;
}