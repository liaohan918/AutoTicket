using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    public class ProxyIP
    {
        public string iP { get; set; }
        public int port { get; set; }
        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime expire_time { get; set; }

    }
}
