using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FluentAvalonia.UI.Controls;
using MoneyBear.Services;
using MoneyBear.ViewModels;
using MoneyBear.Views;

namespace MoneyBear.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;

    public MainWindowViewModel()
    {
        var navHomeItem = new NavigationViewItem()
        {
            Content = "Dashboard",
            Tag = "home",
            IconSource = new BitmapIconSource()
            {
                UriSource = new Uri("avares://MoneyBear/Assets/Icons/Home.png")
            }
        };
        var navDashboardItem = new NavigationViewItem()
        {
            Content = "Gläubiger",
            Tag = "debt",
            IconSource = new BitmapIconSource()
            {
                UriSource = new Uri("avares://MoneyBear/Assets/Icons/Debt.png")
            }
        };
        var navGridItem = new NavigationViewItem()
        {
            Content = "Grid",
            Tag = "grid",
            IconSource = new BitmapIconSource()
            {
                UriSource = new Uri("avares://MoneyBear/Assets/Icons/Grid.png")
            }
        };

        NavItems =
        [
            navHomeItem,
            navDashboardItem,
            navGridItem,
        ];
    }
    public MainWindowViewModel(AppService appService)
    {
        AppService = appService;
        
        var navHomeItem = new NavigationViewItem()
        {
            Content = "Dashboard",
            Tag = "home",
            IconSource = new BitmapIconSource()
            {
                UriSource = new Uri("avares://MoneyBear/Assets/Icons/Home.png")
            }
        };
        var navDashboardItem = new NavigationViewItem()
        {
            Content = "Gläubiger",
            Tag = "debt",
            IconSource = new BitmapIconSource()
            {
                UriSource = new Uri("avares://MoneyBear/Assets/Icons/Debt.png")
            }
        };
        var navGridItem = new NavigationViewItem()
        {
            Content = "Grid",
            Tag = "grid",
            IconSource = new BitmapIconSource()
            {
                UriSource = new Uri("avares://MoneyBear/Assets/Icons/Grid.png")
            }
        };

        NavItems =
        [
            navHomeItem,
            navDashboardItem,
            navGridItem,
        ];
        
        CurrentNavItem = NavItems[0];
    }

    partial void OnCurrentNavItemChanged(NavigationViewItem? item)
    {
        if (item.Tag is null || item.Tag.ToString().Length <= 0) return;
        switch (item.Tag.ToString())
        {
            case "home":
                CurrentView = new HomeViewModel(AppService);
                CurrentNavItem = NavItems[0];
                break;
            case "debt":
                CurrentView = new DebtViewModel(AppService);
                CurrentNavItem = NavItems[1];
                break;
            case "grid":
                CurrentView = new GridViewModel(AppService);
                CurrentNavItem = NavItems[2];
                break;
        }
    }


    #region Eigenschaften

    [ObservableProperty] private ObservableCollection<NavigationViewItem> _navItems;
    [ObservableProperty] private NavigationViewItem _currentNavItem;
    [ObservableProperty] private object _currentView;

    #endregion
}