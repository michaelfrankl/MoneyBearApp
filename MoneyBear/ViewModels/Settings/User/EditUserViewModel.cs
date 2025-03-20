using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.DataProtection;
using MoneyBear.DesignData;
using MoneyBear.Services;
using NASPLOID.Models.MoneyBear;
using NASPLOID.Services.MoneyBear;

namespace MoneyBear.ViewModels.Settings.User;

public partial class EditUserViewModel : ViewModelBase
{
    [ObservableProperty] private AppService _appService;
    
    public EditUserViewModel(AppService appService)
    {
        AppService = appService;
        InitUI();
        AddRoles();
        AddUsers();
        CheckAdmin();
        SelectSelfAsUser();
        
        PasswordIconCmd = new RelayCommand(ChangePasswordIconAndDisplay);
        DeleteUserCmd = new RelayCommand<object>(_ => DeleteUserFromDatabase());
        SaveChangesCmd = new RelayCommand<Window>(async (mainWindow) => await ChangeUserInformation(mainWindow));
    }

    public EditUserViewModel()
    {
        SampleUserData sampleUserData = new SampleUserData();
        PreviewUsers = sampleUserData.GetSampleUsers();
        Roles = sampleUserData.GetSampleRoles();
        SelectedUser = PreviewUsers[0];
        InitUI();
        
        PasswordIconCmd = new RelayCommand(ChangePasswordIconAndDisplay);
    }

    private void InitUI()
    {
        PasswordChars = "*";
        PasswordIcon = "fa-solid fa-eye";
        IsPasswordHidden = true;
        IsTaskActive = false;
    }

    private void CheckAdmin()
    {
        if(AppService.Session.User == null)
            return;
        IsEditingAllowed = AppService.IsAdmin;
        IsAllowedToChangedRoles = AppService.IsAdmin;
    }

    private void AddRoles()
    {
        if(AppService.Session.Roles == null)
            return;
        Roles = new ObservableCollection<MBRole>(AppService.Session.Roles.Select(x => x.Clone()));
    }

    private void AddUsers()
    {
        if(AppService.Session.Users == null)
            return;
        PreviewUsers = new ObservableCollection<MBUser>(AppService.Session.Users.Select(x => x.Clone()));
    }

    private void SelectSelfAsUser()
    {
        if(PreviewUsers == null)
            return;
        if(AppService.Session.User == null)
            return;

        var isUserListed = PreviewUsers.FirstOrDefault(x => x.Id == AppService.User.Id);
        if (isUserListed != null)
        {
            SelectedUser = isUserListed;
            IsPasswordFieldAllowed = true;
            IsEditingAllowed = true;
        }
        else
        {
            if(PreviewUsers.Count > 0)
                SelectedUser = PreviewUsers[0];
            IsPasswordFieldAllowed = false;
            IsEditingAllowed = AppService.IsAdmin;
        }
    }

    partial void OnSelectedUserChanged(MBUser user)
    {
        if(user == null) return;
        if(Roles == null) return;
        if(AppService.User == null) return;

        HidePassword();
        UserName = user.Name?? "";
        Email = user.Email ?? "";
        Password = user.Password ?? "";
        UserRole = user.ToRole(user.Role);
        SelectedRole = Roles.First(x => x.RoleId == UserRole.RoleId);

        if (user.Id == AppService.User.Id)
        {
            IsPasswordFieldAllowed = true;
            IsEditingAllowed = true;
            Password = Program.Protector.Unprotect(Password);
        }
        else
        {
            IsPasswordFieldAllowed = false;
            IsEditingAllowed = AppService.IsAdmin;
        }
    }

    private void HidePassword()
    {
        PasswordIcon = "fa-solid fa-eye";
        PasswordChars = "*";
        IsPasswordHidden = true;
    }

    partial void OnIsTaskActiveChanged(bool isActive)
    {
        if(isActive == null)
            return;
        IsEditingAllowed = !isActive;
        IsAllowedToChangedRoles = !isActive;
    }

