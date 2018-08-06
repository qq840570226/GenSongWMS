namespace BryantG
{
    public class Mission
    {
        public System.DateTime? generateTime { get; private set; } // 任务生成时间
        public System.DateTime? startTime { get; private set; }  // 任务开始执行时间
        public System.DateTime? finishTime { get; private set; }  // 任务完成时间

        public MissionStatus status;  // 任务状态：等待分配小车、小车正在执行任务、任务已完成
        public AGVWPF agv;
        public static uint MissionsCount = 0;  // 生成的任务数
        public uint ID = 0;  // 任务ID
        public Point mssionStartPoint { get; private set; }     // 任务的起始点
        public Point mssionEndPoint { get; private set; }     // 任务的终点

        public Mission(Point mssionStartPoint, Point mssionEndPoint)
        {
            generateTime = System.DateTime.Now;
            startTime = null;
            finishTime = null;
            status = MissionStatus.waiting;
            agv = null;
            this.mssionStartPoint = mssionStartPoint;
            this.mssionEndPoint = mssionEndPoint;
            MissionsCount++;
            ID = MissionsCount;
        }

        public Mission()
        {
            generateTime = null;
            startTime = null;
            finishTime = null;
            status = MissionStatus.waiting;
            agv = null;
            MissionsCount++;
            ID = MissionsCount;
        }

        public void AllocateAGV(AGVWPF agv)
        {
            this.agv = agv;
            startTime = System.DateTime.Now;
            status = MissionStatus.executing;
        }

        public void Finish()
        {
            status = MissionStatus.finished;
            finishTime = System.DateTime.Now;
        }

        public enum MissionStatus
        {
            waiting = 0, executing = 1, finished = 2

        }


    }
}
