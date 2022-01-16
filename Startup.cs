using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddNodeServices();

            HttpContext.ServiceProvider = services.BuildServiceProvider();
        }
        public void Configure()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly(); // 获取当前程序集 
            BaseTicket ticket = (BaseTicket)assembly.CreateInstance($"AutoTicket.{Appsetting.TicketWeb}");
            if (ticket is null)
            {
                Console.WriteLine($"暂不支持{Appsetting.TicketWeb}抢票");
            }

            ticket.Start();
            Console.ReadLine();
        }
    }
}
