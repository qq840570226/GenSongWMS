using System.Collections.Generic;

namespace BryantG
{
    public static class Dispatch
    {

        static public int temp()
        {
            return 1;
        }

        static public MissionAssignment[] Brain( Queue<Mission> missionQueue, List<AGVWPF> agvList, Dictionary<ODPair, PathList> allPaths )
        {
            MissionAssignment[] ma = new MissionAssignment[missionQueue.Count];
            Point missionStartPoint;
            Point missionEndPoint;
            Point agvPoint;
            
            int idx = 0;
            foreach ( Mission m in missionQueue )
            {
                ma[idx] = new MissionAssignment(m,null,null);
                idx++;
            }
            double min_length_1 = -1;
            double min_length_2 = -1;
            Path bestPath_1 = null;     // agv->mission起始点一段路径
            Path bestPath_2 = null;     // mission起始点->mission终点一段路径
            PathList pathList_1;
            PathList pathList_2;
            AGVWPF bestAGV = null;
            if (agvList.Count == 1) // 只有一辆小车
            {
                idx = 0;
                foreach (Mission m in missionQueue)
                {
                    foreach (AGVWPF agv in agvList )
                    {
                        bestAGV = agv;
                        agvPoint = agv.PrePoint;
                        missionStartPoint = m.mssionStartPoint;
                        pathList_1 = allPaths[new ODPair(agvPoint, missionStartPoint)];
                        missionEndPoint = m.mssionEndPoint;
                        ODPair od = new ODPair(missionStartPoint, missionEndPoint);
                        pathList_2 = allPaths[od];
                        //Console.WriteLine(allPaths.ContainsKey(od).ToString());
                       // Console.Read();

                        min_length_1 = -1;
                        if (pathList_1.paths != null )
                        {
                            foreach (Path path in pathList_1.paths)
                            {
                                if (min_length_1 == -1 || min_length_1 > path.length)
                                {
                                    min_length_1 = path.length;
                                    bestPath_1 = path;
                                }
                            }
                        }
                        
                        min_length_2 = -1;
                        if ( pathList_2.paths != null )
                        {
                            foreach (Path path in pathList_2.paths)
                            {
                                if (min_length_2 == -1 || min_length_2 > path.length)
                                {
                                    min_length_2 = path.length;
                                    bestPath_2 = path;
                                }
                            }
                        }
                        
                        if ( min_length_1 != -1 && min_length_2 != -1 )
                        {
                            break;
                        }


                    }
                    if (min_length_1 != -1 && min_length_2 != -1)
                    {
                        ma[idx].agv = bestAGV;
                        ma[idx].path = bestPath_1.combinePath(bestPath_2);
                        break;
                    }
                    idx++;
                    
                }
                
            }

            return ma;
        }

    }

    public struct MissionAssignment
    {
        public Mission mission;
        public AGVWPF agv;
        public Path path;

        public MissionAssignment(Mission mission, AGVWPF agv, Path path)
        {
            this.mission = mission;
            this.agv = agv;
            this.path = path;
        }

    }
}
