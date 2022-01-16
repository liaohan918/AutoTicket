using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    public class TicketResp
    {
        /// <summary>
        /// 开场日次序
        /// </summary>
        public int Date { get; set; }
        /// <summary>
        /// 票档
        /// </summary>
        public int PriceIndex { get; set; } 
        /// <summary>
        /// 剩余票数
        /// </summary>
        public int ReservedCount { get; set; } = 0;
        /// <summary>
        /// 接口返回
        /// </summary>
        public string ShowId { get; set; }
        /// <summary>
        /// 接口返回
        /// </summary>
        public string SectionId { get; set; }  
        /// <summary>
        /// 接口返回
        /// </summary>
        public string DateTime { get; set; }
        /// <summary>
        /// 接口返回
        /// </summary>
        public string Price { get; set; }
        /// <summary>
        /// 接口返回
        /// </summary>
        public int PriceId { get; set; }
    }

}
