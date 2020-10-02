using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public class POSTCertificatesV1DV
    {
        public ContactInfo contact { get; set; }
        public string csr { get; set; }
        public int period { get; set; }
        public string productType { get { return "DV_SSL"; } }
        public string rootType { get { return "GODADDY_SHA_2"; } }

    }

    public class ContactInfo
    {
        public string email { get; set; }
        public string nameFirst { get; set; }
        public string nameLast { get; set; }
        public string phone { get; set; }
    }
}
