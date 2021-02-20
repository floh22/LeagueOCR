using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class AOIList
    {
        public AreaOfInterest Red_Gold;
        public AreaOfInterest Blue_Gold;

        public List<AreaOfInterest> OtherAreas;

        public AOIList()
        {
            OtherAreas = new List<AreaOfInterest>();
        }

        public void DisposeAll()
        {
            GetAllAreaOfInterests().ForEach((aoi) => aoi.Dispose());
        }

        public List<AreaOfInterest> GetAllAreaOfInterests()
        {
            var tempList = OtherAreas.Concat(new List<AreaOfInterest>()
            {
                Red_Gold,
                Blue_Gold
            }).ToList();

            tempList.RemoveAll((item) => item == null);
            return tempList;
        }

        public void Clear()
        {
            DisposeAll();

            Red_Gold = null;
            Blue_Gold = null;
            OtherAreas.Clear();
        }
    }
}
