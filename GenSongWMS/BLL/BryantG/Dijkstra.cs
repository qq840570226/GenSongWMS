using System.Collections.Generic;
using System.Collections;

namespace BryantG
{
    public static class Dijkstra
    {
        static public Dictionary<ODPair, PathList> FindPath(Map map, int pathNum)
        {
            Dictionary<ODPair, PathList> allPath = new Dictionary<ODPair, PathList>();
            Dictionary<uint, PathList> pathList;
            ODPair od;
            Point p1;
            Point p2;
            foreach (KeyValuePair<uint, Point> item1 in map.points)
            {
                p1 = item1.Value;
                pathList = FindPath(item1.Value, map, pathNum);
                foreach (KeyValuePair<uint, PathList> item2 in pathList)
                {
                    p2 = map.points[item2.Key];
                    od = new ODPair(p1, p2);
                    allPath.Add(od, item2.Value);
                }
            }
            return allPath;
        }

        static public Dictionary<uint, PathList> FindPath(Point startPoint, Map map, int pathNum)
        {
            Dictionary<uint, PathList> allPathList = new Dictionary<uint, PathList>();
            Point[] endPoints = map.pointSet;
            int pointNum = endPoints.Length;
            // 初始化
            Point endPoint;
            for (int i = 0; i < pointNum; i++)
            {
                endPoint = endPoints[i];
                allPathList.Add(endPoint.ID, new PathList());
            }

            double min_dist = -1;   //当前最小距离
            int min_idx = -1;
            Dis[] dis = new Dis[pointNum];
            //初始化
            for (int i = 0; i < pointNum; i++)
            {
                dis[i] = new Dis();
                if (startPoint.Equals(endPoints[i]))
                {
                    dis[i].value = 0;
                    dis[i].visited = true;
                }
                else if (((IList)startPoint.Neighbours).Contains(endPoints[i]))
                {
                    dis[i].value = Tools.Distance(startPoint, endPoints[i]);
                    dis[i].path = new Point[] { startPoint, endPoints[i] };
                    if (min_dist == -1 || min_dist > dis[i].value)
                    {
                        min_dist = dis[i].value;
                        min_idx = i;
                    }
                }
                else
                {
                    dis[i].value = -1;
                }
            }
            int foundNum = 0;   //找到最短路径的顶点数
            if (min_idx == -1)
            {
                return allPathList;
            }
            dis[min_idx].visited = true;
            Point[] path = new Point[2] { startPoint, endPoints[min_idx] };
            PathList pathList = new PathList(new Path(path, map), pathNum);
            allPathList[endPoints[min_idx].ID] = pathList;
            foundNum++;
            while (min_idx != -1 && foundNum < pointNum)
            {
                for (int i = 0; i < pointNum; i++)
                {
                    if (!dis[i].visited && ((IList)endPoints[min_idx].Neighbours).Contains(endPoints[i]))
                    {
                        if (Tools.Distance(endPoints[min_idx], endPoints[i]) + dis[min_idx].value < dis[i].value || dis[i].value == -1)
                        {
                            dis[i].value = Tools.Distance(endPoints[min_idx], endPoints[i]) + dis[min_idx].value;
                            //Console.WriteLine("min_idx: "+min_idx);

                            int temp = dis[min_idx].path.Length + 1;
                            dis[i].path = new Point[temp];
                            for (int j = 0; j < dis[min_idx].path.Length; j++)
                            {
                                path = dis[min_idx].path;
                                dis[i].path[j] = path[j];
                            }
                            dis[i].path[dis[min_idx].path.Length] = endPoints[i];
                            //Console.WriteLine("i: " + i);
                            Point[] t = dis[i].path;
                            string str = "";
                            foreach (Point p in t)
                            {
                                str = str + "->" + p.ID.ToString();
                            }
                            //Console.WriteLine(str);
                        }
                    }
                }
                min_idx = -1;
                min_dist = -1;
                for (int i = 0; i < pointNum; i++)
                {
                    if (!dis[i].visited && ((min_dist == -1 && dis[i].value != -1) || min_dist > dis[i].value && dis[i].value != -1))
                    {
                        min_dist = dis[i].value;
                        min_idx = i;
                    }
                }
                if (min_idx != -1)
                {
                    dis[min_idx].visited = true;
                    pathList = new PathList(new Path(dis[min_idx].path, map), pathNum);
                    allPathList[endPoints[min_idx].ID] = pathList;
                }
                foundNum++;
            }
            //Console.WriteLine("start: " + startPoint.ID.ToString());
            //for (int i = 0; i < pointNum; i++)
            //{
            //    Console.WriteLine("dis[i]: " + dis[i].value.ToString());
            //}
            //Console.Read();
            //if (foundNum < pointNum)
            //{
            //    throw new Exception("地图非联通！" +" foundNum: "+ foundNum + "; pointNum: " + pointNum );
            //}
            return allPathList;
        }
    }

    /// <summary>
    /// 记录某个点到起点的最短路径信息
    /// </summary>
    public class Dis
    {
        public double value;
        public bool visited;
        public Point[] path;
        public Dis()
        {
            visited = false;
            value = -1;
            path = null;
        }
    }
}
