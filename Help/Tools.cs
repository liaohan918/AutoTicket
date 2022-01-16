using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    public static class Tools
    {
        public static IEnumerable<int> AllIndexOf(this string str, char value)
        {
            return str.Select((item, index) => new { item, index }).Where(t => t.item == value).Select(t => t.index);
        }

        /// <summary>
        /// 集合随机取数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T GetRandomValue<T>(this List<T> list) 
        {
            if(list == null || list.Count == 0)
                return default(T);
            Random rd = new Random();
            int index = rd.Next(list.Count);
            return list[index];
        }

        /// <summary>
        /// 随机排列数组元素
        /// </summary>
        /// <param name="myList"></param>
        /// <returns></returns>
        public static List<T> ListRandom<T>(List<T> myList)
        {
            List<T> newList = new List<T>();
            if (myList.Count == 2)
            {
                var v = myList.GetRandomValue<T>();
                newList.Add(v);
                myList.Remove(v);
                newList.Add(myList.First());
                return newList;
            }
            Random ran = new Random();
            int index = 0;
            T temp;
            for (int i = 0; i < myList.Count; i++)
            {
                index = ran.Next(0, myList.Count - 1);
                if (index != i)
                {
                    temp = myList[i];
                    myList[i] = myList[index];
                    myList[index] = temp;
                }
            }
            return myList;
        }

        /// <summary>
        /// 从字符串中提取数字
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetIntFromString(this string str)
        {
            return int.Parse(System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9]+", ""));
        }

        public static int[] ToIntArray(this string s, char separator)
        {
            string[] ar = s.Split(separator);
            List<int> ints = new List<int>();
            foreach (var item in ar)
            {
                int v;
                if (int.TryParse(item, out v))
                    ints.Add(v);
            }
            return ints.ToArray();
        }

        /// <summary>
        /// 获取时间戳(秒)
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        /// 获取时间戳（毫秒）
        /// </summary>
        /// <returns></returns>
        public static string GetMillTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }

        
        public static string EncodeURI(string value)
        {
            return UrlEncode(value);
        }

        public static string UrlEncode(string str)
        {
            return UrlEncode(str, System.Text.Encoding.UTF8);
        }
        public static string UrlEncode(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));
        }
        private static byte[] UrlEncodeToBytes(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            byte[] bytes = e.GetBytes(str);
            return UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, false);
        }
        private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count, bool alwaysCreateReturnValue)
        {
            int num = 0;
            int num2 = 0;
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];
                if (ch == ' ')
                {
                    num++;
                }
                else if (!IsSafe(ch))
                {
                    num2++;
                }
            }
            if ((!alwaysCreateReturnValue && (num == 0)) && (num2 == 0))
            {
                return bytes;
            }
            byte[] buffer = new byte[count + (num2 * 2)];
            int num4 = 0;
            for (int j = 0; j < count; j++)
            {
                byte num6 = bytes[offset + j];
                char ch2 = (char)num6;
                if (IsSafe(ch2))
                {
                    buffer[num4++] = num6;
                }
                else if (ch2 == ' ')
                {
                    buffer[num4++] = 43;
                }
                else
                {
                    buffer[num4++] = 37;
                    buffer[num4++] = (byte)IntToHex((num6 >> 4) & 15);
                    buffer[num4++] = (byte)IntToHex(num6 & 15);
                }
            }
            return buffer;
        }
        private static bool IsSafe(char ch)
        {
            if ((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9')))
            {
                return true;
            }
            switch (ch)
            {
                case '\'':
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                case '!':
                    return true;
            }
            return false;
        }
        private static char IntToHex(int n)
        {
            if (n <= 9)
            {
                return (char)(n + 48);
            }
            return (char)((n - 10) + 65);
        }

        public static string DecodeURI(string value)
        {
            return System.Web.HttpUtility.UrlDecode(value, System.Text.Encoding.UTF8);
        }
        
    }
}
