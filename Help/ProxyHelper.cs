using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    /// <summary>
    /// 芝麻代理IP
    /// </summary>
    public class ProxyHelper
    {

        public static List<ProxyIP> GetProxy(int count)
        {
            var FreeProxyIpUrl = "http://http.tiqu.letecs.com/getip3?num="+ count + "&type=2&pro=&city=0&yys=100017&port=1&time=1&ts=1&ys=0&cs=0&lb=1&sb=0&pb=45&mr=2&regions=&gm=4";
            var resp = JsonHelper.JsonToObj<RespModel<List<ProxyIP>>>(HttpRequest.GetAsync(FreeProxyIpUrl).Result);

            return resp?.data;
        }

        public static ProxyIP GetProxy()
        {           
            return GetProxy(1).FirstOrDefault();
        }

    }
}
