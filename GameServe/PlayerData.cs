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
        public bool isEnd = false;
        public bool isLeftGameRoom = false;

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
        public bool isCraft_Vertical = false;
        public bool isCraft_Horizontal = false;
        public int KillCount = 0;
        private int[] material = { 0,0,0};

        //DValue恢复速度
        private float RecoverRate_DValue = 2;
        private int maxDValue = 5;

        //消息队列
        public Queue<string> msgQueue = new Queue<string>();

        public GamePlay gameplayRoom = null;

        const string Format_finalAttack = "finalAttack@{0}:{1}:{2}";  //参数：对象-掉落个数-击退方向(未单位化)

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        public void updateData(string paramName,string value,PlayerData player)
        {
            switch(paramName)
            {
                case "DValue":
                    int data = int.Parse(value);
                    lastModifyTime_DValue = ServerFunction.getTimeStamp_milSeconds();
                    //最后一击
                    if(D_Value >= maxDValue && data > 0)
                    {
                        D_Value = 0.5f * maxDValue;
                        string t = getFinalAttack(player);
                        msgQueue.Enqueue(t);
                    }
                    D_Value += data;
                    break;
                case "Score":
                    int data2 = int.Parse(value);
                    //产生消息
                    string t2 = getSacrifice(this, carryScore, data2);
                    msgQueue.Enqueue(t2);
                    //完成一次献祭
                    Score += carryScore;
                    carryScore = 0;
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

        Random rd = new Random();
        string Format_Sacri = "Sacri@{0}:{1}:{2}:{3}:{4}:{5}";  //阵营：数量：献祭塔编号:奖赏材料:数量:索引
        string getSacrifice(PlayerData player,int count,int TowerIndex)
        {
            int materialIndex = -1;
            int materialNum = 0;
            int index = -1;
            if(count >= config.MAXCOUNT_SACRIFICE)
            {
                materialIndex = rd.Next(0, 4);
                materialNum = rd.Next(1, 2 + count * 2);
                index = gameplayRoom.AddMaterialBox(materialIndex,count);
            }
            return string.Format(Format_Sacri, player.Camp, count, TowerIndex, materialIndex, materialNum, index);
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

        string format_backpack = "BP {0}:{1}:{2}";
        /// <summary>
        /// 更新材料
        /// </summary>
        /// <param name="m"></param>
        public void UpdateMaterial(int mIndex,int delta)
        {
            material[mIndex] += delta;
            //通知客户端
            client.Send(string.Format(format_backpack, material[0], material[1], material[2]));
        }

        /// <summary>
        /// 是否能够创建
        /// </summary>
        /// <param name="materials"></param>
        /// <returns></returns>
        public bool isCanCreate(int[] materials)
        {
            for(int i = 0;i<materials.Length;i++)
            {
                if (materials[i] > material[i])
                    return false;
            }
            return true;
        }
    }
}
