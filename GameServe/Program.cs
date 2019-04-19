using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameServe
{
    class Program
    {
        static Socket socket;
        static int maxConnect = 100;
        static public List<Client> ClientList = new List<Client>();  //已连接用户

        const float maxHeartBeatWaitTime = 2;

        static Queue<ReceiveMsg> MsgQueue = new Queue<ReceiveMsg>();

        static void Main(string[] args)
        {
            string[] info = get_IP_Port();
            //string[] info = new string[2];
            //info[0] = "127.0.0.1";
            //info[1] = "10000";
            
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress IP = IPAddress.Parse(info[0]);
            IPEndPoint IEP = new IPEndPoint(IP, int.Parse(info[1]));
            socket.Bind(IEP);
            socket.Listen(maxConnect);
            Console.WriteLine(string.Format("服务器启动成功_{0}:{1}",info[0],info[1]));
            socket.BeginAccept(Callback_Accpet,socket);
            AccountManagement.getInstance();
            RoomManagement.getInstance();

            Thread heartBeat = new Thread(checkClient);
            heartBeat.Start();
            Thread thread_getMsg = new Thread(getMsgQueue);
            thread_getMsg.Start();
            while(true)
            {
                Console.Read();
            }
        }

        /// <summary>
        /// 初始化获取
        /// </summary>
        /// <returns></returns>
        static string[] get_IP_Port()
        {
            string info;
            info = Console.ReadLine();
            string[] result = new string[2];
            result = info.Split(" ".ToCharArray(), 2);
            return result;
        }

        /// <summary>
        /// 接收连接回调
        /// </summary>
        /// <param name="ar"></param>
        static void Callback_Accpet(IAsyncResult ar)
        {
            Socket t_socket = (Socket)ar.AsyncState;
            Client new_Client = new Client();

            new_Client.isLink = true;
            new_Client.socket = t_socket.EndAccept(ar);
            new_Client.lastTick = ServerFunction.getTimeStamp();

            Console.WriteLine(new_Client.socket.RemoteEndPoint.ToString() + " 连接");
            new_Client.socket.BeginReceive(new_Client.data, 0, new_Client.data.Length, SocketFlags.None, new AsyncCallback(Callback_Receive), new_Client);
            ClientList.Add(new_Client);
            t_socket.BeginAccept(Callback_Accpet, t_socket);
        }

        /// <summary>
        /// 接收数据回调
        /// 不接收游戏中数据
        /// </summary>
        /// <param name="ar"></param>
        static void Callback_Receive(IAsyncResult ar)
        {

            Client t_client = (Client)ar.AsyncState;

            try
            {
                int t = t_client.socket.EndReceive(ar);
                if(t == 0)
                {
                    CloseClient(t_client);
                    return;
                }
            }
            catch
            {
                CloseClient(t_client);
                return;
            }

            string data = System.Text.Encoding.UTF8.GetString(t_client.data).TrimEnd('\0');
            ReceiveMsg msg;
            msg.client = t_client;
            msg.msg = data;
            MsgQueue.Enqueue(msg);

            //清空数组数据
            Array.Clear(t_client.data, 0, t_client.data.Length);

            if (data.CompareTo("HB") != 0)
            {
                Console.WriteLine(data);
            }

            if (t_client.isLink && !t_client.isGame)
            {
                t_client.socket.BeginReceive(t_client.data, 0, t_client.data.Length, SocketFlags.None, new AsyncCallback(Callback_Receive), t_client);
            }
        }

        /// <summary>
        /// 解析客户端数据
        /// </summary>
        /// <param name="s_data"></param>
        static void ParseClientData(Client CT,string msg)
        {
            string data = msg;
            string type;
            try
            {
                type = data.Substring(0, 2);
            }
            catch
            {
                type = "";
            }
            string result = "";
            switch(type)
            {
                //注册
                case "RG":
                    result = AccountManagement.getInstance().Register(data);
                    CT.Send(result);
                    break;
                //登录
                case "LG":
                    result = AccountManagement.getInstance().Login(data);
                    if(result.Split(' ')[1].CompareTo("S") == 0)
                    {
                        CT.isLogin = true;
                    }
                    CT.Send(result);
                    break;
                //创建房间
                case "CR":
                    RoomManagement.getInstance().Parse(CT, data);
                    break;
                //进入房间
                case "ER":
                    RoomManagement.getInstance().Parse(CT, data);
                    break;
                //心跳
                case "HB":
                    CT.updateTick(ServerFunction.getTimeStamp());
                    break;
                case "":
                    CT.isLink = false;
                    break;
                default:
                    break;
            }
 
        }

        /// <summary>
        /// 检查客户端心跳
        /// </summary>
        static void checkClient()
        {
            while (true)
            {
                for (int i = ClientList.Count - 1; i >= 0; i--)
                {
                    if (i >= ClientList.Count)
                    {
                        continue;
                    }

                    long t = ClientList[i].lastTick;
                    //超时
                    if (ServerFunction.getTimeStamp() - t > maxHeartBeatWaitTime || !ClientList[i].isLink)
                    {
                        CloseClient(ClientList[i]);
                    }
                }
                //Console.WriteLine("已连接：" + ClientList.Count);
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 关闭当前客户端
        /// </summary>
        /// <param name="CT"></param>
        static void CloseClient(Client CT)
        {
            Console.WriteLine(CT.socket.RemoteEndPoint.ToString() + "  已断线");
            CT.Close();
            ClientList.Remove(CT);
        }

        /// <summary>
        /// 取消息区
        /// </summary>
        static void getMsgQueue()
        {
            while(true)
            {
                try
                {
                    if(MsgQueue.Count > 0)
                    {
                        ReceiveMsg t = MsgQueue.Dequeue();
                        if (t.client.isLink)
                        {
                            //解析
                            ParseClientData(t.client,t.msg);
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Eor");
                }
                Thread.Sleep(50);
            }
        }
    }
}
