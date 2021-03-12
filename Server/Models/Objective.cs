using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class Objective
    {
        public string Type;
        public int Cooldown;
        public bool IsAlive;
        public int TimesTakenInMatch;
        public int LastTakenBy;

        public Objective(string Type, int Cooldown, bool IsAlive)
        {
            this.Type = Type;
            this.Cooldown = Cooldown;
            this.IsAlive = IsAlive;
            TimesTakenInMatch = 0;
            LastTakenBy = -1;
        }

        public Objective() : this("Debug", 0, false) { }
    }
}
