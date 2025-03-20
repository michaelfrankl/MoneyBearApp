using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using MoneyBear.DesignData;
using MoneyBear.Services;
using MoneyBear.Views.Debts;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels;

public partial class DebtAddViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public DebtAddViewModel(AppService appService, string windowTitle)
    {
        AppService = appService;
        WindowTitle = windowTitle;
        SelectedDebtNote = string.Empty;
        EditingAllowed = true;
        SelectedDebtTypeIndex = 0;
        SetDefaultDateForDatePicker();
        InitDebtData();
        IsDebtOpen = true;
        DebtSum = string.Empty;
        SelectedDebtPaidDate = DateTime.Now;
        SelectedDebtNote = string.Empty;
        SelectedDebtImage = LoadImageFromBytes(null);

        DeleteCommentCmd = new RelayCommand(DeleteComment);
        UploadDebtImageCmd = new RelayCommand<Window>(mainWindow => UploadNewDebtImage(mainWindow));
        DeleteDebtImageCmd = new RelayCommand<object>(_ => DeleteUploadedDebtImage());
        AddNewDebtUserCmd = new RelayCommand<Window>(mainWindow => AddNewDebtUser(mainWindow));

        SaveChangesCmd = new RelayCommand<Window>(async mainWindow => await SaveChanges(mainWindow));
        CancelChangesCmd = new RelayCommand<Window>(CancelChanges);
    }

    public DebtAddViewModel()
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

    private async Task SaveChanges(Window mainWindow)
    {
        if (String.IsNullOrWhiteSpace(DebtSum))
        {
            MessageService.NormalMessageBoxClassic("Bitte geben Sie eine Summe ein!");
            return;
        }
        if (SelectedDebtNoteCount > 1024)
        {
            MessageService.NormalMessageBoxClassic("Die maximale Anzahl an zulässigen Zeichen für das Kommentar wurde überschritten!");
            return;
        }

        if (!IsDebtOpen)
        {
            if (SelectedDebtDate > SelectedDebtPaidDate)
            {
                MessageService.NormalMessageBoxClassic("Das Datum der Schuld kann nicht vor Zeitpunkt der Zahlung liegen!");
                return; 
            }
        }
       
        EditingAllowed = false;
        MBDebtList newDebtEntry = new MBDebtList()
        {
            Id = 0,
            DebtName = SelectedDebtUser.Name,
            DebtDate = DateOnly.FromDateTime(SelectedDebtDate),
            DebtSum = 0,
            DebtType = SelectedDebtType.Id,
            IsDebtOpen = IsDebtOpen,
            DebtNote = SelectedDebtNote,
            UserId = AppService.User.Id,
            EditedDate = DateOnly.FromDateTime(DateTime.Now),
            DebtImage = ConvertBitmapToByteArray(SelectedDebtImage)
        };
        
        if (decimal.TryParse(DebtSum, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("de-DE"), out decimal parsedValue))
        {
            newDebtEntry.DebtSum = parsedValue;
        }
        if (IsDebtOpen)
        {
            newDebtEntry.DebtPaidDate = null;
        }

        CancellationToken cancellationToken = new CancellationToken();
        var result = await MBMiscService.AddNewDebtListEntryAsync(cancellationToken, newDebtEntry, AppService.User.Id);
        switch (result.Item1)
        {
            case (int)ErrorEnum.Success:
                mainWindow.Close(newDebtEntry);
                return;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Beim Speichern ist ein Fehler aufgetreten!", (int)ErrorEnum.Aborted);
                break;
        }
        EditingAllowed = true;
    }
    
    private void CancelChanges(Window window)
        => window.Close(null);

    private void DeleteComment()
        => SelectedDebtNote = string.Empty;
    
    private void InitDebtData()
    {
        if (AppService.Session.DebtTypes == null || AppService.Session.User == null || AppService.Session.DebtUSer == null)
        {
            DebtUserList = new ObservableCollection<MBDebtUser>();
            if (AppService.Session.DebtTypes == null)
                DebtTypes = new ObservableCollection<MBDebtType>();
            return;
        }
        DebtTypes = new ObservableCollection<MBDebtType>(AppService.Session.DebtTypes);
        DebtUserList = new ObservableCollection<MBDebtUser>(AppService.Session.DebtUSer
            .Where(x => x.UserId == AppService.User.Id).Select(x => x.Clone()));
        if(DebtUserList.Count > 0)
            SelectedDebtUser = DebtUserList.First();
        SelectedDebtUserIndex = 0;
    }

    private async Task AddNewDebtUser(Window window)
    {
        var newDebtUserView = new DebtAddNewUserView()
        {
            DataContext = new DebtAddNewUserViewModel(AppService, "Gläubiger hinzufügen")
        };
        var result = await newDebtUserView.ShowDialog<MBDebtUser?>(window);
        if (result != null)
        {
            if (AppService.Session.DebtUSer != null)
            {
                AppService.Session.DebtUSer.Add(result);
                DebtUserList = new ObservableCollection<MBDebtUser>(AppService.Session.DebtUSer.Where(x => x.UserId == AppService.User.Id).Select(x => x.Clone()));
                SelectedDebtUser = DebtUserList.Where(x => x.Name == result.Name).FirstOrDefault();
            }
        }
    }
    
    private void SetDefaultDateForDatePicker()
    {
        DisplayDateStart = new DateTime(DateTime.Now.Year-5, 1, 1);
        DisplayDateEnd = new DateTime(DateTime.Now.Year, 12, 31);
        SelectedDebtDate = DateTime.Now;
    }
    
    private async Task UploadNewDebtImage(Window window)
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

    private async Task DeleteUploadedDebtImage()
    {
        var result =
            await MessageService.AnswerMessageBoxClassic("Möchten Sie das gespeicherte Bild wirklich löschen?");
        if (result)
        {
            SelectedDebtImage = null;
        }
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
    
    [ObservableProperty] private Window _currentWindow;

    [ObservableProperty] private ICommand _uploadDebtImageCmd;
    [ObservableProperty] private ICommand _deleteDebtImageCmd;
    
    [ObservableProperty] private ICommand _deleteCommentCmd;

    [ObservableProperty] private ICommand _addNewDebtUserCmd;
    
    [ObservableProperty] private ICommand _saveChangesCmd;
    [ObservableProperty] private ICommand _cancelChangesCmd;
}