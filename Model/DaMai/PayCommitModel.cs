using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTicket.Model.DaMai
{
    public class Feature
    {
        public Feature(string ua, string umid)
        {
            this.ua= ua;
            this.umidToken = umid;
        }
        public string ua { get; set; }
        public string umidToken { get; set; }
    }

    public class Endpoint
    {
        public string mode { get; set; } = "PC";
        public string osVersion { get; set; } = "PC";
        public string protocolVersion { get; set; } = "3.0";
        public string ultronage { get; set; } = "true";
    }

    public class PayRespModel
    {
        public string resultMessage { get; set; }
        public bool success { get; set; }
    }
}
