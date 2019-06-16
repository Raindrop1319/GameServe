using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathf
{
    class Vector3
    {
        public int x, y, z;

        static public Vector3 zero = new Vector3(0, 0, 0);

        public Vector3(int x,int y,int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3()
        {
            x = 0;
            y = 0;
            z = 0;
        }

        public static Vector3 operator + (Vector3 a,Vector3 b)
        {
            Vector3 result = new Vector3();

            result.x = a.x + b.x;
            result.y = a.y + b.y;
            result.z = a.z + b.z;

            return result;
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            Vector3 result = new Vector3();

            result.x = a.x - b.x;
            result.y = a.y - b.y;
            result.z = a.z - b.z;

            return result;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2}", x, y, z);
        }

        static public Vector3 Parse(string t,char s)
        {
            string[] d = t.Split(s);
            Vector3 result = new Vector3(int.Parse(d[0]), int.Parse(d[1]), int.Parse(d[2]));
            return result;
        }
    }


    class Vector4
    {
        public int x, y, z,w;

        static public Vector4 zero = new Vector4(0, 0, 0,0);

        public Vector4(int x, int y, int z,int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4()
        {
            x = 0;
            y = 0;
            z = 0;
            w = 0;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3}", x, y, z, w);
        }
    }
}
