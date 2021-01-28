using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public class POSTCertificatesV1EVRequest : POSTCertificateRequest
    {
        public ContactInfo contact { get; set; }
        public OrganizationInfo organization { get; set; }
    }
}
