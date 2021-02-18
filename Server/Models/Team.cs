using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string TeamName { get; set; }
        public int Gold{ get; set; }

        public Team(int Id, string TeamName, int Gold)
        {
            this.Id = Id;
            this.TeamName = TeamName;
            this.Gold = Gold;
        }

        public Team() : this(0, "", 0) { }
    }
}
