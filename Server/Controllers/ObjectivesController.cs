using Common;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Server.Controllers
{
    public class ObjectivesController : ApiController
    {

        public HttpResponseMessage GetAllObjectives()
        {
            return Request.CreateResponse(HttpStatusCode.OK, new List<Objective>()
            {
                HttpServer.dragon,
                HttpServer.baron
            }, Configuration.Formatters.JsonFormatter);
        }

        public HttpResponseMessage GetObjective(string Name)
        {
            if (Name.Equals("Dragon", StringComparison.OrdinalIgnoreCase))
                return Request.CreateResponse(HttpStatusCode.OK, HttpServer.dragon, Configuration.Formatters.JsonFormatter);
            if (Name.Equals("Baron", StringComparison.OrdinalIgnoreCase))
                return Request.CreateResponse(HttpStatusCode.OK, HttpServer.baron, Configuration.Formatters.JsonFormatter);
            return Request.CreateResponse(HttpStatusCode.BadRequest);
        }

        public static void UpdateObjectives()
        {
            HttpServer.dragon.Type = AOIList.Dragon_Type.CurrentContent;
            var dragonRespawnTimer = HttpServer.dragon.Type == "elder" ? 360 : 300;
            if (TextToTime(AOIList.Dragon_Timer.CurrentContent, HttpServer.dragon.Cooldown, ref HttpServer.previousDragon, dragonRespawnTimer, 2, out int dragonCd))
            {
                HttpServer.dragon.Cooldown = dragonCd;
                
                //Update alive state
                if (dragonCd == 0)
                    HttpServer.dragon.IsAlive = true;
                else if (HttpServer.dragon.IsAlive == true)
                    HttpServer.dragon.IsAlive = false;
            }
            if (TextToTime(AOIList.Baron_Timer.CurrentContent, HttpServer.baron.Cooldown, ref HttpServer.previousBaron,360, 3, out int baronCd))
            {
                HttpServer.baron.Cooldown = baronCd;

                //Update alive state
                if (baronCd == 0)
                    HttpServer.baron.IsAlive = true;
                else if (HttpServer.baron.IsAlive == true)
                    HttpServer.baron.IsAlive = false;
            }

            Logging.Verbose($"Dragon In: {AOIList.Dragon_Timer.CurrentContent}, Value: {HttpServer.dragon.Cooldown} | Baron In: {AOIList.Baron_Timer.CurrentContent}, Value: {HttpServer.baron.Cooldown}");
        }

        private static bool TextToTime(string inputText, int oldTime, ref string oldText, int maxTime, int listPos, out int newTime)
        {
            //Try to determine and clean up what OCR creates. It's better to not update a timer for a second than to have incorrect values

            newTime = 0;
            try
            {
                //Object timers show nothing when they are off cooldown, so we have to somehow use the absence as a result
                //I thought this would be horrible...
                //sometimes the fire drake icon is interpreted as a number when using the non esports timers
                //Elder drake is interpreted as 05 when using non esports timers
                //Since OCR is very stable for this region however, treat it the same as no input at all
                
                if ((inputText.Length <= 1 || inputText == "0" || inputText == "05" ) && oldTime != int.MaxValue)
                {

                    //If we only accept linear progression in time, then if recently the timer was about to go to 0, absence of results are expected and set the timer to 0
                    if (HttpServer.OnlyIncreaseGold && oldTime < 5)
                    {
                        Logging.Info("Detected Objective Respawn");
                        return true;
                    }
                    else
                    {
                        var oldList = HttpServer.oldValues.ElementAt(listPos);
                        if (oldList.Count > 2)
                            oldList.RemoveAt(0);
                        if (oldList.Count != 0)
                        {
                            var avg = oldList.Sum() / oldList.Count;
                            if (Math.Abs(avg) > 5)
                            {
                                oldList.Add(0);
                                return false;
                            }
                        }
                        if(oldTime != 0)
                        {
                            Logging.Info("Detected Objective Respawn");
                            oldList.Add(0);

                            return true;
                        }
                        return false;
                    }
                }
                else if (inputText.Length >= 3 && inputText.Length < 6)
                {

                    //Received a somewhat correct format string
                    //Check if one of the first digits coundln't be read, use the old time to try and fix the time
                    if (inputText[2] != ':')
                    {
                        if(inputText[0] == ':') {
                            //This assumes that this is called once per second. Even if this weren't the case, the timer would be wrong only until the next update
                            int minutes = (int)Math.Floor((decimal)(oldTime - 1) / 60);
                            int newSeconds = Int32.Parse("" + inputText[2] + inputText[3]);

                            //Try to avoid edge case of going from something like xx:01 to xx:59 and adding a minute
                            if (minutes * 60 + newSeconds > oldTime)
                                minutes--;

                            //Create new Time text
                            inputText = '0' + minutes.ToString() + ':' + inputText[inputText.Length - 2] + inputText[inputText.Length - 1];
                        } else
                        if(inputText[1] == ':')
                        {
                            inputText = '0' + inputText;
                        } else
                        {
                            return false;
                        }
                    }
                    else if (inputText.EndsWith(":"))
                    {
                        //Can't really fix a string that doesn't have a second time
                        return false;
                    }

                    //Fix up the number obtained a bit in a couple common cases of OCR confusion
                    //Assumes 1 tick/sec

                    //OCR likes to confuse 2 with 9, so try to help out here.
                    if(oldText.ElementAt(oldText.Length - 1) == '3' && inputText.ElementAt(inputText.Length - 1) == '9')
                    {
                        Logging.Verbose("Fixing OCR input string: 9 -> 2");
                        inputText = inputText.Remove(inputText.Length - 1, 1) + "2";
                    }

                    //OCR also rarely confuses 9 with 0, so to avoid this try to catch these cases.
                    //Caution here though since if the game were paused at a multiple of 10, this should not be replaced
                    if(oldText.ElementAt(oldText.Length - 1) == '0' && inputText.ElementAt(inputText.Length - 1) == '0' && (oldText.ElementAt(oldText.Length - 2) != inputText.ElementAt(inputText.Length - 2)))
                    {
                        Logging.Verbose("Fixing OCR input string: 0 -> 9");
                        inputText = inputText.Remove(inputText.Length - 1, 1) + "9";
                    }

                    //90 seconds not possible. Since this most likely a simple OCR confusion between 9 and 2, replace it
                    if(inputText.ElementAt(inputText.Length - 2) == '9')
                    {
                        Logging.Verbose("Fixing OCR input string: 9 -> 2");
                        inputText = inputText.Remove(3, 1).Insert(3, "2");
                    }

                    //Text should be somewhat formatted now

                    string[] parts = inputText.Split(':');
                    var seconds = int.Parse(parts[1]);

                    //Throw out incorrect seconds values
                    if (seconds > 60)
                        return false;

                    var timeInSeconds = int.Parse(parts[0]) * 60 + seconds;

                    /*
                     * Unneeded check since timer is relatively accurate and causes more issues than anything else
                    if (timeInSeconds > maxTime)
                        return false;
                    */
                    if (HttpServer.OnlyIncreaseGold)
                    {
                        //Detect Timer reset
                        if ((timeInSeconds > oldTime && Math.Abs(timeInSeconds - maxTime) < 10) || timeInSeconds < oldTime)
                        {
                            newTime = timeInSeconds;
                            oldText = inputText;
                            return true;
                        }

                        return false;
                    }
                    //Use old times to estimate if the current values makes sense
                    else
                    {
                        var oldList = HttpServer.oldValues.ElementAt(listPos);
                        if (oldList.Count > 2)
                            oldList.RemoveAt(0);
                        if (oldList.Count != 0)
                        {
                            var avg = oldList.Sum() / oldList.Count;

                            //React quickly incase the objective just respawned so we can catch the message
                            //Assume that we check once per second with a bit of room for missed OCR passes
                            if(avg < 5 && timeInSeconds < maxTime + 10 && timeInSeconds > maxTime - 10) {
                                Logging.Info("Detected Objective Killed");
                            } else if (Math.Abs((avg - 3) - timeInSeconds) > 5 )
                            {
                                oldList.Add(timeInSeconds);
                                return false;
                            }
                        }
                        oldList.Add(timeInSeconds);
                    }
                    //New time most likely correct
                    oldText = inputText;
                    newTime = timeInSeconds;

                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                newTime = 0;
                return false;
            }
        }
    }
}
