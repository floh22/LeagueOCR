using Common;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Server.Controllers
{
    public class ObjectivesController : ApiController
    {

        public IEnumerable<Objective> GetAllObjectives()
        {
            return new List<Objective>()
            {
                HttpServer.dragon,
                HttpServer.baron
            };
        }

        public IHttpActionResult GetObjective(string Name)
        {
            if (Name.Equals("Dragon", StringComparison.CurrentCultureIgnoreCase))
                return Ok(HttpServer.dragon);
            if (Name.Equals("Baron", StringComparison.CurrentCultureIgnoreCase))
                return Ok(HttpServer.baron);
            return NotFound();
        }

        public static void UpdateObjectives()
        {
            HttpServer.dragon.Type = AOIList.Dragon_Type.CurrentContent;
            var dragonRespawnTimer = HttpServer.dragon.Type == "Elder" ? 360 : 300;
            if (TextToTime(AOIList.Dragon_Timer.CurrentContent, HttpServer.dragon.Cooldown, dragonRespawnTimer, 2, out int dragonCd))
            {
                HttpServer.dragon.Cooldown = dragonCd;

                //Update alive state
                if (dragonCd == 0)
                    HttpServer.dragon.IsAlive = true;
                else if (HttpServer.dragon.IsAlive == true)
                    HttpServer.dragon.IsAlive = false;
            }
            if (TextToTime(AOIList.Baron_Timer.CurrentContent, HttpServer.baron.Cooldown, HttpServer.baron.TimesTakenInMatch == 0 ? 1800 : 360, 3, out int baronCd))
            {
                HttpServer.baron.Cooldown = baronCd;

                //Update alive state
                if (baronCd == 0)
                    HttpServer.baron.IsAlive = true;
                else if (HttpServer.baron.IsAlive == true)
                    HttpServer.baron.IsAlive = false;
            }
        }

        private static bool TextToTime(string inputText, int oldTime, int maxTime, int listPos, out int newTime)
        {
            //Try to determine and clean up what OCR creates. It's better to not update a timer for a second than to have incorrect values

            newTime = 0;
            try
            {
                //Object timers show nothing when they are off cooldown, so we have to somehow use the absence as a result
                //This is insanely inaccurate... I just dont know how to do this better atm
                if ((inputText.Length == 0 || inputText == "0" ) && oldTime != int.MaxValue)
                {

                    //If we only accept linear progression in time, then if recently the timer was about to go to 0, absence of results are expected and set the timer to 0
                    if (HttpServer.OnlyIncreaseGold && oldTime < 5)
                    {
                        newTime = 0;
                        return true;
                    }
                    else
                    {
                        var oldList = HttpServer.oldValues.ElementAt(listPos);
                        if (oldList.Count > 3)
                            oldList.RemoveAt(0);
                        if (oldList.Count != 0)
                        {
                            var avg = oldList.Sum() / oldList.Count;
                            if (Math.Abs(avg) > 10)
                            {
                                oldList.Add(0);
                                return false;
                            }
                        }
                        oldList.Add(0);

                        return true;
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

                    //Text should be somewhat formatted now

                    string[] parts = inputText.Split(':');
                    var seconds = int.Parse(parts[1]);

                    //Throw out incorrect seconds values
                    if (seconds > 60)
                        return false;

                    var timeInSeconds = int.Parse(parts[0]) * 60 + seconds;

                    if (timeInSeconds > maxTime)
                        return false;

                    if (HttpServer.OnlyIncreaseGold)
                    {
                        //Detect Timer reset
                        if ((timeInSeconds > oldTime && Math.Abs(timeInSeconds - maxTime) < 10) || timeInSeconds < oldTime)
                        {
                            newTime = timeInSeconds;
                            return true;
                        }

                        return false;
                    }
                    //Use old times to estimate if the current values makes sense
                    else
                    {
                        var oldList = HttpServer.oldValues.ElementAt(listPos);
                        if (oldList.Count > 3)
                            oldList.RemoveAt(0);
                        if (oldList.Count != 0)
                        {
                            var avg = oldList.Sum() / oldList.Count;
                            //Assume that we check once per second with a bit of room for missed OCR passes
                            if (Math.Abs((avg - 3) - timeInSeconds) > 10)
                            {
                                oldList.Add(timeInSeconds);
                                return false;
                            }
                        }
                        oldList.Add(timeInSeconds);
                    }

                    //New time most likely correct
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
