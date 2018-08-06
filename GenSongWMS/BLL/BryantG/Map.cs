using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BryantG
{
    /// <summary>
    /// 关于地图上点的定义
    /// </summary>
    public class Point : IEquatable<Point>
    {
        public double X;     // 坐标x
        public double Y;     // 坐标y
        public uint ID;        // id
        public bool IsLock;    // 是否被占用
        public PointType PointType;    // 节点类型
        public List<Point> Neighbours { get; set; } = new List<Point>();  //  相邻节点
        public Address Addr { get; set; }    // 节点对应地址

        /// <summary>
        /// 默认构造函数,缺省用
        /// </summary>
        public Point() { }

        /// <summary>
        /// 构造函数,
        /// </summary>
        /// <param name="x">该点坐标x</param>
        /// <param name="y">该点坐标y</param>
        /// <param name="occupyed">是否已被占用</param>
        public Point(double x, double y, uint id, bool occupyed, PointType pointType = PointType.过道)
        {
            // 初始化一堆参数
            // 坐标
            X = x;
            Y = y;
            ID = id;
            // 点被占用情况
            IsLock = occupyed;
            Addr = new Address(x, y, id);
        }

        public void AddNeighbours(Point p)
        {
            Neighbours.Add(p);
        }

        #region Equality
        public bool Equals(Point other)
        {
            return (X == other.X && Y == other.Y) || ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if (obj is Point)
            {
                return Equals((Point)obj);
            }
            return false;
        }

        public override int GetHashCode() { return (int)X ^ (int)Y; }
        #endregion

        /// <summary>
        /// 字符串化point
        /// </summary>
        /// <returns>字符串</returns>
        public override string ToString()
        {
            // 返回节点坐标
            return $"({X}, {Y})";
        }
    }

    /// <summary>
    /// 地图上agv小车的定义
    /// </summary>
    public class AGV
    {
        protected double x;     // AGV 坐标x
        protected double y;     // AGV 坐标y
        protected Vector direction;     // 车头方向
        protected Point initialPoint;   // 起始点
        protected Point prePoint;
        protected uint agvnumber;
        protected Random colorRmd = new Random();   // 颜色随机生成数
        protected bool busy = false;
        protected double speed;     // 车速

        /// <summary>
        /// agv小车当前所处的位置坐标x
        /// </summary>
        public double X { get => x; set => x = value; }
        /// <summary>
        /// agv小车当前所处的位置坐标y
        /// </summary>
        public double Y { get => y; set => y = value; }
        /// <summary>
        /// agv小车当前和x轴的角度
        /// </summary>
        public Vector Direction { get => direction; set => direction = value; }
        /// <summary>
        /// agv小车起始点
        /// </summary>
        public Point InitialPoint { get => initialPoint; set => initialPoint = value; }
        /// <summary>
        /// agv小车行驶中经历的上一个点,包括现在踩中的点
        /// </summary>
        public Point PrePoint { get => prePoint; set => prePoint = value; }
        public uint AGVNumber { get => agvnumber; set => agvnumber = value; }
        public double Speed { get => speed; set => speed = value; }
        /// <summary>
        /// 默认构造函数,缺省用
        /// </summary>
        public AGV() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="start">起始点</param>
        /// <param name="num">小车编号</param>
        public AGV(object initial, uint num, Vector direction)
        {
            // 初始化一堆参数
            // 起始点
            this.initialPoint = initial as Point;
            // 小车编号
            this.agvnumber = num;
            this.direction = direction;
            this.initialPoint.IsLock = true;
            this.prePoint = initialPoint;
            this.X = this.initialPoint.X;
            this.Y = this.initialPoint.Y;
        }

        protected virtual void AfterProcessing()
        { }

    }

    /// <summary>
    /// 地图类,用来初始化地图
    /// </summary>
    public class Map
    {

        /// <summary>
        /// 地图即为点的集合
        /// </summary>
        public Dictionary<uint, Point> points = new Dictionary<uint, Point>();
        public Dictionary<Address, uint> pointsID = new Dictionary<Address, uint>();

        public Point[] pointSet;
        public Dictionary<uint, Vector> arcs = new Dictionary<uint, Vector>(); //arcs包含的信息是节点和向量
        public Dictionary<string, uint> arcsID = new Dictionary<string, uint>(); //arcs包含的信息是节点和向量

        /// <summary>
        /// 由xml文件 建立地图
        /// </summary>
        public Map(System.Xml.Linq.XDocument xdoc, double maxX, double maxY)
        {
            XElement root = xdoc.Root;
            IEnumerable<XElement> enumerable = from el in root.Elements("ID") select el;  // 节点信息
            double min_x = 0;
            double min_y = 0;
            double max_x = 0;
            double max_y = 0;
            double x;
            double y;
            bool flag = true;
            foreach (XElement item in enumerable)
            {
                x = Convert.ToDouble(item.Element("X").Value);
                y = Convert.ToDouble(item.Element("Y").Value);
                if (flag == true)
                {
                    min_x = x;
                    min_y = y;
                    max_x = x;
                    max_y = y;
                    flag = false;
                }
                else
                {
                    if (x < min_x)
                    {
                        min_x = x;
                    }
                    if (y < min_y)
                    {
                        min_y = y;
                    }
                    if (x > max_x)
                    {
                        max_x = x;
                    }
                    if (y > max_y)
                    {
                        max_y = y;
                    }
                }
            }
            uint id;
            Address addr;
            foreach (XElement item in enumerable)
            {
                id = uint.Parse(item.Element("code").Value);
                if (points.ContainsKey(id))
                {
                    continue;
                }
                x = Convert.ToDouble(item.Element("X").Value);
                y = Convert.ToDouble(item.Element("Y").Value);
                if (max_x != min_x)
                {
                    x = (x - min_x) / (max_x - min_x) * maxX + 30;
                }
                else
                {
                    x = maxX / 2;
                }
                if (max_y != min_y)
                {
                    y = (y - min_y) / (max_y - min_y) * maxY + 30;
                }
                else
                {
                    y = maxY / 2;
                }
                addr = new Address(x, y, id);

                points.Add(id, new Point(x, y, id, false));
                pointsID.Add(addr, id);
            }
            pointSet = new Point[points.Keys.Count];
            int i = 0;
            foreach (KeyValuePair<uint, Point> item in points)
            {
                pointSet[i] = item.Value;
                i++;
            }
            Point point;
            Point point1;
            foreach (XElement item in enumerable)
            {
                id = uint.Parse(item.Element("code").Value);
                x = Convert.ToDouble(item.Element("X").Value);
                y = Convert.ToDouble(item.Element("Y").Value);
                if (max_x != min_x)
                {
                    x = (x - min_x) / (max_x - min_x) * maxX;
                }
                else
                {
                    x = maxX / 2;
                }
                if (max_y != min_y)
                {
                    y = (y - min_y) / (max_y - min_y) * maxY;
                }
                else
                {
                    y = maxY / 2;
                }
                addr = new Address(x, y, id);
                point = points[id];
                IEnumerable<XElement> neighbours = from el in item.Elements("relate") select el;  // 相邻节点
                foreach (XElement el in neighbours)
                {
                    id = uint.Parse(el.Value);
                    point1 = points[id];
                    if (!point.Neighbours.Contains(point1))
                    {
                        point.AddNeighbours(point1);
                    }
                }
            }

            enumerable = from el in root.Elements("SEG") select el;  // 边信息
            foreach (XElement item in enumerable)
            {
                string arcName = item.Element("name").Value;
                uint arcCode = uint.Parse(item.Element("code").Value);
                uint startPoint = (uint)Convert.ToDouble(item.Element("begin").Value);
                uint endPoint = (uint)Convert.ToDouble(item.Element("end").Value);
                Vector vector = new Vector(points[startPoint], points[endPoint], arcName, arcCode);
                arcs.Add(arcCode, vector);
                arcsID.Add($"{startPoint},{endPoint}", arcCode);
            }
        }

        /// <summary>
        /// 根据给定地址进行索引，返回地址对应的节点
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public Point this[Address a]
        {
            get
            {
                if (pointsID.ContainsKey(a))
                {
                    return points[pointsID[a]];
                }
                return null; // 不存在
            }
        }

        /// <summary>
        /// 释放节点
        /// </summary>
        /// <param name="pointID"></param>
        /// <returns></returns>
        public bool ReleaseNode(uint pointID)
        {
            if (points.TryGetValue(pointID, out Point nodeTmp) == true)
            {
                if (nodeTmp.IsLock == true)
                {
                    nodeTmp.IsLock = false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 申请节点
        /// </summary>
        /// <param name="PointID"></param>
        /// <returns></returns>
        public bool RequestNode(uint PointID)
        {
            if (points.TryGetValue(PointID, out Point nodeTmp) == true)
            {
                if (nodeTmp.IsLock == true)
                {
                    return false;
                }
                else
                {
                    lock (PointID.ToString())
                    {
                        nodeTmp.IsLock = true;
                        return true;
                    }
                }
            }
            return false;
        }
    }
    /// <summary>
    /// 路径即为节点列表
    /// </summary>
    public class Path
    {
        public Point[] path;
        public Vector[] arcs;

        public double length { get; private set; }

        public Path()
        {

        }

        public Path(Point[] path, Map map)
        {
            this.path = path;
            this.length = 0;
            if (path != null && path.Length > 1)
            {
                arcs = new Vector[path.Length - 1];
                for (int i = 1; i < path.Length - 1; i++)
                {
                    this.length += Tools.Distance(path[i - 1], path[i]);
                    uint[] arr = new uint[2];
                    arr[0] = path[i - 1].ID;
                    arr[1] = path[i].ID;
                    map.arcsID.TryGetValue($"{arr[0]},{arr[1]}", out uint tmp);

                    map.arcs.TryGetValue(tmp, out arcs[i - 1]);
                }
            }
        }

        public Point this[int i]
        {
            get
            {
                return path[i];
            }

        }

        public Path combinePath(Path path2)
        {
            Point[] path_new = new Point[this.path.Length + path2.path.Length - 1];
            this.path.CopyTo(path_new, 0);
            path2.path.CopyTo(path_new, this.path.Length - 1);
            this.path = path_new;
            this.length = this.path.Length + path2.path.Length;
            return this;
        }

        public override string ToString()
        {
            string str = "";
            if (path != null)
            {
                str = path[0].ID.ToString();
                for (int i = 1; i < path.Length; i++)
                {
                    str = str + "-" + path[i].ID.ToString();
                }
            }
            return str;
        }


    }
    /// <summary>
    /// 路径列表
    /// </summary>
    public class PathList
    {
        public Path[] paths = null;
        public int IDx { get; private set; }
        public int PathNum { get; private set; }

        public void AddPath(Path path)
        {

            paths[IDx] = new Path();
            paths[IDx] = path;
            IDx = IDx + 1;
        }

        public Point[] this[int idx] => paths[idx].path;

        public PathList(Path path, int pathNum)
        {
            PathNum = pathNum;
            IDx = 0;
            paths = new Path[pathNum];
            AddPath(path);
        }

        public PathList()
        {

        }

    }
    /// <summary>
    /// 起始点-终点对 
    /// </summary>
    public class ODPair : IEquatable<ODPair>
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        public ODPair(Point startPoint, Point endPoint)
        {
            this.StartPoint = startPoint;
            this.EndPoint = endPoint;
        }
        #region Equality

        public bool Equals(ODPair other)
        {
            return StartPoint.Equals(other.StartPoint) && EndPoint.Equals(other.EndPoint);
        }

        public override bool Equals(object other)
        {
            if (other is ODPair)
            {
                return Equals((ODPair)other);
            }
            return false;
        }
        public override int GetHashCode() { return (int)StartPoint.X ^ (int)EndPoint.X; }
        #endregion

    }

    /// <summary>
    /// 设置点的类型
    /// </summary>
    public enum PointType
    {
        货架,
        过道,
        充电,
        工位,
        不可通行
    };

    public struct Address : IEquatable<Address>
    {
        public readonly double X;
        public readonly double Y;
        public readonly uint ID;
        //public readonly uint ID;
        #region Equality
        public bool Equals(Address other)
        {
            return X == other.X && Y == other.Y && ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if (obj is Address)
            {
                return Equals((Address)obj);
            }
            return false;
        }

        public override int GetHashCode() { return (int)X ^ (int)Y; }
        #endregion
        public override string ToString() { return "[" + X + "," + Y + "," + ID + "]"; }

        public Address(double x, double y, uint id)
        {
            this.X = x;
            this.Y = y;
            this.ID = id;
        }
    }

    //有向边
    public class Edge
    {
        protected Address startAddr; // 起始点
        protected Address endAddr;     // 末端

        public Edge(Address startAddr, Address endAddr)
        {
            this.startAddr = startAddr;
            this.endAddr = endAddr;
        }

        public Edge()
        {

        }
    }

    public class Vector : Edge, IEquatable<Vector>
    {
        public double X;   // 向量(X,Y)
        public double Y;
        public string ArcName = null;
        public uint ArcCode;

        /// <summary>
        /// 生成起点到终点的向量
        /// </summary>
        /// <param name="startAddr">起点</param>
        /// <param name="endAddr">终点</param>
        public Vector(Address startAddr, Address endAddr) : base(startAddr, endAddr)
        {
            this.startAddr = startAddr;
            this.endAddr = endAddr;
            this.X = endAddr.X - startAddr.X;
            this.Y = endAddr.Y - startAddr.Y;
        }

        /// <summary>
        /// 由已有向量乘以长度倍数生成
        /// </summary>
        /// <param name="v">已有向量</param>
        /// <param name="length">长度倍数</param>
        public Vector(Vector v, int length) : base(v.startAddr, v.endAddr)
        {
            this.startAddr = v.startAddr;
            this.X = v.X * length;
            this.Y = v.Y * length;
            this.endAddr = new Address(this.X + this.startAddr.X, this.Y + this.startAddr.Y, 0);
        }

        public Vector(Point startPoint, Point endPoint, string ArcName, uint ArcCode) : base(startPoint.Addr, endPoint.Addr)
        {
            this.startAddr = startPoint.Addr;
            this.endAddr = endPoint.Addr;
            this.X = endPoint.Addr.X - startPoint.Addr.X;
            this.Y = endPoint.Addr.Y - startPoint.Addr.Y;
            this.ArcName = ArcName;
            this.ArcCode = ArcCode;
        }

        public Vector()
        {

        }

        #region Equality
        /// <summary>
        /// 重写等于函数
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Vector other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector)
            {
                return Equals((Vector)obj);
            }
            return false;
        }

        public override int GetHashCode() { return (int)X ^ (int)Y; }

        #endregion
        public bool IsSameDirection(Vector v)
        {
            Vector v1 = new Vector(this, 1);
            Vector v2 = new Vector(v, 1);
            v1.Normalize();
            v2.Normalize();
            return v1.Equals(v2);
        }

        /// <summary>
        /// 归一化函数
        /// </summary>
        public void Normalize()
        {
            try
            {
                double r = Math.Sqrt(Math.Pow(this.X, 2) + Math.Pow(this.Y, 2));
                this.X = this.X / r;
                this.Y = this.Y / r;
            }
            catch (Exception e)
            {
                throw new Exception("Error:" + e);
            }
        }
    }
}
