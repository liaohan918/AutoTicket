using AutoTicket.Model.DaMai;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTicket
{
    public class DaMai : BaseTicket
    {
        private readonly string verifyCookieUrl = "https://api-gw.damai.cn/user.html?_ksTS=" + Tools.GetMillTimeStamp() + "_55&callback=jsonp56";

        private const string pickSeatPrefix = "https://sseat.damai.cn/xuanzuo/io/";

        private const string appKey = "12574478";

        private const string tokenKey = "_m_h5_tk";

        private readonly string token;

        private readonly string itemId;

        private readonly string umId;

        private readonly string ua;

        public DaMai()
        {
            token = GetTokenFromCookieAsync(Appsetting.GetCookie()).Result;
            var targetUrl = Appsetting.GetTargetUrl();
            itemId = targetUrl.Substring(targetUrl.IndexOf("id=") + 3);
            itemId = itemId.Contains("&") ? itemId.Remove(itemId.IndexOf("&")) : itemId;
            umId = Appsetting.Get("Web", Appsetting.TicketWeb, "umid");
            ua = Appsetting.Get("Web", Appsetting.TicketWeb, "ua");
        }

        public override void Start()
        {
            base.Start();
            Console.WriteLine("启动前请确保Cookie为最新");
            if (!VerifyCookie())
            {
                Console.WriteLine("Cookie失效或已过期,请在Appsetting.json填写正确Cookie并重新启动");
                return;
            }
            Task.Run(() => UltraTicket(null));//该线程不需要代理,网速比那些代理的IP要快，不用白不用
            var taskCount = int.Parse(Appsetting.Get("TaskCount"));
            if(taskCount > 0)
            {
                var proxyIPs = ProxyHelper.GetProxy(taskCount);
                if (proxyIPs.Count == taskCount)
                {
                    for (var i = 0; i < taskCount; i++)
                    {
                        var index = i;
                        Task.Run(() => UltraTicket(proxyIPs[index]));
                    }
                }
                else
                {
                    Console.WriteLine("获取代理IP异常,请检查是否将本机IP添加至白名单");
                }
            }            
        }

        private async void UltraTicket(ProxyIP proxyIP)
        {
            IWebProxy proxy = null;
            DateTime expireTime = new DateTime(9999, 12, 30);
            if (proxyIP is not null)
            {
                proxy = new WebProxy(proxyIP.iP, proxyIP.port);
                expireTime = proxyIP.expire_time;
            }
            var dateId = "";//日期Id
            var performId = 0;//场次Id
            var projectId = "";
            var cityId = "";
            var priceId = "";
            PerformView performView = null;//场次
            Sku sku = null;//票价
            AreaResp areaResp = null;
            Seats seats = null;//所有座位信息
            SellSeats sellSeats = null;//区域内在售座位
            Seat seat = null;//抢到的位置
            while (_status == 1)
            {
                if (DateTime.Now < showStartTime)
                {
                    Thread.Sleep(1);
                    continue;
                }
                try
                {
                    var dayChoose = Appsetting.GetDate();
                    var position = Appsetting.GetPositionIndex();
                    var priceIndex = Appsetting.GetPriceIndex();
                    var performResp = GetPerformInfo("", 4, proxy);
                    if (performResp is not null)
                    {
                        if (!performResp.performCalendar.performViews.Any(p => p.salable))
                            continue;
                        if (performResp.performCalendar.showDates)//抢票界面需要选择日期
                        {
                            var dates = performResp.performCalendar.dateViews.Where(d => d.clickable && d.buyPermission && dayChoose.Contains(int.Parse(d.dateId.Substring(6, 2)))).ToList<DateView>();
                            if ((dates?.Count ?? 0) <= 0)
                            {
                                Console.WriteLine("你的抢票日期不在范围内");
                                continue;
                            }
                            dateId = Tools.ListRandom<DateView>(dates).GetRandomValue()?.dateId;
                            if (!string.IsNullOrWhiteSpace(dateId) && !(performResp.performCalendar.dateViews.First(d => d.dateId == dateId)?.Checked ?? false))
                            {
                                performResp = GetPerformInfo(dateId, 4, proxy);
                            }
                        }
                        projectId = performResp.itemBasicInfo.projectId;
                        if (performResp.performCalendar.performViews?.Count > 1)//至少两个场次
                        {
                            performView = GetTargetPerformView(performResp.performCalendar.performViews, position);//获取场次
                            if(!performView.Checked)
                                performResp = GetPerformInfo(performView.performId, 2, proxy);
                        }
                        else
                        {
                            performView = performResp.performCalendar.performViews[0];
                        }
                        performId = int.Parse(performView.performId);
                        sku = GetTargetSku(performResp.perform.skuList, priceIndex);//选择票价
                        if(sku is not null)
                        {
                            cityId = performResp.itemBasicInfo.nationalStandardCityId;
                            areaResp = await GetAreaInfoAsync(proxy, cityId, performId, projectId);
                            priceId = sku.priceId;
                            if(areaResp is not null)
                            {
                                var areaGroup = new List<string>();
                                if (areaResp.seatQuYu.quyu.Count == 1)//只有一个区域
                                {
                                    areaGroup.Add(areaResp.seatQuYu.areaStatusGroup);
                                }
                                else
                                {
                                    areaGroup = areaResp.seatQuYu.quyu.Where(q => q.j.Contains(priceId))?.Select(a => a.vid)?.ToList();//票价对应目标区域
                                    var areaSeatStatus = await QueryPerformSeatStatus(proxy, projectId, performId, areaResp.seatQuYu.ver);//所有区域在售和不在售座位信息                                    
                                    areaGroup = GetAreaIdByJson(areaSeatStatus, areaGroup);
                                }
                                foreach(var areaId in areaGroup)
                                {
                                    seats = await GetAreaSeatsAsync(proxy, areaResp.seatQuYu.resourcesPath, areaId);//获取区域下所有位置信息
                                    sellSeats = await QueryAreaSeatsInfo(proxy, projectId, performId, cityId, areaId);//获取区域内所有可售座位信息
                                    seat = seats.seats.Where(s => sellSeats.seat.Exists(a => a.sid == s.sid) && s.plid == priceId)?.ToList()?.GetRandomValue<Seat>();
                                    if (seat is not null)
                                    {
                                        var order = await GoToOrderConfirm(proxy, projectId, performId, sku.skuId, seat.sid, areaId);
                                        var orderInitData = order.Item1;
                                        var buyRefer = order.Item2;
                                        if (!string.IsNullOrWhiteSpace(orderInitData))
                                        {
                                            var postData = await ParseOrderInitDataToPayDataAsync(orderInitData, seat.sid);
                                            if (await Pay(proxy, postData, buyRefer, seat, performResp))
                                            {
                                                Stop();
                                                return;
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }

                    if ((expireTime - DateTime.Now).TotalSeconds <= 30)
                    {
                        var newProxy = ProxyHelper.GetProxy();
                        proxy = new WebProxy(newProxy.iP, newProxy.port);
                        expireTime = newProxy.expire_time;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    Console.WriteLine($"第{TryCount}次抢票");
                    TryCount++;
                }
            }              
        }

        /// <summary>
        /// 转换订单界面数据为支付必要参数json
        /// </summary>
        /// <param name="orderInitData"></param>
        /// <returns></returns>
        private async Task<string> ParseOrderInitDataToPayDataAsync(string orderInitData, string seatid)
        {
            var confirmOrder_1 = await GetConfirmOrderByJson(orderInitData);
            var dmDeliveryWayPC = await GetDeliveryWayPCByJson(orderInitData);
            var dmEttributesHiddenPC_DmAttributesBlock = await GetEttributesHiddenPC_DmAttributesBlock(orderInitData);
            var dmPayTypePC = await GetPayTypePC(orderInitData);
            var dmProtocolPC = await GetProtocolPC(orderInitData);
            var dmViewerPC = await GetViewerPC(orderInitData, seatid);

            var data = $"{{\"data\": {{{confirmOrder_1},{dmDeliveryWayPC},{dmEttributesHiddenPC_DmAttributesBlock},{dmPayTypePC},{dmProtocolPC},{dmViewerPC}}}";

            var endpointmdol = JsonHelper.ObjToJson(new Endpoint());

            var endpoint = $"\"endpoint\": {endpointmdol}";

            var featureUaAndUmid = JsonHelper.ObjToJson(new Feature(ua, umId));

            var feature = $"\"feature\": {featureUaAndUmid}";

            var structure = await GetStructure(orderInitData);

            var hierarchy = $"\"hierarchy\": {{{structure}}}";

            var submitParams = await GetSubmitParams(orderInitData);
            var validateParams = await GetValidateParams(orderInitData);
            var signature = await GetSignature(orderInitData);

            var linkage = $"\"linkage\": {{\"common\":{{\"compress\":true,{submitParams},{validateParams}}},{signature}}}}}";

            return $"{data},{endpoint},{feature},{hierarchy},{linkage}";
        }
        private async Task<string> GetSignature(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"signature\"");
                var endIndex = orderInitData.IndexOf("\"", startIndex + "\"signature\"".Length + 2);
                return orderInitData.Substring(startIndex, endIndex - startIndex + 1);
            });
        }

        private async Task<string> GetValidateParams(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"validateParams\"");
                var endIndex = orderInitData.IndexOf("\"", startIndex + "\"validateParams\"".Length + 2);
                return orderInitData.Substring(startIndex, endIndex - startIndex + 1);
            });
        }

        private async Task<string> GetSubmitParams(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"submitParams\"");
                var endIndex = orderInitData.IndexOf("\"", startIndex + "\"submitParams\"".Length + 2);
                return orderInitData.Substring(startIndex, endIndex - startIndex + 1);
            });
        }

        private async Task<string> GetStructure(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"structure\":{");
                var endIndex = orderInitData.IndexOf("}", startIndex);
                return orderInitData.Substring(startIndex, endIndex - startIndex + 1);
            });
        }

        private async Task<string> GetViewerPC(string orderInitData, string sid)
        {
            return await Task.Run(() => {
                if (orderInitData.IndexOf("dmViewerPC_") < 0)
                    return "";
                var startIndex = orderInitData.IndexOf("\"dmViewerPC_");
                var endIndex = orderInitData.IndexOf("\"native$null$dmViewerPC\"}", startIndex);
                var result = orderInitData.Substring(startIndex, endIndex - startIndex) + "\"native$null$dmViewerPC\"}";

                var performWatcher = Appsetting.Get("PeopleName");//观影人
                var nameIndex = result.IndexOf($"\"{performWatcher}\"");
                var viewerListIndex = result.IndexOf("\"dmViewerList\"");
                var left = result.IndexOf("[", viewerListIndex);
                var right = result.IndexOf("]", left);
                var lstr = result.Substring(left + 1, right - left - 1);
                var viewers = lstr.Split("},{");
                for(var i = 0;i < viewers.Length; i++)
                {
                    if (viewers[i].Contains(performWatcher))
                    {
                        var ostr = viewers[i];
                        viewers[i] = viewers[i].Replace("\"isUsed\":false", "\"isUsed\":true");
                        if (!ostr.EndsWith("}"))
                            viewers[i] += $",\"seatId\":{sid}";
                        else
                            viewers[i] = viewers[i].TrimEnd('}') + $",\"seatId\":{sid}" + "}";
                        result = result.Replace(ostr, viewers[i]);
                        break;
                    }
                }
                return result;
            });
        }

        private async Task<string> GetProtocolPC(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"dmProtocolPC_");
                var endIndex = orderInitData.IndexOf("\"native$null$dmProtocolPC\"}", startIndex);
                return orderInitData.Substring(startIndex, endIndex - startIndex) + "\"native$null$dmProtocolPC\"}";
            });
        }

        /// <summary>
        /// 获得dmDeliveryWayPCByJson
        /// </summary>
        /// <param name="orderInitData"></param>
        /// <returns></returns>
        private async Task<string> GetPayTypePC(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"dmPayTypePC_");
                var endIndex = orderInitData.IndexOf("\"native$null$dmPayTypePC\"}", startIndex);
                return orderInitData.Substring(startIndex, endIndex - startIndex) + "\"native$null$dmPayTypePC\"}";
            });
        }


        /// <summary>
        /// 获得dmDeliveryWayPCByJson
        /// </summary>
        /// <param name="orderInitData"></param>
        /// <returns></returns>
        private async Task<string> GetEttributesHiddenPC_DmAttributesBlock(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"dmEttributesHiddenPC_DmAttributesBlock");
                var endIndex = orderInitData.IndexOf("\"native$null$dmEttributesHiddenPC\"}", startIndex);
                return orderInitData.Substring(startIndex, endIndex - startIndex) + "\"native$null$dmEttributesHiddenPC\"}";
            });
        }

        /// <summary>
        /// 获得dmDeliveryWayPCByJson
        /// </summary>
        /// <param name="orderInitData"></param>
        /// <returns></returns>
        private async Task<string> GetDeliveryWayPCByJson(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"dmDeliveryWayPC_");
                var endIndex = orderInitData.IndexOf("\"native$null$dmDeliveryWay\"}", startIndex);
                return orderInitData.Substring(startIndex, endIndex - startIndex) + "\"native$null$dmDeliveryWay\"}";
            });
        }


        /// <summary>
        /// 获得ConfirmOrder_1
        /// </summary>
        /// <param name="orderInitData"></param>
        /// <returns></returns>
        private async Task<string> GetConfirmOrderByJson(string orderInitData)
        {
            return await Task.Run(() => {
                var startIndex = orderInitData.IndexOf("\"confirmOrder_1\":{"); 
                 var endIndex = orderInitData.IndexOf("\"block$null$emptyBlock\"}", startIndex);
                return orderInitData.Substring(startIndex, endIndex - startIndex) + "\"block$null$emptyBlock\"}";
            });
        }

        public async Task<bool> Pay(IWebProxy proxy, string postData, string refer, Seat seat,PerformResp performResp)
        {
            var resp = await PostAsync("https://buy.damai.cn/multi/trans/createOrder?feature=%7B%22returnUrl%22:%22https://orders.damai.cn/orderDetail%22,%22serviceVersion%22:%221.8.5%22%7D&submitref=undefined", postData, proxy);
            var model = JsonHelper.JsonToObj<PayRespModel>(resp);
            if (model?.resultMessage.Contains("未支付") ?? false)
            {
                Console.WriteLine(model.resultMessage);
                return true;
            }
            else if(model?.success ?? false)
            {
                Console.WriteLine($"抢票成功, 抢到:{performResp.perform.performName} {seat.chint} {seat.rhint} {seat.shint}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取支付必要信息
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="projectId"></param>
        /// <param name="performId"></param>
        /// <param name="skuId"></param>
        /// <param name="seatId"></param>
        /// <param name="standId"></param>
        /// <returns></returns>
        public async Task<(string, string)> GoToOrderConfirm(IWebProxy proxy, string projectId, int performId, string skuId, string seatId, string standId)
        {
            var data = "{\"damai\":\"1\",\"channel\":\"damai_app\",\"umpChannel\":\"10002\",\"atomSplit\":\"1\",\"seatInfo\":[{\"seatId\":" + seatId + ",\"standId\":\"" + standId + "\"}],\"serviceVersion\":\"2.0.0\"," +
                "\"ua\":\"" + ua + "\"," +
                "\"umidToken\":\"" + umId + "\"}";
            var buyParam = $"{itemId}_1_{skuId}";
            var buyRefer = "https://buy.damai.cn/orderConfirm?exParams=" + Tools.EncodeURI(data)
                + "&buyParam=" + buyParam + "&buyNow=true&projectId=" + projectId + "&performId=" + performId + "&spm=a2oeg.selectseat.bottom.dbuy";
            var resp = await GetAsync(buyRefer, proxy);
            if(resp.IndexOf("window.__INIT_DATA__") > -1)
            {
                var leftParenthesisIndex = resp.IndexOf("window.__INIT_DATA__");
                var rightParenthesisIndex = resp.IndexOf("</script>", resp.IndexOf("window.__INIT_DATA__"));
                resp = resp.Substring(leftParenthesisIndex, rightParenthesisIndex - leftParenthesisIndex);
            }
            return (resp, buyRefer);
        }

        /// <summary>
        /// 根据QueryPerformSeatStatus获取目标区域
        /// </summary>
        /// <param name="areaSeatStatus"></param>
        /// <returns></returns>
        private List<string> GetAreaIdByJson(string json, List<string> areaGroup)
        {
            var resultList = new List<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return resultList;
            }
            var leftParenthesisIndex = json.IndexOf("{", json.IndexOf("seatStatus"));//左括号位置下标
            var rightParenthesisIndex = json.IndexOf("}", leftParenthesisIndex);
            var result = json.Substring(leftParenthesisIndex + 1, rightParenthesisIndex - leftParenthesisIndex - 1);
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\([^\(]*\)", "").Replace("\\","");
            var areaInfo = new Dictionary<string, int>();
            var arr = result.Split(",");
            foreach(var p in arr)
            {
                var kv = p.Split(":");
                if (!kv[1].Contains("2"))
                    continue;
                var k = kv[0].Replace("\"","");
                var v = kv[1];
                areaInfo.Add(k, v.Replace("8", "").Length);
            }
            var maxValue = new KeyValuePair<string, int>();
            foreach(var kvp in areaInfo)
            {
                if (areaGroup.Contains(kvp.Key) && kvp.Value > maxValue.Value)
                {
                    maxValue = kvp;
                    resultList = resultList.Prepend(kvp.Key).ToList();
                }
                else
                {
                    resultList.Add(kvp.Key);
                }
            }
            return resultList;
        }

        /// <summary>
        /// 获取区域内所有可售座位信息
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="projectId"></param>
        /// <param name="performId"></param>
        /// <param name="cityId"></param>
        /// <param name="areaId"></param>
        private async Task<SellSeats> QueryAreaSeatsInfo(IWebProxy proxy, string projectId, int performId, string cityId, string areaId)
        {
            var s = Tools.GetMillTimeStamp();
            var pfId = 2147483647 ^ performId;
            var data = "{\"cityId\":\"" + cityId + "\",\"pfId\":"+ pfId + ",\"standIds\":\""+ areaId + "\",\"channel\":100100010001,\"projectId\":\""+ projectId + "\",\"lessFirst\":true,\"dmChannel\":\"pc@damai_pc\"}";
            var sign = await GetSign(s, data);
            var resp = await GetAsync("https://mtop.damai.cn/h5/mtop.damai.wireless.seat.queryseatstatus/1.0/?jsv=2.6.0" +
                "&appKey=" + appKey +
                "&t=" + s +
                "&sign=" + sign + "&type=originaljson&dataType=json&v=1.0&H5Request=true&AntiCreep=true&AntiFlood=true&api=mtop.damai.wireless.seat.queryseatstatus" +
                "&data=" + Tools.EncodeURI(data), proxy);
            return JsonHelper.JsonToObj<RespModel<SellSeats>>(resp)?.data ?? null;
        }

        /// <summary>
        /// 查询每个区域的座位状态
        /// </summary>
        public async Task<string> QueryPerformSeatStatus(IWebProxy proxy, string projectId, int performId, string areaInfoVersion)   
        {
            var s = Tools.GetMillTimeStamp();
            var data = "{\"performanceId\":\"" + performId + "\",\"projectId\":\"" + projectId + "\",\"areaInfoVersion\":" + areaInfoVersion + ",\"dmChannel\":\"pc@damai_pc\"}";
            var sign = await GetSign(s, data);
            var resp = await GetAsync("https://mtop.damai.cn/h5/mtop.damai.wireless.seat.queryperformseatstatus/1.0/?jsv=2.6.0" +
                "&appKey="+ appKey +
                "&t="+ s +
                "&sign=" + sign + "&type=originaljson&dataType=json&v=1.0&H5Request=true&AntiCreep=true&AntiFlood=true&api=mtop.damai.wireless.seat.queryperformseatstatus" +
                "&data=" + Tools.EncodeURI(data), proxy);
            return resp;// JsonHelper.JsonToObj<RespModel<SeatStatus>>(resp)?.data ?? null;
        }

        public async Task<Dynamicinfo> QueryDynamicinfoAsync(IWebProxy proxy, string projectId,int performId, string itemId)
        {
            var s = Tools.GetMillTimeStamp();
            var data = "{\"projectId\":\"" + projectId + "\",\"performanceId\":\"" + performId + "\",\"itemId\":\"" + itemId + "\",\"hasPromotion\":\"false\",\"dmChannel\":\"pc@damai_pc\"}";
            var sign = await GetSign(s, data);
            var resp = await GetAsync("https://mtop.damai.cn/h5/mtop.damai.wireless.seat.dynamicinfo/1.0/?jsv=2.6.0" +
                "&appKey=" + appKey +
                "&t=" + s +
                "&sign=" + sign +
                "&type=originaljson&dataType=json&v=1.0&H5Request=true&AntiCreep=true&AntiFlood=true&api=mtop.damai.wireless.seat.dynamicinfo" +
                "&data=" + Tools.EncodeURI(data), proxy);
            if (!string.IsNullOrEmpty(resp))
            {
                return JsonHelper.JsonToObj<RespModel<Dynamicinfo>>(resp)?.data;
            }
            return null;
        }

        /// <summary>
        /// 获取区域下位置信息
        /// </summary>
        /// <param name="resourcepath"></param>
        /// <param name="areaId"></param>
        /// <returns></returns>
        private async Task<Seats> GetAreaSeatsAsync(IWebProxy proxy, string resourcepath, string areaId)
        {
            var suffix = resourcepath.Substring(resourcepath.IndexOf("io/") + 3);
            var resp = await GetAsync(pickSeatPrefix + suffix + areaId + ".json", proxy);
            return JsonHelper.JsonToObj<Seats>(resp);
        }

        /// <summary>
        /// 获取分区信息
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="sign"></param>
        /// <param name="cityId"></param>
        /// <param name="performId"></param>
        /// <returns></returns>
        private async Task<AreaResp> GetAreaInfoAsync(IWebProxy proxy, string cityId, int performId, string projectId)
        {
            var s = Tools.GetMillTimeStamp();
            var pfId = 2147483647 ^ performId;
            var data = "{\"cityId\":\""+cityId+"\",\"pfId\":"+ pfId + ",\"excludestatus\":true,\"svgEncVer\":\"1.0\",\"dmChannel\":\"pc@damai_pc\"}";
            var sign = await GetSign(s, data);
            var resp = JsonHelper.JsonToObj<RespModel<Result>>(GetAsync("https://mtop.damai.cn/h5/mtop.damai.wireless.project.getb2b2careainfo/1.3/?jsv=2.6.0" +
                "&appKey=12574478&t=" + s +
                "&sign=" + sign + "&type=originaljson&dataType=json&v=1.3" +
                "&H5Request=true&AntiCreep=true&AntiFlood=true&api=mtop.damai.wireless.project.getB2B2CAreaInfo" +
                "&data=" + Tools.EncodeURI(data), proxy).Result);
            if (!string.IsNullOrWhiteSpace(resp.data.result))
            {
                return JsonHelper.JsonToObj<AreaResp>(resp.data.result.Replace("\\", ""));
            }
            Console.WriteLine("获取区域信息失败");
            return null;
        }

        /// <summary>
        /// 获取Sign
        /// </summary>
        /// <param name="token"></param>
        /// <param name="s"></param>
        /// <param name="appKey"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private async Task<string> GetSign(string s, string data)
        {
            var para = $"{token}&{s}&{appKey}&{data}";
            var sign = await JSHelper.ExecJavaScrptAsync<string>(Path.Combine(Directory.GetCurrentDirectory(), "scripts/damaisign.js"), para);
            if (!string.IsNullOrWhiteSpace(sign))
            {
                return sign;
            }
            return "";
        }

        /// <summary>
        /// 获取Token
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public async Task<string> GetTokenFromCookieAsync(string cookie)
        {
            var token = await JSHelper.ExecJavaScrptAsync<string>(Path.Combine(Directory.GetCurrentDirectory(), "scripts/damaitoken.js"), tokenKey, cookie);
            if (!string.IsNullOrWhiteSpace(token) && token.Contains("_"))
            {
                return token.Split('_')[0];
            }
            return "";
        }

        private Sku GetTargetSku(List<Sku> skuList, IList<int> priceIndex)
        {
            foreach (var i in priceIndex)
            {
                if (i < skuList.Count && skuList[i].skuSalable)
                {
                    return skuList[i];
                }
            }
            Console.WriteLine("目标票价无票或未开售");
            return null;
        }

        /// <summary>
        /// 获取场次
        /// </summary>
        /// <param name="performResp"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private PerformView GetTargetPerformView(List<PerformView> performViews, IList<int> position)
        {
            foreach (var i in position)
            {
                if (i < performViews.Count && performViews[i].salable)
                {
                    return performViews[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取演出信息
        /// </summary>
        /// <param name="date">切换日期时传日期,切换场次时传performId</param>
        /// <param name="dataType">2:data为performId, 4:data为dataId</param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public PerformResp GetPerformInfo(string date, int dataType, IWebProxy proxy)
        {
            PerformResp performResp;
            if (string.IsNullOrWhiteSpace(date))
            {
                performResp = JsonHelper.JsonToObj<PerformResp>(GetAsync("https://detail.damai.cn/subpage?itemId=" + itemId + "&apiVersion=2.0&dmChannel=pc@damai_pc&bizCode=ali.china.damai&scenario=itemsku&dataType=&dataId=&privilegeActId=&callback=__jp0", proxy).Result.Replace("__jp0(", "").TrimEnd(')'));
            }
            else
            {
                performResp = JsonHelper.JsonToObj<PerformResp>(GetAsync("https://detail.damai.cn/subpage?itemId=" + itemId + "&dataId=" + date + "&dataType="+ dataType + "&apiVersion=2.0&dmChannel=pc@damai_pc&bizCode=ali.china.damai&scenario=itemsku&privilegeActId=&callback=__jp2", proxy).Result.Replace("__jp2(", "").TrimEnd(')'));
            }
            if (performResp is null)
            {
                Console.WriteLine("获取演出信息失败");
            }
            return performResp;
        }

        /// <summary>
        /// 验证cookie是否生效
        /// </summary>
        public bool VerifyCookie()
        {
            return GetAsync(verifyCookieUrl, null).Result.Contains("userNick");
        }

        public Dictionary<string, string> GetReqHeaders()
        {
            var headers = new Dictionary<string, string>();
            headers.Add("referer", "https://www.damai.cn/");

            return headers;
        }

        private async Task<string> GetAsync(string serviceAddress, IWebProxy proxy = null)
        {
            return await HttpRequest.GetAsync(serviceAddress, GetReqHeaders(), proxy);
        }

        private async Task<string> PostAsync(string serviceAddress, string strContent, IWebProxy proxy = null)
        {
            return await HttpRequest.PostAsync(serviceAddress, strContent, GetReqHeaders(), proxy);
        }
    }

}
