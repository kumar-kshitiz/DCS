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
        public OperationResult UploadFile(string username, string fileName, byte[] content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return OperationResult.Fail("Username is required.");
                }

                if (!PathSecurity.IsValidFileName(fileName))
                {
                    return OperationResult.Fail("Invalid file name.");
                }

                if (content == null || content.Length == 0)
                {
                    return OperationResult.Fail("File content cannot be empty.");
                }

                if (content.Length > ServerSettings.MaxUploadSizeBytes)
                {
                    return OperationResult.Fail("File exceeds the maximum allowed size.");
                }

                var userRoot = PathSecurity.GetUserRoot(username);
                var targetPath = Path.Combine(userRoot, fileName);
                targetPath = Path.GetFullPath(targetPath);

                if (!targetPath.StartsWith(Path.GetFullPath(userRoot), StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult.Fail("File path is outside the user storage area.");
                }

                File.WriteAllBytes(targetPath, content);
                ActivityLogger.Log(username, "UPLOAD", fileName, "SUCCESS");
                return OperationResult.Ok("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "UPLOAD", fileName, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public byte[] DownloadFile(string username, string relativePath)
        {
            try
            {
                var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
                if (!File.Exists(fullPath))
                {
                    return null;
                }

                ActivityLogger.Log(username, "DOWNLOAD", relativePath, "SUCCESS");
                return File.ReadAllBytes(fullPath);
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "DOWNLOAD", relativePath, "FAILURE");
                return null;
            }
        }

        public OperationResult DeleteFile(string username, string relativePath)
        {
            try
            {
                var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
                if (!File.Exists(fullPath))
                {
                    return OperationResult.Fail("File not found.");
                }

                File.Delete(fullPath);
                ActivityLogger.Log(username, "DELETE", relativePath, "SUCCESS");
                return OperationResult.Ok("File deleted successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "DELETE", relativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public OperationResult RenameFile(string username, string sourceRelativePath, string newFileName)
        {
            try
            {
                if (!PathSecurity.IsValidFileName(newFileName))
                {
                    return OperationResult.Fail("Invalid new file name.");
                }

                var sourcePath = PathSecurity.ResolveUserPath(username, sourceRelativePath);
                var destinationPath = Path.Combine(Path.GetDirectoryName(sourcePath), newFileName);
                destinationPath = Path.GetFullPath(destinationPath);

                if (!destinationPath.StartsWith(Path.GetFullPath(PathSecurity.GetUserRoot(username)), StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult.Fail("Destination must remain in the user directory.");
                }

                if (!File.Exists(sourcePath))
                {
                    return OperationResult.Fail("Source file not found.");
                }

                File.Move(sourcePath, destinationPath);
                ActivityLogger.Log(username, "RENAME", sourceRelativePath, "SUCCESS");
                return OperationResult.Ok("File renamed successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "RENAME", sourceRelativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public OperationResult CopyFile(string username, string sourceRelativePath, string destinationRelativePath)
        {
            try
            {
                var sourcePath = PathSecurity.ResolveUserPath(username, sourceRelativePath);
                var destinationPath = PathSecurity.ResolveUserPath(username, destinationRelativePath);
                if (!File.Exists(sourcePath))
                {
                    return OperationResult.Fail("Source file not found.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Copy(sourcePath, destinationPath, true);
                ActivityLogger.Log(username, "COPY", sourceRelativePath, "SUCCESS");
                return OperationResult.Ok("File copied successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "COPY", sourceRelativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public OperationResult MoveFile(string username, string sourceRelativePath, string destinationRelativePath)
        {
            try
            {
                var sourcePath = PathSecurity.ResolveUserPath(username, sourceRelativePath);
                var destinationPath = PathSecurity.ResolveUserPath(username, destinationRelativePath);
                if (!File.Exists(sourcePath))
                {
                    return OperationResult.Fail("Source file not found.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Move(sourcePath, destinationPath);
                ActivityLogger.Log(username, "MOVE", sourceRelativePath, "SUCCESS");
                return OperationResult.Ok("File moved successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "MOVE", sourceRelativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public string ReadTextFile(string username, string relativePath)
        {
            try
            {
                var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
                if (!File.Exists(fullPath))
                {
                    return string.Empty;
                }

                ActivityLogger.Log(username, "READ_TEXT", relativePath, "SUCCESS");
                return File.ReadAllText(fullPath);
            }
            catch (Exception)
            {
                ActivityLogger.Log(username, "READ_TEXT", relativePath, "FAILURE");
                return string.Empty;
            }
        }

        public OperationResult WriteTextFile(string username, string relativePath, string content, bool append)
        {
            try
            {
                var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
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

                ActivityLogger.Log(username, append ? "APPEND_TEXT" : "WRITE_TEXT", relativePath, "SUCCESS");
                return OperationResult.Ok("Text file updated successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "WRITE_TEXT", relativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public FileInfoDto GetFileInfo(string username, string relativePath)
        {
            try
            {
                var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
                if (!File.Exists(fullPath))
                {
                    return null;
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
            catch
            {
                return null;
            }
        }

        public string CalculateHash(string username, string relativePath)
        {
            try
            {
                var fullPath = PathSecurity.ResolveUserPath(username, relativePath);
                if (!File.Exists(fullPath))
                {
                    return string.Empty;
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

                    ActivityLogger.Log(username, "HASH", relativePath, "SUCCESS");
                    return builder.ToString();
                }
            }
            catch (Exception)
            {
                ActivityLogger.Log(username, "HASH", relativePath, "FAILURE");
                return string.Empty;
            }
        }
    }
}
