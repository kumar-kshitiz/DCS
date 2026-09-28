using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Shared.Interfaces
{
    public interface IAuthService
    {
        OperationResult Register(string username, string password, string displayName);
        UserDto Login(string username, string password);
        OperationResult ChangePassword(string username, string oldPassword, string newPassword);
        OperationResult Logout(string username);
    }
}
