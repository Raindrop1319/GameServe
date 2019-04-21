using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServe
{
    class Event_waitSend
    {
        public string eventData;
        public string selfIndex;
        public int index;
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

        private List<int> answerList;

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

            if (a_true > a_false)
                return 1;
            else
                return 0;
        }
    }
}
