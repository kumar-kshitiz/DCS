using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Shared.Interfaces
{
    public interface ISystemService
    {
        ServerInfoDto GetServerInfo();
        ActivityLogDto[] GetActivityLogs(string username, int count);
        OperationResult Compress(string username, string sourceRelativePath, string archiveRelativePath);
        OperationResult Extract(string username, string archiveRelativePath, string destinationRelativePath);
    }
}
