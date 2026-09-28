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
        public OperationResult CreateDirectory(string username, string relativePath)
        {
            try
            {
                var targetDir = PathSecurity.ResolveUserPath(username, relativePath);
                if (Directory.Exists(targetDir))
                {
                    return OperationResult.Fail("Directory already exists.");
                }

                Directory.CreateDirectory(targetDir);
                ActivityLogger.Log(username, "CREATE_DIRECTORY", relativePath, "SUCCESS");
                return OperationResult.Ok("Directory created successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "CREATE_DIRECTORY", relativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public OperationResult DeleteDirectory(string username, string relativePath)
        {
            try
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
                ActivityLogger.Log(username, "DELETE_DIRECTORY", relativePath, "SUCCESS");
                return OperationResult.Ok("Directory deleted successfully.");
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "DELETE_DIRECTORY", relativePath, "FAILURE");
                return OperationResult.Fail(ex.Message);
            }
        }

        public DirectoryInfoDto[] ListDirectories(string username, string relativePath)
        {
            try
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
            catch
            {
                return new DirectoryInfoDto[0];
            }
        }

        public FileInfoDto[] ListFiles(string username, string relativePath)
        {
            try
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
            catch
            {
                return new FileInfoDto[0];
            }
        }

        public FileInfoDto[] SearchFiles(string username, string searchPattern, bool recursive)
        {
            try
            {
                var root = PathSecurity.GetUserRoot(username);
                if (string.IsNullOrWhiteSpace(searchPattern))
                {
                    return new FileInfoDto[0];
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

                ActivityLogger.Log(username, "SEARCH", searchPattern, "SUCCESS");
                return files;
            }
            catch (Exception ex)
            {
                ActivityLogger.Log(username, "SEARCH", searchPattern, "FAILURE");
                return new FileInfoDto[0];
            }
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
