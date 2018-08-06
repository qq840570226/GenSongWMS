using System.Collections.Generic;

namespace BryantG
{
    /// <summary>
    /// 用来在wpf上绘制的agv小车的类
    /// </summary>
    public class AGVWPF : AGV
    {
        /// <summary>
        /// 建立带wpf动画的agv小车
        /// </summary>
        /// <param name="initial">起始点</param>
        /// <param name="num">小车编号</param>
        public AGVWPF(object initial, uint num , Vector direction) : base(initial, num, direction)
        {

        }
    }

    public struct MissionAssignmentDisplay
    {
        public string startEnd { get; set; }
        public string missionID { get; set; }
        public string agvAddr { get; set; }
        public string path { get; set; }
        public string length { get; set; }

        public MissionAssignmentDisplay(MissionAssignment ma)
        {
            Mission mission = ma.mission;
            AGVWPF agv = ma.agv;

            if ( ma.path != null )
            {
                path = ma.path.ToString();
                int tmp = (int)ma.path.length;
                length = tmp.ToString();
            }
            else
            {
                path = "null";
                length = "-";
            }
            startEnd = mission.mssionStartPoint.ID.ToString() + "-" + mission.mssionEndPoint.ID.ToString();
            missionID = mission.ID.ToString();
            if ( agv != null )
            {
                agvAddr = agv.PrePoint.ID.ToString();
            }
            else
            {
                agvAddr = "-";
            }
        }

        public override string ToString()
        {
            string str ;
            str = "startEnd: " + startEnd + "\r\n";
            str = str + "missionID: " + missionID + "\r\n";
            str = str + "agvID: " + agvAddr + "\r\n";
            str = str + "path: " + path + "\r\n";
            str = str + "length: " + length;
            return str;
        }
    }
}
