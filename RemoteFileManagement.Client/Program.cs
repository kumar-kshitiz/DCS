using System;
using System.IO;
using System.Xml;
using RemoteFileManagement.Client.Services;
using RemoteFileManagement.Client.UI;

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
                var client = new RemotingClient(host, port);
                var ui = new ConsoleUI(client);
                ui.Run();
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
