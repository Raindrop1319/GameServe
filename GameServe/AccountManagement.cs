using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace GameServe
{
    class AccountManagement
    {
        //单例
        private static AccountManagement instance;
        public static AccountManagement getInstance()
        {
            if(instance == null)
            {
                instance = new AccountManagement();
            }
            return instance;
        }

        const string path = "Account.db";
        const string S_register = "insert into Account (nickname,psw) values ('{0}','{1}')";
        const string S_login = "select * from Account where nickname = '{0}' and psw = '{1}'";
        SQLiteConnection conn;

        AccountManagement()
        {
            LinkDB();
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string Register(string msg)
        {
            //  信息：
            //  C：已包含当前ID
            //  S：成功
            string[] t = msg.Split(" ".ToCharArray(), 2)[1].Split(' ');
            string nickname = t[0];
            string psw = t[1];
            if(isContain(nickname))
            {
                return "RG C";
            }
            //成功注册
            SQLiteCommand cmd = new SQLiteCommand(string.Format(S_register, nickname, psw), conn);
            cmd.ExecuteNonQuery();
            return "RG S";
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string Login(string msg)
        {
            //  信息：
            //  F：失败
            //  S nickname：成功
            string[] t = msg.Split(" ".ToCharArray(), 2)[1].Split(' ');
            string nickname = t[0];
            string psw = t[1];
            SQLiteCommand cmd = new SQLiteCommand(string.Format(S_login, nickname, psw),conn);
            SQLiteDataReader DR = cmd.ExecuteReader();
            if(DR.HasRows)
            {
                DR.Read();
                return string.Format("LG S {0}",DR["nickname"]);
            }
            return "LG F";
        }

        /// <summary>
        /// 连接数据库
        /// </summary>
        void LinkDB()
        {
            conn = new SQLiteConnection(string.Format("data source = {0}",path));
            conn.Open();
        }

        /// <summary>
        /// 查询是否已有该ID
        /// </summary>
        /// <param name="nickname"></param>
        /// <returns></returns>
        bool isContain(string nickname)
        {
            string s_cmd = string.Format("select * from Account where nickname = '{0}'", nickname);
            SQLiteCommand cmd = new SQLiteCommand(s_cmd, conn);
            SQLiteDataReader DR = cmd.ExecuteReader();
            return DR.HasRows;
        }
    }
}
