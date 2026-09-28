using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using RemoteFileManagement.Server.Security;
using RemoteFileManagement.Shared.Interfaces;
using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Server.Services
{
    public class SystemService : MarshalByRefObject, ISystemService
    {
        public ServerInfoDto GetServerInfo()
        {
            var used = GetDirectorySize(ServerSettings.StorageRoot);
            var drive = new DriveInfo(Path.GetPathRoot(ServerSettings.StorageRoot));
            return new ServerInfoDto
            {
                Hostname = Environment.MachineName,
                ServerStartTime = ServerSettings.ServerStartTime,
                TotalStorageBytes = drive.TotalSize,
                UsedStorageBytes = used,
                AvailableStorageBytes = drive.AvailableFreeSpace,
                RegisteredUserCount = AuthService.GetRegisteredUserCount(),
                ActiveClientCount = AuthService.GetActiveUserCount()
            };
        }

        public ActivityLogDto[] GetActivityLogs(string username, int count)
        {
            if (count <= 0) count = 20;
            return ActivityLogger.GetRecentLogs(username, count);
        }

        public OperationResult Compress(string username, string sourceRelativePath, string archiveRelativePath)
        {
            try
            {
                var sourcePath = PathSecurity.ResolveUserPath(username, sourceRelativePath);
                var archivePath = PathSecurity.ResolveUserPath(username, archiveRelativePath);

                if (Directory.Exists(sourcePath))
                {
                    if (string.IsNullOrWhiteSpace(Path.GetExtension(archivePath)))
                    {
                        archivePath += ".zip";
                    }

                    ZipFile.CreateFromDirectory(sourcePath, archivePath);
                }
                else if (File.Exists(sourcePath))
                {
                    if (string.IsNullOrWhiteSpace(Path.GetExtension(archivePath)))
                    {
                        archivePath += ".zip";
                    }

                    using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create))
                    {
                        zip.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath));
                    }
                }
                else
                {
                    return OperationResult.Fail("Source path not found.");
                }

                ActivityLogger.Log(username, "COMPRESS", sourceRelativePath, "SUCCESS");
                return OperationResult.Ok("Archive created successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "COMPRESS", sourceRelativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public OperationResult Extract(string username, string archiveRelativePath, string destinationRelativePath)
        {
            try
            {
                var archivePath = PathSecurity.ResolveUserPath(username, archiveRelativePath);
                var destinationPath = PathSecurity.ResolveUserPath(username, destinationRelativePath);

                if (!File.Exists(archivePath))
                {
                    return OperationResult.Fail("Archive file not found.");
                }

                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                using (var archive = ZipFile.OpenRead(archivePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var targetFile = Path.Combine(destinationPath, entry.FullName);
                        var fullTarget = Path.GetFullPath(targetFile);
                        var root = Path.GetFullPath(destinationPath);

                        if (!fullTarget.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("ZIP archive contains unsafe paths.");
                        }

                        if (entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal))
                        {
                            Directory.CreateDirectory(fullTarget);
                            continue;
                        }

                        var dir = Path.GetDirectoryName(fullTarget);
                        if (!string.IsNullOrEmpty(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        entry.ExtractToFile(fullTarget, true);
                    }
                }

                ActivityLogger.Log(username, "EXTRACT", archiveRelativePath, "SUCCESS");
                return OperationResult.Ok("Archive extracted successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "EXTRACT", archiveRelativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        private static long GetDirectorySize(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return 0;
                }

                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);
            }
            catch
            {
                return 0;
            }
        }
    }
}
