using System;
using System.IO;

namespace RemoteFileManagement.Server.Security
{
    public static class PathSecurity
    {
        public static string GetUserRoot(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username is required.");
            }

            var root = Path.Combine(Server.Services.ServerSettings.StorageRoot, "Users", username);
            Directory.CreateDirectory(root);
            return root;
        }

        public static string ResolveUserPath(string username, string relativePath)
        {
            var userRoot = GetUserRoot(username);
            var rootFull = Path.GetFullPath(userRoot);

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return rootFull;
            }

            string safePath = relativePath.Trim();
            safePath = safePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            if (safePath.StartsWith("..", StringComparison.Ordinal) || safePath.Contains(".." + Path.DirectorySeparatorChar) || safePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
            {
                throw new UnauthorizedAccessException("Path traversal is not allowed.");
            }

            if (safePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException("Invalid path characters detected.");
            }

            var path = Path.GetFullPath(Path.Combine(userRoot, safePath));
            if (!path.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("The requested path is outside your storage area.");
            }

            return path;
        }

        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\") || fileName.Contains(":"))
            {
                return false;
            }

            return true;
        }
    }
}
