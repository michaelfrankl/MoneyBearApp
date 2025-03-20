using CommunityToolkit.Mvvm.ComponentModel;
using Color = Avalonia.Media.Color;

namespace MoneyBear.Models;

public partial class PieChartColorSet: ObservableObject
{
    public PieChartColorSet(Color income, Color outgoing)
    {
        IncomeColor = income;
        OutgoingColor = outgoing;
    }

    public PieChartColorSet()
    {
        
    }
    
    
    [ObservableProperty] private Color _incomeColor;
    [ObservableProperty] private Color _outgoingColor;
    
}