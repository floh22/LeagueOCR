﻿using Common;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Server.Controllers
{
    public class TeamsController : ApiController
    {

        public IEnumerable<Team> GetAllTeams()
        {
            return new Team[] { HttpServer.blueTeam, HttpServer.redTeam };
        }

        public IHttpActionResult GetTeam(int id)
        {
            if (id < 0 || id > 1)
                return NotFound();

            return (id == 0) ? Ok(HttpServer.blueTeam) : Ok(HttpServer.redTeam);
        }

        public IHttpActionResult GetTeam(string name)
        {
            if (name.Equals("ORDER", StringComparison.OrdinalIgnoreCase))
                return Ok(HttpServer.blueTeam);
            else if (name.Equals("CHAOS", StringComparison.OrdinalIgnoreCase))
                return Ok(HttpServer.redTeam);
            return NotFound();
        }

        public static void UpdateTeams()
        {
            string blueGoldText = AOIList.Blue_Gold.CurrentContent;
            string redGoldText = AOIList.Red_Gold.CurrentContent;
            var blueTeam = HttpServer.blueTeam;
            var redTeam = HttpServer.redTeam;
            //Console.WriteLine(blueGoldText + ", " + redGoldText);
            if (GoldToInt(blueGoldText, blueTeam.Gold, 0, out int blueGold) && GoldToInt(redGoldText, redTeam.Gold, 1, out int redGold))
            {
                blueTeam.Gold = blueGold;
                redTeam.Gold = redGold;
            } else
            {
                Console.WriteLine("Couldn't determine both Gold Values. Input Text: " + blueGoldText + ", " + redGoldText);
            }
        }

        private static bool GoldToInt(string goldValue, int backup, int listPos, out int newValue)
        {
            //Have backup value incase OCR goes wrong somehow and the int cant be parsed. In that case fall back to the last value
            //This should keep the gold value from fluctuating or displaying something completely incorrect

            try
            {
                //Currently OCR is a bit moody and will often spit out straight nonsense. Try to avoid actually showing this by making sure that the numbers are somewhat logical
                //This breaks the ability to scroll through replays. Once OCR is a bit more stable, remove this!
                newValue = 0;

                //Any value shorter than 2 is missing the first digit or the first decimal, so ignore it
                //In testing it turned out that if the first character is not a number, then usually the number is completely wrong instead of just being in the wrong format, so discard these results
                if (goldValue.Length < 2 || !Char.IsDigit(goldValue[0]))
                    return false;

                //Input text contains a k but it isnt the last value. Since no letter gets confused for k, the k contained is almost surely the last value we should look at, trim anything afterwards that may have gotten picked up
                if (goldValue[goldValue.Length - 1] != 'k' && goldValue.Contains("k"))
                {
                    string newGold = "";
                    goldValue.ToList().ForEach((c) =>
                    {
                        if (c != 'k')
                            newGold += c;
                        else
                            return;
                    });
                    goldValue = newGold;
                }

                goldValue = goldValue.Replace("k", "");
                goldValue = goldValue.Replace("/", "");

                //Sometimes the '.' isnt picked up but the numbers are all correct. In this case compare the number just read to the backup and see if is in the same range. If it is, lets use that updated number since its probably correct
                //Be a bit more strict here than below since if it is falsely rejected by something like a large teamfight or shutdown, the value will get updated next call instead of permanently ruining the results
                if (!goldValue.Contains('.'))
                {
                    var tempValue = Int32.Parse(goldValue) * 100;
                    if (Math.Abs(tempValue - backup) > 1000)
                        return false;
                }
                else
                {
                    goldValue = goldValue.Replace(".", "");
                }

                if (goldValue.Length == 0)
                    return false;
                var parse = Int32.Parse(goldValue) * 100;

                //This should make it so that gold values follow a continuous trajectory and allow for switching on the timeline
                //Use old Values to determine if the current values make any sense or not
                if (HttpServer.OnlyIncreaseGold)
                {
                    if (Math.Abs(parse - backup) > 3000)
                    {
                        return false;
                    }
                }
                else
                {
                    var oldList = HttpServer.oldValues.ElementAt(listPos);
                    if (oldList.Count > 3)
                        oldList.RemoveAt(0);
                    if (oldList.Count != 0)
                    {
                        var avg = oldList.Sum() / oldList.Count;
                        if (Math.Abs(parse - avg) > 3000)
                        {
                            oldList.Add(parse);
                            return false;
                        }
                    }
                    oldList.Add(parse);
                }

                newValue = parse;
                return true;
            }
            catch (Exception)
            {
                newValue = 0;
                return false;
            }
        }
    }
}
