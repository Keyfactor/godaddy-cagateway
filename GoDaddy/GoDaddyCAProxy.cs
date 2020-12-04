using CAProxy.AnyGateway.Interfaces;
using CAProxy.AnyGateway.Models;
using CAProxy.Common;
using CSS.Common.Logging;
using CSS.PKI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Keyfactor.AnyGateway.GoDaddy.API;
using Keyfactor.AnyGateway.GoDaddy.Models;

namespace Keyfactor.AnyGateway.GoDaddy
{
    public class GoDaddyCAProxy : CAProxy.AnyGateway.BaseCAConnector
    {
        private APIProcessor _api { get; set; }
        private int _syncPageSize { get; set; }
        private int _enrollmentRetries { get; set; }
        private int _secondsBetweenEnrollmentRetries { get; set; }

        #region Interface Methods
        public override void Initialize(ICAConnectorConfigProvider configProvider)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            foreach (KeyValuePair<string, object> configEntry in configProvider.CAConnectionData)
                Logger.Trace($"{configEntry.Key}: {configEntry.Value}");

            string[][] connectionKeys = new string[][] { new string[] { "ApiUrl", "string" },
                                                         new string[] { "ApiKey", "string" },
                                                         new string[] { "ShopperId", "string" },
                                                         new string[] { "SyncPageSize", "int" },
                                                         new string[] { "EnrollmentRetries", "int" },
                                                         new string[] { "SecondsBetweenEnrollmentRetries", "int" } };
            ValidateParameters<object>(configProvider.CAConnectionData, connectionKeys);

            string apiUrl = configProvider.CAConnectionData["ApiUrl"].ToString();
            string apiKey = configProvider.CAConnectionData["ApiKey"].ToString();
            string shopperId = configProvider.CAConnectionData["ShopperId"].ToString();
            _syncPageSize = Convert.ToInt32(configProvider.CAConnectionData["SyncPageSize"]);
            _enrollmentRetries = Convert.ToInt32(configProvider.CAConnectionData["EnrollmentRetries"]);
            _secondsBetweenEnrollmentRetries = Convert.ToInt32(configProvider.CAConnectionData["SecondsBetweenEnrollmentRetries"]);

            _api = new APIProcessor(apiUrl, apiKey, shopperId);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
        }

