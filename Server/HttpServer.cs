using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class HttpServer
    {

        public int port;
        public string uri;

        private IDisposable _webApp;

        public HttpServer(int port)
        {
            this.port = port;
            uri = string.Concat("http://localhost:", port);
        }

        public void StartServer()
        {
            using (_webApp = WebApp.Start<Startup>(uri))
            {
                Console.WriteLine("Server started on " + uri);
                Console.WriteLine("Press [enter] to quit...");
                Console.ReadLine();
            }
        }

        public void StopServer()
        {
            Console.WriteLine("Shutting Down Server");
            _webApp.Dispose();
        }
    }
}
