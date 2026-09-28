using System;
using System.IO;

namespace RemoteFileManagement.Server.Security
{
    public static class PathSecurity
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> OperationLocks =
            new System.Collections.Concurrent.ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public static object GetOperationLock(string username) { return OperationLocks.GetOrAdd(username, _ => new object()); }

        public static bool IsWithin(string root, string path)
        {
            root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            path = Path.GetFullPath(path);
            return string.Equals(root, path, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetUserRoot(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || !System.Linq.Enumerable.All(username, ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-'))
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

            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "/" || relativePath == "\\")
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
            if (!IsWithin(rootFull, path))
            {
                throw new UnauthorizedAccessException("The requested path is outside your storage area.");
            }

            var segments = safePath.Split(Path.DirectorySeparatorChar);
            if (Array.Exists(segments, part => !IsValidFileName(part)))
                throw new ArgumentException("Invalid path component.");
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
