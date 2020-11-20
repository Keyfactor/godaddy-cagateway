using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public class GETCertificateResponse
    {
        public string serialNumber { get; set; }
        public string certificateThumbprint { get; set; }
        public CertificateChain pems { get; set; }
    }

    public class CertificateChain
    {
        public string certificate { get; set; }
        public string intermediate { get; set; }
        public string root { get; set; }
        public string cross { get; set; }
    }
}
