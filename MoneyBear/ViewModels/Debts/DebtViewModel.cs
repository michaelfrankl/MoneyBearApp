using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.EntityFrameworkCore;
using MoneyBear.DesignData;
using MoneyBear.Services;
using MoneyBear.ViewModels.DebtsEditView;
using MoneyBear.Views;
using MoneyBear.Views.Debts;
using MoneyBear.Views.Debts.DebtsEditView;
using MoneyBear.Views.Grid;
using NASPLOID;
using NASPLOID.Models;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services;
using NASPLOID.Services.MoneyBear;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace MoneyBear.ViewModels;

public partial class DebtViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public DebtViewModel(AppService appService)
    {
        AppService = appService;
        InitDebtData();
        InitDebtType();
        CurrentWindow = null;
        IsDesign = false;
        //RefreshWindow();
        
        EditDebtCmd = new RelayCommand<MBDebtList>(async debtUserData => await ShowMessage(debtUserData));
        NoteDebtCmd = new RelayCommand<MBDebtList>(async debtUserData => await ShowDebtNote(debtUserData));

        AddNewEntryCmd = new RelayCommand<object>(async _ => await AddNewEntry());
        AddNewUserCmd = new RelayCommand<object>(async _ => await AddNewDebtUser());
    }
    
    public DebtViewModel()
    {
        IsDesign = true;
        IsDebtPaid = true;
        DebtGridData = new SampleDebtData().GetDebtListData();
        DebtUserList = new SampleDebtData().GetDebtUserData();
        SelectedDebtUser = DebtUserList.First();
        DebtTypes = new SampleDebtData().GetDebtTypeData();
        SelectedDebtType = DebtTypes.First();
        SelectedDebtTypeIndex = DebtTypes.IndexOf(SelectedDebtType);
        SelectedDebtUserIndex = 0;
    }

    private void InitDebtData()
    {
        if (AppService.Session.DebtList == null || AppService.Session.User == null || AppService.Session.DebtUSer == null)
        {
            DebtGridData = TempDebtGridData = new ObservableCollection<MBDebtList>();
            if (AppService.Session.DebtUSer == null)
                DebtUserList = new ObservableCollection<MBDebtUser>();
            return;
        }

        DebtGridData = new ObservableCollection<MBDebtList>(AppService.Session.DebtList
            .Where(x => x.UserId == AppService.User.Id).Select(x => x.Clone()));
        TempDebtGridData = new ObservableCollection<MBDebtList>(AppService.Session.DebtList.Where(x => x.UserId == AppService.User.Id).Select(x => x.Clone()));
        DebtUserList = new ObservableCollection<MBDebtUser>(AppService.Session.DebtUSer
            .Where(x => x.UserId == AppService.User.Id).Select(x => x.Clone()));
        var findEntry = DebtUserList.FirstOrDefault(x => x.Name == "Alle" && x.UserId == AppService.User.Id);
        if(findEntry == null)
            DebtUserList.Add(new MBDebtUser() { Name = "Alle", UserId = AppService.User.Id });
        SelectedDebtUser = DebtUserList.First(x => x.Name == "Alle" && x.UserId == AppService.User.Id);
        SelectedDebtUserIndex = DebtUserList.IndexOf(SelectedDebtUser);
    }

    private void InitDebtType()
    {
        if (AppService.Session.DebtTypes == null)
        {
            DebtTypes = new ObservableCollection<MBDebtType>();
            return;
        }
        DebtTypes = new ObservableCollection<MBDebtType>(AppService.Session.DebtTypes.Select(x => x.Clone()));
        var filterType = new MBDebtType("Alle");
        var entry = DebtTypes.FirstOrDefault(x => x.Id == filterType.Id);
        if(entry == null)
            DebtTypes.Add(filterType);
        SelectedDebtType = DebtTypes.First(x => x.Id == filterType.Id);
        SelectedDebtTypeIndex = DebtTypes.IndexOf(SelectedDebtType);
    }

    private async Task AddNewEntry()
    {
        if(AppService.Session.DebtList == null)
            return;
        
        var newEntryView = new DebtAddView()
        {
            DataContext = new DebtAddViewModel(AppService, "Gläubiger Eintrag erstellen")
        };
        var result = await newEntryView.ShowDialog<MBDebtList?>(CurrentWindow);
        if (result != null)
        {
            MBDebtList? debListValue = AppService.Session.DebtList.FirstOrDefault(x => x.Id == result.Id);
            if (debListValue == null)
            {
                AppService.Session.DebtList.Add(result);
                InitDebtData();
            }
        }
    }

    private async Task AddNewDebtUser()
    {
        if(AppService.Session.DebtUSer == null)
            return;
        var addDebtUserView = new DebtAddNewUserView()
        {
            DataContext = new DebtAddNewUserViewModel(AppService, "Gläubiger hinzufügen")
        };
        var result = await addDebtUserView.ShowDialog<MBDebtUser?>(CurrentWindow);
        if (result != null)
        {
            var checkIfExist = AppService.Session.DebtUSer.FirstOrDefault(x => x.Name == result.Name && x.UserId == result.UserId);
            if (checkIfExist == null)
            {
                AppService.Session.DebtUSer.Add(result);
                DebtUserList.Add(result);
            }
        }
    }

    private async Task ShowMessage(MBDebtList debtList)
    {
        var editView = new DebtEditView()
        {
            DataContext = new DebtEditViewModel(AppService, "Gläubiger-Eintrag bearbeiten", debtList)
        };
        var result = await editView.ShowDialog<MBDebtList>(CurrentWindow);
        if (result != null)
        {
            var selectedValue = DebtGridData.FirstOrDefault(x => x.Id == result.Id);
            if (selectedValue != null)
            {
                selectedValue.DebtName = result.DebtName;
                selectedValue.DebtSum = result.DebtSum;
                selectedValue.DebtDate = result.DebtDate;
                selectedValue.IsDebtOpen = result.IsDebtOpen;
                selectedValue.DebtPaidDate = result.DebtPaidDate;
                selectedValue.DebtNote = result.DebtNote;
                selectedValue.DebtImage = result.DebtImage;
                selectedValue.EditedDate = result.EditedDate;
            }
        }
    }

    private async Task ShowDebtNote(MBDebtList debtList)
    {
        var noteView = new DebtsEditViewNoteView()
        {
            DataContext = new DebtsEditViewNoteViewModel(AppService, "Notiz bearbeiten", debtList)
        };
        var result = await noteView.ShowDialog<MBDebtList>(CurrentWindow);
        if (result != null)
        {
            var selectedValue = DebtGridData.FirstOrDefault(x => x.Id == result.Id);
            if (selectedValue != null)
                selectedValue.DebtNote = result.DebtNote;
        }
    }

    public async Task<int> SaveDebtValueChanges(MBDebtList debtList)
    {
        CancellationToken cancellationToken = new CancellationToken();
        var transactionResult =
            await MBMiscService.SaveDebtValueChangesAsync(cancellationToken, debtList);
        return transactionResult;
    }

    partial void OnIsDebtPaidChanged(bool isPaid)
    {
        if (IsDesign)
            return;
        if (isPaid)
        {
            DebtGridData =
                new ObservableCollection<MBDebtList>(TempDebtGridData.Where(x => x.IsDebtOpen == false)
                    .Select(x => x.Clone()));
            return;
        }
        DebtGridData =
            new ObservableCollection<MBDebtList>(TempDebtGridData.Where(x => x.IsDebtOpen == true)
                .Select(x => x.Clone()));
    }

    partial void OnSelectedDebtTypeChanged(MBDebtType selectedDebtType)
    {
        if(IsDesign)
            return;
        if(SelectedDebtUser == null)
            return;
        
        if (selectedDebtType.Id == "Alle")
        {
            if(SelectedDebtUser.Name == "Alle")
                DebtGridData = new ObservableCollection<MBDebtList>(TempDebtGridData.Select(x => x.Clone()));
            else
                DebtGridData = new ObservableCollection<MBDebtList>(TempDebtGridData.Where(x => x.DebtName == SelectedDebtUser.Name).Select(x => x.Clone()));
            return;
        }
        if(SelectedDebtUser.Name == "Alle")
            DebtGridData = new ObservableCollection<MBDebtList>(TempDebtGridData.Where(x => x.DebtType == selectedDebtType.Id).Select(x => x.Clone()));
        else
            DebtGridData = new ObservableCollection<MBDebtList>(TempDebtGridData.Where(x => x.DebtType == selectedDebtType.Id && x.DebtName == SelectedDebtUser.Name).Select(x => x.Clone()));
    }

    partial void OnSelectedDebtUserChanged(MBDebtUser selectedDebtUser)
    {
        if(IsDesign)
            return;
        if(SelectedDebtType == null)
            return;

        if (selectedDebtUser.Name == "Alle")
        {
            if(SelectedDebtType.Id == "Alle")
                DebtGridData = new ObservableCollection<MBDebtList>(TempDebtGridData.Select(x => x.Clone()));
            else
                DebtGridData = new ObservableCollection<MBDebtList>(TempDebtGridData.Where(x => x.DebtType == SelectedDebtType.Id).Select(x => x.Clone()));
            return;
        }
        if(SelectedDebtType.Id == "Alle")
            DebtGridData = new ObservableCollection<MBDebtList>(TempDebtGridData.Where(x => x.DebtName == selectedDebtUser.Name).Select(x => x.Clone()));
        else
            DebtGridData = new ObservableCollection<MBDebtList>(TempDebtGridData.Where(x => x.DebtName == selectedDebtUser.Name && x.DebtType == SelectedDebtType.Id).Select(x => x.Clone()));
    }

    [ObservableProperty] private string _test;
    [ObservableProperty] private string _searchDebtGridFilter;
    [ObservableProperty] private ObservableCollection<MBDebtList> _debtGridData;
    [ObservableProperty] private ObservableCollection<MBDebtList> _tempDebtGridData;
    [ObservableProperty] private ObservableCollection<MBDebtUser> _debtUserList;
    [ObservableProperty] private MBDebtUser _selectedDebtUser;
    [ObservableProperty] private int _selectedDebtUserIndex;
    [ObservableProperty] private bool _isDebtPaid;
    [ObservableProperty] private ObservableCollection<MBDebtType> _debtTypes;
    [ObservableProperty] private MBDebtType _selectedDebtType;
    [ObservableProperty] private int _selectedDebtTypeIndex;

    [ObservableProperty] private bool _isDesign;
    [ObservableProperty] private Window currentWindow;

    [ObservableProperty] private ICommand _addNewEntryCmd;
    [ObservableProperty] private ICommand _addNewUserCmd;

    [ObservableProperty] private ICommand _editDebtCmd;
    [ObservableProperty] private ICommand _noteDebtCmd;
    [ObservableProperty] private ICommand _imageDebtCmd;
}