using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GameServe
{
    class RoomManagement
    {
        //单例
        private static RoomManagement instance;
        public static RoomManagement getInstance()
        {
            if(instance == null)
            {
                instance = new RoomManagement();
            }
            return instance;
        }

        List<Room> RoomList = new List<Room>();
        Mutex lock_roomList = new Mutex();  //变量互斥量

        /// <summary>
        /// 构造
        /// </summary>
        RoomManagement()
        {
            Thread thread_updateRoomList = new Thread(updateRoomList);
            thread_updateRoomList.IsBackground = true;
            thread_updateRoomList.Start();

            Thread thread_sendRoomList = new Thread(SendRoomList);
            thread_sendRoomList.IsBackground = true;
            thread_sendRoomList.Start();
        }

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="CT"></param>
        /// <param name="msg"></param>
        public void Parse(Client CT,string msg)
        {
            string type = msg.Substring(0, 2);
            switch(type)
            {
                case "CR":
                    ParseCreateRoom(CT, msg);
                    break;
                case "ER":
                    ParseEnterRoom(CT, msg);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 解析进入房间
        /// </summary>
        /// <param name="CT"></param>
        /// <param name="msg"></param>
        void ParseEnterRoom(Client CT,string msg)
        {
            if(!CT.isStayRoom)
            {
                string index = msg.Split(' ')[1];

                //查找
                for(int i = 0;i<RoomList.Count;i++)
                {
                    Room t = RoomList[i];
                    if(index.CompareTo(t.getIndex()) == 0)
                    {
                        if (t.Enter(CT))
                        {
                            //加入成功
                            CT.Send("ER S");
                            return;
                        }
                        break;
                    }
                }
            }

            //加入失败
            CT.Send("ER F");
        }

        /// <summary>
        /// 解析创建房间
        /// </summary>
        /// <param name="CT"></param>
        /// <param name="msg"></param>
        void ParseCreateRoom(Client CT,string msg)
        {
            //已在房间在不可创房
            if (!CT.isStayRoom)
            {
                Room newRoom = new Room(CT);
                RoomList.Add(newRoom);
                CT.Send(string.Format("CR S {0} {1}",newRoom.name,newRoom.getIndex()));
            }
            else
            {
                CT.Send("CR F");
            }
        }

        /// <summary>
        /// 更新房间列表
        /// </summary>
        void updateRoomList()
        {
            while(true)
            {
                lock_roomList.WaitOne();
                for(int i = RoomList.Count - 1;i>=0;i--)
                {
                    Room t = RoomList[i];
                    if(t.RoomOwner.isGame || !t.RoomOwner.isLink || !t.RoomOwner.isLogin || !t.RoomOwner.isStayRoom)
                    {
                        t.Clear();
                        RoomList.Remove(t);
                        Console.WriteLine(t.name + " 被移除");
                    }

                    //检查有无可以开始的房间
                    if(t.isReadyGame)
                    {
                        GameplayManagement.getInstance().startGame(t);
                        t.Clear();
                        RoomList.Remove(t);
                        Console.WriteLine(t.name + " 开始游戏");
                    }
                }
                lock_roomList.ReleaseMutex();
                Thread.Sleep(50);
            }
        }

        const string format_RoomUnit = "{0}:{1} ";
        const string format_RL = "RL {0}#{1}";
        /// <summary>
        /// 向客户端给下发房间信息
        /// </summary>
        void SendRoomList()
        {
            string data = "";
            while(true)
            {
                lock_roomList.WaitOne();

                data = "";
                for(int i = 0;i<RoomList.Count;i++)
                {
                    Room t = RoomList[i];
                    data += string.Format(format_RoomUnit, t.name, t.getIndex());
                }
                data = string.Format(format_RL, RoomList.Count, data);
                lock_roomList.ReleaseMutex();

                int count = Program.ClientList.Count;
                for (int i = 0;i<count;i++)
                {
                    if (Program.ClientList[i].isLogin && !Program.ClientList[i].isGame)
                    {
                        Program.ClientList[i].Send(data);
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}
