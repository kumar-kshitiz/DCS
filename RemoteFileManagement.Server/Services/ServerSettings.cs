using System;
using System.IO;
using System.Xml;

namespace RemoteFileManagement.Server.Services
{
    public static class ServerSettings
    {
        public static string Host { get; private set; }
        public static int Port { get; private set; }
        public static string StorageRoot { get; private set; }
        public static string LogDirectory { get; private set; }
        public static long MaxUploadSizeBytes { get; private set; }
        public static DateTime ServerStartTime { get; private set; }

        public static void Load()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(baseDirectory, "Server.config");
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("Server configuration file not found.", configPath);
            }

            var doc = new XmlDocument();
            doc.Load(configPath);

            var appSettings = doc.SelectSingleNode("/configuration/appSettings");
            if (appSettings == null)
            {
                throw new InvalidOperationException("Server configuration is missing appSettings.");
            }

            Host = GetValue(appSettings, "ServerHost", "127.0.0.1");
            Port = int.Parse(GetValue(appSettings, "RemotingPort", "9090"));
            StorageRoot = GetValue(appSettings, "StorageRoot", "Storage");
            LogDirectory = GetValue(appSettings, "LogDirectory", "Storage/Logs");
            MaxUploadSizeBytes = long.Parse(GetValue(appSettings, "MaxUploadSizeBytes", "10485760"));

            StorageRoot = ResolvePath(StorageRoot, baseDirectory);
            LogDirectory = ResolvePath(LogDirectory, baseDirectory);
            ServerStartTime = DateTime.Now;

            Directory.CreateDirectory(StorageRoot);
            Directory.CreateDirectory(LogDirectory);
        }

        private static string ResolvePath(string configuredPath, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return baseDirectory;
            }

            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            return Path.GetFullPath(Path.Combine(baseDirectory, configuredPath));
        }

        private static string GetValue(XmlNode appSettings, string key, string defaultValue)
        {
            var node = appSettings.SelectSingleNode("add[@key='" + key + "']");
            if (node == null || string.IsNullOrWhiteSpace(node.Attributes["value"].Value))
            {
                return defaultValue;
            }

            return node.Attributes["value"].Value;
        }
    }
}
