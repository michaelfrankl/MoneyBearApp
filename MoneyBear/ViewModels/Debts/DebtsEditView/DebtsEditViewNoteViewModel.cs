using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels.DebtsEditView;

public partial class DebtsEditViewNoteViewModel: ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public DebtsEditViewNoteViewModel(AppService appService, string windowTitle, MBDebtList debtList)
    {
        AppService = appService;
        WindowTitle = windowTitle;
        EditingAllowed = true;
        DebtListData = debtList.Clone();
        SelectedDebtNote = DebtListData.DebtNote?? string.Empty;
        SelectedDebtNoteCount = SelectedDebtNote.Length;

        DeleteNoteCmd = new RelayCommand<object>(async _ => await DeleteDebtNoteComment());
        SaveNoteCmd = new RelayCommand<Window>(async mainWindow => await SaveDebtNoteComment(mainWindow));
        CancelNoteCmd = new RelayCommand<Window>(async mainWindow => await CancelDebtNoteComment(mainWindow));
    }

    public DebtsEditViewNoteViewModel()
    {
        SelectedDebtNote = "Testkommentar";
        SelectedDebtNoteCount = SelectedDebtNote.Length;
        EditingAllowed = true;
        IsMaxNoteCount = true;
    }

    private async Task SaveDebtNoteComment(Window mainWindow)
    {
        if (IsMaxNoteCount)
        {
            MessageService.NormalMessageBoxClassic("Die maximale Anzahl an zulässigen Zeichen wurde überschritten!");
            return;
        }
        
        if (SelectedDebtNote == DebtListData.DebtNote)
        {
            var answer =
                await MessageService.AnswerMessageBoxClassic(
                    "Das Kommentar wurde nicht verändert, möchten Sie trotzdem das Fenster schließen?");
            if (answer)
            {
                mainWindow.Close(DebtListData);
                return;
            }
            return;
        }
        EditingAllowed = false;
        DebtListData.DebtNote = SelectedDebtNote;

        CancellationToken cancellationToken = new CancellationToken();
        var result = await MBMiscService.SaveDebtNoteChangesAsync(cancellationToken, DebtListData);
        switch (result)
        {
            case (int)ErrorEnum.Success:
                mainWindow.Close(DebtListData);
                return;
            case (int)ErrorEnum.NullReference:
                MessageService.WarningMessageBoxClassic("Das Kommentar konnte in der Datenbank nicht gefunden werden!", (int)ErrorEnum.NullReference);
                break;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Beim Speichern in der Datenbank sind Fehler aufgetreten!", (int)ErrorEnum.Aborted);
                break;
        }
        EditingAllowed = true;
    }

    private async Task CancelDebtNoteComment(Window mainWindow)
        => mainWindow.Close(DebtListData);

    private async Task DeleteDebtNoteComment()
    {
        var result = await MessageService.AnswerMessageBoxClassic("Das Kommentar wirklich löschen?");
        if (result)
        {
            SelectedDebtNote = string.Empty;
            SelectedDebtNoteCount = SelectedDebtNote.Length;
        }
    }

    partial void OnSelectedDebtNoteChanged(string note)
    {
        if(String.IsNullOrWhiteSpace(note))
        {
            IsMaxNoteCount = false;
            SelectedDebtNoteCount = note.Length;
        }
        SelectedDebtNoteCount = note.Length;
        IsMaxNoteCount = SelectedDebtNoteCount > 1024;
    }


    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private string _selectedDebtNote;
    [ObservableProperty] private int _selectedDebtNoteCount;
    [ObservableProperty] private MBDebtList _debtListData;

    [ObservableProperty] private bool _editingAllowed;
    [ObservableProperty] private bool _isMaxNoteCount;
    
    [ObservableProperty] private ICommand _saveNoteCmd;
    [ObservableProperty] private ICommand _deleteNoteCmd;
    [ObservableProperty] private ICommand _cancelNoteCmd;
}