using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using Mathf;

namespace GameServe
{
    /// <summary>
    /// 游戏中单位
    /// </summary>
    class GamePlay
    {
        const int maxWaitTime_event = 1000; //(毫秒)

        private string name;
        
        private PlayerData[] Players;
        private const int maxPlayer = 2;

        int rand;
        bool[] isReady = { false, false };
        public bool isFinish = false;

        private long lastTime = 0;
        private float deltaTime = 0;

        //待发事件队列
        Queue<Event_waitSend> sendEventQueue = new Queue<Event_waitSend>();
        //待回答事件表
        Dictionary<int, Event_waitAnswer> answerEventList = new Dictionary<int, Event_waitAnswer>();
        //当前事件索引表
        ushort currentIndex = 0;

        public GamePlay(Room rm)
        {
            name = rm.name;

            Players = new PlayerData[maxPlayer];
            for(int i = 0;i<maxPlayer;i++)
            {
                Players[i] = new PlayerData();
            }
            //初始化随机种子
            Random rd = new Random();
            rand = rd.Next(1, 1000);
            //初始化玩家数据
            Players[0].Camp = 0;
            Players[0].client = rm.RoomOwner;
            Players[1].Camp = 1;
            Players[1].client = rm.OP_client;

            Players[0].client.socket.BeginReceive(Players[0].client.data, 0, Players[0].client.data.Length, SocketFlags.None, new AsyncCallback(Callback_Receive), Players[0]);
            Players[1].client.socket.BeginReceive(Players[1].client.data, 0, Players[1].client.data.Length, SocketFlags.None, new AsyncCallback(Callback_Receive), Players[1]);
            Players[0].client.isGame = true;
            Players[1].client.isGame = true;

            Thread t = new Thread(Game);
            t.IsBackground = true;
            t.Start();

            for(int i = 0;i<maxPlayer;i++)
            {
                SendInitData(Players[i]);
            }
        }

        /// <summary>
        /// 游戏
        /// </summary>
        /// <param name="rm"></param>
        public void Game()
        {
            //等待玩家初始化完成
            while (true)
            {
                int t = 0;
                for (int i = 0; i < maxPlayer; i++)
                {
                    if (isReady[i])
                    {
                        t++;
                    }
                }
                if (t == maxPlayer)
                {
                    //开始游戏
                    for(int i = 0;i<maxPlayer;i++)
                    {
                        Players[i].client.Send("BG");
                    }
                    break;
                }
            }

            while(!isFinish)
            {


                //更新玩家数据
                for(int i = 0;i<Players.Length;i++)
                {
                    Players[i].Update(deltaTime);
                }

                //发送玩家事件
                int c = 0;
                string data_1 = "BM";
                for (int i = 0; i < Players.Length; i++)
                {
                    if (Players[i].msgQueue.Count > 0)
                    {
                        data_1 += Players[i].getMsg();
                        c++;
                    }
                }
                if(c > 0)
                {
                    BroadcastMsg(data_1);
                }

                //处理待发事件队列
                if(sendEventQueue.Count > 0)
                {
                    string t_msg = "GE";
                    while (sendEventQueue.Count > 0)
                    {
                        Event_waitSend t = sendEventQueue.Dequeue();
                        t_msg += t.ToString();
                        AddEvent(t);
                    }
                    BroadcastMsg(t_msg);
                }

                //处理待回答事件列表
                long currentTimeStamp = ServerFunction.getTimeStamp_milSeconds();
                if (answerEventList.Count > 0)
                {
                    Dictionary<int, Event_waitAnswer>.KeyCollection keysCollect = null;
                    keysCollect = answerEventList.Keys;
                    int[] keys = new int[keysCollect.Count];
                    keysCollect.CopyTo(keys, 0);
                    for(int i = 0;i<keys.Length;i++)
                    {
                        Event_waitAnswer t = answerEventList[keys[i]];
                        //超时
                        if (currentTimeStamp > t.endTime)
                        {
                            //如果超过一半玩家应答则予以回应
                            if (t.getAnswerNum() > maxPlayer * 0.5f)
                            {
                                int t_answer = t.Answer();
                                string t_data = string.Format("EA {0}:{1}", t.data.selfIndex, t_answer);
                                t.data.client.Send(t_data);
                            }
                            Console.WriteLine("Timeout");
                            answerEventList.Remove(keys[i]);
                        }
                    }
                }
            

            //获取玩家信息包
            string data = "PD ";
                for(int i = 0;i<Players.Length;i++)
                {
                    data += (getPlayerData(i));
                    if(i != Players.Length - 1)
                    {
                        data += "#";
                    }
                }

                //发送玩家信息包
                int j = 0;
                for (int i = 0; i < Players.Length; i++)
                {
                    if (Players[i].client.isLink)
                    {
                        Players[i].client.Send(data);
                    }
                    else
                    {
                        j++;
                    }
                }

                //检测是否所有玩家都已掉线
                if(j == maxPlayer)
                {
                    Console.WriteLine(name + "_房间结束游戏");
                    isFinish = true;
                    break;
                }

                //更新时间参数
                deltaTime = (currentTimeStamp - lastTime) / 1000.0f;
                lastTime = currentTimeStamp;

                Thread.Sleep(50);
            }
        }

        const string format_initData = "RD {0} {1}";
        /// <summary>
        /// 发送初始化数据给玩家
        /// </summary>
        /// <param name="pd"></param>
        void SendInitData(PlayerData pd)
        {
            string t = string.Format(format_initData, rand, pd.Camp);
            pd.client.Send(t);

        }

