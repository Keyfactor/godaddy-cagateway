using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public class POSTCertificateRevokeRequest
    {
        public enum REASON
        {
            AFFILIATION_CHANGED, 
            CESSATION_OF_OPERATION, 
            KEY_COMPROMISE, 
            PRIVILEGE_WITHDRAWN, 
            SUPERSEDED
        }

        public string reason { get; set; }

    }
}
