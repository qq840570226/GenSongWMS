using System;

namespace BryantG
{
    public static class Tools
    {
        /// <summary>
        /// 计算两个节点之间的欧式距离
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        static public double Distance(Point p1,Point p2 )
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }
}
