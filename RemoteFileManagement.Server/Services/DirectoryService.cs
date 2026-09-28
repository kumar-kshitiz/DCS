using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteFileManagement.Server.Security;
using RemoteFileManagement.Shared.Interfaces;
using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Server.Services
{
    public class DirectoryService : MarshalByRefObject, IDirectoryService
    {
        public override object InitializeLifetimeService() { return null; }

        public OperationResult CreateDirectory(string username, string relativePath)
        {
            return ActivityLogger.Execute<OperationResult>(username, "CREATE_DIRECTORY", relativePath, () => CreateDirectoryCore(username, relativePath), OperationResult.Fail("Operation failed."), true);
        }

        public OperationResult DeleteDirectory(string username, string relativePath)
        {
            return ActivityLogger.Execute<OperationResult>(username, "DELETE_DIRECTORY", relativePath, () => DeleteDirectoryCore(username, relativePath), OperationResult.Fail("Operation failed."), true);
        }

        public DirectoryInfoDto[] ListDirectories(string username, string relativePath)
        {
            return ActivityLogger.Execute<DirectoryInfoDto[]>(username, "LIST_DIRECTORIES", relativePath, () => ListDirectoriesCore(username, relativePath), new DirectoryInfoDto[0], true);
        }

        public FileInfoDto[] ListFiles(string username, string relativePath)
        {
            return ActivityLogger.Execute<FileInfoDto[]>(username, "LIST_FILES", relativePath, () => ListFilesCore(username, relativePath), new FileInfoDto[0], true);
        }

        public FileInfoDto[] SearchFiles(string username, string searchPattern, bool recursive)
        {
            return ActivityLogger.Execute<FileInfoDto[]>(username, "SEARCH", searchPattern, () => SearchFilesCore(username, searchPattern, recursive), new FileInfoDto[0], true);
        }


        private OperationResult CreateDirectoryCore(string username, string relativePath)
        {
            var targetDir = PathSecurity.ResolveUserPath(username, relativePath);
            if (Directory.Exists(targetDir))
            {
                return OperationResult.Fail("Directory already exists.");
            }

            Directory.CreateDirectory(targetDir);
            return OperationResult.Ok("Directory created successfully.");
        }

        private OperationResult DeleteDirectoryCore(string username, string relativePath)
        {
            var targetDir = PathSecurity.ResolveUserPath(username, relativePath);
            var userRoot = PathSecurity.GetUserRoot(username);

            if (string.Equals(targetDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), userRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult.Fail("Cannot delete the user's root directory.");
            }

            if (!Directory.Exists(targetDir))
            {
                return OperationResult.Fail("Directory not found.");
            }

            Directory.Delete(targetDir, true);
            return OperationResult.Ok("Directory deleted successfully.");
        }

        private DirectoryInfoDto[] ListDirectoriesCore(string username, string relativePath)
        {
            var root = PathSecurity.ResolveUserPath(username, relativePath);
            var directories = Directory.GetDirectories(root)
                .Select(d => new DirectoryInfo(d))
                .Select(info => new DirectoryInfoDto
                {
                    Name = info.Name,
                    RelativePath = MakeRelativePath(username, info.FullName),
                    CreationTime = info.CreationTime,
                    LastModifiedTime = info.LastWriteTime
                })
                .OrderBy(d => d.Name)
                .ToArray();

            return directories;
        }

        private FileInfoDto[] ListFilesCore(string username, string relativePath)
        {
            var root = PathSecurity.ResolveUserPath(username, relativePath);
            var files = Directory.GetFiles(root)
                .Select(f => new FileInfo(f))
                .Select(info => new FileInfoDto
                {
                    Name = info.Name,
                    Extension = info.Extension,
                    Size = info.Length,
                    CreationTime = info.CreationTime,
                    LastModifiedTime = info.LastWriteTime,
                    RelativePath = MakeRelativePath(username, info.FullName)
                })
                .OrderBy(f => f.Name)
                .ToArray();

            return files;
        }

        private FileInfoDto[] SearchFilesCore(string username, string searchPattern, bool recursive)
        {
            var root = PathSecurity.GetUserRoot(username);
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                throw new ArgumentException("Search pattern is required.");
            }

            var files = Directory.EnumerateFiles(root, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .Select(info => new FileInfoDto
                {
                    Name = info.Name,
                    Extension = info.Extension,
                    Size = info.Length,
                    CreationTime = info.CreationTime,
                    LastModifiedTime = info.LastWriteTime,
                    RelativePath = MakeRelativePath(username, info.FullName)
                })
                .OrderBy(f => f.Name)
                .ToArray();
            return files;
        }

        private static string MakeRelativePath(string username, string fullPath)
        {
            var root = PathSecurity.GetUserRoot(username);
            var fullRoot = Path.GetFullPath(root);
            var relative = fullPath.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace('\\', '/');
        }
    }
}
