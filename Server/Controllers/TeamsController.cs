using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Server.Controllers
{
    public class TeamsController: ApiController
    {
        Team blueTeam = new Team();
        Team redTeam = new Team();

        public IEnumerable<Team> GetAllTeams()
        {
            UpdateTeams();

            return new Team[] { blueTeam, redTeam };
        }

        public IHttpActionResult GetTeam(int id)
        {
            UpdateTeams();

            if (id < 0 || id > 1)
                return NotFound();

            return (id == 0) ? Ok(blueTeam) : Ok(redTeam);
        }

        public IHttpActionResult GetTeam(string name)
        {
            UpdateTeams();

            if (name.Equals("ORDER", StringComparison.OrdinalIgnoreCase))
                return Ok(blueTeam);
            else if (name.Equals("CHAOS", StringComparison.OrdinalIgnoreCase))
                return Ok(redTeam);
            return NotFound();
        }

        private void UpdateTeams()
        {
            blueTeam.Gold = GoldToInt(HttpServer.AOIList.Blue_Gold.CurrentContent, blueTeam.Gold);
            redTeam.Gold = GoldToInt(HttpServer.AOIList.Red_Gold.CurrentContent, redTeam.Gold);
        }

        private int GoldToInt(string goldValue, int backup)
        {
            //Have backup value incase OCR goes wrong somehow and the int cant be parsed. In that case fall back to the last value
            //This should keep the gold value from fluctuating or displaying something completely incorrect
            //Maybe do this step in OCR directly?
            try
            {
                var parse = Int32.Parse(goldValue.Replace("k", ""));
                return parse;
            }
            catch (Exception)
            {
                return backup;
            }
        }
    }
}
