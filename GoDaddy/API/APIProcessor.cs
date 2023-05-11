// Copyright 2021 Keyfactor
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using CSS.Common.Logging;
using Keyfactor.PKI;
using Keyfactor.AnyGateway.GoDaddy.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using Org.BouncyCastle.Ocsp;
using System.Runtime.ConstrainedExecution;

namespace Keyfactor.AnyGateway.GoDaddy.API
{
	internal class APIProcessor : LoggingClientBase
	{
		private string ApiUrl { get; set; }
		private string ApiKey { get; set; }
		private string ShopperId { get; set; }
		private int Timeout { get; set; }
        private int MaxNumberOfTimeouts { get; set; }

        private const string NO_CERTS_PURCHASED_MESSAGE = "Failed to create certificate order";

		private const string NO_CERTS_PURCHASED_REPL_MESSAGE = "Failed to create certificate order.  This error often occurs if there are no certificates purchased to fulfill this enrollment request.  " +
			"Please check your GoDaddy account to make sure you have the correct SSL certificate product purchased to cover this enrollment.";


		internal int TotalNumberOfTimeouts { get; set; } = 0;

        internal int TotalDurationOfDownloadApiCallsInMilliseconds { get; set; } = 0;


        public APIProcessor(string apiUrl, string apiKey, string shopperId, int timeout, int maxNumberOfTimeouts)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			ApiUrl = apiUrl;
			ApiKey = apiKey;
			ShopperId = shopperId;
			Timeout = timeout;
			MaxNumberOfTimeouts = maxNumberOfTimeouts;
			TotalNumberOfTimeouts = 0;

			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
		}

		public string EnrollCSR(string csr, POSTCertificateRequest requestBody)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			string rtnMessage = string.Empty;

			string RESOURCE = "v1/certificates";
			RestRequest request = new RestRequest(RESOURCE, Method.POST);

			request.AddJsonBody(requestBody);

			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

			Logger.Trace($"Json Request Body: {JsonConvert.SerializeObject(requestBody)}");

			return SubmitRequest(request);
		}

		public string RenewReissueCSR(string certificateId, string csr, POSTCertificateRenewalRequest requestBody, bool isRenew)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			string rtnMessage = string.Empty;
			string endpoint = isRenew ? "renew" : "reissue";

			string RESOURCE = $"v1/certificates/{certificateId}/{endpoint}";
			RestRequest request = new RestRequest(RESOURCE, Method.POST);

			request.AddJsonBody(requestBody);

			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

			Logger.Trace($"Json Request Body: {JsonConvert.SerializeObject(requestBody)}");

			return SubmitRequest(request);
		}

		public string GetCertificates(string customerId, int pageNumber, int pageSize, int maxRetries)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			string rtnMessage = string.Empty;

			string RESOURCE = $"v2/customers/{customerId}/certificates?offset={pageNumber.ToString()}&limit={pageSize.ToString()}";
			RestRequest request = new RestRequest(RESOURCE, Method.GET);

			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

            int retries = 0;
            while (true)
            {
                try
                {
                    rtnMessage = SubmitRequest(request);
                    break;
                }
                catch (GoDaddyTimeoutException ex)
                {
                    retries++;
                    if (retries > maxRetries)
                    {
						string msg = $"Maximum number of timeout retries of {maxRetries} exceeded for certificate page retrieval.";
                        Logger.Error(msg);
                        throw new GoDaddyMaxTimeoutException(msg);
                    }
                    else
                        continue;
                }
            }

            return rtnMessage;
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

		public string DownloadCertificate(string certificateId, int maxRetries)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			string cert = string.Empty;

			string RESOURCE = $"v1/certificates/{certificateId}/download";
			RestRequest request = new RestRequest(RESOURCE, Method.GET);

			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

			int retries = 0;
			while (true)
			{
				try
				{
					DateTime before = DateTime.Now;
					cert = SubmitRequest(request);
					DateTime after = DateTime.Now;
					TotalDurationOfDownloadApiCallsInMilliseconds += after.Subtract(before).Milliseconds;

					break;
				}
				catch (GoDaddyTimeoutException ex)
				{
					retries++;
					if (retries > maxRetries)
					{
						Logger.Warn($"Maximum number of timeout retries of {maxRetries} exceeded for certificate {certificateId} retrieval.  Certificate skipped.");
						throw ex;
					}
					else
						continue;
				}
			}

			return cert;
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

			Logger.Trace($"Json Request Body: {JsonConvert.SerializeObject(body)}");
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
					returnStatus = PKIConstants.Microsoft.RequestDisposition.EXTERNAL_VALIDATION;
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
			foreach (Parameter parameter in request.Parameters)
			{
				if (parameter.Name.ToLower() != "authorization")
					Logger.Trace($"{parameter.Name}: {parameter.Value.ToString()}");
			}
			Logger.Trace($"Request Method: {request.Method.ToString()}");

			IRestResponse response = null;

			RestClient client = new RestClient(ApiUrl);
			client.Timeout = Timeout;

			if (!request.Parameters.Exists(p => p.Name == "Authorization"))
				request.AddHeader("Authorization", ApiKey);

			try
			{
				response = client.Execute(request);
                Logger.Trace($"Http Status Code: {response.StatusCode}");
                Logger.Trace($"Response Status: {response.ResponseStatus}");

                if (response.ResponseStatus == ResponseStatus.TimedOut || response.StatusCode == 0)
				{
					string msg = "Request timed out. ";
                    TotalNumberOfTimeouts++;

                    if (TotalNumberOfTimeouts >= MaxNumberOfTimeouts)
                    {
                        msg += $"Maximum timeouts of {MaxNumberOfTimeouts} exceeded.  ";
                        throw new GoDaddyMaxTimeoutException(msg);
                    }
                    else
                    {
                        Logger.Debug(msg);
                        throw new GoDaddyTimeoutException(msg);
                    }
				}
			}
			catch (GoDaddyTimeoutException ex) { throw ex; }
			catch (Exception ex)
			{
				string exceptionMessage = GoDaddyException.FlattenExceptionMessages(ex, $"Error processing {request.Resource}").Replace(NO_CERTS_PURCHASED_MESSAGE, NO_CERTS_PURCHASED_REPL_MESSAGE);
				Logger.Error(exceptionMessage);
				throw ex;
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
					if (error == null)
						errorMessage = "No error message returned.";
					else
						errorMessage = $"{error.code}: {error.message}";
				}
				catch (JsonReaderException)
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

		#endregion Private Methods
	}
}