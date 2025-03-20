namespace MoneyBear.Models;

public enum MonthPreview
{
    CurrentMonth = 0,
    NextMonth = 1,
    TwoMonths = 2,
    ThreeMonths = 3,
    FourMonths = 4,
    FiveMonths = 5,
    SixMonths = 6,
    SevenMonths = 7,
    EightMonths = 8,
    NineMonths = 9,
    TenMonths = 10,
    ElevenMonths = 11,
    NexYear = 12
}

public static class MonthPreviewExtensions
{
    public static MonthPreview GetMonthPreview(int value)
        => (MonthPreview)value;
}