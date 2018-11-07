using System;
using System.Windows;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace GenSongWMS.BLL
{
    /// <summary>
    /// 处理井松叉车的流程
    /// </summary>
    class GenSongClientHandler : ChannelHandlerAdapter
    {
        public GenSongClientHandler()
        { }

        /// <summary>
        /// 激活
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelActive(IChannelHandlerContext context)
        {
            MessageBox.Show(context.Channel.RemoteAddress.ToString() + "已连接!");
        }

        /// <summary>
        /// 接收
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (!(message is IByteBuffer buffer))
            {
                return;
            }
            try
            {
                byte[] functionCode = new byte[2];
                byte[] receiveData;
                buffer.GetBytes(5, functionCode);
                switch (GetBackMsgType(functionCode))
                {
                    //查询地图文件可否下发
                    case MsgType.MF:
                        break;
                    //地图文件下发或者节点数据下发
                    case MsgType.FD:
                        break;
                    //心跳
                    case MsgType.HB:
                        break;
                    //任务下发
                    case MsgType.TB:
                        receiveData = new byte[13];
                        buffer.GetBytes(2, receiveData);
                        task_issued_response taskResponse = new task_issued_response(receiveData[6], (uint)(receiveData[7] | receiveData[8] << 8 | receiveData[9] << 16 | receiveData[10] << 24));
                        break;
                    case MsgType.TD:
                        break;
                    //查询状态
                    case MsgType.SF:
                        receiveData = new byte[33];
                        buffer.GetBytes(2, receiveData);
                        ForkLiftStatus forkliftStatusRes = new ForkLiftStatus(GetForkliftStatus(receiveData[6]),
                            (uint)(receiveData[7] | receiveData[8] << 8 | receiveData[9] << 16 | receiveData[10] << 24),
                            (ushort)(receiveData[11] | receiveData[12] << 8),
                            (ushort)(receiveData[13] | receiveData[14] << 8),
                            (uint)(receiveData[15] | receiveData[16] << 8 | receiveData[17] << 16 | receiveData[18] << 24),
                            (int)(receiveData[19] | receiveData[20] << 8 | receiveData[21] << 16 | receiveData[22] << 24),
                            (int)(receiveData[23] | receiveData[24] << 8 | receiveData[25] << 16 | receiveData[26] << 24),
                            (int)(receiveData[27] | receiveData[28] << 8 | receiveData[29] << 16 | receiveData[30] << 24)
                            );
                        // 增量插入
                        DataCache.DictionaryForkLiftStatus.AddOrUpdate(context.Channel.RemoteAddress.ToString(), forkliftStatusRes, (key, value) => { return value = forkliftStatusRes; });
                        DataCache.DictionaryConfictPoint.AddOrUpdate(forkliftStatusRes.currentNodeNum, true, (key, value) => { return value = true; });
                        break;
                    //任务上报完成
                    case MsgType.TF:
                        receiveData = new byte[12];
                        buffer.GetBytes(2, receiveData);
                        ulong taskNo = (uint)(receiveData[6] | receiveData[7] << 8 | receiveData[8] << 16 | receiveData[9] << 24);
                        // send
                        context.WriteAndFlushAsync(SetResponseTaskFinishMsg(receiveData[2], taskNo));
                        break;
                    case MsgType.ERROR:
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }
            finally
            {
                buffer.Clear();
            }
        }

        /// <summary>
        /// 断开
        /// </summary>
        /// <param name="context"></param>
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            string clientKey = context.Channel.RemoteAddress.ToString();
            // 从字典中删除
            MainFlow.Clients.TryRemove(clientKey, out IChannel channel);
            MainFlow.ClientsID.TryRemove(clientKey, out byte value);
            DataCache.DictionaryForkLiftStatus.TryRemove(clientKey, out ForkLiftStatus forkLiftStatus);
            MessageBox.Show(clientKey + "已断开!");
        }

        /// <summary>
        /// 异常
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {        
            MessageBox.Show("因" + exception.ToString() + "原因" + context.Channel.RemoteAddress.ToString() + "断开!");
        }

        /// <summary>
        /// 解析消息类型
        /// </summary>
        /// <param name="receiveData"></param>
        /// <returns></returns>
        public MsgType GetBackMsgType(byte[] receiveData)
        {
            uint temp = (uint)(receiveData[0] << 8 | receiveData[1]);

            try
            {
                MsgType result = (MsgType)temp;
                return result;
            }
            catch
            {
                return MsgType.ERROR;
            }
        }

        /// <summary>
        /// 解析叉车状态
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public ForkliftStatusEnum GetForkliftStatus(byte status)
        {
            try
            {
                ForkliftStatusEnum forkliftStatusEnum = (ForkliftStatusEnum)status;
                return forkliftStatusEnum;
            }
            catch
            {
                return ForkliftStatusEnum.Malfunction;
            }
        }

        /// <summary>
        /// 发送任务->byte[]
        /// </summary>
        /// <param name="vehicleID"></param>
        /// <param name="taskNo"></param>
        /// <param name="mapNo"></param>
        /// <param name="segNo"></param>
        /// <param name="loadParam"></param>
        /// <param name="vehicleDirection"></param>
        /// <param name="reserved"></param>
        /// <param name="actType"></param>
        /// <returns></returns>
        public static IByteBuffer SetRequestSingleTask(byte vehicleID, ulong taskNo, uint mapNo, ulong segNo, ulong loadParam, uint vehicleDirection, ulong reserved, ulong actType)
        {
            byte[] sendMsg = new byte[32];

            sendMsg[0] = 0x47;
            sendMsg[1] = 0x53;
            sendMsg[2] = 0x16;
            sendMsg[3] = 0x16 >> 8;
            sendMsg[4] = vehicleID;
            //功能码
            sendMsg[5] = 0x54;
            sendMsg[6] = 0x42;
            //附加功能码
            sendMsg[7] = 0x00;

            //任务编号
            sendMsg[8] = (byte)(taskNo & 0xFF);
            sendMsg[9] = (byte)(taskNo >> 8 & 0xFF);
            sendMsg[10] = (byte)(taskNo >> 16 & 0xFF);
            sendMsg[11] = (byte)(taskNo >> 24 & 0xFF);

            //地图号
            sendMsg[12] = (byte)(mapNo & 0xFF);
            sendMsg[13] = (byte)(mapNo >> 8 & 0xFF);
            //线段号
            sendMsg[14] = (byte)(segNo & 0xFF);
            sendMsg[15] = (byte)(segNo >> 8 & 0xFF);
            sendMsg[16] = (byte)(segNo >> 16 & 0xFF);
            sendMsg[17] = (byte)(segNo >> 24 & 0xFF);
            //取放参数
            sendMsg[18] = (byte)(loadParam & 0xFF);
            sendMsg[19] = (byte)(loadParam >> 8 & 0xFF);
            sendMsg[20] = (byte)(loadParam >> 16 & 0xFF);
            sendMsg[21] = (byte)(loadParam >> 24 & 0xFF);
            //运行时车体的姿态角度
            //SendMsgTmp[22] = (byte)(vehicleDirection & 0xFF);
            //SendMsgTmp[23] = (byte)(vehicleDirection >> 8 & 0xFF);
            //保留域
            sendMsg[22] = (byte)(reserved & 0xFF);
            sendMsg[23] = (byte)(reserved >> 8 & 0xFF);
            sendMsg[24] = (byte)(reserved >> 16 & 0xFF);
            sendMsg[25] = (byte)(reserved >> 24 & 0xFF);
            //动作类型
            sendMsg[26] = (byte)(actType & 0xFF);
            sendMsg[27] = (byte)(actType >> 8 & 0xFF);
            sendMsg[28] = (byte)(actType >> 16 & 0xFF);
            sendMsg[29] = (byte)(actType >> 24 & 0xFF);

            //CRC16校验码
            uint crcTmp = 0x0;

            crcTmp = CRC16(sendMsg, 30);
            //crcTmp = CrcChk(Encoding.ASCII.GetChars(SendMsgTmp), 32);
            //CRC16校验码
            sendMsg[30] = (byte)(crcTmp & 0xFF);
            sendMsg[31] = (byte)(crcTmp >> 8 & 0xFF);

            return Unpooled.CopiedBuffer(sendMsg);
        }

        /// <summary>
        /// 任务上报完成回复
        /// </summary>
        /// <param name="vehicleID"></param>
        /// <param name="taskNo"></param>
        /// <returns></returns>
        public static IByteBuffer SetResponseTaskFinishMsg(byte vehicleID, ulong taskNo)
        {
            byte[] sendMsg = new byte[14];

            sendMsg[0] = 0x47;
            sendMsg[1] = 0x53;
            sendMsg[2] = 0x04;
            sendMsg[3] = 0x00;
            sendMsg[4] = vehicleID;
            //功能码
            sendMsg[5] = 0x54;
            sendMsg[6] = 0x46;
            //附加功能码
            sendMsg[7] = 0x01;

            //任务编号
            sendMsg[8] = (byte)(taskNo & 0xFF);
            sendMsg[9] = (byte)(taskNo >> 8 & 0xFF);
            sendMsg[10] = (byte)(taskNo >> 16 & 0xFF);
            sendMsg[11] = (byte)(taskNo >> 24 & 0xFF);

            //CRC16校验码
            uint crcTmp = 0x0;

            crcTmp = CRC16(sendMsg, 12);
            //CRC16校验
            sendMsg[12] = (byte)(crcTmp & 0xFF);
            sendMsg[13] = (byte)(crcTmp >> 8 & 0xFF);

            return Unpooled.CopiedBuffer(sendMsg);
        }


        /// <summary>
        /// 查询状态
        /// </summary>
        /// <param name="vehicleID"></param>
        public static IByteBuffer SetRequestAGVStatus(byte vehicleID)
        {
            byte[] sendMsg = new byte[10];

            sendMsg[0] = 0x47;
            sendMsg[1] = 0x53;
            sendMsg[2] = 0x0;
            sendMsg[3] = 0x00;
            sendMsg[4] = vehicleID;
            //功能码
            sendMsg[5] = 0x53;
            sendMsg[6] = 0x46;
            //附加功能码
            sendMsg[7] = 0x00;
            //CRC16校验码
            uint crcTmp = 0x0;

            crcTmp = CRC16(sendMsg, 8);
            //CRC16校验
            sendMsg[8] = (byte)(crcTmp & 0xFF);
            sendMsg[9] = (byte)(crcTmp >> 8 & 0xFF);

            return Unpooled.CopiedBuffer(sendMsg);
        }

        /// <summary>
        /// crc16校验
        /// </summary>
        /// <param name="Pushdata"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static uint CRC16(byte[] Pushdata, int length)
        {
            uint Reg_CRC = 0xffff;
            uint Temp_reg = 0x00;
            uint i, j;
            for (i = 0; i < length; i++)
            {
                Reg_CRC ^= Pushdata[i];
                for (j = 0; j < 8; j++)
                {
                    if ((Reg_CRC & 0x0001) != 0)

                        Reg_CRC = (uint)((Reg_CRC >> 1) ^ 0xA001);

                    else
                        Reg_CRC >>= 1;
                }
            }
            Temp_reg = (byte)(Reg_CRC >> 8);
            return (uint)(Reg_CRC << 8 | Temp_reg);
        }
    }

    /// <summary>
    /// 报文类型
    /// </summary>
    public enum MsgType
    {
        MF = 0x4D46,//查询地图文件可否下发
        FD = 0x4645,//地图文件下发或者节点数据下发
        HB = 0x4842,//心跳
        TB = 0x5442,//任务下发标准版
        TD = 0x5445,//任务下发            
        SF = 0x5346,//查询状态
        TF = 0x5446,//任务上报完成
        ERROR = 0x0000
    }

    /// <summary>
    /// 叉车状态
    /// </summary>
    public enum ForkliftStatusEnum
    {
        GotoPickupPoint = 0x01,  //前往取货点
        PickupGoods = 0x02,      //取货点取货
        GotoPickdownPoint = 0x03,//前往送货点
        PickdownGoods = 0x04,    //送货点放货
        FinishTask = 0x05,       //任务完成
        Free = 0x06,             //空闲
        Malfunction = 0x07,      //故障
        Charging = 0x08,         //充电中
        ManualOperation = 0x09   //手动
    }

    /// <summary>
    /// 叉车详细状态
    /// </summary>
    public struct ForkLiftStatus
    {
        public readonly ForkliftStatusEnum state; //状态：1前往取货点，2取货点取货，3前往送货点，4送货点,5：任务完成,6：空闲,7：故障,8：充电中,9：手动  
        public readonly uint currentNodeNum;//当前节点号
        public readonly ushort currentPathNum;//当前地图号
        public readonly ushort electricityValue; //电量值
        public readonly uint faultCode;//故障代码
        public readonly int pos_ux;     //坐标X，单位mm
        public readonly int pos_uy;
        public readonly int currentAngle;//当前姿态角度

        /// <summary>
        /// 叉车详细状态
        /// </summary>
        /// <param name="state">状态：1前往取货点，2取货点取货，3前往送货点，4送货点,5：任务完成,6：空闲,7：故障,8：充电中,9：手动</param>
        /// <param name="currentNodeNum">当前节点号</param>
        /// <param name="currentPathNum">当前地图号</param>
        /// <param name="electricityValue">电量值</param>
        /// <param name="faultCode">故障代码</param>
        /// <param name="pos_ux">坐标X，单位mm</param>
        /// <param name="pos_uy">坐标Y，单位mm</param>
        /// <param name="pos_uth"></param>
        /// <param name="currentAngle">当前姿态角度</param>
        public ForkLiftStatus(ForkliftStatusEnum state, uint currentNodeNum, ushort currentPathNum, ushort electricityValue, uint faultCode, int pos_ux, int pos_uy, int currentAngle)
        {
            this.state = state;
            this.currentNodeNum = currentNodeNum;
            this.currentPathNum = currentPathNum;
            this.electricityValue = electricityValue;
            this.faultCode = faultCode;
            this.pos_ux = pos_ux;
            this.pos_uy = pos_uy;
            this.currentAngle = currentAngle;
        }
    };

    /// <summary>
    /// 任务状态
    /// </summary>
    public struct task_issued_response
    {
        public readonly byte recv_state;   //接收状态
        public readonly ulong task_number;//任务编号

        public task_issued_response(byte recv_state, ulong task_number)
        {
            this.recv_state = recv_state;
            this.task_number = task_number;
        }
    };

    /// <summary>
    /// act状态
    /// </summary>
    public enum ACTStatus
    {
        AGV_ACT_MOVE = 0,
        AGV_ACT_PICK = 1,
        AGV_ACT_DROP = 2,
        AGV_ACT_CHGR = 4,
        AGV_ACT_TURN = 5,
        AGV_ACT_LIFT = 6,
        AGV_ACT_FKFD = 7,
        AGV_ACT_FKBK = 8,
        AGV_ACT_ARCM = 9,
        AGV_ACT_EXPD = 10,
        AGV_ACT_SHRK = 11,
    }
}
