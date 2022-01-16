using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    /// <summary>
    /// 演出信息
    /// </summary>
    public class PerformResp
    {
        public ItemBasicInfo itemBasicInfo { get; set; }
        public PerformInfo perform { get; set; }
        public PerformCalender performCalendar { get; set; }
    }

    public class ItemBasicInfo
    {
        public string nationalStandardCityId { get; set; }
        public string categoryId { get; set; }
        public string itemId { get; set; }
        public string projectId { get; set; }
        public string projectStatus { get; set; }
        public string sellingStartTime { get; set; }
    }

    public class PerformInfo
    {
        public bool buyPermission { get; set; }
        public bool performSalable { get; set; }
        public string performName { get; set; }
        public string performId { get; set; }
        public List<Sku> skuList { get; set; }
    }

    public class Sku
    {
        public string itemId { get; set; }
        public string priceId { get; set; }
        public string skuId { get; set; }
        /// <summary>
        /// 是否有票
        /// </summary>
        public bool skuSalable { get; set; }
        public string priceName { get; set; }
    }

    public class PerformCalender
    {
        public bool showDates { get; set; }

        public List<PerformView> performViews { get; set; }
        public List<DateView> dateViews { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    public class PerformView
    {
        public bool buyPermission { get; set; }
        public bool Checked { get; set; }
        public bool clickable { get; set; }
        public bool salable { get; set; }
        public string performId { get; set; }
    }

    public class DateView
    {
        public string dateId { get; set; }
        public bool buyPermission { get; set; }
        public bool clickable { get; set; }

        public bool Checked { get; set; }
    }
}
