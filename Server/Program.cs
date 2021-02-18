using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {

        public static HttpServer HttpServer;

        static void Main(string[] args)
        {
            HttpServer = new HttpServer(3002);
            HttpServer.StartServer();
        }
    }
}
