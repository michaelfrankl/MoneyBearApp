using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MoneyBear.Assets;
using MoneyBear.Models;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;
using SkiaSharp;

namespace MoneyBear.ViewModels.Home;

public partial class HomePieChartColorEditorViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;

    public HomePieChartColorEditorViewModel(AppService appService, string windowTitle)
    {
        AppService = appService;
        WindowTitle = windowTitle;
        SetColorForPieChart();
        InitPieChart();
        
        SaveColorCmd = new RelayCommand<Window>(async (mainWindow) => await SaveUserPrefixColors(mainWindow));
    }
    
    
    public HomePieChartColorEditorViewModel()
    {
        WindowTitle = string.Empty;
        IsTaskActive = true;
        IsDesignView = true;
        DisplayedIncomeColor = Colors.DarkGreen;
        DisplayedOutgoingColor = Colors.DarkRed;
    }

    private async Task SaveUserPrefixColors(Window mainWindow)
    {
        if(AppService.User.Id == null)
            return;
        if(AppService.Session.GlobalVariables == null)
            return;
        
        IsTaskActive = true;
        CancellationToken cancellationToken = new CancellationToken();
        
        string incomeColorPrefix = $"{DisplayedIncomeColor.R},{DisplayedIncomeColor.G},{DisplayedIncomeColor.B}";
        string outgoingColorPrefix = $"{DisplayedOutgoingColor.R},{DisplayedOutgoingColor.G},{DisplayedOutgoingColor.B}";
        
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBMiscService.SavePieChartColorForHomeViewAsync(cancellationToken, incomeColorPrefix, outgoingColorPrefix, GlobalVariables.HomePieChartIncomeColor, GlobalVariables.HomePieChartOutgoingColor, AppService.User.Id);

        }, cancellationToken);
        switch (result)
        {
            case (int)ErrorEnum.Success:
                var incomeColorGV = await MBMiscService.GetGlobalVariableAsync(cancellationToken, GlobalVariables.HomePieChartIncomeColor, AppService.User.Id);
                var outgoingColorGV = await MBMiscService.GetGlobalVariableAsync(cancellationToken, GlobalVariables.HomePieChartOutgoingColor, AppService.User.Id);
                if (incomeColorGV != null)
                {
                    var savedIncomeGV = AppService.Session.GlobalVariables.FirstOrDefault(x => x.Id == GlobalVariables.HomePieChartIncomeColor && x.UserId == AppService.User.Id);
                    if(savedIncomeGV == null)
                        AppService.Session.GlobalVariables.Add(incomeColorGV);
                    else
                        savedIncomeGV.ValueAsString = incomeColorGV.ValueAsString;
                }

                if (outgoingColorGV != null)
                {
                    var savedOutgoingGV = AppService.Session.GlobalVariables.FirstOrDefault(x => x.Id == GlobalVariables.HomePieChartOutgoingColor && x.UserId == AppService.User.Id);
                    if(savedOutgoingGV == null)
                        AppService.Session.GlobalVariables.Add(outgoingColorGV);
                    else
                        savedOutgoingGV.ValueAsString = outgoingColorGV.ValueAsString;
                }
                IsTaskActive = false;
                PieChartColorSet colorSet = new PieChartColorSet(
                    new Color(255, DisplayedIncomeColor.R, DisplayedIncomeColor.G, DisplayedIncomeColor.B),
                    new Color(255, DisplayedOutgoingColor.R, DisplayedOutgoingColor.G, DisplayedOutgoingColor.B));
                mainWindow.Close(colorSet);
                return;
            case (int)ErrorEnum.ValueNotChanged:
                MessageService.NormalMessageBoxClassic("Die ausgewählte Farbe(n) entsprechen den gespeicherten Farben!");
                break;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Es ist ein Fehler aufgetreten!", (int)ErrorEnum.Aborted);
                break;
        }
        IsTaskActive = false;
    }

    private void InitPieChart()
    {
        var income = new PieSeries<double>()
        {
            Fill = new SolidColorPaint(new SKColor(DisplayedIncomeColor.R, DisplayedIncomeColor.G, DisplayedIncomeColor.B)),
            Name = "Einnahmen",
            Values = [2650]
        };
        var outgoing = new PieSeries<double>()
        {
            Fill = new SolidColorPaint(new SKColor(DisplayedOutgoingColor.R, DisplayedOutgoingColor.G, DisplayedOutgoingColor.B)),
            Name = "Ausgaben",
            Values = [1200.00],
        };

        PieSeries =
        [
            income,
            outgoing
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

    partial void OnDisplayedIncomeColorChanged(Color value)
    {
        if(value == null)
            return;
        if(IsDesignView)
            return;

        DisplayedIncomeColor = value;
        InitPieChart();
    }

    partial void OnDisplayedOutgoingColorChanged(Color value)
    {
        if(value == null)
            return;
        if(IsDesignView)
            return;
        DisplayedOutgoingColor = value;
        InitPieChart();
    }
    
    
    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private Color _displayedIncomeColor;
    [ObservableProperty] private Color _displayedOutgoingColor;
    [ObservableProperty] private ObservableCollection<ISeries> _pieSeries;

    [ObservableProperty] private bool _isDesignView;
    [ObservableProperty] private bool _isTaskActive;
    [ObservableProperty] private ICommand _saveColorCmd;
}