using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public class POSTCertificatesV1OVRequest : POSTCertificateRequest
    {
        public OrganizationInfo organization { get; set; }
    }
}
