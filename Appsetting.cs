using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    public static class Appsetting
    {
        public static IConfigurationRoot _root = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("Appsetting.json").Build();

        public static string TicketWeb => Get("TicketWeb");

        public static string Get(string key)        
        {            
            if (_root.GetSection(key) != null)
                 return _root.GetSection(key).Value;
            else
  
            return string.Empty;
        }

        public static string Get(params string[] sections)
        {
            try
            {

                if (sections.Any())
                {
                    return _root[string.Join(":", sections)];
                }
            }
            catch (Exception) { }

            return "";
        }

        /// <summary>
        /// 获取浏览器头部高度
        /// </summary>
        /// <returns></returns>
        public static int GetChromeHeaderHeight()
        {
            return int.Parse(Get("ChromeHeaderHeight"));
        }

        /// <summary>
        /// 获取观影人
        /// </summary>
        /// <returns></returns>
        public static IList<string> GetWatchers()
        {
            return Get("PeopleName").Split(',').ToList();
        }

        /// <summary>
        /// 获取抢票日期
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<int> GetDate()
        {
            var date = Get("Date").ToIntArray(',').ToList<int>();

            return Tools.ListRandom(date);
        }

        /// <summary>
        /// 获取场次
        /// </summary>
        /// <returns></returns>
        public static IList<int> GetPositionIndex()
        {
            var position = Get("PositionIndex").ToIntArray(',').ToList<int>();

            return Tools.ListRandom(position);
        }

        /// <summary>
        /// 获取票档
        /// </summary>
        /// <returns></returns>
        public static IList<int> GetPriceIndex()
        {
            var position = Get("PriceIndex").ToIntArray(',').ToList<int>();

            return Tools.ListRandom(position);
        }

        /// <summary>
        /// 抢票规则
        /// </summary>
        /// <returns></returns>
        public static int GetPriorityRule()
        {
            return int.Parse(Get("PriorityRule"));
        }

        /// <summary>
        /// 抢票页面
        /// </summary>
        /// <returns></returns>
        public static string GetTargetUrl()
        {
            return Get("Web", Appsetting.TicketWeb, "TargetUrl");
        }

        public static string GetCookie()
        {
            return Get("Web", Appsetting.TicketWeb, "Cookie");
        }
    }
}