    private void ChangePasswordIconAndDisplay()
    {
        if (IsPasswordHidden)
        {
            PasswordIcon = "fa-solid fa-eye-slash";
            PasswordChars = "";
            IsPasswordHidden = false;
        }
        else
        {
            PasswordIcon = "fa-solid fa-eye";
            PasswordChars = "*";
            IsPasswordHidden = true;
        }
    }

    private async Task DeleteUserFromDatabase()
    {
        CancellationToken cancellationToken = new CancellationToken();

        if (SelectedUser == null)
            return;
        if (AppService.Session.User == null)
            return;
        if (AppService.User.Id == SelectedUser.Id)
        {
            MessageService.WarningMessageBoxClassic("Bei einer Eigenlöschung, bitte einen Systemadmintrator kontaktieren!", (int)ErrorEnum.Aborted);
            return;
        }

        IsTaskActive = true;
        
        int result = 0;

        await Task.Run(async () =>
        {
            result = await MBUSerService.DeleteUser(cancellationToken, SelectedUser);
        }, cancellationToken);

        switch (result)
        {
            case (int)ErrorEnum.Success:
                MessageService.NormalMessageBoxClassic("Benutzer wurde gelöscht!");
                PreviewUsers.Remove(SelectedUser);
                if (PreviewUsers.Count > 0)
                    SelectedUser = PreviewUsers[0];
                break;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Benutzer konnte nicht gelöscht werden!", (int)ErrorEnum.Aborted);
                break;
            case (int)ErrorEnum.NoMatchFound:
                MessageService.WarningMessageBoxClassic("Benutzer wurde nicht gefunden!", (int)ErrorEnum.NoMatchFound);
                break;
        }
        IsTaskActive = false;
    }

    private async Task ChangeUserInformation(Window window)
    {
        CancellationToken cancellationToken = new CancellationToken();
        if(SelectedUser == null)
            return;
        IsTaskActive = true;

        SelectedUser.Name = UserName;
        SelectedUser.Email = Email;
        if (IsPasswordFieldAllowed)
        {
            SelectedUser.Password = Program.Protector.Protect(Password);
        }
        else
        {
            SelectedUser.Password = Password;
        }
        SelectedUser.Role = SelectedRole.RoleId;
        
        int result = 0;
        await Task.Run(async () =>
        {
            result = await MBUSerService.EditUser(cancellationToken, SelectedUser, IsPasswordFieldAllowed);
        }, cancellationToken);

        switch (result)
        {
            case (int)ErrorEnum.Success:
                var answer = await MessageService.AnswerMessageBoxClassic("Benutzerinformationen wurden geändert!");
                if (!answer)
                {
                    IsTaskActive = false;
                    window.Close();
                    return;
                }
                break;
            case (int)ErrorEnum.Aborted:
                MessageService.WarningMessageBoxClassic("Vorgang konnte nicht durchgeführt werden!", (int)ErrorEnum.Aborted);
                break;
            case (int)ErrorEnum.ValueNotChanged:
                MessageService.WarningMessageBoxClassic("Es wurden keine Änderungen vorgenommen!", (int)ErrorEnum.ValueNotChanged);
                break;
        }
        IsTaskActive = false;
    }

    [ObservableProperty] private ObservableCollection<MBUser> _previewUsers;
    [ObservableProperty] private MBUser _selectedUser;
    [ObservableProperty] private bool _isPasswordFieldAllowed;
    [ObservableProperty] private bool _isEditingAllowed;
    [ObservableProperty] private bool _isAllowedToChangedRoles;
    [ObservableProperty] private string _userName;
    [ObservableProperty] private string _email;
    [ObservableProperty] private string _password;
    [ObservableProperty] private string _passwordChars;
    [ObservableProperty] private string _passwordIcon;
    [ObservableProperty] private bool _isPasswordHidden;
    [ObservableProperty] private MBRole _userRole;
    [ObservableProperty] private ObservableCollection<MBRole> _roles;
    [ObservableProperty] private MBRole _selectedRole;
    
    [ObservableProperty] private ICommand _passwordIconCmd;
    [ObservableProperty] private ICommand _deleteUserCmd;
    [ObservableProperty] private ICommand _saveChangesCmd;
    [ObservableProperty] private bool _isTaskActive;
}