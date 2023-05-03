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

namespace Keyfactor.AnyGateway.GoDaddy.API
{
	internal class APIProcessor : LoggingClientBase
	{
		private string ApiUrl { get; set; }
		private string ApiKey { get; set; }
		private string ShopperId { get; set; }
		private int Timeout { get; set; }

		private const string NO_CERTS_PURCHASED_MESSAGE = "Failed to create certificate order";

		private const string NO_CERTS_PURCHASED_REPL_MESSAGE = "Failed to create certificate order.  This error often occurs if there are no certificates purchased to fulfill this enrollment request.  " +
			"Please check your GoDaddy account to make sure you have the correct SSL certificate product purchased to cover this enrollment.";

		private int NumberOfTimeOuts = 0;


        public APIProcessor(string apiUrl, string apiKey, string shopperId, int timeout)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			ApiUrl = apiUrl;
			ApiKey = apiKey;
			ShopperId = shopperId;
			Timeout = timeout;

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
			Logger.Trace($"Request Method: {request.Method.ToString()}");

			IRestResponse response;

			RestClient client = new RestClient(ApiUrl);
			request.AddHeader("Authorization", ApiKey);

			try
			{
				response = client.Execute(request);
				if (response.ResponseStatus == ResponseStatus.TimedOut)
				{
					NumberOfTimeOuts++;
					throw new GoDaddyException("Request timed out ");
				}
			}
			catch (Exception ex)
			{
				string exceptionMessage = GoDaddyException.FlattenExceptionMessages(ex, $"Error processing {request.Resource}").Replace(NO_CERTS_PURCHASED_MESSAGE, NO_CERTS_PURCHASED_REPL_MESSAGE);
				Logger.Error(exceptionMessage);
				if (NumberOfTimeOuts > 5)
                    throw new Exception("Maximum timeouts of 5 exceeded.  " + exceptionMessage);
                else
                    throw new GoDaddyException(exceptionMessage);
			}
			Logger.Trace($"Response Status Code: {response.StatusCode}");

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

		#endregion Private Methods
	}
}