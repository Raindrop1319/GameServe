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
    }
}
