using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServe
{
    class Room
    {
        public Client RoomOwner;
        public Client OP_client;
        public string name;
        public bool isReadyGame = false;  //是否准备开始游戏标识符  

        private string index;
        
        public Room(Client CT)
        {
            RoomOwner = CT;
            CT.isStayRoom = true;
            name = CT.socket.RemoteEndPoint.ToString();
            index = ServerFunction.MD5(RoomOwner.socket.RemoteEndPoint.ToString());
        }   
        
        public string getIndex()
        {
            return index;
        }   

        public bool Enter(Client CT)
        {
            if(OP_client == null)
            {
                OP_client = CT;
                CT.isStayRoom = true;

                //玩家足够
                isReadyGame = true;

                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            if(RoomOwner != null)
            {
                RoomOwner.isStayRoom = false;
            }
            if(OP_client != null)
            {
                OP_client.isStayRoom = false;
            }
        }
    }
}
