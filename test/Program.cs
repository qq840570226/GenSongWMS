using System.Net;
using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(new IPEndPoint(IPAddress.Parse("127.1.27.0"), 9090).ToString());
            //Console.Read();

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            //for (int i = 0; i < 10000; i++)
            //{
            //    //string str = $"{100},{200}";
            //    string str = i + "," + (i + 1);
            //}
            //stopwatch.Stop();
            //Console.WriteLine(stopwatch.ElapsedTicks);
            //stopwatch.Restart();
            //for (int i = 0; i < 10000; i++)
            //{
            //    int x = 100 * 1000 + 200;
            //}
            //stopwatch.Stop();
            //Console.WriteLine(stopwatch.ElapsedTicks);
            //stopwatch.Restart();
            //Test1 test1;
            //for (int i = 0; i < 10000; i++)
            //{
            //    test1 = new Test1(100 + i, 200 + i);
            //}
            //stopwatch.Stop();
            //Console.WriteLine(stopwatch.ElapsedTicks);
            //Console.Read();

            //Console.WriteLine(default(bool));
            //Console.Read();

            //Test1 test1 = new Test1(100, 200);
            //Test1 test2 = new Test1(100, 200);
            //Console.WriteLine(test1.Equals(test2) ? "yes" : "no ");
            var x = BitConverter.GetBytes(123);
            byte[] xx = BitConverter.GetBytes(123);
            List<byte> vs = new List<byte>();
            vs.AddRange(x);
            vs.ToArray();
            string content = GenerateStatus(1, ForkliftStatusEnum.GotoPickdownPoint, 30201, 1, 5, 1, 2, 2);
            Console.WriteLine(content);
            Console.Read();
        }

        public struct Test1
        {
            public readonly int S;
            public readonly int E;

            public Test1(int s, int e)
            {
                this.S = s;
                this.E = e;
            }
        }

        static string GenerateStatus(byte id, ForkliftStatusEnum forkliftStatusEnum, uint currentNode, uint currentMap, ushort battery, uint X, uint Y, uint angle)
        {
            byte[] sendMsg = new byte[35];

            sendMsg[0] = 0x47;
            sendMsg[1] = 0x53;
            sendMsg[2] = 0x00;
            sendMsg[3] = 0x00;

            sendMsg[4] = id;

            sendMsg[5] = 0x53;
            sendMsg[6] = 0x46;

            sendMsg[7] = 0x01;

            sendMsg[8] = (byte)forkliftStatusEnum;

            sendMsg[9] = (byte)(currentNode & 0xFF);
            sendMsg[10] = (byte)(currentNode >> 8 & 0xFF);
            sendMsg[11] = (byte)(currentNode >> 16 & 0xFF);
            sendMsg[12] = (byte)(currentNode >> 24 & 0xFF);

            sendMsg[13] = (byte)(currentMap & 0xFF);
            sendMsg[14] = (byte)(currentMap >> 8 & 0xFF);

            sendMsg[15] = (byte)(battery & 0xFF);
            sendMsg[16] = (byte)(battery >> 8 & 0xFF);

            sendMsg[17] = 0x00;
            sendMsg[18] = 0x00;
            sendMsg[19] = 0x00;
            sendMsg[20] = 0x00;

            sendMsg[21] = (byte)(X & 0xFF);
            sendMsg[22] = (byte)(X >> 8 & 0xFF);
            sendMsg[23] = (byte)(X >> 16 & 0xFF);
            sendMsg[24] = (byte)(X >> 24 & 0xFF);

            sendMsg[25] = (byte)(Y & 0xFF);
            sendMsg[26] = (byte)(Y >> 8 & 0xFF);
            sendMsg[27] = (byte)(Y >> 16 & 0xFF);
            sendMsg[28] = (byte)(Y >> 24 & 0xFF);

            sendMsg[29] = (byte)(angle & 0xFF);
            sendMsg[30] = (byte)(angle >> 8 & 0xFF);
            sendMsg[31] = (byte)(angle >> 16 & 0xFF);
            sendMsg[32] = (byte)(angle >> 24 & 0xFF);
        
            ushort crcTmp = CRC16(sendMsg, 35);
            //CRC16校验
            sendMsg[33] = (byte)(crcTmp & 0xFF);
            sendMsg[34] = (byte)(crcTmp >> 8 & 0xFF);

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in sendMsg)
            {
                stringBuilder.AppendFormat("{0:x2}", item);
            }
            
            return stringBuilder.ToString();
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
        /// crc16校验
        /// </summary>
        /// <param name="Pushdata"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static ushort CRC16(byte[] Pushdata, int length)
        {
            ushort Reg_CRC = 0xffff;
            ushort Temp_reg = 0x00;
            ushort i, j;
            for (i = 0; i < length; i++)
            {
                Reg_CRC ^= Pushdata[i];
                for (j = 0; j < 8; j++)
                {
                    if ((Reg_CRC & 0x0001) != 0)

                        Reg_CRC = (ushort)((Reg_CRC >> 1) ^ 0xA001);

                    else
                        Reg_CRC >>= 1;
                }
            }
            Temp_reg = (byte)(Reg_CRC >> 8);
            return (ushort)(Reg_CRC << 8 | Temp_reg);
        }
    }
}
