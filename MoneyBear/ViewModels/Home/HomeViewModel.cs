using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MoneyBear.Assets;
using MoneyBear.DesignData;
using MoneyBear.Models;
using MoneyBear.Services;
using MoneyBear.ViewModels.Home;
using MoneyBear.Views.Grid;
using MoneyBear.Views.Home;
using MsBox.Avalonia.Enums;
using NASPLOID.Models.MoneyBear;
using SkiaSharp;
using Color = Avalonia.Media.Color;

namespace MoneyBear.ViewModels;

public partial class HomeViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public HomeViewModel(AppService appService)
    {
        AppService = appService;
        //InitChart();
        SampleGridData sampleGridData = new SampleGridData();
        GridPreviewData = sampleGridData.GetSampleGridData();
        PieSeries = new ObservableCollection<ISeries>();
        CategoryChart = new ObservableCollection<ISeries>();
        SetUpGrid();
        SetUpPieChart();
        SetUpCategoryChart();
        CartesianChartPosition = "Left";
        IsCartesianChartDisplayed = true;
        CartesianChartLegendCmd = new RelayCommand<object>(_ => ShowCartesianChartLegend());
        
        LoadColorEditorCmd = new RelayCommand<object>(_ => LoadHomeChartColorEditorView());
        LoadPieChartColorEditorCmd = new RelayCommand<object>(async _ => await LoadPieChartColorEditorView());
        LoadGridLinePreviewEditorCmd = new RelayCommand<object>(async _ => await LoadGridPreviewEditorView());
        RefreshDataCmd = new RelayCommand<object>(_ => RefreshData());
    }

    public HomeViewModel()
    {
        InitChart();
        SampleGridData sampleGridData = new SampleGridData();
        GridPreviewData = sampleGridData.GetSampleGridData();
        IsJanuarValueDisplayed = true;
    }

    private void RefreshData()
    {
        SetUpGrid();
        SetUpPieChart();
        SetUpCategoryChart();
    }

    private void ShowCartesianChartLegend()
    {
        IsCartesianChartDisplayed = !IsCartesianChartDisplayed;
        if (!IsCartesianChartDisplayed)
            CartesianChartPosition = "Hidden";
        else
            CartesianChartPosition = "Left";
    }
    
    private async Task LoadPieChartColorEditorView()
    {
        var piecharView = new HomePieChartColorEditorView()
        {
            DataContext = new HomePieChartColorEditorViewModel(AppService, "Einnahmen/Ausgaben anpassen")
        };
        var result = await piecharView.ShowDialog<PieChartColorSet>(CurrentWindow);
        if (result != null)
        {
            DisplayedIncomeColor = result.IncomeColor;
            DisplayedOutgoingColor = result.OutgoingColor;
            SetUpPieChart();
        }
    }

    private async Task LoadHomeChartColorEditorView()
    {
        var outgoingColors = new HomeChartColorEditorView()
        {
            DataContext = new HomeChartColorEditorViewModel(AppService, "Ausgaben anpassen")
        };
        var result = await outgoingColors.ShowDialog<LineCategory>(CurrentWindow);
        if (result != null)
        {
            SetUpCategoryChart();
        }
    }

    private async Task LoadGridPreviewEditorView()
    {
        var overViewEditorView = new HomeOverViewEditorView()
        {
            DataContext = new HomeOverViewEditorViewModel(AppService, "Zeilenvorschau-Editor")
        };
        var result = await overViewEditorView.ShowDialog<ObservableCollection<GridLine>>(CurrentWindow);
        if (result != null)
        {
            GridPreviewData = new ObservableCollection<GridLine>(result);
        }
    }

    private void InitChart()
    {
        SetColorForPieChart();
        PieSeries =
        [
            new PieSeries<double>
            {
                Values = new double[] { 2058 },
                Name = "Einnahmen",
                Fill = new SolidColorPaint(new SKColor(DisplayedIncomeColor.R, DisplayedIncomeColor.G, DisplayedIncomeColor.B))
            },

            new PieSeries<double>
            {
                Values = new double[] { 1788 },
                Name = "Ausgaben",
                Fill = new SolidColorPaint(new SKColor(DisplayedOutgoingColor.R, DisplayedOutgoingColor.G, DisplayedOutgoingColor.B))
            }

        ];
    }
    
    private void SetColorForPieChart()
    {
        if(AppService.Session.GlobalVariables == null)
            return;
        if(AppService.User.Id == null)
            return;
        
        MBGlobalVariables? colorPresetIncome = AppService.Session.GlobalVariables.FirstOrDefault(x => x.UserId == AppService.User.Id && x.Id.Equals(GlobalVariables.HomePieChartIncomeColor));
        MBGlobalVariables? colorPresetOutgoing = AppService.Session.GlobalVariables.FirstOrDefault(x => x.UserId == AppService.User.Id && x.Id.Equals(GlobalVariables.HomePieChartOutgoingColor));

        if (colorPresetIncome == null && colorPresetOutgoing == null)
        {
            DisplayedIncomeColor = Colors.DarkGreen;
            DisplayedOutgoingColor = Colors.DarkRed;
            return;
        }
        if(colorPresetIncome == null)
            DisplayedIncomeColor = Colors.DarkGreen;
        if(colorPresetOutgoing == null)
            DisplayedOutgoingColor = Colors.DarkRed;

        if(colorPresetIncome != null && string.IsNullOrWhiteSpace(colorPresetIncome.ValueAsString))
            DisplayedIncomeColor = Colors.DarkGreen;
        if(colorPresetOutgoing != null && string.IsNullOrWhiteSpace(colorPresetOutgoing.ValueAsString))
            DisplayedOutgoingColor = Colors.DarkRed;

        if (colorPresetIncome != null && !string.IsNullOrWhiteSpace(colorPresetIncome.ValueAsString))
        {
            List<int> splitRgbValues = colorPresetIncome.ValueAsString.Split(',').Select(int.Parse).ToList();
            DisplayedIncomeColor = new Color(255, (byte)splitRgbValues[0], (byte)splitRgbValues[1], (byte)splitRgbValues[2]);
        }
        if (colorPresetOutgoing != null && !string.IsNullOrWhiteSpace(colorPresetOutgoing.ValueAsString))
        {
            List<int> splitRgbValues = colorPresetOutgoing.ValueAsString.Split(',').Select(int.Parse).ToList();
            DisplayedOutgoingColor = new Color(255, (byte)splitRgbValues[0], (byte)splitRgbValues[1], (byte)splitRgbValues[2]);
        }
    }

    private async void SetUpGrid()
    {
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.Session.User == null)
            return;
        if(AppService.Session.GlobalVariables == null)
            return;

        GridPreviewData = new ObservableCollection<GridLine>();
        
        MBGlobalVariables? savedLines = AppService.Session.GlobalVariables.FirstOrDefault(x => x.Id.Equals("OverViewGridLines") && x.UserId == AppService.User.Id);
        if (savedLines == null)
        {
            MessageService.NormalMessageBoxClassic("Es wurden keine Zeilen zur Übersicht hinzugefügt! "+Environment.NewLine+"Dies ist unter Darstellung -> Zeilenvorschau anpassen möglich!");
            return;
        }

        if (string.IsNullOrWhiteSpace(savedLines.ValueAsString))
        {
            MessageService.NormalMessageBoxClassic("Es wurden keine Zeilen zur Übersicht hinzugefügt! "+Environment.NewLine+"Dies ist unter Darstellung -> Zeilenvorschau anpassen möglich!");
            return;
        }
        List<int>? lineIds = savedLines.ValueAsString.Split(',').Select(int.Parse).ToList();
        if (lineIds == null)
        {
            MessageService.WarningMessageBoxClassic("Bei den gespeicherten Zeilen gibt es ein Syntax Problem!", (int)ErrorEnum.Aborted);
            return;
        }
        foreach (int id in lineIds)
        {
            var gridline = AppService.Session.GridLines.FirstOrDefault(x => x.Id == id);
            if(gridline == null)
                continue;
            GridPreviewData.Add(gridline.Clone());
        }

        HideAllGridValueColumns();
        
        switch (DateTime.Now.Month)
        {
            #region Month Selection
            case (int)MonthEnum.Januar:
                IsJanuarValueDisplayed = true;
                break;
            case (int)MonthEnum.Februar:
                IsFebruarValueDisplayed = true;
                break;
            case (int)MonthEnum.März:
                IsMärzValueDisplayed = true;
                break;
            case (int)MonthEnum.April:
                IsAprilValueDisplayed = true;
                break;
            case (int)MonthEnum.Mai:
                IsMaiValueDisplayed = true;
                break;
            case (int)MonthEnum.Juni:
                IsJuniValueDisplayed = true;
                break;
            case (int)MonthEnum.Juli:
                IsJuliValueDisplayed = true;
                break;
            case (int)MonthEnum.August:
                IsAugustValueDisplayed = true;
                break;
            case (int)MonthEnum.September:
                IsSeptemberValueDisplayed = true;
                break;
            case (int)MonthEnum.Oktober:
                IsOktoberValueDisplayed = true;
                break;
            case (int)MonthEnum.November:
                IsNovemberValueDisplayed = true;
                break;
            case (int)MonthEnum.Dezember:
                IsDezemberValueDisplayed = true;
                break;
            #endregion
        }
    }

    private void HideAllGridValueColumns()
    {
        IsJanuarValueDisplayed = false;
        IsFebruarValueDisplayed = false;
        IsMärzValueDisplayed = false;
        IsAprilValueDisplayed = false;
        IsMaiValueDisplayed = false;
        IsJuniValueDisplayed = false;
        IsJuliValueDisplayed = false;
        IsAugustValueDisplayed = false;
        IsSeptemberValueDisplayed = false;
        IsOktoberValueDisplayed = false;
        IsNovemberValueDisplayed = false;
        IsDezemberValueDisplayed = false;
    }

    private void SetUpPieChart()
    {
        if(AppService.Session.GridLines == null)
            return;
        if(AppService.User == null)
            return;

        SetColorForPieChart();
        
        ObservableCollection<GridLine> incomeLines = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.LineTypId == (int)LineTypeEnum.Einnahmen && x.Userid == AppService.User.Id
        && x.ValidFrom <= DateOnly.FromDateTime(DateTime.Now) && x.ValidTo >= DateOnly.FromDateTime(DateTime.Now)).Select(x => x.Clone()));
        ObservableCollection<GridLine> outgoingLines = new ObservableCollection<GridLine>(AppService.Session.GridLines.Where(x => x.LineTypId == (int)LineTypeEnum.Ausgaben && x.Userid == AppService.User.Id
            && x.ValidFrom <= DateOnly.FromDateTime(DateTime.Now) && x.ValidTo >= DateOnly.FromDateTime(DateTime.Now)).Select(x => x.Clone()));

        var income = new PieSeries<double>()
        {
            Fill = new SolidColorPaint(new SKColor(DisplayedIncomeColor.R, DisplayedIncomeColor.G, DisplayedIncomeColor.B)),
            Name = "Einnahmen" 
        };
        var outgoing = new PieSeries<double>()
        {
            Fill = new SolidColorPaint(new SKColor(DisplayedOutgoingColor.R, DisplayedOutgoingColor.G, DisplayedOutgoingColor.B)),
            Name = "Ausgaben"
        };
        
        income.Values = new List<double>();
        outgoing.Values = new List<double>();
        decimal? incomeValue = GetSumOfSelectedGridLines(incomeLines);
        decimal? outgoingValue = GetSumOfSelectedGridLines(outgoingLines);
        
        income.Values.Add(decimal.ToDouble(incomeValue?? 0));
        outgoing.Values.Add(decimal.ToDouble(outgoingValue?? 0));

        PieSeries =
        [
            income,
            outgoing
        ];
    }

    private decimal GetSumOfSelectedGridLines(ObservableCollection<GridLine> lines)
    {
        decimal sum = 0;

        foreach (GridLine gridLine in lines)
        {
            switch (DateTime.Now.Month)
            {
                # region Month Selection
                case (int)MonthEnum.Januar:
                    sum += gridLine.JanuarValue?? 0;
                    break;
                case (int)MonthEnum.Februar:
                    sum += gridLine.FebruarValue ?? 0;
                    break;
                case (int)MonthEnum.März:
                    sum += gridLine.MärzValue ?? 0;
                    break;
                case (int)MonthEnum.April:
                    sum += gridLine.AprilValue ?? 0;
                    break;
                case (int)MonthEnum.Mai:
                    sum += gridLine.MaiValue ?? 0;
                    break;
                case (int)MonthEnum.Juni:
                    sum += gridLine.JuniValue ?? 0;
                    break;
                case (int)MonthEnum.Juli:
                    sum += gridLine.JuliValue ?? 0;
                    break;
                case (int)MonthEnum.August:
                    sum += gridLine.AugustValue ?? 0;
                    break;
                case (int)MonthEnum.September:
                    sum += gridLine.SeptemberValue ?? 0;
                    break;
                case (int)MonthEnum.Oktober:
                    sum += gridLine.OktoberValue ?? 0;
                    break;
                case (int)MonthEnum.November:
                    sum += gridLine.NovemberValue ?? 0;
                    break;
                case (int)MonthEnum.Dezember:
                    sum += gridLine.DezemberValue ?? 0;
                    break;
                #endregion
            }
        }
        return sum;
    }

    private async void SetUpCategoryChart()
    {
        CategoryChart = new ObservableCollection<ISeries>();
        
        if(AppService.Session.LineCategories == null)
            return;
        if(AppService.Session.GridLines == null)
            return;
        
        List<string> categoryChartLabels = new List<string>();
        
        ObservableCollection<GridLine> gridLines = new ObservableCollection<GridLine>(AppService.Session.GridLines
            .Where(x => x.Userid == AppService.User.Id && x.ValidFrom <= DateOnly.FromDateTime(DateTime.Now) && x.ValidTo >= DateOnly.FromDateTime(DateTime.Now))
            .Select(x => x.Clone()));

        foreach (LineCategory lineCategory in AppService.Session.LineCategories)
        {
            if(lineCategory.Id.ToLower().Equals("summe") || lineCategory.Id.ToLower().Equals("kontostand"))
                continue;
            var linesForEachCategory = new ObservableCollection<GridLine>(gridLines.Where(x => x.LineCategoryId.ToLower().Equals(lineCategory.Id.ToLower()) 
                && x.LineTypId == (int)LineTypeEnum.Ausgaben)
                .Select(x => x.Clone()));
            if (linesForEachCategory.Count > 0)
            {
                decimal sumOfCategory = CalculateCategorySumForSpecificMonth(linesForEachCategory, DateTime.Now.Month);
                if(sumOfCategory == 0)
                    continue;

                var newColumn = new ColumnSeries<double>()
                {
                    Name = lineCategory.Id,
                    Values = new List<double>()
                    {
                        decimal.ToDouble(sumOfCategory)
                    },
                    Fill = new SolidColorPaint(GetColorForSpecificCategory(lineCategory.Id))
                };
                CategoryChart.Add(newColumn);
                categoryChartLabels.Add(lineCategory.Id);
            }
        }
        
        CategoryXAxes = new ICartesianAxis[]
        {
            new Axis()
            {
                Name = "Ausgaben nach Kategorien",
                Labels = [],
                LabelsPaint = new SolidColorPaint(SKColors.Black)
            }
        };
        CategoryYAxes = new ICartesianAxis[]
        {
            new Axis()
            {
                ShowSeparatorLines = true,
                Labeler = Labelers.Currency,
                NamePadding = new LiveChartsCore.Drawing.Padding(0, 15),
                LabelsPaint = new SolidColorPaint
                {
                    Color = SKColors.Black,
                    SKFontStyle = new SKFontStyle(
                        SKFontStyleWeight.ExtraBold,
                        SKFontStyleWidth.Normal,
                        SKFontStyleSlant.Italic)
                }
            }
        };
    }

    private SKColor GetColorForSpecificCategory(string categoryId)
    {
        if (AppService.Session.ChartCategoryColors == null)
            return new SKColor(255, 255, 255);
        
        var savedColor = AppService.Session.ChartCategoryColors.FirstOrDefault(x => x.UserId == AppService.User.Id && x.CategoryId.ToLower().Equals(categoryId.ToLower()));
        if(savedColor == null)
            return new SKColor(255, 255, 255);
        
        return new SKColor((byte)savedColor.R, (byte)savedColor.G, (byte)savedColor.B);
    }

    private decimal CalculateCategorySumForSpecificMonth(ObservableCollection<GridLine> lines, int month)
    {
        decimal sum = 0;
        if (lines == null)
            return 0;
        if (lines.Count == 0)
            return 0;

        foreach (GridLine line in lines)
        {
            switch (month)
            {
                case (int)MonthEnum.Januar:
                    sum += line.JanuarValue ?? 0;
                    break;
                case (int)MonthEnum.Februar:
                    sum += line.FebruarValue ?? 0;
                    break;
                case (int)MonthEnum.März:
                    sum += line.MärzValue ?? 0;
                    break;
                case (int)MonthEnum.April:
                    sum += line.AprilValue ?? 0;
                    break;
                case (int)MonthEnum.Mai:
                    sum += line.MaiValue ?? 0;
                    break;
                case (int)MonthEnum.Juni:
                    sum += line.JuniValue ?? 0;
                    break;
                case (int)MonthEnum.Juli:
                    sum += line.JuliValue ?? 0;
                    break;
                case (int)MonthEnum.August:
                    sum += line.AugustValue ?? 0;
                    break;
                case (int)MonthEnum.September:
                    sum += line.SeptemberValue ?? 0;
                    break;
                case (int)MonthEnum.Oktober:
                    sum += line.OktoberValue ?? 0;
                    break;
                case (int)MonthEnum.November:
                    sum += line.NovemberValue ?? 0;
                    break;
                case (int)MonthEnum.Dezember:
                    sum += line.DezemberValue ?? 0;
                    break;
            }
        }
        return sum;
    }

    partial void OnSelectedColorChanged(Color value)
    {
        if(value == null)
            return;
        Console.WriteLine($"R: {value.R} | G: {value.G} | B: {value.B} | {value.A}");
    }

    [ObservableProperty] private ObservableCollection<ISeries> _pieSeries;
    [ObservableProperty] private Color _displayedIncomeColor;
    [ObservableProperty] private Color _displayedOutgoingColor;
    [ObservableProperty] private ObservableCollection<GridLine> _gridPreviewData;
    [ObservableProperty] private ObservableCollection<ISeries> _categoryChart;
    [ObservableProperty] private Color _selectedColor;
    [ObservableProperty] private ICartesianAxis[] _categoryXAxes;
    [ObservableProperty] private ICartesianAxis[] _categoryYAxes;
    
    [ObservableProperty] private string _cartesianChartPosition;
    [ObservableProperty] private bool _isCartesianChartDisplayed;
    [ObservableProperty] private ICommand _cartesianChartLegendCmd;

    [ObservableProperty] private bool _isJanuarValueDisplayed;
    [ObservableProperty] private bool _isFebruarValueDisplayed;
    [ObservableProperty] private bool _isMärzValueDisplayed;
    [ObservableProperty] private bool _isAprilValueDisplayed;
    [ObservableProperty] private bool _isMaiValueDisplayed;
    [ObservableProperty] private bool _isJuniValueDisplayed;
    [ObservableProperty] private bool _isJuliValueDisplayed;
    [ObservableProperty] private bool _isAugustValueDisplayed;
    [ObservableProperty] private bool _isSeptemberValueDisplayed;
    [ObservableProperty] private bool _isOktoberValueDisplayed;
    [ObservableProperty] private bool _isNovemberValueDisplayed;
    [ObservableProperty] private bool _isDezemberValueDisplayed;

    [ObservableProperty] private Window _currentWindow;

    [ObservableProperty] private ICommand _refreshDataCmd;
    [ObservableProperty] private ICommand _loadColorEditorCmd;
    [ObservableProperty] private ICommand _loadGridLinePreviewEditorCmd;
    [ObservableProperty] private ICommand _loadPieChartColorEditorCmd;
} 