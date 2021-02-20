using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class Team
    {
        public int Id;
        public string TeamName;
        public int Gold;

        public Team(int Id, string TeamName, int Gold)
        {
            this.Id = Id;
            this.TeamName = TeamName;
            this.Gold = Gold;
        }

        public Team() : this(0, "", 0) { }
    }
}
