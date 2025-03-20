using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace MoneyBear.Services;

public static class ClipboardService
{
    public static void AddTextToClipboard(string value)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } provider)
            throw new NullReferenceException("Clipboard missing -> ClipboardService");
        provider.SetTextAsync(value);
    }

    public static void ClearClipboard(string value)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } provider)
            throw new NullReferenceException("Clipboard missing -> ClipboardService");
        provider.ClearAsync();
    }
}