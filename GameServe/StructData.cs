using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mathf;

namespace GameServe
{
    class Event_waitSend
    {
        public string eventData;
        public string selfIndex;
        public int index = 0;
        public int camp;
        public Client client;

        public Event_waitSend(string ed,string sI,int index,int c,Client ct)
        {
            eventData = ed;
            selfIndex = sI;
            this.index = index;
            camp = c;
            client = ct;
        }

        const string format_ts = " {0}@{1}@{2}";
        public override string ToString()
        {
            return string.Format(format_ts, index, camp, eventData);
        }
    }

    class Event_waitAnswer
    {
        public Event_waitSend data = null;
        public long endTime = 0;

        private List<int> answerList = new List<int>();

        public void AddAnswer(int a)
        {
            answerList.Add(a);
        }

        public int getAnswerNum()
        {
            return answerList.Count;
        }

        public int Answer()
        {
            int a_false = 0;
            int a_true = 0;
            for(int i = answerList.Count - 1;i>=0;i--)
            {
                int t = answerList[i];
                if (t == 0)
                    a_false++;
                if (t == 1)
                    a_true++;
            }

            if (a_true >= a_false)
                return 1;
            else
                return 0;
        }
    }

    /// <summary>
    /// 据点
    /// </summary>
    class StrongholdInfo
    {
        public int camp = -1;
        public bool isHold = false;
        public int holdTimeStamp = 0;
    }

    struct materialBox
    {
        public int materialIndex;
        public int count;
    }

    class UnitInfo
    {
        public Vector3 pos;
        public string rotation;

        public string Index;
        public float DValue = 0;
        public float recoverSpeed;
        public float Time_recover;
        public int MaxDValue;
        public int type;
        public bool isError = false;

        private float lastDamageTime;

        //构造
        public UnitInfo(string index,float recoverSpeed,float Time_recover,int maxDValue,int type,Vector3 pos,string rotation)
        {
            Index = index;
            this.recoverSpeed = recoverSpeed;
            this.Time_recover = Time_recover;
            this.MaxDValue = maxDValue;
            this.type = type;
            this.pos = pos;
            this.rotation = rotation;
        }

        //更新DValue
        public void Update(float currentTime,float deltaTime)
        {
            if (!isError)
            {
                if (currentTime > lastDamageTime + Time_recover)
                {
                    DValue -= (deltaTime * recoverSpeed);
                    if (DValue < 0)
                        DValue = 0;
                }
            }
        }

        //受伤
        public float getDamage(int damage,float currentTime)
        {
            lastDamageTime = currentTime;
            DValue += damage;
            if(DValue > MaxDValue)
            {
                isError = true;
            }

            return DValue;
        }
    }
}
