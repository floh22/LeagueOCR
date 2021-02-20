using Common;
using Microsoft.Owin.Hosting;
using System;

namespace Server
{
    public class HttpServer
    {

        public int port;
        public string uri;

        private IDisposable _webApp;

        public static AOIList AOIList;

        public HttpServer(int port)
        {
            this.port = port;
            uri = string.Concat("http://localhost:", port);
            AOIList = new AOIList();
        }

        public void StartServer()
        {
            _webApp = WebApp.Start<Server.Startup>(uri);
           
        }

        public void StopServer()
        {
            Console.WriteLine("Shutting Down Server");
            _webApp.Dispose();
        }
    }
}
