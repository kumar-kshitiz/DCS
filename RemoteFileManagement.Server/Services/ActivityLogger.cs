using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Server.Services
{
    public static class ActivityLogger
    {
        private static readonly object SyncRoot = new object();
        public static string LogFilePath
        {
            get
            {
                return Path.Combine(ServerSettings.LogDirectory, "activity.log");
            }
        }

        public static void Log(string username, string operation, string target, string status)
        {
            var line = string.Format("[{0}] User: {1} Operation: {2} Target: {3} Status: {4}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Clean(username), Clean(operation), Clean(target), Clean(status));

            lock (SyncRoot)
            {
                Console.WriteLine(line);
                try { File.AppendAllText(LogFilePath, line + Environment.NewLine); }
                catch (Exception ex) { Console.Error.WriteLine("Activity log write failed: " + ex.Message); }
            }
        }

        public static ActivityLogDto[] GetRecentLogs(string username, int count)
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return new ActivityLogDto[0];
                }

                string[] lines; lock (SyncRoot) { lines = File.ReadAllLines(LogFilePath); }
                var results = new List<ActivityLogDto>();

                foreach (var line in lines.Reverse())
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var entry = ParseLine(line);
                    if (entry != null && (string.IsNullOrWhiteSpace(username) || string.Equals(entry.Username, username.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(entry);
                    }

                    if (results.Count >= count)
                    {
                        break;
                    }
                }

                return results.ToArray();
            }
            catch
            {
                return new ActivityLogDto[0];
            }
        }

        private static string Clean(string value)
        {
            return (value ?? "").Replace("\r", "\\r").Replace("\n", "\\n")
                .Replace("Operation: ", "Operation\\: ").Replace("Target: ", "Target\\: ").Replace("Status: ", "Status\\: ");
        }

        public static T Execute<T>(string username, string operation, string target, Func<T> action, T failure, bool authorize)
        {
            try
            {
                T result;
                if (authorize)
                {
                    AuthService.ValidateSession(username);
                    lock (RemoteFileManagement.Server.Security.PathSecurity.GetOperationLock(username))
                    {
                        AuthService.ValidateSession(username);
                        result = action();
                    }
                }
                else result = action();
                var outcome = (object)result as OperationResult;
                var user = (object)result as UserDto;
                var success = outcome != null ? outcome.Success : user != null ? user.IsAuthenticated :
                    (object)result != null;
                Log(username, operation, target, success ? "SUCCESS" : "FAILURE" + (outcome == null ? "" : ": " + outcome.Message));
                return result;
            }
            catch (Exception ex)
            {
                Log(username, operation, target, "FAILURE: " + ex.Message);
                if (typeof(T) == typeof(OperationResult)) return (T)(object)OperationResult.Fail(ex.Message);
                return failure;
            }
        }

        private static ActivityLogDto ParseLine(string line)
        {
            try
            {
                var prefix = line.IndexOf(']');
                if (prefix < 0)
                {
                    return null;
                }

                var timestampText = line.Substring(1, prefix - 1);
                var rest = line.Substring(prefix + 1).Trim();
                var userIndex = rest.IndexOf("User: ", StringComparison.OrdinalIgnoreCase);
                var operationIndex = rest.IndexOf("Operation: ", StringComparison.OrdinalIgnoreCase);
                var targetIndex = rest.IndexOf("Target: ", StringComparison.OrdinalIgnoreCase);
                var statusIndex = rest.IndexOf("Status: ", StringComparison.OrdinalIgnoreCase);

                if (userIndex < 0 || operationIndex < 0 || targetIndex < 0 || statusIndex < 0)
                {
                    return null;
                }

                var username = rest.Substring(userIndex + 6, operationIndex - (userIndex + 6)).Trim();
                var operation = rest.Substring(operationIndex + 10, targetIndex - (operationIndex + 10)).Trim();
                var target = rest.Substring(targetIndex + 7, statusIndex - (targetIndex + 7)).Trim();
                var status = rest.Substring(statusIndex + 7).Trim();

                return new ActivityLogDto
                {
                    Timestamp = DateTime.ParseExact(timestampText, "yyyy-MM-dd HH:mm:ss", null),
                    Username = username,
                    Operation = operation,
                    Target = target,
                    Status = status
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
