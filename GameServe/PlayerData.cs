using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Mathf;
namespace GameServe
{
    /// <summary>
    /// 游戏中单个玩家数据
    /// </summary>
    class PlayerData
    {
        //状态
        public bool isConnect = true;
        public bool isGame = true;

        //网络
        public Client client;

        //游戏数据
        public Vector3 Pos = Vector3.zero;
        public Vector3 Dir = Vector3.zero;
        public int Camp;
        public int Score = 0;
    }
}
