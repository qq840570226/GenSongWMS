using System.Collections.Generic;

namespace BryantG
{
    public static class AllPath
    {
        static public Dictionary<ODPair, PathList> FindAllPath(Map map,int pathNum)
        {
            return Dijkstra.FindPath(map,pathNum);
        }

    }
}
