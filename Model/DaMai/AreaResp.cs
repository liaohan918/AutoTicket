using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket.Model.DaMai
{
    public class AreaResp
    {
        public SeatQuYu seatQuYu { get; set; }
    }


    public class SeatQuYu
    {
        public string areaStatusGroup { get; set; }
        /// <summary>
        /// 当区域有多个时，截取该值字符串获取分区内座位Id
        /// </summary>
        public string resourcesPath { get; set; }

        public string ver { get; set; }
        public List<QuYu> quyu { get; set; }
    }

    public class QuYu
    {
        /// <summary>
        /// 对应areagroupId
        /// </summary>
        public string vid { get; set; }
        /// <summary>
        /// 和vid相同
        /// </summary>
        public string i { get; set; }
        /// <summary>
        /// 对应priceid
        /// </summary>
        public string j { get; set; }
    }

    public class Result
    {
        public string result { get; set; }
    }

    public class SellSeats
    {
        public List<SellSeat> seat { get; set; }
    }

    public class SellSeat
    {
        public string s { get; set; }
        public string sid { get; set; }
    }

    public class Seats
    {
        public List<Seat> seats { get; set; }

        public string stand { get; set; }
    }

    public class Seat
    {
        /// <summary>
        /// priceId
        /// </summary>
        public string plid { get; set; }

        public string sid { get; set; }
        /// <summary>
        /// 第几层
        /// </summary>
        public string chint { get; set; }
        /// <summary>
        /// 第几排
        /// </summary>
        public string rhint { get; set; }
        /// <summary>
        /// 第几号
        /// </summary>
        public string shint { get; set; }
    }

    public class SeatStatus
    {
        public string seatStatus { get; set; }
    }

    public class Dynamicinfo
    {
        public List<StandColor> standColorList { get; set; }
    }

    public class StandColor
    {
        public int standId { get; set; }
    }


}
