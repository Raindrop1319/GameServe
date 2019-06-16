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

        GameplayManagement()
        {
            Thread thread_Update = new Thread(Update);
            thread_Update.IsBackground = true;
            thread_Update.Start();
        }

        List<GamePlay> GameplayList = new List<GamePlay>();

        public void startGame(Room rm)
        {
            GamePlay t = new GamePlay(rm);
            GameplayList.Add(t);
        }

        /// <summary>
        /// 更新
        /// </summary>
        void Update()
        {
            while(true)
            {
                //检查有无结束游戏的房间
                if (GameplayList.Count > 0)
                {
                    for (int i = GameplayList.Count - 1; i >= 0; i--)
                    {
                        GamePlay t = GameplayList[i];
                        if (t.isFinish)
                        {
                            GameplayList.Remove(t);
                            Console.WriteLine(t.name + "已删除");
                        }
                    }
                }

                Thread.Sleep(500);
            }
        }
    }
}
