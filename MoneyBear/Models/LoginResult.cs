namespace MoneyBear.Models;

public enum LoginResult
{
    Success = 0,
    UserNotFound = 1,
    Credentials = 2,
    OtherError = 3
}