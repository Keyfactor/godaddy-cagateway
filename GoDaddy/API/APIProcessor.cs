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
            ApiUrl = apiUrl;
            ApiKey = apiKey;
            ShopperId = shopperId;
        }

        public string EnrollCSR(string csr, POSTCertificatesV1DVRequest requestBody)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = "v1/certificates";
            RestRequest request = new RestRequest(RESOURCE, Method.POST);

            request.AddJsonBody(requestBody);
            return SubmitRequest(request);
        }


        public string GetCertificates(string customerId)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v2/customers/{customerId}/certificates";
            RestRequest request = new RestRequest(RESOURCE, Method.GET);

            return SubmitRequest(request);
        }
        
        public string GetCertificate(string certificateId)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}";
            RestRequest request = new RestRequest(RESOURCE, Method.GET);

            return SubmitRequest(request);
        }

        public string DownloadCertificate(string certificateId)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}/download";
            RestRequest request = new RestRequest(RESOURCE, Method.GET);

            return SubmitRequest(request);
        }
        
        public void RevokeCertificate(string certificateId, POSTCertificateRevokeRequest.REASON reason)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}/revoke";
            RestRequest request = new RestRequest(RESOURCE, Method.POST);

            POSTCertificateRevokeRequest body = new POSTCertificateRevokeRequest();
            body.reason = reason.ToString();

            request.AddJsonBody(body);
            SubmitRequest(request);
        }

        public string GetCustomerId()
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/shoppers/{ShopperId}?includes=customerId";
            RestRequest request = new RestRequest(RESOURCE, Method.GET);

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
            string rtnMessage;
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
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }

                string exceptionMessage = $"Error processing {request.Resource}: {rtnMessage}";
                Logger.Error(exceptionMessage);
                throw new GoDaddyException(exceptionMessage);
            }
            else
                rtnMessage = response.Content;

            return rtnMessage;
        }
        #endregion

















        //public string GetCertificateActions(string certificateId)
        //{
        //    string rtnMessage = string.Empty;

        //    string RESOURCE = $"v1/certificates/{certificateId}/actions";

        //    RestClient client = new RestClient(URL);
        //    RestRequest request = new RestRequest(RESOURCE, Method.GET);
        //    request.AddHeader("Authorization", AUTH);

        //    IRestResponse response = client.Execute(request);

        //    if (response.StatusCode != System.Net.HttpStatusCode.OK &&
        //        response.StatusCode != System.Net.HttpStatusCode.Accepted &&
        //        response.StatusCode != System.Net.HttpStatusCode.Created &&
        //        response.StatusCode != System.Net.HttpStatusCode.NoContent)
        //    {
        //        try
        //        {
        //            APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
        //            rtnMessage = $"{error.code}: {error.message}";
        //        }
        //        catch (JsonReaderException ex)
        //        {
        //            rtnMessage = response.Content;
        //        }
        //    }

        //    return response.Content;
        //}

        //public string GetShopper(string shopperId)
        //{
        //    string rtnMessage = string.Empty;

        //    string RESOURCE = $"v1/shoppers/{shopperId}?includes=customerId";

        //    RestClient client = new RestClient(URL);
        //    RestRequest request = new RestRequest(RESOURCE, Method.GET);
        //    request.AddHeader("Authorization", AUTH);

        //    IRestResponse response = client.Execute(request);

        //    if (response.StatusCode != System.Net.HttpStatusCode.OK &&
        //        response.StatusCode != System.Net.HttpStatusCode.Accepted &&
        //        response.StatusCode != System.Net.HttpStatusCode.Created &&
        //        response.StatusCode != System.Net.HttpStatusCode.NoContent)
        //    {   
        //        try
        //        {
        //            APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
        //            rtnMessage = $"{error.code}: {error.message}";
        //        }
        //        catch (JsonReaderException ex)
        //        {
        //            rtnMessage = response.Content;
        //        }
        //    }

        //    return response.Content;
        //}


        //public string GetDomains()
        //{
        //    string rtnMessage = string.Empty;

        //    string RESOURCE = $"v1/domains";

        //    RestClient client = new RestClient(URL);
        //    RestRequest request = new RestRequest(RESOURCE, Method.GET);
        //    request.AddHeader("Authorization", AUTH);
        //    request.AddHeader("X-Shopper-Id", "305145420");

        //    IRestResponse response = client.Execute(request);

        //    if (response.StatusCode != System.Net.HttpStatusCode.OK &&
        //        response.StatusCode != System.Net.HttpStatusCode.Accepted &&
        //        response.StatusCode != System.Net.HttpStatusCode.Created &&
        //        response.StatusCode != System.Net.HttpStatusCode.NoContent)
        //    {
        //        try
        //        {
        //            APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
        //            rtnMessage = $"{error.code}: {error.message}";
        //        }
        //        catch (JsonReaderException ex)
        //        {
        //            rtnMessage = response.Content;
        //        }
        //    }

        //    return response.Content;
        //}
    }
}
