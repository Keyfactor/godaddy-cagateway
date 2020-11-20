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

        #region Interface Methods
        public override void Initialize(ICAConnectorConfigProvider configProvider)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Trace);
            foreach (KeyValuePair<string, object> configEntry in configProvider.CAConnectionData)
                Logger.Debug($"{configEntry.Key}: {configEntry.Value}");

            //TODO: Validate configuration properties

            string apiUrl = configProvider.CAConnectionData["ApiUrl"].ToString();
            string apiKey = configProvider.CAConnectionData["ApiKey"].ToString();
            string shopperId = configProvider.CAConnectionData["ShopperId"].ToString();

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

            //TODO Try/Catch
            string customerId = JsonConvert.DeserializeObject<GETShopperResponse>(_api.GetCustomerId()).customerId;
            GETCertificatesDetailsResponse certificates = JsonConvert.DeserializeObject<GETCertificatesDetailsResponse>(_api.GetCertificates(customerId));

            foreach(CertificateDetails certificate in certificates.certificates)
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

            ////TODO: Lee create GoDaddy Client to Async populate the blocking collection
            ////Task.Run(async() => await MethodToPopulateBc(bc,cancelToken);
            //MockUpClient client = new MockUpClient();

            //Task readCertsFromCA = Task.Run(() => MockUpClient.PopulateCertificateList(certsFromGoDaddy, cancelToken));

            ////TODO: Update cert variable with Go Daddy Certificate Response Model
            //string cert;
            //while (!certsFromGoDaddy.IsCompleted)
            //{
            //    if (readCertsFromCA.IsFaulted)
            //    {
            //        throw readCertsFromCA.Exception.Flatten();
            //    }

            //    //Process Certs. Task could be a long running task depending on CA Cert Count. Begin taking certs from queue being populated by task
            //    //TODO: Ensure out parametere matches Go Daddy Certificate model
            //    if (certsFromGoDaddy.TryTake(out cert, 10,cancelToken))
            //    {
            //        blockingBuffer.Add(new CAConnectorCertificate { 
            //            CARequestID="",
            //            Certificate="",
            //            CSR="",
            //            ResolutionDate=null,
            //            RevocationDate=null,
            //            RevocationReason=null,
            //            Status=20,
            //            SubmissionDate=DateTime.Now
            //        });
            //    }
            //}
            
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

            POSTCertificatesV1DVRequest certRequest = new POSTCertificatesV1DVRequest();
            certRequest.contact = new ContactInfo();
            certRequest.contact.email = productInfo.ProductParameters["Email"];
            certRequest.contact.nameFirst = productInfo.ProductParameters["FirstName"];
            certRequest.contact.nameLast = productInfo.ProductParameters["LastName"];
            certRequest.contact.phone = productInfo.ProductParameters["Phone"];
            certRequest.SetCSR(csr);
            
            //TODO validate int for period
            certRequest.period = Convert.ToInt32(productInfo.ProductParameters["CertificatePeriodInYears"]);
            certRequest.productType = productInfo.ProductID;
            //TODO - allow assignment of root type
            //certRequest.rootType = productInfo.ProductParameters["RootType"];

            //TODO - try/catch
            string response = _api.EnrollCSR(csr, certRequest);
            POSTCertificatesV1DVResponse dvCertificate = JsonConvert.DeserializeObject<POSTCertificatesV1DVResponse>(response);

            //TODO validate retries and time between retries
            //TODO encapsulate into one method for getting cert details and downloading the cert
            int certificateRetries = Convert.ToInt32(productInfo.ProductParameters["CertificateRetries"]);
            int secondsBetweenCertificateRetries = Convert.ToInt32(productInfo.ProductParameters["SecondsBetweenCertificateRetries"]);

            CertificateStatusEnum certStatus = CertificateStatusEnum.PENDING_ISSUANCE;
            for(int i = 0; i < certificateRetries; i++)
            {
                GETCertificateDetailsResponse certResponse = JsonConvert.DeserializeObject<GETCertificateDetailsResponse>(_api.GetCertificate(dvCertificate.certificateId));
                Enum.TryParse(certResponse.status, out certStatus);
                if (certStatus == CertificateStatusEnum.ISSUED)
                    break;
                Thread.Sleep(secondsBetweenCertificateRetries * 1000);
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

            //TODO - try/catch
            _api.RevokeCertificate(caRequestID, APIProcessor.MapRevokeReason(revocationReason));

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
    }
    #endregion

    public class MockUpClient
    {
        //Mock up of what it looks like to populate the blocking collection from a task
        public static void PopulateCertificateList(BlockingCollection<string> bc, CancellationToken ct)
        {
            string certModel;
            int itemsProcessed = 0;
            int totalItems = 100; //TODO: Get from Response
            do
            { 
                try
                {
                    certModel = "Parsed from JSON Response";

                    if (bc.TryAdd(certModel, 10, ct))
                    {
                        itemsProcessed++;   
                    }
                    else
                    {
                        //adding to the queue was blocked.  Try again
                    }
                }
                catch (OperationCanceledException)
                {
                    //Operation was canceld
                    bc.CompleteAdding();
                    break;
                }
            } while (itemsProcessed < totalItems);
            bc.CompleteAdding();
        }
    }

}
