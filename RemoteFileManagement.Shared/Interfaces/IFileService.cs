using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Shared.Interfaces
{
    public interface IFileService
    {
        OperationResult UploadFile(string username, string fileName, byte[] content);
        byte[] DownloadFile(string username, string relativePath);
        OperationResult DeleteFile(string username, string relativePath);
        OperationResult RenameFile(string username, string sourceRelativePath, string newFileName);
        OperationResult CopyFile(string username, string sourceRelativePath, string destinationRelativePath);
        OperationResult MoveFile(string username, string sourceRelativePath, string destinationRelativePath);
        string ReadTextFile(string username, string relativePath);
        OperationResult WriteTextFile(string username, string relativePath, string content, bool append);
        FileInfoDto GetFileInfo(string username, string relativePath);
        string CalculateHash(string username, string relativePath);
    }
}
