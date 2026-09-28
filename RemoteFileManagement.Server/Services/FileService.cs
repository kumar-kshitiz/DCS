using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;
using RemoteFileManagement.Server.Security;
using RemoteFileManagement.Shared.Interfaces;
using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Server.Services
{
    public class FileService : MarshalByRefObject, IFileService
    {
        public override object InitializeLifetimeService() { return null; }

        public OperationResult UploadFile(string username, string fileName, byte[] content)
        {
            return ActivityLogger.Execute<OperationResult>(username, "UPLOAD", fileName, () => UploadFileCore(username, fileName, content), OperationResult.Fail("Operation failed."), true);
        }

        public byte[] DownloadFile(string username, string relativePath)
        {
            return ActivityLogger.Execute<byte[]>(username, "DOWNLOAD", relativePath, () => DownloadFileCore(username, relativePath), null, true);
        }

        public OperationResult DeleteFile(string username, string relativePath)
        {
            return ActivityLogger.Execute<OperationResult>(username, "DELETE", relativePath, () => DeleteFileCore(username, relativePath), OperationResult.Fail("Operation failed."), true);
        }

        public OperationResult RenameFile(string username, string sourceRelativePath, string newFileName)
        {
            return ActivityLogger.Execute<OperationResult>(username, "RENAME", sourceRelativePath + " -> " + newFileName, () => RenameFileCore(username, sourceRelativePath, newFileName), OperationResult.Fail("Operation failed."), true);
        }

        public OperationResult CopyFile(string username, string sourceRelativePath, string destinationRelativePath)
        {
            return ActivityLogger.Execute<OperationResult>(username, "COPY", sourceRelativePath + " -> " + destinationRelativePath, () => CopyFileCore(username, sourceRelativePath, destinationRelativePath), OperationResult.Fail("Operation failed."), true);
        }

        public OperationResult MoveFile(string username, string sourceRelativePath, string destinationRelativePath)
        {
            return ActivityLogger.Execute<OperationResult>(username, "MOVE", sourceRelativePath + " -> " + destinationRelativePath, () => MoveFileCore(username, sourceRelativePath, destinationRelativePath), OperationResult.Fail("Operation failed."), true);
        }

        public string ReadTextFile(string username, string relativePath)
        {
            return ActivityLogger.Execute<string>(username, "READ_TEXT", relativePath, () => ReadTextFileCore(username, relativePath), string.Empty, true);
        }

        public OperationResult WriteTextFile(string username, string relativePath, string content, bool append)
        {
            return ActivityLogger.Execute<OperationResult>(username, append ? "APPEND_TEXT" : "WRITE_TEXT", relativePath, () => WriteTextFileCore(username, relativePath, content, append), OperationResult.Fail("Operation failed."), true);
        }

        public FileInfoDto GetFileInfo(string username, string relativePath)
        {
            return ActivityLogger.Execute<FileInfoDto>(username, "FILE_INFO", relativePath, () => GetFileInfoCore(username, relativePath), null, true);
        }

        public string CalculateHash(string username, string relativePath)
        {
            return ActivityLogger.Execute<string>(username, "HASH", relativePath, () => CalculateHashCore(username, relativePath), string.Empty, true);
        }


        private OperationResult UploadFileCore(string username, string fileName, byte[] content)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return OperationResult.Fail("Username is required.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return OperationResult.Fail("Invalid file name.");
            }

            if (content == null)
            {
                return OperationResult.Fail("File content cannot be empty.");
            }

            if (content.Length > ServerSettings.MaxUploadSizeBytes)
            {
                return OperationResult.Fail("File exceeds the maximum allowed size.");
            }

            var userRoot = PathSecurity.GetUserRoot(username);
            var targetPath = PathSecurity.ResolveUserPath(username, fileName);
            targetPath = Path.GetFullPath(targetPath);

            if (!targetPath.StartsWith(Path.GetFullPath(userRoot), StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult.Fail("File path is outside the user storage area.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.WriteAllBytes(targetPath, content);
            return OperationResult.Ok("File uploaded successfully.");
        }

        private byte[] DownloadFileCore(string username, string relativePath)
        {
            var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.");
            }
            return File.ReadAllBytes(fullPath);
        }

        private OperationResult DeleteFileCore(string username, string relativePath)
        {
            var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
            if (!File.Exists(fullPath))
            {
                return OperationResult.Fail("File not found.");
            }

            File.Delete(fullPath);
            return OperationResult.Ok("File deleted successfully.");
        }

        private OperationResult RenameFileCore(string username, string sourceRelativePath, string newFileName)
        {
            if (!PathSecurity.IsValidFileName(newFileName))
            {
                return OperationResult.Fail("Invalid new file name.");
            }

            var sourcePath = PathSecurity.ResolveUserPath(username, sourceRelativePath);
            var destinationPath = Path.Combine(Path.GetDirectoryName(sourcePath), newFileName);
            destinationPath = Path.GetFullPath(destinationPath);

            if (!PathSecurity.IsWithin(PathSecurity.GetUserRoot(username), destinationPath))
            {
                return OperationResult.Fail("Destination must remain in the user directory.");
            }

            if (!File.Exists(sourcePath))
            {
                return OperationResult.Fail("Source file not found.");
            }

            File.Move(sourcePath, destinationPath);
            return OperationResult.Ok("File renamed successfully.");
        }

        private OperationResult CopyFileCore(string username, string sourceRelativePath, string destinationRelativePath)
        {
            var sourcePath = PathSecurity.ResolveUserPath(username, sourceRelativePath);
            var destinationPath = PathSecurity.ResolveUserPath(username, destinationRelativePath);
            if (Directory.Exists(destinationPath))
                destinationPath = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
            if (!File.Exists(sourcePath))
            {
                return OperationResult.Fail("Source file not found.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Copy(sourcePath, destinationPath, true);
            return OperationResult.Ok("File copied successfully.");
        }

        private OperationResult MoveFileCore(string username, string sourceRelativePath, string destinationRelativePath)
        {
            var sourcePath = PathSecurity.ResolveUserPath(username, sourceRelativePath);
            var destinationPath = PathSecurity.ResolveUserPath(username, destinationRelativePath);
            if (Directory.Exists(destinationPath))
                destinationPath = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
            if (!File.Exists(sourcePath))
            {
                return OperationResult.Fail("Source file not found.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Move(sourcePath, destinationPath);
            return OperationResult.Ok("File moved successfully.");
        }

        private string ReadTextFileCore(string username, string relativePath)
        {
            var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.");
            }
            return File.ReadAllText(fullPath);
        }

        private OperationResult WriteTextFileCore(string username, string relativePath, string content, bool append)
        {
            var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
            if (Directory.Exists(fullPath))
                return OperationResult.Fail("This path is a folder. Open the folder and create a file with a name such as notes.txt.");
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (append)
            {
                File.AppendAllText(fullPath, content ?? string.Empty, Encoding.UTF8);
            }
            else
            {
                File.WriteAllText(fullPath, content ?? string.Empty, Encoding.UTF8);
            }
            return OperationResult.Ok("Text file updated successfully.");
        }

        private FileInfoDto GetFileInfoCore(string username, string relativePath)
        {
            var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.");
            }

            var info = new FileInfo(fullPath);
            return new FileInfoDto
            {
                Name = info.Name,
                Extension = info.Extension,
                Size = info.Length,
                CreationTime = info.CreationTime,
                LastModifiedTime = info.LastWriteTime,
                RelativePath = relativePath.Replace('\\', '/')
            };
        }

        private string CalculateHashCore(string username, string relativePath)
        {
            var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.");
            }

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
            {
                var hash = sha256.ComputeHash(stream);
                var builder = new StringBuilder();
                foreach (var b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
