using AutoTicket.Model.Polyt;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoTicket
{
    public class Polyt : BaseTicket
    {
        private const string checkLoginUrl = "https://platformpcgateway.polyt.cn/api/1.0/login/getLoginUser";

        private const string showInfoDetailUrl = "https://platformpcgateway.polyt.cn/api/1.0/show/getShowInfoDetail";

        private const string sellSeatListUrl = "https://platformpcgateway.polyt.cn/api/1.0/seat/getSellSeatList";

        private const string commitOrder = "https://platformpcgateway.polyt.cn/api/1.0/platformOrder/commitOrderOnSeat";

        private const string createOrder = "https://platformpcgateway.polyt.cn/api/1.0/platformOrder/createOrder";

        #region 跳转至选座页面必填参数

        private readonly string projectId;
                
        private readonly string productId;
                
        private readonly string theaterId;

        #endregion

        public Polyt()
        {
            var targetUrl = Appsetting.GetTargetUrl();
            var infoArray = targetUrl.Replace(targetUrl.Remove(targetUrl.IndexOf("show")), "").Split("/");
            theaterId = infoArray[2];
            productId = infoArray[3];
            projectId = infoArray[1];
        }


        public override void Start()
        {
            base.Start();
            if (!VerifyCookie())
            {
                Console.WriteLine("Cookie失效或已过期,请在Appsetting.json填写正确Cookie并重新启动");
                return;
            }
            Task.Run(() => UltraTicket(null));
            var taskCount = int.Parse(Appsetting.Get("TaskCount"));
            var proxyIPs = ProxyHelper.GetProxy(taskCount);
            for (var i = 0; i < taskCount; i++)
            {
                var index = i;
                Task.Run(() => UltraTicket(proxyIPs[index]));
            }
        }

        /// <summary>
        /// 超级抢票
        /// </summary>
        private async void UltraTicket(ProxyIP proxyIP)
        {
            IWebProxy proxy = null;
            DateTime expireTime = new DateTime(9999,12, 30);
            if (proxyIP is not null)
            {
                proxy = new WebProxy(proxyIP.iP, proxyIP.port);
                expireTime = proxyIP.expire_time;
            }
            TicketResp targetTicket = null;
            List<int> sellSeats;//可售座位
            SeatsInfo allSeats;//座位信息
            int targetSeatId = 0;//目标座位Id
            List<int> priceChoose;
            List<int> dayChoose;
            while (_status == 1)
            {
                if(DateTime.Now < showStartTime)
                {
                    Thread.Sleep(1);
                    continue;
                }
                try
                {

                    dayChoose = Appsetting.GetPositionIndex().ToList();//抢的日期下标标
                    priceChoose = Appsetting.GetPriceIndex().ToList();//抢的票档
                    targetTicket = await ChoosePriceAsync(proxy, dayChoose, priceChoose);
                    if (targetTicket is not null)
                    {
                        sellSeats = await GetSellSeats(targetTicket.SectionId, targetTicket.ShowId, proxy);
                        if (sellSeats?.Count > 0)
                        {
                            allSeats = await GetSeatsInfoAsync(proxy, targetTicket.ShowId, targetTicket.SectionId);
                            if (allSeats is not null)
                            {
                                targetSeatId = GetTargetSeatId(allSeats, targetTicket.PriceId, sellSeats);
                                if (targetSeatId != 0)
                                {
                                    var newOrder = await CreateOrder(proxy, targetTicket, targetSeatId);
                                    if (newOrder.code == 200 && !string.IsNullOrWhiteSpace(newOrder.data))
                                    {
                                        var commitOrder = await Pay(proxy, newOrder.data);
                                        if (commitOrder.code == 200)
                                        {
                                            Console.WriteLine("抢票成功,请前往App我的订单查看并支付");
                                            Stop();
                                            break;
                                        }
                                        else if (commitOrder.msg.Contains("待支付"))
                                        {

                                            Console.WriteLine(commitOrder.msg);
                                            Stop();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if((expireTime - DateTime.Now).TotalSeconds <= 30)
                    {
                        var newProxy = ProxyHelper.GetProxy();
                        proxy = new WebProxy(newProxy.iP, newProxy.port);
                        expireTime = newProxy.expire_time;
                    }
                    Console.WriteLine($"第{TryCount}次抢票");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    if (e.Message.Contains("405"))
                    {
                        var newProxy = ProxyHelper.GetProxy();
                        proxy = new WebProxy(newProxy.iP, newProxy.port);
                        expireTime = newProxy.expire_time;
                    }                    
                }
                finally
                {
                    Thread.Sleep(1500);
                    TryCount++;
                }

            }
            
        }

        /// <summary>
        /// 创建订单
        /// </summary>
        /// <returns></returns>
        public async Task<RespModel<string>> CreateOrder(IWebProxy proxy, TicketResp targetTicket, int targetSeatId)
        {
            var reqMode = new
            {
                channelId = "",
                projectId = this.projectId,
                seriesId = "",
                showId = targetTicket.ShowId,
                showTime = targetTicket.DateTime,
                priceList = new TicketOrder[]{ new TicketOrder()
                {
                    count = 1,
                    freeTicketCount = 1,
                    priceId = targetTicket.PriceId,
                    seat = targetSeatId
                }},
                requestModel = new PolytReqModel()
            };
            var resp = await PostAsync(commitOrder, JsonHelper.ObjToJson(reqMode), proxy);
            var obj = JsonHelper.JsonToObj<RespModel<string>>(resp);
            return obj;
        }

        private async Task<RespModel<dynamic>> Pay(IWebProxy proxy, string uuid)
        {
            var reqMode = new { 
                consignee = Appsetting.Get("PeopleName"),
                consigneePhonr = Appsetting.Get("Phone"),
                deliveryWay = "01",
                movieIds = "",
                orderFreightAmt = 0,
                payWayCode = "06",
                requestModel = new PolytReqModel(),
                seriesId = "",
                uuid = uuid
            };
            var resp = await PostAsync(createOrder, JsonHelper.ObjToJson(reqMode), proxy);
            var obj = JsonHelper.JsonToObj<RespModel<dynamic>>(resp);
            return obj;
        }

        private int GetTargetSeatId(SeatsInfo allSeats, int priceId, List<int> sellSeats)
        {
            var ComplySeats = allSeats.seatList.Where(s => s.p == priceId && sellSeats.Contains(s.sd)).ToList();//符合条件的位置
            return Tools.ListRandom(ComplySeats).FirstOrDefault()?.sd ?? 0;
        }

        /// <summary>
        /// 获取座位信息
        /// </summary>
        public async Task<SeatsInfo> GetSeatsInfoAsync(IWebProxy proxy, string showId, string sectionId)
        {
            var resp = await GetAsync("https://cdn.polyt.cn/seat/h5/" + showId + "_"+ sectionId + ".json?callback=jsonpCallback", proxy);
            resp = resp.Replace("jsonpCallback(", "").TrimEnd(')').TrimEnd('"');
            var data = JsonHelper.JsonToObj<RespModel<string>>(resp).data;
            var allSeats = JsonHelper.JsonToObj<SeatsInfo>(data);
            if(allSeats is null)
            {
                Console.WriteLine("获取位置描述信息失败");
                return null;
            }
            return allSeats;
        }


        /// <summary>
        /// 获取可售座位
        /// </summary>
        /// <returns></returns>
        public async Task<List<int>> GetSellSeats(string sectionId, string showId, IWebProxy proxy)
        {
            var reqModel = new
            {
                sectionId = sectionId,
                showId = showId,
                requestModel = new PolytReqModel()
            };
            var resp = await PostAsync(sellSeatListUrl, JsonHelper.ObjToJson(reqModel), proxy);
            var model = JsonHelper.JsonToObj<RespModel<SellSeatsId>>(resp);
            if (model.code != 200)
            {
                Console.WriteLine("座位已全部售出");
                return null;
            }
            var sellSeats = model.data?.sellSeatIds ?? new List<int>();
            return sellSeats;
        }


        /// <summary>
        /// 选择票
        /// </summary>
        public async Task<TicketResp> ChoosePriceAsync(IWebProxy proxy, List<int> dayChoose, List<int> priceChoose)
        {
            var reqModel = new
            {
                productId = this.productId,
                projectId = this.projectId,
                theaterId = this.theaterId,
                requestModel = new PolytReqModel()
            };
            var resp = await PostAsync(showInfoDetailUrl, JsonHelper.ObjToJson(reqModel), proxy);
            if (resp.Contains("请求失败"))
            {
                Console.WriteLine("请求失败");
                return null;
            }
            var groups = JsonHelper.JsonToObj<dynamic>(resp).data.platShowInfoDetailVOList;
            var targetTicket = new TicketResp();
            int date = 0;
            foreach (var group in groups)
            {
                int priceIndex = 0;
                foreach (var ticketPrice in group.ticketPriceList)
                {
                    if (ticketPrice.reservedCount > targetTicket.ReservedCount && dayChoose.Contains(date) && priceChoose.Contains(priceIndex))
                    {
                        targetTicket = new TicketResp()
                        {
                            Date = date,
                            PriceIndex = priceIndex,
                            ReservedCount = ticketPrice.reservedCount,
                            ShowId = ticketPrice.showId,
                            SectionId = group.sectionId,
                            DateTime = group.showTime,
                            Price = ticketPrice.actuallyPrice,
                            PriceId = ticketPrice.priceId
                        };
                    }
                    priceIndex++;
                }
                date++;
            }
            if (!string.IsNullOrWhiteSpace(targetTicket.ShowId))
            {
                return targetTicket;
            }
            Console.WriteLine("无票或未开售");
            return null;
        }

        /// <summary>
        /// 验证cookie是否生效
        /// </summary>
        public bool VerifyCookie()
        {
            var reqBody = new
            {
                requestModel = new PolytReqModel()
            };
            return JsonHelper.JsonToObj<RespModel<dynamic>>(PostAsync(checkLoginUrl, JsonHelper.ObjToJson(reqBody), null).Result).data != null;
        }

        public Dictionary<string, string> GetReqHeaders()
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Origin", "https://m.polyt.cn");
            headers.Add("Referer", "https,//m.polyt.cn/");

            return headers;
        }

        private async Task<string> GetAsync(string serviceAddress, IWebProxy proxy)
        {
            return await HttpRequest.GetAsync(serviceAddress, GetReqHeaders(), proxy);
        }

        private async Task<string> PostAsync(string serviceAddress, string strContent, IWebProxy proxy)
        {
            return await HttpRequest.PostAsync(serviceAddress, strContent, GetReqHeaders(), proxy);
        }
    }
}
