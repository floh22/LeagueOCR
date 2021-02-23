using Common;
using Microsoft.Owin.Hosting;
using Server.Controllers;
using Server.Models;
using System;
using System.Collections.Generic;

namespace Server
{
    public class HttpServer
    {

        public int port;
        public string uri;

        private IDisposable _webApp;

        public static AOIList AOIList;
        public static bool trackLowerValues = true;

        public static Team blueTeam = new Team(0, "ORDER", 2500);
        public static Team redTeam = new Team(1, "CHAOS", 2500);

        public static List<List<int>> oldValues;

        public static bool OnlyIncreaseGold = false;

        public HttpServer(int port)
        {
            this.port = port;
            uri = string.Concat("http://localhost:", port);
            AOIList = new AOIList();
            oldValues = new List<List<int>>();
            oldValues.Add(new List<int>());
            oldValues.Add(new List<int>());
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

        public static void UpdateTeams()
        {
            TeamsController.UpdateTeams();
        }
    }
}
