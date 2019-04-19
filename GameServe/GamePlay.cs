﻿using System;
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
        private PlayerData[] Players;
        private const int maxPlayer = 2;

        int rand;
        bool[] isReady = { false, false };
        bool isFinish = false;

        public GamePlay(Room rm)
        {
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
                string data = "PD ";
                for(int i = 0;i<Players.Length;i++)
                {
                    data += (getPlayerData(i));
                    if(i != Players.Length - 1)
                    {
                        data += "#";
                    }
                }

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

                if(j == maxPlayer)
                {
                    break;
                }

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
            //清空数组数据
            Array.Clear(player.client.data, 0, player.client.data.Length);
            if (data.CompareTo("HB") != 0)
            {
                Console.WriteLine("房间内： " + data);
            }
            Parse(data,player);

            if (player.client.isLink)
            {
                player.client.socket.BeginReceive(player.client.data, 0, player.client.data.Length, SocketFlags.None, new AsyncCallback(Callback_Receive), player);
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

        const string format_PDunit = "{0}:{1}:{2}:{3}";
        /// <summary>
        /// 获取玩家数据
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        string getPlayerData(int i)
        {
            PlayerData t = Players[i];

            return string.Format(format_PDunit, t.Camp, t.Pos.ToString(), t.Dir.ToString(), t.Score);
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
        }
    }
}
