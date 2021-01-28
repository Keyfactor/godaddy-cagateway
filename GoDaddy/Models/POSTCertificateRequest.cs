using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public abstract class POSTCertificateRequest
    {
        private const string BEGIN_CSR_DELIM = "-----BEGIN CERTIFICATE REQUEST-----\r\n";
        private const string END_CSR_DELIM = "\r\n-----END CERTIFICATE REQUEST-----";

        public ContactInfo contact { get; set; }
        public string csr { get; set; }
        public int period { get; set; }
        public string productType { get; set; }
        public string rootType { get; set; }
        public string slotSize { get; set; }
        public string[] subjectAlternativeNames { get; set; }

        public void SetCSR(string csrString)
        {
            csr = csrString.StartsWith(BEGIN_CSR_DELIM, StringComparison.OrdinalIgnoreCase) ? csrString : BEGIN_CSR_DELIM + csrString;
            csr = csr.EndsWith(END_CSR_DELIM, StringComparison.OrdinalIgnoreCase) ? csr : csr + END_CSR_DELIM;
        }

    }
}
