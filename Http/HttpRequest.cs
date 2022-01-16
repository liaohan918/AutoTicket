using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    public class HttpRequest
    {
        public static async Task<string> GetAsync(string serviceAddress, Dictionary<string, string> headers = null, IWebProxy proxy = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.Headers.Add("Cookie", Appsetting.GetCookie());
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Safari/537.36");
            //request.Timeout = 10 * 1000;
            if (headers is not null)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    request.Headers.Add(kvp.Key, kvp.Value);
                }
            }
            if (proxy is not null)
            {
                request.Proxy = proxy;
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = await myStreamReader.ReadToEndAsync();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        public static async Task<string> PostAsync(string serviceAddress, string strContent = null, Dictionary<string, string> headers = null, IWebProxy proxy = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Cookie", Appsetting.GetCookie());
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Safari/537.36");
            //request.Timeout = 10 * 1000;
            if (headers is not null)
            {
                foreach(KeyValuePair<string, string> kvp in headers)
                {
                    request.Headers.Add(kvp.Key, kvp.Value);
                }
            }
            if (proxy is not null)
            {
                request.Proxy = proxy;
            }
            //判断有无POST内容
            if (!string.IsNullOrWhiteSpace(strContent))
            {
                using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
                {
                    dataStream.Write(strContent);
                    dataStream.Close();
                }
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string encoding = response.ContentEncoding;
            if (string.IsNullOrWhiteSpace(encoding))
            {
                encoding = "UTF-8"; //默认编码  
            }
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
            string retString = await reader.ReadToEndAsync();
            return retString;
        }

    }
}
