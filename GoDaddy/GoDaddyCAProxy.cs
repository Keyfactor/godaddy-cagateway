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

using CAProxy.AnyGateway.Interfaces;using CAProxy.AnyGateway.Models;using CAProxy.Common;using CSS.Common;using CSS.Common.Logging;using Keyfactor.PKI;using Keyfactor.PKI.PEM;using System;using System.Collections.Concurrent;using System.Collections.Generic;using System.Linq;using System.Threading;using System.Threading.Tasks;using Newtonsoft.Json;using Keyfactor.AnyGateway.GoDaddy.API;using Keyfactor.AnyGateway.GoDaddy.Models;namespace Keyfactor.AnyGateway.GoDaddy{
	public class GoDaddyCAProxy : CAProxy.AnyGateway.BaseCAConnector
	{
		private APIProcessor _api;
		private string _rootType;

		private int _syncPageSize = 50;
		private const int SYNC_PAGE_SIZE_MIN = 10;
        private const int SYNC_PAGE_SIZE_MAX = 1000;

        private int _enrollmentRetries = 2;
        private const int ENROLLMENT_RETRIES_MIN = 0;
        private const int ENROLLMENT_RETRIES_MAX = 5;

        private int _secondsBetweenEnrollmentRetries = 5;
        private const int SECONDS_BETWEEN_ENROLLMENT_RETRIES_MIN = 2;
        private const int SECONDS_BETWEEN_ENROLLMENT_RETRIES_MAX = 20;

        private int _apiTimeoutInSeconds = 20;
        private const int API_TIMEOUT_IN_SECONDS_MIN = 2;
        private const int API_TIMEOUT_IN_SECONDS_MAX = 100;

        private int _numberOfCertDownloadRetriesBeforeSkip = 2;
        private const int NUMBER_OF_CERT_DOWNLOAD_RETRIES_BEFORE_SKIP_MIN = 0;
        private const int NUMBER_OF_CERT_DOWNLOAD_RETRIES_BEFORE_SKIP_MAX = 10;

        private int _numberOfTimeoutsBeforeSyncFailure = 100;
        private const int NUMBER_OF_TIMEOUTS_BEFORE_SYNC_FAILURE_MIN = 0;
        private const int NUMBER_OF_TIMEOUTS_BEFORE_SYNC_FAILURE_MAX = 5000;

        private int _millisecondsBetweenCertDownloads = 1000;
        private const int MILLISECONDS_BETWEEN_CERT_DOWNLOADS_MIN = 0;
        private const int MILLISECONDS_BETWEEN_CERT_DOWNLOADS_MAX = 1000;


        private string[][] _connectionKeys = new string[][] { new string[] { "ApiUrl", "string" },
														 new string[] { "ApiKey", "string" },
														 new string[] { "ShopperId", "string" },
														 new string[] { "RootType", "string" },
														 new string[] { "SyncPageSize", "int" } };


		#region Interface Methods
		public override void Initialize(ICAConnectorConfigProvider configProvider)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			Logger.Trace("GATEWAY CONFIG SETTINGS:");
			foreach (KeyValuePair<string, object> configEntry in configProvider.CAConnectionData)
			{
				if (configEntry.Key.ToLower() != "apikey")
					Logger.Trace($"  {configEntry.Key}: {configEntry.Value}");
			}
			ValidateParameters<object>(configProvider.CAConnectionData, _connectionKeys);

			string apiUrl = configProvider.CAConnectionData["ApiUrl"].ToString();
			string apiKey = configProvider.CAConnectionData["ApiKey"].ToString();
			string shopperId = configProvider.CAConnectionData["ShopperId"].ToString();
			_rootType = configProvider.CAConnectionData["RootType"].ToString();

			//optional parameters
			bool isInt;
			int tempInt;

            if (configProvider.CAConnectionData.ContainsKey("SyncPageSize"))
            {
                isInt = int.TryParse(configProvider.CAConnectionData["SyncPageSize"].ToString(), out tempInt);
                _syncPageSize = !isInt || tempInt < SYNC_PAGE_SIZE_MIN || tempInt > SYNC_PAGE_SIZE_MAX ? _syncPageSize : tempInt;
            }

            if (configProvider.CAConnectionData.ContainsKey("EnrollmentRetries"))
            {
                isInt = int.TryParse(configProvider.CAConnectionData["EnrollmentRetries"].ToString(), out tempInt);
                _enrollmentRetries = !isInt || tempInt < ENROLLMENT_RETRIES_MIN || tempInt > ENROLLMENT_RETRIES_MAX ? _enrollmentRetries : tempInt;
            }

            if (configProvider.CAConnectionData.ContainsKey("SecondsBetweenEnrollmentRetries"))
            {
                isInt = int.TryParse(configProvider.CAConnectionData["SecondsBetweenEnrollmentRetries"].ToString(), out tempInt);
                _secondsBetweenEnrollmentRetries = !isInt || tempInt < SECONDS_BETWEEN_ENROLLMENT_RETRIES_MIN || tempInt > SECONDS_BETWEEN_ENROLLMENT_RETRIES_MAX ? _secondsBetweenEnrollmentRetries : tempInt;
            }

            if (configProvider.CAConnectionData.ContainsKey("ApiTimeoutinSeconds"))
			{
				isInt = int.TryParse(configProvider.CAConnectionData["ApiTimeoutinSeconds"].ToString(), out tempInt);
                _apiTimeoutInSeconds = !isInt || tempInt < API_TIMEOUT_IN_SECONDS_MIN || tempInt > API_TIMEOUT_IN_SECONDS_MAX ? _apiTimeoutInSeconds : tempInt;
			}

            if (configProvider.CAConnectionData.ContainsKey("NumberOfCertDownloadRetriesBeforeSkip"))
            {
                isInt = int.TryParse(configProvider.CAConnectionData["NumberOfCertDownloadRetriesBeforeSkip"].ToString(), out tempInt);
                _numberOfCertDownloadRetriesBeforeSkip = !isInt || tempInt < NUMBER_OF_CERT_DOWNLOAD_RETRIES_BEFORE_SKIP_MIN || tempInt > NUMBER_OF_CERT_DOWNLOAD_RETRIES_BEFORE_SKIP_MAX ? _numberOfCertDownloadRetriesBeforeSkip : tempInt;
            }

            if (configProvider.CAConnectionData.ContainsKey("NumberOfTimeoutsBeforeSyncFailure"))
            {
                isInt = int.TryParse(configProvider.CAConnectionData["NumberOfTimeoutsBeforeSyncFailure"].ToString(), out tempInt);
                _numberOfTimeoutsBeforeSyncFailure = !isInt || tempInt < NUMBER_OF_TIMEOUTS_BEFORE_SYNC_FAILURE_MIN || tempInt > NUMBER_OF_TIMEOUTS_BEFORE_SYNC_FAILURE_MAX ? _numberOfTimeoutsBeforeSyncFailure : tempInt;
            }

            if (configProvider.CAConnectionData.ContainsKey("MillisecondsBetweenCertDownloads"))
            {
                isInt = int.TryParse(configProvider.CAConnectionData["MillisecondsBetweenCertDownloads"].ToString(), out tempInt);
                _millisecondsBetweenCertDownloads = !isInt || tempInt < MILLISECONDS_BETWEEN_CERT_DOWNLOADS_MIN || tempInt > MILLISECONDS_BETWEEN_CERT_DOWNLOADS_MAX ? _millisecondsBetweenCertDownloads : tempInt;
            }

            _api = new APIProcessor(apiUrl, apiKey, shopperId, _apiTimeoutInSeconds * 1000, _numberOfTimeoutsBeforeSyncFailure);

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
            DateTime? overallLastSync = new DateTime(2020, 11, 11);

            string customerId = JsonConvert.DeserializeObject<GETShopperResponse>(_api.GetCustomerId()).customerId;

			int pageNumber = 1;
			bool wasLastPage = false;

			int totalNumberOfCertsFound = 0;
            int totalNumberOfCertsRetrieved = 0;
			_api.TotalNumberOfTimeouts = 0;
			_api.TotalDurationOfDownloadApiCallsInMilliseconds = 0;
            do
            {
				GETCertificatesDetailsResponse certificates = JsonConvert.DeserializeObject<GETCertificatesDetailsResponse>(_api.GetCertificates(customerId, pageNumber, _syncPageSize));
				if (!certificateAuthoritySyncInfo.DoFullSync && certificateAuthoritySyncInfo.OverallLastSync.HasValue)
					certificates.certificates = certificates.certificates.Where(p => p.completedAt.HasValue && p.completedAt.Value > overallLastSync.Value.AddDays(-1)).ToList();
                //certificates.certificates = certificates.certificates.Where(p => p.completedAt.HasValue && p.completedAt.Value > certificateAuthoritySyncInfo.OverallLastSync.Value.AddDays(-1)).ToList();

                foreach (CertificateDetails certificate in certificates.certificates)
				{
                    totalNumberOfCertsFound++;
					Thread.Sleep(_millisecondsBetweenCertDownloads);

					try
					{
						string issuedCert = RemovePEMHeader(JsonConvert.DeserializeObject<GETCertificateResponse>(_api.DownloadCertificate(certificate.certificateId, _numberOfCertDownloadRetriesBeforeSkip)).pems.certificate);
						
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

                        totalNumberOfCertsRetrieved++;
					}
					catch (GoDaddyMaxTimeoutException)
					{
						Logger.Error($"Sync failed due to maximum timeouts of {_numberOfTimeoutsBeforeSyncFailure.ToString()} being reached.");
						return;
					}
					catch (GoDaddyTimeoutException) { }
					catch (Exception) { }
				}

				wasLastPage = certificates.pagination.previous == certificates.pagination.last;
				pageNumber++;
			} while (!wasLastPage);

			blockingBuffer.CompleteAdding();

			string syncStats = "SYNC STATISTICS:" + System.Environment.NewLine;
			syncStats += $"  Total Certificates Found: {totalNumberOfCertsFound.ToString()}" + System.Environment.NewLine;
            syncStats += $"  Total Certificates Successfully Retrived: {totalNumberOfCertsRetrieved.ToString()}" + System.Environment.NewLine;
            syncStats += $"  Total Number of GoDaddy Timeouts When Attempting to Retrieve Certificates: {_api.TotalNumberOfTimeouts.ToString()}" + System.Environment.NewLine;

            int avgDurationApiCallsInMilliseconds = totalNumberOfCertsRetrieved == 0 ? 0 : (_api.TotalDurationOfDownloadApiCallsInMilliseconds / totalNumberOfCertsRetrieved);
            syncStats += $"  Average Time in Milliseconds For Each Successful GoDaddy Certificate Retrieval API Call: {avgDurationApiCallsInMilliseconds.ToString()}" + System.Environment.NewLine;

			Logger.Debug(syncStats);
			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
		}

		[Obsolete]
		public override EnrollmentResult Enroll(string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, CSS.PKI.PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

			throw new NotImplementedException();
		}

		public override EnrollmentResult Enroll(ICertificateDataReader certificateDataReader, string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, CSS.PKI.PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);

			foreach (KeyValuePair<string, string> configEntry in productInfo.ProductParameters)
			{
				Logger.Trace($"{configEntry.Key}: {configEntry.Value}");
			}

			string[][] parameterKeys;
			if (enrollmentType == RequestUtilities.EnrollmentType.New)
				parameterKeys = new string[][] { new string[] { "Email", "string" },
														 new string[] { "FirstName", "string" },
														 new string[] { "LastName", "string" },
														 new string[] { "Phone", "string" },
														 new string[] { "CertificatePeriodInYears", "int" } };
			else
				parameterKeys = new string[][] { new string[] { "PriorCertSN", "string" } };

			ValidateParameters<string>(productInfo.ProductParameters, parameterKeys);

			POSTCertificateEnrollmentResponse enrollmentResponse;

			try
			{
				switch (enrollmentType)
				{
					case RequestUtilities.EnrollmentType.New:
						switch (productInfo.ProductID)
						{
							case "DV_SSL":
							case "DV_WILDCARD_SSL":
							case "UCC_DV_SSL":
								enrollmentResponse = EnrollDV(productInfo, csr, san);
								break;

							case "OV_SSL":
							case "OV_CS":
							case "OV_DS":
							case "OV_WILDCARD_SSL":
							case "UCC_OV_SSL":
								enrollmentResponse = EnrollOV(productInfo, csr, san);
								break;

							case "EV_SSL":
							case "UCC_EV_SSL":
								enrollmentResponse = EnrollEV(productInfo, csr, san);
								break;

							default:
								return new EnrollmentResult { Status = 30, StatusMessage = $"Error attempting to enroll certificate {subject}: Invalid Product ID - {productInfo.ProductID}." };
						}

						break;

					case RequestUtilities.EnrollmentType.Renew:
					case RequestUtilities.EnrollmentType.Reissue:
						CAConnectorCertificate certificate = certificateDataReader.GetCertificateRecord(DataConversion.HexToBytes(productInfo.ProductParameters["PriorCertSN"]));
						enrollmentResponse = RenewReissue(certificate.CARequestID, productInfo, csr, san, enrollmentType == RequestUtilities.EnrollmentType.Renew);
						break;

					default:
						return new EnrollmentResult { Status = 30, StatusMessage = $"Unsupported EnrollmentType: {enrollmentType}" };
				}
			}
			catch (Exception ex)
			{
				return new EnrollmentResult { Status = 30, StatusMessage = $"Error attempting to enroll certificate {subject}: {ex.Message}." };
			}			Logger.Trace($"Enrollment issued for certificate ID {enrollmentResponse.certificateId}");

			CertificateStatusEnum certStatus = CertificateStatusEnum.PENDING_ISSUANCE;
			for (int i = 0; i < _enrollmentRetries; i++)
			{
				try
				{
					GETCertificateDetailsResponse certResponse = JsonConvert.DeserializeObject<GETCertificateDetailsResponse>(_api.GetCertificate(enrollmentResponse.certificateId));
					Enum.TryParse(certResponse.status, out certStatus);
					if (certStatus == CertificateStatusEnum.ISSUED)
						break;
				}
				catch (Exception exc)				{					string errMsg = $"Error retrieving certificate fails for ID {enrollmentResponse.certificateId}:\n{LogHandler.FlattenException(exc)}";					if (i + 1 < _enrollmentRetries)
					{
						errMsg += $"\nRetrying... (Attempt {i + 1} of {_enrollmentRetries})";
					}					else
					{
						errMsg += $"Retrieving certificate failed.";
					}					Logger.Error(errMsg);				}

				Thread.Sleep(_secondsBetweenEnrollmentRetries * 1000);
			}

			string pemCertificate = certStatus == CertificateStatusEnum.ISSUED 
				? RemovePEMHeader(JsonConvert.DeserializeObject<GETCertificateResponse>(_api.DownloadCertificate(enrollmentResponse.certificateId, _numberOfCertDownloadRetriesBeforeSkip)).pems.certificate)
				: string.Empty;

			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

			return new EnrollmentResult
			{
				CARequestID = enrollmentResponse.certificateId,
				Certificate = pemCertificate,
				Status = APIProcessor.MapReturnStatus(certStatus),
				StatusMessage = $"GoDaddy Status = {certStatus.ToString()}"
			};
		}

		public override CAConnectorCertificate GetSingleRecord(string caRequestID)
		{
			Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
			Logger.Trace($"Getting record for CARequestID: {caRequestID}");
			CertificateStatusEnum certStatus = CertificateStatusEnum.PENDING_ISSUANCE;

			GETCertificateDetailsResponse certResponse = JsonConvert.DeserializeObject<GETCertificateDetailsResponse>(_api.GetCertificate(caRequestID));
			Enum.TryParse(certResponse.status, out certStatus);

			string issuedCert = string.Empty;
			if (certStatus == CertificateStatusEnum.ISSUED || certStatus == CertificateStatusEnum.REVOKED || certStatus == CertificateStatusEnum.EXPIRED)
			{
				issuedCert = RemovePEMHeader(JsonConvert.DeserializeObject<GETCertificateResponse>(_api.DownloadCertificate(caRequestID, _numberOfCertDownloadRetriesBeforeSkip)).pems.certificate);
			}

			Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);

			return new CAConnectorCertificate()
			{
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


		#endregion Interface Methods

		#region Private Methods
		private string RemovePEMHeader(string pem)
		{
			return string.IsNullOrEmpty(pem) ? string.Empty : PemUtilities.DERToPEM(PemUtilities.PEMToDER(pem), PemUtilities.PemObjectType.NoHeaders);
		}

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

		private POSTCertificateEnrollmentResponse EnrollDV(EnrollmentProductInfo productInfo, string csr, Dictionary<string, string[]> san)
		{
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
			certRequest.slotSize = productInfo.ProductParameters.Keys.Contains("SlotSize") ? productInfo.ProductParameters["SlotSize"] : "FIVE";

			List<string> sans = new List<string>();
			foreach (string[] sanValues in san.Values)
			{
				foreach (string sanValue in sanValues)
				{
					sans.Add(sanValue);
				}
			}
			certRequest.subjectAlternativeNames = sans.ToArray();

			string response = _api.EnrollCSR(csr, certRequest);

			return JsonConvert.DeserializeObject<POSTCertificateEnrollmentResponse>(response);
		}

		private POSTCertificateEnrollmentResponse EnrollOV(EnrollmentProductInfo productInfo, string csr, Dictionary<string, string[]> san)
		{
			POSTCertificatesV1OVRequest certRequest = new POSTCertificatesV1OVRequest();

			certRequest.contact = new ContactInfo();
			certRequest.contact.jobTitle = productInfo.ProductParameters["JobTitle"];
			certRequest.contact.email = productInfo.ProductParameters["Email"];
			certRequest.contact.nameFirst = productInfo.ProductParameters["FirstName"];
			certRequest.contact.nameLast = productInfo.ProductParameters["LastName"];
			certRequest.contact.phone = productInfo.ProductParameters["Phone"];

			certRequest.organization = new OrganizationInfo();
			certRequest.organization.address = new AddressInfo();
			certRequest.organization.address.address1 = productInfo.ProductParameters["OrganizationAddress"];
			certRequest.organization.address.city = productInfo.ProductParameters["OrganizationCity"];
			certRequest.organization.address.state = productInfo.ProductParameters["OrganizationState"];
			certRequest.organization.address.country = productInfo.ProductParameters["OrganizationCountry"];
			certRequest.organization.name = productInfo.ProductParameters["OrganizationName"];
			certRequest.organization.phone = productInfo.ProductParameters["OrganizationPhone"];

			certRequest.SetCSR(csr);

			certRequest.period = Convert.ToInt32(productInfo.ProductParameters["CertificatePeriodInYears"]);
			certRequest.productType = productInfo.ProductID;
			certRequest.rootType = _rootType;
			certRequest.slotSize = productInfo.ProductParameters.Keys.Contains("SlotSize") ? productInfo.ProductParameters["SlotSize"] : "FIVE";

			List<string> sans = new List<string>();
			foreach (string[] sanValues in san.Values)
			{
				foreach (string sanValue in sanValues)
					sans.Add(sanValue);
			}
			certRequest.subjectAlternativeNames = sans.ToArray();

			string response = _api.EnrollCSR(csr, certRequest);
			return JsonConvert.DeserializeObject<POSTCertificateEnrollmentResponse>(response);
		}

		private POSTCertificateEnrollmentResponse EnrollEV(EnrollmentProductInfo productInfo, string csr, Dictionary<string, string[]> san)
		{
			POSTCertificatesV1EVRequest certRequest = new POSTCertificatesV1EVRequest();

			certRequest.contact = new ContactInfo();
			certRequest.contact.jobTitle = productInfo.ProductParameters["JobTitle"];
			certRequest.contact.email = productInfo.ProductParameters["Email"];
			certRequest.contact.nameFirst = productInfo.ProductParameters["FirstName"];
			certRequest.contact.nameLast = productInfo.ProductParameters["LastName"];
			certRequest.contact.phone = productInfo.ProductParameters["Phone"];

			certRequest.organization = new OrganizationInfo();
			certRequest.organization.address = new AddressInfo();
			certRequest.organization.address.address1 = productInfo.ProductParameters["OrganizationAddress"];
			certRequest.organization.address.city = productInfo.ProductParameters["OrganizationCity"];
			certRequest.organization.address.state = productInfo.ProductParameters["OrganizationState"];
			certRequest.organization.address.country = productInfo.ProductParameters["OrganizationCountry"];
			certRequest.organization.name = productInfo.ProductParameters["OrganizationName"];
			certRequest.organization.phone = productInfo.ProductParameters["OrganizationPhone"];

			certRequest.organization.jurisdictionOfIncorporation = new JurisdictionInfo();
			certRequest.organization.jurisdictionOfIncorporation.state = productInfo.ProductParameters["JurisdictionState"];
			certRequest.organization.jurisdictionOfIncorporation.country = productInfo.ProductParameters["JurisdictionCountry"];
			certRequest.organization.registrationNumber = productInfo.ProductParameters["RegistrationNumber"];

			certRequest.SetCSR(csr);

			certRequest.period = Convert.ToInt32(productInfo.ProductParameters["CertificatePeriodInYears"]);
			certRequest.productType = productInfo.ProductID;
			certRequest.rootType = _rootType;
			certRequest.slotSize = productInfo.ProductParameters.Keys.Contains("SlotSize") ? productInfo.ProductParameters["SlotSize"] : "FIVE";

			List<string> sans = new List<string>();
			foreach (string[] sanValues in san.Values)
			{
				foreach (string sanValue in sanValues)
					sans.Add(sanValue);
			}
			certRequest.subjectAlternativeNames = sans.ToArray();

			string response = _api.EnrollCSR(csr, certRequest);
			return JsonConvert.DeserializeObject<POSTCertificateEnrollmentResponse>(response);
		}

		private POSTCertificateEnrollmentResponse RenewReissue(string certificateId, EnrollmentProductInfo productInfo, string csr, Dictionary<string, string[]> san, bool isRenew)
		{
			POSTCertificateRenewalRequest certRequest = new POSTCertificateRenewalRequest();

			certRequest.SetCSR(csr);
			certRequest.rootType = _rootType;

			List<string> sans = new List<string>();
			foreach (string[] sanValues in san.Values)
			{
				foreach (string sanValue in sanValues)
					sans.Add(sanValue);
			}
			certRequest.subjectAlternativeNames = sans.ToArray();

			string response = _api.RenewReissueCSR(certificateId, csr, certRequest, isRenew);
			return JsonConvert.DeserializeObject<POSTCertificateEnrollmentResponse>(response);
		}





		#endregion Private Methods    }}