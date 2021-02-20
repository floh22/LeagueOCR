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
        public Team blueTeam;
        public Team redTeam;

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
            if (blueTeam == null)
                blueTeam = new Team(0, "ORDER", 3200);
            if (redTeam == null)
                redTeam = new Team(1, "CHAOS", 3200);
            string blueGoldText = HttpServer.AOIList.Blue_Gold.CurrentContent;
            string redGoldText = HttpServer.AOIList.Red_Gold.CurrentContent;
            Console.WriteLine(blueGoldText + ", " + redGoldText);
            blueTeam.Gold = GoldToInt(blueGoldText, blueTeam.Gold);
            redTeam.Gold = GoldToInt(redGoldText, redTeam.Gold);
        }

        private int GoldToInt(string goldValue, int backup)
        {
            //Have backup value incase OCR goes wrong somehow and the int cant be parsed. In that case fall back to the last value
            //This should keep the gold value from fluctuating or displaying something completely incorrect
            //Maybe do this step in OCR directly?
            try
            {
                var parse = Int32.Parse(goldValue.Replace("k", "").Replace(".", "")) * 100;

                //Currently OCR is a bit moody and will often spit out straight nonsense. Try to avoid actually showing this by making sure that the numbers are somewhat logical
                //This breaks the ability to scroll through replays. Once OCR is a bit more stable, remove this!
                if (parse < backup)
                    return backup;
                return parse;
            }
            catch (Exception)
            {
                return backup;
            }
        }
    }
}
