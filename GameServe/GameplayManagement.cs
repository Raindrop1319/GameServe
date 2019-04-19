using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GameServe
{
    class GameplayManagement
    {
        //单例
        private static GameplayManagement instance;
        public static GameplayManagement getInstance()
        {
            if (instance == null)
            {
                instance = new GameplayManagement();
            }
            return instance;
        }

        List<GamePlay> GameplayList = new List<GamePlay>();

        public void startGame(Room rm)
        {
            GamePlay t = new GamePlay(rm);
            GameplayList.Add(t);
        }
    }
}
