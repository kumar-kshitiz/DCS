using System;
using System.IO;
using System.Xml;
using RemoteFileManagement.Client.Services;

namespace RemoteFileManagement.Client
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Client.config");
                var doc = new XmlDocument();
                doc.Load(configPath);

                var hostNode = doc.SelectSingleNode("/configuration/appSettings/add[@key='ServerHost']");
                var portNode = doc.SelectSingleNode("/configuration/appSettings/add[@key='RemotingPort']");

                var host = hostNode != null ? hostNode.Attributes["value"].Value : "127.0.0.1";
                var port = portNode != null ? int.Parse(portNode.Attributes["value"].Value) : 9090;

                Console.WriteLine("Connecting to server: tcp://" + host + ":" + port);
                var webPortNode = doc.SelectSingleNode("/configuration/appSettings/add[@key='WebPort']");
                var webPort = webPortNode != null ? int.Parse(webPortNode.Attributes["value"].Value) : 8080;
                var client = new RemotingClient(host, port);
                if (args.Length > 0) webPort = int.Parse(args[0]);
                var webHost = new WebHost(client, webPort);
                webHost.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client startup error: " + ex.Message);
                Console.WriteLine("Ensure the server is running and the port is correct.");
                Console.ReadKey();
            }
        }
    }
}