        public override void Ping()
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
        }

        public override void ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            List<string> errors = new List<string>();
            if (!connectionInfo.ContainsKey("ApiUrl"))
            {
                errors.Add($"ApiUrl is not found! Ensure CAConnection contains an entry for ApiUrl");
            }
            if (!connectionInfo.ContainsKey("ApiKey"))
            {
                errors.Add($"ApiKey is not found! Ensure CAConnection contains an entry for ApiKey");
            }
            if (!connectionInfo.ContainsKey("ShopperId"))
            {
                errors.Add($"ShopperId is not found! Ensure CAConnection contains an entry for ShopperId");
            }

            if (errors.Any())
            {
                Logger.Error($"The following errors occured while validating CA configuration:");
                Logger.Error($"{String.Join(Environment.NewLine, errors)}");
                throw new Exception("CAConnection contains invalid configuration.");
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
        }

        [Obsolete]
        public override void Synchronize(ICertificateDataReader certificateDataReader, BlockingCollection<CertificateRecord> blockingBuffer, CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken, string logicalName)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            throw new NotImplementedException();
        }

        public override void Synchronize(ICertificateDataReader certificateDataReader, BlockingCollection<CAConnectorCertificate> blockingBuffer, CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            string customerId = JsonConvert.DeserializeObject<GETShopperResponse>(_api.GetCustomerId()).customerId;

            int pageNumber = 1;
            bool wasLastPage = false;

            do
            {
                GETCertificatesDetailsResponse certificates = JsonConvert.DeserializeObject<GETCertificatesDetailsResponse>(_api.GetCertificates(customerId, pageNumber, _syncPageSize));

                foreach (CertificateDetails certificate in certificates.certificates)
                {
                    string issuedCert = JsonConvert.DeserializeObject<GETCertificateResponse>(_api.DownloadCertificate(certificate.certificateId)).pems.certificate;
                    CertificateStatusEnum certStatus = CertificateStatusEnum.ISSUED;
                    if (!Enum.TryParse(certificate.status, out certStatus))
                        certStatus = CertificateStatusEnum.CANCELED;

                    blockingBuffer.Add(new CAConnectorCertificate
                    {
                        CARequestID = certificate.certificateId,
                        Certificate = issuedCert,
                        CSR = string.Empty,
                        ResolutionDate = certificate.completedAt,
                        RevocationDate = certificate.revokedAt,
                        RevocationReason = null,
                        Status = APIProcessor.MapReturnStatus(certStatus),
                        SubmissionDate = certificate.createdAt,
                        ProductID = certificate.type
                    });
                }

                wasLastPage = certificates.pagination.previous == certificates.pagination.last;
                pageNumber++;
            } while (!wasLastPage);
            
            blockingBuffer.CompleteAdding();

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

        [Obsolete]
        public override EnrollmentResult Enroll(string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            throw new NotImplementedException();
        }

        public override EnrollmentResult Enroll(ICertificateDataReader certificateDataReader, string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
            
            //TODO - build this out for new/renew/reissue use cases
            switch (enrollmentType)
            {
                case RequestUtilities.EnrollmentType.New:
                    break;
                case RequestUtilities.EnrollmentType.Renew:
                    break;
                case RequestUtilities.EnrollmentType.Reissue:
                    break;
                default:
                    return new EnrollmentResult { Status = 30, StatusMessage = $"Unsupported EnrollmentType: {enrollmentType}" };
            }

            EnrollmentResult result = new EnrollmentResult();
            //TODO Implement other GoDaddy certificate types

            string[][] parameterKeys = new string[][] { new string[] { "Email", "string" },
                                                         new string[] { "FirstName", "string" },
                                                         new string[] { "LastName", "string" },
                                                         new string[] { "Phone", "string" },
                                                         new string[] { "CertificatePeriodInYears", "int" } };
            ValidateParameters<string>(productInfo.ProductParameters, parameterKeys);

            POSTCertificatesV1DVRequest certRequest = new POSTCertificatesV1DVRequest();
            certRequest.contact = new ContactInfo();
            certRequest.contact.email = productInfo.ProductParameters["Email"];
            certRequest.contact.nameFirst = productInfo.ProductParameters["FirstName"];
            certRequest.contact.nameLast = productInfo.ProductParameters["LastName"];
            certRequest.contact.phone = productInfo.ProductParameters["Phone"];
            certRequest.SetCSR(csr);
            
            certRequest.period = Convert.ToInt32(productInfo.ProductParameters["CertificatePeriodInYears"]);
            certRequest.productType = productInfo.ProductID;
            //TODO - allow assignment of root type
            //certRequest.rootType = productInfo.ProductParameters["RootType"];

            string response;
            try
            {
                response = _api.EnrollCSR(csr, certRequest);
            }
            catch (Exception ex)
            {
                return new EnrollmentResult { Status = 30, StatusMessage = $"Error attempting to enroll certificate {subject}: {ex.Message}." };
            }
            POSTCertificatesV1DVResponse dvCertificate = JsonConvert.DeserializeObject<POSTCertificatesV1DVResponse>(response);

            CertificateStatusEnum certStatus = CertificateStatusEnum.PENDING_ISSUANCE;
            for(int i = 0; i < _enrollmentRetries; i++)
            {
                try
                {
                    GETCertificateDetailsResponse certResponse = JsonConvert.DeserializeObject<GETCertificateDetailsResponse>(_api.GetCertificate(dvCertificate.certificateId));
                    Enum.TryParse(certResponse.status, out certStatus);
                    if (certStatus == CertificateStatusEnum.ISSUED)
                        break;
                }
                catch (Exception) { }

                Thread.Sleep(_secondsBetweenEnrollmentRetries * 1000);
            }

            string pemCertificate = certStatus == CertificateStatusEnum.ISSUED ? JsonConvert.DeserializeObject<GETCertificateResponse>(_api.DownloadCertificate(dvCertificate.certificateId)).pems.certificate : string.Empty;

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);

            return new EnrollmentResult { 
                CARequestID = dvCertificate.certificateId,
                Certificate = pemCertificate,
                Status = APIProcessor.MapReturnStatus(certStatus),
                StatusMessage = $"GoDaddy Status = {certStatus.ToString()}"
            };
        }

        public override CAConnectorCertificate GetSingleRecord(string caRequestID)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            CertificateStatusEnum certStatus = CertificateStatusEnum.PENDING_ISSUANCE;
                
            GETCertificateDetailsResponse certResponse = JsonConvert.DeserializeObject<GETCertificateDetailsResponse>(_api.GetCertificate(caRequestID));
            Enum.TryParse(certResponse.status, out certStatus);

            string issuedCert = string.Empty;
            if (certStatus == CertificateStatusEnum.ISSUED || certStatus == CertificateStatusEnum.REVOKED || certStatus == CertificateStatusEnum.EXPIRED)
                issuedCert = JsonConvert.DeserializeObject<GETCertificateResponse>(_api.DownloadCertificate(caRequestID)).pems.certificate;

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);

            return new CAConnectorCertificate() {
                CARequestID = caRequestID,
                Certificate = issuedCert,
                CSR = certResponse.csr,
                ResolutionDate = certResponse.createdAt,
                RevocationDate = certResponse.revokedAt,
                RevocationReason = null,
                Status = APIProcessor.MapReturnStatus(certStatus),
                SubmissionDate = certResponse.createdAt
            };
        }

        public override int Revoke(string caRequestID, string hexSerialNumber, uint revocationReason)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            try
            {
                _api.RevokeCertificate(caRequestID, APIProcessor.MapRevokeReason(revocationReason));
            }
            catch (Exception ex)
            {
                return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
            
            return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.REVOKED);
        }

        public override void ValidateProductInfo(EnrollmentProductInfo productInfo, Dictionary<string, object> connectionInfo)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Trace);
        }

        #region Not Implemented
        #endregion
        #endregion


        #region Private Methods
        private void ValidateParameters<T>(Dictionary<string, T> connectionData, string[][] keysToValidate)
        {
            List<string> errors = new List<string>();

            foreach (string[] connectionKey in keysToValidate)
            {
                if (!connectionData.ContainsKey(connectionKey[0]))
                    errors.Add($"CAConnection configuration value {connectionKey} not found.");
                else if (connectionKey[1] == "int")
                {
                    int value;
                    bool isIntValue = int.TryParse(connectionData[connectionKey[0]].ToString(), out value);
                    if (!isIntValue)
                        errors.Add($"CAConnection configuration value {connectionKey} must contain an integer.  Found {connectionData[connectionKey[0]]}");
                }
            }

            if (errors.Count > 0)
                throw new GoDaddyException(string.Join(System.Environment.NewLine, errors.ToArray()));
        }
        #endregion
    }
}
