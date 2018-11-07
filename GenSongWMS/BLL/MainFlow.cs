using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace GenSongWMS.BLL
{
    /// <summary>
    /// 主流程
    /// </summary>
    public static class MainFlow
    {
        /// <summary>
        /// 工作线程 默认cpu数*2
        /// </summary>
        private static readonly MultithreadEventLoopGroup group;

        /// <summary>
        /// 引导流程
        /// </summary>
        private static Bootstrap bootstrap;

        /// <summary>
        /// 客户端字典
        /// </summary>
        public static ConcurrentDictionary<string, IChannel> Clients { get; set; }

        /// <summary>
        /// 客户端字典ID
        /// </summary>
        public static ConcurrentDictionary<string, byte> ClientsID { get; set; }

        /// <summary>
        /// 同步叉车状态循环条件
        /// </summary>
        private static bool syncForkliftStatus = true;

        /// <summary>
        /// 主流程开启
        /// </summary>
        static MainFlow()
        {
            group = new MultithreadEventLoopGroup();
            Clients = new ConcurrentDictionary<string, IChannel>();
            ClientsID = new ConcurrentDictionary<string, byte>();
            bootstrap = new Bootstrap();
            bootstrap
                .Group(group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new GenSongClientHandler());
                }));

            // 启动查询进程
            Task taskRetrieveForkliftStatusStatus = Task.Factory.StartNew(GetForkliftStatusStatusAsync, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 结束主流程
        /// </summary>
        public static async Task CloseMainFlow()
        {
            try
            {
                // 结束查询循环
                syncForkliftStatus = false;
                // 关闭客户端
                foreach (var item in Clients.Values)
                {
                    await item.CloseAsync();
                }
            }
            finally
            {
                // 终止工作线程
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }

        /// <summary>
        /// 新建连接
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static async Task<bool> AddForkliftAsync(string ip, string port = "10000", byte id = 0)
        {
            try
            {
                // 生产一个channel
                //IChannel channel = await Task.Run(() => bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port))));
                IChannel channel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port)));
                ClientsID.TryAdd(channel.RemoteAddress.ToString(), id);
                DataCache.DictionaryForkLiftStatus.TryAdd(channel.RemoteAddress.ToString(), default);
                DataCache.DictionaryTrafficJam.TryAdd(channel.RemoteAddress.ToString(), default);
                return Clients.TryAdd(channel.RemoteAddress.ToString(), channel);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static async Task<bool> RemoveForkliftAsync(string ip)
        {
            try
            {
                // 查一下有没有
                string clientKey = null;
                foreach (var item in Clients.Keys)
                {
                    if (item.Contains(ip))
                    {
                        clientKey = item;
                        break;
                    }
                }
                if (clientKey == null)
                {
                    return true;
                }
                // 从字典中删除
                Clients.TryRemove(clientKey, out IChannel channel);
                ClientsID.TryRemove(clientKey, out byte value);
                if (channel.Active)
                {
                    //await Task.Run(() => channel.CloseAsync());
                    await channel.CloseAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 异步查询叉车状态
        /// </summary>
        /// <returns></returns>
        public static async Task GetForkliftStatusStatusAsync()
        {
            while (syncForkliftStatus)
            {
                //查询所有叉车的状态
                foreach (var item in DataCache.DictionaryForkLiftStatus.Keys)
                {
                    // 发送查询
                    ClientsID.TryGetValue(item, out byte id);
                    if (Clients.TryGetValue(item, out IChannel channel))
                    {
                        await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestAGVStatus(id));
                    }
                }
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// 发送任务
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static async Task SetTask(string start, string end, string vids, int delay = 10)
        {
            string clientKey = null;
            //// 找空闲小车
            //foreach (var item in Clients.Keys)
            //{
            //    DataCache.DictionaryForkLiftStatus.TryGetValue(item, out ForkLiftStatus forkLiftStatusTemp);
            //    if (forkLiftStatusTemp.state == ForkliftStatusEnum.Free)
            //    {
            //        clientKey = item;
            //        break;
            //    }
            //}
            //// 没有就算了
            //if (clientKey == null)
            //{
            //    return;
            //}
            byte vid = Convert.ToByte(vids);
            foreach (var item in ClientsID)
            {
                if (item.Value == vid)
                {
                    clientKey = item.Key;
                }
            }

            //ClientsID.TryGetValue(clientKey, out byte vid);
            //// 有就开始计算路径
            //DataCache.DictionaryForkLiftStatus.TryGetValue(clientKey, out ForkLiftStatus forkLiftStatus);
            //// 去取货
            //// 数据缓存线程安全取值
            //if (!(DataCache.GetPath(forkLiftStatus.currentNodeNum.ToString(), start, out List<uint> arcList)
            //    && DataCache.GetPoints(forkLiftStatus.currentNodeNum.ToString(), start, out List<uint> pointList)))
            //{
            //    return;
            //}
            // 开始执行
            Clients.TryGetValue(clientKey, out IChannel channel);
            int tempDelay = 0;
            //for (int i = 0; i < arcList.Count; i++)
            //{
            //    DataCache.DictionaryConfictPoint.TryGetValue(pointList[i], out bool occupyedPoint);
            //    DataCache.DictionaryConfictPoint.TryGetValue(pointList[i + 1], out bool occupyingPoint);
            //    DataCache.DictionaryConfictPath.TryGetValue(arcList[i], out bool occupyingArc);

            //    if (occupyingArc || occupyingPoint)
            //    {
            //        await Task.Delay(1000);
            //        i--;
            //        tempDelay++;
            //        if (tempDelay > delay)
            //        {
            //            DataCache.DictionaryTrafficJam.AddOrUpdate(clientKey, true, (key, value) => { return value = true; });
            //        }
            //        continue;
            //    }
            //    else
            //    {
            //        // 占用
            //        try
            //        {
            //            DataCache.DictionaryConfictPoint.AddOrUpdate(pointList[i + 1], true, (key, value) => { return value = true; });
            //            DataCache.DictionaryConfictPath.AddOrUpdate(arcList[i], true, (key, value) => { return value = true; });
            //        }
            //        catch (ArgumentOutOfRangeException)
            //        {

            //        }
            //        // 下发任务
            //        await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, arcList[i], 0, 0, 1, (uint)ACTStatus.AGV_ACT_MOVE));
            //        // 等待完成
            //        while (true)
            //        {
            //            DataCache.DictionaryForkLiftStatus.TryGetValue(clientKey, out forkLiftStatus);
            //            if (forkLiftStatus.currentNodeNum == pointList[i + 1])
            //            {
            //                break;
            //            }
            //            await Task.Delay(500);
            //        }
            //        // 释放
            //        try
            //        {
            //            DataCache.DictionaryConfictPoint.AddOrUpdate(pointList[i + 1], false, (key, value) => { return value = false; });
            //            DataCache.DictionaryConfictPath.AddOrUpdate(arcList[i], false, (key, value) => { return value = false; });
            //        }
            //        catch (ArgumentOutOfRangeException)
            //        {

            //        }
            //        tempDelay = 0;
            //        DataCache.DictionaryTrafficJam.AddOrUpdate(clientKey, false, (key, value) => { return value = false; });
            //    }
            //}
            //// 取货
            //await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, Convert.ToUInt32(start), 0, 0, 1, (uint)ACTStatus.AGV_ACT_PICK));
            //await Task.Delay(1000);
            // 送货
            // 数据缓存线程安全取值
            if (!(DataCache.GetPath(start, end, out List<uint> arcList)
                && DataCache.GetPoints(start, end, out List<uint> pointList)))
            {
                return;
            }
            // 开始执行
            for (int i = 0; i < arcList.Count; i++)
            {
                DataCache.DictionaryConfictPoint.TryGetValue(pointList[i + 1], out bool occupyingPoint);
                DataCache.DictionaryConfictPath.TryGetValue(arcList[i], out bool occupyingArc);
                {
                    if (occupyingArc || occupyingPoint)
                    {
                        await Task.Delay(1000);
                        i--;
                        tempDelay++;
                        // 交通堵塞了
                        if (tempDelay > delay)
                        {
                            DataCache.DictionaryTrafficJam.AddOrUpdate(clientKey, true, (key, value) => { return value = true; });
                        }
                        continue;
                    }
                    else
                    {
                        // 占用
                        try
                        {
                            DataCache.DictionaryConfictPoint.AddOrUpdate(pointList[i + 1], true, (key, value) => { return value = true; });
                            DataCache.DictionaryConfictPath.AddOrUpdate(arcList[i + 1], true, (key, value) => { return value = true; });
                        }
                        catch (ArgumentOutOfRangeException)
                        {

                        }
                        // 下发任务
                        await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, arcList[i], 0, 0, 1, (uint)ACTStatus.AGV_ACT_MOVE));
                        // 等待完成
                        while (true)
                        {
                            DataCache.DictionaryForkLiftStatus.TryGetValue(clientKey, out ForkLiftStatus forkLiftStatus);
                            if (forkLiftStatus.currentNodeNum == pointList[i + 1] && forkLiftStatus.state == ForkliftStatusEnum.Free)
                            {
                                break;
                            }
                            await Task.Delay(500);
                        }
                        // 释放
                        try
                        {
                            DataCache.DictionaryConfictPoint.AddOrUpdate(pointList[i], false, (key, value) => { return value = false; });
                            DataCache.DictionaryConfictPath.AddOrUpdate(arcList[i + 1], false, (key, value) => { return value = false; });
                        }
                        catch (ArgumentOutOfRangeException)
                        {

                        }
                        tempDelay = 0;
                        DataCache.DictionaryTrafficJam.AddOrUpdate(clientKey, false, (key, value) => { return value = false; });
                    }
                }
            }
            // 卸货
            //await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, Convert.ToUInt32(end), 0, 0, 1, (uint)ACTStatus.AGV_ACT_DROP));
            await Task.Delay(1000);
        }

        public static async Task SetTask2(string start, string end, int delay = 10)
        {
            string clientKey = null;
            // 找空闲小车
            foreach (var item in Clients.Keys)
            {
                DataCache.DictionaryForkLiftStatus.TryGetValue(item, out ForkLiftStatus forkLiftStatusTemp);
                if (forkLiftStatusTemp.state == ForkliftStatusEnum.Free)
                {
                    clientKey = item;
                    break;
                }
            }
            // 没有就算了
            if (clientKey == null)
            {
                return;
            }
            ClientsID.TryGetValue(clientKey, out byte vid);
            if (vid == 1)
            {
                if (!(DataCache.GetPath(start, end, out List<uint> arcList)
                    && DataCache.GetPoints(start, end, out List<uint> pointList)))
                {
                    return;
                }
                Clients.TryGetValue(clientKey, out IChannel channel);
                var x = GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, arcList[0], 0, 0, 1, (uint)ACTStatus.AGV_ACT_MOVE);
                await channel.WriteAndFlushAsync(x);
            }

        }

        public static async Task SetTask3(string start, string end, int delay = 10)
        {
            string clientKey = null;
            // 找空闲小车
            foreach (var item in Clients.Keys)
            {
                DataCache.DictionaryForkLiftStatus.TryGetValue(item, out ForkLiftStatus forkLiftStatusTemp);
                if (forkLiftStatusTemp.state == ForkliftStatusEnum.Free)
                {
                    clientKey = item;
                    break;
                }
            }
            // 没有就算了
            if (clientKey == null)
            {
                return;
            }
            ClientsID.TryGetValue(clientKey, out byte vid);
            DataCache.GetPath(start, end, out List<uint> arcList);
            DataCache.GetPoints(start, end, out List<uint> pointList);
            DataCache.GetPath(end, start, out List<uint> arcList1);
            DataCache.GetPoints(end, start, out List<uint> pointList1);
            Clients.TryGetValue(clientKey, out IChannel channel);
            for (int i = 0; i < 20; i++)
            {
                DataCache.DictionaryForkLiftStatus.TryGetValue(clientKey, out ForkLiftStatus forkLiftStatusTemp);
                if (forkLiftStatusTemp.state == ForkliftStatusEnum.Free  && forkLiftStatusTemp.currentNodeNum == uint.Parse(end))
                {
                    await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, arcList[0], 0, 0, 1, (uint)ACTStatus.AGV_ACT_MOVE));
                }
                else if(forkLiftStatusTemp.state == ForkliftStatusEnum.Free && forkLiftStatusTemp.currentNodeNum == uint.Parse(start))
                {
                    await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, arcList1[0], 0, 0, 1, (uint)ACTStatus.AGV_ACT_MOVE));
                }
            }

        }

        public static byte[] GetVs()
        {
            return GenSongClientHandler.SetRequestSingleTask(1, DataCache.NewTaskID(), 1, 23, 0, 0, 1, (uint)ACTStatus.AGV_ACT_MOVE).Array;
        }
    }
}
