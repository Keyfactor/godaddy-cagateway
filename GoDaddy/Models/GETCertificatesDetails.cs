using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public class GETCertificatesDetailsResponse
    {
        public List<CertificateDetails> certificates { get; set; }
    }

    public class CertificateDetails
{
        public string certificateId { get; set; }
        public int period { get; set; }
        public string commonName { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public string serialNumber { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime? completedAt { get; set; }
        public DateTime? validEndAt { get; set; }
        public DateTime? validStartAt { get; set; }
        public DateTime? revokedAt { get; set; }
    }
}
