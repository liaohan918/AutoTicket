using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket.Model.Polyt
{
    /// <summary>
    /// 保利请求必填参数
    /// </summary>
    public class PolytReqModel
    {
        public string applicationCode { get; } = "plat_pc";
        public string applicationSource { get; } = "plat_pc";
        public string atgc { get; }
        public int current { get; } = 1;
        public int size { get; } = 10;
        public long timestamp { get; } = long.Parse(Tools.GetMillTimeStamp());
        public string utgc { get; } = "utoken";

    }

    public class TicketOrder
    {
        public int count { get; set; }
        public int freeTicketCount { get; set; }
        public int priceId { get; set; }
        public int seat { get; set; }
    }

    public class SellSeatsId
    {
        public List<int> sellSeatIds { get; set; }
    }

    public class SeatsInfo
    {
        public List<Price> priceGradeList;

        public List<Seat> seatList;
    }

    public class Price
    {
        public double p { get; set; }
        public int tp { get; set; }
    }

    public class Seat
    {
        public string s { get; set; }
        public string sf { get; set; }
        /// <summary>
        /// seatId
        /// </summary>
        public int sd { get; set; }
        /// <summary>
        /// 与PriceId
        /// </summary>
        public int p { get; set; }
    }
}
