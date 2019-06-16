using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GameServe
{
    class ServerFunction
    {
        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static long getTimeStamp()
        {
            TimeSpan TS = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(TS.TotalSeconds);
        }

        /// <summary>
        /// 获取时间戳(毫秒)
        /// </summary>
        /// <returns></returns>
        public static long getTimeStamp_milSeconds()
        {
            TimeSpan TS = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(TS.TotalMilliseconds);
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        static public string MD5(string t)
        {
            byte[] result = Encoding.Default.GetBytes(t);
            MD5 md5 = new MD5CryptoServiceProvider();
            return System.BitConverter.ToString(md5.ComputeHash(result)).Replace("-", "");
        }

        const char devideSign = '*';
        /// <summary>
        /// 分包（发生粘包）
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        static public string[] DevideMsg(string msg)
        {
            int count = getStringCount(msg, devideSign);
            //是否有残包
            if(count%2 == 0)
            {
                int msgCount = count / 2;
                string[] result = new string[msgCount];
                int t = 0;
                int start = 0;
                int currentCount = 0;
                for(int i = 0;i<msg.Length;i++)
                {
                    if(msg[i].CompareTo(devideSign) == 0)
                    {
                        t++;
                        if(t == 1)
                        {
                            start = i + 1;
                        }
                        if(t == 2)
                        {
                            t = 0;
                            result[currentCount++] = msg.Substring(start, i - start);
                        }
                    }

                }
                return result;
            }
            else
            {
                //有残包扔出异常
                throw new Exception();
            }
        }

        /// <summary>
        /// 指定字符串中指定字符个数
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        static public int getStringCount(string s,char c)
        {
            int length = s.Length;
            int count = 0;
            for(int i = 0;i<length;i++)
            {
                if(s[i].CompareTo(c) == 0)
                {
                    count++;
                }
            }
            return count;
        }

        static public float SendNumber2Float(string num)
        {
            int t = int.Parse(num);
            return t / config.precision;
        }

        static public float Float2SendNumber(float num)
        {
            return (float)Math.Floor(num * config.precision);
        }
    }
}
