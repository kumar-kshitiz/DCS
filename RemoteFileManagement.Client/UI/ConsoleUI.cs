using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteFileManagement.Client.Services;
using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Client.UI
{
    public class ConsoleUI
    {
        private readonly RemotingClient _client;
        private string _currentUser;
        private string _currentDirectory = "/";

        public ConsoleUI(RemotingClient client)
        {
            _client = client;
        }

        public void Run()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("     REMOTE FILE MANAGEMENT SYSTEM");
                Console.WriteLine("========================================");
                Console.WriteLine("1. Register");
                Console.WriteLine("2. Login");
                Console.WriteLine("3. Change Password");
                Console.WriteLine("4. Exit");
                Console.WriteLine();
                Console.Write("Select an option: ");

                var option = Console.ReadLine();
                switch (option)
                {
                    case "1":
                        RegisterFlow();
                        break;
                    case "2":
                        if (LoginFlow())
                        {
                            MainMenu();
                        }
                        break;
                    case "3":
                        ChangePasswordFlow();
                        break;
                    case "4":
                        return;
                    default:
                        Console.WriteLine("Invalid option.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private void RegisterFlow()
        {
            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Display Name: ");
            var displayName = Console.ReadLine();
            Console.Write("Password: ");
            var password = ReadPassword();

            var result = _client.Register(username, password, displayName);
            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            Console.ReadKey();
        }

        private bool LoginFlow()
        {
            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Password: ");
            var password = ReadPassword();

            var user = _client.Login(username, password);
            if (!user.IsAuthenticated)
            {
                Console.WriteLine("Login failed.");
                Console.ReadKey();
                return false;
            }

            _currentUser = user.Username;
            _currentDirectory = "/";
            Console.WriteLine("Welcome " + user.DisplayName + ".");
            Console.ReadKey();
            return true;
        }

        private void ChangePasswordFlow()
        {
            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Old Password: ");
            var oldPassword = ReadPassword();
            Console.Write("New Password: ");
            var newPassword = ReadPassword();

            var result = _client.ChangePassword(username, oldPassword, newPassword);
            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            Console.ReadKey();
        }

        private void MainMenu()
        {
            while (!string.IsNullOrEmpty(_currentUser))
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("     REMOTE FILE MANAGEMENT SYSTEM");
                Console.WriteLine("========================================");
                Console.WriteLine("Current user: " + _currentUser);
                Console.WriteLine("Current directory: " + _currentDirectory);
                Console.WriteLine();
                Console.WriteLine("1. Upload File");
                Console.WriteLine("2. Download File");
                Console.WriteLine("3. Delete File");
                Console.WriteLine("4. Rename File");
                Console.WriteLine("5. Copy / Move File");
                Console.WriteLine("6. Read / Write Text File");
                Console.WriteLine("7. File Information");
                Console.WriteLine("8. Calculate File Hash");
                Console.WriteLine("9. Create Directory");
                Console.WriteLine("10. Delete Directory");
                Console.WriteLine("11. List Files & Directories");
                Console.WriteLine("12. Search Files");
                Console.WriteLine("13. Compress / Extract ZIP");
                Console.WriteLine("14. Server Information");
                Console.WriteLine("15. Activity Logs");
                Console.WriteLine("16. Logout");
                Console.WriteLine("17. Exit");
                Console.WriteLine();
                Console.Write("Select an option: ");

                var input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        UploadFile();
                        break;
                    case "2":
                        DownloadFile();
                        break;
                    case "3":
                        DeleteFile();
                        break;
                    case "4":
                        RenameFile();
                        break;
                    case "5":
                        CopyMoveFile();
                        break;
                    case "6":
                        ReadWriteTextFile();
                        break;
                    case "7":
                        ShowFileInfo();
                        break;
                    case "8":
                        ShowHash();
                        break;
                    case "9":
                        CreateDirectory();
                        break;
                    case "10":
                        DeleteDirectory();
                        break;
                    case "11":
                        ListDirectory();
                        break;
                    case "12":
                        SearchFiles();
                        break;
                    case "13":
                        CompressExtract();
                        break;
                    case "14":
                        ShowServerInfo();
                        break;
                    case "15":
                        ShowActivityLogs();
                        break;
                    case "16":
                        Logout();
                        return;
                    case "17":
                        Logout();
                        Environment.Exit(0);
                        return;
                    default:
                        Console.WriteLine("Invalid option.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private void UploadFile()
        {
            Console.Write("Enter local file path to upload: ");
            var localPath = Console.ReadLine();
            if (!File.Exists(localPath))
            {
                Console.WriteLine("File not found.");
                Console.ReadKey();
                return;
            }

            var fileName = Path.GetFileName(localPath);
            var content = File.ReadAllBytes(localPath);
            var result = _client.UploadFile(_currentUser, fileName, content);
            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            Console.ReadKey();
        }

        private void DownloadFile()
        {
            Console.Write("Enter remote file path (e.g. notes.txt): ");
            var remotePath = Console.ReadLine();
            Console.Write("Enter local destination path: ");
            var destination = Console.ReadLine();

            var data = _client.DownloadFile(_currentUser, remotePath);
            if (data == null)
            {
                Console.WriteLine("Download failed or file not found.");
                Console.ReadKey();
                return;
            }

            File.WriteAllBytes(destination, data);
            Console.WriteLine("File downloaded successfully.");
            Console.ReadKey();
        }

        private void DeleteFile()
        {
            Console.Write("Enter file path to delete: ");
            var path = Console.ReadLine();
            Console.Write("Confirm delete? (Y/N): ");
            var confirm = Console.ReadLine();

            if (!string.Equals(confirm, "Y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Deletion cancelled.");
                Console.ReadKey();
                return;
            }

            var result = _client.DeleteFile(_currentUser, path);
            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            Console.ReadKey();
        }

        private void RenameFile()
        {
            Console.Write("Enter current file path: ");
            var oldPath = Console.ReadLine();
            Console.Write("Enter new file name: ");
            var newName = Console.ReadLine();

            var result = _client.RenameFile(_currentUser, oldPath, newName);
            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            Console.ReadKey();
        }

        private void CopyMoveFile()
        {
            Console.Write("Enter operation [copy/move]: ");
            var operation = Console.ReadLine();
            Console.Write("Enter source path: ");
            var source = Console.ReadLine();
            Console.Write("Enter destination path: ");
            var destination = Console.ReadLine();

            OperationResult result;
            if (operation.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                result = _client.CopyFile(_currentUser, source, destination);
            }
            else
            {
                result = _client.MoveFile(_currentUser, source, destination);
            }

            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            Console.ReadKey();
        }

        private void ReadWriteTextFile()
        {
            Console.Write("Choose action [read/write/append]: ");
            var action = Console.ReadLine();
            Console.Write("Enter file path: ");
            var filePath = Console.ReadLine();

            if (action.Equals("read", StringComparison.OrdinalIgnoreCase))
            {
                var content = _client.ReadTextFile(_currentUser, filePath);
                Console.WriteLine("--- File content ---");
                Console.WriteLine(content);
                Console.WriteLine("-------------------");
            }
            else
            {
                Console.Write("Enter content: ");
                var content = Console.ReadLine();
                var append = action.Equals("append", StringComparison.OrdinalIgnoreCase);
                var result = _client.WriteTextFile(_currentUser, filePath, content, append);
                Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            }

            Console.ReadKey();
        }

        private void ShowFileInfo()
        {
            Console.Write("Enter file path: ");
            var path = Console.ReadLine();
            var info = _client.GetFileInfo(_currentUser, path);
            if (info == null)
            {
                Console.WriteLine("File not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("File Name: " + info.Name);
            Console.WriteLine("Extension: " + info.Extension);
            Console.WriteLine("Size: " + info.Size + " bytes");
            Console.WriteLine("Creation Time: " + info.CreationTime);
            Console.WriteLine("Last Modified Time: " + info.LastModifiedTime);
            Console.ReadKey();
        }

        private void ShowHash()
        {
            Console.Write("Enter file path to hash: ");
            var path = Console.ReadLine();
            var hash = _client.CalculateHash(_currentUser, path);
            if (string.IsNullOrEmpty(hash))
            {
                Console.WriteLine("Hash calculation failed.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("SHA-256: " + hash);
            Console.ReadKey();
        }

        private void CreateDirectory()
        {
            Console.Write("Enter directory path to create: ");
            var path = Console.ReadLine();
            var result = _client.CreateDirectory(_currentUser, path);
            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            Console.ReadKey();
        }

        private void DeleteDirectory()
        {
            Console.Write("Enter directory path to delete: ");
            var path = Console.ReadLine();
            var result = _client.DeleteDirectory(_currentUser, path);
            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            Console.ReadKey();
        }

        private void ListDirectory()
        {
            Console.Write("Enter directory path (or /): ");
            var path = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(path)) path = "/";

            var directories = _client.ListDirectories(_currentUser, path);
            var files = _client.ListFiles(_currentUser, path);

            Console.WriteLine("Current Directory: " + path);
            Console.WriteLine("Directories:");
            foreach (var dir in directories)
            {
                Console.WriteLine("- " + dir.Name);
            }
            Console.WriteLine("Files:");
            foreach (var file in files)
            {
                Console.WriteLine("- " + file.Name);
            }
            Console.ReadKey();
        }

        private void SearchFiles()
        {
            Console.Write("Search pattern (e.g. *.pdf): ");
            var pattern = Console.ReadLine();
            var results = _client.SearchFiles(_currentUser, pattern, true);
            Console.WriteLine("Results:");
            foreach (var file in results)
            {
                Console.WriteLine("- " + file.Name + " | " + file.RelativePath);
            }
            Console.ReadKey();
        }

        private void CompressExtract()
        {
            Console.Write("Choose action [compress/extract]: ");
            var action = Console.ReadLine();
            if (action.Equals("compress", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("Enter source path: ");
                var source = Console.ReadLine();
                Console.Write("Enter archive path: ");
                var archive = Console.ReadLine();
                var result = _client.Compress(_currentUser, source, archive);
                Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            }
            else
            {
                Console.Write("Enter archive path: ");
                var archive = Console.ReadLine();
                Console.Write("Enter destination directory: ");
                var dest = Console.ReadLine();
                var result = _client.Extract(_currentUser, archive, dest);
                Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            }

            Console.ReadKey();
        }

        private void ShowServerInfo()
        {
            var info = _client.GetServerInfo();
            Console.WriteLine("Server Hostname: " + info.Hostname);
            Console.WriteLine("Server Start Time: " + info.ServerStartTime);
            Console.WriteLine("Total Storage: " + info.TotalStorageBytes + " bytes");
            Console.WriteLine("Used Storage: " + info.UsedStorageBytes + " bytes");
            Console.WriteLine("Available Storage: " + info.AvailableStorageBytes + " bytes");
            Console.WriteLine("Registered User Count: " + info.RegisteredUserCount);
            Console.WriteLine("Active Client Count: " + info.ActiveClientCount);
            Console.ReadKey();
        }

        private void ShowActivityLogs()
        {
            Console.Write("Number of recent logs: ");
            var countText = Console.ReadLine();
            int count;
            int.TryParse(countText, out count);
            if (count <= 0) count = 20;

            var logs = _client.GetActivityLogs(_currentUser, count);
            foreach (var log in logs)
            {
                Console.WriteLine("[" + log.Timestamp + "] User: " + log.Username + " Operation: " + log.Operation + " Target: " + log.Target + " Status: " + log.Status);
            }
            Console.ReadKey();
        }

        private void Logout()
        {
            var result = _client.Logout(_currentUser);
            Console.WriteLine(result.Success ? result.Message : "Error: " + result.Message);
            _currentUser = null;
            Console.ReadKey();
        }

        private static string ReadPassword()
        {
            var pwd = "";
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd = pwd.Substring(0, pwd.Length - 1);
                    }
                    continue;
                }
                pwd += key.KeyChar;
            }
            Console.WriteLine();
            return pwd;
        }
    }
}
