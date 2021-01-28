using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public class OrganizationInfo
    {
        public AddressInfo address { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public JurisdictionInfo jurisdictionOfIncorporation { get; set; }
        public string registrationNumber { get; set; }
    }
}
