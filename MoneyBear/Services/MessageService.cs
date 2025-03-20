using System;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NASPLOID.Models.MoneyBear;

namespace MoneyBear.Services;

public static class MessageService
{
    public static async void WarningMessageBoxClassic(string message, int errorcode)
    {
        var error = new MBErrorCodes();
        if (AppService.Instance.Session.ErrorCodes == null)
        {
            error.ErrorCode = "Aktuell keine weiteren Informationen";
        }
        else
        {
            error = AppService.Instance.Session.ErrorCodes.FirstOrDefault(x => x.Id == errorcode);
            if (error == null)
            {
                error = new MBErrorCodes();
                error.ErrorCode = "Aktuell keine weiteren Informationen";
            }
        }
        var mbox = MessageBoxManager.GetMessageBoxStandard("Warnung!", message+Environment.NewLine +$"Error Code: ({errorcode} - {error.ErrorCode})", ButtonEnum.Ok);
        await mbox.ShowAsync();
    }

    public static async void NormalMessageBoxClassic(string message)
    {
        var mbox = MessageBoxManager.GetMessageBoxStandard("Information", message, ButtonEnum.Ok);
        await mbox.ShowAsync();
    }

    public static async Task<bool> AnswerMessageBoxClassic(string message)
    {
        var mbox = MessageBoxManager.GetMessageBoxStandard("Info!",
            message + Environment.NewLine + $"Möchten Sie fortfahren?", ButtonEnum.YesNo);
        var result = await mbox.ShowAsync();
        if (result == ButtonResult.Yes)
            return true;
        return false;
    }
}