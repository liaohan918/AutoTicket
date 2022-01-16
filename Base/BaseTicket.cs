using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoTicket
{
    public class BaseTicket
    {
        public string _curUrl = string.Empty;//当前页面

        public int _curDay;//当前日

        public int _curPriceIndex;//当前票档

        public int _curPositionIndex;//当前场次

        public ICollection<int> _haveTicketDays;//有票的日期

        public int _status = 0; //状态 0-暂停, 1-开始


        public int _failCode;//失败原因

        private DateTime startTime;

        protected object lock_1 = new object();
        protected object lock_2 = new object();
        protected int _tryCount = 1;
        /// <summary>
        /// 尝试次数
        /// </summary>
        protected int TryCount
        {
            get
            {
                lock (lock_1)
                {
                    return _tryCount;
                }
            }
            set
            {
                lock (lock_2)
                {
                    _tryCount = value;
                }
            }
        }

        /// <summary>
        /// 演出开始时间
        /// </summary>
        protected DateTime showStartTime;

        public BaseTicket()
        {
            showStartTime = DateTime.Parse(Appsetting.Get("StartTime"));
        }

        public virtual void Start()
        {
            Console.WriteLine("开始抢票");
            startTime = DateTime.Now;
            _status = 1;
        }

        public virtual void Stop()
        {            
            _status = 0;
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>本次抢票共耗时{(DateTime.Now - startTime).TotalSeconds}秒,共尝试{TryCount}次");
        }

        /// <summary>
        /// 日期优先
        /// </summary>
        /// <returns></returns>
        public virtual void DateFirst()
        {

        }

        /// <summary>
        /// 票价优先
        /// </summary>
        public virtual void PriceFrist()
        {

        }

        /// <summary>
        /// 场次优先
        /// </summary>
        public virtual void PositionFirst()
        {

        }
    }
}
