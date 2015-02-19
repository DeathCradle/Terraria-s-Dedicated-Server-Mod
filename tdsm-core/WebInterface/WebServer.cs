﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using tdsm.core.Logging;
using tdsm.core.ServerCore;
using tdsm.core.WebInterface.Auth;

namespace tdsm.core.WebInterface
{
    public class RequestEndException : Exception { }

    public abstract class WebPage
    {
        public abstract void ProcessRequest(WebRequest request);
    }

    public class JSPacket : WebPage
    {
        public override void ProcessRequest(WebRequest request)
        {

        }
    }

    public static class WebServer
    {
        static IHttpAuth Authentication = new HttpDigestAuth();

        /// <summary>
        /// Allows a user to disable serving web files from the server, but rather from another application such as nginx or apache.
        /// </summary>
        public static bool ServeWebFiles { get; set; }

        //private static System.Collections.Generic.Queue<String> _additionalModules = new System.Collections.Generic.Queue<String>();
        private static System.Collections.Generic.Dictionary<String, WebPage> _pages = new System.Collections.Generic.Dictionary<String, WebPage>();

        public static readonly string HtmlDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "WebInterface", "Files");

        //public static bool RegisterModule(string path)
        //{
        //    if (_additionalModules.Contains(path)) return false;
        //    lock (_additionalModules) _additionalModules.Enqueue(path);
        //    return true;
        //}

        //TODO: use reflection in plugins
        public static bool RegisterPage(string request, WebPage page)
        {
            if (_pages.ContainsKey(request)) return false;
            lock (_pages) _pages.Add(request, page);
            return true;
        }

        public static void Begin(string bindAddress)
        {
            var split = bindAddress.Split(':');
            IPAddress addr;
            ushort port;

            if (split.Length != 2 || !IPAddress.TryParse(split[0], out addr) || !ushort.TryParse(split[1], out port) || port < 1)
            {
                ProgramLog.Error.Log("{0} is not a valid bind address, web server disabled.", bindAddress);
                return;
            }
            var server = new TcpListener(addr, port);

            //if (RegisterModule("tdsm.admin.js")) 
            if (!RegisterPage("/api/tiles.json", new JSPacket()))
            {
                ProgramLog.Error.Log("Failed to register web api module.");
                return;
            }

            server.Start();

            (new System.Threading.Thread(() =>
            {
                System.Threading.Thread.CurrentThread.Name = "Web";
                ProgramLog.Admin.Log("Web server started on {0}.", bindAddress);

                server.Server.Poll(500000, SelectMode.SelectRead);
                for (; ; )
                {
                    //var client = = server.AcceptSocket();
                    //AcceptClient(client);
                    var handle = server.BeginAcceptSocket(AcceptClient, server);
                    handle.AsyncWaitHandle.WaitOne();
                }

                ProgramLog.Admin.Log("Web server exited.", bindAddress);
            })).Start();
        }

        static void AcceptClient(IAsyncResult result)
        {
            var server = result.AsyncState as TcpListener;
            var client = server.EndAcceptSocket(result);
            client.NoDelay = true;
            try
            {

                string addr;
                var rep = client.RemoteEndPoint;
                if (rep != null)
                    addr = rep.ToString();
                else
                {
                    //return false;
                }

                var req = new WebRequest(client);
                req.StartReceiving(new byte[4192]);
            }
            catch (RequestEndException) { return; }
            catch (SocketException)
            {
                //client.SafeClose();
            }
            catch (Exception e)
            {
                //return false;
                Console.WriteLine(e);
            }

            client.SafeClose();
        }

        static string HandleSocketException(Exception e)
        {
            if (e is SocketException)
                return e.Message;
            if (e is System.IO.IOException)
                return e.Message;
            else if (e is ObjectDisposedException)
                return "Socket already disposed";
            else
                throw new Exception("Unexpected exception in socket handling code", e);
        }


        private static FileInfo GetEncapsulated(WebRequest request)
        {
            if (request.Path != null)
            {
                var url = request.Path;
                if (url == "/") url = "index.html";
                url = url.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                while (url.StartsWith("\\")) url = url.Remove(0, 1);
                var local = Path.Combine(/*WebServer.HtmlDirectory*/ @"C:\Development\Sync\Terraria-s-Dedicated-Server-Mod\tdsm-core\WebInterface\Files", url);

                if (Path.GetFullPath(local).StartsWith(@"C:\Development\Sync\Terraria-s-Dedicated-Server-Mod\tdsm-core\WebInterface\Files"))
                    return new FileInfo(local);
            }
            return null;
        }

        private static readonly Dictionary<String, String> _contentTypes = new Dictionary<String, String>()
        {
            {".js", "application/javascript"},
            {".css", "text/css"}
        };

        internal static void HandleRequest(WebRequest request, string content)
        {
            //var url = this.RequestUrl;
            //if (url == "/") url = "index.html";
            //var local = Path.Combine(/*WebServer.HtmlDirectory*/ @"C:\Development\Sync\Terraria-s-Dedicated-Server-Mod\tdsm-core\WebInterface\Files", EncapsulatePath(url));
            var local = GetEncapsulated(request);

            if (local != null && local.Exists)
            {
                Console.WriteLine("Sending file");
                //TODO implement cache

                using (var fs = local.OpenRead())
                {
                    var ext = local.Extension.ToLower();
                    var contentType = "text/html";
                    if (_contentTypes.ContainsKey(ext)) contentType = _contentTypes[ext];

                    request.RepsondHeader(200, "OK", contentType, fs.Length);

                    var buf = new byte[512];
                    while (fs.Position < fs.Length)
                    {
                        var read = fs.Read(buf, 0, buf.Length);
                        if (read > 0)
                        {
                            request.Send(buf, read, SocketFlags.None);
                        }
                        else break;
                    }
                }

                request.Client.SafeClose();
                throw new RequestEndException();
            }
            else
            {
                //var segments = request.RequestUrl.Split('/');
                if (request.Path != null)
                    if (_pages.ContainsKey(request.Path))
                    {
                        _pages[request.Path].ProcessRequest(request);
                        request.Client.SafeClose();
                        throw new RequestEndException();
                    }
            }
        }
    }
}
