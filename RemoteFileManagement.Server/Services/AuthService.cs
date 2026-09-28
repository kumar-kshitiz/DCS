using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using RemoteFileManagement.Server.Security;
using RemoteFileManagement.Shared.Interfaces;
using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Server.Services
{
    public class AuthService : MarshalByRefObject, IAuthService
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, string> UserPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> UserDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> UserSalt = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> LoggedInUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public AuthService()
        {
            EnsureSeedUsers();
        }

        public OperationResult Register(string username, string password, string displayName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return OperationResult.Fail("Username and password are required.");
                }

                username = username.Trim();
                if (!IsValidUserName(username))
                {
                    return OperationResult.Fail("Invalid username.");
                }

                lock (SyncRoot)
                {
                    if (UserPasswords.ContainsKey(username))
                    {
                        return OperationResult.Fail("User already exists.");
                    }

                    var salt = CreateSalt();
                    var hash = HashPassword(password, salt);
                    UserPasswords[username] = hash;
                    UserSalt[username] = salt;
                    UserDisplayNames[username] = string.IsNullOrWhiteSpace(displayName) ? username : displayName.Trim();

                    var root = PathSecurity.GetUserRoot(username);
                    Directory.CreateDirectory(root);

                    ActivityLogger.Log(username, "REGISTER", username, "SUCCESS");
                    return OperationResult.Ok("Registration successful.");
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("Registration failed. " + ex.Message);
            }
        }

        public UserDto Login(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return new UserDto { IsAuthenticated = false };
                }

                username = username.Trim();
                lock (SyncRoot)
                {
                    if (!UserPasswords.ContainsKey(username))
                    {
                        ActivityLogger.Log(username, "LOGIN", username, "FAILURE");
                        return new UserDto { IsAuthenticated = false };
                    }

                    var hash = HashPassword(password, UserSalt[username]);
                    if (!string.Equals(hash, UserPasswords[username], StringComparison.Ordinal))
                    {
                        ActivityLogger.Log(username, "LOGIN", username, "FAILURE");
                        return new UserDto { IsAuthenticated = false };
                    }

                    LoggedInUsers.Add(username);
                    var displayName = UserDisplayNames.ContainsKey(username) ? UserDisplayNames[username] : username;
                    ActivityLogger.Log(username, "LOGIN", username, "SUCCESS");

                    return new UserDto
                    {
                        Username = username,
                        DisplayName = displayName,
                        IsAuthenticated = true
                    };
                }
            }
            catch
            {
                return new UserDto { IsAuthenticated = false };
            }
        }

        public OperationResult ChangePassword(string username, string oldPassword, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    return OperationResult.Fail("Username, current password and new password are required.");
                }

                lock (SyncRoot)
                {
                    if (!UserPasswords.ContainsKey(username))
                    {
                        return OperationResult.Fail("User not found.");
                    }

                    var currentHash = HashPassword(oldPassword, UserSalt[username]);
                    if (!string.Equals(currentHash, UserPasswords[username], StringComparison.Ordinal))
                    {
                        return OperationResult.Fail("Current password is incorrect.");
                    }

                    if (newPassword.Length < 4)
                    {
                        return OperationResult.Fail("New password must be at least 4 characters.");
                    }

                    UserPasswords[username] = HashPassword(newPassword, UserSalt[username]);
                    ActivityLogger.Log(username, "CHANGE_PASSWORD", username, "SUCCESS");
                    return OperationResult.Ok("Password changed successfully.");
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("Password change failed. " + ex.Message);
            }
        }

        public OperationResult Logout(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return OperationResult.Fail("Username is required.");
                }

                lock (SyncRoot)
                {
                    LoggedInUsers.Remove(username);
                    ActivityLogger.Log(username, "LOGOUT", username, "SUCCESS");
                    return OperationResult.Ok("Logout successful.");
                }
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("Logout failed. " + ex.Message);
            }
        }

        public static int GetRegisteredUserCount()
        {
            lock (SyncRoot)
            {
                return UserPasswords.Count;
            }
        }

        public static int GetActiveUserCount()
        {
            lock (SyncRoot)
            {
                return LoggedInUsers.Count;
            }
        }

        private static void EnsureSeedUsers()
        {
            lock (SyncRoot)
            {
                if (UserPasswords.Count > 0)
                {
                    return;
                }

                Register("alice", "alice123", "Alice Johnson");
                Register("bob", "bob123", "Bob Smith");
                Register("charlie", "charlie123", "Charlie Brown");
            }
        }

        private static bool IsValidUserName(string username)
        {
            return !string.IsNullOrWhiteSpace(username) && username.Length >= 3 && username.All(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-');
        }

        private static string CreateSalt()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes);
        }

        private static string HashPassword(string password, string salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 10000))
            {
                return Convert.ToBase64String(deriveBytes.GetBytes(32));
            }
        }
    }
}
