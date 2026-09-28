using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using RemoteFileManagement.Server.Services;

namespace RemoteFileManagement.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                ServerSettings.Load();

                Console.WriteLine("========================================");
                Console.WriteLine("Remote File Management System - Server");
                Console.WriteLine("========================================");
                Console.WriteLine("Starting remoting service...");
                Console.WriteLine("Host: " + ServerSettings.Host);
                Console.WriteLine("Port: " + ServerSettings.Port);
                Console.WriteLine("Storage Root: " + ServerSettings.StorageRoot);

                var channel = new TcpChannel(ServerSettings.Port);
                ChannelServices.RegisterChannel(channel, false);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(FileService),
                    "FileService",
                    WellKnownObjectMode.Singleton);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(DirectoryService),
                    "DirectoryService",
                    WellKnownObjectMode.Singleton);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(AuthService),
                    "AuthService",
                    WellKnownObjectMode.Singleton);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(SystemService),
                    "SystemService",
                    WellKnownObjectMode.Singleton);

                Console.WriteLine("Remote services registered successfully.");
                Console.WriteLine("Press Ctrl+C to stop the server.");

                while (true)
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server startup failed: " + ex.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
