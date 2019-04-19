using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace GameServe
{
    class Client
    {
        public Socket socket = null;
        public long lastTick = 0;
        public byte[] data = new byte[1024];
        public bool isLink = false;
        public bool isLogin = false;
        public bool isStayRoom = false;
        public bool isGame = false;

        /// <summary>
        /// 向客户端发送消息
        /// </summary>
        /// <param name="s_data"></param>
        /// <returns>是否成功发送</returns>
        public bool Send(string s_data)
        {
            try
            {
                socket.Send(System.Text.Encoding.UTF8.GetBytes(s_data));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void updateTick(long tick)
        {
            lastTick = tick;
        }

        /// <summary>
        /// 关闭当前客户端
        /// </summary>
        public void Close()
        {
            isLink = false;
            isLogin = false;
            isGame = false;
            socket.Close();
        }
    }
}
