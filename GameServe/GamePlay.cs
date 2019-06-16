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

        public string name;

        //时间
        public float Time = 0;
        
        private PlayerData[] Players;
        private const int maxPlayer = 2;

        int rand;
        bool[] isReady = { false, false };
        public bool isFinish = false;

        //据点
        StrongholdInfo[] Stronghold = new StrongholdInfo[config.MAXCOUNT_STRONGHOLD];

        private long lastTime = 0;
        private float deltaTime = 0;

        //线程
        Thread Thread_game = null;

        //结束游戏后房间内剩余玩家
        int leftPlayerCount = maxPlayer;

        //单位信息
        Dictionary<string, UnitInfo>[] UnitDic = new Dictionary<string, UnitInfo>[maxPlayer];

        //待发事件队列
        Queue<Event_waitSend> sendEventQueue = new Queue<Event_waitSend>();
        //待回答事件表
        Dictionary<int, Event_waitAnswer> answerEventList = new Dictionary<int, Event_waitAnswer>();
        //索引
        ushort currentIndex = 0;
        ushort currentIndex_material = 0;
        int UnitIndex = 0;
        //材料箱容器
        Dictionary<ushort, materialBox> Dic_MaterialBox = new Dictionary<ushort, materialBox>();

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

            Thread_game = new Thread(Game);
            Thread_game.IsBackground = true;
            Thread_game.Start();

            for(int i = 0;i<maxPlayer;i++)
            {
                Players[i].gameplayRoom = this;
                SendInitData(Players[i]);
            }
            for(int i = 0;i<Stronghold.Length;i++)
            {
                Stronghold[i] = new StrongholdInfo();
            }

            lastTime = ServerFunction.getTimeStamp_milSeconds();

            //初始化单位信息字典
            for(int i = 0;i<UnitDic.Length;i++)
            {
                UnitDic[i] = new Dictionary<string, UnitInfo>();
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
                Time += deltaTime;

                //检测是否结束游戏
                int t3 = 0;
                for(int i = 0;i<Players.Length;i++)
                {
                    if(Players[i].isEnd)
                    {
                        t3++;
                    }
                }
                if(t3 == Players.Length)
                {
                    //结束游戏
                    Console.WriteLine(name + "_房间结束游戏");
                    string data_EG = getEndGameData();
                    BroadcastMsg(data_EG);
                    break;
                }

                Thread.Sleep(50);
            }

            while(true)
            {
                //检测是否所有玩家都已退出
                int j = 0;
                for (int i = 0; i < Players.Length; i++)
                {
                    if (!Players[i].client.isLink && !Players[i].isLeftGameRoom)
                    {
                        Players[i].isLeftGameRoom = true;
                    }

                    if(Players[i].isLeftGameRoom)
                    {
                        j++;
                    }
                }

                if (j == maxPlayer)
                {
                    isFinish = true;
                    break;
                }

                Thread.Sleep(1000);
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

            if (player.client.isLink && !player.isLeftGameRoom)
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
                case "UD":
                    ParseUpdateData(msg, player);
                    break;
                case "EG":
                    ParseEndGame(msg, player);
                    break;
                case "BH":
                    ParseBackHall(msg, player);
                    break;
                case "SH":
                    ParseStrongHold(msg, player);
                    break;
                case "GM":
                    ParseGetMaterial(msg, player);
                    break;
                case "DM":
                    ParseDamageInfo(msg, player);
                    break;
                case "UB":
                    ParseUnitBehavious(msg, player);
                    break;
                case "CU":
                    ParseCreateUnit(msg, player);
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

        const string format_PDunit = "{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}";  //对象：位置：朝向：分数：DValue：运送分数:垂直飞行：水平飞行
        /// <summary>
        /// 获取玩家数据
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        string getPlayerData(int i)
        {
            PlayerData t = Players[i];

            return string.Format(format_PDunit, t.Camp, t.Pos.ToString(), t.Dir.ToString(), t.Score,(int)(t.D_Value * config.precision),t.carryScore, t.isCraft_Vertical?1:0, t.isCraft_Horizontal?1:0);
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
            player.carryScore += int.Parse(t[3]);
            player.isCraft_Vertical = int.Parse(t[4]) == 1 ? true:false;
            player.isCraft_Horizontal = int.Parse(t[5]) == 1 ? true : false;
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

        /// <summary>
        /// 解析结束游戏
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseEndGame(string msg,PlayerData player)
        {
            player.isEnd = true;
        }

        const string Format_EndGame = " {0}:{1}:{2}:{3}";  //阵营：ID：得分：击杀数
        /// <summary>
        /// 获取游戏结果
        /// </summary>
        /// <returns></returns>
        string getEndGameData()
        {
            string data = "EG";
            //判断获胜方
            int winCamp = 0;
            int maxScore = 0;
            for(int i = 0;i<Players.Length;i++)
            {
                if(maxScore < Players[i].Score)
                {
                    winCamp = Players[i].Camp;
                }
            }
            data += string.Format(" {0}", winCamp);

            for(int i = 0;i<Players.Length; i++)
            {
                data += string.Format(Format_EndGame, Players[i].Camp, Players[i].client.ID, Players[i].Score, Players[i].KillCount);
            }
            return data;
        }

        /// <summary>
        /// 解析返回大厅
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseBackHall(string msg,PlayerData player)
        {
            player.isLeftGameRoom = true;
            player.client.isGame = false;
            player.client.isStayRoom = false;
            //接收消息转换
            Program.addLinstener(player.client);
        }

        string format_Stronghold_hold = "BM Stronghold@H:{0}:{1}:{2}";  //index:camp:占据时间
        string format_Stronghold_close = "BM Stronghold@C:{0}";  //index
        /// <summary>
        /// 解析据点信息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseStrongHold(string msg,PlayerData player)
        {
            string[] data = msg.Split(' ')[1].Split('@');
            int index;
            switch(data[0])
            {
                case "H":
                    index = int.Parse(data[1]);
                    Stronghold[index].camp = player.Camp;
                    Stronghold[index].isHold = true;
                    Stronghold[index].holdTimeStamp = (int)Time;
                    BroadcastMsg(string.Format(format_Stronghold_hold, index, player.Camp, Stronghold[index].holdTimeStamp));
                    break;
                case "C":
                    index = int.Parse(data[1]);
                    Stronghold[index].camp = player.Camp;
                    Stronghold[index].isHold = false;
                    BroadcastMsg(string.Format(format_Stronghold_close, index));
                    break;
            }
        } 

        /// <summary>
        /// 解析获取材料
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseGetMaterial(string msg,PlayerData player)
        {
            ushort index;

            string data = msg.Split(' ')[1];
            index = ushort.Parse(data);

            if(Dic_MaterialBox.ContainsKey(index))
            {
                materialBox t = Dic_MaterialBox[index];
                Dic_MaterialBox.Remove(index);
                //更新背包
                player.UpdateMaterial(t.materialIndex,t.count);
                //广播
                BroadcastMsg(string.Format("BM GetM@{0}:{1}", player.Camp, index));  //参数：玩家，材料箱索引
            }
        }

        /// <summary>
        /// 添加一个材料箱
        /// </summary>
        /// <returns></returns>
        public ushort AddMaterialBox(int materialIndex,int count)
        {
            ushort index = currentIndex_material++;
            materialBox t;
            t.materialIndex = materialIndex;
            t.count = count;
            Dic_MaterialBox.Add(index, t);
            return index;
        }

        const string format_DM = "DM {0}:{1}:{2}:{3}"; //阵营：单位ID：当前DValue：受伤的时间
        /// <summary>
        /// 解析伤害信息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseDamageInfo(string msg,PlayerData player)
        {
            bool isPlayer;
            string target;
            int damage;
            float time;
            int camp;

            string data = msg.Split(' ')[1];
            string[] t = data.Split(':');
            isPlayer = t[0] == "0" ? false : true;
            target = t[1];
            damage = int.Parse(t[2]);
            time = ServerFunction.SendNumber2Float(t[3]);
            camp = int.Parse(t[5]);

            //超时
            if (time + config.MAXTIME_MSG > Time)
                return;

            if(isPlayer)
            {
                Players[int.Parse(target)].D_Value += damage;
            }
            else
            {
                float DValue = UnitDic[camp][target].getDamage(damage, Time);
                //构造广播信息
                string sendMSG = string.Format(format_DM, camp, target, ServerFunction.Float2SendNumber(DValue), Time);
                //广播
                BroadcastMsg(sendMSG);
            }
        }

        /// <summary>
        /// 解析单位行为
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseUnitBehavious(string msg,PlayerData player)
        {
            float time;
            int camp;
            string ID;

            string data = msg.Split(' ')[1];
            string[] t = data.Split('@');

            time = ServerFunction.SendNumber2Float(t[0]);
            camp = int.Parse(t[1]);
            ID = t[2];

            //检查行为是否超时
            if (time + config.MAXTIME_MSG > Time) 
                return;
            else
            {
                //是否合法
                if (!UnitDic[camp][ID].isError)
                {
                    BroadcastMsg(msg);
                }
            }
        }

        const string format_CU = "CU {0}:{1}:{2}:{3}:{4}";
        /// <summary>
        /// 解析单位创造
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="player"></param>
        void ParseCreateUnit(string msg,PlayerData player)
        {
            Vector3 pos;
            string rotation;
            float recoverSpeed;
            float Time_recover;
            int MaxDValue;
            int[] material = new int[3];
            int type;
            int index;

            string[] data = msg.Split(' ')[1].Split(':');

            pos = Vector3.Parse(data[0], ',');
            rotation = data[1];
            recoverSpeed = ServerFunction.SendNumber2Float(data[2]);
            Time_recover = ServerFunction.SendNumber2Float(data[3]);
            MaxDValue = int.Parse(data[4]);
            material[0] = int.Parse(data[5]);
            material[1] = int.Parse(data[6]);
            material[2] = int.Parse(data[7]);
            type = int.Parse(data[8]);

            //是否能创建
            if (!player.isCanCreate(material))
                return;

            index = UnitIndex++;
            UnitInfo t = new UnitInfo(index.ToString(), recoverSpeed, Time_recover, MaxDValue, type, pos, rotation);
            for(int i = 0; i < material.Length; i++)
                player.UpdateMaterial(i, -material[i]);

            //广播
            string MSG = string.Format(format_CU,
                index,
                data[0],
                data[1],
                player.Camp,
                data[8]
                );
            BroadcastMsg(MSG);
        }
    }
}
