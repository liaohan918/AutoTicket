using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    public class RespModel<T>
    {
        public int code { get; set; }
        //public string exception { get; set; }
        public string msg { get; set; }
        public T data { get; set; }

    }
}
