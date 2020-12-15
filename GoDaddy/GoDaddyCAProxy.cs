﻿using CAProxy.AnyGateway.Interfaces;
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
        private string _rootType { get; set; }
        private int _syncPageSize { get; set; }
        private int _enrollmentRetries { get; set; }
        private int _secondsBetweenEnrollmentRetries { get; set; }
        private string[][] _connectionKeys = new string[][] { new string[] { "ApiUrl", "string" },
                                                         new string[] { "ApiKey", "string" },
                                                         new string[] { "ShopperId", "string" },
                                                         new string[] { "RootType", "string" },
                                                         new string[] { "SyncPageSize", "int" },
                                                         new string[] { "EnrollmentRetries", "int" },
                                                         new string[] { "SecondsBetweenEnrollmentRetries", "int" } };

        #region Interface Methods
        public override void Initialize(ICAConnectorConfigProvider configProvider)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            foreach (KeyValuePair<string, object> configEntry in configProvider.CAConnectionData)
                Logger.Trace($"{configEntry.Key}: {configEntry.Value}");
            ValidateParameters<object>(configProvider.CAConnectionData, _connectionKeys);

            string apiUrl = configProvider.CAConnectionData["ApiUrl"].ToString();
            string apiKey = configProvider.CAConnectionData["ApiKey"].ToString();
            string shopperId = configProvider.CAConnectionData["ShopperId"].ToString();
            _rootType = configProvider.CAConnectionData["RootType"].ToString();
            _syncPageSize = Convert.ToInt32(configProvider.CAConnectionData["SyncPageSize"]);
            _enrollmentRetries = Convert.ToInt32(configProvider.CAConnectionData["EnrollmentRetries"]);
            _secondsBetweenEnrollmentRetries = Convert.ToInt32(configProvider.CAConnectionData["SecondsBetweenEnrollmentRetries"]);

            _api = new APIProcessor(apiUrl, apiKey, shopperId);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

        public override void Ping()
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

        public override void ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            ValidateParameters<object>(connectionInfo, _connectionKeys);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }
        [Obsolete]
        public override void Synchronize(ICertificateDataReader certificateDataReader, BlockingCollection<CertificateRecord> blockingBuffer, CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken, string logicalName)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            throw new NotImplementedException();
        }

        public override void Synchronize(ICertificateDataReader certificateDataReader, BlockingCollection<CAConnectorCertificate> blockingBuffer, CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            string customerId = JsonConvert.DeserializeObject<GETShopperResponse>(_api.GetCustomerId()).customerId;

            int pageNumber = 1;
            bool wasLastPage = false;

            do
            {
                GETCertificatesDetailsResponse certificates = JsonConvert.DeserializeObject<GETCertificatesDetailsResponse>(_api.GetCertificates(customerId, pageNumber, _syncPageSize));
                
                foreach (CertificateDetails certificate in certificates.certificates)
                {
                    Thread.Sleep(1000);
                    try
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
                    catch (GoDaddyException) { }
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
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            throw new NotImplementedException();
        }

        public override EnrollmentResult Enroll(ICertificateDataReader certificateDataReader, string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            foreach (KeyValuePair<string, string> configEntry in productInfo.ProductParameters)
                Logger.Trace($"{configEntry.Key}: {configEntry.Value}");

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
            certRequest.rootType = _rootType;
            certRequest.slotSize = productInfo.ProductParameters.Keys.Contains("SlotSize") ? productInfo.ProductParameters["SlotSize"] : string.Empty;

            List<string> sans = new List<string>();
            foreach(string[] sanValues in san.Values)
            {
                foreach (string sanValue in sanValues)
                    sans.Add(sanValue);
            }
            certRequest.subjectAlternativeNames = sans.ToArray();

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

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            return new EnrollmentResult { 
                CARequestID = dvCertificate.certificateId,
                Certificate = pemCertificate,
                Status = APIProcessor.MapReturnStatus(certStatus),
                StatusMessage = $"GoDaddy Status = {certStatus.ToString()}"
            };
        }

        public override CAConnectorCertificate GetSingleRecord(string caRequestID)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            CertificateStatusEnum certStatus = CertificateStatusEnum.PENDING_ISSUANCE;
                
            GETCertificateDetailsResponse certResponse = JsonConvert.DeserializeObject<GETCertificateDetailsResponse>(_api.GetCertificate(caRequestID));
            Enum.TryParse(certResponse.status, out certStatus);

            string issuedCert = string.Empty;
            if (certStatus == CertificateStatusEnum.ISSUED || certStatus == CertificateStatusEnum.REVOKED || certStatus == CertificateStatusEnum.EXPIRED)
                issuedCert = JsonConvert.DeserializeObject<GETCertificateResponse>(_api.DownloadCertificate(caRequestID)).pems.certificate;

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

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
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            try
            {
                _api.RevokeCertificate(caRequestID, APIProcessor.MapRevokeReason(revocationReason));
            }
            catch (Exception ex)
            {
                return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.FAILED);
            }

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            
            return Convert.ToInt32(PKIConstants.Microsoft.RequestDisposition.REVOKED);
        }

        public override void ValidateProductInfo(EnrollmentProductInfo productInfo, Dictionary<string, object> connectionInfo)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

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
