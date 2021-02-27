using Common;
using Microsoft.Owin.Hosting;
using Server.Controllers;
using Server.Models;
using System;
using System.Collections.Generic;
using static Common.Utils;

namespace Server
{
    public class HttpServer
    {

        public int port;
        public string uri;

        private IDisposable _webApp;

        public static bool trackLowerValues = true;

        public static Team blueTeam = new Team(0, "ORDER", 2500);
        public static Team redTeam = new Team(1, "CHAOS", 2500);

        public static Objective baron = new Objective("Baron", 1800, false);
        public static Objective dragon = new Objective("Dragon", 300, false);
        public static List<DragonType> oldTypes;

        public static List<List<int>> oldValues;

        public static bool OnlyIncreaseGold = false;

        public HttpServer(int port)
        {
            this.port = port;
            uri = string.Concat("http://localhost:", port);
            oldValues = new List<List<int>>();
            //blue Team
            oldValues.Add(new List<int>());
            //red Team
            oldValues.Add(new List<int>());
            //dragon Timer
            oldValues.Add(new List<int>());
            dragon.IsAlive = false;
            //baron Timer
            oldValues.Add(new List<int>());
            baron.IsAlive = false;

            oldTypes = new List<DragonType>();
        }

        public void StartServer()
        {
            _webApp = WebApp.Start<Server.Startup>(uri);

            //Init objective values
            dragon.Cooldown = int.MaxValue;
            baron.Cooldown = int.MaxValue;
        }

        public void StopServer()
        {
            Console.WriteLine("Shutting Down Server");

            //Reset state data
            baron.TimesTakenInMatch = -1;
            dragon.TimesTakenInMatch = -1;
            oldTypes.Clear();
            oldValues.ForEach((vList) => vList.Clear());

            //Stop Web server
            _webApp.Dispose();
        }

        public static void UpdateTeams()
        {
            TeamsController.UpdateTeams();
        }

        public static void UpdateNeutralTimers()
        {
            ObjectivesController.UpdateObjectives();
        }
    }
}
