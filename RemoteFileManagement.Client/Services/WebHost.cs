using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using RemoteFileManagement.Shared.Models;

namespace RemoteFileManagement.Client.Services
{
    public class WebHost
    {
        private readonly RemotingClient _client;
        private readonly int _port;
        private string CookieName { get { return "session_" + _port; } }
        private readonly HttpListener _listener = new HttpListener();
        private JavaScriptSerializer Serializer { get { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }; } }
        private readonly Dictionary<string, UserDto> _sessions = new Dictionary<string, UserDto>();

        public WebHost(RemotingClient client, int port)
        {
            _client = client;
            _port = port;
        }

        public void Run(bool openBrowser = true)
        {
            _listener.Prefixes.Add("http://localhost:" + _port + "/");
            _listener.Start();
            Console.WriteLine("Web interface: http://localhost:" + _port + "/");
            if (openBrowser) Process.Start("http://localhost:" + _port + "/");
            Console.WriteLine("Press Ctrl+C to stop the client.");
            while (true)
            {
                var context = _listener.GetContext();
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { Handle(context); }
                    catch (Exception ex)
                    {
                        try { WriteJson(context, new { success = false, message = ex.Message }, 500); }
                        catch { context.Response.Abort(); }
                    }
                    finally { System.Runtime.Remoting.Messaging.CallContext.FreeNamedDataSlot("SessionToken"); }
                });
            }
        }

        private void Handle(HttpListenerContext context)
        {
            System.Runtime.Remoting.Messaging.CallContext.FreeNamedDataSlot("SessionToken");
            var path = context.Request.Url.AbsolutePath;
            if (path == "/" || path == "/index.html")
            {
                var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web", "index.html");
                var bytes = File.ReadAllBytes(file);
                context.Response.Headers["Cache-Control"] = "no-store";
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
                return;
            }

            if (path == "/api/register")
            {
                var request = ReadBody<Dictionary<string, object>>(context);
                WriteJson(context, _client.Register(Value(request, "username"), Value(request, "password"), Value(request, "displayName")));
                return;
            }

            if (path == "/api/login")
            {
                var request = ReadBody<Dictionary<string, object>>(context);
                var user = _client.Login(Value(request, "username"), Value(request, "password"));
                if (!user.IsAuthenticated)
                {
                    WriteJson(context, new { success = false, message = "Login failed." }, 401);
                    return;
                }

                var oldCookie = context.Request.Cookies[CookieName];
                if (oldCookie != null)
                {
                    UserDto old;
                    lock (_sessions)
                    {
                        if (_sessions.TryGetValue(oldCookie.Value, out old))
                        {
                            System.Runtime.Remoting.Messaging.CallContext.LogicalSetData("SessionToken", old.SessionToken);
                            _client.Logout(old.Username);
                            _sessions.Remove(oldCookie.Value);
                        }
                    }
                }
                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData("SessionToken", user.SessionToken);
                var token = Guid.NewGuid().ToString("N");
                lock (_sessions) _sessions[token] = user;
                context.Response.Headers.Add("Set-Cookie", CookieName + "=" + token + "; Path=/; HttpOnly");
                WriteJson(context, new { success = true, username = user.Username, displayName = user.DisplayName });
                return;
            }

            var username = GetUser(context);
            if (username == null)
            {
                WriteJson(context, new { success = false, message = "Please log in." }, 401);
                return;
            }

            if (path == "/api/logout")
            {
                var result = _client.Logout(username);
                if (!result.Success) { WriteJson(context, result); return; }
                lock (_sessions) _sessions.Remove(context.Request.Cookies[CookieName].Value);
                context.Response.Headers.Add("Set-Cookie", CookieName + "=; Path=/; HttpOnly; Max-Age=0");
                WriteJson(context, new { success = true });
                return;
            }

            if (path == "/api/list")
            {
                var pathValue = Query(context, "path", "/");
                WriteJson(context, new { success = true, path = pathValue, directories = _client.ListDirectories(username, pathValue), files = _client.ListFiles(username, pathValue) });
                return;
            }

            if (path == "/api/upload")
            {
                var request = ReadBody<Dictionary<string, object>>(context);
                var result = _client.UploadFile(username, Value(request, "name"), Convert.FromBase64String(Value(request, "content")));
                WriteJson(context, result);
                return;
            }

            if (path == "/api/download")
            {
                var bytes = _client.DownloadFile(username, Query(context, "path", "/"));
                if (bytes == null) { WriteJson(context, new { success = false, message = "File not found." }, 404); return; }
                context.Response.ContentType = "application/octet-stream";
                context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + Path.GetFileName(Query(context, "path", "/")) + "\"");
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
                return;
            }

            if (path == "/api/create-directory")
            {
                var request = ReadBody<Dictionary<string, object>>(context);
                WriteJson(context, _client.CreateDirectory(username, Value(request, "path")));
                return;
            }

            if (path == "/api/delete")
            {
                var request = ReadBody<Dictionary<string, object>>(context);
                var target = Value(request, "path");
                var result = string.Equals(Value(request, "directory"), "true", StringComparison.OrdinalIgnoreCase) ? _client.DeleteDirectory(username, target) : _client.DeleteFile(username, target);
                WriteJson(context, result);
                return;
            }

            if (path == "/api/rename")
            {
                var request = ReadBody<Dictionary<string, object>>(context);
                WriteJson(context, _client.RenameFile(username, Value(request, "path"), Value(request, "name")));
                return;
            }

            if (path == "/api/text")
            {
                var filePath = Query(context, "path", "/");
                if (context.Request.HttpMethod == "GET") WriteJson(context, new { success = true, content = _client.ReadTextFile(username, filePath) });
                else { var request = ReadBody<Dictionary<string, object>>(context); WriteJson(context, _client.WriteTextFile(username, filePath, Value(request, "content"), string.Equals(Value(request, "append"), "true", StringComparison.OrdinalIgnoreCase))); }
                return;
            }

            if (path == "/api/search")
            {
                WriteJson(context, new { success = true, files = _client.SearchFiles(username, Query(context, "pattern", "*"), true) });
                return;
            }

            if (path == "/api/change-password" || path == "/api/copy" || path == "/api/move" || path == "/api/compress" || path == "/api/extract")
            {
                var request = ReadBody<Dictionary<string, object>>(context);
                OperationResult result;
                if (path == "/api/change-password") result = _client.ChangePassword(username, Value(request, "oldPassword"), Value(request, "newPassword"));
                else if (path == "/api/copy") result = _client.CopyFile(username, Value(request, "source"), Value(request, "destination"));
                else if (path == "/api/move") result = _client.MoveFile(username, Value(request, "source"), Value(request, "destination"));
                else if (path == "/api/compress") result = _client.Compress(username, Value(request, "source"), Value(request, "destination"));
                else result = _client.Extract(username, Value(request, "source"), Value(request, "destination"));
                WriteJson(context, result);
                return;
            }
            if (path == "/api/file-info") { WriteJson(context, new { success = true, info = _client.GetFileInfo(username, Query(context, "path", "")) }); return; }
            if (path == "/api/hash") { WriteJson(context, new { success = true, hash = _client.CalculateHash(username, Query(context, "path", "")) }); return; }
            if (path == "/api/logs") { int count; int.TryParse(Query(context, "count", "20"), out count); WriteJson(context, new { success = true, logs = _client.GetActivityLogs(username, count) }); return; }

            if (path == "/api/server-info")
            {
                WriteJson(context, new { success = true, info = _client.GetServerInfo() });
                return;
            }

            WriteJson(context, new { success = false, message = "Not found." }, 404);
        }

        private string GetUser(HttpListenerContext context)
        {
            var cookie = context.Request.Cookies[CookieName];
            if (cookie == null) return null;
            lock (_sessions)
            {
                UserDto user;
                if (!_sessions.TryGetValue(cookie.Value, out user)) return null;
                System.Runtime.Remoting.Messaging.CallContext.LogicalSetData("SessionToken", user.SessionToken);
                return user.Username;
            }
        }

        private T ReadBody<T>(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding)) return Serializer.Deserialize<T>(reader.ReadToEnd());
        }

        private string Query(HttpListenerContext context, string key, string fallback)
        {
            return string.IsNullOrEmpty(context.Request.QueryString[key]) ? fallback : context.Request.QueryString[key];
        }

        private string Value(Dictionary<string, object> values, string key)
        {
            return values.ContainsKey(key) && values[key] != null ? Convert.ToString(values[key]) : string.Empty;
        }

        private void WriteJson(HttpListenerContext context, object value, int statusCode = 200)
        {
            var bytes = Encoding.UTF8.GetBytes(Serializer.Serialize(value));
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
