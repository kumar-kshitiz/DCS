using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using RemoteFileManagement.Client.Services;
using RemoteFileManagement.Server.Services;
using RemoteFileManagement.Shared.Interfaces;

public static class IntegrationTests
{
    private static int assertions;
    private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    private sealed class Browser
    {
        public string Url;
        public CookieContainer Cookies = new CookieContainer();
        public Browser(int port) { Url = "http://localhost:" + port; }
        public Dictionary<string, object> Get(string path) { return Request(path, null); }
        public Dictionary<string, object> Post(string path, object body) { return Request(path, body); }
        private Dictionary<string, object> Request(string path, object body)
        {
            var request = (HttpWebRequest)WebRequest.Create(Url + path);
            request.CookieContainer = Cookies;
            request.Timeout = 15000;
            if (body != null)
            {
                request.Method = "POST";
                request.ContentType = "application/json";
                var bytes = Encoding.UTF8.GetBytes(Json.Serialize(body));
                using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
            }
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
                return Json.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
        }
        public byte[] Download(string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(Url + "/api/download?path=" + Uri.EscapeDataString(path));
            request.CookieContainer = Cookies;
            using (var response = request.GetResponse())
            using (var output = new MemoryStream())
            { response.GetResponseStream().CopyTo(output); return output.ToArray(); }
        }
    }
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Interlocked.Increment(ref assertions);
    }
    private static void Ok(Dictionary<string, object> result, string action)
    { Check(result.ContainsKey("Success") ? (bool)result["Success"] : (bool)result["success"], action + ": " + Json.Serialize(result)); }
    private static int Port()
    { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    private static object[] Items(Dictionary<string, object> result, string name)
    { return ((System.Collections.ArrayList)result[name]).ToArray(); }
    private static Dictionary<string, object> Item(object value) { return (Dictionary<string, object>)value; }
    private static void StartWeb(RemotingClient client, int port)
    {
        var thread = new Thread(() => new WebHost(client, port).Run(false)) { IsBackground = true };
        thread.Start();
        for (int i = 0; i < 100; i++)
        {
            try { new Browser(port).Get("/api/list"); }
            catch (ArgumentException) { return; } // HTML was served; JSON parsing failed.
            catch (WebException ex) { if (ex.Response != null && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Unauthorized) return; Thread.Sleep(50); continue; }
            return;
        }
        throw new Exception("Web listener did not start.");
    }
    public static int Main()
    {
        try
        {
            ThreadPool.SetMinThreads(128, 128); // Test clients, HTTP hosts, and remoting share this process.
            ServicePointManager.DefaultConnectionLimit = 32;
            ServerSettings.Load();
            var channel = new TcpChannel(Port());
            ChannelServices.RegisterChannel(channel, false);
            int serverPort = int.Parse(new Uri(channel.GetUrlsForUri("AuthService")[0]).Port.ToString());
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(AuthService), "AuthService", WellKnownObjectMode.Singleton);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(FileService), "FileService", WellKnownObjectMode.Singleton);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(DirectoryService), "DirectoryService", WellKnownObjectMode.Singleton);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(SystemService), "SystemService", WellKnownObjectMode.Singleton);
            new AuthService();
            var remoting = new RemotingClient("127.0.0.1", serverPort);
            var remoting2 = new RemotingClient("127.0.0.1", serverPort); // unique channels in one process
            int webPort = Port(), webPort2 = Port();
            StartWeb(remoting, webPort); StartWeb(remoting2, webPort2);
            var a = new Browser(webPort); var a2 = new Browser(webPort2); var b = new Browser(webPort);
            bool denied = false;
            try { new Browser(webPort).Get("/api/list"); } catch (WebException ex) { denied = ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Unauthorized; }
            Check(denied, "Anonymous web request must be denied.");
            Ok(a.Post("/api/register", new { username = "tester", password = "test123", displayName = "Test User" }), "register");
            Check(!(bool)a.Post("/api/register", new { username = "tester", password = "test123" })["Success"], "Duplicate registration must fail.");
            Ok(a.Post("/api/login", new { username = "tester", password = "test123" }), "login");
            Ok(a2.Post("/api/login", new { username = "tester", password = "test123" }), "second same-account login");
            Ok(b.Post("/api/login", new { username = "bob", password = "bob123" }), "distinct-account login");
            Check(Convert.ToInt32(Item(a.Get("/api/server-info")["info"])["ActiveClientCount"]) == 3, "Count clients rather than distinct usernames.");
            Ok(a.Post("/api/create-directory", new { path = "docs" }), "create directory");
            var folderWrite = a.Post("/api/text?path=docs", new { content = "invalid", append = false });
            Check(!(bool)folderWrite["Success"] && ((string)folderWrite["Message"]).Contains("This path is a folder"), "Reject writing to directory with a clear message.");
            Ok(a.Post("/api/text?path=docs/created.txt", new { content = "created inside folder", append = false }), "create file inside folder");
            Check((string)a.Get("/api/text?path=docs/created.txt")["content"] == "created inside folder", "Read newly created nested file.");
            Ok(a.Post("/api/delete", new { path = "docs/created.txt", directory = false }), "delete nested test file");
            var content = Convert.ToBase64String(Encoding.UTF8.GetBytes("hello"));
            Ok(a.Post("/api/upload", new { name = "docs/note.txt", content = content }), "nested upload");
            Check(Encoding.UTF8.GetString(a2.Download("docs/note.txt")) == "hello", "Same-account clients see the same storage.");
            Check(Items(b.Get("/api/list?path=/"), "files").Length == 0, "Different accounts have isolated storage.");
            Check(Items(a.Get("/api/list?path=/"), "directories").Length == 1, "Root slash navigation works.");
            Check(Items(a.Get("/api/list?path=docs"), "files").Length == 1, "List nested files.");
            Check(Items(a.Get("/api/search?pattern=*.txt"), "files").Length == 1, "Search returns metadata.");
            Check((string)a.Get("/api/text?path=docs/note.txt")["content"] == "hello", "Read text.");
            Ok(a.Post("/api/text?path=docs/note.txt", new { content = "new", append = false }), "overwrite text");
            Ok(a2.Post("/api/text?path=docs/note.txt", new { content = "!", append = true }), "append text");
            Check((string)a.Get("/api/text?path=docs/note.txt")["content"] == "new!", "Append content is preserved.");
            Check(Convert.ToInt64(Item(a.Get("/api/file-info?path=docs/note.txt")["info"])["Size"]) > 0, "File metadata.");
            Check(((string)a.Get("/api/hash?path=docs/note.txt")["hash"]).Length == 64, "SHA-256 hash.");
            Ok(a.Post("/api/copy", new { source = "docs/note.txt", destination = "copy.txt" }), "copy");
            Ok(a.Post("/api/create-directory", new { path = "destination" }), "destination folder");
            Ok(a.Post("/api/copy", new { source = "copy.txt", destination = "destination" }), "copy to existing folder");
            Check(Encoding.UTF8.GetString(a.Download("destination/copy.txt")).Contains("new!"), "Copy keeps filename inside folder.");
            Ok(a.Post("/api/move", new { source = "copy.txt", destination = "docs" }), "move to existing folder");
            Check(Encoding.UTF8.GetString(a.Download("docs/copy.txt")).Contains("new!"), "Move keeps filename inside folder.");
            Ok(a.Post("/api/move", new { source = "docs/copy.txt", destination = "moved.txt" }), "move");
            Ok(a.Post("/api/rename", new { path = "moved.txt", name = "renamed.txt" }), "rename");
            Ok(a.Post("/api/compress", new { source = "docs", destination = "archives/docs" }), "compress directory");
            Ok(a.Post("/api/extract", new { source = "archives/docs.zip", destination = "extracted" }), "extract");
            Check((string)a.Get("/api/text?path=extracted/note.txt")["content"] == "new!", "ZIP round trip.");
            Ok(a.Post("/api/compress", new { source = "renamed.txt", destination = "single.zip" }), "compress file");
            Check(!(bool)a.Post("/api/compress", new { source = "docs", destination = "docs/self.zip" })["Success"], "Reject archive within source.");
            Ok(a.Post("/api/upload", new { name = "empty.txt", content = "" }), "empty upload");
            Check(a.Download("empty.txt").Length == 0, "Empty download.");
            Check((string)a.Get("/api/text?path=empty.txt")["content"] == "", "Empty read.");
            // Exceeds JavaScriptSerializer's default limit, but remains below the server upload limit.
            var big = new byte[2 * 1024 * 1024];
            Ok(a.Post("/api/upload", new { name = "large.bin", content = Convert.ToBase64String(big) }), "large JSON upload");
            Check(a.Download("large.bin").Length == big.Length, "Large download.");
            Check(!(bool)a.Post("/api/upload", new { name = "../bob/escape.txt", content = content })["Success"], "Reject traversal upload.");
            Ok(a.Post("/api/create-directory", new { path = "parallel" }), "parallel folder");
            Task.WaitAll(Enumerable.Range(0, 32).Select(i => Task.Factory.StartNew(() =>
            {
                var browser = i % 2 == 0 ? a : a2;
                Ok(browser.Post("/api/upload", new { name = "parallel/" + i + ".txt", content = content }), "parallel upload");
                browser.Get("/api/list?path=parallel");
            })).ToArray());
            Check(Items(a.Get("/api/list?path=parallel"), "files").Length == 32, "All parallel uploads complete.");
            Ok(a.Post("/api/text?path=append.txt", new { content = "", append = false }), "create append file");
            Task.WaitAll(Enumerable.Range(0, 24).Select(i => Task.Factory.StartNew(() =>
                Ok((i % 2 == 0 ? a : a2).Post("/api/text?path=append.txt", new { content = "x", append = true }), "parallel append"))).ToArray());
            Check((string)a.Get("/api/text?path=append.txt")["content"] == new string('x', 24), "Concurrent appends lose no content.");
            Ok(a.Post("/api/change-password", new { oldPassword = "test123", newPassword = "changed123" }), "password change");
            bool badLogin = false;
            try { new Browser(webPort).Post("/api/login", new { username = "tester", password = "test123" }); }
            catch (WebException) { badLogin = true; }
            Check(badLogin, "Old password fails.");
            Ok(a.Get("/api/logout"), "logout one client");
            Check(Convert.ToInt32(Item(a2.Get("/api/server-info")["info"])["ActiveClientCount"]) == 2, "Logout only removes one session.");
            Check(Items(a2.Get("/api/list?path=docs"), "files").Length == 1, "Other same-account session remains active.");
            denied = false;
            try { a.Get("/api/list"); } catch (WebException) { denied = true; }
            Check(denied, "Logged-out cookie cannot access web API.");
            Ok(a.Post("/api/login", new { username = "tester", password = "changed123" }), "new password login");
            Ok(a.Post("/api/delete", new { path = "renamed.txt", directory = false }), "delete file");
            Ok(a.Post("/api/delete", new { path = "parallel", directory = true }), "recursive delete");
            Check(!(bool)a.Post("/api/delete", new { path = "/", directory = true })["Success"], "Root deletion is denied.");
            var rawFile = (IFileService)Activator.GetObject(typeof(IFileService), "tcp://127.0.0.1:" + serverPort + "/FileService");
            CallContext.FreeNamedDataSlot("SessionToken");
            Check(!rawFile.UploadFile("tester", "unauthorized.txt", new byte[] { 1 }).Success, "Direct unauthenticated remote call is denied.");
            var logs = Items(a.Get("/api/logs?count=1000"), "logs").Select(Item).ToArray();
            Check(logs.All(x => string.Equals((string)x["Username"], "tester", StringComparison.OrdinalIgnoreCase)), "Log filtering is exact.");
            foreach (var operation in new[] { "REGISTER", "LOGIN", "CHANGE_PASSWORD", "LOGOUT", "UPLOAD", "DOWNLOAD", "DELETE", "RENAME", "COPY", "MOVE", "READ_TEXT", "WRITE_TEXT", "APPEND_TEXT", "FILE_INFO", "HASH", "CREATE_DIRECTORY", "DELETE_DIRECTORY", "LIST_FILES", "LIST_DIRECTORIES", "SEARCH", "COMPRESS", "EXTRACT" })
                Check(logs.Any(x => (string)x["Operation"] == operation), "Missing log: " + operation);
            Check(logs.Any(x => ((string)x["Status"]).StartsWith("FAILURE")), "Failed actions are logged.");
            Check(logs.Count(x => (string)x["Operation"] == "APPEND_TEXT" && (string)x["Status"] == "SUCCESS") == 25, "Parallel logs lose no entries.");
            Ok(a.Get("/api/logout"), "final logout A"); Ok(a2.Get("/api/logout"), "final logout A2"); Ok(b.Get("/api/logout"), "final logout B");
            Check(remoting.GetServerInfo().ActiveClientCount == 0, "All clients logged out.");
            Console.WriteLine("PASS: " + assertions + " assertions, all existing actions and concurrent clients.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}