using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Xml.Linq;
using BryantG;

namespace GenSongWMS.BLL
{
    /// <summary>
    /// 数据缓存区
    /// </summary>
    public static class DataCache
    {
        /// <summary>
        /// 叉车状态集合
        /// </summary>
        public static ConcurrentDictionary<string, ForkLiftStatus> DictionaryForkLiftStatus { get; set; } = new ConcurrentDictionary<string, ForkLiftStatus>();

        /// <summary>
        /// 所有路径列表
        /// </summary>
        public static Dictionary<ODPair, PathList> AllPaths { get; set; }

        /// <summary>
        /// 所有边路径
        /// </summary>
        public static ConcurrentDictionary<PointToPoint, List<uint>> AllArcPaths { get; set; }

        /// <summary>
        /// 所有点路径
        /// </summary>
        public static ConcurrentDictionary<PointToPoint, List<uint>> AllPointPaths { get; set; }

        /// <summary>
        /// 冲突节点
        /// </summary>
        public static ConcurrentDictionary<uint, bool> DictionaryConfictPoint { get; set; } = new ConcurrentDictionary<uint, bool>();

        /// <summary>
        /// 冲突边
        /// </summary>
        public static ConcurrentDictionary<uint, bool> DictionaryConfictPath { get; set; } = new ConcurrentDictionary<uint, bool>();

        /// <summary>
        /// 交通堵塞情况
        /// </summary>
        public static ConcurrentDictionary<string, bool> DictionaryTrafficJam { get; set; } = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// 任务id
        /// </summary>
        public static uint TaskID { get; set; } = 0;

        /// <summary>
        /// 锁
        /// </summary>
        private static readonly object taskIdLock = new object();

        /// <summary>
        /// 选取最短路径
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool GetPath(string startPoint, string endPoint, out List<uint> result)
        {
            return AllArcPaths.TryGetValue(new PointToPoint(startPoint,endPoint), out result);
        }

        /// <summary>
        /// 选取最短路径-点集合
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool GetPoints(string startPoint, string endPoint, out List<uint> result)
        {
            return AllPointPaths.TryGetValue(new PointToPoint(startPoint,endPoint), out result);
        }

        /// <summary>
        /// 地图初始化
        /// </summary>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        public static XDocument MapInit(XDocument xdoc)
        {
            Map map = new Map(xdoc, 900, 550);
            AllPaths = new Dictionary<ODPair, PathList>(AllPath.FindAllPath(map, 1));
            string strPointName;
            string strArcsName;
            List<uint> arcList;
            List<uint> pointList;
            AllArcPaths = new ConcurrentDictionary<PointToPoint, List<uint>>();
            AllPointPaths = new ConcurrentDictionary<PointToPoint, List<uint>>();

            Point point, startPoint, endPoint;
            XDocument myXDoc = new XDocument(new XElement("allPaths"));
            XElement rootNode = myXDoc.Root;
            foreach (var item in AllPaths)
            {
                arcList = new List<uint>();
                pointList = new List<uint>();
                startPoint = item.Key.StartPoint;
                endPoint = item.Key.EndPoint;
                if (!startPoint.Equals(endPoint) && item.Value.paths != null)
                {
                    //定义一个XElement结构
                    XElement odNode = new XElement("OD", new XAttribute("start", startPoint.ID.ToString()), new XAttribute("end", endPoint.ID.ToString()));

                    strArcsName = "";
                    arcList.Clear();
                    pointList.Clear();


                    foreach (Path path in item.Value.paths)
                    {
                        strPointName = path.path[0].ID.ToString();
                        pointList.Add(path.path[0].ID);
                        for (int i = 1; i < path.path.Length; i++)
                        {
                            point = path.path[i];
                            strPointName = $"{strPointName}-{point.ID}";
                            map.arcsID.TryGetValue($"{path.path[i - 1].ID},{path.path[i].ID}", out uint tmpUint);
                            map.arcs.TryGetValue(tmpUint, out Vector tmpVector);
                            strArcsName = $"{strArcsName}-{tmpVector.ArcCode}";
                            pointList.Add(path.path[i].ID);
                            arcList.Add(tmpVector.ArcCode);
                        }
                        
                        AllArcPaths.TryAdd(new PointToPoint(startPoint.ID, endPoint.ID), arcList);
                        AllPointPaths.TryAdd(new PointToPoint(startPoint.ID, endPoint.ID), pointList);

                        strArcsName = strArcsName.Substring(1, strArcsName.Length - 1);

                        XElement newNode = new XElement("path", new XElement("points", strPointName), new XElement("length", path.length.ToString()));
                        odNode.Add(newNode);

                        XElement newNodeArcs = new XElement("pathArcs", new XElement("Arc", strArcsName), new XElement("length", path.arcs.Length.ToString()));
                        odNode.Add(newNodeArcs);
                    }
                    rootNode.Add(odNode);
                }
                else if (!startPoint.Equals(endPoint) && item.Value.paths == null)
                {
                    XElement odNode = new XElement("OD", new XAttribute("start", startPoint.ID.ToString()), new XAttribute("end", endPoint.ID.ToString()));
                    XElement newNode = new XElement("path", "");
                    odNode.Add(newNode);
                    rootNode.Add(odNode);
                }
            }
            return myXDoc;
        }

        /// <summary>
        /// 生成新的任务id
        /// </summary>
        /// <returns></returns>
        public static uint NewTaskID()
        {
            lock (taskIdLock)
            {
                if (TaskID > 0xffff)
                    TaskID = 0;
                return TaskID++;
            }
        }
    }

    public struct PointToPoint
    {
        public readonly uint Start;
        public readonly uint End;

        public PointToPoint(uint start, uint end)
        {
            Start = start;
            End = end;
        }

        public PointToPoint(string start, string end)
        {
            Start = Convert.ToUInt32(start);
            End = Convert.ToUInt32(end);
        }
    }
}
