using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    public class JSHelper
    {
        public JSHelper()
        {

        }

        public static async Task<T> ExecJavaScrptAsync<T>(string jsname, params object[] argument)
        {
            var _nodeServices = (INodeServices)HttpContext.ServiceProvider.GetService(typeof(INodeServices));
            return await _nodeServices.InvokeAsync<T>(jsname, argument);
        }
    }
}
