using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Shared.Interfaces
{
    public interface IDirectoryService
    {
        OperationResult CreateDirectory(string username, string relativePath);
        OperationResult DeleteDirectory(string username, string relativePath);
        DirectoryInfoDto[] ListDirectories(string username, string relativePath);
        FileInfoDto[] ListFiles(string username, string relativePath);
        FileInfoDto[] SearchFiles(string username, string searchPattern, bool recursive);
    }
}
