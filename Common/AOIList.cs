using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class AOIList
    {
        public static AreaOfInterest Red_Gold;
        public static AreaOfInterest Blue_Gold;
        public static AreaOfInterest Dragon_Timer;
        public static AreaOfInterest Dragon_Type;
        public static AreaOfInterest Baron_Timer;


        public static void DisposeAll()
        {
            GetOCRAreaOfInterests().ForEach((aoi) => aoi.Dispose());
        }

        public static List<AreaOfInterest> GetOCRAreaOfInterests()
        {
            var tempList = new List<AreaOfInterest>()
            {
                Red_Gold,
                Blue_Gold,
                Dragon_Timer,
                Baron_Timer
            };

            tempList.RemoveAll((item) => item == null);
            return tempList;
        }

        public static List<AreaOfInterest> GetIMAreaOfInterests()
        {
            var tempList = new List<AreaOfInterest>()
            {
                Dragon_Type
            };

            tempList.RemoveAll((item) => item == null);
            return tempList;
        }

        public static void Clear()
        {
            DisposeAll();

            Red_Gold = null;
            Blue_Gold = null;
            Dragon_Timer = null;
            Dragon_Type = null;
            Baron_Timer = null;
        }
    }
}
