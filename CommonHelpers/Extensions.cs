using RTSSSharedMemoryNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonHelpers
{
    public static class Extensions
    {
        public static string JoinWith0(this IEnumerable<string> list)
        {
            return string.Join('\0', list);
        }

        public static string JoinWith0<TSource>(this IEnumerable<TSource> list, Func<TSource, string> selector)
        {
            return list.Select(selector).JoinWith0();
        }

        public static string[] SplitWith0(this string str)
        {
            return str.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }

        public static string JoinWithN(this IEnumerable<string> list)
        {
            return string.Join('\n', list);
        }

        public static string JoinWithN<TSource>(this IEnumerable<TSource> list, Func<TSource, string> selector)
        {
            return list.Select(selector).JoinWithN();
        }

        public static string[] SplitWithN(this string str)
        {
            return str.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
