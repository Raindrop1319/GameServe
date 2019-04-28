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
        private long lastModifyTime_DValue;  //毫秒时间戳
        public float D_Value = 0;
        public int carryScore = 0;
        private float EnergyDropRate = 0.4f;  //能量掉落比率

        //DValue恢复速度
        private float RecoverRate_DValue = 2;
        private int maxDValue = 5;

        //消息队列
        public Queue<string> msgQueue = new Queue<string>();

        const string Format_finalAttack = "finalAttack@{0}:{1}:{2}";  //参数：对象-掉落个数-击退方向(未单位化)

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        public void updateData(string paramName,string value,PlayerData player)
        {
            float data = int.Parse(value);
            switch(paramName)
            {
                case "DValue":
                    lastModifyTime_DValue = ServerFunction.getTimeStamp_milSeconds();
                    //最后一击
                    if(D_Value >= maxDValue && data > 0)
                    {
                        D_Value = 0;
                        string t = getFinalAttack(player);
                        msgQueue.Enqueue(t);
                    }
                    D_Value += data;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 更新
        /// </summary>
        public void Update(float deltaTime)
        {
            long currentStamp = ServerFunction.getTimeStamp_milSeconds();

            //DValue更新
            if(lastModifyTime_DValue + config.MaxRecoverTime_DValue < currentStamp)
            {
                if(D_Value > 0)
                {
                    D_Value -= (deltaTime * RecoverRate_DValue);
                    if (D_Value < 0)
                        D_Value = 0;
                    if (D_Value > maxDValue)
                        D_Value = maxDValue;
                }
            }
        }

        string getFinalAttack(PlayerData player)
        {
            int count = (int)(carryScore * EnergyDropRate);
            Vector3 dir = Pos - player.Pos;
            carryScore -= count;
            return string.Format(Format_finalAttack, Camp, count,dir.ToString());
        }

        public string getMsg()
        {
            string data = "";
            while(msgQueue.Count > 0)
            {
                data += (" " + msgQueue.Dequeue());
            }
            return data;
        }
    }
}
