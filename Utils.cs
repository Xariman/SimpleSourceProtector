using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleSourceProtector
{
    public static class Utils
    {
        private static Random m_rand = new Random((int)DateTime.Now.Ticks);
        public static Random Random { get { return m_rand; } }
        private const string m_chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static HashSet<string> m_generatedList = new HashSet<string>();

        public static string RandomString(int minSize = 1, int maxSize = 8, int minAddNums = 0, int maxAddNums = 5)
        {
            var size = m_rand.Next(minSize, maxSize);
            var nums = m_rand.Next(minAddNums, maxAddNums);

            char[] buffer = new char[size + nums];

            string str;
            do
            {
                for (int i = 0; i < size + nums; i++)
                    buffer[i] = (i < size) ? m_chars[m_rand.Next(m_chars.Length)] : (char)m_rand.Next('0', '9' + 1);
                str = new string(buffer);
            } while (m_generatedList.Contains(str));

            m_generatedList.Add(str);

            return str;
        }

        public static string Unformat(string str)
        {
            var list = str.Replace(@"\""", "&pqute;").Split('"');
            for (int i = 0; i < list.Length; i++)
            {
                if (i % 2 != 0)
                    continue;
                list[i] = Regex.Replace(list[i], @"\s+", " ", RegexOptions.Compiled);
                list[i] = Regex.Replace(list[i], @"\s*([\+\-\=\*\/\|\&\!\%\,\<\>\{\}\;\[\]\(\)]+)\s*", (Match match) => match.Groups[1].Value, RegexOptions.Compiled);
            }

            return string.Join("\"", list).Replace("&pqute;", @"\""");
        }

    }

    public static class IListExtensions
    {
        public static void Shuffle<T>(this IList<T> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = Utils.Random.Next(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
    }

}
