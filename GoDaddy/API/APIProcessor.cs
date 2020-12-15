using CSS.Common.Logging;
using CSS.PKI;
using Keyfactor.AnyGateway.GoDaddy.Models;
using Newtonsoft.Json;
using RestSharp;
using System;

namespace Keyfactor.AnyGateway.GoDaddy.API
{
    class APIProcessor : LoggingClientBase
    {
        private string ApiUrl { get; set; }
        private string ApiKey { get; set; }
        private string ShopperId { get; set; }

        public APIProcessor(string apiUrl, string apiKey, string shopperId)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            ApiUrl = apiUrl;
            ApiKey = apiKey;
            ShopperId = shopperId;

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

        public string EnrollCSR(string csr, POSTCertificatesV1DVRequest requestBody)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            
            string rtnMessage = string.Empty;

            string RESOURCE = "v1/certificates";
            RestRequest request = new RestRequest(RESOURCE, Method.POST);

            request.AddJsonBody(requestBody);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            return SubmitRequest(request);
        }


        public string GetCertificates(string customerId, int pageNumber, int pageSize)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            string rtnMessage = string.Empty;

            string RESOURCE = $"v2/customers/{customerId}/certificates?offset={pageNumber.ToString()}&limit={pageSize.ToString()}";
            RestRequest request = new RestRequest(RESOURCE, Method.GET);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            return SubmitRequest(request);
        }

        public string GetCertificate(string certificateId)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}";
            RestRequest request = new RestRequest(RESOURCE, Method.GET);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            return SubmitRequest(request);
        }

        public string DownloadCertificate(string certificateId)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}/download";
            RestRequest request = new RestRequest(RESOURCE, Method.GET);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            return SubmitRequest(request);
        }
        
        public void RevokeCertificate(string certificateId, POSTCertificateRevokeRequest.REASON reason)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}/revoke";
            RestRequest request = new RestRequest(RESOURCE, Method.POST);

            POSTCertificateRevokeRequest body = new POSTCertificateRevokeRequest();
            body.reason = reason.ToString();

            request.AddJsonBody(body);
            SubmitRequest(request);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

        public string GetCustomerId()
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/shoppers/{ShopperId}?includes=customerId";
            RestRequest request = new RestRequest(RESOURCE, Method.GET);

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            return SubmitRequest(request);
        }

        public static int MapReturnStatus(CertificateStatusEnum status)
        {
            PKIConstants.Microsoft.RequestDisposition returnStatus = PKIConstants.Microsoft.RequestDisposition.UNKNOWN;

            switch (status)
            {
                case CertificateStatusEnum.DENIED:
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.DENIED;
                    break;
                case CertificateStatusEnum.EXPIRED:
                case CertificateStatusEnum.CURRENT:
                case CertificateStatusEnum.ISSUED:
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.ISSUED;
                    break;
                case CertificateStatusEnum.PENDING_ISSUANCE:
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.PENDING;
                    break;
                case CertificateStatusEnum.REVOKED:
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.REVOKED;
                    break;
                default:
                    returnStatus = PKIConstants.Microsoft.RequestDisposition.FAILED;
                    break;
            }

            return Convert.ToInt32(returnStatus);
        }

        public static POSTCertificateRevokeRequest.REASON MapRevokeReason(uint reason)
        {
            POSTCertificateRevokeRequest.REASON returnReason = POSTCertificateRevokeRequest.REASON.PRIVILEGE_WITHDRAWN;

            switch (reason)
            {
                case 1:
                    returnReason = POSTCertificateRevokeRequest.REASON.KEY_COMPROMISE;
                    break;
                case 3:
                    returnReason = POSTCertificateRevokeRequest.REASON.AFFILIATION_CHANGED;
                    break;
                case 4:
                    returnReason = POSTCertificateRevokeRequest.REASON.SUPERSEDED;
                    break;
                case 5:
                    returnReason = POSTCertificateRevokeRequest.REASON.CESSATION_OF_OPERATION;
                    break;
            }

            return returnReason;
        }


        #region Private Methods
        private string SubmitRequest(RestRequest request)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            Logger.Trace($"Request Resource: {request.Resource}");
            Logger.Trace($"Request Method: {request.Method.ToString()}");
            Logger.Trace($"Request Body: {(request.Body == null ? string.Empty : request.Body.Value.ToString())}");

            IRestResponse response;

            RestClient client = new RestClient(ApiUrl);
            request.AddHeader("Authorization", ApiKey);

            try
            {
                response = client.Execute(request);
            }
            catch (Exception ex)
            {
                string exceptionMessage = GoDaddyException.FlattenExceptionMessages(ex, $"Error processing {request.Resource}");
                Logger.Error(exceptionMessage);
                throw new GoDaddyException(exceptionMessage);
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                string errorMessage;

                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    errorMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    errorMessage = response.Content;
                }

                string exceptionMessage = $"Error processing {request.Resource}: {errorMessage}";
                Logger.Error(exceptionMessage);
                throw new GoDaddyException(exceptionMessage);
            }

            Logger.Trace($"API Result: {response.Content}");
            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            return response.Content;
        }
        #endregion
    }
}
