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
        public override object InitializeLifetimeService() { return null; }

        public ServerInfoDto GetServerInfo()
        {
            return ActivityLogger.Execute<ServerInfoDto>("SERVER", "SERVER_INFO", "SERVER", () => GetServerInfoCore(), null, false);
        }

        public ActivityLogDto[] GetActivityLogs(string username, int count)
        {
            return ActivityLogger.Execute<ActivityLogDto[]>(username, "ACTIVITY_LOGS", username, () => GetActivityLogsCore(username, count), new ActivityLogDto[0], true);
        }

        public OperationResult Compress(string username, string sourceRelativePath, string archiveRelativePath)
        {
            return ActivityLogger.Execute<OperationResult>(username, "COMPRESS", sourceRelativePath + " -> " + archiveRelativePath, () => CompressCore(username, sourceRelativePath, archiveRelativePath), OperationResult.Fail("Operation failed."), true);
        }

        public OperationResult Extract(string username, string archiveRelativePath, string destinationRelativePath)
        {
            return ActivityLogger.Execute<OperationResult>(username, "EXTRACT", archiveRelativePath + " -> " + destinationRelativePath, () => ExtractCore(username, archiveRelativePath, destinationRelativePath), OperationResult.Fail("Operation failed."), true);
        }


        private ServerInfoDto GetServerInfoCore()
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

        private ActivityLogDto[] GetActivityLogsCore(string username, int count)
        {
            if (count <= 0) count = 20;
            return ActivityLogger.GetRecentLogs(username, count);
        }

        private OperationResult CompressCore(string username, string sourceRelativePath, string archiveRelativePath)
        {
            var sourcePath = PathSecurity.ResolveUserPath(username, sourceRelativePath);
            var archivePath = PathSecurity.ResolveUserPath(username, archiveRelativePath);

            if (Directory.Exists(sourcePath))
            {
                if (string.IsNullOrWhiteSpace(Path.GetExtension(archivePath)))
                {
                    archivePath += ".zip";
                }

                if (PathSecurity.IsWithin(sourcePath, archivePath))
                    return OperationResult.Fail("Archive must be outside the source directory.");
                Directory.CreateDirectory(Path.GetDirectoryName(archivePath));
                ZipFile.CreateFromDirectory(sourcePath, archivePath);
            }
            else if (File.Exists(sourcePath))
            {
                if (string.IsNullOrWhiteSpace(Path.GetExtension(archivePath)))
                {
                    archivePath += ".zip";
                }

                Directory.CreateDirectory(Path.GetDirectoryName(archivePath));
                using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath));
                }
            }
            else
            {
                return OperationResult.Fail("Source path not found.");
            }
            return OperationResult.Ok("Archive created successfully.");
        }

        private OperationResult ExtractCore(string username, string archiveRelativePath, string destinationRelativePath)
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

                    if (!PathSecurity.IsWithin(root, fullTarget))
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
            return OperationResult.Ok("Archive extracted successfully.");
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
