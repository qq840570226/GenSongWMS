using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

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
            Task taskRetrieveForkliftStatusStatus = Task.Factory.StartNew(GetForkliftStatusStatusAsync);
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
                foreach (var item in Clients)
                {
                    await item.Value.CloseAsync();
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
        /// <returns></returns>
        public static async Task<bool> AddForkliftAsync(string ip)
        {
            try
            {
                // 先查一下有没有连接过
                if (Clients.TryGetValue(ip + ":9090", out IChannel channelTemp))
                {
                    if (channelTemp.Active)
                    {
                        return false;
                    }
                }
                // 生产一个channel
                IChannel channel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), 9090));
                return Clients.TryAdd(ip + "9090", channel);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="ipAndPort"></param>
        /// <returns></returns>
        public static async Task<bool> RemoveForkliftAsync(string ipAndPort)
        {
            try
            {
                Clients.TryRemove(ipAndPort, out IChannel channel);
                if (channel.Active)
                {
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
                foreach (var item in DataCache.DictionaryForkLiftStatus)
                {
                    // 发送查询
                    byte id = Convert.ToByte(item.Key.Split(':')[0].Split('.')[3]);
                    if (Clients.TryGetValue(item.Key, out IChannel channel))
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
        public static async Task SetTask(string start, string end, int delay = 10)
        {
            string id = null;
            // 找空闲小车
            foreach (var item in Clients)
            {
                DataCache.DictionaryForkLiftStatus.TryGetValue(item.Key, out ForkLiftStatus forkLiftStatusTemp);
                if (forkLiftStatusTemp.state == ForkliftStatusEnum.Free)
                {
                    id = item.Key;
                    break;
                }
            }
            // 没有就算了
            if (id == null)
            {
                return;
            }
            byte vid = Convert.ToByte(id.Split(':')[0].Split('.')[3]);
            // 有就开始计算路径
            DataCache.DictionaryForkLiftStatus.TryGetValue(id, out ForkLiftStatus forkLiftStatus);
            // 去取货
            // 数据缓存线程安全取值
            if (!(DataCache.GetPath(forkLiftStatus.currentNodeNum.ToString(), start, out List<uint> arcList)
                && DataCache.GetPoints(forkLiftStatus.currentNodeNum.ToString(), start, out List<uint> pointList)))
            {
                return;
            }
            // 开始执行
            Clients.TryGetValue(id, out IChannel channel);
            int tempDelay = 0;
            for (int i = 0; i < arcList.Count; i++)
            {
                if (DataCache.DictionaryConfictPoint.TryGetValue(pointList[i + 1], out bool occupyingPoint)
                    && DataCache.DictionaryConfictPath.TryGetValue(arcList[i], out bool occupyingArc))
                {
                    if (occupyingArc || occupyingPoint)
                    {
                        await Task.Delay(1000);
                        i--;
                        tempDelay++;
                        if (tempDelay > delay)
                        {
                            DataCache.DictionaryTrafficJam.AddOrUpdate(id, true, (key, value) => { return value = true; });
                        }
                        continue;
                    }
                    else
                    {
                        // 占用
                        DataCache.DictionaryConfictPoint.AddOrUpdate(pointList[i + 1], true, (key, value) => { return value = true; });
                        DataCache.DictionaryConfictPath.AddOrUpdate(arcList[i + 1], true, (key, value) => { return value = true; });
                        // 下发任务
                        await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, arcList[i], 0, 0, 1, (uint)ACTStatus.AGV_ACT_MOVE));
                        // 等待完成
                        while (true)
                        {
                            DataCache.DictionaryForkLiftStatus.TryGetValue(id, out forkLiftStatus);
                            if (forkLiftStatus.currentNodeNum == pointList[i + 1])
                            {
                                break;
                            }
                            await Task.Delay(500);
                        }
                        // 释放
                        DataCache.DictionaryConfictPoint.AddOrUpdate(pointList[i], false, (key, value) => { return value = false; });
                        DataCache.DictionaryConfictPath.AddOrUpdate(arcList[i + 1], false, (key, value) => { return value = false; });
                        tempDelay = 0;
                        DataCache.DictionaryTrafficJam.AddOrUpdate(id, false, (key, value) => { return value = false; });
                    }
                }
                else
                {
                    await Task.Delay(1000);
                    i--;
                    continue;
                }
            }
            // 取货
            await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, Convert.ToUInt32(start), 0, 0, 1, (uint)ACTStatus.AGV_ACT_PICK));
            await Task.Delay(1000);
            // 送货
            // 数据缓存线程安全取值
            if (!(DataCache.GetPath(start, end, out arcList)
                && DataCache.GetPoints(start, end, out pointList)))
            {
                return;
            }
            // 开始执行
            for (int i = 0; i < arcList.Count; i++)
            {
                if (DataCache.DictionaryConfictPoint.TryGetValue(pointList[i + 1], out bool occupyingPoint)
                    && DataCache.DictionaryConfictPath.TryGetValue(arcList[i], out bool occupyingArc))
                {
                    if (occupyingArc || occupyingPoint)
                    {
                        await Task.Delay(1000);
                        i--;
                        tempDelay++;
                        // 交通堵塞了
                        if (tempDelay > delay)
                        {
                            DataCache.DictionaryTrafficJam.AddOrUpdate(id, true, (key, value) => { return value = true; });
                        }
                        continue;
                    }
                    else
                    {
                        // 占用
                        DataCache.DictionaryConfictPoint.AddOrUpdate(pointList[i + 1], true, (key, value) => { return value = true; });
                        DataCache.DictionaryConfictPath.AddOrUpdate(arcList[i + 1], true, (key, value) => { return value = true; });
                        // 下发任务
                        await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, arcList[i], 0, 0, 1, (uint)ACTStatus.AGV_ACT_MOVE));
                        // 等待完成
                        while (true)
                        {
                            DataCache.DictionaryForkLiftStatus.TryGetValue(id, out forkLiftStatus);
                            if (forkLiftStatus.currentNodeNum == pointList[i + 1])
                            {
                                break;
                            }
                            await Task.Delay(500);
                        }
                        // 释放
                        DataCache.DictionaryConfictPoint.AddOrUpdate(pointList[i], false, (key, value) => { return value = false; });
                        DataCache.DictionaryConfictPath.AddOrUpdate(arcList[i + 1], false, (key, value) => { return value = false; });
                        tempDelay = 0;
                        DataCache.DictionaryTrafficJam.AddOrUpdate(id, false, (key, value) => { return value = false; });
                    }
                }
                else
                {
                    await Task.Delay(1000);
                    i--;
                    continue;
                }
            }
            // 卸货
            await channel.WriteAndFlushAsync(GenSongClientHandler.SetRequestSingleTask(vid, DataCache.NewTaskID(), 1, Convert.ToUInt32(end), 0, 0, 1, (uint)ACTStatus.AGV_ACT_DROP));
            await Task.Delay(1000);
        }
    }
}
