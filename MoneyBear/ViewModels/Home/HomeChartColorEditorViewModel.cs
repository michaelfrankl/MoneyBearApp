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
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MoneyBear.DesignData;
using MoneyBear.Services;
using MoneyBear.ViewModels.Import;
using MoneyBear.Views.Home;
using MoneyBear.Views.Import;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;
using SkiaSharp;
using Color = Avalonia.Media.Color;

namespace MoneyBear.ViewModels.Home;

public partial class HomeChartColorEditorViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;

    public HomeChartColorEditorViewModel(AppService appService, string windowTitle)
    {
        AppService = appService;
        WindowTitle = windowTitle;
        InitUI();

        SaveColorCmd = new RelayCommand<Window>(async mainWindow  => await SaveCurrentColorForCategoryAsync(mainWindow));
    }

    public HomeChartColorEditorViewModel()
    {
        IsDesignMode = true;
        IsTaskActive = true;
        WindowTitle = "";
        CategoryFilter = string.Empty;
        SampleGridData sampleGridData = new SampleGridData();
        Categories = sampleGridData.GetSampleLineCategories();
        SelectedCategory = Categories[0];
        //SetColorAfterCategoryChanged(SelectedCategory);
        CurrentColor = new SKColor(54, 54, 168);
        DisplayedColor = new Color(255, CurrentColor.Red, CurrentColor.Green, CurrentColor.Blue);
        DisplayedColorBrush = new HsvColor(DisplayedColor);
        //InitChartPreview();
    }

    private async Task SaveCurrentColorForCategoryAsync(Window mainWindow)
    {
        CancellationToken cancellationToken = new CancellationToken();

        if (AppService.User == null)
        {
            MessageService.WarningMessageBoxClassic("Nutzer wurde nicht gefunden!", (int)ErrorEnum.NullReference);
            return;
        }

        if (CurrentColor == null)
        {
            MessageService.WarningMessageBoxClassic("Es wurde keine Farbe ausgewählt!", (int)ErrorEnum.NullReference);
            return;
        }

        if (SelectedCategory == null)
        {
            MessageService.WarningMessageBoxClassic("Es wurde keine Kategorie ausgewählt!", (int)ErrorEnum.NullReference);
            return;
        }

        IsTaskActive = true;
        
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBMiscService.SaveChartColorForCategoryAsync(cancellationToken, CurrentColor.Red, CurrentColor.Green, CurrentColor.Blue, SelectedCategory.Id, AppService.User.Id);

        }, cancellationToken);
        switch (result)
        {
            case (int)ErrorEnum.Success:
                var sessionColor = AppService.Session.ChartCategoryColors.FirstOrDefault(x => x.CategoryId.ToLower().Equals(SelectedCategory.Id.ToLower()) && x.UserId == AppService.User.Id);
                if (sessionColor != null)
                {
                    sessionColor.R = CurrentColor.Red;
                    sessionColor.G = CurrentColor.Green;
                    sessionColor.B = CurrentColor.Blue;
                }
                mainWindow.Close(SelectedCategory);
                return;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Es ist ein Fehler aufgetreten!", (int)ErrorEnum.Aborted);
                break;
        }
        IsTaskActive = false;
    }

    private void InitUI()
    {
        if (AppService.Session.LineCategories == null)
        {
            MessageService.WarningMessageBoxClassic("Unerwarteter Fehler aufgetreten!", (int)ErrorEnum.NullReference);
            return;
        }
        
        Categories = new ObservableCollection<LineCategory>(AppService.Session.LineCategories.Where(x => x.Id != "Kein Filter").Select(x =>x.Clone()).OrderBy(x => x.Id));
        if (Categories.Count == 0)
        {
            MessageService.WarningMessageBoxClassic("Unerwarteter Fehler aufgetreten!", (int)ErrorEnum.NullReference);
            return;
        }
        CategoryFilter = string.Empty;
        SelectedCategory = Categories[0];
        SetColorAfterCategoryChanged(SelectedCategory);
        InitChartPreview();
    }

    private void SetColorAfterCategoryChanged(LineCategory category)
    {
        var currentColor = AppService.Session.ChartCategoryColors.FirstOrDefault(x => x.CategoryId.ToLower().Equals(category.Id.ToLower()));
        if (currentColor == null)
            CurrentColor = new SKColor(255, 255, 255);
        else
            CurrentColor = new SKColor((byte)currentColor.R, (byte)currentColor.G, (byte)currentColor.B);
        DisplayedColor = new Color(255, CurrentColor.Red, CurrentColor.Green, CurrentColor.Blue);
        DisplayedColorBrush = new HsvColor(DisplayedColor);
    }

    private void InitChartPreview()
    {
        ChartPreview = new ObservableCollection<ISeries>();
        var sampleColumn = new ColumnSeries<double>()
        {
            Name = SelectedCategory.Id,
            Values = new double[] { 150.55 },
            Fill = new SolidColorPaint(CurrentColor)
        };
        ChartPreview.Add(sampleColumn);
    }

    public void ColorPickerChanged(Color? newColor)
    {
        if (newColor != null)
        {
            CurrentColor = new SKColor(newColor.Value.R, newColor.Value.G, newColor.Value.B);
            DisplayedColorBrush = new HsvColor(newColor?? Colors.White);
            DisplayedColor = newColor?? Colors.White;
        }
    }

    partial void OnDisplayedColorChanged(Color value)
    {
        if(value == null)
            return;
        CurrentColor = new SKColor(value.R, value.G, value.B);
        DisplayedColorBrush = new HsvColor(value);
        InitChartPreview();
    }

    partial void OnSelectedCategoryChanged(LineCategory category)
    {
        if(category == null)
            return;
        if(IsDesignMode)
            return;
        SetColorAfterCategoryChanged(category);
        InitChartPreview();
    }

    partial void OnCategoryFilterChanged(string value)
    {
        if(value == null)
            return;
        if(IsDesignMode)
            return;
        
        if(AppService.Session.LineCategories == null)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            Categories = new ObservableCollection<LineCategory>(AppService.Session.LineCategories.Where(x => x.Id != "Kein Filter").Select(x => x.Clone()).OrderBy(x => x.Id));
            return;
        }
        Categories = new ObservableCollection<LineCategory>(AppService.Session.LineCategories.Where(x =>x.Id != "Kein Filter" && x.Id.ToLower().Contains(value.ToLower())).Select(x => x.Clone()).OrderBy(x => x.Id));
    }


    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private LineCategory _selectedCategory;
    [ObservableProperty] private ObservableCollection<LineCategory> _categories;
    [ObservableProperty] private string _categoryFilter;
    [ObservableProperty] private SKColor _currentColor;
    [ObservableProperty] private Color _displayedColor;
    [ObservableProperty] private HsvColor _displayedColorBrush;
    [ObservableProperty] private ObservableCollection<ISeries> _chartPreview;
    [ObservableProperty] private bool _isDesignMode;

    [ObservableProperty] private bool _isTaskActive;
    [ObservableProperty] private ICommand _saveColorCmd;

}