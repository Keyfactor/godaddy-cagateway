using CAProxy.AnyGateway.Interfaces;
using CAProxy.AnyGateway.Models;
using CAProxy.Common;
using CSS.PKI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CAProxy.Generic.GoDaddy
{
    public class GoDaddyCAProxy : ICAConnector
    {
        public void Synchronize(ICertificateDataReader certificateDataReader, BlockingCollection<CertificateRecord> blockingBuffer, CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken, string logicalName)
        {
            throw new NotImplementedException();
        }

        public EnrollmentResult Enroll(string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
        {
            throw new NotImplementedException();
        }

        public CAConnectorCertificate GetSingleRecord(string caRequestID)
        {
            throw new NotImplementedException();
        }

        public void Initialize(ICAConnectorConfigProvider configProvider)
        {
            throw new NotImplementedException();
        }

        public int Revoke(string caRequestID, string hexSerialNumber, uint revocationReason)
        {
            throw new NotImplementedException();
        }

        public void Ping()
        {
            throw new NotImplementedException();
        }

        public void ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
        {
            throw new NotImplementedException();
        }

        public void ValidateProductInfo(EnrollmentProductInfo productInfo, Dictionary<string, object> connectionInfo)
        {
            throw new NotImplementedException();
        }

        #region Not Implemented

        public EnrollmentResult Approve(string caRequestID)
        {
            throw new NotImplementedException();
        }

        public void Deny(string caRequestID)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
