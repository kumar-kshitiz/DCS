using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using RemoteFileManagement.Shared.Interfaces;
using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Client.Services
{
    public class RemotingClient
    {
        public string ServerHost { get; private set; }
        public int ServerPort { get; private set; }

        private readonly TcpChannel _channel;
        private readonly IAuthService _authService;
        private readonly IFileService _fileService;
        private readonly IDirectoryService _directoryService;
        private readonly ISystemService _systemService;

        public RemotingClient(string serverHost, int serverPort)
        {
            ServerHost = serverHost;
            ServerPort = serverPort;

            var channelName = "ClientChannel" + Guid.NewGuid().ToString("N");
            _channel = new TcpChannel();
            ChannelServices.RegisterChannel(_channel, false);

            var url = "tcp://" + serverHost + ":" + serverPort + "/";

            _authService = (IAuthService)Activator.GetObject(typeof(IAuthService), url + "AuthService");
            _fileService = (IFileService)Activator.GetObject(typeof(IFileService), url + "FileService");
            _directoryService = (IDirectoryService)Activator.GetObject(typeof(IDirectoryService), url + "DirectoryService");
            _systemService = (ISystemService)Activator.GetObject(typeof(ISystemService), url + "SystemService");
        }

        public UserDto Login(string username, string password)
        {
            return _authService.Login(username, password);
        }

        public OperationResult Register(string username, string password, string displayName)
        {
            return _authService.Register(username, password, displayName);
        }

        public OperationResult ChangePassword(string username, string oldPassword, string newPassword)
        {
            return _authService.ChangePassword(username, oldPassword, newPassword);
        }

        public OperationResult Logout(string username)
        {
            return _authService.Logout(username);
        }

        public OperationResult UploadFile(string username, string fileName, byte[] content)
        {
            return _fileService.UploadFile(username, fileName, content);
        }

        public byte[] DownloadFile(string username, string relativePath)
        {
            return _fileService.DownloadFile(username, relativePath);
        }

        public OperationResult RenameFile(string username, string sourceRelativePath, string newFileName)
        {
            return _fileService.RenameFile(username, sourceRelativePath, newFileName);
        }

        public OperationResult DeleteFile(string username, string relativePath)
        {
            return _fileService.DeleteFile(username, relativePath);
        }

        public OperationResult CopyFile(string username, string sourceRelativePath, string destinationRelativePath)
        {
            return _fileService.CopyFile(username, sourceRelativePath, destinationRelativePath);
        }

        public OperationResult MoveFile(string username, string sourceRelativePath, string destinationRelativePath)
        {
            return _fileService.MoveFile(username, sourceRelativePath, destinationRelativePath);
        }

        public string ReadTextFile(string username, string relativePath)
        {
            return _fileService.ReadTextFile(username, relativePath);
        }

        public OperationResult WriteTextFile(string username, string relativePath, string content, bool append)
        {
            return _fileService.WriteTextFile(username, relativePath, content, append);
        }

        public FileInfoDto GetFileInfo(string username, string relativePath)
        {
            return _fileService.GetFileInfo(username, relativePath);
        }

        public string CalculateHash(string username, string relativePath)
        {
            return _fileService.CalculateHash(username, relativePath);
        }

        public OperationResult CreateDirectory(string username, string relativePath)
        {
            return _directoryService.CreateDirectory(username, relativePath);
        }

        public OperationResult DeleteDirectory(string username, string relativePath)
        {
            return _directoryService.DeleteDirectory(username, relativePath);
        }

        public DirectoryInfoDto[] ListDirectories(string username, string relativePath)
        {
            return _directoryService.ListDirectories(username, relativePath);
        }

        public FileInfoDto[] ListFiles(string username, string relativePath)
        {
            return _directoryService.ListFiles(username, relativePath);
        }

        public FileInfoDto[] SearchFiles(string username, string searchPattern, bool recursive)
        {
            return _directoryService.SearchFiles(username, searchPattern, recursive);
        }

        public ServerInfoDto GetServerInfo()
        {
            return _systemService.GetServerInfo();
        }

        public ActivityLogDto[] GetActivityLogs(string username, int count)
        {
            return _systemService.GetActivityLogs(username, count);
        }

        public OperationResult Compress(string username, string sourceRelativePath, string archiveRelativePath)
        {
            return _systemService.Compress(username, sourceRelativePath, archiveRelativePath);
        }

        public OperationResult Extract(string username, string archiveRelativePath, string destinationRelativePath)
        {
            return _systemService.Extract(username, archiveRelativePath, destinationRelativePath);
        }
    }
}
