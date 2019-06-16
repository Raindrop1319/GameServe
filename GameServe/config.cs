using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServe
{
    class config
    {
        //数据精度
        public static int precision = 10000;
        //DValue恢复时间(毫秒)
        public static int MaxRecoverTime_DValue = 5000;
        //据点数量
        public static int MAXCOUNT_STRONGHOLD = 5;
        //获得材料所需能量数
        public static int MAXCOUNT_SACRIFICE = 2;
        //最大消息等待时间
        public static float MAXTIME_MSG = 2;
    }
}