        void Callback_Receive(IAsyncResult ar)
        {
            PlayerData player = (PlayerData)ar.AsyncState;

            try
            {
                int t = player.client.socket.EndReceive(ar);
                if (t == 0)
                {
                    player.client.isLink = false;
                    return;
                }
            }
            catch
            {
                player.client.isLink = false;
                return;
            }

            string data = System.Text.Encoding.UTF8.GetString(player.client.data).TrimEnd('\0');
            string[] devideData;
            try
            {
                devideData = ServerFunction.DevideMsg(data);
                for (int i = 0; i < devideData.Length; i++)
                {
                    if (devideData[i].CompareTo("HB") != 0 && devideData[i].Substring(0,2).CompareTo("PD") != 0)
                    {
                        Console.WriteLine("房间内：  " + devideData[i]);
                    }
                    Parse(devideData[i], player);
                }
            }
            catch
            {
                Console.WriteLine("Error Package");
            }

            //清空数组数据
            Array.Clear(player.client.data, 0, player.client.data.Length);

            if (player.client.isLink)
            {
                try
                {
                    player.client.socket.BeginReceive(player.client.data, 0, player.client.data.Length, SocketFlags.None, new AsyncCallback(Callback_Receive), player);
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// 解析消息
        /// </summary>
        /// <param name="msg"></param>
        void Parse(string msg,PlayerData player)
        {
            string type = msg.Substring(0, 2);
            switch(type)
            {
                case "RD":
                    ParseReadyGame(msg, player);
                    break;
                case "HB":
                    ParseHeartbeat(msg, player);
                    break;
                case "PD":
                    ParsePlayerData(msg, player);
                    break;
                case "GE":
                    ParseGameEvent(msg, player);
                    break;
                case "EA":
                    ParseEventAnswer(msg, player);
                    break;
                case "UP":
                    ParseUpdateData(msg, player);
                    break;
            }
        }

        /// <summary>
        /// 准备游戏
        /// </summary>
        /// <param name="msg"></param>
        void ParseReadyGame(string msg, PlayerData player)
        {
            isReady[player.Camp] = true;
        }

        /// <summary>
        /// 更新心跳
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseHeartbeat(string msg,PlayerData player)
        {
            player.client.updateTick(ServerFunction.getTimeStamp());
        }

        const string format_PDunit = "{0}:{1}:{2}:{3}:{4}:{5}";  //对象：位置：朝向：分数：DValue：运送分数
        /// <summary>
        /// 获取玩家数据
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        string getPlayerData(int i)
        {
            PlayerData t = Players[i];

            return string.Format(format_PDunit, t.Camp, t.Pos.ToString(), t.Dir.ToString(), t.Score,(int)(t.D_Value * config.precision),t.carryScore);
        }

        /// <summary>
        /// 解析玩家数据
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParsePlayerData(string msg,PlayerData player)
        {
            string[] t = msg.Split(' ');
            player.Pos = Vector3.Parse(t[1], ',');
            player.Dir = Vector3.Parse(t[2], ',');
            player.Score = int.Parse(t[3]);
            player.carryScore = int.Parse(t[4]);
        }

        /// <summary>
        /// 解析游戏事件
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseGameEvent(string msg,PlayerData player)
        {
            // GE selfIndex@event:(参数) ...
            string[] msgs = msg.Split(' ');
            int eventCount = msgs.Length - 1;
            for(int i = 1;i<=eventCount;i++)
            {
                string[] t = msgs[i].Split('@');
                ushort index = currentIndex++;
                Event_waitSend t_e = new Event_waitSend(t[1], t[0], index, player.Camp, player.client);
                sendEventQueue.Enqueue(t_e);
            }
        }

        /// <summary>
        /// 解析事件回答
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseEventAnswer(string msg,PlayerData player)
        {
            // EA index@answer
            string[] data = msg.Split(' ')[1].Split('@');
            int index = int.Parse(data[0]);
            int answer = int.Parse(data[1].Split(':')[1]);
            try
            {
                Event_waitAnswer t = answerEventList[index];
                t.AddAnswer(answer);
                if(t.getAnswerNum() == maxPlayer)
                {
                    int t_answer = t.Answer();
                    answerEventList.Remove(index);
                    string t_data = string.Format("EA {0}:{1}",t.data.selfIndex,t_answer);
                    t.data.client.Send(t_data);
                }
            }
            catch
            {
                Console.WriteLine("Error EA");
            }
        }

        /// <summary>
        /// 向所有玩家发送该信息
        /// </summary>
        /// <param name="msg"></param>
        void BroadcastMsg(string msg)
        {
            for (int i = 0; i < Players.Length; i++)
            {
                if (Players[i].client.isLink)
                {
                    Players[i].client.Send(msg);
                }
            }
        }

        /// <summary>
        /// 将事件放入待回答队列
        /// </summary>
        void AddEvent(Event_waitSend t)
        {
            Event_waitAnswer t_a = new Event_waitAnswer();
            t_a.data = t;
            t_a.endTime = ServerFunction.getTimeStamp_milSeconds() + maxWaitTime_event;
            answerEventList.Add(t.index, t_a);
        }

        /// <summary>
        /// 解析数据更新
        /// camp@数据名@value
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseUpdateData(string msg,PlayerData player)
        {
            int camp;
            string paramName;
            string value;

            //解析参数
            string[] t = msg.Split(' ')[1].Split('@');
            camp = int.Parse(t[0]);
            paramName = t[1];
            value = t[2];

            Players[camp].updateData(paramName, value, player);
        }

    }
}
