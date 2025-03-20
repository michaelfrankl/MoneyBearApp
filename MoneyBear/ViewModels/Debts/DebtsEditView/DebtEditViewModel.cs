using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.Measure;
using MoneyBear.DesignData;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;
using QuestPDF.Infrastructure;

namespace MoneyBear.ViewModels;

public partial class DebtEditViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;

    public DebtEditViewModel(AppService appService, string windowTitle, MBDebtList data)
    {
        AppService = appService;
        WindowTitle = windowTitle;
        DebtListData = data;
        CurrentDebtList = DebtListData.Clone();
        InitUserData();
        SetDefaultDateForDatePicker();
        SelectedDebtDate = new DateTime(CurrentDebtList.DebtDate.Year, CurrentDebtList.DebtDate.Month, CurrentDebtList.DebtDate.Day);
        EditingAllowed = true;
        DebtSum = CurrentDebtList.DebtSum.ToString()?? "0";
        IsDebtOpen = CurrentDebtList.IsDebtOpen ?? true;
        if(CurrentDebtList.DebtPaidDate != null)
            SelectedDebtPaidDate = new DateTime(CurrentDebtList.DebtPaidDate.Value.Year, CurrentDebtList.DebtPaidDate.Value.Month, CurrentDebtList.DebtPaidDate.Value.Day);
        else
            SelectedDebtPaidDate = DateTime.Now;
        SelectedDebtNote = CurrentDebtList.DebtNote ?? "";
        SelectedDebtNoteCount = SelectedDebtNote.Length;
        SelectedDebtImage = LoadImageFromBytes(CurrentDebtList.DebtImage?? null);
        
        DeleteCommentCmd = new RelayCommand<object>(async _=> await DeleteComment());
        UploadDebtImage = new RelayCommand<Window>(async window => await UploadImage(window));
        DeleteDebtImage = new RelayCommand<object>(async _=> await DeleteUploadedDebtImage());

        CancelChangesCmd = new RelayCommand<Window>(mainWindow  => CancelDebtChangesAsync(mainWindow));
        SaveChangesCmd = new RelayCommand<Window>(async mainWindow => await SaveDebtChangesAsync(mainWindow));
    }
    
    public DebtEditViewModel()
    {
        WindowTitle = "Eintrag bearbeiten";
        DebtUserList = new SampleDebtData().GetDebtUserData();
        DebtTypes = new SampleDebtData().GetDebtTypeData();
        SelectedDebtTypeIndex = 0;
        SetDefaultDateForDatePicker();
        EditingAllowed = true;
        DebtSum = "1300.00";
        IsDebtOpen = false;
        SelectedDebtPaidDate = DateTime.Now.AddDays(4);
        SelectedDebtNote = "Dies ist eine Testnotiz \n mit einem Zeilenumbruch!";
        SelectedDebtNoteCount = SelectedDebtNote.Length;
        SelectedDebtImage = LoadImageFromBytes(new SampleDebtData().GetTestDebtImage());
    }

    private void InitUserData()
    {
        if(AppService.Session.DebtUSer == null || AppService.Session.DebtTypes == null)
            return;
        DebtUserList = new ObservableCollection<MBDebtUser>(AppService.Session.DebtUSer.Where(x => x.UserId == AppService.User.Id).Select(x => x.Clone()));
        var currentUser = DebtUserList.FirstOrDefault(x => x.Name == DebtListData.DebtName);
        if (currentUser != null)
        {
            SelectedDebtUser = currentUser;
            SelectedDebtUserIndex = DebtUserList.IndexOf(SelectedDebtUser);
        }
        else
            SelectedDebtUserIndex = 0;
        DebtTypes = new ObservableCollection<MBDebtType>(AppService.Session.DebtTypes.Select(x => x.Clone()));
        if(DebtTypes.Count > 0)
            SelectedDebtType = DebtTypes[0];
    }
    
    private Bitmap? LoadImageFromBytes(byte[]? imageData)
    {
        if (imageData == null)
            return null;
        if (imageData.Length == 0)
            return null;
        
        using var ms = new MemoryStream(imageData);
        return new Bitmap(ms);
    }

    private async Task SaveDebtChangesAsync(Window window)
    {
        if(AppService.Session.User == null)
            return;
        
        EditingAllowed = false;
        if (!CheckIfValueChanged())
        {
            var result =
                await MessageService.AnswerMessageBoxClassic(
                    "Es wurden keine Änderungen gefunden, möchten Sie dennoch das Fenster schließen?");
            if (result)
            {
                window.Close(DebtListData);
                return;
            }
            return;
        }
        if (SelectedDebtNoteCount > 1024)
        {
            MessageService.NormalMessageBoxClassic("Die maximale Anzahl an zulässigen Zeichen für das Kommentar wurde überschritten!");
            EditingAllowed = true;
            return;
        }

        if (CurrentDebtList.DebtDate > CurrentDebtList.DebtPaidDate)
        {
            MessageService.NormalMessageBoxClassic("Das Datum der Schuld kann nicht vor Zeitpunkt der Zahlung liegen!");
            EditingAllowed = true;
            return;
        }

        CurrentDebtList.DebtName = SelectedDebtUser.Name;
        CurrentDebtList.DebtDate = DateOnly.FromDateTime(SelectedDebtDate);
        if (decimal.TryParse(DebtSum, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("de-DE"), out decimal parsedValue))
        {
            CurrentDebtList.DebtSum = parsedValue;
        }
        CurrentDebtList.IsDebtOpen = IsDebtOpen;
        CurrentDebtList.DebtPaidDate = DateOnly.FromDateTime(SelectedDebtPaidDate);
        CurrentDebtList.DebtNote = SelectedDebtNote;
        CurrentDebtList.DebtImage = ConvertBitmapToByteArray(SelectedDebtImage);

        CancellationToken cancellationToken = new CancellationToken();
        var transactionResult =
            await MBMiscService.SaveDebtListChangesAsync(cancellationToken, CurrentDebtList,
                AppService.Session.User.Id);
        switch (transactionResult)
        {
            case (int)ErrorEnum.Success:
                window.Close(CurrentDebtList);
                return;
            case (int)ErrorEnum.NullReference:
                MessageService.WarningMessageBoxClassic("Es wurde kein Eintrag in der Datenbank gefunden", (int)ErrorEnum.NullReference);
                break;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Die geänderten Daten konnten nicht gespeichert werden!", (int)ErrorEnum.Aborted);
                break;
        }
        EditingAllowed = true;
    }

    private bool CheckIfValueChanged()
    {
        if (SelectedDebtUser.Name != DebtListData.DebtName)
            return true;
        if (DateOnly.FromDateTime(SelectedDebtDate) != DebtListData.DebtDate)
            return true;
        if (decimal.Parse(DebtSum) != DebtListData.DebtSum)
            return true;
        if (decimal.TryParse(DebtSum, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("de-DE"), out decimal parsedValue))
        {
            if (parsedValue != DebtListData.DebtSum)
                return true;
        }
        if (DateOnly.FromDateTime(SelectedDebtPaidDate) != DebtListData.DebtPaidDate)
            return true;
        if (SelectedDebtNote != DebtListData.DebtNote)
            return true;
        if (ConvertBitmapToByteArray(SelectedDebtImage?? null) != DebtListData.DebtImage)
            return true;
        
        return false;
    }
    
    private byte[] ConvertBitmapToByteArray(Bitmap? bitmap)
    {
        if (bitmap == null)
            return new byte[0];

        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream);
            return stream.ToArray();
        }
    }

    private void CancelDebtChangesAsync(Window window)
        => window.Close(DebtListData);
    
    private void SetDefaultDateForDatePicker()
    {
        DisplayDateStart = new DateTime(DateTime.Now.Year-5, 1, 1);
        DisplayDateEnd = new DateTime(DateTime.Now.Year, 12, 31);
        SelectedDebtDate = DateTime.Now;
    }

    private async Task DeleteComment()
    {
        var result = await MessageService.AnswerMessageBoxClassic("Sind Sie sich sicher das Kommentar zu löschen?");
        if (result)
        {
            if (SelectedDebtNoteCount == 0)
            {
                MessageService.NormalMessageBoxClassic("Es gibt keine Notiz, welche gelöscht werden kann!");
                return;
            }
            
            SelectedDebtNote = string.Empty;
            SelectedDebtNoteCount = SelectedDebtNote.Length;
        }
    }

    private async Task UploadImage(Window window)
    {
        var topLevel = window.GetVisualRoot() as Window;
        if(topLevel == null)return;

        var storageProvider = topLevel.StorageProvider;
        if(storageProvider == null)return;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Bild hochladen",
            AllowMultiple = false,
            FileTypeFilter = new []
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new []{"*.png", "*.jpg", "*.jpeg"}
                }
            }
        });
        if (files.Count > 0)
        {
            var file = files[0];
            await using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();
            SelectedDebtImage = LoadImageFromBytes(imageBytes);
        }
    }

    private async Task DeleteUploadedDebtImage()
    {
        var result =
            await MessageService.AnswerMessageBoxClassic("Möchten Sie das gespeicherte Bild wirklich löschen?");
        if (result)
        {
            SelectedDebtImage = null;
        }
    }

    partial void OnSelectedDebtNoteChanged(string note)
    {
        if (String.IsNullOrWhiteSpace(note))
        {
            SelectedDebtNoteCount = 0;
        }
        else
        {
            SelectedDebtNoteCount = note.Length;
            IsOverMaxNoteCount = SelectedDebtNoteCount > 1024;
        }
    }



    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private MBDebtList _debtListData;
    [ObservableProperty] private MBDebtList _currentDebtList;
    [ObservableProperty] private ObservableCollection<MBDebtUser> _debtUserList;
    [ObservableProperty] private MBDebtUser _selectedDebtUser;
    [ObservableProperty] private int _selectedDebtUserIndex;
    [ObservableProperty] private DateTime _displayDateStart;
    [ObservableProperty] private DateTime _displayDateEnd;
    [ObservableProperty] private DateTime _selectedDebtDate;
    [ObservableProperty] private ObservableCollection<MBDebtType> _debtTypes;
    [ObservableProperty] private MBDebtType _selectedDebtType;
    [ObservableProperty] private int _selectedDebtTypeIndex;
    [ObservableProperty] private string _debtSum;
    [ObservableProperty] private bool _isDebtOpen;
    [ObservableProperty] private DateTime _selectedDebtPaidDate;
    [ObservableProperty] private string _selectedDebtNote;
    [ObservableProperty] private int _selectedDebtNoteCount;
    [ObservableProperty] private Bitmap? _selectedDebtImage;

    [ObservableProperty] private bool _editingAllowed;
    [ObservableProperty] private bool _isOverMaxNoteCount;

    [ObservableProperty] private ICommand _uploadDebtImage;
    [ObservableProperty] private ICommand _deleteDebtImage;
    
    [ObservableProperty] private ICommand _saveCommentCmd;
    [ObservableProperty] private ICommand _deleteCommentCmd;

    [ObservableProperty] private ICommand _saveChangesCmd;
    [ObservableProperty] private ICommand _cancelChangesCmd;
}